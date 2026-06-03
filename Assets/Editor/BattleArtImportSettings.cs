#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace WarOfEras.EditorTools
{
    public static class BattleArtImportSettings
    {
        public static void Apply()
        {
            // 当前只批量修正蛮荒时代图片导入设置，后续时代资源可按相同规则扩展 root。
            var root = "Assets/Resources/Barbarian";
            foreach (var path in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
            {
                var assetPath = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
