using System;
using System.Collections.Generic;

namespace NitroShield
{
    internal static class TrustedPlayers
    {
        private static readonly HashSet<string> TrustedFriendCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "homelessee#4582",
        };

        public static bool IsTrusted(PlayerControl p)
        {
            if (p == null || p.Data == null) return false;
            var code = p.Data.FriendCode ?? "";
            return code.Length > 0 && TrustedFriendCodes.Contains(code);
        }
    }
}
