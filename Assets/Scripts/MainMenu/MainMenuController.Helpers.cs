using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WarOfEras.Battle.Core;

namespace WarOfEras.MainMenu
{
    public sealed partial class MainMenuController
    {
        private static Font CreateUiFont()
        {
            var font = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "STXinwei",
                    "\u534e\u6587\u65b0\u9b4f",
                    "STHupo",
                    "\u534e\u6587\u7425\u73c0",
                    "STXingkai",
                    "\u534e\u6587\u884c\u6977",
                    "FZShuTi",
                    "\u65b9\u6b63\u8212\u4f53",
                    "SimHei",
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "Arial"
                },
                16);
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null)
                {
                    return whiteSprite;
                }

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                return whiteSprite;
            }
        }

        private static Sprite DotSprite
        {
            get
            {
                if (dotSprite == null)
                {
                    dotSprite = CreateDotSprite();
                }

                return dotSprite;
            }
        }

        private static Sprite MenuButtonSprite
        {
            get
            {
                if (menuButtonSprite == null)
                {
                    menuButtonSprite = CreateBeveledSprite(
                        new Color(0.055f, 0.11f, 0.18f, 1f),
                        new Color(0.025f, 0.045f, 0.075f, 1f),
                        new Color(0.012f, 0.02f, 0.04f, 1f),
                        new Color(0.16f, 0.76f, 1f, 1f),
                        new Color(0.56f, 0.92f, 1f, 1f));
                }

                return menuButtonSprite;
            }
        }

        private static Sprite PrimaryButtonSprite
        {
            get
            {
                if (primaryButtonSprite == null)
                {
                    primaryButtonSprite = CreateBeveledSprite(
                        new Color(0.45f, 0.88f, 1f, 1f),
                        new Color(0.42f, 0.43f, 0.94f, 1f),
                        new Color(0.32f, 0.18f, 0.72f, 1f),
                        new Color(0.74f, 0.96f, 1f, 1f),
                        new Color(1f, 1f, 1f, 1f));
                }

                return primaryButtonSprite;
            }
        }

        private static Sprite PanelSprite
        {
            get
            {
                if (panelSprite == null)
                {
                    panelSprite = CreateBeveledSprite(
                        new Color(0.055f, 0.09f, 0.14f, 1f),
                        new Color(0.022f, 0.038f, 0.068f, 1f),
                        new Color(0.008f, 0.014f, 0.032f, 1f),
                        new Color(0.18f, 0.46f, 0.82f, 1f),
                        new Color(0.46f, 0.86f, 1f, 1f));
                }

                return panelSprite;
            }
        }

        private static Sprite TopFadeSprite
        {
            get
            {
                if (topFadeSprite == null)
                {
                    topFadeSprite = CreateVerticalGradientSprite(new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.44f));
                }

                return topFadeSprite;
            }
        }

        private static Sprite BottomFadeSprite
        {
            get
            {
                if (bottomFadeSprite == null)
                {
                    bottomFadeSprite = CreateVerticalGradientSprite(new Color(0f, 0f, 0f, 0.54f), new Color(0f, 0f, 0f, 0f));
                }

                return bottomFadeSprite;
            }
        }

        private static Sprite CreateBeveledSprite(Color top, Color center, Color bottom, Color border, Color highlight)
        {
            // 主菜单按钮和面板是运行时程序化贴图，减少对额外 UI 贴图资源的依赖。
            const int width = 96;
            const int height = 40;
            const int borderSize = 5;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (var y = 0; y < height; y++)
            {
                var v = y / (float)(height - 1);
                var baseColor = v > 0.5f
                    ? Color.Lerp(center, top, (v - 0.5f) * 2f)
                    : Color.Lerp(bottom, center, v * 2f);

                for (var x = 0; x < width; x++)
                {
                    var edge = x < borderSize || x >= width - borderSize || y < borderSize || y >= height - borderSize;
                    var corner = (x < borderSize * 2 || x >= width - borderSize * 2)
                        && (y < borderSize * 2 || y >= height - borderSize * 2);
                    var color = edge ? Color.Lerp(baseColor, border, corner ? 0.9f : 0.58f) : baseColor;

                    if (y > height - borderSize * 2 && !edge)
                    {
                        color = Color.Lerp(color, highlight, 0.18f);
                    }

                    if (y == height - borderSize - 1 || y == borderSize)
                    {
                        color = Color.Lerp(color, highlight, 0.35f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(18f, 14f, 18f, 14f));
        }

        private static Sprite CreateVerticalGradientSprite(Color bottom, Color top)
        {
            const int width = 4;
            const int height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (var y = 0; y < height; y++)
            {
                var color = Color.Lerp(bottom, top, y / (float)(height - 1));
                for (var x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateDotSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var center = (size - 1) * 0.5f;
            var radius = size * 0.38f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = Mathf.Clamp01(radius + 1.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private sealed class MenuHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
        {
            private const float AnimationSpeed = 14f;

            private Button button;
            private Image targetImage;
            private Color normalColor;
            private Color hoverColor;
            private Vector3 normalScale = Vector3.one;
            private Vector3 hoverScale = Vector3.one;
            private bool pointerInside;
            private bool selected;
            private bool initialized;

            public void Initialize(Button sourceButton, Image image, Color normal, Color hover, float scale)
            {
                button = sourceButton;
                targetImage = image;
                normalColor = normal;
                hoverColor = hover;
                normalScale = transform.localScale;
                hoverScale = normalScale * scale;
                initialized = true;

                if (targetImage != null)
                {
                    targetImage.color = normalColor;
                }
            }

            public void SetColors(Color normal, Color hover)
            {
                normalColor = normal;
                hoverColor = hover;

                if (!IsHighlighted() && targetImage != null)
                {
                    targetImage.color = normalColor;
                }
            }

            private void Update()
            {
                if (!initialized)
                {
                    return;
                }

                var highlighted = IsHighlighted();
                var targetScale = highlighted ? hoverScale : normalScale;
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * AnimationSpeed);

                if (targetImage != null)
                {
                    var targetColor = highlighted ? hoverColor : normalColor;
                    targetImage.color = Color.Lerp(targetImage.color, targetColor, Time.unscaledDeltaTime * AnimationSpeed);
                }
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                pointerInside = true;
                transform.SetAsLastSibling();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                pointerInside = false;
            }

            public void OnSelect(BaseEventData eventData)
            {
                selected = true;
                transform.SetAsLastSibling();
            }

            public void OnDeselect(BaseEventData eventData)
            {
                selected = false;
            }

            private bool IsHighlighted()
            {
                return button != null && button.interactable && (pointerInside || selected);
            }
        }

        private readonly struct DifficultyButtonBinding
        {
            public DifficultyButtonBinding(Button button, GameDifficulty difficulty)
            {
                Button = button;
                Difficulty = difficulty;
            }

            public Button Button { get; }
            public GameDifficulty Difficulty { get; }
        }
    }
}
