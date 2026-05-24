#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace WarOfEras.EditorTools
{
    public static class BattleArtImportSettings
    {
        public static void Apply()
        {
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
