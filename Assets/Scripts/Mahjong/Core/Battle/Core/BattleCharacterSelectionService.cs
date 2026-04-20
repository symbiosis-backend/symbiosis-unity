using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleCharacterSelectionService : MonoBehaviour
    {
        public static BattleCharacterSelectionService Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        private const string SelectedCharacterKey = "MahjongGame.Battle.SelectedCharacterId";
        private const string UnlockedCharactersKey = "MahjongGame.Battle.UnlockedCharacterIds";
        private const string PurchasedCharactersKey = "MahjongGame.Battle.PurchasedCharacterIds";
        private const string EconomyMigrationKey = "MahjongGame.Battle.CharacterEconomyV1";

        [Serializable]
        private sealed class UnlockedCharactersSaveData
        {
            public List<string> Ids = new List<string>();
        }

        [Header("Links")]
        [SerializeField] private BattleCharacterDatabase database;

        [Header("Persistence")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool autoApplyStatsOnLoad = true;
        [SerializeField] private bool autoUnlockStarterCharacters = true;
        [SerializeField] private bool autoSelectFallbackStarter = true;
        [SerializeField] private bool saveImmediatelyOnChange = true;

        [Header("Economy")]
        [SerializeField] private int firstPaidCharacterPrice = 10000;
        [SerializeField] private int paidCharacterPriceStep = 20000;
        [SerializeField] private bool migrateLegacyFullUnlocks = true;

        [Header("Auto Find")]
        [SerializeField] private bool autoFindDatabase = true;
        [SerializeField] private bool waitForDatabaseIfMissing = true;
        [SerializeField] private float databaseWaitTimeout = 5f;
        [SerializeField] private bool verboseLogs = true;

        [Header("Runtime")]
        [SerializeField] private string selectedCharacterId;
        [SerializeField] private List<string> unlockedCharacterIds = new List<string>();
        [SerializeField] private List<string> purchasedCharacterIds = new List<string>();

        private Coroutine initializeRoutine;

        public event Action<string> SelectedCharacterChanged;
        public event Action SelectionStateChanged;

        public string SelectedCharacterId => selectedCharacterId;
        public IReadOnlyList<string> UnlockedCharacterIds => unlockedCharacterIds;
        public IReadOnlyList<string> PurchasedCharacterIds => purchasedCharacterIds;
        public bool HasAnyUnlockedCharacter => unlockedCharacterIds.Count > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
                PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (initializeRoutine == null)
                initializeRoutine = StartCoroutine(InitializeRoutine());
        }

        private void OnDestroy()
        {
            BattleCharacterDatabase.CatalogChanged -= OnCharacterCatalogChanged;

            if (initializeRoutine != null)
            {
                StopCoroutine(initializeRoutine);
                initializeRoutine = null;
            }

            if (Instance == this)
                Instance = null;
        }

        public bool HasSelectedCharacter()
        {
            return !string.IsNullOrWhiteSpace(selectedCharacterId) && GetSelectedCharacter() != null;
        }

        public BattleCharacterDatabase.BattleCharacterData GetSelectedCharacter()
        {
            if (!DatabaseReady() || string.IsNullOrWhiteSpace(selectedCharacterId))
                return null;

            return database.GetCharacterOrNull(selectedCharacterId);
        }

        public bool IsUnlocked(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            return unlockedCharacterIds.Contains(characterId);
        }

        public bool CanSelect(string characterId)
        {
            if (!DatabaseReady() || string.IsNullOrWhiteSpace(characterId))
                return false;

            BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(characterId);
            return data != null && data.IsEnabled && IsUnlocked(characterId);
        }

        public bool SelectCharacter(string characterId, bool applyStatsToHub = true, bool save = true)
        {
            if (!CanSelect(characterId))
                return false;

            selectedCharacterId = characterId;

            if (applyStatsToHub)
                ApplySelectedCharacterStatsToHub();

            RaiseSelectionChanged();

            if (save && saveImmediatelyOnChange)
                Save();

            return true;
        }

        public int GetUnlockPrice(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || IsUnlocked(characterId))
                return 0;

            if (unlockedCharacterIds.Count == 0)
                return 0;

            BattleCharacterDatabase.BattleCharacterData data =
                DatabaseReady() ? database.GetCharacterOrNull(characterId) : null;

            if (data != null && data.PriceAmount > 0)
                return Mathf.Max(0, data.PriceAmount);

            int purchasedCount = CountPurchasedCharacters();
            int price = firstPaidCharacterPrice + purchasedCount * paidCharacterPriceStep;
            return Mathf.Max(0, price);
        }

        public bool CanAffordCharacter(string characterId)
        {
            int price = GetUnlockPrice(characterId);
            if (price <= 0)
                return true;

            return CurrencyService.I != null && CurrencyService.I.CanSpendOzAltin(price);
        }

        public bool TryPurchaseCharacter(string characterId, bool selectAfterPurchase = true, bool applyStatsToHub = true)
        {
            if (!DatabaseReady() || string.IsNullOrWhiteSpace(characterId))
                return false;

            BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(characterId);
            if (data == null || !data.IsEnabled)
                return false;

            if (IsUnlocked(characterId))
                return !selectAfterPurchase || SelectCharacter(characterId, applyStatsToHub, true);

            int price = GetUnlockPrice(characterId);
            if (price > 0)
            {
                if (CurrencyService.I == null || !CurrencyService.I.SpendOzAltin(price))
                {
                    Debug.Log($"[BattleCharacterSelectionService] Not enough Oz Altin to unlock '{characterId}'. Price={price}");
                    return false;
                }
            }

            unlockedCharacterIds.Add(characterId);
            if (price > 0 && !purchasedCharacterIds.Contains(characterId))
                purchasedCharacterIds.Add(characterId);

            if (selectAfterPurchase)
                selectedCharacterId = characterId;

            if (applyStatsToHub && selectAfterPurchase)
                ApplySelectedCharacterStatsToHub();

            RaiseSelectionChanged();

            if (saveImmediatelyOnChange)
                Save();

            return true;
        }

        public bool SelectOrPurchaseCharacter(string characterId, bool applyStatsToHub = true)
        {
            if (IsUnlocked(characterId))
                return SelectCharacter(characterId, applyStatsToHub, true);

            return TryPurchaseCharacter(characterId, true, applyStatsToHub);
        }

        public bool ApplySelectedCharacterStatsToHub()
        {
            if (!DatabaseReady() || string.IsNullOrWhiteSpace(selectedCharacterId))
                return false;

            return database.TryApplyCharacterStatsToHub(selectedCharacterId);
        }

        public void RefreshAfterCatalogChanged()
        {
            if (!TryResolveDatabaseAndPrepare())
                return;

            EnsureValidState();

            if (autoApplyStatsOnLoad)
                ApplySelectedCharacterStatsToHub();

            RaiseSelectionChanged();
        }

        public void Save()
        {
            PlayerPrefs.SetString(SelectedCharacterKey, selectedCharacterId ?? string.Empty);

            UnlockedCharactersSaveData saveData = new UnlockedCharactersSaveData
            {
                Ids = new List<string>(unlockedCharacterIds)
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(UnlockedCharactersKey, json);

            UnlockedCharactersSaveData purchasedSaveData = new UnlockedCharactersSaveData
            {
                Ids = new List<string>(purchasedCharacterIds)
            };

            string purchasedJson = JsonUtility.ToJson(purchasedSaveData);
            PlayerPrefs.SetString(PurchasedCharactersKey, purchasedJson);
            PlayerPrefs.SetInt(EconomyMigrationKey, 1);
            PlayerPrefs.Save();
        }

        public void ClearAllProgress(bool save = true)
        {
            selectedCharacterId = string.Empty;
            unlockedCharacterIds.Clear();
            purchasedCharacterIds.Clear();

            if (save)
                Save();

            RaiseSelectionChanged();
        }

        public void ResetForNewProfile()
        {
            ClearPrefs();

            selectedCharacterId = string.Empty;
            unlockedCharacterIds.Clear();
            purchasedCharacterIds.Clear();

            if (TryResolveDatabaseAndPrepare())
            {
                if (autoUnlockStarterCharacters)
                    UnlockStartersOrFallback();

                if (autoSelectFallbackStarter && unlockedCharacterIds.Count > 0)
                    SelectFallbackCharacter();

                if (autoApplyStatsOnLoad)
                    ApplySelectedCharacterStatsToHub();
            }

            if (saveImmediatelyOnChange)
                Save();

            RaiseSelectionChanged();
        }

        public static void ClearPrefs()
        {
            PlayerPrefs.DeleteKey(SelectedCharacterKey);
            PlayerPrefs.DeleteKey(UnlockedCharactersKey);
            PlayerPrefs.DeleteKey(PurchasedCharactersKey);
            PlayerPrefs.DeleteKey(EconomyMigrationKey);
            PlayerPrefs.Save();
        }

        private IEnumerator InitializeRoutine()
        {
            float timer = 0f;

            while (!TryResolveDatabaseAndPrepare())
            {
                if (!waitForDatabaseIfMissing || timer >= databaseWaitTimeout)
                    break;

                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            LoadFromPrefs();
            BattleCharacterDatabase.CatalogChanged -= OnCharacterCatalogChanged;
            BattleCharacterDatabase.CatalogChanged += OnCharacterCatalogChanged;
            EnsureValidState();
            RaiseSelectionChanged();

            if (autoApplyStatsOnLoad)
                ApplySelectedCharacterStatsToHub();

            if (verboseLogs)
            {
                string dbName = database != null ? database.name : "NULL";
                int dbCount = database != null ? database.CharacterCount : 0;
                Debug.Log($"[BattleCharacterSelectionService] Init done. DB={dbName}, Characters={dbCount}, Unlocked={unlockedCharacterIds.Count}, Selected='{selectedCharacterId}'");
            }

            initializeRoutine = null;
        }

        private bool TryResolveDatabaseAndPrepare()
        {
            if (database == null)
            {
                if (BattleCharacterDatabase.HasInstance)
                {
                    database = BattleCharacterDatabase.Instance;
                }
                else if (autoFindDatabase)
                {
                    database = FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include);
                }
            }

            if (database == null)
                return false;

            database.RebuildCache();
            return database.CharacterCount > 0;
        }

        private bool DatabaseReady()
        {
            return database != null && database.CharacterCount > 0;
        }

        private void LoadFromPrefs()
        {
            selectedCharacterId = PlayerPrefs.GetString(SelectedCharacterKey, string.Empty);
            unlockedCharacterIds.Clear();
            purchasedCharacterIds.Clear();

            string unlockedJson = PlayerPrefs.GetString(UnlockedCharactersKey, string.Empty);
            LoadIdsFromJson(unlockedJson, unlockedCharacterIds);

            string purchasedJson = PlayerPrefs.GetString(PurchasedCharactersKey, string.Empty);
            LoadIdsFromJson(purchasedJson, purchasedCharacterIds);
        }

        private void LoadIdsFromJson(string json, List<string> target)
        {
            if (target == null || string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                UnlockedCharactersSaveData saveData = JsonUtility.FromJson<UnlockedCharactersSaveData>(json);
                if (saveData == null || saveData.Ids == null)
                    return;

                for (int i = 0; i < saveData.Ids.Count; i++)
                {
                    string id = saveData.Ids[i];
                    if (!string.IsNullOrWhiteSpace(id) && !target.Contains(id))
                        target.Add(id);
                }
            }
            catch
            {
            }
        }

        private void EnsureValidState()
        {
            if (!DatabaseReady())
                return;

            RemoveInvalidUnlockedIds();
            RemoveInvalidPurchasedIds();

            if (migrateLegacyFullUnlocks)
                MigrateLegacyUnlocksIfNeeded();

            if (autoUnlockStarterCharacters)
                UnlockStartersOrFallback();

            if (!string.IsNullOrWhiteSpace(selectedCharacterId) && !CanSelect(selectedCharacterId))
                selectedCharacterId = string.Empty;

            if (string.IsNullOrWhiteSpace(selectedCharacterId) && autoSelectFallbackStarter && unlockedCharacterIds.Count > 0)
                SelectFallbackCharacter();

            if (saveImmediatelyOnChange)
                Save();
        }

        private void UnlockStartersOrFallback()
        {
            if (unlockedCharacterIds.Count > 0)
                return;

            if (!string.IsNullOrWhiteSpace(selectedCharacterId) &&
                database.GetCharacterOrNull(selectedCharacterId) != null)
            {
                unlockedCharacterIds.Add(selectedCharacterId);
                return;
            }
        }

        private void MigrateLegacyUnlocksIfNeeded()
        {
            if (PlayerPrefs.GetInt(EconomyMigrationKey, 0) == 1)
                return;

            if (purchasedCharacterIds.Count > 0)
                return;

            if (unlockedCharacterIds.Count <= 1)
                return;

            string freeId = ChooseFreeCharacterForMigration();
            unlockedCharacterIds.Clear();

            if (!string.IsNullOrWhiteSpace(freeId))
                unlockedCharacterIds.Add(freeId);
        }

        private string ChooseFreeCharacterForMigration()
        {
            if (!string.IsNullOrWhiteSpace(selectedCharacterId) &&
                database.GetCharacterOrNull(selectedCharacterId) != null)
                return selectedCharacterId;

            List<BattleCharacterDatabase.BattleCharacterData> starters = database.GetStarterFreeCharacters();
            if (starters != null && starters.Count > 0 && starters[0] != null)
                return starters[0].Id;

            List<BattleCharacterDatabase.BattleCharacterData> enabled = database.GetEnabledCharacters();
            if (enabled != null && enabled.Count > 0 && enabled[0] != null)
                return enabled[0].Id;

            return string.Empty;
        }

        private void UnlockFirstEnabledAsLastResort()
        {
            List<BattleCharacterDatabase.BattleCharacterData> enabled = database.GetEnabledCharacters();

            for (int i = 0; i < enabled.Count; i++)
            {
                BattleCharacterDatabase.BattleCharacterData data = enabled[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                    continue;

                if (!unlockedCharacterIds.Contains(data.Id))
                {
                    unlockedCharacterIds.Add(data.Id);
                    return;
                }
            }
        }

        private void SelectFallbackCharacter()
        {
            for (int i = 0; i < unlockedCharacterIds.Count; i++)
            {
                string id = unlockedCharacterIds[i];
                if (CanSelect(id))
                {
                    selectedCharacterId = id;
                    return;
                }
            }

            List<BattleCharacterDatabase.BattleCharacterData> enabled = database.GetEnabledCharacters();
            for (int i = 0; i < enabled.Count; i++)
            {
                BattleCharacterDatabase.BattleCharacterData data = enabled[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                    continue;

                if (!unlockedCharacterIds.Contains(data.Id))
                    unlockedCharacterIds.Add(data.Id);

                selectedCharacterId = data.Id;
                return;
            }
        }

        private void RemoveInvalidUnlockedIds()
        {
            for (int i = unlockedCharacterIds.Count - 1; i >= 0; i--)
            {
                string id = unlockedCharacterIds[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    unlockedCharacterIds.RemoveAt(i);
                    continue;
                }

                BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(id);
                if (data == null || !data.IsEnabled)
                    unlockedCharacterIds.RemoveAt(i);
            }
        }

        private void RemoveInvalidPurchasedIds()
        {
            for (int i = purchasedCharacterIds.Count - 1; i >= 0; i--)
            {
                string id = purchasedCharacterIds[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    purchasedCharacterIds.RemoveAt(i);
                    continue;
                }

                BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(id);
                if (data == null || !data.IsEnabled)
                {
                    purchasedCharacterIds.RemoveAt(i);
                    continue;
                }

                if (!unlockedCharacterIds.Contains(id))
                    unlockedCharacterIds.Add(id);
            }
        }

        private int CountPurchasedCharacters()
        {
            int count = 0;
            for (int i = 0; i < purchasedCharacterIds.Count; i++)
            {
                string id = purchasedCharacterIds[i];
                if (!string.IsNullOrWhiteSpace(id) && IsUnlocked(id))
                    count++;
            }

            return count;
        }

        private void RaiseSelectionChanged()
        {
            SelectedCharacterChanged?.Invoke(selectedCharacterId);
            SelectionStateChanged?.Invoke();
        }

        private void OnCharacterCatalogChanged()
        {
            RefreshAfterCatalogChanged();
        }
    }
}
