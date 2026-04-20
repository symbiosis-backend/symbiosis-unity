using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class LayoutPresetService : MonoBehaviour
    {
        public static LayoutPresetService I { get; private set; }

        public event Action<int, List<LayoutSlot>> LayoutLoaded;
        public event Action<string> LayoutError;
        public event Action StateChanged;

        [Header("State")]
        [SerializeField] private int lastLevel;
        [SerializeField] private string lastLayoutName;
        [SerializeField] private bool rememberLastRequest = true;

        public int LastLevel => lastLevel;
        public string LastLayoutName => lastLayoutName;
        public bool HasLastLayout => lastLevel > 0;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        public List<LayoutSlot> GetLevel(int level)
        {
            level = Mathf.Clamp(level, 1, 10);

            List<LayoutSlot> result = LayoutPresets.GetByLevel(level);

            if (result == null || result.Count == 0)
            {
                EmitError("Layout not found for level " + level);
                return new List<LayoutSlot>();
            }

            SaveState(level);
            LayoutLoaded?.Invoke(level, result);
            StateChanged?.Invoke();

            return Clone(result);
        }

        public List<LayoutSlot> GetRandom()
        {
            int level = UnityEngine.Random.Range(1, 11);
            return GetLevel(level);
        }

        public List<LayoutSlot> ReloadLast()
        {
            if (!HasLastLayout)
                return GetLevel(1);

            return GetLevel(lastLevel);
        }

        public int GetSlotCount(int level)
        {
            List<LayoutSlot> list = LayoutPresets.GetByLevel(level);
            return list != null ? list.Count : 0;
        }

        public string GetLevelName(int level)
        {
            return LayoutPresets.GetLevelName(level);
        }

        public List<int> GetAllLevels()
        {
            List<int> list = new();
            for (int i = 1; i <= 10; i++)
                list.Add(i);

            return list;
        }

        public bool TryGetLevel(int level, out List<LayoutSlot> slots)
        {
            slots = GetLevel(level);
            return slots != null && slots.Count > 0;
        }

        public void ClearState()
        {
            lastLevel = 0;
            lastLayoutName = string.Empty;
            StateChanged?.Invoke();
        }

        private void SaveState(int level)
        {
            if (!rememberLastRequest)
                return;

            lastLevel = level;
            lastLayoutName = LayoutPresets.GetLevelName(level);
        }

        private void EmitError(string msg)
        {
            Debug.LogWarning("[LayoutPresetService] " + msg);
            LayoutError?.Invoke(msg);
        }

        private List<LayoutSlot> Clone(List<LayoutSlot> source)
        {
            List<LayoutSlot> copy = new();

            if (source == null)
                return copy;

            for (int i = 0; i < source.Count; i++)
            {
                LayoutSlot s = source[i];
                if (s == null)
                    continue;

                copy.Add(new LayoutSlot
                {
                    X = s.X,
                    Y = s.Y,
                    Z = s.Z
                });
            }

            return copy;
        }
    }
}
