using System.IO;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using WarOfEras.Core;

namespace WarOfEras.Editor
{
    public static class WarOfErasProjectSetup
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu/MainMenu.unity",
            "Assets/Scenes/Battle/Battle.unity",
            "Assets/Scenes/Result/Result.unity"
        };

        private const string BattleScenePath = "Assets/Scenes/Battle/Battle.unity";
        private const string BackgroundPath = "Assets/Art/SourcePacks/素材包/蛮荒部落素材包/背景/蛮荒部落_背景.png";
        private const string BasePath = "Assets/Art/SourcePacks/素材包/蛮荒部落素材包/基地/蛮荒部落_基地.png";

        private static readonly string[] UnitSpritePaths =
        {
            "Assets/Art/SourcePacks/素材包/蛮荒部落素材包/行进动作_拆分帧/巨骨勇士/巨骨勇士_move_01.png",
            "Assets/Art/SourcePacks/素材包/蛮荒部落素材包/行进动作_拆分帧/猎矛手/猎矛手_move_01.png",
            "Assets/Art/SourcePacks/素材包/蛮荒部落素材包/行进动作_拆分帧/图腾萨满/图腾萨满_move_01.png",
            "Assets/Art/SourcePacks/素材包/蛮荒部落素材包/行进动作_拆分帧/骨牙掠袭者/骨牙掠袭者_move_01.png",
            "Assets/Art/SourcePacks/素材包/蛮荒部落素材包/行进动作_拆分帧/掷石奴/掷石奴_move_01.png"
        };

        public static void Run()
        {
            PlayerSettings.companyName = "XMU";
            PlayerSettings.productName = "War of Eras";

            EnsureFolders();
            CreateScenes();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("War of Eras project setup completed.");
        }

        public static void BuildBattleScene()
        {
            PlayerSettings.companyName = "XMU";
            PlayerSettings.productName = "War of Eras";

            EnsureFolders();
            ConfigureBattleSpriteImporters();
            CreateBattleVisualScene();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("War of Eras battle scene completed.");
        }

        public static void BuildMacOS()
        {
            BuildBattleScene();

            Directory.CreateDirectory("Builds/macOS");

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { BattleScenePath },
                locationPathName = "Builds/macOS/War of Eras.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("War of Eras macOS build failed: " + report.summary.result);
            }

            Debug.Log("War of Eras macOS build completed: " + buildOptions.locationPathName);
        }

        private static void EnsureFolders()
        {
            foreach (var scenePath in ScenePaths)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            }
        }

        private static void CreateScenes()
        {
            foreach (var scenePath in ScenePaths)
            {
                if (File.Exists(scenePath))
                {
                    continue;
                }

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var bootstrap = new GameObject("Game Bootstrap");
                bootstrap.AddComponent<GameBootstrap>();

                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 3f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f);
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);

                EditorSceneManager.SaveScene(scene, scenePath);
            }
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[ScenePaths.Length];
            for (var i = 0; i < ScenePaths.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(ScenePaths[i], true);
            }

            EditorBuildSettings.scenes = scenes;
        }

        private static void ConfigureBattleSpriteImporters()
        {
            ConfigureSpriteImporter(BackgroundPath, 100f);
            ConfigureSpriteImporter(BasePath, 100f);

            foreach (var spritePath in UnitSpritePaths)
            {
                ConfigureSpriteImporter(spritePath, 100f);
            }
        }

        private static void ConfigureSpriteImporter(string path, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing battle sprite asset", path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void CreateBattleVisualScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateSprite("Battle Background", BackgroundPath, new Vector3(0f, 0f, 4f), Vector3.one, 0, false);
            CreateLane();

            CreateSprite("Player Base", BasePath, new Vector3(-5.65f, -1.48f, 0f), new Vector3(0.42f, 0.42f, 1f), 20, false);
            CreateSprite("Enemy Base", BasePath, new Vector3(5.65f, -1.48f, 0f), new Vector3(0.42f, 0.42f, 1f), 20, true);

            var frontLine = new[]
            {
                new Vector3(-1.75f, -1.45f, 0f),
                new Vector3(-0.85f, -1.62f, 0f),
                new Vector3(0.05f, -1.47f, 0f),
                new Vector3(0.95f, -1.64f, 0f),
                new Vector3(1.85f, -1.5f, 0f)
            };

            for (var i = 0; i < UnitSpritePaths.Length; i++)
            {
                CreateSprite("Tribe Unit " + (i + 1), UnitSpritePaths[i], frontLine[i], new Vector3(0.44f, 0.44f, 1f), 35 + i, i > 2);
            }

            CreateLabel("War of Eras - Battle Scene", new Vector3(0f, 3.25f, 0f), 0.34f, Color.white, FontStyle.Bold);
            CreateLabel("Visual blockout using imported tribe assets", new Vector3(0f, 2.82f, 0f), 0.18f, new Color(0.82f, 0.9f, 1f), FontStyle.Normal);

            EditorSceneManager.SaveScene(scene, BattleScenePath);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.08f);
        }

        private static void CreateLane()
        {
            var lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = "Main Battle Lane";
            lane.transform.position = new Vector3(0f, -2.08f, 1f);
            lane.transform.localScale = new Vector3(12.5f, 0.18f, 0.05f);

            var renderer = lane.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.2f, 0.13f, 0.08f, 0.78f)
            };
        }

        private static void CreateSprite(string name, string assetPath, Vector3 position, Vector3 scale, int sortingOrder, bool flipX)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new FileNotFoundException("Unable to load sprite", assetPath);
            }

            var spriteObject = new GameObject(name);
            spriteObject.transform.position = position;
            spriteObject.transform.localScale = scale;

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.flipX = flipX;
        }

        private static void CreateLabel(string value, Vector3 position, float size, Color color, FontStyle style)
        {
            var labelObject = new GameObject(value);
            labelObject.transform.position = position;

            var text = labelObject.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = size;
            text.color = color;
            text.fontStyle = style;
        }
    }
}
