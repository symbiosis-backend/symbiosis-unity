using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeyBotController : MonoBehaviour
    {
        private enum BotStrategy
        {
            Normal = 0,
            Cifte = 1
        }

        [Header("Timing")]
        public float DrawDelay = 0.6f;
        public float DiscardDelay = 0.8f;
        public float PostDrawStabilizeDelay = 0.1f;
        public float ExtraPhaseRecoveryDelay = 0.15f;

        [Header("Behavior")]
        public bool CanTakeFromPreviousDiscard = true;
        [Range(0, 20)] public int MinCiftePreferenceScore = 8;
        [Range(1, 50)] public int MinDiscardTakeGainNormal = 12;
        [Range(1, 50)] public int MinDiscardTakeGainCifte = 16;
        [Range(0, 10)] public int ImmediateUsefulTakeBonus = 2;

        private OkeyPlayerSeat seat;
        private Coroutine playRoutine;

        private void Awake()
        {
            seat = GetComponent<OkeyPlayerSeat>();
        }

        public void StartBotTurn(OkeyTurnManager turnManager)
        {
            if (seat == null || turnManager == null)
                return;

            if (turnManager.WinController != null && turnManager.WinController.GameEnded)
                return;

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(PlayRoutine(turnManager));
        }

        private IEnumerator PlayRoutine(OkeyTurnManager turnManager)
        {
            if (seat == null || turnManager == null)
            {
                playRoutine = null;
                yield break;
            }

            if (turnManager.WinController != null && turnManager.WinController.GameEnded)
            {
                playRoutine = null;
                yield break;
            }

            if (turnManager.CurrentSeatIndex != seat.SeatIndex)
            {
                Debug.LogWarning($"[Bot P{seat.SeatIndex}] Not my turn.");
                playRoutine = null;
                yield break;
            }

            BotStrategy strategy = ChooseStrategy(turnManager.Table);

            if (turnManager.CurrentPhase == OkeyTurnManager.TurnPhase.MustDraw && seat.HandCount < 15)
            {
                yield return new WaitForSeconds(DrawDelay);

                if (turnManager.WinController != null && turnManager.WinController.GameEnded)
                {
                    playRoutine = null;
                    yield break;
                }

                bool drawOk = false;
                string drawSource = "NONE";

                if (CanTakeFromPreviousDiscard)
                {
                    OkeyDiscardPile previousPile = turnManager.GetPreviousSeatDiscardPile(seat.SeatIndex);
                    if (previousPile != null && previousPile.HasTile)
                    {
                        OkeyTileInstance discardTop = previousPile.PeekTopTile();
                        if (discardTop != null && ShouldTakePreviousDiscard(discardTop, turnManager.Table, strategy))
                        {
                            drawOk = turnManager.TryTakeTopDiscardForSeat(seat.SeatIndex, previousPile);
                            if (drawOk)
                                drawSource = $"DISCARD P{previousPile.OwnerSeatIndex}";
                        }
                    }
                }

                if (!drawOk)
                {
                    drawOk = turnManager.TryDrawForSeat(seat.SeatIndex);
                    if (drawOk)
                        drawSource = "DECK";
                }

                if (!drawOk)
                {
                    Debug.LogWarning($"[Bot P{seat.SeatIndex}] Draw failed.");
                    playRoutine = null;
                    yield break;
                }

                yield return null;

                if (PostDrawStabilizeDelay > 0f)
                    yield return new WaitForSeconds(PostDrawStabilizeDelay);

                strategy = ChooseStrategy(turnManager.Table);
                Debug.Log($"[Bot P{seat.SeatIndex}] Draw OK from {drawSource}. Strategy={strategy}. HandCount={seat.HandCount}");
            }

            if (turnManager.CurrentPhase != OkeyTurnManager.TurnPhase.MustDiscard)
            {
                if (seat.HandCount >= 15 && ExtraPhaseRecoveryDelay > 0f)
                    yield return new WaitForSeconds(ExtraPhaseRecoveryDelay);
            }

            bool canProceedToDiscard =
                seat.HandCount >= 15 ||
                turnManager.CurrentPhase == OkeyTurnManager.TurnPhase.MustDiscard;

            if (!canProceedToDiscard)
            {
                Debug.LogWarning($"[Bot P{seat.SeatIndex}] Cannot continue. Phase={turnManager.CurrentPhase}, HandCount={seat.HandCount}.");
                playRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(DiscardDelay);

            if (turnManager.WinController != null && turnManager.WinController.GameEnded)
            {
                playRoutine = null;
                yield break;
            }

            OkeyTileInstance tile = ChooseDiscardTile(turnManager.Table, strategy);
            if (tile == null)
            {
                Debug.LogWarning($"[Bot P{seat.SeatIndex}] No tile to discard. HandCount={seat.HandCount}");
                playRoutine = null;
                yield break;
            }

            bool discardOk = turnManager.TryDiscardForSeat(seat.SeatIndex, tile);

            if (!discardOk)
            {
                List<OkeyTileInstance> fallbackTiles = seat.Tiles
                    .Where(t => t != null && t != tile)
                    .OrderByDescending(t => ScoreDiscardCandidate(t, seat.Tiles.Where(x => x != null).ToList(), turnManager.Table, strategy))
                    .ToList();

                for (int i = 0; i < fallbackTiles.Count; i++)
                {
                    discardOk = turnManager.TryDiscardForSeat(seat.SeatIndex, fallbackTiles[i]);
                    if (discardOk)
                    {
                        Debug.Log($"[Bot P{seat.SeatIndex}] Fallback discard OK: {fallbackTiles[i].name}");
                        break;
                    }
                }
            }

            if (!discardOk)
                Debug.LogWarning($"[Bot P{seat.SeatIndex}] Discard failed. HandCount={seat.HandCount}, Phase={turnManager.CurrentPhase}");
            else
                Debug.Log($"[Bot P{seat.SeatIndex}] Discard OK. Strategy={strategy}");

            playRoutine = null;
        }

        private BotStrategy ChooseStrategy(OkeyTableModule table)
        {
            if (table == null || seat == null || seat.Tiles == null)
                return BotStrategy.Normal;

            List<OkeyTileInstance> tiles = seat.Tiles.Where(t => t != null).ToList();
            if (tiles.Count == 0)
                return BotStrategy.Normal;

            int cifteScore = EvaluateCiftePotential(tiles, table);
            int normalScore = EvaluateNormalPotential(tiles, table);

            if (cifteScore >= normalScore + MinCiftePreferenceScore)
                return BotStrategy.Cifte;

            return BotStrategy.Normal;
        }

        private bool ShouldTakePreviousDiscard(OkeyTileInstance discardTop, OkeyTableModule table, BotStrategy strategy)
        {
            if (discardTop == null || table == null || seat == null)
                return false;

            List<OkeyTileInstance> current = seat.Tiles.Where(t => t != null).ToList();
            int before = EvaluateHandPotential(current, table, strategy);

            List<OkeyTileInstance> withDiscard = new List<OkeyTileInstance>(current)
            {
                discardTop
            };

            int after = EvaluateHandPotential(withDiscard, table, strategy);
            int gain = after - before;

            if (strategy == BotStrategy.Cifte)
            {
                bool usefulPair = FormsOrImprovesPair(discardTop, current, table);
                if (!usefulPair)
                    return false;

                if (CreatesExactWinningPairPressure(discardTop, current, table))
                    return true;

                return gain >= MinDiscardTakeGainCifte;
            }

            bool usefulNormal = CreatesUsefulNormalConnection(discardTop, current, table);
            if (!usefulNormal)
                return false;

            if (CreatesImmediateGroup(discardTop, current, table))
                return true;

            return gain >= MinDiscardTakeGainNormal;
        }

        private OkeyTileInstance ChooseDiscardTile(OkeyTableModule table, BotStrategy strategy)
        {
            if (seat == null || seat.Tiles == null || seat.Tiles.Count == 0)
                return null;

            List<OkeyTileInstance> tiles = seat.Tiles.Where(t => t != null).ToList();
            if (tiles.Count == 0)
                return null;

            List<OkeyTileInstance> safeCandidates = tiles.Where(t => !IsWildTile(t, table)).ToList();
            if (safeCandidates.Count == 0)
                safeCandidates = tiles;

            OkeyTileInstance best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < safeCandidates.Count; i++)
            {
                OkeyTileInstance tile = safeCandidates[i];
                int score = ScoreDiscardCandidate(tile, tiles, table, strategy);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = tile;
                }
            }

            return best ?? safeCandidates[safeCandidates.Count - 1];
        }

        private int ScoreDiscardCandidate(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table, BotStrategy strategy)
        {
            if (tile == null)
                return int.MinValue;

            int score = 0;

            if (IsWildTile(tile, table))
                score -= 1000;

            int sameNumberCount = CountSameNumberUseful(tile, allTiles, table);
            int sameColorNearCount = CountSameColorNeighbors(tile, allTiles, table);
            bool exactPair = HasExactPair(tile, allTiles, table);
            bool inImmediateGroup = IsTileInsideImmediateGroup(tile, allTiles, table);

            if (strategy == BotStrategy.Cifte)
            {
                if (exactPair)
                    score -= 90;
                else
                    score += 24;

                if (sameNumberCount > 0)
                    score -= 8;

                if (sameColorNearCount > 0)
                    score -= 3;

                if (inImmediateGroup)
                    score -= 8;
            }
            else
            {
                if (sameNumberCount == 0)
                    score += 24;
                else if (sameNumberCount == 1)
                    score += 4;
                else
                    score -= 12;

                if (sameColorNearCount == 0)
                    score += 24;
                else if (sameColorNearCount == 1)
                    score += 4;
                else
                    score -= 16;

                if (exactPair)
                    score -= 10;

                if (inImmediateGroup)
                    score -= 24;
            }

            if (tile.Number == 1 || tile.Number == 13)
                score += 4;

            score += tile.RuntimeId % 3;
            return score;
        }

        private int EvaluateHandPotential(List<OkeyTileInstance> tiles, OkeyTableModule table, BotStrategy strategy)
        {
            return strategy == BotStrategy.Cifte
                ? EvaluateCiftePotential(tiles, table)
                : EvaluateNormalPotential(tiles, table);
        }

        private int EvaluateCiftePotential(List<OkeyTileInstance> tiles, OkeyTableModule table)
        {
            if (tiles == null || table == null)
                return 0;

            int wildCount = 0;
            Dictionary<string, int> counts = new Dictionary<string, int>();

            for (int i = 0; i < tiles.Count; i++)
            {
                OkeyTileInstance tile = tiles[i];
                if (tile == null)
                    continue;

                if (IsWildTile(tile, table))
                {
                    wildCount++;
                    continue;
                }

                string key = $"{tile.Color}_{tile.Number}";
                if (!counts.ContainsKey(key))
                    counts[key] = 0;

                counts[key]++;
            }

            int pairs = 0;
            int singles = 0;

            foreach (var kv in counts)
            {
                pairs += kv.Value / 2;
                if ((kv.Value % 2) == 1)
                    singles++;
            }

            return pairs * 20 + singles * 4 + wildCount * 10;
        }

        private int EvaluateNormalPotential(List<OkeyTileInstance> tiles, OkeyTableModule table)
        {
            if (tiles == null || table == null)
                return 0;

            int score = 0;

            for (int i = 0; i < tiles.Count; i++)
            {
                OkeyTileInstance tile = tiles[i];
                if (tile == null)
                    continue;

                if (IsWildTile(tile, table))
                {
                    score += 20;
                    continue;
                }

                int sameNumber = CountSameNumberUseful(tile, tiles, table);
                int sameColorNear = CountSameColorNeighbors(tile, tiles, table);

                score += sameNumber * 8;
                score += sameColorNear * 10;
            }

            OkeyHandAnalyzer.AnalysisResult analysis = OkeyHandAnalyzer.AnalyzeWithOkey(tiles, table);
            score += analysis.Groups.Count * 30;
            score -= analysis.Leftovers.Count * 3;

            return score;
        }

        private bool FormsOrImprovesPair(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table)
        {
            if (tile == null || table == null)
                return false;

            if (IsWildTile(tile, table))
                return true;

            for (int i = 0; i < allTiles.Count; i++)
            {
                OkeyTileInstance other = allTiles[i];
                if (other == null || other == tile)
                    continue;

                if (IsWildTile(other, table))
                    continue;

                if (other.Color == tile.Color && other.Number == tile.Number)
                    return true;
            }

            return false;
        }

        private bool CreatesExactWinningPairPressure(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table)
        {
            if (tile == null || table == null)
                return false;

            int exactCopies = 0;

            for (int i = 0; i < allTiles.Count; i++)
            {
                OkeyTileInstance other = allTiles[i];
                if (other == null)
                    continue;

                if (IsWildTile(other, table))
                    continue;

                if (other.Color == tile.Color && other.Number == tile.Number)
                    exactCopies++;
            }

            return exactCopies >= 1 + ImmediateUsefulTakeBonus - 1;
        }

        private bool CreatesUsefulNormalConnection(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table)
        {
            if (tile == null || table == null)
                return false;

            if (IsWildTile(tile, table))
                return true;

            if (CountSameNumberUseful(tile, allTiles, table) > 0)
                return true;

            if (CountSameColorNeighbors(tile, allTiles, table) > 0)
                return true;

            return false;
        }

        private bool CreatesImmediateGroup(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table)
        {
            if (tile == null || table == null)
                return false;

            List<OkeyTileInstance> test = new List<OkeyTileInstance>(allTiles)
            {
                tile
            };

            OkeyHandAnalyzer.AnalysisResult analysis = OkeyHandAnalyzer.AnalyzeWithOkey(test, table);
            if (analysis == null || analysis.Groups == null)
                return false;

            for (int i = 0; i < analysis.Groups.Count; i++)
            {
                if (analysis.Groups[i] != null && analysis.Groups[i].Tiles != null && analysis.Groups[i].Tiles.Contains(tile))
                    return true;
            }

            return false;
        }

        private bool IsTileInsideImmediateGroup(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table)
        {
            if (tile == null || table == null)
                return false;

            OkeyHandAnalyzer.AnalysisResult analysis = OkeyHandAnalyzer.AnalyzeWithOkey(allTiles, table);
            if (analysis == null || analysis.Groups == null)
                return false;

            for (int i = 0; i < analysis.Groups.Count; i++)
            {
                if (analysis.Groups[i] != null && analysis.Groups[i].Tiles != null && analysis.Groups[i].Tiles.Contains(tile))
                    return true;
            }

            return false;
        }

        private bool HasExactPair(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table)
        {
            if (tile == null)
                return false;

            for (int i = 0; i < allTiles.Count; i++)
            {
                OkeyTileInstance other = allTiles[i];
                if (other == null || other == tile)
                    continue;

                if (IsWildTile(other, table))
                    continue;

                if (other.Color == tile.Color && other.Number == tile.Number)
                    return true;
            }

            return false;
        }

        private int CountSameNumberUseful(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table)
        {
            int count = 0;

            for (int i = 0; i < allTiles.Count; i++)
            {
                OkeyTileInstance other = allTiles[i];
                if (other == null || other == tile)
                    continue;

                if (IsWildTile(other, table))
                    continue;

                if (other.Number == tile.Number && other.Color != tile.Color)
                    count++;
            }

            return count;
        }

        private int CountSameColorNeighbors(OkeyTileInstance tile, List<OkeyTileInstance> allTiles, OkeyTableModule table)
        {
            int count = 0;

            for (int i = 0; i < allTiles.Count; i++)
            {
                OkeyTileInstance other = allTiles[i];
                if (other == null || other == tile)
                    continue;

                if (IsWildTile(other, table))
                    continue;

                if (other.Color != tile.Color)
                    continue;

                int diff = Mathf.Abs(other.Number - tile.Number);
                if (diff == 1 || diff == 2)
                    count++;
            }

            return count;
        }

        private bool IsWildTile(OkeyTileInstance tile, OkeyTableModule table)
        {
            if (tile == null || table == null)
                return false;

            if (table.IsFakeOkey(tile))
                return true;

            if (table.IsCurrentRoundOkey(tile))
                return true;

            return false;
        }
    }
}