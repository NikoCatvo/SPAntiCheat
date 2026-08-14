using System.Collections.Generic;

namespace NitroShield
{
    internal static class StateChecks
    {
        public static bool Enabled = true;
        public static bool CheckCosmetics  = true;
        public static bool CheckLobbyRpcs  = true;

        private static readonly HashSet<byte> Cosmetic = Ids(
            RpcCalls.SetColor, RpcCalls.SetHatStr, RpcCalls.SetSkinStr,
            RpcCalls.SetVisorStr, RpcCalls.SetPetStr, RpcCalls.SetNamePlateStr);

        private static readonly HashSet<byte> LobbyIllegal = Ids(
            RpcCalls.MurderPlayer, RpcCalls.CheckMurder,
            RpcCalls.EnterVent, RpcCalls.ExitVent, RpcCalls.BootFromVent,
            RpcCalls.ClimbLadder, RpcCalls.UsePlatform, RpcCalls.UseZipline, RpcCalls.CheckZipline,
            RpcCalls.CompleteTask,
            RpcCalls.Shapeshift, RpcCalls.CheckShapeshift, RpcCalls.RejectShapeshift,
            RpcCalls.ProtectPlayer, RpcCalls.CheckProtect,
            RpcCalls.StartVanish, RpcCalls.CheckVanish, RpcCalls.StartAppear, RpcCalls.CheckAppear,
            RpcCalls.TriggerSpores, RpcCalls.CheckSpore);

        public static void Check(PlayerControl player, byte callId, ref bool blockRpc)
        {
            if (!Enabled || player == null || blockRpc) return;

            bool inLobby = LobbyBehaviour.Instance != null;
            bool inGameplay = ShipStatus.Instance != null && LobbyBehaviour.Instance == null;

            if (CheckCosmetics && inGameplay && Cosmetic.Contains(callId))
            {
                // 原版客户端在游戏期间会合法重发化妆品数据用于新人同步
                // 无法可靠区分"修改"与"同步"，仅做软提示不阻止
                Anticheat.Flag(player, Strings.ViolationCosmetics(Anticheat.Name(player), Strings.RpcName((RpcCalls)callId)), false);
            }

            if (CheckLobbyRpcs && inLobby && LobbyIllegal.Contains(callId))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationLobbyRpc(Anticheat.Name(player), Strings.RpcName((RpcCalls)callId)));
            }
        }

        private static HashSet<byte> Ids(params RpcCalls[] calls)
        {
            var set = new HashSet<byte>();
            foreach (var c in calls) set.Add((byte)c);
            return set;
        }
    }
}
