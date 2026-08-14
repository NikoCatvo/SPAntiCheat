using HarmonyLib;
using System;

namespace NitroShield
{
    internal static class MeetingTimer
    {
        public static bool Enabled = true;
        public static float GraceSeconds = 10f;
        public static bool EmergencyOnly = false;

        private static DateTime _roundStartUtc = DateTime.MinValue;
    // 公开获取轮次开始信息，用于早期会议检测
    public static bool IsRoundStarted => _roundStartUtc != DateTime.MinValue;
    public static float SecondsSinceRoundStart()
    {
        if (_roundStartUtc == DateTime.MinValue) return -1f;
        return (float)(DateTime.UtcNow - _roundStartUtc).TotalSeconds;
    }
        private static bool _latched;

        public static void MarkRoundStart()
        {
            _roundStartUtc = DateTime.UtcNow;
            NitroShield.Anticheat.ResetTaskTracking();
            NitroShield.VentilationGuard.ResetVentOpHistory();
        }

        public static bool InGracePeriod(out float remaining)
        {
            remaining = 0f;
            if (!Enabled || _roundStartUtc == DateTime.MinValue) return false;

            float elapsed = (float)(DateTime.UtcNow - _roundStartUtc).TotalSeconds;
            if (elapsed < GraceSeconds)
            {
                remaining = GraceSeconds - elapsed;
                return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
        private static class OnShipFixedUpdate
        {
            private static void Postfix()
            {
                if (LobbyBehaviour.Instance != null)
                {
                    _latched = false;
                    return;
                }
                if (!_latched)
                {
                    MarkRoundStart();
                    _latched = true;
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        private static class OnMeetingClose
        {
            private static void Postfix()
            {
                MarkRoundStart();
                NitroShield.Anticheat.EndMeetingBlock();
                // 会议结束后重置 EmergencyOnly，确保下轮报告尸体不受影响
                EmergencyOnly = false;
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
        private static class OnGameStart
        {
            private static void Prefix() => MarkRoundStart();
        }
    }
}
