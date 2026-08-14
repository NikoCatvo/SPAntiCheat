using Hazel;

namespace NitroShield.Rpc
{
    internal class SetName : RpcCheck
    {
        public const int MaxNameLength = 12;

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            reader.ReadUInt32();
            string requested = reader.ReadString();

            if (NameFilter.IsOffensive(requested, out var term))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationBlockedTerm(requested, term ?? ""));
                return;
            }

            if (Anticheat.IsModded()) return;

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

        public override bool IsHostOnly() => Anticheat.IsModded();
    }
}
