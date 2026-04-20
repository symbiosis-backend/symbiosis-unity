using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // API: Commands | State | Events
    [DisallowMultipleComponent]
    public sealed class BattleLayoutPresetService : MonoBehaviour
    {
        public static BattleLayoutPresetService I { get; private set; }

        public event Action<int, string, List<LayoutSlot>> LayoutLoaded;
        public event Action<string> LayoutLoadFailed;
        public event Action<BattleLayoutPresetService> StateChanged;

        [Header("State")]
        [SerializeField] private int lastLevel;
        [SerializeField] private string lastLayoutName;
        [SerializeField] private bool rememberLastRequest = true;

        [Header("Config")]
        [SerializeField, Min(1)] private int minLevel = 1;
        [SerializeField, Min(1)] private int maxLevel = 10;

        public int LastLevel => lastLevel;
        public string LastLayoutName => lastLayoutName;
        public bool HasLastLayout => lastLevel > 0;
        public int MinLevel => Mathf.Max(1, minLevel);
        public int MaxLevel => Mathf.Max(MinLevel, maxLevel);

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
        }

        public List<LayoutSlot> GetLevel(int level)
        {
            int safeLevel = NormalizeLevel(level);
            List<LayoutSlot> result = BattleLayoutPresets.GetByLevel(safeLevel);

            if (result == null || result.Count == 0)
            {
                EmitError("Battle layout not found for level " + safeLevel);
                return new List<LayoutSlot>();
            }

            SaveState(safeLevel);

            List<LayoutSlot> clone = Clone(result);
            LayoutLoaded?.Invoke(safeLevel, lastLayoutName, clone);
            StateChanged?.Invoke(this);

            return clone;
        }

        public bool TryGetLevel(int level, out List<LayoutSlot> slots)
        {
            slots = GetLevel(level);
            return slots != null && slots.Count > 0;
        }

        public List<LayoutSlot> GetRandom()
        {
            int safeMin = MinLevel;
            int safeMax = MaxLevel;
            int level = UnityEngine.Random.Range(safeMin, safeMax + 1);
            return GetLevel(level);
        }

        public bool TryGetRandom(out List<LayoutSlot> slots)
        {
            slots = GetRandom();
            return slots != null && slots.Count > 0;
        }

        public List<LayoutSlot> ReloadLast()
        {
            if (!HasLastLayout)
                return GetLevel(MinLevel);

            return GetLevel(lastLevel);
        }

        public int GetSlotCount(int level)
        {
            List<LayoutSlot> slots = BattleLayoutPresets.GetByLevel(NormalizeLevel(level));
            return slots != null ? slots.Count : 0;
        }

        public string GetLevelName(int level)
        {
            return ResolveLevelName(NormalizeLevel(level));
        }

        public List<int> GetAllLevels()
        {
            List<int> result = new();
            for (int i = MinLevel; i <= MaxLevel; i++)
                result.Add(i);

            return result;
        }

        public Dictionary<int, string> GetAllLevelNames()
        {
            Dictionary<int, string> result = new();
            for (int i = MinLevel; i <= MaxLevel; i++)
                result[i] = ResolveLevelName(i);

            return result;
        }

        public void ClearState()
        {
            lastLevel = 0;
            lastLayoutName = string.Empty;
            StateChanged?.Invoke(this);
        }

        public void SetRememberLastRequest(bool value)
        {
            rememberLastRequest = value;
            StateChanged?.Invoke(this);
        }

        public void SetLevelRange(int min, int max)
        {
            minLevel = Mathf.Max(1, min);
            maxLevel = Mathf.Max(minLevel, max);
            StateChanged?.Invoke(this);
        }

        private int NormalizeLevel(int level)
        {
            int safeMin = MinLevel;
            int safeMax = MaxLevel;

            if (level < safeMin)
                return safeMin;

            if (level > safeMax)
            {
                int span = safeMax - safeMin + 1;
                if (span <= 0)
                    return safeMin;

                return safeMin + Mathf.Abs(level - safeMin) % span;
            }

            return level;
        }

        private void SaveState(int level)
        {
            if (!rememberLastRequest)
                return;

            lastLevel = level;
            lastLayoutName = ResolveLevelName(level);
        }

        private string ResolveLevelName(int level)
        {
            return level switch
            {
                1 => "Duel Line",
                2 => "Twin Rows",
                3 => "Crown",
                4 => "Fang",
                5 => "Arena",
                6 => "Wings",
                7 => "Gate",
                8 => "Core Ring",
                9 => "Crossfire",
                10 => "Fortress",
                _ => "Battle Layout " + level
            };
        }

        private void EmitError(string message)
        {
            Debug.LogWarning("[BattleLayoutPresetService] " + message, this);
            LayoutLoadFailed?.Invoke(message);
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