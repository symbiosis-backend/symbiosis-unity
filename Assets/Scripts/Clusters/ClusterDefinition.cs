using System;

namespace MahjongGame.Clusters
{
    [Serializable]
    public sealed class ClusterDefinition
    {
        public string id;
        public string displayName;
        public string sceneName;
        public string description;
        public string[] connectedClusterIds;
    }
}
