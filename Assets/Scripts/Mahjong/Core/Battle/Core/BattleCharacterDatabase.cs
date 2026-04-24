using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleCharacterDatabase : MonoBehaviour
    {
        public static BattleCharacterDatabase Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        public enum CharacterAnimalType
        {
            Tiger = 0,
            Fox = 1,
            Wolf = 2,
            Bear = 3
        }

        public enum CharacterGender
        {
            Male = 0,
            Female = 1
        }

        public enum CharacterUnlockType
        {
            Default = 0,
            SoftCurrency = 1,
            PremiumCurrency = 2,
            Event = 3,
            Reward = 4
        }

        public enum CharacterPriceCurrencyType
        {
            None = 0,
            OzAltin = 1,
            OzAmetist = 2
        }

        public const int DefaultFirstPaidCharacterPrice = 10000;
        public static event Action CatalogChanged;

        [Serializable]
        public struct BattleCharacterStats
        {
            public int MaxHp;
            public int Attack;
            [Range(0f, 1f)] public float Armor;
            [Range(0f, 1f)] public float ParryChance;
            [Range(0f, 1f)] public float CritChance;
            [Min(1f)] public float CritDamageMultiplier;

            public BattleCharacterStats(
                int maxHp,
                int attack,
                float armor,
                float parryChance,
                float critChance,
                float critDamageMultiplier)
            {
                MaxHp = NormalizeLegacyMaxHp(maxHp);
                Attack = Mathf.Max(0, attack);
                Armor = Mathf.Clamp01(armor);
                ParryChance = Mathf.Clamp01(parryChance);
                CritChance = Mathf.Clamp01(critChance);
                CritDamageMultiplier = Mathf.Max(1f, critDamageMultiplier);
            }

            public void Sanitize()
            {
                MaxHp = NormalizeLegacyMaxHp(MaxHp);
                Attack = Mathf.Max(0, Attack);
                Armor = Mathf.Clamp01(Armor);
                ParryChance = Mathf.Clamp01(ParryChance);
                CritChance = Mathf.Clamp01(CritChance);
                CritDamageMultiplier = Mathf.Max(1f, CritDamageMultiplier);
            }

            private static int NormalizeLegacyMaxHp(int hp)
            {
                int safeHp = Mathf.Max(1, hp);
                return safeHp >= 300 ? Mathf.Max(1, Mathf.RoundToInt(safeHp / 10f)) : safeHp;
            }
        }

        [Serializable]
        public sealed class BattleCharacterData
        {
            [Header("Identity")]
            public string Id;
            public string ServerId;
            public string DisplayName;
            public CharacterAnimalType AnimalType;
            public CharacterGender Gender;

            [Header("Visual")]
            [FormerlySerializedAs("SelectSprite")]
            public Sprite ProfileSprite;
            public Sprite LobbySprite;
            public Sprite BattleSprite;

            [Header("3D Models / FBX")]
            public GameObject ProfileModelPrefab;
            public GameObject LobbyModelPrefab;
            public GameObject BattleModelPrefab;
            public Texture2D ModelTexture;
            public Vector2 ModelTextureScale = Vector2.one;
            public Vector2 ModelTextureOffset = Vector2.zero;
            public bool UseSolidModelColor;
            public Color SolidModelColor = new Color(0.45f, 0.34f, 0.23f, 1f);

            [Header("Remote 3D Models / Addressables")]
            public AssetReferenceGameObject ProfileModelAddress;
            public AssetReferenceGameObject LobbyModelAddress;
            public AssetReferenceGameObject BattleModelAddress;
            public string ProfileModelAddressKey;
            public string LobbyModelAddressKey;
            public string BattleModelAddressKey;

            [Header("3D Animation Controllers")]
            public RuntimeAnimatorController ProfileAnimatorController;
            public RuntimeAnimatorController LobbyAnimatorController;
            public RuntimeAnimatorController BattleAnimatorController;

            [Header("3D Animation Clips")]
            public AnimationClip ProfileIdleAnimation;
            public AnimationClip LobbyIdleAnimation;
            public AnimationClip BattleIdleAnimation;
            public AnimationClip AttackAnimation;
            public AnimationClip HitAnimation;
            public AnimationClip VictoryAnimation;
            public AnimationClip DefeatAnimation;

            public Sprite SelectSprite => ProfileSprite;
            public GameObject SelectModelPrefab => ProfileModelPrefab;
            public GameObject DisplayModelPrefab => LobbyModelPrefab != null ? LobbyModelPrefab : ProfileModelPrefab;
            public GameObject CombatModelPrefab => BattleModelPrefab != null ? BattleModelPrefab : DisplayModelPrefab;
            public AssetReferenceGameObject SelectModelAddress => ProfileModelAddress;
            public AssetReferenceGameObject DisplayModelAddress => IsValidAddress(LobbyModelAddress) ? LobbyModelAddress : ProfileModelAddress;
            public AssetReferenceGameObject CombatModelAddress => IsValidAddress(BattleModelAddress) ? BattleModelAddress : DisplayModelAddress;
            public string SelectModelKey => ProfileModelAddressKey;
            public string DisplayModelKey => !string.IsNullOrWhiteSpace(LobbyModelAddressKey) ? LobbyModelAddressKey : ProfileModelAddressKey;
            public string CombatModelKey => !string.IsNullOrWhiteSpace(BattleModelAddressKey) ? BattleModelAddressKey : DisplayModelKey;
            public string ServerKey => string.IsNullOrWhiteSpace(ServerId) ? Id : ServerId;
            public string LocalizedDisplayName => BattleCharacterDatabase.GetLocalizedDisplayName(this);

            [Header("Battle Stats")]
            public BattleCharacterStats Stats;

            [Header("Unlock")]
            public bool IsStarterFree;
            public CharacterUnlockType UnlockType = CharacterUnlockType.Default;
            public CharacterPriceCurrencyType PriceCurrency = CharacterPriceCurrencyType.None;
            public int PriceAmount = 0;

            [Header("Catalog")]
            public int SortOrder = 0;
            public bool IsEnabled = true;

            public void Sanitize()
            {
                if (string.IsNullOrWhiteSpace(Id))
                    Id = $"{AnimalType}_{Gender}";

                Id = Id.Trim();

                if (string.IsNullOrWhiteSpace(ServerId))
                    ServerId = Id;
                else
                    ServerId = ServerId.Trim();

                if (string.IsNullOrWhiteSpace(DisplayName))
                    DisplayName = $"{AnimalType} {Gender}";

                Stats.Sanitize();
                PriceAmount = Mathf.Max(0, PriceAmount);

                if (IsStarterFree)
                {
                    UnlockType = CharacterUnlockType.Default;
                    PriceCurrency = CharacterPriceCurrencyType.None;
                    PriceAmount = 0;
                }

                if (UnlockType == CharacterUnlockType.Default)
                {
                    PriceCurrency = CharacterPriceCurrencyType.None;
                    PriceAmount = 0;
                }

                if (PriceAmount <= 0 && UnlockType != CharacterUnlockType.Default)
                    PriceCurrency = CharacterPriceCurrencyType.None;
            }

            public bool HasAnyModelReference()
            {
                return ProfileModelPrefab != null ||
                       LobbyModelPrefab != null ||
                       BattleModelPrefab != null ||
                       !string.IsNullOrWhiteSpace(ProfileModelAddressKey) ||
                       !string.IsNullOrWhiteSpace(LobbyModelAddressKey) ||
                       !string.IsNullOrWhiteSpace(BattleModelAddressKey) ||
                       IsValidAddress(ProfileModelAddress) ||
                       IsValidAddress(LobbyModelAddress) ||
                       IsValidAddress(BattleModelAddress);
            }

            public static bool IsValidAddress(AssetReferenceGameObject reference)
            {
                return reference != null && reference.RuntimeKeyIsValid();
            }
        }

        [Serializable]
        public sealed class RemoteCharacterCatalog
        {
            public bool success;
            public string version;
            public string checkedAt;
            public RemoteCharacterData[] characters;
        }

        [Serializable]
        public sealed class RemoteCharacterData
        {
            public string id;
            public string serverId;
            public string displayName;
            public string animalType;
            public string gender;
            public bool isEnabled = true;
            public bool isStarterFree;
            public string unlockType;
            public string priceCurrency;
            public int priceAmount;
            public int sortOrder;
            public RemoteCharacterStats stats;
            public string profileModelAddressKey;
            public string lobbyModelAddressKey;
            public string battleModelAddressKey;
        }

        [Serializable]
        public sealed class RemoteCharacterStats
        {
            public int maxHp;
            public int attack;
            public float armor;
            public float parryChance;
            public float critChance;
            public float critDamageMultiplier;
        }

        [Header("Catalog Settings")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool autoMarkFirstCharactersAsStarterFree = true;
        [SerializeField] private int starterFreeCount = 1;
        [SerializeField] private bool verboseLogs = true;

        [Header("Characters")]
        [SerializeField] private List<BattleCharacterData> characters = new List<BattleCharacterData>();

#if UNITY_EDITOR
        private const string CharacterFbxFolder = "Assets/Scripts/Mahjong/Sprites/FBX";
        private const string ProfileFbxPath = "Assets/Scripts/Mahjong/Sprites/FBX/Profile.fbx";
        private const string LobbyFbxPath = "Assets/Scripts/Mahjong/Sprites/FBX/Lobby.fbx";
        private const string BattleFbxPath = "Assets/Scripts/Mahjong/Sprites/FBX/Battle.fbx";
#endif

        private readonly Dictionary<string, BattleCharacterData> byId =
            new Dictionary<string, BattleCharacterData>(StringComparer.Ordinal);

        private readonly List<BattleCharacterData> sortedCache =
            new List<BattleCharacterData>();

        public IReadOnlyList<BattleCharacterData> Characters => sortedCache;
        public int CharacterCount => sortedCache.Count;

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

            InitializeRuntimeData();
        }

        private void OnEnable()
        {
            if (Instance == this)
                InitializeRuntimeData();
        }

        private void OnValidate()
        {
            InitializeRuntimeData();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public IReadOnlyList<BattleCharacterData> GetAllCharacters()
        {
            return sortedCache;
        }

        public List<BattleCharacterData> GetEnabledCharacters()
        {
            List<BattleCharacterData> result = new List<BattleCharacterData>();

            for (int i = 0; i < sortedCache.Count; i++)
            {
                BattleCharacterData data = sortedCache[i];
                if (data != null && data.IsEnabled)
                    result.Add(data);
            }

            return result;
        }

        public List<BattleCharacterData> GetStarterFreeCharacters()
        {
            List<BattleCharacterData> result = new List<BattleCharacterData>();

            for (int i = 0; i < sortedCache.Count; i++)
            {
                BattleCharacterData data = sortedCache[i];
                if (data != null && data.IsEnabled && data.IsStarterFree)
                    result.Add(data);
            }

            return result;
        }

        public bool TryGetCharacter(string id, out BattleCharacterData data)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                data = null;
                return false;
            }

            if (!byId.TryGetValue(id, out data))
                return false;

            return data != null && data.IsEnabled;
        }

        public BattleCharacterData GetCharacterOrNull(string id)
        {
            BattleCharacterData data;
            return TryGetCharacter(id, out data) ? data : null;
        }

        public bool TryGetCharacterByServerId(string serverId, out BattleCharacterData data)
        {
            return TryGetCharacter(serverId, out data);
        }

        public BattleCharacterData GetCharacterByIndex(int index)
        {
            if (index < 0 || index >= sortedCache.Count)
                return null;

            BattleCharacterData data = sortedCache[index];
            return data != null && data.IsEnabled ? data : null;
        }

        public static string GetLocalizedDisplayName(BattleCharacterData data)
        {
            if (data == null)
                return string.Empty;

            return GetLocalizedDisplayName(data.Id, data.DisplayName);
        }

        public static string GetLocalizedDisplayName(string id, string fallbackDisplayName)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                string key = $"battle.character.{id.Trim()}.name";
                string localized = GameLocalization.Text(key);
                if (!string.IsNullOrWhiteSpace(localized) && !string.Equals(localized, key, StringComparison.Ordinal))
                    return localized;
            }

            if (!string.IsNullOrWhiteSpace(fallbackDisplayName))
                return fallbackDisplayName.Trim();

            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }

        public BattleCharacterData GetFirstStarterCharacterOrNull()
        {
            for (int i = 0; i < sortedCache.Count; i++)
            {
                BattleCharacterData data = sortedCache[i];
                if (data != null && data.IsEnabled && data.IsStarterFree)
                    return data;
            }

            return null;
        }

        public BattleCharacterData FindByAnimalAndGender(
            CharacterAnimalType animalType,
            CharacterGender gender)
        {
            for (int i = 0; i < sortedCache.Count; i++)
            {
                BattleCharacterData data = sortedCache[i];

                if (data == null || !data.IsEnabled)
                    continue;

                if (data.AnimalType == animalType && data.Gender == gender)
                    return data;
            }

            return null;
        }

        public bool IsStarterFree(string id)
        {
            BattleCharacterData data = GetCharacterOrNull(id);
            return data != null && data.IsStarterFree;
        }

        public bool IsPaidCharacter(string id)
        {
            BattleCharacterData data = GetCharacterOrNull(id);

            if (data == null)
                return false;

            return !data.IsStarterFree &&
                   data.UnlockType != CharacterUnlockType.Default;
        }

        public bool TryApplyCharacterStatsToHub(string id)
        {
            BattleCharacterData data = GetCharacterOrNull(id);

            if (data == null || !BattleStatsHub.HasInstance)
                return false;

            BattleStatsHub.Instance.SetMaxHp(data.Stats.MaxHp, false);
            BattleStatsHub.Instance.SetAttack(data.Stats.Attack, false);
            BattleStatsHub.Instance.SetArmor(data.Stats.Armor, false);
            BattleStatsHub.Instance.SetParryChance(data.Stats.ParryChance, false);
            BattleStatsHub.Instance.SetCritChance(data.Stats.CritChance, false);
            BattleStatsHub.Instance.SetCritDamageMultiplier(
                data.Stats.CritDamageMultiplier,
                false);

            BattleStatsHub.Instance.PushSnapshot();
            return true;
        }

        public bool ApplyRemoteCatalog(RemoteCharacterCatalog catalog)
        {
            if (catalog == null || !catalog.success || catalog.characters == null)
                return false;

            bool changed = false;

            for (int i = 0; i < catalog.characters.Length; i++)
            {
                RemoteCharacterData remote = catalog.characters[i];
                if (remote == null || string.IsNullOrWhiteSpace(remote.id))
                    continue;

                BattleCharacterData data = FindOrCreateCharacter(remote.id.Trim());
                ApplyRemoteCharacter(data, remote);
                changed = true;
            }

            if (!changed)
                return false;

            InitializeRuntimeData();
            CatalogChanged?.Invoke();
            return true;
        }

        public void RebuildCache()
        {
            byId.Clear();
            sortedCache.Clear();

            if (characters == null)
                return;

            for (int i = 0; i < characters.Count; i++)
            {
                BattleCharacterData data = characters[i];
                if (data == null)
                    continue;

                if (string.IsNullOrWhiteSpace(data.Id))
                    data.Id = $"{data.AnimalType}_{data.Gender}";

                if (!byId.ContainsKey(data.Id))
                    byId.Add(data.Id, data);

                string serverKey = data.ServerKey;
                if (!string.IsNullOrWhiteSpace(serverKey) && !byId.ContainsKey(serverKey))
                    byId.Add(serverKey, data);

                sortedCache.Add(data);
            }

            sortedCache.Sort(CompareCharacters);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[BattleCharacterDatabase] Cache rebuilt. " +
                    $"Total={sortedCache.Count}, " +
                    $"Enabled={GetEnabledCharacters().Count}, " +
                    $"Starters={GetStarterFreeCharacters().Count}");
            }
        }

        private void InitializeRuntimeData()
        {
            SanitizeAll();
            RebuildCache();
        }

        private BattleCharacterData FindOrCreateCharacter(string id)
        {
            BattleCharacterData existing = FindExistingCharacter(characters, id);
            if (existing != null)
                return existing;

            BattleCharacterData created = new BattleCharacterData
            {
                Id = id,
                ServerId = id,
                DisplayName = id,
                IsEnabled = true,
            };

            characters.Add(created);
            return created;
        }

        private static void ApplyRemoteCharacter(BattleCharacterData data, RemoteCharacterData remote)
        {
            data.Id = remote.id.Trim();
            data.ServerId = string.IsNullOrWhiteSpace(remote.serverId) ? data.Id : remote.serverId.Trim();

            if (!string.IsNullOrWhiteSpace(remote.displayName))
                data.DisplayName = remote.displayName.Trim();

            data.AnimalType = ParseEnum(remote.animalType, data.AnimalType);
            data.Gender = ParseEnum(remote.gender, data.Gender);
            data.IsEnabled = remote.isEnabled;
            data.IsStarterFree = remote.isStarterFree;
            data.UnlockType = ParseEnum(remote.unlockType, data.UnlockType);
            data.PriceCurrency = ParseEnum(remote.priceCurrency, data.PriceCurrency);
            data.PriceAmount = Mathf.Max(0, remote.priceAmount);
            data.SortOrder = remote.sortOrder;

            if (remote.stats != null)
            {
                data.Stats = new BattleCharacterStats(
                    remote.stats.maxHp,
                    remote.stats.attack,
                    remote.stats.armor,
                    remote.stats.parryChance,
                    remote.stats.critChance,
                    remote.stats.critDamageMultiplier);
            }

            data.ProfileModelAddressKey = CleanKey(remote.profileModelAddressKey);
            data.LobbyModelAddressKey = CleanKey(remote.lobbyModelAddressKey);
            data.BattleModelAddressKey = CleanKey(remote.battleModelAddressKey);
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return Enum.TryParse(value.Trim(), true, out T parsed) ? parsed : fallback;
        }

        private static string CleanKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private void SanitizeAll()
        {
            starterFreeCount = Mathf.Max(0, starterFreeCount);

            if (characters == null)
                return;

            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] == null)
                    continue;

                characters[i].Sanitize();
            }

