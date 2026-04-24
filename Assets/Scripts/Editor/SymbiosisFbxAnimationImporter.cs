using MahjongGame;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SymbiosisFbxAnimationImporter
{
    private const string CharacterFbxFolder = "Assets/Scripts/Mahjong/Sprites/FBX";
    private const string BearBaseColorPath = "Assets/Scripts/Mahjong/Sprites/FBX/BearMale_basecolor.JPEG";
    private const string BearMaterialPath = "Assets/Scripts/Mahjong/Sprites/FBX/BearMaterial.mat";

    static SymbiosisFbxAnimationImporter()
    {
        EditorApplication.delayCall += ConfigureCharacterFbxAnimations;
    }

    [MenuItem("Symbiosis/Characters/Reimport FBX Animations")]
    public static void ConfigureCharacterFbxAnimations()
    {
        bool reimported = false;

        ConfigureCharacterTextureImports();
        ConfigureBearMaterial();

        string[] characterFbxPaths = FindCharacterFbxPaths();
        for (int i = 0; i < characterFbxPaths.Length; i++)
        {
            if (ConfigureImporter(characterFbxPaths[i]))
                reimported = true;
        }

        if (reimported)
            AssetDatabase.SaveAssets();

        BindCharacterDatabaseAnimations();
    }

    private static string[] FindCharacterFbxPaths()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { CharacterFbxFolder });
        System.Collections.Generic.List<string> paths = new System.Collections.Generic.List<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                paths.Add(path);
        }

        return paths.ToArray();
    }

    private static bool ConfigureImporter(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return false;

        bool changed = false;
        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            changed = true;
        }

        if (importer.animationType != ModelImporterAnimationType.Generic)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            changed = true;
        }

        if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            changed = true;
        }

        if (importer.materialSearch != ModelImporterMaterialSearch.Local)
        {
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            changed = true;
        }

        if (importer.materialName != ModelImporterMaterialName.BasedOnMaterialName)
        {
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            changed = true;
        }

        if (importer.importNormals != ModelImporterNormals.Import)
        {
            importer.importNormals = ModelImporterNormals.Import;
            changed = true;
        }

        if (importer.importTangents != ModelImporterTangents.CalculateMikk)
        {
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            changed = true;
        }

        if (!importer.importLights)
        {
            importer.importLights = true;
            changed = true;
        }

        if (!importer.importCameras)
        {
            importer.importCameras = true;
            changed = true;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips != null && clips.Length > 0)
        {
            string fallbackName = System.IO.Path.GetFileNameWithoutExtension(path);
            bool clipsChanged = false;
            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation clip = clips[i];
                if (string.IsNullOrWhiteSpace(clip.name))
                {
                    clip.name = fallbackName;
                    clipsChanged = true;
                }

                clipsChanged |= !clip.loopTime;
                clipsChanged |= !clip.loopPose;
                clipsChanged |= !clip.lockRootRotation;
                clipsChanged |= !clip.lockRootHeightY;
                clipsChanged |= !clip.lockRootPositionXZ;
                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clips[i] = clip;
            }

            if (clipsChanged || importer.clipAnimations == null || importer.clipAnimations.Length == 0)
            {
                importer.clipAnimations = clips;
                changed = true;
            }
        }

        if (!changed)
            return false;

        importer.SaveAndReimport();
        return true;
    }

    private static void ConfigureCharacterTextureImports()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CharacterFbxFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name.IndexOf("basecolor", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("albedo", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            ConfigureTextureImport(path, true);
        }
    }

    private static void ConfigureTextureImport(string texturePath, bool forceUpdate)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            changed = true;
        }

        if (importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = false;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
        else if (forceUpdate)
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
    }

    private static Material ConfigureBearMaterial()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BearBaseColorPath);
        if (texture == null)
            return null;

        Material material = AssetDatabase.LoadAssetAtPath<Material>(BearMaterialPath);
        bool created = false;
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
                return null;

            material = new Material(shader)
            {
                name = "BearMaterial"
            };
            AssetDatabase.CreateAsset(material, BearMaterialPath);
            created = true;
        }

        bool changed = AssignTextureIfNeeded(material, texture);
        changed |= AssignColorIfNeeded(material, Color.white);
        changed |= SetFloatIfNeeded(material, "_Smoothness", 0.25f);
        changed |= SetFloatIfNeeded(material, "_Glossiness", 0.25f);

        if (created || changed)
        {
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        return material;
    }

    private static bool AssignTextureIfNeeded(Material material, Texture2D texture)
    {
        bool changed = false;
        changed |= SetTextureIfNeeded(material, "_BaseMap", texture);
        changed |= SetTextureIfNeeded(material, "_MainTex", texture);
        changed |= SetTextureIfNeeded(material, "_BaseColorMap", texture);
        changed |= SetTextureIfNeeded(material, "_DiffuseMap", texture);
        changed |= SetTextureIfNeeded(material, "_AlbedoMap", texture);

        if (material.mainTexture != texture)
        {
            material.mainTexture = texture;
            changed = true;
        }

        if (material.mainTextureOffset != Vector2.zero)
        {
            material.mainTextureOffset = Vector2.zero;
            changed = true;
        }

        if (material.mainTextureScale != Vector2.one)
        {
            material.mainTextureScale = Vector2.one;
            changed = true;
        }

        return changed;
    }

    private static bool SetTextureIfNeeded(Material material, string propertyName, Texture texture)
    {
        if (!material.HasProperty(propertyName) || material.GetTexture(propertyName) == texture)
            return false;

        material.SetTexture(propertyName, texture);
        return true;
    }

    private static bool AssignColorIfNeeded(Material material, Color color)
    {
        bool changed = false;
        changed |= SetColorIfNeeded(material, "_BaseColor", color);
        changed |= SetColorIfNeeded(material, "_Color", color);
        return changed;
    }

    private static bool SetColorIfNeeded(Material material, string propertyName, Color color)
    {
        if (!material.HasProperty(propertyName) || material.GetColor(propertyName) == color)
            return false;

        material.SetColor(propertyName, color);
        return true;
    }

    private static bool SetFloatIfNeeded(Material material, string propertyName, float value)
    {
        if (!material.HasProperty(propertyName) || Mathf.Approximately(material.GetFloat(propertyName), value))
            return false;

        material.SetFloat(propertyName, value);
        return true;
    }

    private static void BindCharacterDatabaseAnimations()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/BattleCharacters/BattleCharasterDatabase.prefab");
        BattleCharacterDatabase database = prefab != null
            ? prefab.GetComponent<BattleCharacterDatabase>()
            : null;

        if (database == null)
            return;

        database.EditorAutoAssignSharedFbxAssets();
        EditorUtility.SetDirty(database);
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
    }
}
