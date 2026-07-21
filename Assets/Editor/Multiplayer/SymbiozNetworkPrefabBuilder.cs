using System.IO;
using Dynasty.Legacy.Symbioz;
using FishNet.Object;
using UnityEditor;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    public static class SymbiozNetworkPrefabBuilder
    {
        private const string PrefabDirectory = "Assets/Resources/Network";
        private const string PrefabPath = PrefabDirectory + "/MatrixNetworkPlayer.prefab";
        private const ulong RuntimeMatrixPlayerAssetPathHash = 0xD15F1A6B51D00001UL;

        [MenuItem("Symbiosis/Network/Rebuild Matrix Player Prefab")]
        public static void RebuildMatrixPlayerPrefab()
        {
            Directory.CreateDirectory(PrefabDirectory);

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "MatrixNetworkPlayer";
            root.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Collider collider = root.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            NetworkObject networkObject = root.AddComponent<NetworkObject>();
            networkObject.SetAssetPathHash(RuntimeMatrixPlayerAssetPathHash);
            root.AddComponent<SymbiozNetworkPawn>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SymbiozNetworkPrefabBuilder] Rebuilt FishNet player prefab at " + PrefabPath);
        }
    }
}