#if UNITY_EDITOR
            EditorAutoAssignSharedFbxAssets();
#endif

            if (autoMarkFirstCharactersAsStarterFree)
                ApplyStarterFreeRule();
        }

#if UNITY_EDITOR
        public void EditorAutoAssignSharedFbxAssets()
        {
            GameObject profileModel = AssetDatabase.LoadAssetAtPath<GameObject>(ProfileFbxPath);
            GameObject lobbyModel = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyFbxPath);
            GameObject battleModel = AssetDatabase.LoadAssetAtPath<GameObject>(BattleFbxPath);
            AnimationClip profileClip = LoadFirstAnimationClip(ProfileFbxPath);
            AnimationClip lobbyClip = LoadFirstAnimationClip(LobbyFbxPath);
            AnimationClip battleClip = LoadFirstAnimationClip(BattleFbxPath);

            bool changed = false;
            for (int i = 0; i < characters.Count; i++)
            {
                BattleCharacterData data = characters[i];
                if (data == null)
                    continue;

                GameObject resolvedProfileModel = LoadCharacterModel(data, "Profile") ?? profileModel;
                GameObject resolvedLobbyModel = LoadCharacterModel(data, "Lobby") ?? lobbyModel;
                GameObject resolvedBattleModel = LoadCharacterModel(data, "Battle") ?? battleModel;
                Texture2D resolvedTexture = LoadCharacterBaseColor(data);

                AnimationClip resolvedProfileClip = LoadFirstAnimationClip(resolvedProfileModel) ?? data.ProfileIdleAnimation ?? profileClip;
                AnimationClip resolvedLobbyClip = LoadFirstAnimationClip(resolvedLobbyModel) ?? data.LobbyIdleAnimation ?? lobbyClip;
                AnimationClip resolvedBattleClip = LoadFirstAnimationClip(resolvedBattleModel) ?? data.BattleIdleAnimation ?? battleClip;

                changed |= AssignIfDifferent(ref data.ProfileModelPrefab, resolvedProfileModel);
                changed |= AssignIfDifferent(ref data.LobbyModelPrefab, resolvedLobbyModel);
                changed |= AssignIfDifferent(ref data.BattleModelPrefab, resolvedBattleModel);

                changed |= AssignIfDifferent(ref data.ProfileIdleAnimation, resolvedProfileClip);
                changed |= AssignIfDifferent(ref data.LobbyIdleAnimation, resolvedLobbyClip);
                changed |= AssignIfDifferent(ref data.BattleIdleAnimation, resolvedBattleClip);

                if (resolvedTexture != null)
                    changed |= AssignIfDifferent(ref data.ModelTexture, resolvedTexture);

                if (data.UseSolidModelColor)
                {
                    data.UseSolidModelColor = false;
                    changed = true;
                }

                if (data.ModelTextureScale != Vector2.one)
                {
                    data.ModelTextureScale = Vector2.one;
                    changed = true;
                }

                if (data.ModelTextureOffset != Vector2.zero)
                {
                    data.ModelTextureOffset = Vector2.zero;
                    changed = true;
                }
            }

            if (changed)
                MarkDirtyInEditor();
        }

        private static GameObject LoadCharacterModel(BattleCharacterData data, string context)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id))
                return null;

            string id = data.Id.Trim();
            string compactId = id.Replace("_", string.Empty);
            string animal = data.AnimalType.ToString();
            string gender = data.Gender.ToString();
            string compactAnimalGender = animal + gender;
            string[] contextNames = string.Equals(context, "Profile", StringComparison.Ordinal)
                ? new[] { "Salute", "Profile", "Profil" }
                : new[] { context };

            for (int i = 0; i < contextNames.Length; i++)
            {
                string contextName = contextNames[i];
                GameObject model =
                    LoadModelAtPath($"{CharacterFbxFolder}/{id}_{contextName}.fbx") ??
                    LoadModelAtPath($"{CharacterFbxFolder}/{id}{contextName}.fbx") ??
                    LoadModelAtPath($"{CharacterFbxFolder}/{compactId}_{contextName}.fbx") ??
                    LoadModelAtPath($"{CharacterFbxFolder}/{compactId}{contextName}.fbx") ??
                    LoadModelAtPath($"{CharacterFbxFolder}/{animal}_{gender}_{contextName}.fbx") ??
                    LoadModelAtPath($"{CharacterFbxFolder}/{animal}{gender}{contextName}.fbx") ??
                    LoadModelAtPath($"{CharacterFbxFolder}/{compactAnimalGender}_{contextName}.fbx") ??
                    LoadModelAtPath($"{CharacterFbxFolder}/{compactAnimalGender}{contextName}.fbx");

                if (model != null)
                    return model;
            }

            return null;
        }

        private static Texture2D LoadCharacterBaseColor(BattleCharacterData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id))
                return null;

            string id = data.Id.Trim();
            string compactId = id.Replace("_", string.Empty);
            string compactIdWithExtraE = compactId.Replace("Female", "Femalee");
            string[] names =
            {
                $"{id}_basecolor",
                $"{id}_BaseColor",
                $"{id}_albedo",
                $"{id}_Albedo",
                $"{compactId}_basecolor",
                $"{compactId}_BaseColor",
                $"{compactId}_albedo",
                $"{compactId}_Albedo",
                $"{compactIdWithExtraE}_basecolor",
                $"{compactIdWithExtraE}_BaseColor",
                $"{compactIdWithExtraE}_albedo",
                $"{compactIdWithExtraE}_Albedo"
            };

            string[] extensions = { "JPEG", "JPG", "PNG", "jpeg", "jpg", "png" };
            for (int i = 0; i < names.Length; i++)
            {
                for (int e = 0; e < extensions.Length; e++)
                {
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        $"{CharacterFbxFolder}/{names[i]}.{extensions[e]}");
                    if (texture != null)
                        return texture;
                }
            }

            return null;
        }

        private static GameObject LoadModelAtPath(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static bool AssignIfMissing<T>(ref T target, T value) where T : UnityEngine.Object
        {
            if (target != null || value == null)
                return false;

            target = value;
            return true;
        }

        private static bool AssignIfDifferent<T>(ref T target, T value) where T : UnityEngine.Object
        {
            if (target == value || value == null)
                return false;

            target = value;
            return true;
        }

        private static AnimationClip LoadFirstAnimationClip(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            return LoadFirstAnimationClip(assets);
        }

        private static AnimationClip LoadFirstAnimationClip(GameObject model)
        {
            if (model == null)
                return null;

            string path = AssetDatabase.GetAssetPath(model);
            if (string.IsNullOrWhiteSpace(path))
                return null;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            return LoadFirstAnimationClip(assets);
        }

        private static AnimationClip LoadFirstAnimationClip(UnityEngine.Object[] assets)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    continue;

                return clip;
            }

            return null;
        }

        [ContextMenu("Bind Shared FBX Models And Animations")]
        private void BindSharedFbxModelsAndAnimationsFromContext()
        {
            EditorAutoAssignSharedFbxAssets();
            InitializeRuntimeData();
            MarkDirtyInEditor();
        }
