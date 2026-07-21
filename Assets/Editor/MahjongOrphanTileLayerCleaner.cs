using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame.EditorTools
{
    [InitializeOnLoad]
    internal static class MahjongOrphanTileLayerCleaner
    {
        static MahjongOrphanTileLayerCleaner()
        {
            EditorApplication.delayCall += CleanupOpenScenes;
        }

        private static void CleanupOpenScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                bool changed = false;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int j = roots.Length - 1; j >= 0; j--)
                {
                    GameObject root = roots[j];
                    if (!IsGeneratedTileLayerRoot(root))
                        continue;

                    Object.DestroyImmediate(root);
                    changed = true;
                }

                if (changed)
                    EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static bool IsGeneratedTileLayerRoot(GameObject root)
        {
            if (root == null)
                return false;

            if (root.transform.parent != null)
                return false;

            if (root.name != "DropShadow" && root.name != "DepthBody")
                return false;

            if (root.transform.childCount != 0)
                return false;

            return root.GetComponent<RectTransform>() != null
                && root.GetComponent<CanvasRenderer>() != null
                && root.GetComponent<Image>() != null;
        }
    }
}
