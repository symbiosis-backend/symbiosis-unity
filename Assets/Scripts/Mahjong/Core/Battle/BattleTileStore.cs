using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [Serializable]
    public sealed class BattleRoundConfig
    {
        [Min(1)] public int RoundIndex = 1;
        [Min(2)] public int TilesToUse = 30;
        [Min(1)] public int LayoutLevel = 1;
    }

    [DisallowMultipleComponent]
    public sealed class BattleTileStore : MonoBehaviour
    {
        public static BattleTileStore I { get; private set; }

        [Header("Battle Tile Pool")]
        [SerializeField] private List<BattleTileData> battleTiles = new();

        [Header("Battle Match Config")]
        [SerializeField, Min(1)] private int totalRounds = 3;
        [SerializeField] private List<BattleRoundConfig> roundConfigs = new()
        {
            new BattleRoundConfig { RoundIndex = 1, TilesToUse = 30, LayoutLevel = 1 },
            new BattleRoundConfig { RoundIndex = 2, TilesToUse = 30, LayoutLevel = 2 },
            new BattleRoundConfig { RoundIndex = 3, TilesToUse = 30, LayoutLevel = 3 }
        };

        public IReadOnlyList<BattleTileData> BattleTiles => battleTiles;
        public int TotalRounds => Mathf.Max(1, totalRounds);
        public IReadOnlyList<BattleRoundConfig> RoundConfigs => roundConfigs;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
        }

        public BattleRoundConfig GetRoundConfig(int roundIndex)
        {
            if (roundConfigs != null)
            {
                for (int i = 0; i < roundConfigs.Count; i++)
                {
                    BattleRoundConfig cfg = roundConfigs[i];
                    if (cfg != null && cfg.RoundIndex == roundIndex)
                        return cfg;
                }
            }

            return new BattleRoundConfig
            {
                RoundIndex = Mathf.Max(1, roundIndex),
                TilesToUse = 30,
                LayoutLevel = Mathf.Max(1, roundIndex)
            };
        }

        public IReadOnlyList<BattleTileData> GetTilesForRound(int roundIndex)
        {
            BattleRoundConfig cfg = GetRoundConfig(roundIndex);
            int targetCount = Mathf.Max(2, cfg.TilesToUse);

            List<BattleTileData> result = new();
            if (battleTiles == null || battleTiles.Count == 0)
                return result;

            List<BattleTileData> validPool = new();
            for (int i = 0; i < battleTiles.Count; i++)
            {
                BattleTileData t = battleTiles[i];
                if (t != null && t.Prefab != null && !string.IsNullOrWhiteSpace(t.Id))
                    validPool.Add(t);
            }

            if (validPool.Count == 0)
                return result;

            while (result.Count < targetCount)
            {
                for (int i = 0; i < validPool.Count && result.Count < targetCount; i++)
                    result.Add(validPool[i]);
            }

            if ((result.Count & 1) != 0)
                result.RemoveAt(result.Count - 1);

            return result;
        }

        public int GetLayoutLevelForRound(int roundIndex)
        {
            BattleRoundConfig cfg = GetRoundConfig(roundIndex);
            return Mathf.Max(1, cfg.LayoutLevel);
        }

        public int GetTilesToUseForRound(int roundIndex)
        {
            BattleRoundConfig cfg = GetRoundConfig(roundIndex);
            return Mathf.Max(2, cfg.TilesToUse);
        }

        public bool TryGetTileDataById(string id, out BattleTileData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(id) || battleTiles == null)
                return false;

            for (int i = 0; i < battleTiles.Count; i++)
            {
                BattleTileData item = battleTiles[i];
                if (item == null)
                    continue;

                if (string.Equals(item.Id, id, StringComparison.Ordinal))
                {
                    data = item;
                    return true;
                }
            }

            return false;
        }
    }
}
