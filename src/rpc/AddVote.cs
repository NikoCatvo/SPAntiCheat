using HarmonyLib;

namespace NitroShield.Rpc
{
    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    internal static class VoteKickGuard
    {
        public static bool Enabled = true;

        private static bool Prefix(int srcClient, int clientId)
        {
            if (!Anticheat.Enabled || !Enabled) return true;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return true;

            var client = AmongUsClient.Instance.FindClientById(srcClient);
            if (client == null)
            {
                Anticheat.Flag(Strings.ViolationUnknownClientVote(srcClient.ToString()));
                return false;
            }

            // 断线玩家的待处理投票可能延迟到达，不阻止，仅日志
            if (client.Character == null)
            {
                NitroShieldPlugin.Log.LogWarning($"[Shield] 收到断线玩家({srcClient})的投票，放行");
                return true;
            }

            var voter = client.Character;
            if (Anticheat.IsExempt(voter)) return true;

            if (voter.Data != null && voter.Data.IsDead)
            {
                Anticheat.Flag(voter, Strings.ViolationDeadVote(Anticheat.Name(voter)));
                return false;
            }

            if (MeetingHud.Instance == null)
            {
                Anticheat.Flag(voter, Strings.ViolationOutsideMeetingVote(Anticheat.Name(voter)));
                return false;
            }

            return true;
        }
    }
}
