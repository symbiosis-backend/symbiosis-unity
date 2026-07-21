using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class GlobalChatService : MonoBehaviour
    {
        public static GlobalChatService I { get; private set; }

        private const string BaseUrl = "https://dlsymbiosis.com";
        private const string KeySessionToken = "symbiosis_server_session_token";
        private const string DeveloperSupportSeenKeyPrefix = "symbiosis.developer_support.seen.";
        private const int MaxStoredDeveloperSupportSeenEntries = 512;
        private static readonly TimeSpan LiveMessageLifetime = TimeSpan.FromMinutes(3d);
        private static readonly TimeSpan FallbackPostInterval = TimeSpan.FromMinutes(1d);
        private const int FallbackSeedMessageCount = 3;
        public const string ChannelGlobal = "global";
        public const string ChannelMahjong = "mahjong";
        public const string ChannelDeveloperSupport = "developer_support";

        private readonly Dictionary<string, List<GlobalChatMessage>> messagesByChannel = new Dictionary<string, List<GlobalChatMessage>>();
        private readonly Dictionary<string, List<GlobalChatMessage>> fallbackMessagesByChannel = new Dictionary<string, List<GlobalChatMessage>>();
        private readonly Dictionary<string, long> lastMessageIdByChannel = new Dictionary<string, long>();
        private readonly Dictionary<string, int> fallbackCursorByChannel = new Dictionary<string, int>();
        private string currentChannel = ChannelGlobal;
        private string lastError = string.Empty;
        private long nextFallbackMessageId = -1L;
        private long nextDeveloperSupportBeforeId;
        private bool developerSupportPaginationInitialized;
        private string developerSupportUpdatedSince = string.Empty;
        private long developerSupportUpdatedAfterId;
        private DateTimeOffset developerSupportServerNowUtc;
        private float developerSupportServerClockReceivedAt;
        private bool hasDeveloperSupportServerClock;
        private readonly Dictionary<long, int> seenDeveloperSupportVersions = new Dictionary<long, int>();
        private string loadedDeveloperSupportSeenScope = string.Empty;
        private bool hasUnreadDeveloperSupportReaction;
        private Coroutine developerSupportNotificationRoutine;
        private bool developerSupportRefreshInProgress;

        public event Action MessagesChanged;
        public event Action<string> ErrorChanged;
        public event Action<bool> DeveloperSupportUnreadChanged;

        public IReadOnlyList<GlobalChatMessage> Messages
        {
            get
            {
                if (string.Equals(currentChannel, ChannelDeveloperSupport, StringComparison.Ordinal))
                {
                    PruneExpiredClosedDeveloperSupportMessages();
                    return GetMessages(currentChannel);
                }

                PruneExpiredMessages(currentChannel);
                List<GlobalChatMessage> serverMessages = GetMessages(currentChannel);
                if (serverMessages.Count > 0)
                    return serverMessages;

                EnsureFallbackMessages(currentChannel, DateTimeOffset.UtcNow);
                return GetFallbackMessages(currentChannel);
            }
        }
        public string CurrentChannel => currentChannel;
        public string CurrentChannelLabel => GetChannelLabel(currentChannel);
        public string LastError => lastError;
        public bool CanManageDeveloperSupport { get; private set; }
        public bool CanLoadOlderDeveloperSupport { get; private set; }
        public bool HasUnreadDeveloperSupportReaction => hasUnreadDeveloperSupportReaction;
        public GlobalChatMessage LatestActionableMessage => FindLatestActionableMessage();

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

        private void OnEnable()
        {
            if (developerSupportNotificationRoutine == null)
                developerSupportNotificationRoutine = StartCoroutine(DeveloperSupportNotificationRoutine());
        }

        private void OnDisable()
        {
            if (developerSupportNotificationRoutine != null)
            {
                StopCoroutine(developerSupportNotificationRoutine);
                developerSupportNotificationRoutine = null;
            }
            developerSupportRefreshInProgress = false;
        }

        private IEnumerator DeveloperSupportNotificationRoutine()
        {
            yield return new WaitForSecondsRealtime(3f);
            while (true)
            {
                PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
                if (profile != null && !profile.IsDeveloper && !string.IsNullOrWhiteSpace(GetSessionToken()))
                    yield return RefreshDeveloperSupport(50);
                yield return new WaitForSecondsRealtime(20f);
            }
        }

        public void SetChannel(string channel)
        {
            string normalized = NormalizeChannel(channel);
            if (string.Equals(currentChannel, normalized, StringComparison.Ordinal))
                return;

            currentChannel = normalized;
            if (!string.Equals(currentChannel, ChannelDeveloperSupport, StringComparison.Ordinal))
                PruneExpiredMessages(currentChannel);
            MessagesChanged?.Invoke();
        }

        public IEnumerator Refresh(int limit = 50)
        {
            string channel = currentChannel;
            if (string.Equals(channel, ChannelDeveloperSupport, StringComparison.Ordinal))
            {
                yield return RefreshDeveloperSupport(limit);
                yield break;
            }

            long lastMessageId = GetLastMessageId(channel);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string requestToken = GetSessionToken();
                string url = $"{BaseUrl}/chat/global?channel={UnityWebRequest.EscapeURL(channel)}&sinceId={lastMessageId}&limit={Mathf.Clamp(limit, 1, 100)}&language={GetTranslationLanguageCode()}";

                using UnityWebRequest request = UnityWebRequest.Get(url);
                if (!string.IsNullOrWhiteSpace(requestToken))
                    request.SetRequestHeader("X-Session-Token", requestToken);
                request.timeout = 10;

                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                ChatListResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<ChatListResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Invalid chat response.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(requestToken, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    bool changed = EnsureFallbackMessages(channel, DateTimeOffset.UtcNow);
                    SetError(ProfileService.IsSessionAuthenticationError(requestError)
                        ? (string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError)
                        : string.Empty);
                    if (changed)
                        MessagesChanged?.Invoke();
                    yield break;
                }

                if (response.messages != null)
                {
                    for (int i = 0; i < response.messages.Length; i++)
                        AddOrUpdate(channel, response.messages[i]);
                }

                PruneExpiredMessages(channel);
                TrimMessages(channel, 100);
                yield return ResolvePendingTranslations(channel);
                SetError(string.Empty);
                MessagesChanged?.Invoke();
                yield break;
            }
        }

        public IEnumerator Send(string text, Action<bool, string> completed = null)
        {
            string channel = currentChannel;
            string cleanText = ChatModerationFilter.Clean(text, out bool moderated);

            if (string.IsNullOrWhiteSpace(cleanText))
            {
                completed?.Invoke(false, "Message is empty.");
                yield break;
            }

            int characterLimit = string.Equals(channel, ChannelDeveloperSupport, StringComparison.Ordinal) ? 1000 : 240;
            if (cleanText.Length > characterLimit)
                cleanText = cleanText.Substring(0, characterLimit);

            if (moderated)
                SetError(GameLocalization.Text("chat.moderated"));

            if (string.Equals(channel, ChannelDeveloperSupport, StringComparison.Ordinal))
            {
                DeveloperSupportSendRequest supportPayload = new DeveloperSupportSendRequest
                {
                    text = cleanText
                };
                yield return PostDeveloperSupport(
                    $"{BaseUrl}/chat/developer/send",
                    supportPayload,
                    GameLocalization.Text("chat.support.sent"),
                    completed);
                yield break;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                string requestToken = GetSessionToken();
                if (string.IsNullOrWhiteSpace(requestToken))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0)
                        yield return RecoverSession(string.Empty, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    string missingError = string.IsNullOrWhiteSpace(recoveryError) ? GameLocalization.Text("network.session_expired") : recoveryError;
                    SetError(missingError);
                    completed?.Invoke(false, missingError);
                    yield break;
                }

                ChatSendRequest payload = new ChatSendRequest
                {
                    token = requestToken,
                    channel = channel,
                    text = cleanText
                };

                using UnityWebRequest request = new UnityWebRequest($"{BaseUrl}/chat/global/send", "POST");
                byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                ChatSendResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<ChatSendResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success || response.message == null))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Invalid chat response.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(requestToken, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    string finalError = string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError;
                    SetError(finalError);
                    completed?.Invoke(false, finalError);
                    yield break;
                }

                AddOrUpdate(channel, response.message);
                TrimMessages(channel, 100);
                SetError(string.Empty);
                MessagesChanged?.Invoke();
                completed?.Invoke(true, string.Empty);
                yield break;
            }
        }

        public IEnumerator AddDeveloperComment(GlobalChatMessage message, string text, Action<bool, string> completed = null)
        {
            string cleanText = ChatModerationFilter.Clean(text, out bool moderated);
            if (message == null || message.id <= 0 || string.IsNullOrWhiteSpace(cleanText))
            {
                string error = GameLocalization.Text("chat.support.comment_empty");
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            if (cleanText.Length > 800)
                cleanText = cleanText.Substring(0, 800);
            if (moderated)
                SetError(GameLocalization.Text("chat.moderated"));

            DeveloperSupportCommentRequest payload = new DeveloperSupportCommentRequest
            {
                text = cleanText
            };
            yield return PostDeveloperSupport(
                $"{BaseUrl}/chat/developer/{message.id}/comment",
                payload,
                GameLocalization.Text("chat.support.comment_added"),
                completed);
        }

        public IEnumerator SetDeveloperStatus(GlobalChatMessage message, string status, bool active = true, Action<bool, string> completed = null)
        {
            if (message == null || message.id <= 0 || !IsDeveloperSupportStatus(status))
            {
                string error = GameLocalization.Text("chat.support.invalid_status");
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            DeveloperSupportStatusRequest payload = new DeveloperSupportStatusRequest
            {
                status = status,
                active = active,
                version = message.version
            };
            yield return PostDeveloperSupport(
                $"{BaseUrl}/chat/developer/{message.id}/status",
                payload,
                GameLocalization.Text("chat.support.status_updated"),
                completed);
        }

        public IEnumerator VoteDeveloperSupport(GlobalChatMessage message, int vote, Action<bool, string> completed = null)
        {
            if (message == null || message.id <= 0 || vote < -1 || vote > 1)
            {
                string error = GameLocalization.Text("chat.support.vote.failed");
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            DeveloperSupportVoteRequest payload = new DeveloperSupportVoteRequest { vote = vote };
            string successKey = vote == 0 ? "chat.support.vote.removed" : "chat.support.vote.recorded";
            yield return PostDeveloperSupport(
                $"{BaseUrl}/chat/developer/{message.id}/vote",
                payload,
                GameLocalization.Text(successKey),
                completed);
        }

        private IEnumerator RefreshDeveloperSupport(int limit)
        {
            if (developerSupportRefreshInProgress)
                yield break;

            developerSupportRefreshInProgress = true;
            yield return RefreshDeveloperSupportCore(limit);
            developerSupportRefreshInProgress = false;
        }

        private IEnumerator RefreshDeveloperSupportCore(int limit)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string requestToken = GetSessionToken();
                if (string.IsNullOrWhiteSpace(requestToken))
                {
                    bool recoveredMissingSession = false;
                    string missingSessionError = string.Empty;
                    if (attempt == 0)
                        yield return RecoverSession(string.Empty, (success, error) => { recoveredMissingSession = success; missingSessionError = error; });
                    if (recoveredMissingSession)
                        continue;

                    string finalMissingSessionError = string.IsNullOrWhiteSpace(missingSessionError)
                        ? GameLocalization.Text("network.session_expired")
                        : missingSessionError;
                    SetError(finalMissingSessionError);
                    yield break;
                }

                string url = $"{BaseUrl}/chat/developer?limit={Mathf.Clamp(limit, 1, 100)}&language={GetTranslationLanguageCode()}";
                if (!string.IsNullOrWhiteSpace(developerSupportUpdatedSince))
                {
                    url += $"&updatedSince={UnityWebRequest.EscapeURL(developerSupportUpdatedSince)}";
                    if (developerSupportUpdatedAfterId > 0L)
                        url += $"&updatedAfterId={developerSupportUpdatedAfterId}";
                }
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("X-Session-Token", requestToken);
                request.timeout = 10;
                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                DeveloperSupportListResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<DeveloperSupportListResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Invalid developer support response.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(requestToken, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    SetError(string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError);
                    yield break;
                }

                if (response.messages != null)
                {
                    for (int i = 0; i < response.messages.Length; i++)
                        AddOrUpdate(ChannelDeveloperSupport, response.messages[i]);
                }

                CanManageDeveloperSupport = response.canManage;
                if (!response.isUpdatePage && !developerSupportPaginationInitialized)
                {
                    CanLoadOlderDeveloperSupport = response.hasMore;
                    nextDeveloperSupportBeforeId = response.nextBeforeId;
                    developerSupportPaginationInitialized = true;
                }
                if (!string.IsNullOrWhiteSpace(response.serverNow))
                {
                    developerSupportUpdatedSince = response.serverNow;
                    developerSupportUpdatedAfterId = response.serverAfterId;
                }
                UpdateDeveloperSupportServerClock(response.serverTime);
                PruneExpiredClosedDeveloperSupportMessages();
                UpdateDeveloperSupportUnreadState();
                yield return ResolvePendingTranslations(ChannelDeveloperSupport);
                SetError(string.Empty);
                MessagesChanged?.Invoke();
                yield break;
            }
        }

        public IEnumerator LoadOlderDeveloperSupport(int limit = 50)
        {
            if (!CanLoadOlderDeveloperSupport || nextDeveloperSupportBeforeId <= 0L)
                yield break;

            long beforeId = nextDeveloperSupportBeforeId;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string requestToken = GetSessionToken();
                if (string.IsNullOrWhiteSpace(requestToken))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0)
                        yield return RecoverSession(string.Empty, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    SetError(string.IsNullOrWhiteSpace(recoveryError) ? GameLocalization.Text("network.session_expired") : recoveryError);
                    yield break;
                }

                string url = $"{BaseUrl}/chat/developer?limit={Mathf.Clamp(limit, 1, 100)}&beforeId={beforeId}&language={GetTranslationLanguageCode()}";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("X-Session-Token", requestToken);
                request.timeout = 10;
                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                DeveloperSupportListResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<DeveloperSupportListResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Invalid developer support response.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(requestToken, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    SetError(string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError);
                    yield break;
                }

                if (response.messages != null)
                {
                    for (int i = 0; i < response.messages.Length; i++)
                        AddOrUpdate(ChannelDeveloperSupport, response.messages[i]);
                }

                CanManageDeveloperSupport = response.canManage;
                CanLoadOlderDeveloperSupport = response.hasMore;
                nextDeveloperSupportBeforeId = response.nextBeforeId;
                developerSupportPaginationInitialized = true;
                UpdateDeveloperSupportServerClock(response.serverTime);
                UpdateDeveloperSupportUnreadState();
                yield return ResolvePendingTranslations(ChannelDeveloperSupport);
                SetError(string.Empty);
                MessagesChanged?.Invoke();
                yield break;
            }
        }

        public IEnumerator RefreshForLanguageChange()
        {
            string channel = currentChannel;
            foreach (KeyValuePair<string, List<GlobalChatMessage>> entry in messagesByChannel)
            {
                List<GlobalChatMessage> messages = entry.Value;
                for (int i = 0; i < messages.Count; i++)
                    ClearMessageTranslation(messages[i]);
            }

            lastMessageIdByChannel[ChannelGlobal] = 0L;
            lastMessageIdByChannel[ChannelMahjong] = 0L;
            developerSupportUpdatedSince = string.Empty;
            developerSupportUpdatedAfterId = 0L;
            developerSupportPaginationInitialized = false;

            MessagesChanged?.Invoke();
            yield return Refresh(50);
        }

        private IEnumerator ResolvePendingTranslations(string channel)
        {
            string token = GetSessionToken();
            if (string.IsNullOrWhiteSpace(token))
                yield break;

            List<ChatTranslationRef> refs = new List<ChatTranslationRef>();
            List<GlobalChatMessage> messages = GetMessages(channel);
            bool developerSupport = string.Equals(channel, ChannelDeveloperSupport, StringComparison.Ordinal);
            for (int i = 0; i < messages.Count && refs.Count < 100; i++)
            {
                GlobalChatMessage message = messages[i];
                if (message == null || message.id <= 0L)
                    continue;

                if (NeedsTranslationResolve(message.translationStatus))
                {
                    refs.Add(new ChatTranslationRef
                    {
                        scope = developerSupport ? "support_request" : "global",
                        sourceId = message.id
                    });
                }

                if (!developerSupport || message.comments == null)
                    continue;
                for (int commentIndex = 0; commentIndex < message.comments.Length && refs.Count < 100; commentIndex++)
                {
                    DeveloperSupportComment comment = message.comments[commentIndex];
                    if (comment == null || comment.id <= 0L || !NeedsTranslationResolve(comment.translationStatus))
                        continue;
                    refs.Add(new ChatTranslationRef { scope = "support_comment", sourceId = comment.id });
                }
            }

            if (refs.Count == 0)
                yield break;

            ChatTranslationResolveRequest payload = new ChatTranslationResolveRequest
            {
                token = token,
                targetLanguage = GetTranslationLanguageCode(),
                refs = refs.ToArray()
            };
            using UnityWebRequest request = new UnityWebRequest($"{BaseUrl}/chat/translations/resolve", "POST");
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (RequestFailed(request))
                yield break;

            ChatTranslationResolveResponse response = ParseResponse<ChatTranslationResolveResponse>(request.downloadHandler.text);
            if (response == null || !response.success || response.translations == null)
                yield break;
            if (!IsTranslationForCurrentLanguage(response.targetLanguage))
                yield break;

            bool changed = ApplyTranslationEntries(messages, response.translations);
            if (changed)
                MessagesChanged?.Invoke();
        }

        private static bool ApplyTranslationEntries(List<GlobalChatMessage> messages, ChatTranslationEntry[] entries)
        {
            bool changed = false;
            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                ChatTranslationEntry entry = entries[entryIndex];
                if (entry == null || entry.sourceId <= 0L)
                    continue;

                for (int messageIndex = 0; messageIndex < messages.Count; messageIndex++)
                {
                    GlobalChatMessage message = messages[messageIndex];
                    if (message == null)
                        continue;
                    if ((entry.scope == "global" || entry.scope == "support_request") && message.id == entry.sourceId)
                    {
                        changed |= ApplyTranslation(message, entry);
                        break;
                    }
                    if (entry.scope != "support_comment" || message.comments == null)
                        continue;
                    for (int commentIndex = 0; commentIndex < message.comments.Length; commentIndex++)
                    {
                        DeveloperSupportComment comment = message.comments[commentIndex];
                        if (comment != null && comment.id == entry.sourceId)
                        {
                            changed |= ApplyTranslation(comment, entry);
                            break;
                        }
                    }
                }
            }
            return changed;
        }

        private static bool ApplyTranslation(GlobalChatMessage message, ChatTranslationEntry entry)
        {
            bool changed = message.translatedText != entry.translatedText ||
                           message.translatedLanguage != entry.translatedLanguage ||
                           message.sourceLanguage != entry.sourceLanguage ||
                           message.translationStatus != entry.translationStatus;
            message.translatedText = entry.translatedText;
            message.translatedLanguage = entry.translatedLanguage;
            message.sourceLanguage = entry.sourceLanguage;
            message.translationStatus = entry.translationStatus;
            message.isTranslated = IsUsefulTranslation(message.text, entry.translatedText, entry.translatedLanguage);
            return changed;
        }

        private static bool ApplyTranslation(DeveloperSupportComment comment, ChatTranslationEntry entry)
        {
            bool changed = comment.translatedText != entry.translatedText ||
                           comment.translatedLanguage != entry.translatedLanguage ||
                           comment.sourceLanguage != entry.sourceLanguage ||
                           comment.translationStatus != entry.translationStatus;
            comment.translatedText = entry.translatedText;
            comment.translatedLanguage = entry.translatedLanguage;
            comment.sourceLanguage = entry.sourceLanguage;
            comment.translationStatus = entry.translationStatus;
            comment.isTranslated = IsUsefulTranslation(comment.text, entry.translatedText, entry.translatedLanguage);
            return changed;
        }

        private static bool NeedsTranslationResolve(string status)
        {
            return string.Equals(status, "pending", StringComparison.Ordinal) ||
                   string.Equals(status, "processing", StringComparison.Ordinal);
        }

        private static bool IsUsefulTranslation(string original, string translated, string translatedLanguage)
        {
            return !string.IsNullOrWhiteSpace(translated) &&
                   !string.IsNullOrWhiteSpace(translatedLanguage) &&
                   IsTranslationForCurrentLanguage(translatedLanguage) &&
                   !string.Equals(original?.Trim(), translated.Trim(), StringComparison.Ordinal);
        }

        public static bool IsTranslationForCurrentLanguage(string translatedLanguage)
        {
            return !string.IsNullOrWhiteSpace(translatedLanguage) &&
                   string.Equals(translatedLanguage.Trim(), GetTranslationLanguageCode(), StringComparison.OrdinalIgnoreCase);
        }

        private static void ClearMessageTranslation(GlobalChatMessage message)
        {
            if (message == null)
                return;
            message.translatedText = string.Empty;
            message.translatedLanguage = string.Empty;
            message.translationStatus = "pending";
            message.isTranslated = false;
            if (message.comments == null)
                return;
            for (int i = 0; i < message.comments.Length; i++)
            {
                DeveloperSupportComment comment = message.comments[i];
                if (comment == null)
                    continue;
                comment.translatedText = string.Empty;
                comment.translatedLanguage = string.Empty;
                comment.translationStatus = "pending";
                comment.isTranslated = false;
            }
        }

        private static string GetTranslationLanguageCode()
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
            switch (language)
            {
                case GameLanguage.Russian: return "ru";
                case GameLanguage.English: return "en";
                case GameLanguage.German: return "de";
                default: return "tr";
            }
        }

        private IEnumerator PostDeveloperSupport<T>(string url, T payload, string successMessage, Action<bool, string> completed)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string requestToken = GetSessionToken();
                if (string.IsNullOrWhiteSpace(requestToken))
                {
                    bool recoveredMissingSession = false;
                    string missingSessionError = string.Empty;
                    if (attempt == 0)
                        yield return RecoverSession(string.Empty, (success, error) => { recoveredMissingSession = success; missingSessionError = error; });
                    if (recoveredMissingSession)
                        continue;

                    string finalMissingSessionError = string.IsNullOrWhiteSpace(missingSessionError)
                        ? GameLocalization.Text("network.session_expired")
                        : missingSessionError;
                    SetError(finalMissingSessionError);
                    completed?.Invoke(false, finalMissingSessionError);
                    yield break;
                }

                ApplyCurrentSessionToken(payload, requestToken);
                using UnityWebRequest request = new UnityWebRequest(url, "POST");
                byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;
                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                DeveloperSupportMessageResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<DeveloperSupportMessageResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success || response.message == null))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Invalid developer support response.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(requestToken, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    string finalError = string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError;
                    SetError(finalError);
                    completed?.Invoke(false, finalError);
                    yield break;
                }

                AddOrUpdate(ChannelDeveloperSupport, response.message);
                UpdateDeveloperSupportUnreadState();
                SetError(successMessage);
                MessagesChanged?.Invoke();
                completed?.Invoke(true, successMessage);
                yield break;
            }
        }

        private static bool IsDeveloperSupportStatus(string status)
        {
            return string.Equals(status, "voting", StringComparison.Ordinal) ||
                   string.Equals(status, "confirmed", StringComparison.Ordinal) ||
                   string.Equals(status, "under_review", StringComparison.Ordinal) ||
                   string.Equals(status, "rejected", StringComparison.Ordinal) ||
                   string.Equals(status, "closed", StringComparison.Ordinal);
        }

        private void PruneExpiredClosedDeveloperSupportMessages()
        {
            List<GlobalChatMessage> messages = GetMessages(ChannelDeveloperSupport);
            DateTimeOffset now = GetEstimatedDeveloperSupportServerNow();
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                GlobalChatMessage message = messages[i];
                if (message == null || !HasDeveloperSupportStatus(message, "closed") || string.IsNullOrWhiteSpace(message.closedAt))
                    continue;

                if (DateTimeOffset.TryParse(
                        message.closedAt,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out DateTimeOffset closedAt) && now - closedAt >= TimeSpan.FromHours(24d))
                {
                    messages.RemoveAt(i);
                }
            }
        }

        public static bool HasDeveloperSupportStatus(GlobalChatMessage message, string status)
        {
            if (message == null || string.IsNullOrWhiteSpace(status))
                return false;

            if (message.statuses != null)
            {
                for (int i = 0; i < message.statuses.Length; i++)
                {
                    if (string.Equals(message.statuses[i], status, StringComparison.Ordinal))
                        return true;
                }
            }

            return (message.statuses == null || message.statuses.Length == 0) &&
                   string.Equals(message.status, status, StringComparison.Ordinal);
        }

        public void MarkDeveloperSupportReactionsSeen()
        {
            EnsureDeveloperSupportSeenScope();
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null || profile.IsDeveloper || string.IsNullOrWhiteSpace(profile.PublicPlayerId))
            {
                SetDeveloperSupportUnread(false);
                return;
            }

            bool changed = false;
            List<GlobalChatMessage> messages = GetMessages(ChannelDeveloperSupport);
            for (int i = 0; i < messages.Count; i++)
            {
                GlobalChatMessage message = messages[i];
                bool ownReaction = IsOwnDeveloperSupportMessage(message, profile) && HasDeveloperReaction(message);
                bool activeVoting = HasDeveloperSupportStatus(message, "voting");
                if (!ownReaction && !activeVoting)
                    continue;

                int version = Mathf.Max(1, message.version);
                if (!seenDeveloperSupportVersions.TryGetValue(message.id, out int seenVersion) || seenVersion < version)
                {
                    seenDeveloperSupportVersions[message.id] = version;
                    changed = true;
                }
            }

            if (changed)
                SaveDeveloperSupportSeenState();
            UpdateDeveloperSupportUnreadState();
        }

        public bool IsDeveloperSupportMessageUnread(GlobalChatMessage message)
        {
            EnsureDeveloperSupportSeenScope();
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            bool ownReaction = IsOwnDeveloperSupportMessage(message, profile) && HasDeveloperReaction(message);
            bool activeVoting = HasDeveloperSupportStatus(message, "voting");
            if (profile == null || profile.IsDeveloper || (!ownReaction && !activeVoting))
            {
                return false;
            }

            int version = Mathf.Max(1, message.version);
            return !seenDeveloperSupportVersions.TryGetValue(message.id, out int seenVersion) || seenVersion < version;
        }

        private void UpdateDeveloperSupportUnreadState()
        {
            EnsureDeveloperSupportSeenScope();
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null || profile.IsDeveloper || string.IsNullOrWhiteSpace(profile.PublicPlayerId))
            {
                SetDeveloperSupportUnread(false);
                return;
            }

            bool unread = false;
            List<GlobalChatMessage> messages = GetMessages(ChannelDeveloperSupport);
            for (int i = 0; i < messages.Count; i++)
            {
                GlobalChatMessage message = messages[i];
                bool ownReaction = IsOwnDeveloperSupportMessage(message, profile) && HasDeveloperReaction(message);
                bool activeVoting = HasDeveloperSupportStatus(message, "voting");
                if (!ownReaction && !activeVoting)
                    continue;

                int version = Mathf.Max(1, message.version);
                if (!seenDeveloperSupportVersions.TryGetValue(message.id, out int seenVersion) || seenVersion < version)
                {
                    unread = true;
                    break;
                }
            }
            SetDeveloperSupportUnread(unread);
        }

        private static bool IsOwnDeveloperSupportMessage(GlobalChatMessage message, PlayerProfile profile)
        {
            return message != null && profile != null &&
                   !string.IsNullOrWhiteSpace(message.publicPlayerId) &&
                   string.Equals(message.publicPlayerId.Trim(), profile.PublicPlayerId.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasDeveloperReaction(GlobalChatMessage message)
        {
            return message != null &&
                   (message.version > 1 ||
                    (message.statuses != null && message.statuses.Length > 0) ||
                    !string.IsNullOrWhiteSpace(message.status) ||
                    (message.comments != null && message.comments.Length > 0));
        }

        private void EnsureDeveloperSupportSeenScope()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            string rawScope = profile != null
                ? (!string.IsNullOrWhiteSpace(profile.PublicPlayerId) ? profile.PublicPlayerId : profile.LocalProfileId)
                : string.Empty;
            string scope = NormalizeDeveloperSupportSeenScope(rawScope);
            if (string.Equals(scope, loadedDeveloperSupportSeenScope, StringComparison.Ordinal))
                return;

            loadedDeveloperSupportSeenScope = scope;
            seenDeveloperSupportVersions.Clear();
            if (string.IsNullOrWhiteSpace(scope))
                return;

            string json = PlayerPrefs.GetString(DeveloperSupportSeenKeyPrefix + scope, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return;
            try
            {
                DeveloperSupportSeenState state = JsonUtility.FromJson<DeveloperSupportSeenState>(json);
                if (state != null && state.entries != null)
                {
                    for (int i = 0; i < state.entries.Length; i++)
                    {
                        DeveloperSupportSeenEntry entry = state.entries[i];
                        if (entry != null && entry.requestId > 0L && entry.version > 0)
                            seenDeveloperSupportVersions[entry.requestId] = entry.version;
                    }
                }
            }
            catch
            {
                seenDeveloperSupportVersions.Clear();
            }
        }

        private void SaveDeveloperSupportSeenState()
        {
            if (string.IsNullOrWhiteSpace(loadedDeveloperSupportSeenScope))
                return;

            List<long> requestIds = new List<long>(seenDeveloperSupportVersions.Keys);
            requestIds.Sort((left, right) => right.CompareTo(left));
            int count = Mathf.Min(MaxStoredDeveloperSupportSeenEntries, requestIds.Count);
            DeveloperSupportSeenEntry[] entries = new DeveloperSupportSeenEntry[count];
            for (int i = 0; i < count; i++)
            {
                long requestId = requestIds[i];
                entries[i] = new DeveloperSupportSeenEntry
                {
                    requestId = requestId,
                    version = seenDeveloperSupportVersions[requestId]
                };
            }

            string json = JsonUtility.ToJson(new DeveloperSupportSeenState { entries = entries });
            PlayerPrefs.SetString(DeveloperSupportSeenKeyPrefix + loadedDeveloperSupportSeenScope, json);
            PlayerPrefs.Save();
        }

        private static string NormalizeDeveloperSupportSeenScope(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            string normalized = value.Trim().ToUpperInvariant();
            for (int i = 0; i < normalized.Length; i++)
            {
                char character = normalized[i];
                if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
                    builder.Append(character);
            }
            return builder.ToString();
        }

        private void SetDeveloperSupportUnread(bool unread)
        {
            if (hasUnreadDeveloperSupportReaction == unread)
                return;
            hasUnreadDeveloperSupportReaction = unread;
            DeveloperSupportUnreadChanged?.Invoke(unread);
        }

        private void UpdateDeveloperSupportServerClock(string serverTime)
        {
            if (!DateTimeOffset.TryParse(
                    serverTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset parsed))
            {
                return;
            }

            developerSupportServerNowUtc = parsed;
            developerSupportServerClockReceivedAt = Time.realtimeSinceStartup;
            hasDeveloperSupportServerClock = true;
        }

        private DateTimeOffset GetEstimatedDeveloperSupportServerNow()
        {
            if (!hasDeveloperSupportServerClock)
                return DateTimeOffset.UtcNow;

            double elapsedSeconds = Math.Max(0d, Time.realtimeSinceStartup - developerSupportServerClockReceivedAt);
            return developerSupportServerNowUtc.AddSeconds(elapsedSeconds);
        }

        public IEnumerator ReportMessage(GlobalChatMessage message, Action<bool, string> completed = null)
        {
            string token = GetSessionToken();
            if (message == null || (message.id <= 0 && message.userId <= 0))
            {
                string error = GameLocalization.Text("chat.no_report_target");
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            ChatReportRequest payload = new ChatReportRequest
            {
                token = token,
                channel = NormalizeChannel(message.channel),
                messageId = Math.Max(0, message.id),
                reportedUserId = Math.Max(0, message.userId),
                reason = "inappropriate_content",
                details = string.Empty
            };

            yield return PostSimple($"{BaseUrl}/chat/report", payload, GameLocalization.Text("chat.report_sent"), completed);
        }

        public IEnumerator BlockUser(GlobalChatMessage message, Action<bool, string> completed = null)
        {
            string token = GetSessionToken();
            if (message == null || message.userId <= 0)
            {
                string error = GameLocalization.Text("chat.no_block_target");
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            UserBlockRequest payload = new UserBlockRequest
            {
                token = token,
                blockedUserId = message.userId,
                reason = "blocked_from_chat"
            };

            yield return PostSimple($"{BaseUrl}/users/block", payload, GameLocalization.Text("chat.blocked"), (success, responseMessage) =>
            {
                if (success)
                {
                    RemoveMessagesFromUser(message.userId);
                    MessagesChanged?.Invoke();
                }

                completed?.Invoke(success, responseMessage);
            });
        }

        private List<GlobalChatMessage> GetMessages(string channel)
        {
            string normalized = NormalizeChannel(channel);
            if (!messagesByChannel.TryGetValue(normalized, out List<GlobalChatMessage> list))
            {
                list = new List<GlobalChatMessage>();
                messagesByChannel[normalized] = list;
            }

            return list;
        }

        private long GetLastMessageId(string channel)
        {
            return lastMessageIdByChannel.TryGetValue(NormalizeChannel(channel), out long value) ? value : 0L;
        }

        private List<GlobalChatMessage> GetFallbackMessages(string channel)
        {
            string normalized = NormalizeChannel(channel);
            if (!fallbackMessagesByChannel.TryGetValue(normalized, out List<GlobalChatMessage> list))
            {
                list = new List<GlobalChatMessage>();
                fallbackMessagesByChannel[normalized] = list;
            }

            return list;
        }

        private void SetLastMessageId(string channel, long value)
        {
            lastMessageIdByChannel[NormalizeChannel(channel)] = Math.Max(GetLastMessageId(channel), value);
        }

        private void AddOrUpdate(string channel, GlobalChatMessage message)
        {
            if (message == null || message.id <= 0)
                return;

            List<GlobalChatMessage> messages = GetMessages(channel);
            message.channel = NormalizeChannel(string.IsNullOrWhiteSpace(message.channel) ? channel : message.channel);

            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i] != null && messages[i].id == message.id)
                {
                    messages[i] = message;
                    SetLastMessageId(channel, message.id);
                    return;
                }
            }

            messages.Add(message);
            messages.Sort((a, b) => a.id.CompareTo(b.id));
            SetLastMessageId(channel, message.id);
        }

        private void TrimMessages(string channel, int maxCount)
        {
            List<GlobalChatMessage> messages = GetMessages(channel);
            while (messages.Count > maxCount)
                messages.RemoveAt(0);
        }

        private GlobalChatMessage FindLatestActionableMessage()
        {
            List<GlobalChatMessage> messages = GetMessages(currentChannel);
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                GlobalChatMessage message = messages[i];
                if (message != null && message.id > 0 && message.userId > 0)
                    return message;
            }

            return null;
        }

        private void RemoveMessagesFromUser(int userId)
        {
            if (userId <= 0)
                return;

            foreach (List<GlobalChatMessage> messages in messagesByChannel.Values)
                messages.RemoveAll(message => message != null && message.userId == userId);
        }

        private void PruneExpiredMessages(string channel)
        {
            if (string.Equals(NormalizeChannel(channel), ChannelDeveloperSupport, StringComparison.Ordinal))
                return;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            PruneExpiredMessageList(GetMessages(channel), now);
            PruneExpiredMessageList(GetFallbackMessages(channel), now);
        }

        private static bool PruneExpiredMessageList(List<GlobalChatMessage> messages, DateTimeOffset now)
        {
            bool changed = false;
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (IsExpired(messages[i], now))
                {
                    messages.RemoveAt(i);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool IsExpired(GlobalChatMessage message, DateTimeOffset now)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.createdAt))
                return false;

            if (!DateTimeOffset.TryParse(
                    message.createdAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset createdAt))
            {
                return false;
            }

            return now - createdAt > LiveMessageLifetime;
        }

        private bool EnsureFallbackMessages(string channel, DateTimeOffset now)
        {
            string normalized = NormalizeChannel(channel);
            if (string.Equals(normalized, ChannelDeveloperSupport, StringComparison.Ordinal))
                return false;

            if (GetMessages(normalized).Count > 0)
                return false;

            List<GlobalChatMessage> messages = GetFallbackMessages(normalized);
            bool changed = PruneExpiredMessageList(messages, now);
            int cursor = fallbackCursorByChannel.TryGetValue(normalized, out int value) ? value : 0;
            if (messages.Count == 0)
            {
                int seedCount = FallbackSeedMessageCount;
                for (int index = seedCount - 1; index >= 0; index--)
                {
                    FallbackLine line = CreateNaturalFallbackLine(normalized);
                    messages.Add(CreateFallbackMessage(normalized, line, now - TimeSpan.FromMinutes(index)));
                    cursor++;
                }

                fallbackCursorByChannel[normalized] = cursor;
                return true;
            }

            DateTimeOffset lastCreatedAt = GetMessageTimestamp(messages[messages.Count - 1], now);
            while (now - lastCreatedAt >= FallbackPostInterval)
            {
                FallbackLine line = CreateNaturalFallbackLine(normalized);
                lastCreatedAt = lastCreatedAt.Add(FallbackPostInterval);
                messages.Add(CreateFallbackMessage(normalized, line, lastCreatedAt));
                cursor++;
                changed = true;
            }

            fallbackCursorByChannel[normalized] = cursor;
            return changed;
        }

        private GlobalChatMessage CreateFallbackMessage(string channel, FallbackLine line, DateTimeOffset createdAt)
        {
            return new GlobalChatMessage
            {
                id = nextFallbackMessageId--,
                userId = 0,
                nickname = line.Nickname,
                publicPlayerId = string.Empty,
                isProfilePublic = false,
                channel = NormalizeChannel(channel),
                text = line.Text,
                createdAt = createdAt.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)
            };
        }

        private static DateTimeOffset GetMessageTimestamp(GlobalChatMessage message, DateTimeOffset fallback)
        {
            if (message != null && !string.IsNullOrWhiteSpace(message.createdAt) &&
                DateTimeOffset.TryParse(
                    message.createdAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset createdAt))
            {
                return createdAt;
            }

            return fallback;
        }

        private static FallbackLine CreateNaturalFallbackLine(string channel)
        {
            NaturalChatLine line = NaturalBotProfileGenerator.CreateChatLine(NormalizeChannel(channel));
            string nickname = string.IsNullOrWhiteSpace(line.Profile.Nickname) ? "Player" : line.Profile.Nickname;
            string text = string.IsNullOrWhiteSpace(line.Text) ? "Ready for a match." : line.Text;
            return new FallbackLine(nickname, text);
        }

        private void SetError(string value)
        {
            if (value == null)
                value = string.Empty;

            if (string.Equals(lastError, value, StringComparison.Ordinal))
                return;

            lastError = value;
            ErrorChanged?.Invoke(lastError);
        }

        private static string GetSessionToken()
        {
            string token = ProfileService.I != null ? ProfileService.I.CurrentSessionToken : string.Empty;
            if (!string.IsNullOrWhiteSpace(token))
                return token;

            return PlayerPrefs.GetString(ClientProfileScope.AppendToKey(KeySessionToken), string.Empty);
        }

        private static IEnumerator RecoverSession(string failedToken, Action<bool, string> completed)
        {
            if (ProfileService.I == null)
            {
                completed?.Invoke(false, GameLocalization.Text("network.session_expired"));
                yield break;
            }

            yield return ProfileService.I.RecoverServerSession(failedToken, completed);
        }

        private static string NormalizeChannel(string channel)
        {
            if (string.Equals(channel, ChannelDeveloperSupport, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(channel, "developer", StringComparison.OrdinalIgnoreCase))
            {
                return ChannelDeveloperSupport;
            }

            return string.Equals(channel, ChannelMahjong, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(channel, "madonna", StringComparison.OrdinalIgnoreCase)
                ? ChannelMahjong
                : ChannelGlobal;
        }

        private static string GetChannelLabel(string channel)
        {
            string normalized = NormalizeChannel(channel);
            if (string.Equals(normalized, ChannelDeveloperSupport, StringComparison.Ordinal))
                return "Developer Support";

            return string.Equals(normalized, ChannelMahjong, StringComparison.Ordinal) ? "Mahjong" : "Global";
        }

        private static bool RequestFailed(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.ProtocolError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
        }

        private static string ReadError(UnityWebRequest request)
        {
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                ChatErrorResponse response = ParseResponse<ChatErrorResponse>(responseText);
                if (response != null && !string.IsNullOrWhiteSpace(response.error))
                    return response.error;
            }

            return string.IsNullOrWhiteSpace(request.error) ? "Chat request failed." : request.error;
        }

        private IEnumerator PostSimple<T>(string url, T payload, string successMessage, Action<bool, string> completed)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string requestToken = GetSessionToken();
                if (string.IsNullOrWhiteSpace(requestToken))
                {
                    bool recoveredMissingSession = false;
                    string missingSessionError = string.Empty;
                    if (attempt == 0)
                        yield return RecoverSession(string.Empty, (success, error) => { recoveredMissingSession = success; missingSessionError = error; });
                    if (recoveredMissingSession)
                        continue;

                    string finalMissingSessionError = string.IsNullOrWhiteSpace(missingSessionError)
                        ? GameLocalization.Text("network.session_expired")
                        : missingSessionError;
                    SetError(finalMissingSessionError);
                    completed?.Invoke(false, finalMissingSessionError);
                    yield break;
                }

                ApplyCurrentSessionToken(payload, requestToken);
                using UnityWebRequest request = new UnityWebRequest(url, "POST");
                byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                ChatErrorResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<ChatErrorResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Chat request failed.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(requestToken, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    string finalError = string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError;
                    SetError(finalError);
                    completed?.Invoke(false, finalError);
                    yield break;
                }

                SetError(successMessage);
                completed?.Invoke(true, successMessage);
                yield break;
            }
        }

        private static void ApplyCurrentSessionToken<T>(T payload, string token)
        {
            if (payload is ChatReportRequest reportRequest)
                reportRequest.token = token;
            else if (payload is UserBlockRequest blockRequest)
                blockRequest.token = token;
            else if (payload is DeveloperSupportSendRequest supportSendRequest)
                supportSendRequest.token = token;
            else if (payload is DeveloperSupportCommentRequest supportCommentRequest)
                supportCommentRequest.token = token;
            else if (payload is DeveloperSupportStatusRequest supportStatusRequest)
                supportStatusRequest.token = token;
            else if (payload is DeveloperSupportVoteRequest supportVoteRequest)
                supportVoteRequest.token = token;
        }

        private static T ParseResponse<T>(string json) where T : class
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return null;
            }
        }

        [Serializable]
        public sealed class GlobalChatMessage
        {
            public long id;
            public int userId;
            public string nickname;
            public string allianceTag;
            public int allianceLevel;
            public string publicPlayerId;
            public bool isProfilePublic = true;
            public bool isDeveloper;
            public string channel;
            public string text;
            public string sourceLanguage;
            public string translatedText;
            public string translatedLanguage;
            public string translationStatus;
            public bool isTranslated;
            public string createdAt;
            public string status;
            public string[] statuses;
            public int version;
            public string updatedAt;
            public string closedAt;
            public int likeCount;
            public int dislikeCount;
            public int myVote;
            public DeveloperSupportComment[] comments;
        }

        [Serializable]
        public sealed class DeveloperSupportComment
        {
            public long id;
            public long requestId;
            public int developerUserId;
            public string developerNickname;
            public bool isDeveloper;
            public string text;
            public string sourceLanguage;
            public string translatedText;
            public string translatedLanguage;
            public string translationStatus;
            public bool isTranslated;
            public string createdAt;
        }

        [Serializable]
        private sealed class ChatSendRequest
        {
            public string token;
            public string channel;
            public string text;
        }

        [Serializable]
        private sealed class ChatReportRequest
        {
            public string token;
            public string channel;
            public long messageId;
            public int reportedUserId;
            public string reason;
            public string details;
        }

        [Serializable]
        private sealed class UserBlockRequest
        {
            public string token;
            public int blockedUserId;
            public string reason;
        }

        [Serializable]
        private sealed class DeveloperSupportSendRequest
        {
            public string token;
            public string text;
        }

        [Serializable]
        private sealed class DeveloperSupportCommentRequest
        {
            public string token;
            public string text;
        }

        [Serializable]
        private sealed class DeveloperSupportStatusRequest
        {
            public string token;
            public string status;
            public bool active;
            public int version;
        }

        [Serializable]
        private sealed class DeveloperSupportVoteRequest
        {
            public string token;
            public int vote;
        }

        [Serializable]
        private sealed class ChatTranslationRef
        {
            public string scope;
            public long sourceId;
        }

        [Serializable]
        private sealed class ChatTranslationResolveRequest
        {
            public string token;
            public string targetLanguage;
            public ChatTranslationRef[] refs;
        }

        [Serializable]
        private sealed class ChatTranslationEntry
        {
            public string scope;
            public long sourceId;
            public string translatedText;
            public string translatedLanguage;
            public string sourceLanguage;
            public string translationStatus;
        }

        [Serializable]
        private sealed class ChatTranslationResolveResponse
        {
            public bool success;
            public string error;
            public string targetLanguage;
            public ChatTranslationEntry[] translations;
        }

        [Serializable]
        private sealed class DeveloperSupportSeenState
        {
            public DeveloperSupportSeenEntry[] entries;
        }

        [Serializable]
        private sealed class DeveloperSupportSeenEntry
        {
            public long requestId;
            public int version;
        }

        [Serializable]
        private sealed class ChatListResponse
        {
            public bool success;
            public string error;
            public GlobalChatMessage[] messages;
        }

        [Serializable]
        private sealed class ChatSendResponse
        {
            public bool success;
            public string error;
            public GlobalChatMessage message;
        }

        [Serializable]
        private sealed class DeveloperSupportListResponse
        {
            public bool success;
            public string error;
            public bool canManage;
            public bool hasMore;
            public long nextBeforeId;
            public bool isUpdatePage;
            public string serverNow;
            public long serverAfterId;
            public string serverTime;
            public GlobalChatMessage[] messages;
        }

        [Serializable]
        private sealed class DeveloperSupportMessageResponse
        {
            public bool success;
            public string error;
            public GlobalChatMessage message;
        }

        [Serializable]
        private sealed class ChatErrorResponse
        {
            public bool success;
            public string error;
        }

        private sealed class FallbackLine
        {
            public FallbackLine(string nickname, string text)
            {
                Nickname = nickname;
                Text = text;
            }

            public string Nickname { get; }
            public string Text { get; }
        }
    }
}
