using System.Collections.Generic;
using UnityEngine;

namespace OkeyGame
{
    public sealed class OkeyTableModule : MonoBehaviour
    {
        [Header("Tile Prefabs (Unique 54)")]
        public List<OkeyTileInstance> uniqueTilePrefabs = new();

        [Header("Rules")]
        [Range(1, 4)] public int copiesPerNormalTile = 2;
        [Range(0, 4)] public int jokerCount = 2;

        [Header("Runtime (Read Only)")]
        [SerializeField] private List<OkeyTileInstance> deck = new();
        [SerializeField] private OkeyTileInstance indicatorTile;
        [SerializeField] private TileColor okeyColor = TileColor.Black;
        [SerializeField] private int okeyNumber = 1;
        [SerializeField] private TileColor fakeOkeyActsAsColor = TileColor.Black;
        [SerializeField] private int fakeOkeyActsAsNumber = 1;
        [SerializeField] private bool hasIndicator = false;

        public int DeckCount => deck.Count;
        public OkeyTileInstance IndicatorTile => indicatorTile;
        public TileColor OkeyColor => okeyColor;
        public int OkeyNumber => okeyNumber;
        public TileColor FakeOkeyActsAsColor => fakeOkeyActsAsColor;
        public int FakeOkeyActsAsNumber => fakeOkeyActsAsNumber;
        public bool HasIndicator => hasIndicator;

        public void GenerateFullDeck(bool shuffle = true)
        {
            BuildDeck(shuffle);
        }

        public void BuildDeck(bool shuffle = true)
        {
            ClearOldRuntimeTiles();

            deck.Clear();
            indicatorTile = null;
            hasIndicator = false;
            okeyColor = TileColor.Black;
            okeyNumber = 1;
            fakeOkeyActsAsColor = TileColor.Black;
            fakeOkeyActsAsNumber = 1;

            if (uniqueTilePrefabs == null || uniqueTilePrefabs.Count == 0)
            {
                Debug.LogError("[OkeyTable] uniqueTilePrefabs is empty. Add tile prefabs in Inspector.");
                return;
            }

            List<OkeyTileInstance> normalPrefabs = new();
            List<OkeyTileInstance> jokerPrefabs = new();

            for (int i = 0; i < uniqueTilePrefabs.Count; i++)
            {
                OkeyTileInstance prefab = uniqueTilePrefabs[i];
                if (prefab == null)
                    continue;

                if (prefab.IsJoker || prefab.Color == TileColor.Joker)
                    jokerPrefabs.Add(prefab);
                else
                    normalPrefabs.Add(prefab);
            }

            int runtimeId = 0;

            for (int i = 0; i < normalPrefabs.Count; i++)
            {
                OkeyTileInstance prefab = normalPrefabs[i];

                for (int copy = 0; copy < copiesPerNormalTile; copy++)
                {
                    OkeyTileInstance inst = Instantiate(prefab, transform);
                    inst.name = $"{prefab.Color}-{prefab.Number}-copy{copy + 1}";
                    inst.SetRuntimeId(runtimeId++);
                    inst.SetFaceVisible(false);
                    inst.gameObject.SetActive(false);
                    deck.Add(inst);
                }
            }

            for (int j = 0; j < jokerCount; j++)
            {
                if (jokerPrefabs.Count == 0)
                {
                    Debug.LogWarning("[OkeyTable] jokerCount > 0, but no Joker prefabs provided.");
                    break;
                }

                OkeyTileInstance prefab = jokerPrefabs[j % jokerPrefabs.Count];
                OkeyTileInstance inst = Instantiate(prefab, transform);
                inst.name = $"Joker-{j + 1}";
                inst.SetRuntimeId(runtimeId++);
                inst.SetFaceVisible(false);
                inst.gameObject.SetActive(false);
                deck.Add(inst);
            }

            if (shuffle)
                ShuffleDeck();

            DetermineIndicatorAndOkey();
            SyncHierarchyToDeckOrder();

            Debug.Log($"[OkeyTable] Deck built. Total={deck.Count}");
        }

        public void ShuffleDeck()
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                (deck[i], deck[r]) = (deck[r], deck[i]);
            }

