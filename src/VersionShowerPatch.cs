using HarmonyLib;
using System;
using UnityEngine;

namespace NitroShield
{
    /// <summary>
    /// 主菜单显示模组名称与版本号。
    /// 实现参考 BanMod 的 VersionShower_Start：克隆原版版本号文本对象，
    /// 固定显示在主菜单左下角（世界坐标 1f, 2.67f, -2f），
    /// 字号明确设为 2f（与 BanMod 一致），避免继承过小字号导致看不清。
    /// 不影响原版版本号（克隆副本，不改动原对象）。
    /// </summary>
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    internal static class VersionShowerPatch
    {
        public static string ModVersion { get; private set; } = "1.1.4";

        private static void Postfix(VersionShower __instance)
        {
            try
            {
                if (__instance == null || __instance.text == null) return;
                ModVersion = ReadAssemblyVersion();

                // 克隆原版文本对象（保持字体/材质一致），避免破坏官方版本号
                var cred = UnityEngine.Object.Instantiate(__instance.text);

                string text =
                    "<b><size=120%><color=#FF6B6B>SP AntiCheat</color></b>" +
                    "<color=#E0FFFF><size=50%>   v" + ModVersion + "</color>";

                cred.text = text;
                cred.alignment = TMPro.TextAlignmentOptions.Left;

                // 固定在主菜单左下角（与 BanMod 使用相同坐标）
                cred.transform.position = new Vector3(1f, 2.67f, -2f);
                cred.fontSize = 2f;
                cred.fontSizeMax = 2f;
                cred.fontSizeMin = 2f;
            }
            catch (Exception e)
            {
                NitroShieldPlugin.Log.LogWarning($"[VersionShower] {e.Message}");
            }
        }

        internal static string ReadAssemblyVersion()
        {
            try
            {
                var asm = typeof(VersionShowerPatch).Assembly;
                var v = asm.GetName().Version;
                if (v != null && v.Major >= 1)
                    return v.ToString(3);
            }
            catch { }
            return "1.1.4";
        }

        /// <summary>暴露给 Ui/其他地方使用的版本号。</summary>
        public static string GetVersion() => ModVersion;
    }
}