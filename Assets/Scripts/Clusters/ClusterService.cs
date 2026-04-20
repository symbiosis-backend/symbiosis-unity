using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame.Clusters
{
    public static class ClusterService
    {
        public const string ElysiumId = "elysium";
        public const string MatrixId = ElysiumId;
        public const string SlumsId = "slums";
        public const string ElysiumSceneName = "ClusterElysium";
        public const string MatrixSceneName = ElysiumSceneName;
        public const string SlumsSceneName = "ClusterSlums";

        private static readonly Dictionary<string, ClusterDefinition> Clusters = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                ElysiumId,
                new ClusterDefinition
                {
                    id = ElysiumId,
                    displayName = "ClusterElysium",
                    sceneName = ElysiumSceneName,
                    description = "Soft natural entry cluster and first online gathering place.",
                    connectedClusterIds = new[] { SlumsId }
                }
            },
            {
                SlumsId,
                new ClusterDefinition
                {
                    id = SlumsId,
                    displayName = "ClusterSlums",
                    sceneName = SlumsSceneName,
                    description = "Independent connected district with rougher streets and return routes.",
                    connectedClusterIds = new[] { ElysiumId }
                }
            }
        };

        public static IReadOnlyCollection<ClusterDefinition> All => Clusters.Values;

        public static bool TryGet(string id, out ClusterDefinition cluster)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                cluster = null;
                return false;
            }

            return Clusters.TryGetValue(id, out cluster);
        }

        public static string TryGetSceneClusterId(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return string.Empty;

            foreach (ClusterDefinition cluster in Clusters.Values)
            {
                if (string.Equals(cluster.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                    return cluster.id;
            }

            return string.Empty;
        }

        public static void EnterElysium()
        {
            LoadCluster(ElysiumId);
        }

        public static void EnterMatrix()
        {
            EnterElysium();
        }

        public static void LoadCluster(string id)
        {
            if (!TryGet(id, out ClusterDefinition cluster))
            {
                Debug.LogWarning($"[ClusterService] Unknown cluster id '{id}'.");
                return;
            }

            LoadScene(cluster.sceneName);
        }

        public static void ReturnToMain()
        {
            LoadScene("Main");
        }

        private static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[ClusterService] Scene name is empty.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[ClusterService] Scene '{sceneName}' is not in Build Settings.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
