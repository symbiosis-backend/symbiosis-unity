using System;
using System.Collections;
using System.Text;
using MahjongGame.Networking;
using MahjongGame.Multiplayer;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame.Tournaments
{
    [DisallowMultipleComponent]
    public sealed class TournamentService : MonoBehaviour
    {
        private const string KeySessionToken = "symbiosis_server_session_token";

        public static TournamentService I { get; private set; }

        public TournamentListResponse LastList { get; private set; }
        public TournamentActiveResponse LastActive { get; private set; }
        public TournamentBracketResponse LastBracket { get; private set; }
        public TournamentFundsResponse LastFunds { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        public event Action<TournamentActiveResponse> ActiveChanged;
        public event Action<TournamentListResponse> ListChanged;
        public event Action<TournamentBracketResponse> BracketChanged;
        public event Action<TournamentFundsResponse> FundsChanged;
        public event Action<string> ErrorChanged;

        public static TournamentService EnsureInstance()
        {
            if (I != null)
                return I;

            GameObject host = new GameObject("TournamentService");
            return host.AddComponent<TournamentService>();
        }

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RefreshAll()
        {
            StartCoroutine(GetListRoutine());
            StartCoroutine(GetActiveRoutine());
            StartCoroutine(GetFundsRoutine());
        }

        public void RefreshActive()
        {
            StartCoroutine(GetActiveRoutine());
        }

        public void Join(int tournamentId = 0)
        {
            StartCoroutine(JoinRoutine(tournamentId));
        }

        public void Leave(int tournamentId)
        {
            if (tournamentId <= 0)
                return;

            StartCoroutine(PostTokenRoutine<TournamentBasicResponse>(
                "/tournaments/" + tournamentId + "/leave",
                response =>
                {
                    if (response != null && response.success)
                        RefreshAll();
                }));
        }

        public void Claim(int rewardId)
        {
            if (rewardId <= 0)
                return;

            StartCoroutine(PostTokenRoutine<TournamentClaimResponse>(
                "/tournaments/rewards/" + rewardId + "/claim",
                response =>
                {
                    if (response != null && response.success)
                        RefreshAll();
                }));
        }

        public void ContinueCurrentMatch(string battleSceneName)
        {
            if (!BattleTotemRequirementUI.EnsureBattleReady())
                return;

            TournamentActiveResponse active = LastActive;
            if (active == null || active.currentMatch == null || string.IsNullOrWhiteSpace(active.battleMatchId))
                return;

            MahjongBattleOpponentData opponent = new MahjongBattleOpponentData
            {
                Id = active.opponent != null ? active.opponent.id : "tournament_peer",
                DisplayName = active.opponent != null && !string.IsNullOrWhiteSpace(active.opponent.displayName) ? active.opponent.displayName : "Tournament Opponent",
                AllianceTag = active.opponent != null ? active.opponent.allianceTag : string.Empty,
                AllianceLevel = active.opponent != null ? Mathf.Max(0, active.opponent.allianceLevel) : 0,
                AvatarId = active.opponent != null ? Mathf.Max(0, active.opponent.avatarId) : 0,
                Gender = active.opponent != null ? MahjongBattleOpponentData.ParseGender(active.opponent.gender) : PlayerGender.NotSpecified,
                CharacterId = active.opponent != null ? active.opponent.characterId : string.Empty,
                RankTier = active.opponent != null ? active.opponent.rankTier : "Bronze",
                RankPoints = active.opponent != null ? Mathf.Max(0, active.opponent.rankPoints) : 0,
                Level = active.opponent != null ? Mathf.Max(1, 1 + Mathf.Max(0, active.opponent.rankPoints) / 100) : 1,
                IsBot = false,
                Loadout = active.opponent?.loadout?.Clone()
            };

            OnlineRankedBattleNetwork.RankedMatchInfo match = new OnlineRankedBattleNetwork.RankedMatchInfo
            {
                matchId = active.battleMatchId,
                seed = Mathf.Max(1, active.currentMatch.battleSeed),
                playerIndex = ResolvePlayerIndex(active.currentMatch, active.opponent),
                source = "tournament",
                tournamentId = active.currentMatch.tournamentId,
                tournamentMatchId = active.currentMatch.id,
                roundIndex = active.currentMatch.roundIndex,
                opponent = active.opponent
            };

            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.TournamentMatch);
            MahjongSession.StartTournamentBattle(opponent, match.seed, match.tournamentId, match.tournamentMatchId, match.roundIndex);
            OnlineRankedBattleNetwork.EnsureInstance().ActivateTournamentMatch(match);
            UnityEngine.SceneManagement.SceneManager.LoadScene(string.IsNullOrWhiteSpace(battleSceneName) ? "GameMahjongBattle" : battleSceneName);
        }

        public IEnumerator GetBracket(int tournamentId, Action<TournamentBracketResponse> completed)
        {
            if (tournamentId <= 0)
            {
                completed?.Invoke(null);
                yield break;
            }

            yield return GetJson("/tournaments/" + tournamentId + "/bracket", completed);
        }

        public IEnumerator GetGrandFunds(Action<TournamentFundsResponse> completed)
        {
            yield return GetJson("/tournaments/grand-funds", completed);
        }

        public void RefreshBracket(int tournamentId)
        {
            if (tournamentId <= 0)
                return;

            StartCoroutine(GetBracket(tournamentId, response =>
            {
                if (response == null || !response.success)
                    return;

                LastBracket = response;
                BracketChanged?.Invoke(response);
            }));
        }

        public void RefreshFunds()
        {
            StartCoroutine(GetFundsRoutine());
        }

        private IEnumerator GetListRoutine()
        {
            string token = GetSessionToken();
            string path = string.IsNullOrWhiteSpace(token)
                ? "/tournaments"
                : "/tournaments?token=" + UnityWebRequest.EscapeURL(token);

            yield return GetJson<TournamentListResponse>(
                path,
                response =>
                {
                    if (response == null || !response.success)
                        return;

                    LastList = response;
                    ListChanged?.Invoke(response);
                });
        }

        private IEnumerator GetFundsRoutine()
        {
            yield return GetJson<TournamentFundsResponse>(
                "/tournaments/grand-funds",
                response =>
                {
                    if (response == null || !response.success)
                        return;

                    LastFunds = response;
                    FundsChanged?.Invoke(response);
                });
        }

        private IEnumerator GetActiveRoutine()
        {
            string token = GetSessionToken();
            yield return GetJson<TournamentActiveResponse>(
                "/tournaments/me/active?token=" + UnityWebRequest.EscapeURL(token),
                response =>
                {
                    if (response == null || !response.success)
                        return;

                    LastActive = response;
                    ActiveChanged?.Invoke(response);
                });
        }

        private IEnumerator JoinRoutine(int tournamentId)
        {
            string path = "/tournaments/" + Mathf.Max(0, tournamentId) + "/join";
            yield return PostTokenRoutine<TournamentJoinResponse>(
                path,
                response =>
                {
                    if (response == null || !response.success)
                        return;

                    if (response.active != null)
                    {
                        LastActive = response.active;
                        ActiveChanged?.Invoke(response.active);
                    }

                    RefreshAll();
                });
        }

        private IEnumerator GetJson<T>(string path, Action<T> completed) where T : TournamentBasicResponse
        {
            for (int i = 0; i < BackendEndpoints.BaseUrls.Length; i++)
            {
                using UnityWebRequest request = UnityWebRequest.Get(BackendEndpoints.BuildUrl(BackendEndpoints.BaseUrls[i], path));
                BackendEndpoints.ApplyClientVersionHeaders(request);
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (BackendEndpoints.RequestFailed(request))
                {
                    if (BackendEndpoints.CanRetryWithFallback(request) && i < BackendEndpoints.BaseUrls.Length - 1)
                        continue;

                    SetError(ReadError(request));
                    completed?.Invoke(null);
                    yield break;
                }

                T response = Parse<T>(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                if (response == null || !response.success)
                    SetError(response != null && !string.IsNullOrWhiteSpace(response.error) ? response.error : "Tournament request failed.");
                else
                    SetError(string.Empty);

                completed?.Invoke(response);
                yield break;
            }
        }

        private IEnumerator PostTokenRoutine<T>(string path, Action<T> completed) where T : TournamentBasicResponse
        {
            TokenRequest payload = new TokenRequest { token = GetSessionToken() };
            if (CurrencyService.I != null)
                payload.clientOzTileBalance = Mathf.Max(0, CurrencyService.I.GetOzTile());
            string json = JsonUtility.ToJson(payload);

            for (int i = 0; i < BackendEndpoints.BaseUrls.Length; i++)
            {
                using UnityWebRequest request = new UnityWebRequest(BackendEndpoints.BuildUrl(BackendEndpoints.BaseUrls[i], path), "POST");
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                BackendEndpoints.ApplyClientVersionHeaders(request);
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (BackendEndpoints.RequestFailed(request))
                {
                    if (BackendEndpoints.CanRetryWithFallback(request) && i < BackendEndpoints.BaseUrls.Length - 1)
                        continue;

                    SetError(ReadError(request));
                    completed?.Invoke(null);
                    yield break;
                }

                T response = Parse<T>(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                if (response == null || !response.success)
                    SetError(response != null && !string.IsNullOrWhiteSpace(response.error) ? response.error : "Tournament request failed.");
                else
                {
                    SetError(string.Empty);
                    SyncLocalOzTileFromResponse(response);
                }

                completed?.Invoke(response);
                yield break;
            }
        }

        private static int ResolvePlayerIndex(TournamentMatchInfo match, OnlineRankedBattleNetwork.RankedOpponentInfo opponent)
        {
            if (match == null || opponent == null)
                return 1;

            int opponentId;
            if (!int.TryParse(opponent.id, out opponentId))
                return 1;

            return match.playerAUserId == opponentId ? 2 : 1;
        }

        private void SetError(string value)
        {
            value = value ?? string.Empty;
            if (string.Equals(LastError, value, StringComparison.Ordinal))
                return;

            LastError = value;
            ErrorChanged?.Invoke(value);
        }

        private static void SyncLocalOzTileFromResponse(TournamentBasicResponse response)
        {
            if (CurrencyService.I == null)
                return;

            TournamentJoinResponse join = response as TournamentJoinResponse;
            if (join != null && join.active != null)
            {
                CurrencyService.I.SetOzTile(Mathf.Max(0, join.active.ozTileBalance));
                return;
            }

            TournamentActiveResponse active = response as TournamentActiveResponse;
            if (active != null && (active.active != null || active.currentMatch != null || (active.pendingRewards != null && active.pendingRewards.Length > 0)))
                CurrencyService.I.SetOzTile(Mathf.Max(0, active.ozTileBalance));
        }

        private static string GetSessionToken()
        {
            return PlayerPrefs.GetString(KeySessionToken, string.Empty);
        }

        private static string ReadError(UnityWebRequest request)
        {
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                TournamentBasicResponse response = Parse<TournamentBasicResponse>(responseText);
                if (response != null && !string.IsNullOrWhiteSpace(response.error))
                    return response.error;
            }

            return string.IsNullOrWhiteSpace(request.error) ? "Tournament request failed." : request.error;
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

        [Serializable]
        private sealed class TokenRequest
        {
            public string token;
            public int clientOzTileBalance;
        }
    }
}
