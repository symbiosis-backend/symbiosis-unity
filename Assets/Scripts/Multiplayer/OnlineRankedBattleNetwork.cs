using System;
using System.Collections;
using System.Text;
using MahjongGame;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class OnlineRankedBattleNetwork : MonoBehaviour
    {
        public static OnlineRankedBattleNetwork I { get; private set; }

        private const string BaseUrl = "https://dlsymbiosis.com";
        private const string KeySessionToken = "symbiosis_server_session_token";
        private const string RankedPathPrefix = "/battle/ranked";
        private const string RandomPathPrefix = "/battle/random";

        [SerializeField, Min(0.5f)] private float matchmakingPollSeconds = 2f;
        [SerializeField, Min(0.15f)] private float eventPollSeconds = 0.35f;
        [SerializeField] private bool debugLogs = true;

        private Coroutine matchmakingRoutine;
        private Coroutine eventPollRoutine;
        private bool matchmakingCancelRequested;
        private int lastEventSeq;
        private string activePathPrefix = RankedPathPrefix;

        public event Action<string> StatusChanged;
        public event Action<string> ErrorChanged;
        public event Action<RankedMatchInfo> MatchFound;
        public event Action<RankedServerEvent> ServerEventReceived;
        public event Action<int, string> RemoteTilePicked;
        public event Action<BattleBoardSide, int> RemoteDamageApplied;

        public string Status { get; private set; } = "Ready";
        public string LastError { get; private set; } = string.Empty;
        public string MatchId { get; private set; } = string.Empty;
        public int MatchSeed { get; private set; }
        public int PlayerIndex { get; private set; }
        public RankedOpponentInfo Opponent { get; private set; }
        public bool IsSearching => matchmakingRoutine != null;
        public bool IsInMatch => !string.IsNullOrWhiteSpace(MatchId);
        public bool IsRandomMatchPath => string.Equals(activePathPrefix, RandomPathPrefix, StringComparison.Ordinal);

        public static OnlineRankedBattleNetwork EnsureInstance()
        {
            if (I != null)
                return I;

            GameObject host = new GameObject("OnlineRankedBattleNetwork");
            return host.AddComponent<OnlineRankedBattleNetwork>();
        }

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

        private void OnDestroy()
        {
            if (I == this)
                I = null;
        }

        public void StartRankedSearch()
        {
            if (matchmakingRoutine != null)
                StopCoroutine(matchmakingRoutine);

            activePathPrefix = RankedPathPrefix;
            ClearMatch();
            matchmakingCancelRequested = false;
            matchmakingRoutine = StartCoroutine(SearchRoutine(
                RankedPathPrefix,
                "Searching ranked match...",
                "Waiting for another player...",
                failOnError: true));
        }

        public void StartRandomSearch()
        {
            if (matchmakingRoutine != null)
                StopCoroutine(matchmakingRoutine);

            activePathPrefix = RandomPathPrefix;
            ClearMatch();
            matchmakingCancelRequested = false;
            matchmakingRoutine = StartCoroutine(SearchRoutine(
                RandomPathPrefix,
                "Searching random match...",
                "Looking for a player..."));
        }

        public void CancelRankedSearch()
        {
            matchmakingCancelRequested = true;

            if (matchmakingRoutine != null)
            {
                StopCoroutine(matchmakingRoutine);
                matchmakingRoutine = null;
            }

            StartCoroutine(CancelSearchRoutine(activePathPrefix));
            SetStatus("Search cancelled");
        }

        public void CancelRandomSearch()
        {
            CancelRankedSearch();
        }

        public void StartMatchEventPolling()
        {
            if (!IsInMatch)
                return;

            if (eventPollRoutine != null)
                StopCoroutine(eventPollRoutine);

            eventPollRoutine = StartCoroutine(EventPollRoutine());
        }

        public void StopMatchEventPolling()
        {
            if (eventPollRoutine == null)
                return;

            StopCoroutine(eventPollRoutine);
            eventPollRoutine = null;
        }

        public void SendTilePick(int tileIndex, string tileId)
        {
            if (!IsInMatch)
                return;

            RankedEventRequest payload = CreateEventRequest("pick");
            payload.tileIndex = tileIndex;
            payload.tileId = tileId ?? string.Empty;
            StartCoroutine(PostEvent(payload));
        }

        public void SendBoardManifest(RankedBoardTile[] tiles, int maxHp, int damagePerPair)
        {
            if (!IsInMatch || tiles == null || tiles.Length == 0)
                return;

            RankedEventRequest payload = CreateEventRequest("board");
            payload.tiles = tiles;
            payload.maxHp = Mathf.Max(1, maxHp);
            payload.damagePerPair = Mathf.Max(1, damagePerPair);
            StartCoroutine(PostEvent(payload));
        }

        public void RequestServerBoard(RankedBoardSlot[] slots, string[] tilePool, int maxHp, int damagePerPair)
        {
            if (!IsInMatch || slots == null || slots.Length == 0 || tilePool == null || tilePool.Length == 0)
                return;

            RankedEventRequest payload = CreateEventRequest("board");
            payload.slots = slots;
            payload.tilePool = tilePool;
            payload.maxHp = Mathf.Max(1, maxHp);
            payload.damagePerPair = Mathf.Max(1, damagePerPair);
            StartCoroutine(PostEvent(payload));
        }

        public void SendDamage(BattleBoardSide targetSide, int amount)
        {
            if (!IsInMatch || amount <= 0)
                return;

            RankedEventRequest payload = CreateEventRequest("damage");
            payload.targetSide = targetSide.ToString();
            payload.amount = Mathf.Max(0, amount);
            StartCoroutine(PostEvent(payload));
        }

        public void SendMatchFinished()
        {
            if (!IsInMatch)
                return;

            StartCoroutine(PostEvent(CreateEventRequest("finish")));
            StopMatchEventPolling();
            MatchId = string.Empty;
        }

        private IEnumerator SearchRoutine(string pathPrefix, string searchingStatus, string waitingStatus, bool failOnError = false)
        {
            SetError(string.Empty);
            SetStatus(searchingStatus);

            while (!matchmakingCancelRequested)
            {
                bool completed = false;
                RankedMatchResponse response = null;
                string error = string.Empty;

                yield return PostJson(
                    BaseUrl + pathPrefix + "/queue",
                    CreateQueueRequest(),
                    text =>
                    {
                        response = ParseResponse<RankedMatchResponse>(text);
                        completed = true;
                    },
                    requestError =>
                    {
                        error = requestError;
                        completed = true;
                    });

                if (!completed)
                    yield return null;

                if (!string.IsNullOrWhiteSpace(error))
                {
                    SetError(error);
                    SetStatus(failOnError ? "Ranked search failed" : "Random online search unavailable");
                    matchmakingRoutine = null;
                    yield break;
                }

                if (TryApplyMatchResponse(response))
                {
                    matchmakingRoutine = null;
                    yield break;
                }

                SetStatus(waitingStatus);
                yield return new WaitForSecondsRealtime(matchmakingPollSeconds);
            }

            matchmakingRoutine = null;
        }

        private IEnumerator CancelSearchRoutine(string pathPrefix)
        {
            yield return PostJson(
                BaseUrl + pathPrefix + "/cancel",
                new TokenRequest { token = GetSessionToken() },
                _ => { },
                error => SetError(error));
        }

        private IEnumerator EventPollRoutine()
        {
            while (IsInMatch)
            {
                string token = GetSessionToken();
                string pathPrefix = string.IsNullOrWhiteSpace(activePathPrefix) ? RankedPathPrefix : activePathPrefix;
                string url = BaseUrl + pathPrefix + "/events" +
                             "?token=" + UnityWebRequest.EscapeURL(token) +
                             "&matchId=" + UnityWebRequest.EscapeURL(MatchId) +
                             "&afterSeq=" + lastEventSeq;

                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.timeout = 8;
                yield return request.SendWebRequest();

                if (!RequestFailed(request))
                    ApplyEvents(ParseResponse<RankedEventsResponse>(request.downloadHandler.text));

                yield return new WaitForSecondsRealtime(eventPollSeconds);
            }

            eventPollRoutine = null;
        }

        private IEnumerator PostEvent(RankedEventRequest payload)
        {
            string pathPrefix = string.IsNullOrWhiteSpace(activePathPrefix) ? RankedPathPrefix : activePathPrefix;
            yield return PostJson(BaseUrl + pathPrefix + "/event", payload, _ => { }, error => SetError(error));
        }

        private bool TryApplyMatchResponse(RankedMatchResponse response)
        {
            if (response == null || !response.success)
            {
                SetError(response != null && !string.IsNullOrWhiteSpace(response.error)
                    ? response.error
                    : "Invalid ranked match response.");
                return false;
            }

            if (!response.matched)
                return false;

            MatchId = response.matchId ?? string.Empty;
            MatchSeed = response.seed;
            PlayerIndex = Mathf.Clamp(response.playerIndex, 1, 2);
            Opponent = response.opponent;
            lastEventSeq = 0;

            SetStatus("Opponent found");
            SetError(string.Empty);

            RankedMatchInfo match = new RankedMatchInfo
            {
                matchId = MatchId,
                seed = MatchSeed,
                playerIndex = PlayerIndex,
                opponent = Opponent
            };
            MatchFound?.Invoke(match);
            Log($"Match found: {MatchId} Seed={MatchSeed} PlayerIndex={PlayerIndex}");
            return true;
        }

        private RankedQueueRequest CreateQueueRequest()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            MahjongBattleData battle = null;
            if (profile != null)
            {
                profile.EnsureData();
                battle = profile.Mahjong != null ? profile.Mahjong.Battle : null;
            }

            return new RankedQueueRequest
            {
                token = GetSessionToken(),
                rankTier = battle != null ? battle.RankTier : "Unranked",
                rankPoints = battle != null ? Mathf.Max(0, battle.RankPoints) : 0
            };
        }

        private RankedEventRequest CreateEventRequest(string type)
        {
            return new RankedEventRequest
            {
                token = GetSessionToken(),
                matchId = MatchId,
                type = type
            };
        }

        private void ApplyEvents(RankedEventsResponse response)
        {
            if (response == null || !response.success || response.events == null)
                return;

            for (int i = 0; i < response.events.Length; i++)
            {
                RankedServerEvent item = response.events[i];
                if (item == null)
                    continue;

                lastEventSeq = Mathf.Max(lastEventSeq, item.seq);

                if (string.Equals(item.type, "tile", StringComparison.OrdinalIgnoreCase))
                {
                    RemoteTilePicked?.Invoke(item.tileIndex, item.tileId);
                }
                else if (string.Equals(item.type, "reveal", StringComparison.OrdinalIgnoreCase))
                {
                    ServerEventReceived?.Invoke(item);
                }
                else if (string.Equals(item.type, "pair", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.type, "board", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.type, "finish", StringComparison.OrdinalIgnoreCase))
                {
                    ServerEventReceived?.Invoke(item);
                }
                else if (string.Equals(item.type, "damage", StringComparison.OrdinalIgnoreCase))
                {
                    ServerEventReceived?.Invoke(item);

                    BattleBoardSide localTargetSide = item.targetIndex == PlayerIndex
                        ? BattleBoardSide.Player
                        : BattleBoardSide.Opponent;
                    RemoteDamageApplied?.Invoke(localTargetSide, item.amount);
                }
            }
        }

        private IEnumerator PostJson(string url, object payload, Action<string> onSuccess, Action<string> onError)
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
                onError?.Invoke(ReadError(request));
                yield break;
            }

            onSuccess?.Invoke(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
        }

        private void ClearMatch()
        {
            StopMatchEventPolling();
            MatchId = string.Empty;
            MatchSeed = 0;
            PlayerIndex = 0;
            Opponent = null;
            lastEventSeq = 0;
        }

        private void SetStatus(string value)
        {
            if (value == null)
                value = string.Empty;
            if (string.Equals(Status, value, StringComparison.Ordinal))
                return;

            Status = value;
            StatusChanged?.Invoke(Status);
        }

        private void SetError(string value)
        {
            if (value == null)
                value = string.Empty;
            if (string.Equals(LastError, value, StringComparison.Ordinal))
                return;

            LastError = value;
            ErrorChanged?.Invoke(LastError);
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

            return string.IsNullOrWhiteSpace(request.error) ? "Request failed." : request.error;
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

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log("[OnlineRankedBattleNetwork] " + message, this);
        }

        [Serializable]
        public sealed class RankedMatchInfo
        {
            public string matchId;
            public int seed;
            public int playerIndex;
            public RankedOpponentInfo opponent;
        }

        [Serializable]
        public sealed class RankedOpponentInfo
        {
            public string id;
            public string displayName;
            public string publicPlayerId;
            public int avatarId;
            public string rankTier;
            public int rankPoints;
        }

        [Serializable]
        public sealed class RankedBoardTile
        {
            public string id;
            public int x;
            public int y;
            public int z;
        }

        [Serializable]
        public sealed class RankedBoardSlot
        {
            public int x;
            public int y;
            public int z;
        }

        [Serializable]
        private sealed class RankedQueueRequest
        {
            public string token;
            public string rankTier;
            public int rankPoints;
        }

        [Serializable]
        private sealed class TokenRequest
        {
            public string token;
        }

        [Serializable]
        private sealed class RankedMatchResponse
        {
            public bool success;
            public bool matched;
            public string error;
            public string status;
            public string matchId;
            public int seed;
            public int playerIndex;
            public RankedOpponentInfo opponent;
        }

        [Serializable]
        private sealed class RankedEventRequest
        {
            public string token;
            public string matchId;
            public string type;
            public int tileIndex;
            public string tileId;
            public string targetSide;
            public int amount;
            public int maxHp;
            public int damagePerPair;
            public RankedBoardTile[] tiles;
            public RankedBoardSlot[] slots;
            public string[] tilePool;
        }

        [Serializable]
        private sealed class RankedEventsResponse
        {
            public bool success;
            public string error;
            public RankedServerEvent[] events;
        }

        [Serializable]
        public sealed class RankedServerEvent
        {
            public int seq;
            public int recipientIndex;
            public int senderIndex;
            public int actorIndex;
            public int targetIndex;
            public int winnerIndex;
            public string type;
            public int tileIndex;
            public string tileId;
            public int firstTileIndex;
            public int secondTileIndex;
            public string firstTileId;
            public string secondTileId;
            public bool matched;
            public string targetSide;
            public int amount;
            public int hpAfter;
            public int maxHp;
            public RankedBoardTile[] tiles;
            public string createdAt;
        }

        [Serializable]
        private sealed class BasicResponse
        {
            public bool success;
            public string error;
        }
    }
}
