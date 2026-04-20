using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame.Clusters
{
    public static class ClusterService
    {
        public const string MatrixId = "matrix";
        public const string SlumsId = "slums";
        public const string MatrixSceneName = "ClusterMatrix";
        public const string SlumsSceneName = "ClusterSlums";

        private static readonly Dictionary<string, ClusterDefinition> Clusters = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                MatrixId,
                new ClusterDefinition
                {
                    id = MatrixId,
                    displayName = "Matrix",
                    sceneName = MatrixSceneName,
                    description = "Primary entry cluster and first online hub.",
                    connectedClusterIds = new[] { SlumsId }
                }
            },
            {
                SlumsId,
                new ClusterDefinition
                {
                    id = SlumsId,
                    displayName = "Slums",
                    sceneName = SlumsSceneName,
                    description = "First connected location inside the Matrix route.",
                    connectedClusterIds = new[] { MatrixId }
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

        public static void EnterMatrix()
        {
            LoadCluster(MatrixId);
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
