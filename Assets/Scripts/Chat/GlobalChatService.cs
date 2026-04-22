using System;
using System.Collections;
using System.Collections.Generic;
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

        private readonly List<GlobalChatMessage> messages = new List<GlobalChatMessage>();
        private long lastMessageId;
        private string lastError = string.Empty;

        public event Action MessagesChanged;
        public event Action<string> ErrorChanged;

        public IReadOnlyList<GlobalChatMessage> Messages => messages;
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

        public IEnumerator Refresh(int limit = 50)
        {
            string token = GetSessionToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                SetError("Chat requires server profile.");
                yield break;
            }

            string url = $"{BaseUrl}/chat/global?token={UnityWebRequest.EscapeURL(token)}&sinceId={lastMessageId}&limit={Mathf.Clamp(limit, 1, 100)}";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (RequestFailed(request))
            {
                SetError(ReadError(request));
                yield break;
            }

            ChatListResponse response = ParseResponse<ChatListResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
            {
                SetError(response != null && !string.IsNullOrWhiteSpace(response.error)
                    ? response.error
                    : "Invalid chat response.");
                yield break;
            }

            if (response.messages != null)
            {
                for (int i = 0; i < response.messages.Length; i++)
                    AddOrUpdate(response.messages[i]);
            }

            TrimMessages(100);
            SetError(string.Empty);
            MessagesChanged?.Invoke();
        }

        public IEnumerator Send(string text, Action<bool, string> completed = null)
        {
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

            AddOrUpdate(response.message);
            TrimMessages(100);
            SetError(string.Empty);
            MessagesChanged?.Invoke();
            completed?.Invoke(true, string.Empty);
        }

        private void AddOrUpdate(GlobalChatMessage message)
        {
            if (message == null || message.id <= 0)
                return;

            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i] != null && messages[i].id == message.id)
                {
                    messages[i] = message;
                    lastMessageId = Math.Max(lastMessageId, message.id);
                    return;
                }
            }

            messages.Add(message);
            messages.Sort((a, b) => a.id.CompareTo(b.id));
            lastMessageId = Math.Max(lastMessageId, message.id);
        }

        private void TrimMessages(int maxCount)
        {
            while (messages.Count > maxCount)
                messages.RemoveAt(0);
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
            public string text;
            public string createdAt;
        }

        [Serializable]
        private sealed class ChatSendRequest
        {
            public string token;
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
    }
}
