using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MahjongGame.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class AllianceService : MonoBehaviour
    {
        public static AllianceService I { get; private set; }

        private const string KeySessionToken = "symbiosis_server_session_token";
        private readonly List<AllianceMember> members = new List<AllianceMember>();
        private readonly List<AllianceInvite> incomingInvites = new List<AllianceInvite>();
        private readonly List<AllianceJoinRequest> pendingRequests = new List<AllianceJoinRequest>();
        private readonly List<AllianceActivity> activity = new List<AllianceActivity>();
        private readonly List<AllianceContributionBreakdown> contributionBreakdown = new List<AllianceContributionBreakdown>();
        private readonly List<AllianceChatMessage> chatMessages = new List<AllianceChatMessage>();
        private readonly List<AllianceSummary> searchResults = new List<AllianceSummary>();
        private readonly List<AllianceSummary> leaderboard = new List<AllianceSummary>();
        private string lastError = string.Empty;
        private long lastChatMessageId;

        public event Action AllianceChanged;
        public event Action ChatChanged;
        public event Action SearchChanged;
        public event Action LeaderboardChanged;
        public event Action<string> ErrorChanged;

        public AllianceSummary Current { get; private set; }
        public IReadOnlyList<AllianceMember> Members { get { return members; } }
        public IReadOnlyList<AllianceInvite> IncomingInvites { get { return incomingInvites; } }
        public IReadOnlyList<AllianceJoinRequest> PendingRequests { get { return pendingRequests; } }
        public IReadOnlyList<AllianceActivity> Activity { get { return activity; } }
        public IReadOnlyList<AllianceContributionBreakdown> ContributionBreakdown { get { return contributionBreakdown; } }
        public IReadOnlyList<AllianceChatMessage> ChatMessages { get { return chatMessages; } }
        public IReadOnlyList<AllianceSummary> SearchResults { get { return searchResults; } }
        public IReadOnlyList<AllianceSummary> Leaderboard { get { return leaderboard; } }
        public string LastError { get { return lastError; } }
        public AllianceRules Rules { get; private set; }
        public AllianceChestState Chest { get; private set; }
        public AllianceTournamentState Tournament { get; private set; }
        public bool HasAlliance { get { return Current != null && Current.id > 0; } }
        public bool CanManage { get { return Current != null && (Current.viewerRole == "leader" || Current.viewerRole == "officer"); } }
        public bool IsLeader { get { return Current != null && Current.viewerRole == "leader"; } }

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

        public IEnumerator Refresh()
        {
            string token = GetSessionToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                SetError(GameLocalization.Text("alliance.error_profile"));
                yield break;
            }

            using UnityWebRequest request = UnityWebRequest.Get(BuildUrl("/alliances/me?token=" + UnityWebRequest.EscapeURL(token)));
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (RequestFailed(request))
            {
                SetError(ReadError(request));
                yield break;
            }

            AllianceStateResponse response = Parse<AllianceStateResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
            {
                SetError(response != null && !string.IsNullOrWhiteSpace(response.error) ? response.error : "Invalid alliance response.");
                yield break;
            }

            ApplyState(response);
        }

        public IEnumerator Create(string name, string tag, string description, Action<bool, string> completed = null)
        {
            AllianceCreateRequest payload = new AllianceCreateRequest
            {
                token = GetSessionToken(),
                name = name,
                tag = tag,
                description = description,
                language = ResolveLanguage(),
                visibility = "invite_only",
                specialization = "social",
                weeklyFocus = "any",
                recruitmentNewPlayersWelcome = true
            };

            yield return PostState("/alliances/create", payload, completed);
        }

        public IEnumerator Search(string query)
        {
            string url = "/alliances/search?limit=20";
            if (!string.IsNullOrWhiteSpace(query))
                url += "&query=" + UnityWebRequest.EscapeURL(query.Trim());

            using UnityWebRequest request = UnityWebRequest.Get(BuildUrl(url));
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (RequestFailed(request))
            {
                SetError(ReadError(request));
                yield break;
            }

            AllianceSearchResponse response = Parse<AllianceSearchResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
            {
                SetError(response != null && !string.IsNullOrWhiteSpace(response.error) ? response.error : "Invalid alliance search.");
                yield break;
            }

            Replace(searchResults, response.alliances);
            SetError(string.Empty);
            SearchChanged?.Invoke();
        }

        public IEnumerator Join(int allianceId, bool requestOnly, Action<bool, string> completed = null)
        {
            AllianceIdRequest payload = new AllianceIdRequest { token = GetSessionToken(), allianceId = allianceId };
            yield return requestOnly
                ? PostSimple("/alliances/join-request", payload, completed)
                : PostState("/alliances/join", payload, completed);
        }

        public IEnumerator RespondInvite(int inviteId, bool accepted, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/invite/respond", new AllianceInviteRespondRequest
            {
                token = GetSessionToken(),
                inviteId = inviteId,
                accepted = accepted
            }, completed);
        }

        public IEnumerator RespondRequest(int requestId, bool accepted, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/join-request/respond", new AllianceRequestRespondRequest
            {
                token = GetSessionToken(),
                requestId = requestId,
                accepted = accepted
            }, completed);
        }

        public IEnumerator Invite(string nickname, Action<bool, string> completed = null)
        {
            yield return PostSimple("/alliances/invite", new AllianceInviteRequest
            {
                token = GetSessionToken(),
                nickname = nickname
            }, completed);
            yield return Refresh();
        }

        public IEnumerator Leave(Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/leave", new AllianceTokenRequest { token = GetSessionToken() }, completed);
        }

        public IEnumerator Kick(int targetUserId, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/kick", new AllianceTargetRequest { token = GetSessionToken(), targetUserId = targetUserId }, completed);
        }

        public IEnumerator Promote(int targetUserId, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/promote", new AllianceTargetRequest { token = GetSessionToken(), targetUserId = targetUserId }, completed);
        }

        public IEnumerator Demote(int targetUserId, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/demote", new AllianceTargetRequest { token = GetSessionToken(), targetUserId = targetUserId }, completed);
        }

        public IEnumerator TransferLeadership(int targetUserId, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/transfer-leadership", new AllianceTargetRequest { token = GetSessionToken(), targetUserId = targetUserId }, completed);
        }

        public IEnumerator Donate(string currency, int amount, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/donate", new AllianceDonateRequest { token = GetSessionToken(), currency = currency, amount = amount }, completed);
        }

        public IEnumerator AddTestBots(int count, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/debug/add-bots", new AllianceTestBotsRequest { token = GetSessionToken(), count = count }, completed);
        }

        public IEnumerator UpdateSettings(string announcement, string weeklyFocus, Action<bool, string> completed = null)
        {
            if (Current == null)
                yield break;

            AllianceUpdateRequest payload = new AllianceUpdateRequest
            {
                token = GetSessionToken(),
                name = Current.name,
                tag = Current.tag,
                description = Current.description,
                language = Current.language,
                visibility = Current.visibility,
                specialization = Current.specialization,
                weeklyFocus = weeklyFocus,
                announcement = announcement,
                recruitmentMinRankPoints = Current.recruitmentMinRankPoints,
                recruitmentNewPlayersWelcome = Current.recruitmentNewPlayersWelcome,
                recruitmentCompetitive = Current.recruitmentCompetitive
            };
            yield return PostState("/alliances/update", payload, completed);
        }

        public IEnumerator RefreshChat()
        {
            if (!HasAlliance)
                yield break;

            string token = GetSessionToken();
            string url = "/alliances/chat?sinceId=" + lastChatMessageId + "&limit=50";
            using UnityWebRequest request = UnityWebRequest.Get(BuildUrl(url));
            request.SetRequestHeader("X-Session-Token", token);
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (RequestFailed(request))
            {
                SetError(ReadError(request));
                yield break;
            }

            AllianceChatResponse response = Parse<AllianceChatResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
                yield break;

            if (response.messages != null)
            {
                for (int i = 0; i < response.messages.Length; i++)
                    AddChatMessage(response.messages[i]);
            }
            ChatChanged?.Invoke();
        }

        public IEnumerator SendChat(string text, Action<bool, string> completed = null)
        {
            yield return PostChat("/alliances/chat/send", new AllianceChatSendRequest
            {
                token = GetSessionToken(),
                text = text
            }, completed);
        }

        public IEnumerator RefreshLeaderboard()
        {
            using UnityWebRequest request = UnityWebRequest.Get(BuildUrl("/alliances/leaderboard?limit=50"));
            request.timeout = 10;
            yield return request.SendWebRequest();
            if (RequestFailed(request))
            {
                SetError(ReadError(request));
                yield break;
            }

            AllianceLeaderboardResponse response = Parse<AllianceLeaderboardResponse>(request.downloadHandler.text);
            if (response == null || !response.success)
                yield break;

            Replace(leaderboard, response.alliances);
            LeaderboardChanged?.Invoke();
        }

        public IEnumerator ClaimChest(Action<bool, string> completed = null)
        {
            yield return PostSimple("/alliances/chest/claim", new AllianceTokenRequest { token = GetSessionToken() }, completed);
            yield return Refresh();
        }

        public IEnumerator SelectChampion(int targetUserId, Action<bool, string> completed = null)
        {
            yield return PostState("/alliances/champion/select", new AllianceTargetRequest { token = GetSessionToken(), targetUserId = targetUserId }, completed);
        }

        private IEnumerator PostState(string path, object payload, Action<bool, string> completed)
        {
            bool ok = false;
            string message = string.Empty;
            yield return SendPost(path, payload, (success, text, body) =>
            {
                ok = success;
                message = text;
                if (success)
                {
                    AllianceStateResponse response = Parse<AllianceStateResponse>(body);
                    if (response != null && response.success)
                        ApplyState(response);
                }
            });
            completed?.Invoke(ok, message);
        }

        private IEnumerator PostSimple(string path, object payload, Action<bool, string> completed)
        {
            bool ok = false;
            string message = string.Empty;
            yield return SendPost(path, payload, (success, text, body) =>
            {
                ok = success;
                message = text;
                if (!success)
                    SetError(text);
            });
            completed?.Invoke(ok, message);
        }

        private IEnumerator PostChat(string path, object payload, Action<bool, string> completed)
        {
            bool ok = false;
            string message = string.Empty;
            yield return SendPost(path, payload, (success, text, body) =>
            {
                ok = success;
                message = text;
                if (success)
                {
                    AllianceChatSendResponse response = Parse<AllianceChatSendResponse>(body);
                    if (response != null && response.success && response.message != null)
                    {
                        AddChatMessage(response.message);
                        ChatChanged?.Invoke();
                    }
                }
            });
            completed?.Invoke(ok, message);
        }

        private IEnumerator SendPost(string path, object payload, Action<bool, string, string> completed)
        {
            string json = JsonUtility.ToJson(payload);
            using UnityWebRequest request = new UnityWebRequest(BuildUrl(path), "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            yield return request.SendWebRequest();

            string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (RequestFailed(request))
            {
                completed?.Invoke(false, ReadError(request), body);
                yield break;
            }

            BasicResponse basic = Parse<BasicResponse>(body);
            if (basic != null && !basic.success)
            {
                completed?.Invoke(false, string.IsNullOrWhiteSpace(basic.error) ? "Alliance request failed." : basic.error, body);
                yield break;
            }

            completed?.Invoke(true, basic != null && !string.IsNullOrWhiteSpace(basic.message) ? basic.message : string.Empty, body);
        }

        private void ApplyState(AllianceStateResponse response)
        {
            Current = response.alliance;
            Rules = response.rules;
            Chest = response.chest;
            Tournament = response.tournament;
            Replace(members, response.members);
            Replace(incomingInvites, response.incomingInvites);
            Replace(pendingRequests, response.pendingRequests);
            Replace(activity, response.activity);
            Replace(contributionBreakdown, response.contributionBreakdown);
            if (Current == null)
            {
                Chest = null;
                Tournament = null;
                chatMessages.Clear();
                lastChatMessageId = 0;
            }
            ApplyProfileAllianceSummary(Current);
            SetError(string.Empty);
            AllianceChanged?.Invoke();
        }

        private static void ApplyProfileAllianceSummary(AllianceSummary alliance)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return;

            profile.EnsureData();
            profile.AllianceTag = alliance != null ? alliance.tag ?? string.Empty : string.Empty;
            profile.AllianceName = alliance != null ? alliance.name ?? string.Empty : string.Empty;
            profile.AllianceLevel = alliance != null ? Mathf.Max(0, alliance.level) : 0;
        }

        private void AddChatMessage(AllianceChatMessage message)
        {
            if (message == null)
                return;

            for (int i = 0; i < chatMessages.Count; i++)
            {
                if (chatMessages[i].id == message.id)
                    return;
            }

            chatMessages.Add(message);
            if (message.id > lastChatMessageId)
                lastChatMessageId = message.id;
            if (chatMessages.Count > 100)
                chatMessages.RemoveRange(0, chatMessages.Count - 100);
        }

        private static void Replace<T>(List<T> target, T[] source)
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
            if (lastError == value)
                return;
            lastError = value;
            ErrorChanged?.Invoke(lastError);
        }

        private static string BuildUrl(string path)
        {
            return BackendEndpoints.BuildUrl(BackendEndpoints.PrimaryBaseUrl, path);
        }

        private static string GetSessionToken()
        {
            return PlayerPrefs.GetString(KeySessionToken, string.Empty);
        }

        private static string ResolveLanguage()
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            if (language == GameLanguage.English) return "english";
            if (language == GameLanguage.Turkish) return "turkish";
            if (language == GameLanguage.German) return "german";
            return "russian";
        }

        private static bool RequestFailed(UnityWebRequest request)
        {
            return BackendEndpoints.RequestFailed(request);
        }

        private static string ReadError(UnityWebRequest request)
        {
            if (request.responseCode == 404)
                return GameLocalization.Text("alliance.error_backend_unavailable");

            string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            BasicResponse response = Parse<BasicResponse>(body);
            if (response != null && !string.IsNullOrWhiteSpace(response.error))
                return response.error;
            return string.IsNullOrWhiteSpace(request.error) ? "Alliance request failed." : request.error;
        }

        private static T Parse<T>(string json) where T : class
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

        [Serializable] private sealed class BasicResponse { public bool success; public string error; public string message; }
        [Serializable] private sealed class AllianceTokenRequest { public string token; }
        [Serializable] private sealed class AllianceIdRequest { public string token; public int allianceId; }
        [Serializable] private sealed class AllianceTargetRequest { public string token; public int targetUserId; }
        [Serializable] private sealed class AllianceDonateRequest { public string token; public string currency; public int amount; }
        [Serializable] private sealed class AllianceTestBotsRequest { public string token; public int count; }
        [Serializable] private sealed class AllianceInviteRequest { public string token; public string nickname; }
        [Serializable] private sealed class AllianceInviteRespondRequest { public string token; public int inviteId; public bool accepted; }
        [Serializable] private sealed class AllianceRequestRespondRequest { public string token; public int requestId; public bool accepted; }
        [Serializable] private sealed class AllianceChatSendRequest { public string token; public string text; }

        [Serializable]
        private sealed class AllianceCreateRequest
        {
            public string token;
            public string name;
            public string tag;
            public string description;
            public string language;
            public string visibility;
            public string specialization;
            public string weeklyFocus;
            public bool recruitmentNewPlayersWelcome;
        }

        [Serializable]
        private sealed class AllianceUpdateRequest
        {
            public string token;
            public string name;
            public string tag;
            public string description;
            public string language;
            public string visibility;
            public string specialization;
            public string weeklyFocus;
            public string announcement;
            public int recruitmentMinRankPoints;
            public bool recruitmentNewPlayersWelcome;
            public bool recruitmentCompetitive;
        }
    }
}
