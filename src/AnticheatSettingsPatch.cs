// UI toggle removed – functionality now via /ace command
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace NitroShield
{
    // 在游戏设置菜单中添加一个切换反作弊的 UI 开关
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    internal static class AnticheatSettingsPatch
    {
        private static void Postfix(GameSettingMenu __instance)
        {
            // 创建 Toggle 对象并放在菜单中心稍下位置
            var toggleObj = new GameObject("AnticheatToggle");
            toggleObj.transform.SetParent(__instance.transform, false);

            var rect = toggleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -150); // 调整位置以避免遮挡其他控件

            var toggle = toggleObj.AddComponent<Toggle>();

            // 背景图层
            var bgObj = new GameObject("Background");
            var bgImg = bgObj.AddComponent<Image>();
            bgObj.transform.SetParent(toggleObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(20, 20);
            toggle.targetGraphic = bgImg;

            // 勾选图层
            var checkObj = new GameObject("Checkmark");
            var checkImg = checkObj.AddComponent<Image>();
            checkObj.transform.SetParent(bgObj.transform, false);
            toggle.graphic = checkImg;

            // 文本标签
            var labelObj = new GameObject("Label");
            var txt = labelObj.AddComponent<Text>();
            txt.text = "反作弊";
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            labelObj.transform.SetParent(toggleObj.transform, false);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(30, 0);

            // 初始化状态并监听变化
            toggle.isOn = Anticheat.Enabled;
            toggle.onValueChanged.AddListener((UnityAction<bool>)((bool on) => Anticheat.Enabled = on));
        }
    }
}
