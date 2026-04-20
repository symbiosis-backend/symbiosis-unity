using System;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ProfileService : MonoBehaviour
    {
        public static ProfileService I { get; private set; }

        public static event Action ProfileChanged;

        private LocalProfileStorage storage;
        private PlayerProfile currentProfile;

        public PlayerProfile Current => currentProfile;

        private void Awake()
        {
            RuntimeFileLogger.Write("[Startup] ProfileService Awake begin");

            if (I != null && I != this)
            {
                RuntimeFileLogger.Write("[Startup] Duplicate ProfileService destroyed");
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
            RuntimeFileLogger.Write("[Startup] ProfileService persistent");

            storage = new LocalProfileStorage();
            RuntimeFileLogger.Write("[Startup] ProfileService storage ready");

            // Автоматическая инициализация профиля при старте.
            RuntimeFileLogger.Write("[Startup] ProfileService Awake done. HasCurrent=" + (currentProfile != null));
        }

        public bool HasProfile()
        {
            return storage != null && storage.Exists();
        }

        public void LoadProfile()
        {
            if (storage == null)
                storage = new LocalProfileStorage();

            currentProfile = storage.Load();

            if (currentProfile != null)
            {
                currentProfile.EnsureData();
                currentProfile.TouchLoginTime();
                Save();
                NotifyProfileChanged();
                Debug.Log("[ProfileService] Profile loaded");
            }
            else
            {
                Debug.LogWarning("[ProfileService] No profile found");
            }
        }

        public void CreateNewProfile()
        {
            ResetProfileScopedCharacterSelection();

            currentProfile = new PlayerProfile();
            currentProfile.EnsureData();
            Save();
            NotifyProfileChanged();

            Debug.Log("[ProfileService] New profile created");
        }

        public void CompleteProfile(string name, int avatarId)
        {
            CompleteProfile(name, avatarId, 0, PlayerGender.NotSpecified, string.Empty);
        }

        public void CompleteProfile(string name, int avatarId, int age, PlayerGender gender, string publicPlayerId)
        {
            if (currentProfile == null)
            {
                Debug.LogError("[ProfileService] Cannot complete profile: profile is null");
                return;
            }

            currentProfile.CompleteProfile(name, avatarId, age, gender, publicPlayerId);
            currentProfile.EnsureData();
            Save();
            NotifyProfileChanged();

            Debug.Log("[ProfileService] Profile completed");
        }

        public void Save()
        {
            if (currentProfile == null)
            {
                Debug.LogError("[ProfileService] Save failed: profile is null");
                return;
            }

            if (storage == null)
                storage = new LocalProfileStorage();

            currentProfile.EnsureData();
            storage.Save(currentProfile);
        }

        public void SetDisplayName(string name)
        {
            if (currentProfile == null)
                return;

            currentProfile.DisplayName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
            Save();
            NotifyProfileChanged();
        }

        public void SetAvatar(int avatarId)
        {
            if (currentProfile == null)
                return;

            currentProfile.AvatarId = Mathf.Max(0, avatarId);
            Save();
            NotifyProfileChanged();
        }

        public void SetPublicPlayerId(string publicPlayerId)
        {
            if (currentProfile == null)
                return;

            string normalized = PlayerProfile.NormalizePublicPlayerId(publicPlayerId);
            currentProfile.PublicPlayerId = string.IsNullOrWhiteSpace(normalized)
                ? PlayerProfile.GeneratePublicPlayerId()
                : normalized;
            Save();
            NotifyProfileChanged();
        }

        public void SetAge(int age)
        {
            if (currentProfile == null)
                return;

            currentProfile.Age = Mathf.Clamp(age, 0, 120);
            Save();
            NotifyProfileChanged();
        }

        public void SetGender(PlayerGender gender)
        {
            if (currentProfile == null)
                return;

            currentProfile.Gender = Enum.IsDefined(typeof(PlayerGender), gender)
                ? gender
                : PlayerGender.NotSpecified;
            Save();
            NotifyProfileChanged();
        }

        public bool TryAddFriendByPublicId(string publicPlayerId)
        {
            if (currentProfile == null)
                return false;

            bool added = currentProfile.TryAddFriend(publicPlayerId);
            if (!added)
                return false;

            Save();
            NotifyProfileChanged();
            return true;
        }

        public bool RemoveFriendByPublicId(string publicPlayerId)
        {
            if (currentProfile == null)
                return false;

            bool removed = currentProfile.RemoveFriend(publicPlayerId);
            if (!removed)
                return false;

            Save();
            NotifyProfileChanged();
            return true;
        }

        public void DeleteProfile()
        {
            if (storage == null)
                storage = new LocalProfileStorage();

            storage.Delete();
            ResetProfileScopedCharacterSelection();
            currentProfile = null;
            NotifyProfileChanged();

            Debug.Log("[ProfileService] Profile deleted");
        }

        public void NotifyProfileChanged()
        {
            ProfileChanged?.Invoke();
        }

        private void ResetProfileScopedCharacterSelection()
        {
            if (BattleCharacterSelectionService.HasInstance)
            {
                BattleCharacterSelectionService.Instance.ResetForNewProfile();
                return;
            }

            BattleCharacterSelectionService.ClearPrefs();
        }
    }
}
