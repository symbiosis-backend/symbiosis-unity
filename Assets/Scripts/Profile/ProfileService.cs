using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ProfileService : MonoBehaviour
    {
        public static ProfileService I { get; private set; }

        public static event Action ProfileChanged;

        private LocalProfileStorage storage;
        private PlayerProfile currentProfile;
        private string lastServerError = string.Empty;

        private const string BaseUrl = "http://91.99.176.77:8080";
        private const string KeyDeviceId = "symbiosis_server_device_id";

        public PlayerProfile Current => currentProfile;
        public string LastServerError => lastServerError;

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

        public IEnumerator LoadOrCreateServerProfile(GameLanguage language)
        {
            lastServerError = string.Empty;

            ServerBootstrapRequest payload = new ServerBootstrapRequest
            {
                deviceId = GetOrCreateDeviceId(),
                language = ToServerLanguage(language)
            };

            yield return SendProfileRequest(
                "/profiles/bootstrap",
                JsonUtility.ToJson(payload),
                response =>
                {
                    ApplyServerUser(response.user);
                    Debug.Log("[ProfileService] Server profile loaded");
                }
            );
        }

        public IEnumerator CompleteProfileOnServer(
            string name,
            int avatarId,
            int age,
            PlayerGender gender,
            GameLanguage language,
            Action<bool, string> completed
        )
        {
            lastServerError = string.Empty;

            ServerCompleteProfileRequest payload = new ServerCompleteProfileRequest
            {
                deviceId = GetOrCreateDeviceId(),
                nickname = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim(),
                age = Mathf.Clamp(age, 0, 120),
                gender = ToServerGender(gender),
                avatarId = Mathf.Max(0, avatarId),
                language = ToServerLanguage(language)
            };

            bool ok = false;
            string error = string.Empty;

            yield return SendProfileRequest(
                "/profiles/complete",
                JsonUtility.ToJson(payload),
                response =>
                {
                    ApplyServerUser(response.user);
                    ok = true;
                },
                requestError =>
                {
                    error = requestError;
                }
            );

            completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error);
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

        private IEnumerator SendProfileRequest(
            string path,
            string json,
            Action<ServerProfileResponse> onSuccess,
            Action<string> onError = null
        )
        {
            using UnityWebRequest request = new UnityWebRequest(BaseUrl + path, "POST");
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 12;

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            bool failed = request.result == UnityWebRequest.Result.ConnectionError ||
                          request.result == UnityWebRequest.Result.ProtocolError ||
                          request.result == UnityWebRequest.Result.DataProcessingError;

            if (failed)
            {
                lastServerError = string.IsNullOrWhiteSpace(responseText)
                    ? request.error
                    : ExtractError(responseText, request.error);
                Debug.LogError("[ProfileService] Server profile request failed: " + lastServerError);
                onError?.Invoke(lastServerError);
                yield break;
            }

            ServerProfileResponse response = null;
            try
            {
                response = JsonUtility.FromJson<ServerProfileResponse>(responseText);
            }
            catch (Exception ex)
            {
                lastServerError = "Invalid server response: " + ex.Message;
                Debug.LogError("[ProfileService] " + lastServerError);
                onError?.Invoke(lastServerError);
                yield break;
            }

            if (response == null || !response.success || response.user == null)
            {
                lastServerError = response != null && !string.IsNullOrWhiteSpace(response.error)
                    ? response.error
                    : "Server profile response was empty.";
                Debug.LogError("[ProfileService] " + lastServerError);
                onError?.Invoke(lastServerError);
                yield break;
            }

            onSuccess?.Invoke(response);
        }

        private void ApplyServerUser(ServerUserDto user)
        {
            if (user == null)
                return;

            if (currentProfile == null)
                currentProfile = new PlayerProfile();

            currentProfile.EnsureData();
            currentProfile.SetOnlinePlayerId(user.id.ToString());
            currentProfile.SetGuestState(false);

            string displayName = string.IsNullOrWhiteSpace(user.nickname) ? "Player" : user.nickname.Trim();
            string publicId = string.IsNullOrWhiteSpace(user.publicPlayerId)
                ? currentProfile.PublicPlayerId
                : user.publicPlayerId;

            currentProfile.CompleteProfile(
                displayName,
                Mathf.Max(0, user.avatarId),
                Mathf.Clamp(user.age, 0, 120),
                FromServerGender(user.gender),
                publicId
            );

            currentProfile.IsProfileCompleted = user.profileCompleted;
            currentProfile.CreatedAtUtc = string.IsNullOrWhiteSpace(user.createdAt)
                ? currentProfile.CreatedAtUtc
                : user.createdAt;
            currentProfile.LastLoginUtc = DateTime.UtcNow.ToString("O");
            currentProfile.EnsureData();

            Save();
            NotifyProfileChanged();
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

        private static string GetOrCreateDeviceId()
        {
            string value = PlayerPrefs.GetString(KeyDeviceId, string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrWhiteSpace(value) || value == SystemInfo.unsupportedIdentifier)
                value = Guid.NewGuid().ToString("N");

            PlayerPrefs.SetString(KeyDeviceId, value);
            PlayerPrefs.Save();
            return value;
        }

        private static string ToServerLanguage(GameLanguage language)
        {
            return language switch
            {
                GameLanguage.Russian => "russian",
                GameLanguage.English => "english",
                _ => "turkish"
            };
        }

        private static string ToServerGender(PlayerGender gender)
        {
            return gender switch
            {
                PlayerGender.Male => "male",
                PlayerGender.Female => "female",
                PlayerGender.Other => "other",
                _ => "not_specified"
            };
        }

        private static PlayerGender FromServerGender(string value)
        {
            return value switch
            {
                "male" => PlayerGender.Male,
                "female" => PlayerGender.Female,
                "other" => PlayerGender.Other,
                _ => PlayerGender.NotSpecified
            };
        }

        private static string ExtractError(string responseText, string fallback)
        {
            try
            {
                ServerProfileResponse response = JsonUtility.FromJson<ServerProfileResponse>(responseText);
                if (response != null && !string.IsNullOrWhiteSpace(response.error))
                    return response.error;
            }
            catch
            {
            }

            return string.IsNullOrWhiteSpace(fallback) ? responseText : fallback;
        }

        [Serializable]
        private sealed class ServerBootstrapRequest
        {
            public string deviceId;
            public string language;
        }

        [Serializable]
        private sealed class ServerCompleteProfileRequest
        {
            public string deviceId;
            public string nickname;
            public int age;
            public string gender;
            public int avatarId;
            public string language;
        }

        [Serializable]
        private sealed class ServerProfileResponse
        {
            public bool success;
            public string error;
            public ServerUserDto user;
        }

        [Serializable]
        private sealed class ServerUserDto
        {
            public int id;
            public string email;
            public string nickname;
            public string publicPlayerId;
            public string deviceId;
            public string language;
            public int age;
            public string gender;
            public int avatarId;
            public bool profileCompleted;
            public string createdAt;
            public string updatedAt;
        }
    }
}
