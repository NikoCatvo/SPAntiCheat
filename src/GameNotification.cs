using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace NitroShield
{
    /// <summary>
    /// 反作弊通知叠加层。
    ///
    /// 使用 OnGUI 在屏幕空间直接绘制，不依赖任何场景 UI：
    /// 因此无论是在加载界面、进入游戏动画、会议过渡动画，
    /// 还是在任何特殊渲染阶段，提示都始终可见（永远在最上层）。
    ///
    /// 样式参考 Hydra 的 NotificationManager / FinalSuspect 通知弹窗：
    /// 左下角纵向堆叠，标题红色，正文保留原色，自动淡出。
    /// </summary>
    public class NotificationOverlay : MonoBehaviour
    {
        public static NotificationOverlay Instance;

        private class Entry
        {
            public string Text;
            public float Life;
            public float Ttl;
            public Color Tint;
            public bool Expired => Life >= Ttl;
        }

        private readonly List<Entry> _entries = new();
        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _bgFillStyle;
        private GUIStyle _bgStrokeStyle;

        private const float BoxWidth = 360f;
        private const float Margin = 12f;
        private const float Spacing = 6f;
        private const int MaxEntries = 6;

        public NotificationOverlay(System.IntPtr ptr) : base(ptr) { }

        public void Awake()
        {
            if (Instance == null) Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>弹出一条提示。text 支持 &lt;color&gt; 富文本标签。</summary>
        public void Show(string text, Color tint, float ttl = 8f)
        {
            if (string.IsNullOrEmpty(text)) return;
            _entries.Add(new Entry { Text = text, Ttl = ttl, Tint = tint });
            while (_entries.Count > MaxEntries) _entries.RemoveAt(0);
        }

        private void Update()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                _entries[i].Life += Time.deltaTime;
                if (_entries[i].Expired) _entries.RemoveAt(i);
            }
        }

        private void OnGUI()
        {
            if (_entries.Count == 0) return;
            EnsureStyles();

            // 通知面板显示在屏幕【左下角】，从下往上堆叠。
            // 注意：IMGUI 的 y 坐标原点在屏幕顶部，左下角需要用 Screen.height 从底部计算。
            float panelH = 52f;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                // 剩余寿命：条目在 Life >= Ttl 时被移除，两个 alpha 都在此时归零 → 同步消失
                float ttlLeft = e.Ttl - e.Life;
                // 背景透明度上限 0.4（防止遮挡视野），最后 1.5 秒淡出
                float bgAlpha = Mathf.Clamp01(ttlLeft / 1.5f) * 0.4f;
                if (bgAlpha <= 0f) continue;
                // 文本：平时完全不透明（可读），仅在最后 1.2 秒跟随背景一起淡出，避免"背景没了字还在"的错位感
                float textAlpha = Mathf.Clamp01(ttlLeft / 1.2f);

                float y = Screen.height - Margin - panelH * (i + 1) - Spacing * i;
                Rect rect = new Rect(Margin, y, BoxWidth, panelH);

                // 仅背景跟随 alpha（GUI.color 只作用于背景 Box）
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, bgAlpha);

                // 圆角卡片：先画红色描边层（比卡片大 4px），再画填充层，形成圆角描边边框
                GUI.Box(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f),
                    GUIContent.none, _bgStrokeStyle);
                GUI.Box(rect, GUIContent.none, _bgFillStyle);

                // 文本：平时完全不透明，最后阶段与背景同步淡出
                GUI.color = new Color(1f, 1f, 1f, textAlpha);
                _titleStyle.normal.textColor = new Color(1f, 0.30f, 0.30f, 1f);
                _titleStyle.richText = true;
                GUI.Label(new Rect(rect.x + 10, rect.y + 4, rect.width - 20, 20), "反作弊", _titleStyle);

                _textStyle.normal.textColor = new Color(1f, 1f, 1f, 1f);
                _textStyle.richText = true;
                GUI.Label(new Rect(rect.x + 10, rect.y + 26, rect.width - 20, panelH - 30), e.Text, _textStyle);

                GUI.color = prev;
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            _titleStyle.richText = true;
            _textStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            _textStyle.richText = true;

            // 圆角卡片：48x48 纹理、圆角半径 12px，9-slice 拉伸后任意尺寸圆角不变形
            const int size = 48, radius = 12;
            _bgStrokeStyle = BuildRoundedStyle(new Color(0.9f, 0.15f, 0.15f, 1f), size, radius);
            _bgFillStyle = BuildRoundedStyle(new Color(0.08f, 0.08f, 0.1f, 1f), size, radius);
        }

        private static GUIStyle BuildRoundedStyle(Color color, int size, int radius)
        {
            var tex = CreateRoundedRectTexture(size, radius, color);
            var style = new GUIStyle(GUI.skin.box)
            {
                normal = { background = tex }
            };
            style.border.left = radius;
            style.border.right = radius;
            style.border.top = radius;
            style.border.bottom = radius;
            return style;
        }

        /// <summary>生成圆角矩形纹理（4x4 超采样抗锯齿，边缘平滑过渡）。</summary>
        private static Texture2D CreateRoundedRectTexture(int size, int radius, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            const int ss = 4; // 每像素 4x4 子采样
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float px = x + (sx + 0.5f) / ss;
                            float py = y + (sy + 0.5f) / ss;
                            if (IsInsideRoundedRect(px, py, size, radius)) hits++;
                        }
                    }
                    float a = hits / (float)(ss * ss);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>点 (px,py) 是否位于圆角矩形内部：中部直接通过，四角按四分之一圆判断。</summary>
        private static bool IsInsideRoundedRect(float px, float py, int size, float r)
        {
            if (px >= r && px <= size - r) return true;
            if (py >= r && py <= size - r) return true;
            float cx = px < r ? r : size - r;
            float cy = py < r ? r : size - r;
            float dx = px - cx;
            float dy = py - cy;
            return dx * dx + dy * dy <= r * r;
        }
    }

    internal static class GameNotification
    {
        private static NotificationPopper _popper;

        [HarmonyPatch(typeof(NotificationPopper), nameof(NotificationPopper.Awake))]
        private static class OnAwake
        {
            private static void Prefix(NotificationPopper __instance)
            {
                _popper = __instance;
            }
        }

        /// <summary>显示提示（叠加层始终可见；若叠加层未就绪，回退到原版弹窗）。</summary>
        public static void Show(string text)
        {
            if (!Anticheat.SendNotification) return;

            if (NotificationOverlay.Instance != null)
            {
                NotificationOverlay.Instance.Show(text, Color.white);
                return;
            }

            // 回退：原版 NotificationPopper（某些特殊场景叠加层组件可能未加载）
            if (_popper == null) return;
            LobbyNotificationMessage newMessage = Object.Instantiate(
                _popper.notificationMessageOrigin,
                Vector3.zero,
                Quaternion.identity,
                _popper.transform);
            newMessage.transform.localPosition = new Vector3(0f, 0f, -2f);
            text = "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + text + "</font>";
            newMessage.SetUp(text, _popper.settingsChangeSprite, _popper.settingsChangeColor,
                (System.Action)(() => _popper.OnMessageDestroy(newMessage)));
            _popper.ShiftMessages();
            _popper.AddMessageToQueue(newMessage);
            SoundManager.Instance.PlaySoundImmediate(_popper.settingsChangeSound, false);
        }
    }
}