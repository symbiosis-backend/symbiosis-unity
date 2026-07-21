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

        public IEnumerator Refresh(Action<bool, string> completed = null)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string token = GetSessionToken();
                if (string.IsNullOrWhiteSpace(token))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0)
                        yield return RecoverSession(string.Empty, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    string missingError = string.IsNullOrWhiteSpace(recoveryError) ? GameLocalization.Text("friends.error_profile") : recoveryError;
                    SetError(missingError);
                    completed?.Invoke(false, missingError);
                    yield break;
                }

                string url = BaseUrl + "/friends/list?token=" + UnityWebRequest.EscapeURL(token);
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.timeout = 10;

                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                FriendListResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<FriendListResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Invalid friends response.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(token, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    string finalError = string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError;
                    SetError(finalError);
                    completed?.Invoke(false, finalError);
                    yield break;
                }

                ReplaceList(friends, response.friends);
                ReplaceList(incomingRequests, response.incomingRequests);
                ReplaceList(outgoingRequests, response.outgoingRequests);
                SetError(string.Empty);
                FriendsChanged?.Invoke();
                completed?.Invoke(true, string.Empty);
                yield break;
            }
        }

        public IEnumerator Search(string nickname, Action<bool, string, FriendUser[]> completed)
        {
            string cleanNickname = string.IsNullOrWhiteSpace(nickname) ? string.Empty : nickname.Trim();

            if (cleanNickname.Length < 2)
            {
                completed?.Invoke(false, "Enter at least 2 characters.", null);
                yield break;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                string token = GetSessionToken();
                if (string.IsNullOrWhiteSpace(token))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0)
                        yield return RecoverSession(string.Empty, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    string missingError = string.IsNullOrWhiteSpace(recoveryError) ? GameLocalization.Text("friends.error_profile") : recoveryError;
                    SetError(missingError);
                    completed?.Invoke(false, missingError, null);
                    yield break;
                }

                string url = BaseUrl + "/friends/search?token=" + UnityWebRequest.EscapeURL(token) +
                             "&nickname=" + UnityWebRequest.EscapeURL(cleanNickname);
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.timeout = 10;

                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                FriendSearchResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<FriendSearchResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Invalid search response.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(token, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    string finalError = string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError;
                    SetError(finalError);
                    completed?.Invoke(false, finalError, null);
                    yield break;
                }

                SetError(string.Empty);
                completed?.Invoke(true, string.Empty, response.users ?? new FriendUser[0]);
                yield break;
            }
        }

        public IEnumerator SendRequestByNickname(string nickname, Action<bool, string> completed = null)
        {
            string cleanNickname = string.IsNullOrWhiteSpace(nickname) ? string.Empty : nickname.Trim();

            if (string.IsNullOrWhiteSpace(cleanNickname))
            {
                completed?.Invoke(false, "Enter nickname.");
                yield break;
            }

            FriendNicknameRequest payload = new FriendNicknameRequest
            {
                token = GetSessionToken(),
                nickname = cleanNickname
            };

            bool requestSucceeded = false;
            yield return PostJson(BaseUrl + "/friends/request-by-nickname", payload, (success, text) =>
            {
                requestSucceeded = success;
                if (!success)
                    SetError(text);
                else
                    SetError(string.Empty);

                completed?.Invoke(success, text);
            });

            if (requestSucceeded)
                yield return Refresh();
        }

        public IEnumerator Accept(int requestId, Action<bool, string> completed = null)
        {
            FriendRequestAction payload = new FriendRequestAction
            {
                token = GetSessionToken(),
                requestId = requestId
            };

            bool requestSucceeded = false;
            yield return PostJson(BaseUrl + "/friends/accept", payload, (success, message) =>
            {
                requestSucceeded = success;
                completed?.Invoke(success, message);
            });
            if (requestSucceeded)
                yield return Refresh();
        }

        public IEnumerator Decline(int requestId, Action<bool, string> completed = null)
        {
            FriendRequestAction payload = new FriendRequestAction
            {
                token = GetSessionToken(),
                requestId = requestId
            };

            bool requestSucceeded = false;
            yield return PostJson(BaseUrl + "/friends/decline", payload, (success, message) =>
            {
                requestSucceeded = success;
                completed?.Invoke(success, message);
            });
            if (requestSucceeded)
                yield return Refresh();
        }

        private IEnumerator PostJson(string url, object payload, Action<bool, string> completed)
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

                    completed?.Invoke(false, string.IsNullOrWhiteSpace(missingSessionError)
                        ? GameLocalization.Text("network.session_expired")
                        : missingSessionError);
                    yield break;
                }

                ApplyCurrentSessionToken(payload, requestToken);
                string json = JsonUtility.ToJson(payload);
                using UnityWebRequest request = new UnityWebRequest(url, "POST");
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                string requestError = RequestFailed(request) ? ReadError(request) : string.Empty;
                BasicResponse response = string.IsNullOrWhiteSpace(requestError)
                    ? ParseResponse<BasicResponse>(request.downloadHandler.text)
                    : null;
                if (string.IsNullOrWhiteSpace(requestError) && (response == null || !response.success))
                {
                    requestError = response != null && !string.IsNullOrWhiteSpace(response.error)
                        ? response.error
                        : "Request failed.";
                }

                if (!string.IsNullOrWhiteSpace(requestError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    if (attempt == 0 && ProfileService.IsSessionAuthenticationError(requestError))
                        yield return RecoverSession(requestToken, (success, error) => { recovered = success; recoveryError = error; });
                    if (recovered)
                        continue;

                    completed?.Invoke(false, string.IsNullOrWhiteSpace(recoveryError) ? requestError : recoveryError);
                    yield break;
                }

                completed?.Invoke(true, string.IsNullOrWhiteSpace(response.message) ? string.Empty : response.message);
                yield break;
            }
        }

        private static void ApplyCurrentSessionToken(object payload, string token)
        {
            if (payload is FriendTokenRequest tokenRequest)
                tokenRequest.token = token;
            else if (payload is FriendNicknameRequest nicknameRequest)
                nicknameRequest.token = token;
            else if (payload is FriendRequestAction actionRequest)
                actionRequest.token = token;
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
            string token = ProfileService.I != null ? ProfileService.I.CurrentSessionToken : string.Empty;
            if (!string.IsNullOrWhiteSpace(token))
                return token;

            return PlayerPrefs.GetString(ClientProfileScope.AppendToKey(KeySessionToken), string.Empty);
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
            public int allianceId;
            public string allianceTag;
            public string allianceName;
            public int allianceLevel;
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
