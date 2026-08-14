using HarmonyLib;
using Hazel;
using InnerNet;

namespace NitroShield
{
    internal static class Protections
    {
        public static bool BlockVentKickExploit = true;
        public static bool BlockVotingOverload  = true;
        public static bool BlockLargeMessages   = true;
        public static int  MaxMessageLength     = 1400;

        // 通风口操作类型常量（VentilationSystem.Operation 枚举，2026 版本声明顺序）
        private const byte OpEnter          = 2;
        private const byte OpLeaveVent      = 3;
        private const byte OpBootImpostors  = 5;

        private static bool IsHost() => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

        public static bool ShouldBlock(System.Type netObj, PlayerControl player, byte callId, MessageReader reader)
        {
            var rpc = (RpcCalls)callId;

            // 通风管道传送 + 通风口踢人漏洞防御（Hydra Vent TP / Kick Exploit）
            //
            // Hydra 的攻击方式（参考 Teleporter.TeleportToVent 源码）：
            //   1. 非房主伪造 ShipStatus.UpdateSystem(Ventilation) 报文，payload =
            //        [netobject player][seqId ushort][op byte][ventId byte]
            //      其中 op = Enter(2) / BootImpostors(5)，netobject 写的是【受害者】
            //   2. Enter 使房主认为受害者进入了通风口；BootImpostors 使房主把受害者
            //      "踢出"到目标通风口 → 无视距离的通风管道传送
            //   3. 用超大 seqId（从 10000 起步）压制序列号防重放
            //
            // 归属说明：外挂可以"传送别人"，报文 netobject 是被强传的受害者，攻击者
            // 本人从不出现在报文中；Hazel 协议层在服务器中继模式下无法获取真实发送者，
            // 因此 VentilationGuard 对被强传的受害者【绝不惩罚】，只拦截 + 匿名警告。
            //
            // 注意：非房主场景下 ShouldBlock 的 player 参数恒为 null
            //（OnShipStatusRpc 未传玩家），因此旧实现是死代码 ——
            // 由 VentilationGuard 在报文内读取 netobject player 定位被操作者。
            if (VentilationGuard.Enabled && netObj == typeof(ShipStatus)
                && rpc == RpcCalls.UpdateSystem)
            {
                int pos = reader.Position;
                try
                {
                    if (AmongUsClient.Instance == null) { reader.Position = pos; return false; }

                    // 完整解析并检测（从 system byte 开始），由 VentilationGuard 自行判断是否放行
                    if (VentilationGuard.CheckVentilationUpdate(netObj, callId, reader))
                    {
                        reader.Position = pos;
                        return true;
                    }
                }
                catch (System.Exception e)
                {
                    NitroShieldPlugin.Log.LogWarning($"[VentKick] check error: {e.Message}");
                }
                finally { reader.Position = pos; }
            }

            return false;
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleRpc))]
        private static class VotingOverloadGuard
        {
            private static bool Prefix(byte callId, MessageReader reader)
            {
                if (!BlockVotingOverload || callId != (byte)RpcCalls.VotingComplete) return true;

                int pos = reader.Position;
                try
                {
                    int arrayLength = reader.ReadPackedInt32();
                    if (arrayLength > 1024 || arrayLength > reader.BytesRemaining)
                    {
                        GameNotification.Show(Strings.NotifyBlockedVotingOverload);
                        return false;
                    }
                    reader.Position = pos;
                }
                catch { reader.Position = pos; }
                return true;
            }
        }

        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameData))]
        private static class LargeMessageGuard
        {
            private static bool Prefix(MessageReader parentReader)
            {
                if (!BlockLargeMessages) return true;
                if (parentReader != null && parentReader.Length > MaxMessageLength)
                {
                    parentReader.Recycle();
                    return false;
                }
                return true;
            }
        }
    }
}
