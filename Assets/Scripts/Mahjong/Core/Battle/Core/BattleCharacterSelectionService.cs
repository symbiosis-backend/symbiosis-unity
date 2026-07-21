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
        private const string EconomyMigrationV2Key = "MahjongGame.Battle.CharacterEconomyV2";
        private const string GlobalProfileScope = "global";
        private const bool ForceUnlockAllCharactersForLocalTesting = false;

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
        [SerializeField] private bool autoUnlockStarterCharacters = false;
        [SerializeField] private bool autoSelectFallbackStarter = false;
        [SerializeField] private bool saveImmediatelyOnChange = true;

        [Header("Economy")]
        [SerializeField] private int firstPaidCharacterPrice = 5000;
        [SerializeField] private int paidCharacterPriceStep = 5000;
        [SerializeField] private bool migrateLegacyFullUnlocks = true;
        [SerializeField] private bool unlockAllCharactersForLocalTesting = false;

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
        private string loadedPrefsScopeKey = string.Empty;

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

            ProfileService.ProfileChanged -= HandleProfileChanged;
            ProfileService.ProfileChanged += HandleProfileChanged;
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

            ProfileService.ProfileChanged -= HandleProfileChanged;

            if (Instance == this)
                Instance = null;
        }

        public bool HasSelectedCharacter()
        {
            if (string.IsNullOrWhiteSpace(selectedCharacterId))
                return false;

            // A saved id alone is not a valid battle character. Profiles created after
            // another player used the device can still have a stale/global id, and a
            // catalog entry can exist while the character is not owned by this profile.
            return CanSelect(selectedCharacterId);
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

            if (IsLocalTestingUnlockEnabled() && !DatabaseReady())
                TryResolveDatabaseAndPrepare();

            if (IsLocalTestingUnlockEnabled() && IsEnabledCharacter(characterId))
                return true;

            return unlockedCharacterIds.Contains(characterId);
        }

        public bool IsPersistentlyUnlocked(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (!DatabaseReady() && !TryResolveDatabaseAndPrepare())
                return unlockedCharacterIds.Contains(characterId);

            BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(characterId);
            if (data != null && data.IsStarterFree)
                return true;

            return unlockedCharacterIds.Contains(characterId);
        }

        public bool CanSelect(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (!DatabaseReady() && !TryResolveDatabaseAndPrepare())
                return false;

            BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(characterId);
            if (data == null || !data.IsEnabled)
                return false;

            return IsUnlocked(characterId);
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

            BattleCharacterDatabase.BattleCharacterData data =
                DatabaseReady() ? database.GetCharacterOrNull(characterId) : null;

            if (data == null)
                return 0;

            if (data.IsStarterFree || data.UnlockType == BattleCharacterDatabase.CharacterUnlockType.Default)
                return 0;

            if (IsFirstFreeCharacterChoice(data))
                return 0;

            if (ResolveUnlockCurrency(data) == BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist)
                return Mathf.Max(0, data.PriceAmount);

            if (unlockedCharacterIds.Count == 0)
                return 0;

            int purchasedCount = CountPurchasedOzTileCharacters();
            int price = firstPaidCharacterPrice + purchasedCount * paidCharacterPriceStep;
            return Mathf.Max(0, price);
        }

        public bool CanAffordCharacter(string characterId)
        {
            int price = GetUnlockPrice(characterId);
            if (price <= 0)
                return true;

            BattleCharacterDatabase.BattleCharacterData data =
                DatabaseReady() ? database.GetCharacterOrNull(characterId) : null;
            BattleCharacterDatabase.CharacterPriceCurrencyType currency = ResolveUnlockCurrency(data);
            return CanSpendCurrency(currency, price);
        }

        public bool IsFirstFreeCharacterChoice(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (!DatabaseReady() && !TryResolveDatabaseAndPrepare())
                return false;

            return IsFirstFreeCharacterChoice(database.GetCharacterOrNull(characterId));
        }

        private bool IsFirstFreeCharacterChoice(BattleCharacterDatabase.BattleCharacterData data)
        {
            return data != null &&
                   data.IsEnabled &&
                   data.AnimalType != BattleCharacterDatabase.CharacterAnimalType.Dragon &&
                   string.IsNullOrWhiteSpace(selectedCharacterId) &&
                   purchasedCharacterIds.Count == 0;
        }

        public bool TryPurchaseCharacter(string characterId, bool selectAfterPurchase = true, bool applyStatsToHub = true)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (!DatabaseReady() && !TryResolveDatabaseAndPrepare())
                return false;

            BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(characterId);
            if (data == null || !data.IsEnabled)
                return false;

            if (IsUnlocked(characterId))
                return !selectAfterPurchase || SelectCharacter(characterId, applyStatsToHub, true);

            int price = GetUnlockPrice(characterId);
            if (price > 0)
            {
                if (!SpendCurrency(ResolveUnlockCurrency(data), price))
                {
                    Debug.Log($"[BattleCharacterSelectionService] Not enough currency to unlock '{characterId}'. Price={price}");
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

        public bool TryUnlockCharacterWithAmetist(string characterId, int price, bool selectAfterPurchase = true, bool applyStatsToHub = true)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (!DatabaseReady() && !TryResolveDatabaseAndPrepare())
                return false;

            BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(characterId);
            if (data == null || !data.IsEnabled)
                return false;

            if (!IsPersistentlyUnlocked(characterId))
            {
                int safePrice = Mathf.Max(0, price);
                if (safePrice > 0)
                {
                    if (CurrencyService.I == null || !CurrencyService.I.SpendOzAmetist(safePrice))
                        return false;
                }

                unlockedCharacterIds.Add(characterId);
                if (!purchasedCharacterIds.Contains(characterId))
                    purchasedCharacterIds.Add(characterId);
            }

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
            if ((!DatabaseReady() && !TryResolveDatabaseAndPrepare()) || string.IsNullOrWhiteSpace(selectedCharacterId))
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
            string scopeKey = GetPrefsScopeKey();
            PlayerPrefs.SetString(GetScopedPrefsKey(SelectedCharacterKey, scopeKey), selectedCharacterId ?? string.Empty);
            PlayerPrefs.SetString(SelectedCharacterKey, selectedCharacterId ?? string.Empty);

            UnlockedCharactersSaveData saveData = new UnlockedCharactersSaveData
            {
                Ids = new List<string>(unlockedCharacterIds)
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(GetScopedPrefsKey(UnlockedCharactersKey, scopeKey), json);

            UnlockedCharactersSaveData purchasedSaveData = new UnlockedCharactersSaveData
            {
                Ids = new List<string>(purchasedCharacterIds)
            };

            string purchasedJson = JsonUtility.ToJson(purchasedSaveData);
            PlayerPrefs.SetString(GetScopedPrefsKey(PurchasedCharactersKey, scopeKey), purchasedJson);
            PlayerPrefs.SetInt(GetScopedPrefsKey(EconomyMigrationKey, scopeKey), 1);
            PlayerPrefs.SetInt(GetScopedPrefsKey(EconomyMigrationV2Key, scopeKey), 1);
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

            RaiseSelectionChanged();
        }

        public static void ClearPrefs()
        {
            PlayerPrefs.DeleteKey(SelectedCharacterKey);
            PlayerPrefs.DeleteKey(UnlockedCharactersKey);
            PlayerPrefs.DeleteKey(PurchasedCharactersKey);
            PlayerPrefs.DeleteKey(EconomyMigrationKey);
            PlayerPrefs.DeleteKey(EconomyMigrationV2Key);

            string scopeKey = GetPrefsScopeKey();
            PlayerPrefs.DeleteKey(GetScopedPrefsKey(SelectedCharacterKey, scopeKey));
            PlayerPrefs.DeleteKey(GetScopedPrefsKey(UnlockedCharactersKey, scopeKey));
            PlayerPrefs.DeleteKey(GetScopedPrefsKey(PurchasedCharactersKey, scopeKey));
            PlayerPrefs.DeleteKey(GetScopedPrefsKey(EconomyMigrationKey, scopeKey));
            PlayerPrefs.DeleteKey(GetScopedPrefsKey(EconomyMigrationV2Key, scopeKey));
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

        private bool IsLocalTestingUnlockEnabled()
        {
            return ForceUnlockAllCharactersForLocalTesting || unlockAllCharactersForLocalTesting;
        }

        private bool IsEnabledCharacter(string characterId)
        {
            if (!DatabaseReady())
                return false;

            BattleCharacterDatabase.BattleCharacterData data = database.GetCharacterOrNull(characterId);
            return data != null && data.IsEnabled;
        }

        private void LoadFromPrefs()
        {
            loadedPrefsScopeKey = GetPrefsScopeKey();
            string readScopeKey = loadedPrefsScopeKey;

            selectedCharacterId = PlayerPrefs.GetString(GetScopedPrefsKey(SelectedCharacterKey, readScopeKey), string.Empty);
            unlockedCharacterIds.Clear();
            purchasedCharacterIds.Clear();

            string unlockedJson = PlayerPrefs.GetString(GetScopedPrefsKey(UnlockedCharactersKey, readScopeKey), string.Empty);
            LoadIdsFromJson(unlockedJson, unlockedCharacterIds);

            string purchasedJson = PlayerPrefs.GetString(GetScopedPrefsKey(PurchasedCharactersKey, readScopeKey), string.Empty);
            LoadIdsFromJson(purchasedJson, purchasedCharacterIds);
        }

        private void HandleProfileChanged()
        {
            string scopeKey = GetPrefsScopeKey();
            if (string.Equals(scopeKey, loadedPrefsScopeKey, StringComparison.Ordinal))
                return;

            LoadFromPrefs();
            EnsureValidState();

            if (autoApplyStatsOnLoad)
                ApplySelectedCharacterStatsToHub();

            RaiseSelectionChanged();
        }

        private static string GetScopedPrefsKey(string baseKey, string scopeKey)
        {
            if (string.IsNullOrWhiteSpace(scopeKey) || string.Equals(scopeKey, GlobalProfileScope, StringComparison.Ordinal))
                return baseKey;

            return baseKey + "." + scopeKey;
        }

        private static string GetPrefsScopeKey()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return GlobalProfileScope;

            if (!string.IsNullOrWhiteSpace(profile.OnlinePlayerId))
                return "account_" + SanitizePrefsScope(profile.OnlinePlayerId);

            if (!string.IsNullOrWhiteSpace(profile.LocalProfileId))
                return "profile_" + SanitizePrefsScope(profile.LocalProfileId);

            return GlobalProfileScope;
        }

        private static string SanitizePrefsScope(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return GlobalProfileScope;

            string trimmed = value.Trim();
            char[] chars = new char[trimmed.Length];
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                chars[i] = char.IsLetterOrDigit(c) ? c : '_';
            }

            return new string(chars);
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
            {
                MigrateLegacyUnlocksIfNeeded();
                MigrateForcedUnlockPrefsIfNeeded();
            }

            if (autoUnlockStarterCharacters)
                UnlockStartersOrFallback();

            if (IsLocalTestingUnlockEnabled())
                UnlockAllEnabledCharactersForTesting();

            if (!string.IsNullOrWhiteSpace(selectedCharacterId) && !CanSelect(selectedCharacterId))
                selectedCharacterId = string.Empty;

            if (string.IsNullOrWhiteSpace(selectedCharacterId) && autoSelectFallbackStarter)
                SelectFallbackCharacter();

            if (saveImmediatelyOnChange)
                Save();
        }

        private void UnlockStartersOrFallback()
        {
            if (unlockedCharacterIds.Count > 0)
                return;

            List<BattleCharacterDatabase.BattleCharacterData> starters = database.GetStarterFreeCharacters();
            if (starters != null && starters.Count > 0 && starters[0] != null && !string.IsNullOrWhiteSpace(starters[0].Id))
            {
                unlockedCharacterIds.Add(starters[0].Id);
                return;
            }

            if (verboseLogs)
                Debug.LogWarning("[BattleCharacterSelectionService] No starter character configured; unlocking the first enabled character as release fallback.");

            UnlockFirstEnabledAsLastResort();
        }

        private void UnlockAllEnabledCharactersForTesting()
        {
            if (!DatabaseReady())
                return;

            List<BattleCharacterDatabase.BattleCharacterData> enabled = database.GetEnabledCharacters();
            for (int i = 0; i < enabled.Count; i++)
            {
                BattleCharacterDatabase.BattleCharacterData data = enabled[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Id))
                    continue;

                if (!unlockedCharacterIds.Contains(data.Id))
                    unlockedCharacterIds.Add(data.Id);
            }
        }

        private void MigrateLegacyUnlocksIfNeeded()
        {
            if (PlayerPrefs.GetInt(GetScopedPrefsKey(EconomyMigrationKey, loadedPrefsScopeKey), 0) == 1)
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

        private void MigrateForcedUnlockPrefsIfNeeded()
        {
            if (PlayerPrefs.GetInt(GetScopedPrefsKey(EconomyMigrationV2Key, loadedPrefsScopeKey), 0) == 1)
                return;

            if (!DatabaseReady() || purchasedCharacterIds.Count > 0)
                return;

            List<BattleCharacterDatabase.BattleCharacterData> enabled = database.GetEnabledCharacters();
            if (enabled == null || enabled.Count <= 1 || unlockedCharacterIds.Count < enabled.Count)
                return;

            string freeId = ChooseFreeCharacterForMigration();
            unlockedCharacterIds.Clear();

            if (!string.IsNullOrWhiteSpace(freeId))
                unlockedCharacterIds.Add(freeId);

            if (!string.IsNullOrWhiteSpace(selectedCharacterId) && !unlockedCharacterIds.Contains(selectedCharacterId))
                selectedCharacterId = freeId;
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

            if (verboseLogs)
                Debug.Log("[BattleCharacterSelectionService] No unlocked fallback character is available.");
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

        private int CountPurchasedOzTileCharacters()
        {
            int count = 0;
            for (int i = 0; i < purchasedCharacterIds.Count; i++)
            {
                string id = purchasedCharacterIds[i];
                if (string.IsNullOrWhiteSpace(id) || !IsUnlocked(id))
                    continue;

                BattleCharacterDatabase.BattleCharacterData data =
                    DatabaseReady() ? database.GetCharacterOrNull(id) : null;
                if (data != null && ResolveUnlockCurrency(data) == BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile)
                    count++;
            }

            return count;
        }

        private static BattleCharacterDatabase.CharacterPriceCurrencyType ResolveUnlockCurrency(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile;

            return IsDonationCharacter(data)
                ? BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist
                : BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile;
        }

        private static bool IsDonationCharacter(BattleCharacterDatabase.BattleCharacterData data)
        {
            return data != null &&
                   (data.AnimalType == BattleCharacterDatabase.CharacterAnimalType.Dragon ||
                    data.UnlockType == BattleCharacterDatabase.CharacterUnlockType.PremiumCurrency ||
                    data.PriceCurrency == BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist);
        }

        private static bool CanSpendCurrency(BattleCharacterDatabase.CharacterPriceCurrencyType currency, int amount)
        {
            if (amount <= 0)
                return true;

            if (CurrencyService.I == null)
                return false;

            switch (currency)
            {
                case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist:
                    return CurrencyService.I.CanSpendOzAmetist(amount);
                case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAltin:
                    return CurrencyService.I.CanSpendOzAltin(amount);
                case BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile:
                    return CurrencyService.I.CanSpendOzTile(amount);
                default:
                    return false;
            }
        }

        private static bool SpendCurrency(BattleCharacterDatabase.CharacterPriceCurrencyType currency, int amount)
        {
            if (amount <= 0)
                return true;

            if (CurrencyService.I == null)
                return false;

            switch (currency)
            {
                case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAmetist:
                    return CurrencyService.I.SpendOzAmetist(amount);
                case BattleCharacterDatabase.CharacterPriceCurrencyType.OzAltin:
                    return CurrencyService.I.SpendOzAltin(amount);
                case BattleCharacterDatabase.CharacterPriceCurrencyType.OzTile:
                    return CurrencyService.I.SpendOzTile(amount);
                default:
                    return false;
            }
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
