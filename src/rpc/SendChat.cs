using Hazel;
using UnityEngine;

namespace NitroShield.Rpc
{
    internal class SendChat : RpcCheck
    {
        public static int SpamThreshold = 10;
        public static float SpamWindow = 5.0f;
        public static int MaxMessageLength = 300;

        private static readonly RateTracker _chatRate = new();

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (player == null) return;

            string message = reader.ReadString();

            if (MuteManager.IsMuted(player))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }

            MuteManager.RecordChatColorVote(player, message);

            // 检测消息长度
            if (message != null && message.Length > MaxMessageLength)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationChatOversized(Anticheat.Name(player), message.Length));
                return;
            }

            int count = _chatRate.Record(player.OwnerId, Time.realtimeSinceStartup, SpamWindow);
            if (count > SpamThreshold)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationChatSpam(Anticheat.Name(player), count, SpamWindow));
            }


        }
    }
}
