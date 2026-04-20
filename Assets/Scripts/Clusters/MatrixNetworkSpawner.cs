using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace MahjongGame.Clusters
{
    [DisallowMultipleComponent]
    public sealed class MatrixNetworkSpawner : MonoBehaviour
    {
        private readonly Dictionary<int, NetworkObject> spawnedPlayers = new();

        private NetworkManager networkManager;
        private NetworkObject playerPrefab;

        public void Configure(NetworkManager manager, NetworkObject prefab)
        {
            networkManager = manager;
            playerPrefab = prefab;
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (networkManager != null && networkManager.SceneManager != null)
                networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        }

        private void TrySubscribe()
        {
            if (networkManager == null)
                networkManager = GetComponent<NetworkManager>();

            if (networkManager == null || networkManager.SceneManager == null)
                return;

            networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
            networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
        }

        private void OnClientLoadedStartScenes(NetworkConnection connection, bool asServer)
        {
            if (!asServer || connection == null || !connection.IsValid)
                return;

            if (playerPrefab == null)
            {
                Debug.LogWarning("[MatrixNetworkSpawner] Matrix player prefab is not assigned.");
                return;
            }

            if (spawnedPlayers.ContainsKey(connection.ClientId))
                return;

            NetworkObject player = networkManager.GetPooledInstantiated(playerPrefab, true);
            player.transform.position = GetSpawnPosition(connection.ClientId);
            networkManager.ServerManager.Spawn(player, connection);
            networkManager.SceneManager.AddOwnerToDefaultScene(player);
            spawnedPlayers[connection.ClientId] = player;
        }

        private static Vector3 GetSpawnPosition(int clientId)
        {
            int slot = Mathf.Abs(clientId) % 8;
            float angle = slot / 8f * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(angle) * 2.2f, Mathf.Sin(angle) * 1.4f, 0f);
        }
    }
}
