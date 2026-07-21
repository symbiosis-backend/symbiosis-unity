using System;
using System.Collections;
using System.Text;
using MahjongGame.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class DuelChallengeService : MonoBehaviour
    {
        public static DuelChallengeService I { get; private set; }

        private const string BaseUrl = "https://dlsymbiosis.com";
        private const string KeySessionToken = "symbiosis_server_session_token";

        [SerializeField, Min(1f)] private float pollSeconds = 2f;

        private Coroutine incomingPollRoutine;
        private DuelChallengeInfo currentIncomingChallenge;

        public event Action<DuelChallengeInfo> IncomingChallengeChanged;
        public DuelChallengeInfo CurrentIncomingChallenge => currentIncomingChallenge;

        public static DuelChallengeService EnsureInstance()
        {
            if (I != null)
                return I;

            GameObject host = new GameObject("DuelChallengeService");
            return host.AddComponent<DuelChallengeService>();
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

        public void StartIncomingPolling()
        {
            if (incomingPollRoutine != null)
                StopCoroutine(incomingPollRoutine);

            incomingPollRoutine = StartCoroutine(IncomingPollRoutine());
        }

        public void StopIncomingPolling()
        {
            if (incomingPollRoutine == null)
                return;

            StopCoroutine(incomingPollRoutine);
            incomingPollRoutine = null;
        }

        public IEnumerator SendChallenge(string nickname, int stakeOzTile, Action<bool, string, DuelChallengeInfo> completed)
        {
            DuelChallengeRequest payload = CreateProfileRequest<DuelChallengeRequest>();
            payload.nickname = nickname ?? string.Empty;
            payload.stakeOzTile = Mathf.Max(1, stakeOzTile);

            DuelChallengeEnvelope response = null;
            string error = string.Empty;
            yield return PostJson(
                BaseUrl + "/battle/duel/challenge",
                payload,
                text => response = ParseResponse<DuelChallengeEnvelope>(text),
                value => error = value);

            completed?.Invoke(response != null && response.success, ResolveMessage(response, error), response != null ? response.challenge : null);
        }

        public IEnumerator PollChallengeStatus(string challengeId, Action<bool, string, DuelChallengeInfo> completed)
        {
            string url = BaseUrl + "/battle/duel/status" +
                         "?token=" + UnityWebRequest.EscapeURL(GetSessionToken()) +
                         "&challengeId=" + UnityWebRequest.EscapeURL(challengeId ?? string.Empty);

            DuelChallengeEnvelope response = null;
            string error = string.Empty;
            yield return GetJson(url, text => response = ParseResponse<DuelChallengeEnvelope>(text), value => error = value);
            completed?.Invoke(response != null && response.success, ResolveMessage(response, error), response != null ? response.challenge : null);
        }

        public IEnumerator RespondToChallenge(string challengeId, bool accepted, Action<bool, string, DuelChallengeInfo, OnlineRankedBattleNetwork.RankedMatchInfo> completed)
        {
            DuelRespondRequest payload = CreateProfileRequest<DuelRespondRequest>();
            payload.challengeId = challengeId ?? string.Empty;
            payload.accepted = accepted;

            DuelRespondEnvelope response = null;
            string error = string.Empty;
            yield return PostJson(
                BaseUrl + "/battle/duel/respond",
                payload,
                text => response = ParseResponse<DuelRespondEnvelope>(text),
                value => error = value);

            OnlineRankedBattleNetwork.RankedMatchInfo match = null;
            if (response != null && response.matched)
            {
                match = new OnlineRankedBattleNetwork.RankedMatchInfo
                {
                    matchId = response.matchId,
                    seed = response.seed,
                    playerIndex = response.playerIndex,
                    opponent = response.opponent
                };
            }

            completed?.Invoke(response != null && response.success, ResolveMessage(response, error), response != null ? response.challenge : null, match);
        }

        public int GetLocalMaxStakeOzTile()
        {
            int rankPoints = ResolvePlayerRankPoints();
            if (rankPoints >= 900) return 10000;
            if (rankPoints >= 500) return 5000;
            if (rankPoints >= 250) return 2500;
            if (rankPoints >= 100) return 1000;
            return 500;
        }

        private IEnumerator IncomingPollRoutine()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(Mathf.Max(1f, pollSeconds));
            while (isActiveAndEnabled)
            {
                yield return PollIncomingOnce();
                yield return wait;
            }

            incomingPollRoutine = null;
        }

        private IEnumerator PollIncomingOnce()
        {
            ProfileSnapshot snapshot = CreateProfileSnapshot();
            string url = BaseUrl + "/battle/duel/incoming" +
                         "?token=" + UnityWebRequest.EscapeURL(GetSessionToken()) +
                         "&rankTier=" + UnityWebRequest.EscapeURL(snapshot.rankTier) +
                         "&rankPoints=" + snapshot.rankPoints +
                         "&characterId=" + UnityWebRequest.EscapeURL(snapshot.characterId);

            DuelIncomingEnvelope response = null;
            yield return GetJson(url, text => response = ParseResponse<DuelIncomingEnvelope>(text), _ => { });

            DuelChallengeInfo best = null;
            if (response != null && response.success && response.challenges != null)
            {
                for (int i = 0; i < response.challenges.Length; i++)
                {
                    DuelChallengeInfo item = response.challenges[i];
                    if (item == null || !string.Equals(item.status, "pending", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (best == null || item.remainingSeconds > best.remainingSeconds)
                        best = item;
                }
            }

            currentIncomingChallenge = best;
            IncomingChallengeChanged?.Invoke(best);
        }

        private static T CreateProfileRequest<T>() where T : ProfileSnapshot, new()
        {
            ProfileSnapshot snapshot = CreateProfileSnapshot();
            return new T
            {
                token = GetSessionToken(),
                characterId = snapshot.characterId,
                rankTier = snapshot.rankTier,
                rankPoints = snapshot.rankPoints,
                loadout = snapshot.loadout
            };
        }

        private static ProfileSnapshot CreateProfileSnapshot()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            MahjongBattleData battle = null;
            if (profile != null)
            {
                profile.EnsureData();
                battle = profile.Mahjong != null ? profile.Mahjong.Battle : null;
            }

            BattleTileStore store = BattleTileStore.I != null
                ? BattleTileStore.I
                : FindAnyObjectByType<BattleTileStore>(FindObjectsInactive.Include);
            BattleLoadoutSnapshot.TryCreateFromProfile(profile, store, out BattleLoadoutSnapshot loadout);

            return new ProfileSnapshot
            {
                token = GetSessionToken(),
                characterId = BattleCharacterSelectionService.Instance != null ? BattleCharacterSelectionService.Instance.SelectedCharacterId : string.Empty,
                rankTier = battle != null ? battle.RankTier : "Unranked",
                rankPoints = battle != null ? Mathf.Max(0, battle.RankPoints) : 0,
                loadout = loadout
            };
        }

        private static int ResolvePlayerRankPoints()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return 0;

            profile.EnsureData();
            MahjongBattleData battle = profile.Mahjong != null ? profile.Mahjong.Battle : null;
            return battle != null ? Mathf.Max(0, battle.RankPoints) : 0;
        }

        private static IEnumerator PostJson(string url, object payload, Action<string> onSuccess, Action<string> onError)
        {
            string json = JsonUtility.ToJson(payload);
            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            BackendEndpoints.ApplyClientVersionHeaders(request);
            request.timeout = 10;
            yield return request.SendWebRequest();
            CompleteRequest(request, onSuccess, onError);
        }

        private static IEnumerator GetJson(string url, Action<string> onSuccess, Action<string> onError)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            BackendEndpoints.ApplyClientVersionHeaders(request);
            request.timeout = 10;
            yield return request.SendWebRequest();
            CompleteRequest(request, onSuccess, onError);
        }

        private static void CompleteRequest(UnityWebRequest request, Action<string> onSuccess, Action<string> onError)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                onError?.Invoke(ReadError(request));
                return;
            }

            onSuccess?.Invoke(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
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

        private static string ResolveMessage(BasicResponse response, string fallback)
        {
            if (response != null && !string.IsNullOrWhiteSpace(response.error))
                return response.error;

            return fallback ?? string.Empty;
        }

        private static string GetSessionToken()
        {
            return PlayerPrefs.GetString(KeySessionToken, string.Empty);
        }

        [Serializable]
        private class ProfileSnapshot
        {
            public string token;
            public string characterId;
            public string rankTier;
            public int rankPoints;
            public BattleLoadoutSnapshot loadout;
        }

        [Serializable]
        private sealed class DuelChallengeRequest : ProfileSnapshot
        {
            public string nickname;
            public int stakeOzTile;
        }

        [Serializable]
        private sealed class DuelRespondRequest : ProfileSnapshot
        {
            public string challengeId;
            public bool accepted;
        }

        [Serializable]
        public sealed class DuelChallengeInfo
        {
            public string id;
            public string status;
            public int stakeOzTile;
            public string expiresAt;
            public int remainingSeconds;
            public int maxStakeOzTile;
            public OnlineRankedBattleNetwork.RankedOpponentInfo challenger;
            public OnlineRankedBattleNetwork.RankedOpponentInfo target;
            public bool isIncoming;
            public bool isOutgoing;
            public OnlineRankedBattleNetwork.RankedMatchInfo match;
        }

        [Serializable]
        private class BasicResponse
        {
            public bool success;
            public string error;
        }

        [Serializable]
        private sealed class DuelChallengeEnvelope : BasicResponse
        {
            public DuelChallengeInfo challenge;
        }

        [Serializable]
        private sealed class DuelIncomingEnvelope : BasicResponse
        {
            public DuelChallengeInfo[] challenges;
        }

        [Serializable]
        private sealed class DuelRespondEnvelope : BasicResponse
        {
            public DuelChallengeInfo challenge;
            public bool matched;
            public string matchId;
            public int seed;
            public int playerIndex;
            public OnlineRankedBattleNetwork.RankedOpponentInfo opponent;
        }
    }
}
