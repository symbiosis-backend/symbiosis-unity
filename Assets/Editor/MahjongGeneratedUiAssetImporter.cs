using UnityEditor;
using UnityEngine;

public sealed class MahjongGeneratedUiAssetImporter : AssetPostprocessor
{
    private const string BambooLobbySpriteRoot = "Assets/Resources/Mahjong/Sprites/BambooLobby/";
    private const string GeneratedSpriteRoot = "Assets/Resources/Mahjong/Sprites/MainSettings/Generated/";
    private const string GeneratedFontAtlasRoot = "Assets/Resources/Mahjong/Fonts/Generated/";

    private void OnPreprocessTexture()
    {
        TextureImporter importer = assetImporter as TextureImporter;
        if (importer == null)
            return;

        string path = assetPath.Replace('\\', '/');
        if (path.StartsWith(BambooLobbySpriteRoot, System.StringComparison.Ordinal) ||
            path.StartsWith(GeneratedSpriteRoot, System.StringComparison.Ordinal))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = path.Contains("FullscreenPanel") ? 4096 : 2048;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            return;
        }

        if (path.StartsWith(GeneratedFontAtlasRoot, System.StringComparison.Ordinal))
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.filterMode = FilterMode.Bilinear;
        }
    }
}
