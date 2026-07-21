using System.Collections;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Players;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Core
{
    public sealed class TileEffectResolver
    {
        public IEnumerator Resolve(PlayerState player, PlayerView view, BoardMover mover, CorrosionWaveSystem waveSystem)
        {
            if (player?.currentNode == null || view == null || mover == null)
            {
                yield break;
            }

            Debug.Log($"[Tile] {player.currentNode.type} activated by {player.nickname}");
            switch (player.currentNode.type)
            {
                case TileType.Purple:
                    yield return mover.MoveBySteps(player, view, 10, SelectMainPath);
                    Debug.Log("[Tile] Purple: Player moved +10 cells.");
                    break;
                case TileType.Yellow:
                    player.hasShortcutPass = true;
                    Debug.Log($"[Tile] Yellow Shortcut Pass granted to {player.nickname}");
                    Debug.Log("[Tile] Yellow: Shortcut Pass granted.");
                    break;
                case TileType.Green:
                    if (!player.extraRollUsedThisTurn)
                    {
                        player.extraRollAvailable = true;
                        Debug.Log($"[Tile] Green Extra Roll granted to {player.nickname}");
                        Debug.Log("[Tile] Green: Extra roll granted.");
                    }
                    break;
                case TileType.Red:
                    yield return ResolveRed(player, view, mover);
                    break;
                case TileType.BlackRed:
                    if (waveSystem != null)
                    {
                        yield return waveSystem.Trigger(player.currentNode);
                    }
                    break;
                case TileType.Safe:
                    Debug.Log("[Tile] Safe: Player is protected here.");
                    break;
                default:
                    Debug.Log("[Tile] Normal");
                    break;
            }
        }

        private static IEnumerator ResolveRed(PlayerState player, PlayerView view, BoardMover mover)
        {
            if (Random.Range(0, 2) == 0)
            {
                int stepsBack = Random.Range(7, 11);
                Debug.Log($"[Tile] Red penalty: move back {stepsBack}");
                Debug.Log($"[Tile] Red: Player moved back {stepsBack} cells.");
                yield return mover.MoveBackBySteps(player, view, stepsBack);
            }
            else
            {
                player.skipNextTurn = true;
                Debug.Log("[Tile] Red: Player will skip next turn.");
            }
        }

        private static BoardNode SelectMainPath(PlayerState player, BoardNode current)
        {
            BoardNode selected = null;
            foreach (BoardNode next in current.nextNodes)
            {
                if (next.isShortcut || !next.IsTraversable)
                {
                    continue;
                }

                if (selected == null || next.progressIndex < selected.progressIndex)
                {
                    selected = next;
                }
            }

            return selected;
        }
    }
}
