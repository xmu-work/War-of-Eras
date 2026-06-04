using System;
using UnityEngine;

namespace WarOfEras.Battle.Core
{
    public sealed partial class BattleGameController
    {
        internal static Sprite SharedWhiteSprite => WhiteSprite;
        internal static Sprite SharedVfxCircleSprite => VfxCircleSprite;

        private static readonly Vector2[] AgePowerLightningIconPolygon =
        {
            new Vector2(-0.12f, 0.72f),
            new Vector2(0.26f, 0.1f),
            new Vector2(0.07f, 0.1f),
            new Vector2(0.22f, -0.72f),
            new Vector2(-0.35f, -0.04f),
            new Vector2(-0.1f, -0.04f)
        };

        private static readonly Vector2[] CommandShieldOuterPolygon =
        {
            new Vector2(0f, 0.74f),
            new Vector2(0.5f, 0.32f),
            new Vector2(0.32f, -0.58f),
            new Vector2(0f, -0.78f),
            new Vector2(-0.32f, -0.58f),
            new Vector2(-0.5f, 0.32f)
        };

        private static readonly Vector2[] CommandShieldInnerPolygon =
        {
            new Vector2(0f, 0.54f),
            new Vector2(0.32f, 0.22f),
            new Vector2(0.2f, -0.42f),
            new Vector2(0f, -0.58f),
            new Vector2(-0.2f, -0.42f),
            new Vector2(-0.32f, 0.22f)
        };

        private static readonly Vector2[] MobilizationFlagPolygon =
        {
            new Vector2(-0.4f, 0.58f),
            new Vector2(0.46f, 0.48f),
            new Vector2(0.25f, 0.18f),
            new Vector2(0.46f, -0.04f),
            new Vector2(-0.4f, 0.04f)
        };

        private static readonly Vector2[] AttackEvolutionArrowheadPolygon =
        {
            new Vector2(0.46f, 0.58f),
            new Vector2(0.52f, 0.12f),
            new Vector2(0.08f, 0.28f)
        };

        private static readonly Vector2[] RestartArrowheadPolygon =
        {
            new Vector2(0.32f, 0.62f),
            new Vector2(0.68f, 0.54f),
            new Vector2(0.5f, 0.22f)
        };

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

        private static Sprite TowerBuildMarkerSprite
        {
            get
            {
                if (towerBuildMarkerSprite == null)
                {
                    towerBuildMarkerSprite = CreateTowerBuildMarkerSprite(128);
                }

                return towerBuildMarkerSprite;
            }
        }

