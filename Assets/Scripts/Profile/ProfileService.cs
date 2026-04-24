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

        private const string BaseUrl = "https://dlsymbiosis.com";
        private const string ProfileResetId = "profiles_reset_20260422_onboarding_v1";
        private const string KeyAppliedProfileResetId = "symbiosis_applied_profile_reset_id";
        private const string KeyDeviceId = "symbiosis_server_device_id";
        private const string KeySessionToken = "symbiosis_server_session_token";
        private const string KeyRememberProfile = "symbiosis_remember_profile";
        private const string KeyRememberedAccountEmail = "symbiosis_remembered_account_email";
        private const string KeyRememberedAccountPassword = "symbiosis_remembered_account_password";

        public PlayerProfile Current => currentProfile;
        public string LastServerError => lastServerError;
        public bool RememberProfile => ShouldRememberProfile();
        public bool HasRememberedAccount => ShouldRememberProfile() && HasRememberedAccountCredentials();
        public bool CanAutoLoadProfile => ShouldRememberProfile() && (!string.IsNullOrWhiteSpace(GetSessionToken()) || HasProfile());
        public AccountSlotInfo[] LastAccountSlots { get; private set; } = Array.Empty<AccountSlotInfo>();
        public string LastAccountDynastyName { get; private set; } = string.Empty;

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
            ApplyProfileResetIfNeeded();

            // Автоматическая инициализация профиля при старте.
            RuntimeFileLogger.Write("[Startup] ProfileService Awake done. HasCurrent=" + (currentProfile != null));
        }

        public bool HasProfile()
        {
            ApplyProfileResetIfNeeded();
            return storage != null && storage.Exists();
        }

        public void LoadProfile()
        {
            if (storage == null)
                storage = new LocalProfileStorage();

            ApplyProfileResetIfNeeded();
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
                language = ToServerLanguage(language),
                token = GetSessionToken()
            };

            bool loaded = false;
            string error = string.Empty;

            yield return SendProfileRequest(
                "/profiles/bootstrap",
                JsonUtility.ToJson(payload),
                response =>
                {
                    ApplyServerUser(response.user);
                    StoreSessionToken(response.token);
                    loaded = true;
                    Debug.Log("[ProfileService] Server profile loaded");
                },
                requestError =>
                {
                    error = requestError;
                },
                logErrors: false
            );

            if (loaded || !IsProfileNotFoundError(error))
                yield break;

            ClearServerIdentityPrefs();
            lastServerError = string.Empty;

            ServerBootstrapRequest freshPayload = new ServerBootstrapRequest
            {
                deviceId = GetOrCreateDeviceId(),
                language = ToServerLanguage(language),
                token = string.Empty
            };

            yield return SendProfileRequest(
                "/profiles/bootstrap",
                JsonUtility.ToJson(freshPayload),
                response =>
                {
                    ApplyServerUser(response.user);
                    StoreSessionToken(response.token);
                    Debug.Log("[ProfileService] Server profile recreated after stale session");
                }
            );
        }

        public IEnumerator CompleteProfileOnServer(
            string dynastyName,
            string email,
            string password,
            string name,
            int slotIndex,
            int avatarId,
            int age,
            PlayerGender gender,
            GameLanguage language,
            bool rememberProfile,
            Action<bool, string> completed
        )
        {
            lastServerError = string.Empty;
            SetRememberProfile(rememberProfile);

            ServerCompleteProfileRequest payload = new ServerCompleteProfileRequest
            {
                deviceId = GetOrCreateDeviceId(),
                token = GetSessionToken(),
                dynastyName = string.IsNullOrWhiteSpace(dynastyName) ? string.Empty : dynastyName.Trim(),
                slotIndex = Mathf.Clamp(slotIndex, 1, 3),
                email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant(),
                password = password ?? string.Empty,
                nickname = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim(),
                age = Mathf.Clamp(age, 0, 120),
                gender = ToServerGender(gender),
                avatarId = Mathf.Max(0, avatarId),
                language = ToServerLanguage(language)
            };

            bool ok = false;
            string error = string.Empty;
            string registerPath = "/profiles/register";

            yield return SendProfileRequest(
                registerPath,
                JsonUtility.ToJson(payload),
                response =>
                {
                    ApplyServerUser(response.user);
                    StoreSessionToken(response.token);
                    ok = true;
                },
                requestError =>
                {
                    error = requestError;
                },
                logErrors: false
            );

            if (!ok && IsEndpointNotFoundError(error))
            {
                registerPath = "/register";
                error = string.Empty;
                lastServerError = string.Empty;

                yield return SendProfileRequest(
                    registerPath,
                    JsonUtility.ToJson(payload),
                    response =>
                    {
                        ApplyServerUser(response.user);
                        StoreSessionToken(response.token);
                        ok = true;
                    },
                    requestError =>
                    {
                        error = requestError;
                    },
                    logErrors: false
                );
            }

            if (!ok && IsProfileNotFoundError(error))
            {
                ClearServerIdentityPrefs();
                lastServerError = string.Empty;

                yield return LoadOrCreateServerProfile(language);

                if (currentProfile != null)
                {
                    payload.deviceId = GetOrCreateDeviceId();
                    payload.token = GetSessionToken();
                    error = string.Empty;

                    yield return SendProfileRequest(
                        registerPath,
                        JsonUtility.ToJson(payload),
                        response =>
                        {
                            ApplyServerUser(response.user);
                            StoreSessionToken(response.token);
                            ok = true;
                        },
                        requestError =>
                        {
                            error = requestError;
                        }
                    );
                }
            }

            if (ok)
                StoreRememberedAccount(payload.email, payload.password);

            completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error);
        }

        public IEnumerator LoginOnServer(
            string email,
            string password,
            int slotIndex,
            bool rememberProfile,
            Action<bool, string> completed
        )
        {
            lastServerError = string.Empty;
            SetRememberProfile(rememberProfile);

            ServerLoginRequest payload = new ServerLoginRequest
            {
                deviceId = GetOrCreateDeviceId(),
                email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant(),
                password = password ?? string.Empty,
                slotIndex = Mathf.Clamp(slotIndex, 1, 3)
            };

            bool ok = false;
            string error = string.Empty;

            yield return SendProfileRequest(
                "/login",
                JsonUtility.ToJson(payload),
                response =>
                {
                    ApplyServerUser(response.user);
                    StoreSessionToken(response.token);
                    ok = true;
                },
                requestError =>
                {
                    error = requestError;
                }
            );

            if (ok)
                StoreRememberedAccount(payload.email, payload.password);

            completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error);
        }

        public IEnumerator RequestPasswordRecovery(
            string email,
            GameLanguage language,
            Action<bool, string> completed
        )
        {
            lastServerError = string.Empty;

            ServerPasswordRecoveryRequest payload = new ServerPasswordRecoveryRequest
            {
                email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant(),
                language = ToServerLanguage(language)
            };

            bool ok = false;
            string error = string.Empty;

            yield return SendProfileRequest(
                "/account/recover-password",
                JsonUtility.ToJson(payload),
                response =>
                {
                    ok = true;
                },
                requestError =>
                {
                    error = requestError;
                },
                logErrors: false,
                requireUser: false
            );

            if (!ok && IsEndpointNotFoundError(error))
                error = "Password recovery endpoint is not configured.";

            completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error);
        }

        public bool TryGetRememberedAccountCredentials(out string email, out string password)
        {
            email = GetRememberedAccountEmail();
            password = GetRememberedAccountPassword();

            return ShouldRememberProfile() &&
                   !string.IsNullOrWhiteSpace(email) &&
                   !string.IsNullOrWhiteSpace(password);
        }

        public IEnumerator LoadAccountSlotsOnServer(
            string email,
            string password,
            Action<bool, string, AccountSlotInfo[], string> completed
        )
        {
            lastServerError = string.Empty;
            LastAccountSlots = Array.Empty<AccountSlotInfo>();
            LastAccountDynastyName = string.Empty;

            ServerLoginRequest payload = new ServerLoginRequest
            {
                deviceId = GetOrCreateDeviceId(),
                email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant(),
                password = password ?? string.Empty,
                slotIndex = 1
            };

            bool ok = false;
            string error = string.Empty;
            ServerProfileResponse slotResponse = null;

            yield return SendProfileRequest(
                "/account/slots",
                JsonUtility.ToJson(payload),
                response =>
                {
                    slotResponse = response;
                    ok = true;
                },
                requestError =>
                {
                    error = requestError;
                },
                requireUser: false
            );

            if (ok && slotResponse != null)
            {
                LastAccountDynastyName = slotResponse.account != null ? slotResponse.account.dynastyName ?? string.Empty : string.Empty;
                LastAccountSlots = ToAccountSlotInfo(slotResponse.profiles);
            }

            completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error, LastAccountSlots, LastAccountDynastyName);
        }

        public IEnumerator DeleteProfileSlotOnServer(
            string email,
            string password,
            int slotIndex,
            Action<bool, string, AccountSlotInfo[], string> completed
        )
        {
            lastServerError = string.Empty;

            ServerLoginRequest payload = new ServerLoginRequest
            {
                deviceId = GetOrCreateDeviceId(),
                email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant(),
                password = password ?? string.Empty,
                slotIndex = Mathf.Clamp(slotIndex, 1, 3)
            };

            bool ok = false;
            string error = string.Empty;
            ServerProfileResponse slotResponse = null;

            yield return SendProfileRequest(
                "/account/delete-slot",
                JsonUtility.ToJson(payload),
                response =>
                {
                    slotResponse = response;
                    ok = true;
                },
                requestError =>
                {
                    error = requestError;
                },
                requireUser: false
            );

            if (ok && slotResponse != null)
            {
                LastAccountDynastyName = slotResponse.account != null ? slotResponse.account.dynastyName ?? string.Empty : LastAccountDynastyName;
                LastAccountSlots = ToAccountSlotInfo(slotResponse.profiles);

                if (currentProfile != null && currentProfile.ProfileSlotIndex == payload.slotIndex)
                {
                    if (storage == null)
                        storage = new LocalProfileStorage();

                    storage.Delete();
                    PlayerPrefs.DeleteKey(KeySessionToken);
                    ResetProfileScopedCharacterSelection();
                    currentProfile = null;
                    PlayerPrefs.Save();
                    NotifyProfileChanged();
                }
            }

            completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error, LastAccountSlots, LastAccountDynastyName);
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
            ClearServerIdentityPrefs();
            ResetProfileScopedCharacterSelection();
            currentProfile = null;
            NotifyProfileChanged();

            Debug.Log("[ProfileService] Profile deleted");
        }

        public void Logout()
        {
            DeleteProfile();
            SetRememberProfile(false);
        }

        public void ChangeProfile()
        {
            if (storage == null)
                storage = new LocalProfileStorage();

            storage.Delete();
            PlayerPrefs.DeleteKey(KeySessionToken);
            ResetProfileScopedCharacterSelection();
            currentProfile = null;
            PlayerPrefs.Save();
            NotifyProfileChanged();
        }

        public void SetRememberProfile(bool remember)
        {
            PlayerPrefs.SetInt(KeyRememberProfile, remember ? 1 : 0);

            if (!remember)
            {
                PlayerPrefs.DeleteKey(KeySessionToken);
                ClearRememberedAccount();

                if (storage == null)
                    storage = new LocalProfileStorage();

                storage.Delete();
            }

            PlayerPrefs.Save();
        }

        private IEnumerator SendProfileRequest(
            string path,
            string json,
            Action<ServerProfileResponse> onSuccess,
            Action<string> onError = null,
            bool logErrors = true,
            bool requireUser = true
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
                if (logErrors)
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
                if (logErrors)
                    Debug.LogError("[ProfileService] " + lastServerError);
                onError?.Invoke(lastServerError);
                yield break;
            }

            if (response == null || !response.success || (requireUser && response.user == null))
            {
                lastServerError = response != null && !string.IsNullOrWhiteSpace(response.error)
                    ? response.error
                    : "Server profile response was empty.";
                if (logErrors)
                    Debug.LogError("[ProfileService] " + lastServerError);
                onError?.Invoke(lastServerError);
                yield break;
            }

            onSuccess?.Invoke(response);
        }

        private static AccountSlotInfo[] ToAccountSlotInfo(ServerProfileSlotDto[] profiles)
        {
            AccountSlotInfo[] slots =
            {
                AccountSlotInfo.Empty(1),
                AccountSlotInfo.Empty(2),
                AccountSlotInfo.Empty(3)
            };

            if (profiles == null)
                return slots;

            for (int i = 0; i < profiles.Length; i++)
            {
                ServerProfileSlotDto profile = profiles[i];
                if (profile == null)
                    continue;

                int index = Mathf.Clamp(profile.slotIndex <= 0 ? i + 1 : profile.slotIndex, 1, 3) - 1;
                slots[index] = new AccountSlotInfo
                {
                    SlotIndex = index + 1,
                    Nickname = profile.nickname ?? string.Empty,
                    PublicPlayerId = profile.publicPlayerId ?? string.Empty,
                    Age = Mathf.Clamp(profile.age, 0, 120),
                    Gender = FromServerGender(profile.gender),
                    AvatarId = Mathf.Max(0, profile.avatarId),
                    Occupied = profile.occupied || profile.profileCompleted || profile.id > 0,
                    InUseByOtherDevice = profile.inUseByOtherDevice,
                    LastActiveAt = profile.lastActiveAt ?? string.Empty
                };
            }

            return slots;
        }

        private void ApplyServerUser(ServerUserDto user)
        {
            if (user == null)
                return;

            if (currentProfile == null)
            {
                if (storage == null)
                    storage = new LocalProfileStorage();

                currentProfile = storage.Load();
                if (currentProfile == null)
                    currentProfile = new PlayerProfile();
            }

            currentProfile.EnsureData();
            currentProfile.SetOnlinePlayerId(user.id.ToString());
            currentProfile.SetGuestState(user.isGuest);
            currentProfile.DynastyName = user.dynastyName ?? string.Empty;
            currentProfile.DynastyId = user.dynastyId ?? string.Empty;
            currentProfile.ProfileSlotIndex = Mathf.Clamp(user.slotIndex <= 0 ? 1 : user.slotIndex, 1, 3);

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

            SaveIfRemembered();
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

        private void ApplyProfileResetIfNeeded()
        {
            string appliedResetId = PlayerPrefs.GetString(KeyAppliedProfileResetId, string.Empty);
            if (appliedResetId == ProfileResetId)
                return;

            if (storage == null)
                storage = new LocalProfileStorage();

            storage.Delete();
            ClearServerIdentityPrefs();
            ResetProfileScopedCharacterSelection();
            currentProfile = null;

            if (AppSettings.I != null)
                AppSettings.I.ClearLanguagePreference();

            PlayerPrefs.SetString(KeyAppliedProfileResetId, ProfileResetId);
            PlayerPrefs.Save();

            Debug.Log("[ProfileService] Applied profile reset: " + ProfileResetId);
            RuntimeFileLogger.Write("[Startup] Applied profile reset: " + ProfileResetId);
        }

        private static void ClearServerIdentityPrefs()
        {
            PlayerPrefs.DeleteKey(KeySessionToken);
            PlayerPrefs.DeleteKey(KeyDeviceId);
            ClearRememberedAccount();
        }

        private void SaveIfRemembered()
        {
            if (ShouldRememberProfile())
            {
                Save();
                return;
            }

            if (storage == null)
                storage = new LocalProfileStorage();

            storage.Delete();
        }

        private static bool ShouldRememberProfile()
        {
            bool hasStoredIdentity =
                !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(KeySessionToken, string.Empty)) ||
                HasRememberedAccountCredentials();

            int defaultValue = hasStoredIdentity ? 1 : 0;
            return PlayerPrefs.GetInt(KeyRememberProfile, defaultValue) == 1;
        }

        private static bool HasRememberedAccountCredentials()
        {
            return !string.IsNullOrWhiteSpace(GetRememberedAccountEmail()) &&
                   !string.IsNullOrWhiteSpace(GetRememberedAccountPassword());
        }

        private static string GetRememberedAccountEmail()
        {
            return PlayerPrefs.GetString(KeyRememberedAccountEmail, string.Empty);
        }

        private static string GetRememberedAccountPassword()
        {
            return PlayerPrefs.GetString(KeyRememberedAccountPassword, string.Empty);
        }

        private static void StoreRememberedAccount(string email, string password)
        {
            if (!ShouldRememberProfile())
                return;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            PlayerPrefs.SetString(KeyRememberedAccountEmail, email.Trim().ToLowerInvariant());
            PlayerPrefs.SetString(KeyRememberedAccountPassword, password);
            PlayerPrefs.Save();
        }

        private static void ClearRememberedAccount()
        {
            PlayerPrefs.DeleteKey(KeyRememberedAccountEmail);
            PlayerPrefs.DeleteKey(KeyRememberedAccountPassword);
        }

        private static bool IsProfileNotFoundError(string error)
        {
            return !string.IsNullOrWhiteSpace(error) &&
                   error.IndexOf("profile not found", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsEndpointNotFoundError(string error)
        {
            return !string.IsNullOrWhiteSpace(error) &&
                   (error.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    error.IndexOf("Cannot POST", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string GetOrCreateDeviceId()
        {
            string value = PlayerPrefs.GetString(KeyDeviceId, string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            string rawDeviceId = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrWhiteSpace(rawDeviceId) || rawDeviceId == SystemInfo.unsupportedIdentifier)
                rawDeviceId = Guid.NewGuid().ToString("N");

            value = rawDeviceId + ":" + ProfileResetId;

            PlayerPrefs.SetString(KeyDeviceId, value);
            PlayerPrefs.Save();
            return value;
        }

        private static string GetSessionToken()
        {
            return PlayerPrefs.GetString(KeySessionToken, string.Empty);
        }

        private static void StoreSessionToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            if (!ShouldRememberProfile())
                return;

            PlayerPrefs.SetString(KeySessionToken, token);
            PlayerPrefs.Save();
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
            public string token;
        }

        [Serializable]
        private sealed class ServerCompleteProfileRequest
        {
            public string deviceId;
            public string token;
            public string dynastyName;
            public int slotIndex;
            public string email;
            public string password;
            public string nickname;
            public int age;
            public string gender;
            public int avatarId;
            public string language;
        }

        [Serializable]
        private sealed class ServerLoginRequest
        {
            public string deviceId;
            public string email;
            public string password;
            public int slotIndex;
        }

        [Serializable]
        private sealed class ServerPasswordRecoveryRequest
        {
            public string email;
            public string language;
        }

        [Serializable]
        private sealed class ServerProfileResponse
        {
            public bool success;
            public string error;
            public string token;
            public ServerUserDto user;
            public ServerAccountDto account;
            public ServerProfileSlotDto[] profiles;
        }

        [Serializable]
        private sealed class ServerUserDto
        {
            public int id;
            public string email;
            public string nickname;
            public string publicPlayerId;
            public string deviceId;
            public int accountId;
            public string dynastyName;
            public string dynastyId;
            public int slotIndex;
            public string language;
            public int age;
            public string gender;
            public int avatarId;
            public bool profileCompleted;
            public bool isGuest;
            public string createdAt;
            public string updatedAt;
        }

        [Serializable]
        private sealed class ServerAccountDto
        {
            public int id;
            public string dynastyName;
            public string dynastyId;
            public string email;
        }

        [Serializable]
        private sealed class ServerProfileSlotDto
        {
            public int id;
            public int slotIndex;
            public string nickname;
            public string publicPlayerId;
            public int age;
            public string gender;
            public int avatarId;
            public bool profileCompleted;
            public bool occupied;
            public bool inUseByOtherDevice;
            public string lastActiveAt;
        }

        public struct AccountSlotInfo
        {
            public int SlotIndex;
            public string Nickname;
            public string PublicPlayerId;
            public int Age;
            public PlayerGender Gender;
            public int AvatarId;
            public bool Occupied;
            public bool InUseByOtherDevice;
            public string LastActiveAt;

            public static AccountSlotInfo Empty(int slotIndex)
            {
                return new AccountSlotInfo
                {
                    SlotIndex = Mathf.Clamp(slotIndex, 1, 3),
                    Nickname = string.Empty,
                    PublicPlayerId = string.Empty,
                    Age = 0,
                    Gender = PlayerGender.NotSpecified,
                    AvatarId = 0,
                    Occupied = false,
                    InUseByOtherDevice = false,
                    LastActiveAt = string.Empty
                };
            }
        }
    }
}
