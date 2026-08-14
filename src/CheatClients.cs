using Hazel;
using System.Collections.Generic;

namespace NitroShield
{
    internal static class CheatClients
    {
        public static bool Enabled = true;

        private const byte Sicko           = 164;
        private const byte AUM             = 85;
        private const byte AUMChat         = 101;
        private const byte KillNetwork     = 250;
        private const byte KillNetworkChat = 119;

        private static readonly HashSet<int> _known = new();

        public static bool Check(PlayerControl player, byte callId, MessageReader reader)
        {
            if (!Enabled || player == null) return false;

            string client = null;

            switch (callId)
            {
                case Sicko:
                    if (reader.BytesRemaining == 0) client = "SickoMenu";
                    break;

                case AUM:
                    if (reader.BytesRemaining >= 1)
                    {
                        int savedPos = reader.Position;
                        byte id = reader.ReadByte();
                        reader.Position = savedPos;
                        if (id == player.PlayerId) client = "AmongUsMenu (AUM)";
                    }
                    break;

                case AUMChat:        client = "AmongUsMenu (AUM) 聊天"; break;
                case KillNetwork:    client = "KillNetwork"; break;
                case KillNetworkChat: client = "KillNetwork 聊天"; break;
            }

            if (client == null) return false;

            if (_known.Add(player.OwnerId))
                Anticheat.Flag(player, Strings.ViolationCheatClient(Anticheat.Name(player), client));

            return true;
        }

        public static void ResetKnown() => _known.Clear();
    }
}
