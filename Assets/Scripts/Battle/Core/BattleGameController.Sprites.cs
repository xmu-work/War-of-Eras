using System;
using UnityEngine;

namespace WarOfEras.Battle.Core
{
    public sealed partial class BattleGameController
    {
        internal static Sprite SharedWhiteSprite => WhiteSprite;

        private static Sprite PanelSprite
        {
            get
            {
                if (panelSprite == null)
                {
                    panelSprite = CreateBeveledUiSprite(96, 64, 10, 0.72f, 1f);
                }

                return panelSprite;
            }
        }

        private static Sprite ButtonSprite
        {
            get
            {
                if (buttonSprite == null)
                {
                    buttonSprite = CreateBeveledUiSprite(120, 48, 14, 0.62f, 1f);
                }

                return buttonSprite;
            }
        }

        private static Sprite IconDiscSprite
        {
            get
            {
                if (iconDiscSprite == null)
                {
                    iconDiscSprite = CreateDiscUiSprite(48);
                }

                return iconDiscSprite;
            }
        }

        private static Sprite VfxCircleSprite
        {
            get
            {
                if (vfxCircleSprite == null)
                {
                    vfxCircleSprite = CreateSoftCircleSprite(96);
                }

                return vfxCircleSprite;
            }
        }

        private static Sprite ResourceWellSiteSprite
        {
            get
            {
                if (resourceWellSiteSprite == null)
                {
                    resourceWellSiteSprite = LoadGeneratedSprite("Battle/Facilities/ResourceWellSite", 100f, () => CreateResourceWellSiteSprite(96));
                }

                return resourceWellSiteSprite;
            }
        }

        private static Sprite ResourceWellBuiltSprite
        {
            get
            {
                if (resourceWellBuiltSprite == null)
                {
                    resourceWellBuiltSprite = LoadGeneratedSprite("Battle/Facilities/ResourceWellBuilt", 100f, () => CreateResourceWellBuiltSprite(128));
                }

                return resourceWellBuiltSprite;
            }
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

        private static Sprite LoadGeneratedSprite(string resourcePath, float pixelsPerUnit, Func<Sprite> fallbackFactory)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return fallbackFactory();
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            sprite.name = resourcePath;
            return sprite;
        }

        private static Sprite CreateBeveledUiSprite(int width, int height, int cornerRadius, float baseShade, float alpha)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!IsInsideRoundedRect(x, y, width, height, cornerRadius))
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var left = x / (float)(width - 1);
                    var top = y / (float)(height - 1);
                    var edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    var bevel = edge < 3f ? -0.24f : edge < 8f ? 0.18f : 0f;
                    var diagonalLight = Mathf.Lerp(0.12f, -0.1f, (left + (1f - top)) * 0.5f);
                    var shade = Mathf.Clamp01(baseShade + bevel + diagonalLight);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
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
                new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        }

        private static Sprite CreateDiscUiSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.48f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var ring = distance > radius - 4f ? 0.48f : 0f;
                    var highlight = y > center.y ? 0.16f : -0.06f;
                    var shade = Mathf.Clamp01(0.7f + ring + highlight);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, 1f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateSoftCircleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.48f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var normalized = Mathf.Clamp01(distance / radius);
                    var alpha = Mathf.SmoothStep(0.85f, 0.05f, normalized);
                    if (normalized > 0.72f)
                    {
                        alpha = Mathf.Max(alpha, Mathf.SmoothStep(1f, 0.72f, normalized) * 0.75f);
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateResourceWellSiteSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var half = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center.x) / half;
                    var dy = (y - center.y) / half;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
                    var alpha = 0f;

                    if (distance > 0.36f && distance < 0.46f)
                    {
                        alpha = Mathf.Max(alpha, 0.82f);
                    }

                    if (diamond > 0.62f && diamond < 0.72f)
                    {
                        alpha = Mathf.Max(alpha, 0.62f);
                    }

                    if (Mathf.Abs(dx) < 0.045f && Mathf.Abs(dy) < 0.32f)
                    {
                        alpha = Mathf.Max(alpha, 0.7f);
                    }

                    if (Mathf.Abs(dy) < 0.045f && Mathf.Abs(dx) < 0.32f)
                    {
                        alpha = Mathf.Max(alpha, 0.7f);
                    }

                    if (distance < 0.12f)
                    {
                        alpha = Mathf.Max(alpha, 0.9f);
                    }

                    texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateResourceWellBuiltSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.45f);
            var half = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center.x) / half;
                    var dy = (y - center.y) / half;
                    var body = dx * dx / 0.48f + dy * dy / 0.24f;
                    var inner = dx * dx / 0.26f + dy * dy / 0.1f;
                    var post = Mathf.Abs(dx) > 0.3f && Mathf.Abs(dx) < 0.42f && dy > -0.06f && dy < 0.48f;
                    var roof = Mathf.Abs(dy - 0.5f) < 0.08f && Mathf.Abs(dx) < 0.5f - Mathf.Abs(dy - 0.5f);

                    if (inner <= 1f)
                    {
                        var shimmer = 0.08f * Mathf.Sin((x + y) * 0.18f);
                        texture.SetPixel(x, y, new Color(0.16f + shimmer, 0.54f + shimmer, 0.78f + shimmer, 0.96f));
                    }
                    else if (body <= 1f || post || roof)
                    {
                        var shade = Mathf.Clamp01(0.58f + 0.12f * Mathf.Sin((x * 3f + y * 5f) * 0.05f) - Mathf.Abs(dy) * 0.1f);
                        texture.SetPixel(x, y, new Color(shade, shade * 0.92f, shade * 0.76f, 1f));
                    }
                    else if (body <= 1.18f)
                    {
                        texture.SetPixel(x, y, new Color(0.08f, 0.06f, 0.04f, 0.36f));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            var cornerX = x < radius ? radius : width - 1 - radius;
            var cornerY = y < radius ? radius : height - 1 - radius;
            if ((x >= radius && x < width - radius) || (y >= radius && y < height - radius))
            {
                return true;
            }

            return Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY)) <= radius;
        }
    }
}