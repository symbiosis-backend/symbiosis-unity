using UnityEngine;

namespace MahjongGame
{
    public static class BattleCharacterRuntimeBootstrap
    {
        private const string DatabaseResourcePath = "BattleCharacters/BattleCharasterDatabase";
        private const string SelectionServiceResourcePath = "BattleCharacters/BattleCharasterSelectionService";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBattleCharacterServices()
        {
            EnsureDatabase();
            EnsureSelectionService();
        }

        private static void EnsureDatabase()
        {
            if (BattleCharacterDatabase.HasInstance)
                return;

            InstantiateResource(DatabaseResourcePath, "BattleCharasterDatabase");
        }

        private static void EnsureSelectionService()
        {
            if (BattleCharacterSelectionService.HasInstance)
                return;

            InstantiateResource(SelectionServiceResourcePath, "BattleCharasterSelectionService");
        }

        private static void InstantiateResource(string resourcePath, string fallbackName)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[BattleCharacterRuntimeBootstrap] Missing Resources prefab: {resourcePath}");
                return;
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.name = fallbackName;
        }
    }
}
