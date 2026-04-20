#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class RemoteContentAddressablesSetup
{
    private const string RemoteGroupName = "RemoteContent";
    private const string RemoteBuildPath = "ServerData/[BuildTarget]";
    private const string RemoteLoadPath = "http://91.99.176.77:8080/downloads/addressables/[BuildTarget]";

    [MenuItem("Symbiosis/Addressables/Configure Remote Content")]
    public static void ConfigureRemoteContent()
    {
        AddressableAssetSettings settings = GetOrCreateSettings();
        ConfigureProfiles(settings);
        ConfigureRemoteCatalog(settings);
        ConfigureRemoteGroup(settings);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RemoteContentAddressablesSetup] Addressables remote content configured for " + RemoteLoadPath);
    }

    [MenuItem("Symbiosis/Addressables/Build Remote Content")]
    public static void BuildRemoteContent()
    {
        ConfigureRemoteContent();
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        if (!string.IsNullOrEmpty(result.Error))
            throw new InvalidOperationException("Addressables build failed: " + result.Error);

        Debug.Log("[RemoteContentAddressablesSetup] Addressables build completed. Upload ServerData/Android to /opt/symbiosis/backend/downloads/addressables/Android.");
    }

    [MenuItem("Symbiosis/Addressables/Mark Selected As Remote Content")]
    public static void MarkSelectedAsRemoteContent()
    {
        AddressableAssetSettings settings = GetOrCreateSettings();
        ConfigureProfiles(settings);
        ConfigureRemoteCatalog(settings);
        AddressableAssetGroup group = ConfigureRemoteGroup(settings);

        int marked = 0;
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path) == false && !File.Exists(path))
                continue;

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
                continue;

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(path);
            entry.SetLabel("remote", true, true);
            marked++;
        }

        if (marked == 0)
        {
            Debug.LogWarning("[RemoteContentAddressablesSetup] Select one or more assets or folders before marking remote content.");
            return;
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[RemoteContentAddressablesSetup] Marked remote addressable entries: " + marked);
    }

    public static AddressableAssetSettings GetOrCreateSettings()
    {
        if (!Directory.Exists(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder))
            Directory.CreateDirectory(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder);

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null)
            throw new InvalidOperationException("Could not create Addressables settings.");

        return settings;
    }

    private static void ConfigureProfiles(AddressableAssetSettings settings)
    {
        string profileId = settings.activeProfileId;
        if (string.IsNullOrEmpty(profileId))
            profileId = settings.profileSettings.GetProfileId("Default");

        if (string.IsNullOrEmpty(profileId))
            profileId = settings.profileSettings.AddProfile("Default", null);

        settings.activeProfileId = profileId;
        settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, RemoteBuildPath);
        settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, RemoteLoadPath);
    }

    private static void ConfigureRemoteCatalog(AddressableAssetSettings settings)
    {
        settings.BuildRemoteCatalog = true;
        settings.DisableCatalogUpdateOnStartup = false;
        settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
    }

    private static AddressableAssetGroup ConfigureRemoteGroup(AddressableAssetSettings settings)
    {
        AddressableAssetGroup group = settings.FindGroup(RemoteGroupName);
        if (group == null)
        {
            group = settings.CreateGroup(
                RemoteGroupName,
                false,
                false,
                true,
                null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema)
            );
        }

        BundledAssetGroupSchema bundleSchema = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
        bundleSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        bundleSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
        bundleSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
        bundleSchema.UseAssetBundleCache = true;
        bundleSchema.UseAssetBundleCrc = true;
        bundleSchema.UseAssetBundleCrcForCachedBundles = true;

        ContentUpdateGroupSchema updateSchema = group.GetSchema<ContentUpdateGroupSchema>() ?? group.AddSchema<ContentUpdateGroupSchema>();
        updateSchema.StaticContent = false;

        return group;
    }
}
#endif