            Debug.Log("[OkeyTable] Deck shuffled.");
        }

        public void DetermineIndicatorAndOkey()
        {
            indicatorTile = null;
            hasIndicator = false;

            if (deck.Count == 0)
            {
                Debug.LogError("[OkeyTable] Cannot determine indicator. Deck is empty.");
                return;
            }

            for (int i = deck.Count - 1; i >= 0; i--)
            {
                OkeyTileInstance candidate = deck[i];
                if (candidate == null)
                    continue;

                if (candidate.IsJoker)
                    continue;

                if (candidate.Color == TileColor.Joker)
                    continue;

                indicatorTile = candidate;
                deck.RemoveAt(i);
                break;
            }

            if (indicatorTile == null)
            {
                Debug.LogError("[OkeyTable] Failed to find a valid indicator tile.");
                return;
            }

            hasIndicator = true;

            indicatorTile.gameObject.SetActive(true);
            indicatorTile.ShowFront();

            okeyColor = indicatorTile.Color;
            okeyNumber = GetNextNumber(indicatorTile.Number);

            fakeOkeyActsAsColor = indicatorTile.Color;
            fakeOkeyActsAsNumber = indicatorTile.Number;

            Debug.Log($"[OkeyTable] Indicator = {indicatorTile.Color}-{indicatorTile.Number}");
            Debug.Log($"[OkeyTable] Real Okey = {okeyColor}-{okeyNumber}");
            Debug.Log($"[OkeyTable] Fake Okey acts as = {fakeOkeyActsAsColor}-{fakeOkeyActsAsNumber}");
        }

        private int GetNextNumber(int number)
        {
            number++;
            return number > 13 ? 1 : number;
        }

        public bool IsCurrentRoundOkey(OkeyTileInstance tile)
        {
            if (!hasIndicator || tile == null)
                return false;

            if (tile.IsJoker)
                return false;

            if (tile.Color == TileColor.Joker)
                return false;

            return tile.Color == okeyColor && tile.Number == okeyNumber;
        }

        public bool IsFakeOkey(OkeyTileInstance tile)
        {
            if (!hasIndicator || tile == null)
                return false;

            return tile.IsJoker || tile.Color == TileColor.Joker;
        }

        public bool ShouldRenderFaceDown(OkeyTileInstance tile)
        {
            return IsCurrentRoundOkey(tile);
        }

        public void ApplySpecialVisualState(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            if (ShouldRenderFaceDown(tile))
                tile.ShowBack();
            else
                tile.ShowFront();

            tile.RefreshVisuals();
        }

        public void ApplyHandOrBoardVisual(OkeyTileInstance tile)
        {
            ApplySpecialVisualState(tile);
        }

        public TileColor GetEffectiveColor(OkeyTileInstance tile)
        {
            if (tile == null)
                return TileColor.Black;

            return IsFakeOkey(tile) ? fakeOkeyActsAsColor : tile.Color;
        }

        public int GetEffectiveNumber(OkeyTileInstance tile)
        {
            if (tile == null)
                return 1;

            return IsFakeOkey(tile) ? fakeOkeyActsAsNumber : tile.Number;
        }

        public OkeyTileInstance DrawFromTop()
        {
            if (deck.Count == 0)
                return null;

            OkeyTileInstance tile = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            return tile;
        }

        public void ReturnToBottom(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            if (tile == indicatorTile)
            {
                Debug.LogWarning("[OkeyTable] Tried to return indicator tile back to deck. Ignored.");
                return;
            }

            tile.SetFaceVisible(false);

            if (!deck.Contains(tile))
                deck.Add(tile);

            SyncHierarchyToDeckOrder();
        }

        private void SyncHierarchyToDeckOrder()
        {
            for (int i = 0; i < deck.Count; i++)
            {
                OkeyTileInstance tile = deck[i];
                if (tile == null)
                    continue;

                // ВАЖНО:
                // Не меняем parent у обычных камней deck.
                // OkeyDealer уже раскладывает остаток колоды в DrawPileRoot,
                // и это размещение должно сохраняться.
                tile.transform.SetSiblingIndex(i);
            }

            if (indicatorTile != null)
            {
                if (indicatorTile.transform.parent != transform)
                    indicatorTile.transform.SetParent(transform, false);

                indicatorTile.transform.SetAsLastSibling();
            }
        }

        private void ClearOldRuntimeTiles()
        {
            List<GameObject> toDestroy = new();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                    toDestroy.Add(child.gameObject);
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(toDestroy[i]);
                else Destroy(toDestroy[i]);
#else
                Destroy(toDestroy[i]);
#endif
            }
        }
    }
}