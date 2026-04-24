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
        private static readonly TimeSpan LiveMessageLifetime = TimeSpan.FromMinutes(3d);
        private static readonly TimeSpan FallbackPostInterval = TimeSpan.FromMinutes(1d);
        private const int FallbackSeedMessageCount = 3;
        public const string ChannelGlobal = "global";
        public const string ChannelMahjong = "mahjong";

        private readonly Dictionary<string, List<GlobalChatMessage>> messagesByChannel = new Dictionary<string, List<GlobalChatMessage>>();
        private readonly Dictionary<string, List<GlobalChatMessage>> fallbackMessagesByChannel = new Dictionary<string, List<GlobalChatMessage>>();
        private readonly Dictionary<string, long> lastMessageIdByChannel = new Dictionary<string, long>();
        private readonly Dictionary<string, int> fallbackCursorByChannel = new Dictionary<string, int>();
        private string currentChannel = ChannelGlobal;
        private string lastError = string.Empty;
        private long nextFallbackMessageId = -1L;

        private static readonly FallbackLine[] GlobalFallbackScript =
        {
            new FallbackLine("Luna", "\u041F\u0440\u0438\u0432\u0435\u0442. \u0413\u043B\u043E\u0431\u0430\u043B\u044C\u043D\u044B\u0439 \u0447\u0430\u0442 \u0441\u043D\u043E\u0432\u0430 \u0436\u0438\u0432\u043E\u0439."),
            new FallbackLine("Nova", "Hey everyone, this room feels better when it has a little motion."),
            new FallbackLine("Mira", "Merhaba, biraz hareket olunca sohbet daha canli gorunuyor."),
            new FallbackLine("Luna", "\u0414\u0430\u0436\u0435 \u043F\u0430\u0440\u0430 \u043A\u043E\u0440\u043E\u0442\u043A\u0438\u0445 \u0440\u0435\u043F\u043B\u0438\u043A \u0443\u0436\u0435 \u0434\u0430\u0451\u0442 \u044D\u0444\u0444\u0435\u043A\u0442 \u0436\u0438\u0432\u043E\u0433\u043E \u043A\u0430\u043D\u0430\u043B\u0430."),
            new FallbackLine("Nova", "Small talk is enough. The chat just should not feel abandoned."),
            new FallbackLine("Mira", "Kisa mesajlar yeterli, yeter ki sohbet bos durmasin."),
            new FallbackLine("Luna", "\u041F\u0443\u0441\u0442\u044C \u0438\u0434\u0451\u0442 \u043B\u0451\u0433\u043A\u0438\u0439 \u0444\u043E\u043D, \u0431\u0435\u0437 \u0441\u043F\u0430\u043C\u0430 \u0438 \u0448\u0443\u043C\u0430."),
            new FallbackLine("Nova", "Right, a calm rolling chat works better than spam."),
            new FallbackLine("Mira", "Tamam, ritmi koruyalim ve global kanali canli tutalim."),
        };

        private static readonly FallbackLine[] MahjongFallbackScript =
        {
            new FallbackLine("Luna", "\u041A\u0442\u043E \u0441\u0435\u0433\u043E\u0434\u043D\u044F \u0437\u0430\u0439\u0434\u0451\u0442 \u0432 \u043C\u0430\u0434\u0436\u043E\u043D\u0433-\u043B\u043E\u0431\u0431\u0438?"),
            new FallbackLine("Nova", "I can stay for a short mahjong match if someone joins."),
            new FallbackLine("Mira", "Mahjong kanalinda biraz yazisma lobi hissini guclendiriyor."),
            new FallbackLine("Luna", "\u041F\u0435\u0440\u0435\u0434 \u0438\u0433\u0440\u043E\u0439 \u043F\u0430\u0440\u0430 \u043F\u0440\u043E\u0441\u0442\u044B\u0445 \u0444\u0440\u0430\u0437 \u0442\u043E\u0436\u0435 \u0441\u043E\u0437\u0434\u0430\u0451\u0442 \u043D\u0443\u0436\u043D\u044B\u0439 \u0444\u043E\u043D."),
            new FallbackLine("Nova", "A quick back-and-forth makes the table feel occupied."),
            new FallbackLine("Mira", "Biraz mesaj olunca burasi daha dolu ve sicak hissettiriyor."),
            new FallbackLine("Luna", "\u0414\u0435\u0440\u0436\u0438\u043C \u0442\u0435\u043C\u043F \u0441\u043F\u043E\u043A\u043E\u0439\u043D\u044B\u043C \u0438 \u0431\u0435\u0437 \u043F\u0435\u0440\u0435\u0433\u0440\u0443\u0437\u043A\u0438."),
            new FallbackLine("Nova", "Light chatter is enough here. No need to flood the screen."),
            new FallbackLine("Mira", "Tam istedigimiz sey bu, sakin ama yasayan bir sohbet."),
        };

        public event Action MessagesChanged;
        public event Action<string> ErrorChanged;

        public IReadOnlyList<GlobalChatMessage> Messages
        {
            get
            {
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

        public void SetChannel(string channel)
        {
            string normalized = NormalizeChannel(channel);
            if (string.Equals(currentChannel, normalized, StringComparison.Ordinal))
                return;

            currentChannel = normalized;
            PruneExpiredMessages(currentChannel);
            MessagesChanged?.Invoke();
        }

        public IEnumerator Refresh(int limit = 50)
        {
            string channel = currentChannel;
            string token = GetSessionToken();
            long lastMessageId = GetLastMessageId(channel);
            string url = $"{BaseUrl}/chat/global?channel={UnityWebRequest.EscapeURL(channel)}&sinceId={lastMessageId}&limit={Mathf.Clamp(limit, 1, 100)}";
            if (!string.IsNullOrWhiteSpace(token))
                url += $"&token={UnityWebRequest.EscapeURL(token)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (RequestFailed(request))
            {
                bool changed = EnsureFallbackMessages(channel, DateTimeOffset.UtcNow);
                SetError(string.Empty);
                if (changed)
                    MessagesChanged?.Invoke();
                yield break;
            }

            ChatListResponse response = ParseResponse<ChatListResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
            {
                bool changed = EnsureFallbackMessages(channel, DateTimeOffset.UtcNow);
                SetError(string.Empty);
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
            SetError(string.Empty);
            MessagesChanged?.Invoke();
        }

        public IEnumerator Send(string text, Action<bool, string> completed = null)
        {
            string channel = currentChannel;
            string token = GetSessionToken();
            string cleanText = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                const string error = "Chat requires server profile.";
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(cleanText))
            {
                completed?.Invoke(false, "Message is empty.");
                yield break;
            }

            if (cleanText.Length > 240)
                cleanText = cleanText.Substring(0, 240);

            ChatSendRequest payload = new ChatSendRequest
            {
                token = token,
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

            if (RequestFailed(request))
            {
                string error = ReadError(request);
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            ChatSendResponse response = ParseResponse<ChatSendResponse>(request.downloadHandler.text);
            if (response == null || !response.success || response.message == null)
            {
                string error = response != null && !string.IsNullOrWhiteSpace(response.error)
                    ? response.error
                    : "Invalid chat response.";
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            AddOrUpdate(channel, response.message);
            TrimMessages(channel, 100);
            SetError(string.Empty);
            MessagesChanged?.Invoke();
            completed?.Invoke(true, string.Empty);
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

        private void PruneExpiredMessages(string channel)
        {
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
            if (GetMessages(normalized).Count > 0)
                return false;

            List<GlobalChatMessage> messages = GetFallbackMessages(normalized);
            bool changed = PruneExpiredMessageList(messages, now);
            FallbackLine[] script = GetFallbackScript(normalized);
            if (script.Length == 0)
                return changed;

            int cursor = fallbackCursorByChannel.TryGetValue(normalized, out int value) ? value : 0;
            if (messages.Count == 0)
            {
                int seedCount = Mathf.Min(FallbackSeedMessageCount, script.Length);
                for (int index = seedCount - 1; index >= 0; index--)
                {
                    FallbackLine line = script[cursor % script.Length];
                    messages.Add(CreateFallbackMessage(normalized, line, now - TimeSpan.FromMinutes(index)));
                    cursor++;
                }

                fallbackCursorByChannel[normalized] = cursor;
                return true;
            }

            DateTimeOffset lastCreatedAt = GetMessageTimestamp(messages[messages.Count - 1], now);
            while (now - lastCreatedAt >= FallbackPostInterval)
            {
                FallbackLine line = script[cursor % script.Length];
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

        private static FallbackLine[] GetFallbackScript(string channel)
        {
            return string.Equals(NormalizeChannel(channel), ChannelMahjong, StringComparison.Ordinal)
                ? MahjongFallbackScript
                : GlobalFallbackScript;
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
            return PlayerPrefs.GetString(KeySessionToken, string.Empty);
        }

        private static string NormalizeChannel(string channel)
        {
            return string.Equals(channel, ChannelMahjong, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(channel, "madonna", StringComparison.OrdinalIgnoreCase)
                ? ChannelMahjong
                : ChannelGlobal;
        }

        private static string GetChannelLabel(string channel)
        {
            return string.Equals(NormalizeChannel(channel), ChannelMahjong, StringComparison.Ordinal)
                ? "Mahjong"
                : "Global";
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
            public string publicPlayerId;
            public string channel;
            public string text;
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
