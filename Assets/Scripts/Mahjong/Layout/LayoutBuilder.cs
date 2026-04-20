using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class LayoutBuilder : MonoBehaviour
    {
        public event Action<LayoutBuilder> LayoutChanged;
        public event Action<LayoutBuilder> StateChanged;

        [Header("Data")]
        [SerializeField] private LayoutData layout;

        [Header("Base Spacing")]
        [SerializeField] private float gapX = -10f;
        [SerializeField] private float gapY = -6f;

        [Header("Layer Visual Shift")]
        [SerializeField] private float layerShiftX = 6f;
        [SerializeField] private float layerShiftY = 1.5f;

        private readonly List<LayoutSlot> runtimeSlots = new();
        private Vector2 tileSize = new(56f, 76f);

        public LayoutData LayoutAsset => layout;
        public IReadOnlyList<LayoutSlot> RuntimeSlots => runtimeSlots;
        public IReadOnlyList<LayoutSlot> Slots
        {
            get
            {
                if (runtimeSlots.Count > 0)
                    return runtimeSlots;

                if (layout != null && layout.Slots != null)
                    return layout.Slots;

                return null;
            }
        }

        public Vector2 TileSize => tileSize;
        public float GapX => gapX;
        public float GapY => gapY;
        public float LayerShiftX => layerShiftX;
        public float LayerShiftY => layerShiftY;
        public bool HasRuntimeSlots => runtimeSlots.Count > 0;
        public bool HasAnySlots => Slots != null && Slots.Count > 0;
        public int SlotCount => Slots != null ? Slots.Count : 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            tileSize.x = Mathf.Max(16f, tileSize.x);
            tileSize.y = Mathf.Max(16f, tileSize.y);
            NotifyStateChanged();
        }
#endif

        public void SetLayout(LayoutData newLayout)
        {
            if (layout == newLayout)
                return;

            layout = newLayout;
            NotifyLayoutChanged();
        }

        public void ClearLayoutAsset()
        {
            if (layout == null)
                return;

            layout = null;
            NotifyLayoutChanged();
        }

        public void SetTileSize(Vector2 size)
        {
            Vector2 newSize = new(
                Mathf.Max(16f, size.x),
                Mathf.Max(16f, size.y));

            if (tileSize == newSize)
                return;

            tileSize = newSize;
            NotifyStateChanged();
        }

        public Vector2 GetTileSize()
        {
            return tileSize;
        }

        public void SetSlots(List<LayoutSlot> slots)
        {
            runtimeSlots.Clear();

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    LayoutSlot s = slots[i];
                    if (s == null)
                        continue;

                    runtimeSlots.Add(new LayoutSlot(s.X, s.Y, s.Z));
                }
            }

            NotifyLayoutChanged();
        }

        public void SetSlots(IReadOnlyList<LayoutSlot> slots)
        {
            runtimeSlots.Clear();

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    LayoutSlot s = slots[i];
                    if (s == null)
                        continue;

                    runtimeSlots.Add(new LayoutSlot(s.X, s.Y, s.Z));
                }
            }

            NotifyLayoutChanged();
        }

        public void ClearRuntimeSlots()
        {
            if (runtimeSlots.Count == 0)
                return;

            runtimeSlots.Clear();
            NotifyLayoutChanged();
        }

        public bool HasSlot(int x, int y, int z)
        {
            IReadOnlyList<LayoutSlot> slots = Slots;
            if (slots == null || slots.Count == 0)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                LayoutSlot slot = slots[i];
                if (slot == null)
                    continue;

                if (slot.X == x && slot.Y == y && slot.Z == z)
                    return true;
            }

            return false;
        }

        public Vector2 GetUiPos(LayoutSlot slot)
        {
            if (slot == null)
                return Vector2.zero;

            float stepX = Mathf.Max(1f, tileSize.x + gapX);
            float stepY = Mathf.Max(1f, tileSize.y + gapY);

            int x = Mathf.Clamp(slot.X, -200, 200);
            int y = Mathf.Clamp(slot.Y, -200, 200);
            int z = Mathf.Clamp(slot.Z, -50, 50);

            float px = x * stepX + z * layerShiftX;
            float py = -y * stepY - z * layerShiftY;

            return new Vector2(px, py);
        }

        public bool TryGetSlotUiPosition(int index, out Vector2 position)
        {
            position = Vector2.zero;

            IReadOnlyList<LayoutSlot> slots = Slots;
            if (slots == null || index < 0 || index >= slots.Count)
                return false;

            LayoutSlot slot = slots[index];
            if (slot == null)
                return false;

            position = GetUiPos(slot);
            return true;
        }

        public bool TryGetBounds(out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;

            IReadOnlyList<LayoutSlot> slots = Slots;
            if (slots == null || slots.Count == 0)
                return false;

            bool found = false;
            Vector2 half = tileSize * 0.5f;

            for (int i = 0; i < slots.Count; i++)
            {
                LayoutSlot slot = slots[i];
                if (slot == null)
                    continue;

                Vector2 pos = GetUiPos(slot);
                Vector2 localMin = pos - half;
                Vector2 localMax = pos + half;

                if (!found)
                {
                    min = localMin;
                    max = localMax;
                    found = true;
                }
                else
                {
                    min = Vector2.Min(min, localMin);
                    max = Vector2.Max(max, localMax);
                }
            }

            return found;
        }

        public Vector2 GetCenterOffset()
        {
            if (!TryGetBounds(out Vector2 min, out Vector2 max))
                return Vector2.zero;

            return -(min + max) * 0.5f;
        }

        public bool TryGetCenterOffset(out Vector2 centerOffset)
        {
            centerOffset = Vector2.zero;

            if (!TryGetBounds(out Vector2 min, out Vector2 max))
                return false;

            centerOffset = -(min + max) * 0.5f;
            return true;
        }

        public void SetGap(float x, float y)
        {
            if (Mathf.Approximately(gapX, x) && Mathf.Approximately(gapY, y))
                return;

            gapX = x;
            gapY = y;
            NotifyStateChanged();
        }

        public void SetLayerShift(float x, float y)
        {
            if (Mathf.Approximately(layerShiftX, x) && Mathf.Approximately(layerShiftY, y))
                return;

            layerShiftX = x;
            layerShiftY = y;
            NotifyStateChanged();
        }

        public Vector2 GetGap()
        {
            return new Vector2(gapX, gapY);
        }

        public Vector2 GetLayerShift()
        {
            return new Vector2(layerShiftX, layerShiftY);
        }

        public void CopySettingsFrom(LayoutBuilder other)
        {
            if (other == null || other == this)
                return;

            layout = other.layout;
            gapX = other.gapX;
            gapY = other.gapY;
            layerShiftX = other.layerShiftX;
            layerShiftY = other.layerShiftY;
            tileSize = other.tileSize;

            runtimeSlots.Clear();
            for (int i = 0; i < other.runtimeSlots.Count; i++)
            {
                LayoutSlot s = other.runtimeSlots[i];
                if (s == null)
                    continue;

                runtimeSlots.Add(new LayoutSlot(s.X, s.Y, s.Z));
            }

            NotifyLayoutChanged();
        }

        private void NotifyLayoutChanged()
        {
            LayoutChanged?.Invoke(this);
            StateChanged?.Invoke(this);
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
        }
    }
}