        private static Sprite ResourceWellBuildMarkerSprite
        {
            get
            {
                if (resourceWellBuildMarkerSprite == null)
                {
                    resourceWellBuildMarkerSprite = CreateResourceWellBuildMarkerSprite(128);
                }

                return resourceWellBuildMarkerSprite;
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

        private static Sprite GetAgePowerButtonSprite(int index)
        {
            var clampedIndex = Mathf.Clamp(index, 0, AgePowers.Length - 1);
            if (agePowerIconSprites == null || agePowerIconSprites.Length != AgePowers.Length)
            {
                agePowerIconSprites = new Sprite[AgePowers.Length];
            }

            if (agePowerIconSprites[clampedIndex] == null)
            {
                agePowerIconSprites[clampedIndex] = CreateAgePowerIconSprite(clampedIndex);
            }

            return agePowerIconSprites[clampedIndex];
        }

        private static Sprite ShieldIconSprite
        {
            get
            {
                if (shieldIconSprite == null)
                {
                    shieldIconSprite = CreateCommandIconSprite(
                        CommandIconKind.Shield,
                        0,
                        new Color(0.18f, 0.42f, 0.66f, 1f),
                        new Color(0.82f, 0.94f, 1f, 1f),
                        "Generated Shield Command Icon");
                }

                return shieldIconSprite;
            }
        }

        private static Sprite MobilizationIconSprite
        {
            get
            {
                if (mobilizationIconSprite == null)
                {
                    mobilizationIconSprite = CreateCommandIconSprite(
                        CommandIconKind.Mobilization,
                        0,
                        new Color(0.58f, 0.43f, 0.14f, 1f),
                        new Color(1f, 0.84f, 0.34f, 1f),
                        "Generated Mobilization Command Icon");
                }

                return mobilizationIconSprite;
            }
        }

        private static Sprite AttackEvolutionIconSprite
        {
            get
            {
                if (attackEvolutionIconSprite == null)
                {
                    attackEvolutionIconSprite = CreateCommandIconSprite(
                        CommandIconKind.AttackEvolution,
                        0,
                        new Color(0.65f, 0.16f, 0.12f, 1f),
                        new Color(1f, 0.62f, 0.26f, 1f),
                        "Generated Attack Evolution Command Icon");
                }

                return attackEvolutionIconSprite;
            }
        }

        private static Sprite DefenseEvolutionIconSprite
        {
            get
            {
                if (defenseEvolutionIconSprite == null)
                {
                    defenseEvolutionIconSprite = CreateCommandIconSprite(
                        CommandIconKind.DefenseEvolution,
                        0,
                        new Color(0.18f, 0.38f, 0.58f, 1f),
                        new Color(0.76f, 0.96f, 1f, 1f),
                        "Generated Defense Evolution Command Icon");
                }

                return defenseEvolutionIconSprite;
            }
        }

        private static Sprite RestartIconSprite
        {
            get
            {
                if (restartIconSprite == null)
                {
                    restartIconSprite = CreateCommandIconSprite(
                        CommandIconKind.Restart,
                        0,
                        new Color(0.38f, 0.32f, 0.48f, 1f),
                        new Color(0.95f, 0.86f, 1f, 1f),
                        "Generated Restart Command Icon");
                }

                return restartIconSprite;
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

        private static Sprite CreateTowerBuildMarkerSprite(int size)
        {
            var texture = CreateMarkerTexture(size);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var half = size * 0.5f;
            var outerShield = new[]
            {
                new Vector2(0f, 0.74f),
                new Vector2(0.55f, 0.26f),
                new Vector2(0.34f, -0.66f),
                new Vector2(-0.34f, -0.66f),
                new Vector2(-0.55f, 0.26f)
            };
            var innerShield = new[]
            {
                new Vector2(0f, 0.58f),
                new Vector2(0.38f, 0.2f),
                new Vector2(0.24f, -0.48f),
                new Vector2(-0.24f, -0.48f),
                new Vector2(-0.38f, 0.2f)
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center.x) / half;
                    var dy = (y - center.y) / half;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var point = new Vector2(dx, dy);
                    var color = Color.clear;

                    if (distance > 0.79f && distance < 0.9f)
                    {
                        color = new Color(0.18f, 1f, 0.32f, 0.92f);
                    }
                    else if (distance > 0.61f && distance < 0.68f)
                    {
                        color = new Color(0.96f, 0.71f, 0.28f, 0.96f);
                    }

                    if (IsPointInPolygon(point, outerShield))
                    {
                        color = new Color(0.55f, 0.13f, 0.12f, 1f);
                    }

                    if (IsPointInPolygon(point, innerShield))
                    {
                        color = new Color(0.88f, 0.33f, 0.23f, 1f);
                    }

                    if (Mathf.Abs(dx) < 0.14f && dy > -0.5f && dy < 0.2f)
                    {
                        color = new Color(0.2f, 0.08f, 0.06f, 1f);
                    }

                    if (Mathf.Abs(dx) < 0.06f && dy > 0.36f && dy < 0.76f)
                    {
                        color = new Color(0.96f, 0.82f, 0.42f, 1f);
                    }

                    if (Mathf.Abs(dx) < 0.36f && dy > -0.76f && dy < -0.61f)
                    {
                        color = new Color(1f, 0.86f, 0.43f, 1f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            return CreateMarkerSprite(texture, "Generated Tower Build Marker");
        }

        private static Sprite CreateResourceWellBuildMarkerSprite(int size)
        {
            var texture = CreateMarkerTexture(size);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var half = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center.x) / half;
                    var dy = (y - center.y) / half;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var ellipse = dx * dx / 0.86f + dy * dy / 0.56f;
                    var color = Color.clear;

                    if (ellipse > 0.82f && ellipse < 1.05f)
                    {
                        color = new Color(0.19f, 1f, 0.37f, 0.9f);
                    }

                    if (distance < 0.58f)
                    {
                        color = new Color(0.06f, 0.31f, 0.42f, 1f);
                    }

                    if (distance < 0.43f)
                    {
                        color = new Color(0.26f, 0.82f, 1f, 1f);
                    }

                    if (distance > 0.49f && distance < 0.61f)
                    {
                        color = new Color(0.88f, 0.71f, 0.34f, 1f);
                    }

                    if (Mathf.Abs(dx) > 0.28f && Mathf.Abs(dx) < 0.43f && dy > -0.18f && dy < 0.64f)
                    {
                        color = new Color(0.86f, 0.7f, 0.34f, 1f);
                    }

                    if (Mathf.Abs(dx) < 0.56f && dy > 0.57f && dy < 0.74f)
                    {
                        color = new Color(0.96f, 0.82f, 0.43f, 1f);
                    }

                    if (Mathf.Abs(dx) < 0.7f && dy < -0.58f && dy > -0.75f)
                    {
                        color = new Color(0.31f, 0.25f, 0.13f, 1f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            return CreateMarkerSprite(texture, "Generated Resource Well Build Marker");
        }

        private static Texture2D CreateMarkerTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        private static Sprite CreateMarkerSprite(Texture2D texture, string name)
        {
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
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

        private static Sprite CreateAgePowerIconSprite(int index)
        {
            var clampedIndex = Mathf.Clamp(index, 0, AgeTints.Length - 1);
            var tint = AgeTints[clampedIndex];
            return CreateCommandIconSprite(
                CommandIconKind.AgePower,
                clampedIndex,
                Color.Lerp(tint, Color.black, 0.42f),
                Color.Lerp(tint, Color.white, 0.32f),
                "Generated Age Power Command Icon " + clampedIndex);
        }

        private static Sprite CreateCommandIconSprite(CommandIconKind kind, int variant, Color baseColor, Color accentColor, string name)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var half = size * 0.5f;
            var brightAccent = Color.Lerp(accentColor, Color.white, 0.28f);
            var shadowAccent = Color.Lerp(accentColor, Color.black, 0.34f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2((x - center.x) / half, (y - center.y) / half);
                    var dx = point.x;
                    var dy = point.y;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var color = Color.clear;

                    if (distance <= 0.92f)
                    {
                        var shade = Mathf.Clamp01(0.42f + (1f - distance) * 0.44f + dy * 0.08f);
                        color = Color.Lerp(Color.black, baseColor, shade);
                        color.a = 1f;

                        if (distance > 0.78f)
                        {
                            color = Color.Lerp(color, accentColor, Mathf.InverseLerp(0.78f, 0.92f, distance) * 0.72f);
                        }
                    }

                    switch (kind)
                    {
                        case CommandIconKind.AgePower:
                            PaintAgePowerIconPixel(variant, point, distance, brightAccent, shadowAccent, ref color);
                            break;
                        case CommandIconKind.Shield:
                            PaintShieldIconPixel(point, brightAccent, shadowAccent, ref color);
                            break;
                        case CommandIconKind.Mobilization:
                            PaintMobilizationIconPixel(point, brightAccent, shadowAccent, ref color);
                            break;
                        case CommandIconKind.AttackEvolution:
                            PaintAttackEvolutionIconPixel(point, brightAccent, shadowAccent, ref color);
                            break;
                        case CommandIconKind.DefenseEvolution:
                            PaintDefenseEvolutionIconPixel(point, brightAccent, shadowAccent, ref color);
                            break;
                        case CommandIconKind.Restart:
                            PaintRestartIconPixel(point, distance, brightAccent, shadowAccent, ref color);
                            break;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        private static void PaintAgePowerIconPixel(int variant, Vector2 point, float distance, Color accent, Color shadow, ref Color color)
        {
            var dx = point.x;
            var dy = point.y;

            switch (variant)
            {
                case 0:
                    if (Mathf.Abs(distance - 0.36f) < 0.028f || Mathf.Abs(distance - 0.58f) < 0.022f)
                    {
                        color = accent;
                    }

                    if (IsPointNearSegment(point, new Vector2(-0.58f, 0.12f), new Vector2(-0.22f, -0.02f), 0.035f)
                        || IsPointNearSegment(point, new Vector2(-0.22f, -0.02f), new Vector2(0.08f, 0.14f), 0.035f)
                        || IsPointNearSegment(point, new Vector2(0.08f, 0.14f), new Vector2(0.52f, -0.04f), 0.035f))
                    {
                        color = Color.Lerp(shadow, Color.black, 0.24f);
                    }

                    break;
                case 1:
                    if ((dx + 0.05f) * (dx + 0.05f) / 0.16f + (dy + 0.1f) * (dy + 0.1f) / 0.14f < 1f)
                    {
                        color = accent;
                    }

                    if (Mathf.Abs(dx + 0.05f) < 0.08f && dy > 0.2f && dy < 0.42f)
                    {
                        color = shadow;
                    }

                    if (IsPointNearSegment(point, new Vector2(0.03f, 0.42f), new Vector2(0.32f, 0.66f), 0.035f)
                        || (Mathf.Abs(dx - 0.4f) < 0.08f && Mathf.Abs(dy - 0.7f) < 0.08f))
                    {
                        color = Color.Lerp(accent, Color.white, 0.34f);
                    }

                    break;
                case 2:
                    if (IsPointInPolygon(point, AgePowerLightningIconPolygon))
                    {
                        color = accent;
                    }

                    break;
                case 3:
                    if (distance < 0.14f)
                    {
                        color = accent;
                    }

                    var angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f, 360f);
                    var sector = Mathf.Repeat(angle + 30f, 120f);
                    if (distance > 0.28f && distance < 0.68f && sector < 42f)
                    {
                        color = accent;
                    }

                    if (Mathf.Abs(distance - 0.76f) < 0.028f)
                    {
                        color = shadow;
                    }

                    break;
                default:
                    if (Mathf.Abs(distance - 0.52f) < 0.032f)
                    {
                        color = accent;
                    }

                    if (IsPointNearSegment(point, Vector2.zero, new Vector2(0.02f, 0.44f), 0.032f)
                        || IsPointNearSegment(point, Vector2.zero, new Vector2(0.34f, -0.18f), 0.032f))
                    {
                        color = accent;
                    }

                    if (Mathf.Abs(distance - 0.34f) < 0.02f && Mathf.Abs(dx) > 0.18f)
                    {
                        color = shadow;
                    }

                    break;
            }
        }

        private static void PaintShieldIconPixel(Vector2 point, Color accent, Color shadow, ref Color color)
        {
            if (IsPointInPolygon(point, CommandShieldOuterPolygon))
            {
                color = shadow;
            }

            if (IsPointInPolygon(point, CommandShieldInnerPolygon))
            {
                color = accent;
            }

            if (Mathf.Abs(point.x) < 0.04f && point.y > -0.52f && point.y < 0.48f)
            {
                color = Color.Lerp(accent, Color.white, 0.34f);
            }
        }

        private static void PaintMobilizationIconPixel(Vector2 point, Color accent, Color shadow, ref Color color)
        {
            if (Mathf.Abs(point.x + 0.42f) < 0.035f && point.y > -0.62f && point.y < 0.66f)
            {
                color = shadow;
            }

            if (IsPointInPolygon(point, MobilizationFlagPolygon))
            {
                color = accent;
            }

            if (IsPointNearSegment(point, new Vector2(-0.18f, -0.24f), new Vector2(0.5f, -0.24f), 0.04f)
                || IsPointNearSegment(point, new Vector2(0.22f, -0.44f), new Vector2(0.5f, -0.24f), 0.04f)
                || IsPointNearSegment(point, new Vector2(0.22f, -0.04f), new Vector2(0.5f, -0.24f), 0.04f))
            {
                color = Color.Lerp(accent, Color.white, 0.26f);
            }
        }

        private static void PaintAttackEvolutionIconPixel(Vector2 point, Color accent, Color shadow, ref Color color)
        {
            if (IsPointNearSegment(point, new Vector2(-0.48f, -0.48f), new Vector2(0.42f, 0.38f), 0.055f))
            {
                color = accent;
            }

            if (IsPointInPolygon(point, AttackEvolutionArrowheadPolygon))
            {
                color = accent;
            }

            if (IsPointNearSegment(point, new Vector2(-0.5f, 0.32f), new Vector2(-0.08f, 0.64f), 0.035f)
                || IsPointNearSegment(point, new Vector2(-0.5f, 0.14f), new Vector2(-0.2f, 0.36f), 0.03f))
            {
                color = shadow;
            }
        }

        private static void PaintDefenseEvolutionIconPixel(Vector2 point, Color accent, Color shadow, ref Color color)
        {
            PaintShieldIconPixel(point * 1.1f + new Vector2(0f, -0.12f), accent, shadow, ref color);

            if (IsPointNearSegment(point, new Vector2(0f, -0.46f), new Vector2(0f, 0.42f), 0.045f)
                || IsPointNearSegment(point, new Vector2(-0.22f, 0.2f), new Vector2(0f, 0.46f), 0.045f)
                || IsPointNearSegment(point, new Vector2(0.22f, 0.2f), new Vector2(0f, 0.46f), 0.045f))
            {
                color = Color.Lerp(accent, Color.white, 0.32f);
            }
        }

        private static void PaintRestartIconPixel(Vector2 point, float distance, Color accent, Color shadow, ref Color color)
        {
            var angle = Mathf.Repeat(Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg + 360f, 360f);
            if (distance > 0.42f && distance < 0.58f && angle > 26f && angle < 330f)
            {
                color = accent;
            }

            if (IsPointInPolygon(point, RestartArrowheadPolygon))
            {
                color = accent;
            }

            if (IsPointNearSegment(point, new Vector2(-0.2f, -0.12f), new Vector2(0.2f, -0.12f), 0.04f)
                || IsPointNearSegment(point, new Vector2(0f, -0.34f), new Vector2(0.2f, -0.12f), 0.04f)
                || IsPointNearSegment(point, new Vector2(0f, 0.1f), new Vector2(0.2f, -0.12f), 0.04f))
            {
                color = shadow;
            }
        }

        private static bool IsPointNearSegment(Vector2 point, Vector2 start, Vector2 end, float width)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return Vector2.Distance(point, start) <= width;
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            var closest = start + segment * t;
            return Vector2.Distance(point, closest) <= width;
        }

        private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                if (((pi.y > point.y) != (pj.y > point.y))
                    && point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
                {
                    inside = !inside;
                }
            }

            return inside;
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
