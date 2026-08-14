using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using UnityEngine;

namespace NitroShield.Rpc
{
    internal static class RoleUtil
    {
        public static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead;
        public static bool Impostor(PlayerControl p) => p?.Data != null && RoleManager.IsImpostorRole(p.Data.RoleType);
        public static bool IsRole(PlayerControl p, RoleTypes r) => p?.Data != null && p.Data.RoleType == r;
        public static bool InVent(PlayerControl p) => p != null && (p.inVent || p.walkingToVent);
        public static bool InRange(PlayerControl a, PlayerControl b, float range)
            => a != null && b != null && Vector2.Distance(a.GetTruePosition(), b.GetTruePosition()) <= range;
    }

    internal class CheckMurder : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            PlayerControl target = reader.ReadNetObject<PlayerControl>();
            if (target == null) return;

            // 注意：不做距离检测。躲猫猫模式或高延迟玩家击杀距离会误判，
            // 仅保留角色/状态/目标合法性等不依赖坐标的语义校验。
            bool killerOk = player != target
                            && RoleUtil.Alive(player) && RoleUtil.Impostor(player) && !RoleUtil.InVent(player);
            bool targetOk = target != player && RoleUtil.Alive(target) && !RoleUtil.Impostor(target);

            if (!killerOk || !targetOk)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationInvalidKill(Anticheat.Name(player), Anticheat.Name(target)));
            }
        }
    }

    internal class MurderPlayer : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            PlayerControl target = reader.ReadNetObject<PlayerControl>();
            if (target == null) return;

            bool killerOk = player != target
                            && RoleUtil.Alive(player) && RoleUtil.Impostor(player) && !RoleUtil.InVent(player);
            bool targetOk = target != player && RoleUtil.Alive(target) && !RoleUtil.Impostor(target);

            if (!killerOk || !targetOk)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationIllegalMurder(Anticheat.Name(player)));
            }
        }
    }

    internal class Shapeshift : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            if (!RoleUtil.IsRole(player, RoleTypes.Shapeshifter) || !RoleUtil.Alive(player))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationShapeshift(Anticheat.Name(player)));
            }
        }
    }

    internal class StartVanish : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            if (!RoleUtil.IsRole(player, RoleTypes.Phantom) || !RoleUtil.Alive(player))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationVanish(Anticheat.Name(player)));
            }
        }
    }

    internal class ProtectPlayer : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (Anticheat.IsModded()) return;
            // 守护天使必须已死亡才能使用保护技能
            bool isDeadGA = RoleUtil.IsRole(player, RoleTypes.GuardianAngel) && player.Data != null && player.Data.IsDead;
            if (!isDeadGA)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationProtect(Anticheat.Name(player)));
            }
        }
    }
}
