using Hazel;

namespace NitroShield.Rpc
{
    internal class SetStartCounter : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            reader.ReadPackedInt32();
            sbyte counter = reader.ReadSByte();

            if (player.OwnerId != AmongUsClient.Instance.HostId)
            {
                // 仅标记篡改行为：counter > 0（倒计时篡改）或 counter < -1（非法值）
                // counter == -1 是合法重置信号，counter == 0 是房主取消计时
                if (counter > 0 || counter < -1)
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    Anticheat.Flag(player, Strings.ViolationStartCounterSpoof(Anticheat.Name(player), counter));
                }
            }
        }
    }
}
