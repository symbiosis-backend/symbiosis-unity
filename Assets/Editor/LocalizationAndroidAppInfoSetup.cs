using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Platform.Android;
using UnityEngine.Localization.Settings;

namespace MahjongGame.Editor
{
    public static class LocalizationAndroidAppInfoSetup
    {
        [MenuItem("Tools/OzGame/Configure Android Localization App Info")]
        public static void EnsureConfigured()
        {
            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                Debug.LogWarning("[LocalizationAndroidAppInfoSetup] Skipped. No active Unity Localization Settings asset is configured. OzGame uses GameLocalization, so Unity Localization/Addressables should stay disabled unless real Locale assets are added.");
                return;
            }

            AppInfo appInfo = LocalizationSettings.Metadata.GetMetadata<AppInfo>();
            if (appInfo == null)
            {
                appInfo = new AppInfo();
                LocalizationSettings.Metadata.AddMetadata(appInfo);
                Debug.Log("[LocalizationAndroidAppInfoSetup] Added Android App Info metadata.");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
