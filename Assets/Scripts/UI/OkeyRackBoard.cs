using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeyRackBoard : MonoBehaviour
    {
        public OkeyTurnManager TurnManager;
        public OkeyPlayerSeat OwnerSeat;
        [Min(1)] public int Columns = 16;
        [Min(1)] public int Rows = 2;
        public Vector2 CellSize = new(95f, 130f);
        public Vector2 Spacing = new(6f, 6f);
        public bool DebugSlotBackground = false;
        public RectTransform SlotGrid;

        private readonly List<RectTransform> slots = new(64);

        public int TotalSlots => Columns * Rows;
        public IReadOnlyList<RectTransform> Slots => slots;

        private void Awake()
        {
            EnsureBoard();
            EnsureSlotsCache();
            if (slots.Count != TotalSlots) RebuildSlots();
            else EnsureDropTargetsOnAllSlots();
        }

        private void LateUpdate()
        {
            ApplyLayoutGeometry();
            EnsureDropTargetsOnAllSlots();
        }

        public void EnsureBoard()
        {
            if (transform is not RectTransform boardRt)
            {
                Debug.LogError($"[RackBoard:{name}] RectTransform missing.");
                return;
            }

            boardRt.anchorMin = Vector2.zero;
            boardRt.anchorMax = Vector2.one;
            boardRt.pivot = new Vector2(.5f, .5f);
            boardRt.offsetMin = Vector2.zero;
            boardRt.offsetMax = Vector2.zero;
            boardRt.anchoredPosition = Vector2.zero;
            boardRt.localScale = Vector3.one;

            if (SlotGrid == null)
            {
                Transform found = transform.Find("SlotGrid");
                if (found != null) SlotGrid = found as RectTransform;
            }

            if (SlotGrid == null)
            {
                GameObject go = new("SlotGrid", typeof(RectTransform), typeof(GridLayoutGroup));
                go.transform.SetParent(transform, false);
                SlotGrid = go.GetComponent<RectTransform>();
            }

            ConfigureGrid();
            ApplyLayoutGeometry();
        }

        private void ConfigureGrid()
        {
            if (SlotGrid == null) return;

            GridLayoutGroup grid = SlotGrid.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = SlotGrid.gameObject.AddComponent<GridLayoutGroup>();

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.cellSize = CellSize;
            grid.spacing = Spacing;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(0, 0, 0, 0);
        }

        private void ApplyLayoutGeometry()
        {
            if (SlotGrid == null) return;

            float width = Columns * CellSize.x + (Columns - 1) * Spacing.x;
            float height = Rows * CellSize.y + (Rows - 1) * Spacing.y;

            SlotGrid.anchorMin = new Vector2(.5f, .5f);
            SlotGrid.anchorMax = new Vector2(.5f, .5f);
            SlotGrid.pivot = new Vector2(.5f, .5f);
            SlotGrid.sizeDelta = new Vector2(width, height);
            SlotGrid.anchoredPosition = Vector2.zero;
            SlotGrid.localScale = Vector3.one;
        }

        [ContextMenu("Rebuild Slots")]
        public void RebuildSlots()
        {
            EnsureBoard();
            if (SlotGrid == null) return;

            for (int i = SlotGrid.childCount - 1; i >= 0; i--)
            {
                Transform child = SlotGrid.GetChild(i);
                if (child != null && child.name.StartsWith("Slot_"))
                    DestroySafe(child.gameObject);
            }

            slots.Clear();

            for (int i = 0; i < TotalSlots; i++)
            {
                GameObject slotGO = new($"Slot_{i:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(OkeyRackSlot));
                slotGO.transform.SetParent(SlotGrid, false);

                RectTransform rt = slotGO.GetComponent<RectTransform>();
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(.5f, .5f);
                rt.anchorMax = new Vector2(.5f, .5f);
                rt.pivot = new Vector2(.5f, .5f);
                rt.sizeDelta = CellSize;

                SetupSlotVisual(slotGO.GetComponent<Image>());
                EnsureDropTarget(slotGO, rt);
                slots.Add(rt);
            }

            ConfigureGrid();
            ApplyLayoutGeometry();
        }

        public void ClearBoard()
        {
            EnsureSlotsCache();
            for (int i = 0; i < slots.Count; i++)
                ClearSlot(slots[i], true);
        }

        public bool PlaceTile(OkeyTileInstance tile)
        {
            if (tile == null) return false;
            EnsureSlotsCache();

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null || SlotHasTile(slots[i])) continue;
                PlaceIntoTransform(tile, slots[i]);
                return true;
            }

            return false;
        }

        public bool PlaceTileIntoSlot(OkeyTileInstance tile, Transform slotTransform)
        {
            if (tile == null || slotTransform is not RectTransform targetSlot) return false;
            EnsureSlotsCache();

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != targetSlot) continue;
                OkeyTileInstance existing = GetTileInSlot(targetSlot);
                if (existing != null && existing != tile) return false;
                PlaceIntoTransform(tile, targetSlot);
                return true;
            }

            return false;
        }

        public bool InsertTileIntoSlot(OkeyTileInstance tile, Transform slotTransform)
        {
            if (tile == null || slotTransform is not RectTransform targetSlot) return false;
            EnsureSlotsCache();

            int targetIndex = slots.IndexOf(targetSlot);
            if (targetIndex < 0) return false;

            int rowStart = GetRowStartIndex(targetIndex);
            OkeyTileInstance[] row = ReadRow(rowStart);

            int localTarget = targetIndex - rowStart;
            RemoveTileFromRow(row, tile);

            if (!TryInsertBlockIntoRow(row, new List<OkeyTileInstance> { tile }, localTarget))
                return false;

            WriteRow(rowStart, row);
            return true;
        }

        public List<OkeyTileInstance> GetConnectedGroup(OkeyTileInstance centerTile)
        {
            List<OkeyTileInstance> result = new();
            if (centerTile == null) return result;

            EnsureSlotsCache();

            int centerIndex = FindTileSlotIndex(centerTile);
            if (centerIndex < 0) return result;

            int rowStart = GetRowStartIndex(centerIndex);
            int rowEnd = GetRowEndIndex(centerIndex);

            int left = centerIndex;
            while (left > rowStart && SlotHasTile(slots[left - 1])) left--;

            int right = centerIndex;
            while (right < rowEnd && SlotHasTile(slots[right + 1])) right++;

            for (int i = left; i <= right; i++)
            {
                OkeyTileInstance tile = GetTileInSlot(slots[i]);
                if (tile != null) result.Add(tile);
            }

            return result;
        }

        public bool InsertGroupIntoSlot(List<OkeyTileInstance> group, Transform slotTransform)
        {
            return InsertGroupIntoSlot(group, slotTransform, 0);
        }

        public bool InsertGroupIntoSlot(List<OkeyTileInstance> group, Transform slotTransform, int anchorIndex)
        {
            if (group == null || group.Count == 0 || slotTransform is not RectTransform targetSlot) return false;
            EnsureSlotsCache();

            int targetIndex = slots.IndexOf(targetSlot);
            if (targetIndex < 0) return false;

            List<OkeyTileInstance> cleanGroup = NormalizeGroup(group);
            if (cleanGroup.Count <= 1) return false;

            if (anchorIndex < 0) anchorIndex = 0;
            if (anchorIndex >= cleanGroup.Count) anchorIndex = cleanGroup.Count - 1;

            int sourceRowStart = -1;
            for (int i = 0; i < cleanGroup.Count; i++)
            {
                int idx = FindTileSlotIndex(cleanGroup[i]);
                if (idx < 0) return false;

                int rowStart = GetRowStartIndex(idx);
                if (sourceRowStart < 0) sourceRowStart = rowStart;
                else if (sourceRowStart != rowStart) return false;
            }

            int targetRowStart = GetRowStartIndex(targetIndex);
            int localTarget = targetIndex - targetRowStart;
            int localStart = localTarget - anchorIndex;
            if (localStart < 0 || localStart + cleanGroup.Count > Columns) return false;

            OkeyTileInstance[] sourceRow = ReadRow(sourceRowStart);
            OkeyTileInstance[] targetRow = sourceRowStart == targetRowStart ? sourceRow : ReadRow(targetRowStart);

            RemoveTilesFromRow(sourceRow, cleanGroup);

            if (!TryInsertBlockIntoRow(targetRow, cleanGroup, localStart))
                return false;

            if (sourceRowStart == targetRowStart)
            {
                WriteRow(sourceRowStart, targetRow);
            }
            else
            {
                WriteRow(sourceRowStart, sourceRow);
                WriteRow(targetRowStart, targetRow);
            }

            return true;
        }

        public List<OkeyTileInstance> GetTilesInVisualOrder()
        {
            return CollectTiles(false);
        }

        public List<OkeyTileInstance> GetSlotLayout()
        {
            return CollectTiles(true);
        }

        public void RestoreSlotLayout(List<OkeyTileInstance> layout)
        {
            if (layout == null) return;
            EnsureSlotsCache();
            ClearBoard();

            int count = Mathf.Min(layout.Count, slots.Count);
            for (int i = 0; i < count; i++)
            {
                if (layout[i] != null)
                    PlaceIntoTransform(layout[i], slots[i]);
            }
        }

        public bool IsSlotTransform(Transform t)
        {
            if (t == null) return false;
            EnsureSlotsCache();

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == t) return true;
            }

            return false;
        }

        private List<OkeyTileInstance> NormalizeGroup(List<OkeyTileInstance> group)
        {
            List<OkeyTileInstance> clean = new();
            HashSet<OkeyTileInstance> seen = new();

            for (int i = 0; i < group.Count; i++)
            {
                OkeyTileInstance t = group[i];
                if (t == null || seen.Contains(t)) continue;
                seen.Add(t);
                clean.Add(t);
            }

            return clean;
        }

        private bool TryInsertBlockIntoRow(OkeyTileInstance[] row, List<OkeyTileInstance> block, int startIndex)
        {
            if (row == null || block == null || block.Count == 0) return false;
            if (startIndex < 0 || startIndex + block.Count > Columns) return false;

            int firstOccupiedInRange = FindFirstOccupiedInRange(row, startIndex, startIndex + block.Count - 1);
            if (firstOccupiedInRange >= 0)
            {
                int blockEnd = FindContiguousOccupiedEnd(row, firstOccupiedInRange);
                int gapLen = GetGapLength(row, blockEnd + 1);
                if (gapLen < block.Count) return false;

                for (int i = blockEnd; i >= firstOccupiedInRange; i--)
                {
                    row[i + block.Count] = row[i];
                    row[i] = null;
                }
            }

            for (int i = startIndex; i < startIndex + block.Count; i++)
            {
                if (row[i] != null && !block.Contains(row[i]))
                    return false;
            }

            for (int i = 0; i < block.Count; i++)
                row[startIndex + i] = block[i];

            return true;
        }

        private void RemoveTileFromRow(OkeyTileInstance[] row, OkeyTileInstance tile)
        {
            if (row == null || tile == null) return;
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] == tile)
                {
                    row[i] = null;
                    return;
                }
            }
        }

        private void RemoveTilesFromRow(OkeyTileInstance[] row, List<OkeyTileInstance> tiles)
        {
            if (row == null || tiles == null || tiles.Count == 0) return;
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] != null && tiles.Contains(row[i]))
                    row[i] = null;
            }
        }

        private List<OkeyTileInstance> CollectTiles(bool includeNulls)
        {
            EnsureSlotsCache();
            List<OkeyTileInstance> result = new(slots.Count);

            for (int i = 0; i < slots.Count; i++)
            {
                OkeyTileInstance tile = slots[i] == null ? null : GetTileInSlot(slots[i]);
                if (includeNulls || tile != null)
                    result.Add(tile);
            }

            return result;
        }

        private int GetRowStartIndex(int slotIndex)
        {
            int row = slotIndex / Columns;
            return row * Columns;
        }

        private int GetRowEndIndex(int slotIndex)
        {
            int row = slotIndex / Columns;
            return Mathf.Min(row * Columns + Columns - 1, slots.Count - 1);
        }

        private int FindTileSlotIndex(OkeyTileInstance tile)
        {
            if (tile == null) return -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (GetTileInSlot(slots[i]) == tile)
                    return i;
            }
            return -1;
        }

        private OkeyTileInstance[] ReadRow(int rowStart)
        {
            OkeyTileInstance[] state = new OkeyTileInstance[Columns];
            int rowEnd = Mathf.Min(rowStart + Columns - 1, slots.Count - 1);

            for (int i = rowStart; i <= rowEnd; i++)
                state[i - rowStart] = GetTileInSlot(slots[i]);

            return state;
        }

        private void WriteRow(int rowStart, OkeyTileInstance[] rowState)
        {
            int rowEnd = Mathf.Min(rowStart + Columns - 1, slots.Count - 1);

            for (int i = rowStart; i <= rowEnd; i++)
                ClearSlot(slots[i], false);

            for (int i = 0; i < Columns && rowStart + i < slots.Count; i++)
            {
                if (rowState[i] != null)
                    PlaceIntoTransform(rowState[i], slots[rowStart + i]);
            }
        }

        private int FindContiguousOccupiedEnd(OkeyTileInstance[] rowState, int startIndex)
        {
            if (rowState == null || startIndex < 0 || startIndex >= rowState.Length) return -1;
            if (rowState[startIndex] == null) return startIndex - 1;

            int end = startIndex;
            while (end + 1 < rowState.Length && rowState[end + 1] != null) end++;
            return end;
        }

        private int GetGapLength(OkeyTileInstance[] rowState, int startIndex)
        {
            if (rowState == null || startIndex < 0 || startIndex >= rowState.Length) return 0;

            int len = 0;
            for (int i = startIndex; i < rowState.Length; i++)
            {
                if (rowState[i] != null) break;
                len++;
            }

            return len;
        }

        private int FindFirstOccupiedInRange(OkeyTileInstance[] rowState, int startIndex, int endIndex)
        {
            if (rowState == null) return -1;
            if (startIndex < 0) startIndex = 0;
            if (endIndex >= rowState.Length) endIndex = rowState.Length - 1;

            for (int i = startIndex; i <= endIndex; i++)
            {
                if (rowState[i] != null) return i;
            }

            return -1;
        }

        private void ClearSlot(RectTransform slot, bool deactivateObjects)
        {
            if (slot == null) return;

            for (int c = slot.childCount - 1; c >= 0; c--)
            {
                Transform child = slot.GetChild(c);
                if (child == null) continue;

                OkeyTileInstance tile = child.GetComponent<OkeyTileInstance>();
                if (tile == null) continue;

                if (deactivateObjects)
                    tile.gameObject.SetActive(false);

                tile.transform.SetParent(null, false);
            }
        }

        private void SetupSlotVisual(Image img)
        {
            if (img == null) return;
            img.raycastTarget = true;
            img.color = DebugSlotBackground ? new Color(1f, 1f, 1f, .08f) : new Color(1f, 1f, 1f, 0f);
        }

        private void PrepareTileForSlot(OkeyTileInstance tile)
        {
            if (tile == null) return;

            tile.gameObject.SetActive(true);
            SetupTileRect(tile);

            CanvasGroup cg = tile.GetComponent<CanvasGroup>();
            if (cg == null) cg = tile.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;
            cg.alpha = 1f;

            OkeyTileDrag drag = tile.GetComponent<OkeyTileDrag>();
            if (drag == null) drag = tile.gameObject.AddComponent<OkeyTileDrag>();
            drag.enabled = true;

            Graphic[] graphics = tile.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].raycastTarget = true;
            }
        }

        private void PlaceIntoTransform(OkeyTileInstance tile, Transform parent)
        {
            if (tile == null || parent == null) return;
            tile.transform.SetParent(parent, false);
            PrepareTileForSlot(tile);
        }

        private void SetupTileRect(OkeyTileInstance tile)
        {
            if (tile.transform is not RectTransform rt) return;

            rt.anchorMin = new Vector2(.5f, .5f);
            rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.sizeDelta = CellSize;
        }

        private void EnsureSlotsCache()
        {
            if (SlotGrid == null) EnsureBoard();
            if (SlotGrid == null) return;

            slots.Clear();

            for (int i = 0; i < SlotGrid.childCount; i++)
            {
                RectTransform rt = SlotGrid.GetChild(i) as RectTransform;
                if (rt != null && rt.name.StartsWith("Slot_"))
                    slots.Add(rt);
            }

            if (slots.Count != TotalSlots) RebuildSlots();
            else EnsureDropTargetsOnAllSlots();
        }

        private void EnsureDropTargetsOnAllSlots()
        {
            if (SlotGrid == null) return;

            for (int i = 0; i < SlotGrid.childCount; i++)
            {
                RectTransform rt = SlotGrid.GetChild(i) as RectTransform;
                if (rt == null || !rt.name.StartsWith("Slot_")) continue;
                EnsureDropTarget(rt.gameObject, rt);
            }
        }

        private void EnsureDropTarget(GameObject slotGO, RectTransform slotRt)
        {
            if (slotGO == null || slotRt == null) return;

            Image img = slotGO.GetComponent<Image>();
            if (img == null) img = slotGO.AddComponent<Image>();
            SetupSlotVisual(img);

            OkeyRackSlotDropTarget dropTarget = slotGO.GetComponent<OkeyRackSlotDropTarget>();
            if (dropTarget == null) dropTarget = slotGO.AddComponent<OkeyRackSlotDropTarget>();

            dropTarget.TurnManager = TurnManager;
            dropTarget.Seat = OwnerSeat;
            dropTarget.SlotTransform = slotRt;
        }

        private bool SlotHasTile(RectTransform slot)
        {
            return GetTileInSlot(slot) != null;
        }

        private OkeyTileInstance GetTileInSlot(RectTransform slot)
        {
            if (slot == null) return null;

            for (int i = 0; i < slot.childCount; i++)
            {
                Transform child = slot.GetChild(i);
                if (child == null) continue;

                OkeyTileInstance tile = child.GetComponent<OkeyTileInstance>();
                if (tile != null) return tile;
            }

            return null;
        }

        private void DestroySafe(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(go);
            else Destroy(go);
#else
            Destroy(go);
#endif
        }
    }
}