#endif

        private void ApplyStarterFreeRule()
        {
            if (characters == null || characters.Count == 0)
                return;

            List<BattleCharacterData> temp =
                new List<BattleCharacterData>();

            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null && characters[i].IsEnabled)
                    temp.Add(characters[i]);
            }

            temp.Sort(CompareCharacters);

            for (int i = 0; i < temp.Count; i++)
            {
                BattleCharacterData data = temp[i];
                bool shouldBeStarter = i < starterFreeCount;

                data.IsStarterFree = shouldBeStarter;

                if (shouldBeStarter)
                {
                    data.UnlockType = CharacterUnlockType.Default;
                    data.PriceCurrency = CharacterPriceCurrencyType.None;
                    data.PriceAmount = 0;
                }
                else
                {
                    data.UnlockType = CharacterUnlockType.SoftCurrency;
                    data.PriceCurrency = CharacterPriceCurrencyType.OzAltin;
                    data.PriceAmount = DefaultFirstPaidCharacterPrice;
                }
            }

        }

        private static int CompareCharacters(
            BattleCharacterData a,
            BattleCharacterData b)
        {
            if (ReferenceEquals(a, b))
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            int sortCompare = a.SortOrder.CompareTo(b.SortOrder);
            if (sortCompare != 0)
                return sortCompare;

            return string.CompareOrdinal(a.Id, b.Id);
        }

        [ContextMenu("Battle Characters/Generate Default Characters")]
        private void GenerateDefaultCharactersFromContext()
        {
            GenerateDefaultCharacters();
        }

        private void GenerateDefaultCharacters()
        {
            List<BattleCharacterData> existing = characters;
            characters = new List<BattleCharacterData>(8);

            autoMarkFirstCharactersAsStarterFree = true;
            starterFreeCount = 1;

            AddDefaultCharacter(
                existing,
                "Tiger_Male",
                "Kaplan",
                CharacterAnimalType.Tiger,
                CharacterGender.Male,
                new BattleCharacterStats(100, 16, 0.05f, 0.05f, 0.12f, 1.7f),
                0);

            AddDefaultCharacter(
                existing,
                "Tiger_Female",
                "Di\u015Fi Kaplan",
                CharacterAnimalType.Tiger,
                CharacterGender.Female,
                new BattleCharacterStats(100, 16, 0.05f, 0.05f, 0.12f, 1.7f),
                1);

            AddDefaultCharacter(
                existing,
                "Fox_Male",
                "Tilki",
                CharacterAnimalType.Fox,
                CharacterGender.Male,
                new BattleCharacterStats(90, 14, 0.03f, 0.12f, 0.18f, 1.8f),
                2);

            AddDefaultCharacter(
                existing,
                "Fox_Female",
                "Di\u015Fi Tilki",
                CharacterAnimalType.Fox,
                CharacterGender.Female,
                new BattleCharacterStats(90, 14, 0.03f, 0.12f, 0.18f, 1.8f),
                3);

            AddDefaultCharacter(
                existing,
                "Wolf_Male",
                "Kurt",
                CharacterAnimalType.Wolf,
                CharacterGender.Male,
                new BattleCharacterStats(110, 15, 0.08f, 0.1f, 0.1f, 1.65f),
                4);

            AddDefaultCharacter(
                existing,
                "Wolf_Female",
                "Di\u015Fi Kurt",
                CharacterAnimalType.Wolf,
                CharacterGender.Female,
                new BattleCharacterStats(110, 15, 0.08f, 0.1f, 0.1f, 1.65f),
                5);

            AddDefaultCharacter(
                existing,
                "Bear_Male",
                "Ay\u0131",
                CharacterAnimalType.Bear,
                CharacterGender.Male,
                new BattleCharacterStats(130, 12, 0.15f, 0.08f, 0.06f, 1.5f),
                6);

            AddDefaultCharacter(
                existing,
                "Bear_Female",
                "Di\u015Fi Ay\u0131",
                CharacterAnimalType.Bear,
                CharacterGender.Female,
                new BattleCharacterStats(130, 12, 0.15f, 0.08f, 0.06f, 1.5f),
                7);

            InitializeRuntimeData();
            MarkDirtyInEditor();
        }

        private void AddDefaultCharacter(
            List<BattleCharacterData> existing,
            string id,
            string displayName,
            CharacterAnimalType animalType,
            CharacterGender gender,
            BattleCharacterStats stats,
            int sortOrder)
        {
            BattleCharacterData data = FindExistingCharacter(existing, id) ??
                                       new BattleCharacterData();

            data.Id = id;
            data.ServerId = id;
            data.DisplayName = displayName;
            data.AnimalType = animalType;
            data.Gender = gender;
            data.Stats = stats;
            data.IsStarterFree = sortOrder == 0;
            data.UnlockType = data.IsStarterFree
                ? CharacterUnlockType.Default
                : CharacterUnlockType.SoftCurrency;
            data.PriceCurrency = data.IsStarterFree
                ? CharacterPriceCurrencyType.None
                : CharacterPriceCurrencyType.OzAltin;
            data.PriceAmount = data.IsStarterFree ? 0 : DefaultFirstPaidCharacterPrice;
            data.SortOrder = sortOrder;
            data.IsEnabled = true;

            characters.Add(data);
        }

        private static BattleCharacterData FindExistingCharacter(
            List<BattleCharacterData> source,
            string id)
        {
            if (source == null || string.IsNullOrWhiteSpace(id))
                return null;

            for (int i = 0; i < source.Count; i++)
            {
                BattleCharacterData data = source[i];
                if (data == null)
                    continue;

                if (string.Equals(data.Id, id, StringComparison.Ordinal) ||
                    string.Equals(data.ServerId, id, StringComparison.Ordinal))
                    return data;

                string legacyId = GetLegacyCharacterId(id);
                if (!string.IsNullOrEmpty(legacyId) &&
                    (string.Equals(data.Id, legacyId, StringComparison.Ordinal) ||
                     string.Equals(data.ServerId, legacyId, StringComparison.Ordinal)))
                    return data;
            }

            return null;
        }

        private static string GetLegacyCharacterId(string id)
        {
            switch (id)
            {
                case "Fox_Male":
                    return "Monkey_Male";

                case "Fox_Female":
                    return "Monkey_Female";

                case "Wolf_Male":
                    return "Turtle_Male";

                case "Wolf_Female":
                    return "Turtle_Female";

                case "Bear_Male":
                    return "Panda_Male";

                case "Bear_Female":
                    return "Panda_Female";

                default:
                    return null;
            }
        }

        private void MarkDirtyInEditor()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Fill 8 Character IDs From Animals/Gender")]
        private void FillIdsAndNames()
        {
            GenerateDefaultCharacters();
        }

        [ContextMenu("Apply Starter Free Rule")]
        private void ApplyStarterFreeRuleFromContext()
        {
            InitializeRuntimeData();
            MarkDirtyInEditor();
        }

        private static string GetAnimalDisplayName(CharacterAnimalType animalType)
        {
            switch (animalType)
            {
                case CharacterAnimalType.Tiger:
                    return "Kaplan";

                case CharacterAnimalType.Fox:
                    return "Tilki";

                case CharacterAnimalType.Wolf:
                    return "Kurt";

                case CharacterAnimalType.Bear:
                    return "Ay\u0131";

                default:
                    return animalType.ToString();
            }
        }

        private static string GetGenderDisplayName(CharacterGender gender)
        {
            switch (gender)
            {
                case CharacterGender.Male:
                    return "М";

                case CharacterGender.Female:
                    return "Ж";

                default:
                    return gender.ToString();
            }
        }
    }
}
