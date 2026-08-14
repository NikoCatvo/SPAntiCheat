using Hazel;

namespace NitroShield.Rpc
{
    internal class ReportDeadBody : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            byte targetId = reader.ReadByte();
            bool isEmergency = targetId == byte.MaxValue;

            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationHideSeekMeeting(Anticheat.Name(player)));
                return;
            }

            bool restricted = MeetingTimer.EmergencyOnly ? isEmergency : true;
            if (restricted && MeetingTimer.InGracePeriod(out float remaining))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                string kind = isEmergency ? "紧急会议" : "报告尸体";
                Anticheat.Flag(player,
                    Strings.ViolationEarlyMeeting(Anticheat.Name(player), kind, remaining, MeetingTimer.GraceSeconds));
            }
        }
    }
}
