using Hazel;
using UnityEngine;

namespace NitroShield.Rpc
{
    internal class CompleteTask : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            uint taskIndex = reader.ReadPackedUInt32();

            if (ShipStatus.Instance == null)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationTaskNoShip(Anticheat.Name(player), taskIndex));
                return;
            }

            // ---- 以下任务作弊检测在任何房间（含 modded）都生效，不做 modded 豁免 ----
            // 极速完成：0.1 秒内完成 3 个以上任务 = Task Finisher 外挂
            // （外挂房间版本号常被标为 modded，此检测必须在 IsModded 短路之前执行）
            if (player != null && Anticheat.RecordTaskBurst(player))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationTaskBurst(Anticheat.Name(player)));
                return;
            }

            // 任务数越界检查（含网络延迟容差2个）
            if (player.Data != null && player.Data.Tasks != null)
            {
                int overLimit = (int)(taskIndex + 1 - (uint)player.Data.Tasks.Count);
                if (overLimit > 2)
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    Anticheat.Flag(player, Strings.ViolationTaskCount(Anticheat.Name(player), taskIndex, player.Data.Tasks.Count));
                }
            }

            // ---- 以下角色类检测在 modded 房间豁免（模组可能合法修改角色/任务）----
            if (Anticheat.IsModded()) return;

            if (player.Data != null && RoleManager.IsImpostorRole(player.Data.RoleType))
            {
                bool hasTasks = player.Data.Tasks != null && player.Data.Tasks.Count > 0;
                if (!hasTasks)
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    Anticheat.Flag(player, Strings.ViolationTaskAsImpostor(Anticheat.Name(player), taskIndex));
                    return;
                }
            }
        }
    }

    internal class EnterVent : RpcCheck
    {
        public override System.Type GetExpectedNetObject() => typeof(PlayerPhysics);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (ShipStatus.Instance == null)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationVentNoShip(Anticheat.Name(player)), false);
                return;
            }

            if (Anticheat.IsModded()) return;

            // ventId 越界（外挂常使用不存在的通风口编号伪造传送）
            try
            {
                byte ventId = reader.ReadByte();
                var vents = ShipStatus.Instance.AllVents;
                if (vents != null && ventId >= vents.Length)
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    Anticheat.Flag(player, Strings.ViolationVentBadId(Anticheat.Name(player), ventId));
                    return;
                }
            }
            catch { return; }

            if (player.Data != null && player.Data.Role != null && !player.Data.IsDead && !player.Data.Role.CanVent)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationVentNoRole(Anticheat.Name(player), Strings.RoleName(player.Data.RoleType)), false);
            }
        }
    }

    internal class ExitVent : RpcCheck
    {
        public override System.Type GetExpectedNetObject() => typeof(PlayerPhysics);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (ShipStatus.Instance == null)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationExitVentNoShip(Anticheat.Name(player)), false);
                return;
            }

            if (Anticheat.IsModded()) return;

            // ventId 越界
            try
            {
                byte ventId = reader.ReadByte();
                var vents = ShipStatus.Instance.AllVents;
                if (vents != null && ventId >= vents.Length)
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    Anticheat.Flag(player, Strings.ViolationVentBadId(Anticheat.Name(player), ventId));
                    return;
                }
            }
            catch { return; }

            if (player.Data != null && player.Data.Role != null && !player.Data.IsDead && !player.Data.Role.CanVent)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationExitVentNoRole(Anticheat.Name(player), Strings.RoleName(player.Data.RoleType)), false);
            }
        }
    }

    /// <summary>
    /// BootFromVent：将玩家强制踢出通风口（房主专属操作）。
    /// 普通玩家发送此 RPC = 伪造传送 / 踢人漏洞（参考 Hydra Teleporter）。
    /// 仅在房主侧生效，避免误伤房主的合法踢出广播。
    /// </summary>
    internal class BootFromVent : RpcCheck
    {
        public override System.Type GetExpectedNetObject() => typeof(PlayerPhysics);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            // 房主侧：非房主玩家发送 BootFromVent = 伪造传送
            bool amHost = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
            if (!amHost) return; // 非房主收到房主广播属合法，放行

            if (Anticheat.IsModded()) return;

            if (player != null && player.Data != null && !player.Data.IsDead)
            {
                blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationVentBoot(Anticheat.Name(player)));
            }
        }
    }

    internal class ClimbLadder : RpcCheck
    {
        public override System.Type GetExpectedNetObject() => typeof(PlayerPhysics);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (ShipStatus.Instance == null)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationClimbLadderNoShip(Anticheat.Name(player)), false);
                return;
            }

            if (!player.Data.IsDead) return;

            // 记录第一次检测到的死亡时间（如果还未记录）
            if (!Anticheat.IsRecentDeath(player))
                Anticheat.RecordDeath(player);

            // 如果死亡时间在宽限期（3 秒）内，直接放行，不触发作弊
            if (Anticheat.IsRecentDeath(player))
                return;

            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            Anticheat.Flag(player, Strings.ViolationClimbLadderDead(Anticheat.Name(player)), false);
        }
    }

    internal class SnapTo : RpcCheck
    {
        public override System.Type GetExpectedNetObject() => typeof(CustomNetworkTransform);

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            // SnapTo 在原版中有大量合法使用场景（传送台、梯子、开局同步等），
            // 不做距离检测（避免网络延迟误判），直接放行。
            NetHelpers.ReadVector2(reader);
        }
    }
}
