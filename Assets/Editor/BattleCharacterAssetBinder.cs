using MahjongGame;
using UnityEditor;
using UnityEngine;

public static class BattleCharacterAssetBinder
{
    private const string DatabasePrefabPath = "Assets/Resources/BattleCharacters/BattleCharasterDatabase.prefab";

    public static void BindCharacterDatabaseFbxAssets()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DatabasePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[BattleCharacterAssetBinder] Database prefab not found: " + DatabasePrefabPath);
            EditorApplication.Exit(1);
            return;
        }

        BattleCharacterDatabase database = prefab.GetComponent<BattleCharacterDatabase>();
        if (database == null)
        {
            Debug.LogError("[BattleCharacterAssetBinder] BattleCharacterDatabase component not found.");
            EditorApplication.Exit(1);
            return;
        }

        database.EditorAutoAssignSharedFbxAssets();
        EditorUtility.SetDirty(database);
        PrefabUtility.SavePrefabAsset(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BattleCharacterAssetBinder] Character database FBX assets rebound.");
    }
}
