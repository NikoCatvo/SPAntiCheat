using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NitroShield
{
    internal static class SpriteLoader
    {
        private static Assembly _thisAssembly = typeof(SpriteLoader).Assembly;
        private const int MaxSize = 95;
        public const float Alpha = 0.5f;

        public static Sprite LoadSprite(float pixelsPerUnit = 100f)
        {
            string resName = null;
            foreach (var name in _thisAssembly.GetManifestResourceNames())
            {
                if (name.Contains("ModStamp", StringComparison.OrdinalIgnoreCase))
                {
                    resName = name;
                    break;
                }
            }
            if (resName == null)
            {
                NitroShieldPlugin.Log.LogWarning("[ModStamp] No embedded ModStamp resource");
                return null;
            }

            var stream = _thisAssembly.GetManifestResourceStream(resName);
            if (stream == null)
            {
                NitroShieldPlugin.Log.LogWarning("[ModStamp] Stream null");
                return null;
            }

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();

            var tex = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(tex, bytes, false))
            {
                NitroShieldPlugin.Log.LogWarning("[ModStamp] LoadImage failed");
                UnityEngine.Object.Destroy(tex);
                return null;
            }

            if (tex.width > MaxSize || tex.height > MaxSize)
            {
                var ratio = (float)MaxSize / Mathf.Max(tex.width, tex.height);
                var newW = (int)(tex.width * ratio);
                var newH = (int)(tex.height * ratio);

                tex.filterMode = FilterMode.Bilinear;
                tex.Apply();

                var rtx = new RenderTexture(newW, newH, 0);
                rtx.filterMode = FilterMode.Bilinear;
                Graphics.Blit(tex, rtx);

                var small = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
                small.filterMode = FilterMode.Bilinear;
                RenderTexture.active = rtx;
                small.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
                small.Apply();
                RenderTexture.active = null;

                UnityEngine.Object.Destroy(tex);
                UnityEngine.Object.Destroy(rtx);
                tex = small;
            }

            var colors = tex.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                var origA = colors[i].a;
                colors[i] = new Color(colors[i].r, colors[i].g, colors[i].b, origA * Alpha);
            }
            tex.SetPixels(colors);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2, pixelsPerUnit);
            NitroShieldPlugin.Log.LogMessage($"[ModStamp] Loaded {tex.width}x{tex.height}");
            return sprite;
        }
    }

    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    internal static class ModStampPatch
    {
        private static Sprite _customSprite;

        public static void Prefix(ModManager __instance)
        {
            // Always force mod stamp to show, every frame
            try { __instance.ShowModStamp(); } catch { }

            if (_customSprite == null)
            {
                _customSprite = SpriteLoader.LoadSprite(100f);
            }

            if (_customSprite == null) return;

            try
            {
                var stamp = __instance.ModStamp;
                if (stamp != null)
                {
                    stamp.sprite = _customSprite;
                    stamp.color = Color.white;
                    stamp.enabled = true;
                    stamp.gameObject.SetActive(true);
                    // Ensure parent is also active
                    var p = stamp.transform.parent;
                    if (p != null) p.gameObject.SetActive(true);
                }
            }
            catch (Exception e)
            {
                NitroShieldPlugin.Log.LogWarning($"[ModStamp] Update failed: {e.Message}");
            }
        }
    }
}