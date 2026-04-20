using UnityEditor;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    [CustomEditor(typeof(TileStore))]
    public sealed class TileStoreEditor : UnityEditor.Editor
    {
        private SerializedProperty baseTilesProp;
        private SerializedProperty levelPacksProp;

        private int tabIndex;

        private void OnEnable()
        {
            baseTilesProp = serializedObject.FindProperty("baseTiles");
            levelPacksProp = serializedObject.FindProperty("levelPacks");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(4);
            tabIndex = GUILayout.Toolbar(tabIndex, new[] { "Base Tiles", "Level Packs", "Debug" });
            GUILayout.Space(8);

            switch (tabIndex)
            {
                case 0:
                    DrawBaseTilesTab();
                    break;

                case 1:
                    DrawLevelPacksTab();
                    break;

                case 2:
                    DrawDebugTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBaseTilesTab()
        {
            EditorGUILayout.HelpBox(
                "Сюда добавляй fallback-камни. Они используются, если у уровня нет собственного набора.",
                MessageType.Info);

            EditorGUILayout.PropertyField(baseTilesProp, true);
        }

        private void DrawLevelPacksTab()
        {
            EditorGUILayout.HelpBox(
                "1 пак = 1 уровень.\n\n" +
                "Внутри пака лежат:\n" +
                "- музыка уровня\n" +
                "- камни уровня\n" +
                "- 10 раскладок уровня\n" +
                "- у каждой раскладки свой фон и свой текст",
                MessageType.Info);

            EditorGUILayout.PropertyField(levelPacksProp, true);
        }

        private void DrawDebugTab()
        {
            TileStore store = (TileStore)target;

            EditorGUILayout.LabelField("Base Tiles Count", store.BaseTiles != null ? store.BaseTiles.Count.ToString() : "0");
            EditorGUILayout.LabelField("Level Packs Count", store.LevelPacks != null ? store.LevelPacks.Count.ToString() : "0");

            GUILayout.Space(6);

            if (store.LevelPacks != null)
            {
                for (int i = 0; i < store.LevelPacks.Count; i++)
                {
                    LevelPack pack = store.LevelPacks[i];
                    if (pack == null)
                        continue;

                    int tileCount = pack.Tiles != null ? pack.Tiles.Count : 0;
                    int stageCount = pack.Stages != null ? pack.Stages.Count : 0;

                    EditorGUILayout.LabelField(
                        $"Level {pack.LevelNumber} - {pack.DisplayName}",
                        $"Tiles: {tileCount} | Stages: {stageCount}"
                    );
                }
            }
        }
    }
}
