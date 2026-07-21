using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    public static class AdMobSettingsSetup
    {
        private const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";

        [MenuItem("Tools/Symbiosis/Monetization/Use AdMob Test App Id")]
        public static void UseTestAppId()
        {
            Type settingsType = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            if (settingsType == null)
            {
                Debug.LogError("GoogleMobileAdsSettings type was not found.");
                return;
            }

            MethodInfo loadInstance = settingsType.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            object settings = loadInstance?.Invoke(null, null);
            if (settings == null)
            {
                Debug.LogError("GoogleMobileAdsSettings asset could not be loaded.");
                return;
            }

            settingsType.GetProperty("GoogleMobileAdsAndroidAppId")?.SetValue(settings, TestAndroidAppId);
            EditorUtility.SetDirty((UnityEngine.Object)settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"AdMob Android test App ID configured: {TestAndroidAppId}");
        }
    }
}
