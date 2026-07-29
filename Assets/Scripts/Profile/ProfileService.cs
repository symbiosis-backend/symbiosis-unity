using System;
using System.Collections;
using System.Text;
using MahjongGame.Networking;
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
        private string runtimeSessionToken = string.Empty;
        private bool sessionRecoveryInProgress;
        private bool lastSessionRecoverySucceeded;
        private string lastSessionRecoveryError = string.Empty;
        private float sessionRecoveryBlockedUntilRealtime;
        private int authenticationGeneration;
        private int sessionRecoveryGeneration = -1;
        private bool automaticSessionRecoveryDisabled;
        private int sessionRecoveryFailureCount;
        private bool explicitAuthenticationInProgress;

        private const int MaxAutomaticSessionRecoveryFailures = 3;

        private const string ProfileResetId = "profiles_reset_20260422_onboarding_v1";
        private const string KeyAppliedProfileResetId = "symbiosis_applied_profile_reset_id";
        private const string KeyDeviceId = "symbiosis_server_device_id";
        private const string KeySessionToken = "symbiosis_server_session_token";
        private const string KeyRememberProfile = "symbiosis_remember_profile";
        private const string KeyRememberedAccountEmail = "symbiosis_remembered_account_email";
        private const string KeyRememberedAccountPassword = "symbiosis_remembered_account_password";
        private const string OzkullarDeveloperAccountEmail = "ozkullar@developer.symbiosis.local";

        public PlayerProfile Current => currentProfile;
        public string LastServerError => lastServerError;
        public string CurrentSessionToken => GetSessionToken();
        public bool RememberProfile => ShouldRememberProfile();
        public bool HasRememberedAccount => ShouldRememberProfile() && HasRememberedAccountCredentials();
        public bool HasVerifiedOzkullarDeveloperSession =>
            currentProfile != null &&
            currentProfile.IsDeveloper &&
            !string.IsNullOrWhiteSpace(GetSessionToken()) &&
            string.Equals(
                CurrentAccountEmail,
                OzkullarDeveloperAccountEmail,
                StringComparison.OrdinalIgnoreCase);
        public string CurrentAccountEmail
        {
            get
            {
                if (currentProfile != null && !string.IsNullOrWhiteSpace(currentProfile.AccountEmail))
                    return currentProfile.AccountEmail.Trim().ToLowerInvariant();

                return GetRememberedAccountEmail();
            }
        }
        public bool CanAutoLoadProfile => ShouldRememberProfile() && (!string.IsNullOrWhiteSpace(GetSessionToken()) || HasProfile());
        public AccountSlotInfo[] LastAccountSlots { get; private set; } = Array.Empty<AccountSlotInfo>();
        public string LastAccountDynastyName { get; private set; } = string.Empty;

        private static string ScopedKey(string key)
        {
            return ClientProfileScope.AppendToKey(key);
        }

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
            ClearLegacyDeveloperCredentials();
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
                if (currentProfile.Energy != null)
                    currentProfile.Energy.Refill(DateTime.UtcNow.Ticks);
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
            int requestAuthenticationGeneration = authenticationGeneration;
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
                    if (authenticationGeneration != requestAuthenticationGeneration)
                        return;

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

            if (authenticationGeneration != requestAuthenticationGeneration)
                yield break;

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
                    if (authenticationGeneration != requestAuthenticationGeneration)
                        return;

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
            int registrationAuthenticationGeneration = BeginExplicitAuthentication(clearSessionToken: false);
            try
            {
                lastServerError = string.Empty;
                SetRememberProfile(rememberProfile);

            ServerCompleteProfileRequest payload = new ServerCompleteProfileRequest
            {
                deviceId = GetOrCreateDeviceId(),
                token = GetSessionToken(),
                dynastyName = string.IsNullOrWhiteSpace(dynastyName) ? string.Empty : dynastyName.Trim(),
                slotIndex = Mathf.Clamp(slotIndex, 1, 3),
                autoAssignSlot = true,
                email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant(),
                password = password ?? string.Empty,
                nickname = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim(),
                age = Mathf.Clamp(age, 0, 120),
                gender = ToServerGender(gender),
                avatarId = Mathf.Max(0, avatarId),
                isProfilePublic = currentProfile == null || currentProfile.IsProfilePublic,
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
                    if (authenticationGeneration != registrationAuthenticationGeneration)
                        return;

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

            if (authenticationGeneration != registrationAuthenticationGeneration)
            {
                completed?.Invoke(false, GameLocalization.Text("network.session_expired"));
                yield break;
            }

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
                        if (authenticationGeneration != registrationAuthenticationGeneration)
                            return;

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

                if (authenticationGeneration != registrationAuthenticationGeneration)
                {
                    completed?.Invoke(false, GameLocalization.Text("network.session_expired"));
                    yield break;
                }
            }

            if (!ok && IsProfileNotFoundError(error))
            {
                ClearServerIdentityPrefs();
                lastServerError = string.Empty;

                yield return LoadOrCreateServerProfile(language);

                if (authenticationGeneration != registrationAuthenticationGeneration)
                {
                    completed?.Invoke(false, GameLocalization.Text("network.session_expired"));
                    yield break;
                }

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
                            if (authenticationGeneration != registrationAuthenticationGeneration)
                                return;

                            ApplyServerUser(response.user);
                            StoreSessionToken(response.token);
                            ok = true;
                        },
                        requestError =>
                        {
                            error = requestError;
                        }
                    );

                    if (authenticationGeneration != registrationAuthenticationGeneration)
                    {
                        completed?.Invoke(false, GameLocalization.Text("network.session_expired"));
                        yield break;
                    }
                }
            }

            if (ok)
                StoreRememberedAccount(payload.email, payload.password);

                completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error);
            }
            finally
            {
                EndExplicitAuthentication(registrationAuthenticationGeneration);
            }
        }

        public IEnumerator LoginOnServer(
            string email,
            string password,
            int slotIndex,
            bool rememberProfile,
            Action<bool, string> completed
        )
        {
            int loginGeneration = BeginExplicitAuthentication();
            try
            {
                yield return LoginOnServerInternal(
                    email,
                    password,
                    slotIndex,
                    rememberProfile,
                    loginGeneration,
                    completed
                );
            }
            finally
            {
                EndExplicitAuthentication(loginGeneration);
            }
        }

        private IEnumerator LoginOnServerInternal(
            string email,
            string password,
            int slotIndex,
            bool rememberProfile,
            int expectedAuthenticationGeneration,
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
                    if (authenticationGeneration != expectedAuthenticationGeneration)
                        return;

                    ApplyServerUser(response.user);
                    StoreSessionToken(response.token);
                    ok = true;
                },
                requestError =>
                {
                    error = requestError;
                }
            );

            if (authenticationGeneration != expectedAuthenticationGeneration)
            {
                completed?.Invoke(false, GameLocalization.Text("network.session_expired"));
                yield break;
            }

            if (!ok && IsInvalidCredentialsError(error))
                ClearRememberedLogin();

            if (ok)
                StoreRememberedAccount(payload.email, payload.password);

            completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error);
        }

        public IEnumerator RecoverServerSession(string failedToken, Action<bool, string> completed = null)
        {
            string currentToken = GetSessionToken();
            if (!string.IsNullOrWhiteSpace(currentToken) &&
                (string.IsNullOrWhiteSpace(failedToken) ||
                 !string.Equals(currentToken, failedToken, StringComparison.Ordinal)))
            {
                completed?.Invoke(true, string.Empty);
                yield break;
            }

            if (automaticSessionRecoveryDisabled)
            {
                completed?.Invoke(false, string.IsNullOrWhiteSpace(lastSessionRecoveryError)
                    ? GameLocalization.Text("network.session_expired")
                    : lastSessionRecoveryError);
                yield break;
            }

            if (explicitAuthenticationInProgress)
            {
                completed?.Invoke(false, GameLocalization.Text("network.session_recovery_wait"));
                yield break;
            }

            if (sessionRecoveryInProgress)
            {
                int awaitedGeneration = sessionRecoveryGeneration;
                while (sessionRecoveryInProgress && sessionRecoveryGeneration == awaitedGeneration)
                    yield return null;

                currentToken = GetSessionToken();
                bool tokenWasRenewed = !string.IsNullOrWhiteSpace(currentToken) &&
                                       !string.Equals(currentToken, failedToken, StringComparison.Ordinal);
                completed?.Invoke(tokenWasRenewed || lastSessionRecoverySucceeded, tokenWasRenewed ? string.Empty : lastSessionRecoveryError);
                yield break;
            }

            if (Time.unscaledTime < sessionRecoveryBlockedUntilRealtime)
            {
                completed?.Invoke(false, string.IsNullOrWhiteSpace(lastSessionRecoveryError)
                    ? GameLocalization.Text("network.session_recovery_wait")
                    : lastSessionRecoveryError);
                yield break;
            }

            sessionRecoveryInProgress = true;
            sessionRecoveryGeneration = authenticationGeneration;
            lastSessionRecoverySucceeded = false;
            lastSessionRecoveryError = string.Empty;

            if (!TryGetRememberedAccountCredentials(out string email, out string password))
            {
                lastSessionRecoveryError = GameLocalization.Text("network.session_expired");
                sessionRecoveryBlockedUntilRealtime = Time.unscaledTime + 15f;
                automaticSessionRecoveryDisabled = true;
                sessionRecoveryFailureCount = MaxAutomaticSessionRecoveryFailures;
                sessionRecoveryInProgress = false;
                sessionRecoveryGeneration = -1;
                completed?.Invoke(false, lastSessionRecoveryError);
                yield break;
            }

            if (currentProfile == null)
            {
                lastSessionRecoveryError = GameLocalization.Text("network.session_expired");
                sessionRecoveryBlockedUntilRealtime = Time.unscaledTime + 15f;
                automaticSessionRecoveryDisabled = true;
                sessionRecoveryFailureCount = MaxAutomaticSessionRecoveryFailures;
                sessionRecoveryInProgress = false;
                sessionRecoveryGeneration = -1;
                completed?.Invoke(false, lastSessionRecoveryError);
                yield break;
            }

            int recoveryGeneration = authenticationGeneration;
            int slotIndex = Mathf.Clamp(currentProfile.ProfileSlotIndex <= 0 ? 1 : currentProfile.ProfileSlotIndex, 1, 3);

            bool recovered = false;
            string recoveryError = string.Empty;
            try
            {
                yield return LoginOnServerInternal(
                    email,
                    password,
                    slotIndex,
                    true,
                    recoveryGeneration,
                    (success, error) =>
                    {
                        recovered = success;
                        recoveryError = error ?? string.Empty;
                    }
                );
            }
            finally
            {
                if (sessionRecoveryGeneration == recoveryGeneration)
                {
                    sessionRecoveryInProgress = false;
                    sessionRecoveryGeneration = -1;
                }
            }

            if (authenticationGeneration != recoveryGeneration)
            {
                bool anotherAuthenticationSucceeded = !string.IsNullOrWhiteSpace(GetSessionToken());
                completed?.Invoke(
                    anotherAuthenticationSucceeded,
                    anotherAuthenticationSucceeded ? string.Empty : GameLocalization.Text("network.session_expired")
                );
                if (!anotherAuthenticationSucceeded)
                {
                    automaticSessionRecoveryDisabled = true;
                    sessionRecoveryFailureCount = MaxAutomaticSessionRecoveryFailures;
                }
                yield break;
            }

            lastSessionRecoverySucceeded = recovered;
            lastSessionRecoveryError = recovered
                ? string.Empty
                : GameLocalization.Text("network.session_expired");
            if (recovered)
            {
                sessionRecoveryFailureCount = 0;
                sessionRecoveryBlockedUntilRealtime = 0f;
                automaticSessionRecoveryDisabled = false;
            }
            else if (IsInvalidCredentialsError(recoveryError))
            {
                sessionRecoveryFailureCount = MaxAutomaticSessionRecoveryFailures;
                sessionRecoveryBlockedUntilRealtime = Time.unscaledTime + 15f;
                automaticSessionRecoveryDisabled = true;
            }
            else
            {
                sessionRecoveryFailureCount++;
                float retryDelay = 15f * Mathf.Pow(2f, Mathf.Clamp(sessionRecoveryFailureCount - 1, 0, 4));
                sessionRecoveryBlockedUntilRealtime = Time.unscaledTime + retryDelay;
                automaticSessionRecoveryDisabled = sessionRecoveryFailureCount >= MaxAutomaticSessionRecoveryFailures;
            }
            completed?.Invoke(lastSessionRecoverySucceeded, lastSessionRecoveryError);
        }

        public static bool IsSessionAuthenticationError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return false;

            return error.IndexOf("invalid session", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   error.IndexOf("session expired", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   error.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   error.IndexOf("401", StringComparison.OrdinalIgnoreCase) >= 0;
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

            if (!ok && IsInvalidCredentialsError(error))
                ClearRememberedLogin();

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
                    CancelPendingAuthentication();

                    if (storage == null)
                        storage = new LocalProfileStorage();

                    storage.Delete();
                    ClearSessionToken();
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

        public void SetProfilePublic(bool isPublic)
        {
            if (currentProfile == null)
                return;

            currentProfile.EnsureData();
            if (currentProfile.IsProfilePublic == isPublic)
                return;

            currentProfile.IsProfilePublic = isPublic;
            Save();
            NotifyProfileChanged();

            string token = GetSessionToken();
            if (!string.IsNullOrWhiteSpace(token))
                StartCoroutine(UpdateProfilePrivacyOnServer(token, isPublic));
        }

        private IEnumerator UpdateProfilePrivacyOnServer(string token, bool isPublic)
        {
            ProfilePrivacyRequest payload = new ProfilePrivacyRequest
            {
                token = token,
                isProfilePublic = isPublic
            };

            yield return SendProfileRequest(
                "/profiles/privacy",
                JsonUtility.ToJson(payload),
                response => ApplyServerUser(response.user),
                requestError => Debug.LogWarning("[ProfileService] Profile privacy update failed: " + requestError),
                logErrors: false
            );
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
            CancelPendingAuthentication();

            if (storage == null)
                storage = new LocalProfileStorage();

            storage.Delete();
            ClearServerIdentityPrefs();
            ResetProfileScopedCharacterSelection();
            currentProfile = null;
            NotifyProfileChanged();

            Debug.Log("[ProfileService] Profile deleted");
        }

        public IEnumerator DeleteAccountOnServer(string password, Action<bool, string> completed)
        {
            lastServerError = string.Empty;
            string token = GetSessionToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                lastServerError = "Invalid session";
                completed?.Invoke(false, lastServerError);
                yield break;
            }

            ServerDeleteAccountRequest payload = new ServerDeleteAccountRequest
            {
                token = token,
                password = password ?? string.Empty,
                confirmation = "DELETE"
            };

            bool ok = false;
            string error = string.Empty;
            yield return SendProfileRequest(
                "/account/delete",
                JsonUtility.ToJson(payload),
                _ => ok = true,
                requestError => error = requestError,
                requireUser: false
            );

            if (ok)
            {
                DeleteProfile();
                SetRememberProfile(false);
                LastAccountSlots = Array.Empty<AccountSlotInfo>();
                LastAccountDynastyName = string.Empty;
            }

            completed?.Invoke(ok, string.IsNullOrWhiteSpace(error) ? lastServerError : error);
        }

        public void Logout()
        {
            DeleteProfile();
            SetRememberProfile(false);
        }

        public void ClearRememberedLogin()
        {
            CancelPendingAuthentication();
            ClearSessionToken();
            PlayerPrefs.DeleteKey(ScopedKey(KeyDeviceId));
            ClearRememberedAccount();
            PlayerPrefs.SetInt(ScopedKey(KeyRememberProfile), 0);
            PlayerPrefs.Save();
        }

        public void ChangeProfile()
        {
            CancelPendingAuthentication();

            if (storage == null)
                storage = new LocalProfileStorage();

            storage.Delete();
            ClearSessionToken();
            ResetProfileScopedCharacterSelection();
            currentProfile = null;
            PlayerPrefs.Save();
            NotifyProfileChanged();
        }

        public void SetRememberProfile(bool remember)
        {
            PlayerPrefs.SetInt(ScopedKey(KeyRememberProfile), remember ? 1 : 0);

            if (!remember)
            {
                ClearSessionToken();
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
            string responseText = string.Empty;
            string requestError = string.Empty;
            bool failed = true;

            for (int i = 0; i < BackendEndpoints.BaseUrls.Length; i++)
            {
                using UnityWebRequest request = new UnityWebRequest(BackendEndpoints.BuildUrl(BackendEndpoints.BaseUrls[i], path), "POST");
                byte[] body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                BackendEndpoints.ApplyClientVersionHeaders(request);
                request.timeout = 12;

                yield return request.SendWebRequest();

                responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                requestError = request.error;
                failed = BackendEndpoints.RequestFailed(request);
                if (!failed || !BackendEndpoints.CanRetryWithFallback(request) || i == BackendEndpoints.BaseUrls.Length - 1)
                    break;
            }

            if (failed)
            {
                lastServerError = string.IsNullOrWhiteSpace(responseText)
                    ? requestError
                    : ExtractError(responseText, requestError);
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
                    LastActiveAt = profile.lastActiveAt ?? string.Empty,
                    CreatedAt = profile.createdAt ?? string.Empty
                };
            }

            return slots;
        }

        private void ApplyServerUser(ServerUserDto user)
        {
            if (user == null)
                return;

            EnsureLocalProfileForServerUser(user);

            currentProfile.EnsureData();
            currentProfile.SetOnlinePlayerId(user.id.ToString());
            currentProfile.SetGuestState(user.isGuest);
            currentProfile.AccountEmail = string.IsNullOrWhiteSpace(user.email) ? currentProfile.AccountEmail : user.email.Trim().ToLowerInvariant();
            currentProfile.DynastyName = user.dynastyName ?? string.Empty;
            currentProfile.DynastyId = user.dynastyId ?? string.Empty;
            currentProfile.AllianceTag = user.allianceTag ?? string.Empty;
            currentProfile.AllianceName = user.allianceName ?? string.Empty;
            currentProfile.AllianceLevel = Mathf.Max(0, user.allianceLevel);
            currentProfile.ProfileSlotIndex = Mathf.Clamp(user.slotIndex <= 0 ? 1 : user.slotIndex, 1, 3);
            currentProfile.IsProfilePublic = user.isProfilePublic;
            currentProfile.IsDeveloper = user.isDeveloper;
            currentProfile.HasInfiniteCurrency = user.hasInfiniteCurrency;

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

            if (CurrencyService.I != null)
            {
                CurrencyService.I.SetOzAltin(user.goldBalance);
                CurrencyService.I.SetOzAmetist(user.amethystBalance);
                CurrencyService.I.SetOzTile(user.ozTileBalance);
            }
            else
            {
                currentProfile.Currencies.SetAltin(user.goldBalance);
                currentProfile.Currencies.SetAmetist(user.amethystBalance);
                currentProfile.Currencies.SetTile(user.ozTileBalance);
            }

            Monetization.NoAdsService.ApplyServerNoAdsUntil(user.noAdsUntil);

            SaveIfRemembered();
            NotifyProfileChanged();
        }

        private void EnsureLocalProfileForServerUser(ServerUserDto user)
        {
            if (currentProfile == null)
            {
                if (storage == null)
                    storage = new LocalProfileStorage();

                currentProfile = storage.Load();
                if (currentProfile == null)
                    currentProfile = new PlayerProfile();
            }

            currentProfile.EnsureData();

            string serverOnlineId = user.id.ToString();
            bool hasLocalOnlineId = !string.IsNullOrWhiteSpace(currentProfile.OnlinePlayerId);
            bool isDifferentServerProfile = hasLocalOnlineId &&
                                            !string.Equals(currentProfile.OnlinePlayerId, serverOnlineId, StringComparison.Ordinal);

            if (!isDifferentServerProfile)
                return;

            Debug.Log("[ProfileService] Server profile changed. Starting clean local profile state.");
            ResetProfileScopedCharacterSelection();
            currentProfile = new PlayerProfile();
            currentProfile.EnsureData();
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
            string appliedResetId = PlayerPrefs.GetString(ScopedKey(KeyAppliedProfileResetId), string.Empty);
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

            PlayerPrefs.SetString(ScopedKey(KeyAppliedProfileResetId), ProfileResetId);
            PlayerPrefs.Save();

            Debug.Log("[ProfileService] Applied profile reset: " + ProfileResetId);
            RuntimeFileLogger.Write("[Startup] Applied profile reset: " + ProfileResetId);
        }

        private static void ClearServerIdentityPrefs()
        {
            ClearSessionToken();
            PlayerPrefs.DeleteKey(ScopedKey(KeyDeviceId));
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
                !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(ScopedKey(KeySessionToken), string.Empty)) ||
                HasRememberedAccountCredentials();

            int defaultValue = hasStoredIdentity ? 1 : 0;
            return PlayerPrefs.GetInt(ScopedKey(KeyRememberProfile), defaultValue) == 1;
        }

        private static bool HasRememberedAccountCredentials()
        {
            return !string.IsNullOrWhiteSpace(GetRememberedAccountEmail()) &&
                   !string.IsNullOrWhiteSpace(GetRememberedAccountPassword());
        }

        private static string GetRememberedAccountEmail()
        {
            return PlayerPrefs.GetString(ScopedKey(KeyRememberedAccountEmail), string.Empty);
        }

        private static string GetRememberedAccountPassword()
        {
            return PlayerPrefs.GetString(ScopedKey(KeyRememberedAccountPassword), string.Empty);
        }

        private static void StoreRememberedAccount(string email, string password)
        {
            if (!ShouldRememberProfile())
                return;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            string normalizedEmail = email.Trim().ToLowerInvariant();
            if (normalizedEmail == "ozkullar" || normalizedEmail == "ozkullar@developer.symbiosis.local")
            {
                // A privileged developer password must never be persisted in PlayerPrefs.
                ClearRememberedAccount();
                PlayerPrefs.Save();
                return;
            }

            PlayerPrefs.SetString(ScopedKey(KeyRememberedAccountEmail), normalizedEmail);
            PlayerPrefs.SetString(ScopedKey(KeyRememberedAccountPassword), password);
            PlayerPrefs.Save();
        }

        private static void ClearLegacyDeveloperCredentials()
        {
            string identifier = GetRememberedAccountEmail().Trim().ToLowerInvariant();
            if (identifier != "ozkullar" && identifier != "ozkullar@developer.symbiosis.local")
                return;

            ClearRememberedAccount();
            PlayerPrefs.Save();
        }

        private static void ClearRememberedAccount()
        {
            PlayerPrefs.DeleteKey(ScopedKey(KeyRememberedAccountEmail));
            PlayerPrefs.DeleteKey(ScopedKey(KeyRememberedAccountPassword));
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

        private static bool IsInvalidCredentialsError(string error)
        {
            return !string.IsNullOrWhiteSpace(error) &&
                   error.IndexOf("Invalid credentials", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetOrCreateDeviceId()
        {
            string value = PlayerPrefs.GetString(ScopedKey(KeyDeviceId), string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            string rawDeviceId = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrWhiteSpace(rawDeviceId) || rawDeviceId == SystemInfo.unsupportedIdentifier)
                rawDeviceId = Guid.NewGuid().ToString("N");

            value = rawDeviceId + ":" + ProfileResetId;

            PlayerPrefs.SetString(ScopedKey(KeyDeviceId), value);
            PlayerPrefs.Save();
            return value;
        }

        private static string GetSessionToken()
        {
            if (I != null && !string.IsNullOrWhiteSpace(I.runtimeSessionToken))
                return I.runtimeSessionToken;

            string storedToken = PlayerPrefs.GetString(ScopedKey(KeySessionToken), string.Empty);
            if (I != null)
                I.runtimeSessionToken = storedToken;
            return storedToken;
        }

        private static void StoreSessionToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            if (I != null)
            {
                I.runtimeSessionToken = token;
                I.sessionRecoveryBlockedUntilRealtime = 0f;
                I.lastSessionRecoverySucceeded = true;
                I.lastSessionRecoveryError = string.Empty;
                I.automaticSessionRecoveryDisabled = false;
                I.sessionRecoveryFailureCount = 0;
            }

            if (!ShouldRememberProfile())
                return;

            PlayerPrefs.SetString(ScopedKey(KeySessionToken), token);
            PlayerPrefs.Save();
        }

        private static void ClearSessionToken()
        {
            if (I != null)
            {
                I.runtimeSessionToken = string.Empty;
                I.lastSessionRecoverySucceeded = false;
                I.lastSessionRecoveryError = string.Empty;
                I.sessionRecoveryBlockedUntilRealtime = 0f;
            }

            PlayerPrefs.DeleteKey(ScopedKey(KeySessionToken));
        }

        private int BeginExplicitAuthentication(bool clearSessionToken = true)
        {
            CancelPendingAuthentication();
            if (clearSessionToken)
                ClearSessionToken();
            automaticSessionRecoveryDisabled = false;
            sessionRecoveryFailureCount = 0;
            explicitAuthenticationInProgress = true;
            return authenticationGeneration;
        }

        private void EndExplicitAuthentication(int completedGeneration)
        {
            if (authenticationGeneration == completedGeneration)
                explicitAuthenticationInProgress = false;
        }

        private void CancelPendingAuthentication()
        {
            authenticationGeneration++;
            sessionRecoveryInProgress = false;
            sessionRecoveryGeneration = -1;
            explicitAuthenticationInProgress = false;
            lastSessionRecoverySucceeded = false;
            lastSessionRecoveryError = string.Empty;
            sessionRecoveryBlockedUntilRealtime = 0f;
        }

        private static string ToServerLanguage(GameLanguage language)
        {
            return language switch
            {
                GameLanguage.Russian => "russian",
                GameLanguage.English => "english",
                GameLanguage.German => "german",
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
            public bool autoAssignSlot;
            public string email;
            public string password;
            public string nickname;
            public int age;
            public string gender;
            public int avatarId;
            public bool isProfilePublic;
            public string language;
        }

        [Serializable]
        private sealed class ProfilePrivacyRequest
        {
            public string token;
            public bool isProfilePublic;
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
        private sealed class ServerDeleteAccountRequest
        {
            public string token;
            public string password;
            public string confirmation;
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
            public string allianceTag;
            public string allianceName;
            public int allianceLevel;
            public int goldBalance;
            public int amethystBalance;
            public string noAdsUntil;
            public int ozTileBalance;
            public bool isDeveloper;
            public bool hasInfiniteCurrency;
            public int slotIndex;
            public string language;
            public int age;
            public string gender;
            public int avatarId;
            public bool isProfilePublic = true;
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
            public string createdAt;
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
            public string CreatedAt;

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
                    LastActiveAt = string.Empty,
                    CreatedAt = string.Empty
                };
            }
        }
    }
}
