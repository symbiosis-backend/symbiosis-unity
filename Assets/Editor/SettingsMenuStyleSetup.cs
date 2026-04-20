using UnityEditor;
using UnityEngine;

public static class SettingsMenuStyleSetup
{
    private const string PrefabPath = "Assets/Scripts/Mahjong/Prefabs/SettingsMenu.prefab";

    [MenuItem("Tools/Mahjong/Setup Settings Menu Styles")]
    public static void SetupSettingsMenuStyles()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[SettingsMenuStyleSetup] SettingsMenu prefab not found.");
            return;
        }

        Component component = prefab.GetComponent("SettingsMenuUI");
        if (component == null)
        {
            Debug.LogWarning("[SettingsMenuStyleSetup] SettingsMenuUI component not found.");
            return;
        }

        SerializedObject serialized = new SerializedObject(component);

        SerializedProperty styles = serialized.FindProperty("sceneVisualStyles");
        if (styles == null)
            return;

        styles.arraySize = 3;

        ConfigureStyle(
            styles.GetArrayElementAtIndex(0),
            "GameMahjongBattle",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -28f),
            new Vector2(90f, 90f));

        ConfigureStyle(
            styles.GetArrayElementAtIndex(1),
            "LobbyMahjongBattle",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-58f, -42f),
            new Vector2(90f, 90f));

        ConfigureStyle(
            styles.GetArrayElementAtIndex(2),
            "LobbyMahjong",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-58f, -42f),
            new Vector2(90f, 90f));

        SetBool(serialized, "applyVisualOverrides", true);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prefab);
        PrefabUtility.SavePrefabAsset(prefab);
        AssetDatabase.SaveAssets();

        Debug.Log("[SettingsMenuStyleSetup] SettingsMenu scene styles configured.");
    }

    private static void ConfigureStyle(
        SerializedProperty style,
        string sceneName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        SetString(style, "SceneName", sceneName);
        SetBool(style, "ApplyOpenButtonRect", true);
        SetVector2(style, "OpenButtonAnchorMin", anchorMin);
        SetVector2(style, "OpenButtonAnchorMax", anchorMax);
        SetVector2(style, "OpenButtonPivot", pivot);
        SetVector2(style, "OpenButtonPosition", position);
        SetVector2(style, "OpenButtonSize", size);
        SetBool(style, "ApplyOpenButtonGraphic", false);
        SetBool(style, "ApplyPanelRect", false);
        SetBool(style, "ApplyPanelColor", false);
        SetBool(style, "ApplyWindowRect", false);
        SetBool(style, "ApplyWindowGraphic", false);
        SetBool(style, "ApplySettingButtonSize", false);
        SetBool(style, "ApplySettingButtonColors", false);
    }

    private static void SetString(SerializedProperty parent, string name, string value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null)
            property.stringValue = value;
    }

    private static void SetBool(SerializedObject serialized, string name, bool value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
            property.boolValue = value;
    }

    private static void SetBool(SerializedProperty parent, string name, bool value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null)
            property.boolValue = value;
    }

    private static void SetVector2(SerializedProperty parent, string name, Vector2 value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null)
            property.vector2Value = value;
    }
}
