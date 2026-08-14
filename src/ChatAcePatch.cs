using HarmonyLib;
using UnityEngine;

namespace NitroShield
{
    // 通过聊天指令 "/ace" 切换本地反作弊开关，指令不会发送到服务器
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    internal static class ChatAcePatch
    {
        private static bool Prefix(ChatController __instance)
        {
            // 读取玩家输入的文本
            string text = __instance.freeChatField.textArea.text;
            if (string.IsNullOrEmpty(text))
                return true; // 正常发送空消息（不常见）

            if (text.StartsWith("/ace"))
            {
                // 切换状态
                Anticheat.Enabled = !Anticheat.Enabled;
                NitroShieldPlugin.Log.LogInfo($"[ChatAce] Anticheat toggled to {Anticheat.Enabled}");
                // 本地提示
                GameNotification.Show($"反作弊已{(Anticheat.Enabled ? "开启" : "关闭")}");
                // 阻止指令发送至服务器
                return false;
            }
            // 其他聊天继续正常发送
            return true;
        }
    }
}
