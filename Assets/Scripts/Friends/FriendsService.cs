using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class FriendsService : MonoBehaviour
    {
        public static FriendsService I { get; private set; }

        private const string BaseUrl = "https://dlsymbiosis.com";
        private const string KeySessionToken = "symbiosis_server_session_token";

        private readonly List<FriendUser> friends = new List<FriendUser>();
        private readonly List<IncomingFriendRequest> incomingRequests = new List<IncomingFriendRequest>();
        private readonly List<OutgoingFriendRequest> outgoingRequests = new List<OutgoingFriendRequest>();
        private string lastError = string.Empty;
        private Coroutine heartbeatRoutine;

        public event Action FriendsChanged;
        public event Action<string> ErrorChanged;

        public IReadOnlyList<FriendUser> Friends => friends;
        public IReadOnlyList<IncomingFriendRequest> IncomingRequests => incomingRequests;
        public IReadOnlyList<OutgoingFriendRequest> OutgoingRequests => outgoingRequests;
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

        private void OnEnable()
        {
            heartbeatRoutine = StartCoroutine(HeartbeatLoop());
        }

        private void OnDisable()
        {
            if (heartbeatRoutine != null)
            {
                StopCoroutine(heartbeatRoutine);
                heartbeatRoutine = null;
            }
        }

        public IEnumerator Refresh(Action<bool, string> completed = null)
        {
            string token = GetSessionToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                const string error = "Friends require server profile.";
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            string url = BaseUrl + "/friends/list?token=" + UnityWebRequest.EscapeURL(token);
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (RequestFailed(request))
            {
                string error = ReadError(request);
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            FriendListResponse response = ParseResponse<FriendListResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
            {
                string error = response != null && !string.IsNullOrWhiteSpace(response.error)
                    ? response.error
                    : "Invalid friends response.";
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            ReplaceList(friends, response.friends);
            ReplaceList(incomingRequests, response.incomingRequests);
            ReplaceList(outgoingRequests, response.outgoingRequests);
            SetError(string.Empty);
            FriendsChanged?.Invoke();
            completed?.Invoke(true, string.Empty);
        }

        public IEnumerator Search(string nickname, Action<bool, string, FriendUser[]> completed)
        {
            string token = GetSessionToken();
            string cleanNickname = string.IsNullOrWhiteSpace(nickname) ? string.Empty : nickname.Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                const string error = "Friends require server profile.";
                SetError(error);
                completed?.Invoke(false, error, null);
                yield break;
            }

            if (cleanNickname.Length < 2)
            {
                completed?.Invoke(false, "Enter at least 2 characters.", null);
                yield break;
            }

            string url = BaseUrl + "/friends/search?token=" + UnityWebRequest.EscapeURL(token) +
                         "&nickname=" + UnityWebRequest.EscapeURL(cleanNickname);
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (RequestFailed(request))
            {
                string error = ReadError(request);
                SetError(error);
                completed?.Invoke(false, error, null);
                yield break;
            }

            FriendSearchResponse response = ParseResponse<FriendSearchResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
            {
                string error = response != null && !string.IsNullOrWhiteSpace(response.error)
                    ? response.error
                    : "Invalid search response.";
                SetError(error);
                completed?.Invoke(false, error, null);
                yield break;
            }

            SetError(string.Empty);
            completed?.Invoke(true, string.Empty, response.users ?? new FriendUser[0]);
        }

        public IEnumerator SendRequestByNickname(string nickname, Action<bool, string> completed = null)
        {
            string token = GetSessionToken();
            string cleanNickname = string.IsNullOrWhiteSpace(nickname) ? string.Empty : nickname.Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                const string error = "Friends require server profile.";
                SetError(error);
                completed?.Invoke(false, error);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(cleanNickname))
            {
                completed?.Invoke(false, "Enter nickname.");
                yield break;
            }

            FriendNicknameRequest payload = new FriendNicknameRequest
            {
                token = token,
                nickname = cleanNickname
            };

            yield return PostJson(BaseUrl + "/friends/request-by-nickname", payload, (success, text) =>
            {
                if (!success)
                    SetError(text);
                else
                    SetError(string.Empty);

                completed?.Invoke(success, text);
            });

            yield return Refresh();
        }

        public IEnumerator Accept(int requestId, Action<bool, string> completed = null)
        {
            FriendRequestAction payload = new FriendRequestAction
            {
                token = GetSessionToken(),
                requestId = requestId
            };

            yield return PostJson(BaseUrl + "/friends/accept", payload, completed);
            yield return Refresh();
        }

        public IEnumerator Decline(int requestId, Action<bool, string> completed = null)
        {
            FriendRequestAction payload = new FriendRequestAction
            {
                token = GetSessionToken(),
                requestId = requestId
            };

            yield return PostJson(BaseUrl + "/friends/decline", payload, completed);
            yield return Refresh();
        }

        private IEnumerator HeartbeatLoop()
        {
            while (true)
            {
                string token = GetSessionToken();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    FriendTokenRequest payload = new FriendTokenRequest { token = token };
                    yield return PostJson(BaseUrl + "/presence/heartbeat", payload, null);
                }

                yield return new WaitForSecondsRealtime(30f);
            }
        }

        private IEnumerator PostJson(string url, object payload, Action<bool, string> completed)
        {
            string json = JsonUtility.ToJson(payload);
            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (RequestFailed(request))
            {
                string error = ReadError(request);
                completed?.Invoke(false, error);
                yield break;
            }

            BasicResponse response = ParseResponse<BasicResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
            {
                string error = response != null && !string.IsNullOrWhiteSpace(response.error)
                    ? response.error
                    : "Request failed.";
                completed?.Invoke(false, error);
                yield break;
            }

            completed?.Invoke(true, string.IsNullOrWhiteSpace(response.message) ? string.Empty : response.message);
        }

        private static void ReplaceList<T>(List<T> target, T[] source)
        {
            target.Clear();
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    target.Add(source[i]);
            }
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
                BasicResponse response = ParseResponse<BasicResponse>(responseText);
                if (response != null && !string.IsNullOrWhiteSpace(response.error))
                    return response.error;
            }

            return string.IsNullOrWhiteSpace(request.error) ? "Friends request failed." : request.error;
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
        public sealed class FriendUser
        {
            public int id;
            public string nickname;
            public string publicPlayerId;
            public bool online;
            public string lastSeenAt;
            public bool isFriend;
            public bool hasPendingOutgoing;
            public bool hasPendingIncoming;
        }

        [Serializable]
        public sealed class IncomingFriendRequest
        {
            public int id;
            public int senderId;
            public string senderNickname;
            public string senderPublicPlayerId;
            public bool online;
            public string lastSeenAt;
            public string createdAt;
        }

        [Serializable]
        public sealed class OutgoingFriendRequest
        {
            public int id;
            public int receiverId;
            public string receiverNickname;
            public string receiverPublicPlayerId;
            public bool online;
            public string lastSeenAt;
            public string createdAt;
        }

        [Serializable]
        private sealed class FriendListResponse
        {
            public bool success;
            public string error;
            public FriendUser[] friends;
            public IncomingFriendRequest[] incomingRequests;
            public OutgoingFriendRequest[] outgoingRequests;
        }

        [Serializable]
        private sealed class FriendSearchResponse
        {
            public bool success;
            public string error;
            public FriendUser[] users;
        }

        [Serializable]
        private sealed class FriendTokenRequest
        {
            public string token;
        }

        [Serializable]
        private sealed class FriendNicknameRequest
        {
            public string token;
            public string nickname;
        }

        [Serializable]
        private sealed class FriendRequestAction
        {
            public string token;
            public int requestId;
        }

        [Serializable]
        private sealed class BasicResponse
        {
            public bool success;
            public string error;
            public string message;
        }
    }
}
