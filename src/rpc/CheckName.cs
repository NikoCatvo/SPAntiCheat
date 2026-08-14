using Hazel;

namespace NitroShield.Rpc
{
    internal class CheckName : RpcCheck
    {
        public const int MaxNameLength = 10;

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            string requested = reader.ReadString();

            if (NameFilter.IsOffensive(requested, out var term))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationBlockedTerm(requested, term ?? ""));
                return;
            }

            if (requested.Length > MaxNameLength)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationNameTooLong(requested, requested.Length));
                return;
            }

            if (requested.Contains('<'))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationNameInvalidChars(requested));
            }
        }
    }
}
