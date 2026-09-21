using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>Shared look for runtime-built UI: warm palette, rounded 9-sliced sprites, soft shadows.</summary>
    public static class UIStyle
    {
        public static readonly Color Backdrop = new Color(0.10f, 0.06f, 0.04f, 0.55f);
        public static readonly Color Panel = Hex("#F7EEDC");
        public static readonly Color Header = Hex("#6F452A");
        public static readonly Color HeaderText = Hex("#FFF3DC");
        public static readonly Color HeaderPill = Hex("#573620");
        public static readonly Color HeaderButton = Hex("#8E5C3A");
        public static readonly Color Tray = Hex("#E8D6B6");
        public static readonly Color SlotEmpty = Hex("#DCC7A2");
        public static readonly Color SlotFilled = Hex("#FFF8EA");
        public static readonly Color Accent = Hex("#F0A93B");
        public static readonly Color DetailBox = Hex("#FFFBF1");
        public static readonly Color TextDark = Hex("#5A3A24");
        public static readonly Color TextSoft = Hex("#8A7154");
        public static readonly Color TextHint = Hex("#B39B78");
        public static readonly Color ShadowTint = new Color(0.16f, 0.08f, 0.02f, 1f);

        private const int ShadowSize = 128;
        private const int ShadowBorder = 56;

        private static readonly Dictionary<int, Sprite> RoundedCache = new Dictionary<int, Sprite>();
        private static Sprite _shadow;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        public static Font ResolveFont(Font custom)
        {
            if (custom != null) return custom;

            Font osFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Trebuchet MS", "Verdana", "Arial" }, 32);
            return osFont != null ? osFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Sprite Rounded(int radius)
        {
            if (RoundedCache.TryGetValue(radius, out Sprite cached) && cached != null) return cached;

            int size = radius * 2 + 2;
            var pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = RoundedRectDistance(x + 0.5f - half, y + 0.5f - half, half - radius, half - radius, radius);
                    byte alpha = (byte)(Mathf.Clamp01(0.5f - dist) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            Sprite sprite = MakeSprite(pixels, size, radius);
            RoundedCache[radius] = sprite;
            return sprite;
        }

        public static Sprite Shadow()
        {
            if (_shadow != null) return _shadow;

            var pixels = new Color32[ShadowSize * ShadowSize];
            float half = ShadowSize * 0.5f;
            float inner = half - 36f;

            for (int y = 0; y < ShadowSize; y++)
            {
                for (int x = 0; x < ShadowSize; x++)
                {
                    float dist = RoundedRectDistance(x + 0.5f - half, y + 0.5f - half, inner - 20f, inner - 20f, 20f);
                    float t = Mathf.Clamp01(1f - dist / 36f);
                    float alpha = t * t * (3f - 2f * t);
                    pixels[y * ShadowSize + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            _shadow = MakeSprite(pixels, ShadowSize, ShadowBorder);
            return _shadow;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(Transform parent, string name, Font font, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Adds a soft drop shadow as a sibling drawn just before <paramref name="target"/>.</summary>
        public static Image AddShadowBehind(RectTransform target, float expand, Vector2 offset, float alpha)
        {
            Image shadow = CreateImage(target.parent, "Shadow", Shadow(), new Color(ShadowTint.r, ShadowTint.g, ShadowTint.b, alpha));
            shadow.pixelsPerUnitMultiplier = ShadowBorder / Mathf.Max(1f, expand + 20f);

            var rect = (RectTransform)shadow.transform;
            rect.anchorMin = target.anchorMin;
            rect.anchorMax = target.anchorMax;
            rect.pivot = target.pivot;
            rect.offsetMin = target.offsetMin - new Vector2(expand, expand) + offset;
            rect.offsetMax = target.offsetMax + new Vector2(expand, expand) + offset;
            rect.SetSiblingIndex(target.GetSiblingIndex());
            return shadow;
        }

        private static float RoundedRectDistance(float px, float py, float halfW, float halfH, float radius)
        {
            float dx = Mathf.Abs(px) - halfW;
            float dy = Mathf.Abs(py) - halfH;
            float ox = Mathf.Max(dx, 0f);
            float oy = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
        }

        private static Sprite MakeSprite(Color32[] pixels, int size, int border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
