using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OkeyDiscardPile : MonoBehaviour, IDropHandler
    {
        [Header("Setup")]
        [Range(0, 3)] public int OwnerSeatIndex = 0;
        public OkeyTurnManager TurnManager;
        public RectTransform VisualRoot;

        [Header("Runtime (Read Only)")]
        [SerializeField] private OkeyTileInstance topTile;

        private readonly List<OkeyTileInstance> pileTiles = new List<OkeyTileInstance>();
        private Image hitAreaImage;

        public OkeyTileInstance TopTile => topTile;
        public bool HasTile => topTile != null;

        private void Awake()
        {
            AutoResolve();
            EnsureHitArea();
            EnsurePassiveLinks();
            RefreshTopVisual();
        }

        private void OnEnable()
        {
            AutoResolve();
            EnsureHitArea();
            EnsurePassiveLinks();
            RefreshTopVisual();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoResolveEditorSafe();
            EnsureHitAreaEditorSafe();
            EnsurePassiveLinksEditorSafe();
        }
#endif

        private void AutoResolve()
        {
            if (VisualRoot == null)
                VisualRoot = transform as RectTransform;

            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<OkeyTurnManager>();
        }

#if UNITY_EDITOR
        private void AutoResolveEditorSafe()
        {
            if (VisualRoot == null)
                VisualRoot = transform as RectTransform;

            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<OkeyTurnManager>();
        }
#endif

        private void EnsureHitArea()
        {
            hitAreaImage = GetComponent<Image>();
            if (hitAreaImage == null)
                hitAreaImage = gameObject.AddComponent<Image>();

            Color c = hitAreaImage.color;
            c.a = 0f;
            hitAreaImage.color = c;
            hitAreaImage.raycastTarget = true;
        }

#if UNITY_EDITOR
        private void EnsureHitAreaEditorSafe()
        {
            Image img = GetComponent<Image>();
            if (img == null)
                return;

            Color c = img.color;
            c.a = 0f;
            img.color = c;
            img.raycastTarget = true;
        }
#endif

        private void EnsurePassiveLinks()
        {
            OkeyDiscardPileDragSource dragSource = GetComponent<OkeyDiscardPileDragSource>();
            if (dragSource == null)
                dragSource = gameObject.AddComponent<OkeyDiscardPileDragSource>();

            if (dragSource.DiscardPile == null)
                dragSource.DiscardPile = this;

            if (dragSource.TurnManager == null)
                dragSource.TurnManager = TurnManager != null
                    ? TurnManager
                    : FindAnyObjectByType<OkeyTurnManager>();

            if (dragSource.RootCanvas == null)
                dragSource.RootCanvas = GetComponentInParent<Canvas>();

            if (dragSource.RootCanvas == null)
                dragSource.RootCanvas = FindAnyObjectByType<Canvas>();
        }

#if UNITY_EDITOR
        private void EnsurePassiveLinksEditorSafe()
        {
            OkeyDiscardPileDragSource dragSource = GetComponent<OkeyDiscardPileDragSource>();
            if (dragSource == null)
                return;

            if (dragSource.DiscardPile == null)
                dragSource.DiscardPile = this;

            if (dragSource.TurnManager == null)
                dragSource.TurnManager = TurnManager != null
                    ? TurnManager
                    : FindAnyObjectByType<OkeyTurnManager>();

            if (dragSource.RootCanvas == null)
                dragSource.RootCanvas = GetComponentInParent<Canvas>();

            if (dragSource.RootCanvas == null)
                dragSource.RootCanvas = FindAnyObjectByType<Canvas>();
        }
#endif

        public void OnDrop(PointerEventData eventData)
        {
            AutoResolve();

            if (TurnManager == null)
                return;

            if (TurnManager.WinController != null && TurnManager.WinController.GameEnded)
                return;

            if (!TurnManager.IsLocalPlayersTurn())
                return;

            if (OwnerSeatIndex != TurnManager.LocalSeatIndex)
                return;

            if (TurnManager.CurrentPhase != OkeyTurnManager.TurnPhase.MustDiscard)
                return;

            if (eventData == null || eventData.pointerDrag == null)
                return;

            OkeyTileInstance tile = eventData.pointerDrag.GetComponent<OkeyTileInstance>();
            if (tile == null)
                tile = eventData.pointerDrag.GetComponentInParent<OkeyTileInstance>();

            if (tile == null)
                return;

            if (TurnManager.LocalSeat == null)
                return;

            if (!TurnManager.LocalSeat.ContainsTile(tile))
                return;

            bool success = TurnManager.TryDiscardForLocalPlayer(tile);
            if (!success)
                return;

            OkeyTileDrag drag = eventData.pointerDrag.GetComponent<OkeyTileDrag>();
            if (drag == null)
                drag = eventData.pointerDrag.GetComponentInParent<OkeyTileDrag>();

            if (drag != null)
                drag.MarkDropHandled();
        }

        public bool IsTopTile(OkeyTileInstance tile)
        {
            return tile != null && topTile == tile;
        }

        public OkeyTileInstance PeekTopTile()
        {
            return topTile;
        }

        public List<OkeyTileInstance> GetDiscardedTilesSnapshot()
        {
            List<OkeyTileInstance> result = new List<OkeyTileInstance>(pileTiles.Count);

            for (int i = 0; i < pileTiles.Count; i++)
            {
                if (pileTiles[i] != null)
                    result.Add(pileTiles[i]);
            }

            return result;
        }

        public void PlaceDiscardedTile(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            if (VisualRoot == null)
                VisualRoot = transform as RectTransform;

            if (VisualRoot == null)
                return;

            if (pileTiles.Contains(tile))
                pileTiles.Remove(tile);

            pileTiles.Add(tile);
            topTile = tile;

            RefreshTopVisual();
        }

        public OkeyTileInstance TakeTopTile()
        {
            if (topTile == null)
                return null;

            OkeyTileInstance tile = topTile;

            pileTiles.Remove(tile);
            topTile = pileTiles.Count > 0 ? pileTiles[pileTiles.Count - 1] : null;

            tile.transform.SetParent(null, false);
            MakeTileInteractiveOutsideDiscard(tile);

            RefreshTopVisual();
            return tile;
        }

        public void RestoreTopTile(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            PlaceDiscardedTile(tile);
        }

        public void ClearPile()
        {
            for (int i = 0; i < pileTiles.Count; i++)
            {
                if (pileTiles[i] == null)
                    continue;

                pileTiles[i].gameObject.SetActive(false);
                pileTiles[i].transform.SetParent(null, false);
            }

            pileTiles.Clear();
            topTile = null;
        }

        private void RefreshTopVisual()
        {
            if (VisualRoot == null)
                return;

            for (int i = 0; i < pileTiles.Count; i++)
            {
                OkeyTileInstance tile = pileTiles[i];
                if (tile == null)
                    continue;

                bool isTop = tile == topTile;

                tile.transform.SetParent(VisualRoot, false);
                tile.gameObject.SetActive(isTop);

                if (!isTop)
                    continue;

                ApplyDiscardRect(tile);
                MakeTileVisualOnlyInDiscard(tile);
            }
        }

        private void ApplyDiscardRect(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            tile.ShowFront();
            tile.RefreshVisuals();

            if (tile.transform is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }

            tile.transform.SetAsLastSibling();
        }

        private void MakeTileVisualOnlyInDiscard(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            OkeyTileDrag tileDrag = tile.GetComponent<OkeyTileDrag>();
            if (tileDrag != null)
                tileDrag.enabled = false;

            Graphic[] graphics = tile.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].raycastTarget = false;
            }

            CanvasGroup cg = tile.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = tile.gameObject.AddComponent<CanvasGroup>();

            cg.blocksRaycasts = false;
            cg.interactable = false;
            cg.alpha = 1f;
        }

        private void MakeTileInteractiveOutsideDiscard(OkeyTileInstance tile)
        {
            if (tile == null)
                return;

            OkeyTileDrag tileDrag = tile.GetComponent<OkeyTileDrag>();
            if (tileDrag != null)
                tileDrag.enabled = true;

            Graphic[] graphics = tile.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].raycastTarget = true;
            }

            CanvasGroup cg = tile.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = tile.gameObject.AddComponent<CanvasGroup>();

            cg.blocksRaycasts = true;
            cg.interactable = true;
            cg.alpha = 1f;
        }
    }
}