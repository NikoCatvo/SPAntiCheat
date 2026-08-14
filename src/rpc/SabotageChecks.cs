using Hazel;
using InnerNet;
using AmongUs.GameOptions;
using System;
using System.Collections.Generic;

namespace NitroShield.Rpc
{
    internal class UpdateSystem : RpcCheck
    {
        private static readonly SystemTypes[] UpdatableWhenDead =
        {
            SystemTypes.MedBay, SystemTypes.Sabotage, SystemTypes.Security, SystemTypes.Ventilation
        };

        public override System.Type GetExpectedNetObject() => typeof(ShipStatus);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            SystemTypes system = (SystemTypes)reader.ReadByte();
            player = reader.ReadNetObject<PlayerControl>();
            if (player == null) return;
            if (Anticheat.IsExempt(player)) return;

            if (ShipStatus.Instance == null || !ShipStatus.Instance.Systems.ContainsKey(system))
            {
                Anticheat.Flag(player, Strings.ViolationSystemNotFound(Anticheat.Name(player), Strings.SystemName(system)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }

            if (player.Data.IsDead && Array.IndexOf(UpdatableWhenDead, system) < 0)
            {
                Anticheat.Flag(player, Strings.ViolationSystemDead(Anticheat.Name(player), Strings.SystemName(system)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }

            switch (system)
            {
                case SystemTypes.Electrical:
                    ValidateSwitches(player, reader, ref blockRpc);
                    break;
                case SystemTypes.Sabotage:
                    ValidateSabotage(player, reader, ref blockRpc);
                    break;
                case SystemTypes.Comms:
                case SystemTypes.Reactor:
                case SystemTypes.Laboratory:
                case SystemTypes.HeliSabotage:
                case SystemTypes.LifeSupp:
                case SystemTypes.MushroomMixupSabotage:
                    ValidateSabotageActivation(player, system, reader, ref blockRpc);
                    break;
            }
        }

        // 直接发送的破坏激活检测（如 RpcUpdateSystem(SystemTypes.Comms, 128)）
        // 游戏中正常流程不会直接从客户端发送此 RPC，出现即为外挂
        private static void ValidateSabotageActivation(PlayerControl player, SystemTypes system, MessageReader reader, ref bool blockRpc)
        {
            byte count = reader.ReadByte();

            // 会议期间（已超过 5 秒宽限）的任何破坏激活均视为违规
            if (Anticheat.IsMeetingBlockActive())
            {
                Anticheat.Flag(player, Strings.ViolationSabotageDuringMeeting(Anticheat.Name(player), Strings.SystemName(system)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }

            // count == 128 为破坏激活（OnyxMenu/BetterAmongUs 均采用此值）
            // 修复操作通常为 16/17，不触发 0x80
            bool isActivation = count == 128;

            if (isActivation)
            {
                // 非内鬼激活破坏 = 违规
                if (player.Data != null && !RoleManager.IsImpostorRole(player.Data.RoleType))
                {
                    string msg;
                    if (system == SystemTypes.Comms)
                        msg = Strings.ViolationCommsSabotage(Anticheat.Name(player));
                    else if (system == SystemTypes.LifeSupp)
                        msg = Strings.ViolationOxygenSabotage(Anticheat.Name(player));
                    else
                        msg = Strings.ViolationSabotageNotImpostor(Anticheat.Name(player), Strings.SystemName(system));
                    Anticheat.Flag(player, msg);
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    return;
                }
            }
            else
            {
                // 修复操作（如 count=16, 17 修复通讯台）
                // 仅当系统当前未激活时，修复操作可疑
                try
                {
                    var sysObj = ShipStatus.Instance.Systems[system];
                    var activatable = sysObj.Cast<IActivatable>();
                    if (activatable != null && !activatable.IsActive)
                    {
                        // 系统未激活但有人发送修复 = 可疑
                        // 不阻止，仅标记
                    }
                }
                catch { }
            }

            // 捉迷藏模式的破坏始终视为违规
            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
            {
                Anticheat.Flag(player, Strings.ViolationSabotageHideSeek(Anticheat.Name(player), Strings.SystemName(system)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
        }

private static void ValidateSabotage(PlayerControl player, MessageReader reader, ref bool blockRpc)
{
    SystemTypes target = (SystemTypes)reader.ReadByte();

    // 1. 会议期间且已超过 5 秒的破坏视为违规（不区分角色）
    if (Anticheat.IsMeetingBlockActive())
    {
        Anticheat.Flag(player, Strings.ViolationSabotageDuringMeeting(Anticheat.Name(player), Strings.SystemName(target)));
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
        return;
    }

    if (!MapUtil.IsValidSabotageTarget(target))
    {
        Anticheat.Flag(player, Strings.ViolationInvalidSabotageTarget(Anticheat.Name(player), Strings.SystemName(target)));
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
        return;
    }

    // 2. 通讯破坏（Comms）仅在非内鬼时阻止，内鬼可正常进行
    if (target == SystemTypes.Comms && player.Data != null && !RoleManager.IsImpostorRole(player.Data.RoleType))
    {
        Anticheat.Flag(player, Strings.ViolationCommsSabotage(Anticheat.Name(player)));
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
        return;
    }

    // 3. 其他破坏，非内鬼标记（排除已处理的 Comms）
    if (target != SystemTypes.Comms && player.Data != null && !RoleManager.IsImpostorRole(player.Data.RoleType))
    {
        Anticheat.Flag(player, Strings.ViolationSabotageNotImpostor(Anticheat.Name(player), Strings.SystemName(target)));
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
    }

    // 4. 捉迷藏模式的破坏始终视为违规
    if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
    {
        Anticheat.Flag(player, Strings.ViolationSabotageHideSeek(Anticheat.Name(player), Strings.SystemName(target)));
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
    }
}

        private static void ValidateSwitches(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            byte switches = reader.ReadByte();

            if ((switches & 128) != 0)
            {
                Anticheat.Flag(player, Strings.ViolationSwitchCrash(Anticheat.Name(player), switches));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
            else
            {
                // Airship 有 8 个开关（最多 0~7），Skeld/Mira 有 6 个（最多 0~5）
                // 按当前地图限制上限，避免误判合法开关
                uint maxSwitches = MapUtil.GetCurrentMap() == MapNames.Airship ? 7u : 5u;
                if (switches > maxSwitches)
                {
                    Anticheat.Flag(player, Strings.ViolationInvalidSwitch(Anticheat.Name(player), switches));
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                }
            }

            if (MeetingHud.Instance != null)
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
        }
    }

    internal class CloseDoorsOfType : RpcCheck
    {
        public override System.Type GetExpectedNetObject() => typeof(ShipStatus);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            // 躲猫猫（Hide & Seek）模式下关门是作弊行为：
            // 内鬼（Seeker）不能破坏、不能关门，普通玩家也没有合法关门途径。
            // 注意：CloseDoorsOfType 是 ShipStatus 级别的 RPC，报文中不含发送者
            // 玩家（与 Hydra 一致，无法定位具体是谁），因此用匿名提示 + 阻断。
            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
            {
                Anticheat.Flag(Strings.ViolationHideSeekCloseDoors);
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
        }
    }
}
