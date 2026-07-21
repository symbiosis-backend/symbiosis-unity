using System;
using UnityEngine;

namespace OzGame.Okey
{
    public enum OkeyGamePhase { Loading, WaitingPlayers, Dealing, PlayerTurn, BotTurn, RoundEnd, MatchEnd, Reconnecting }

    public class OkeyGame : MonoBehaviour
    {
        [SerializeField] private OkeyLocalNet localNet;
        [SerializeField] private OkeyMode mode = OkeyMode.DuzOkey;
        [SerializeField] private string localPlayerId = "local-player";
        [SerializeField] private bool startOnAwake = true;

        public OkeyMatch Match { get; private set; }
        public OkeyGamePhase Phase { get; private set; } = OkeyGamePhase.Loading;

        public event Action<OkeyMatch> StateChanged;

        private IDisposable stateSub;

        private void Awake()
        {
            if (localNet == null) localNet = GetComponent<OkeyLocalNet>();
        }

        private void Start()
        {
            if (startOnAwake) StartLocalBots();
        }

        private void OnDestroy()
        {
            stateSub?.Dispose();
        }

        public void StartLocalBots()
        {
            Phase = OkeyGamePhase.Loading;
            stateSub?.Dispose();
            stateSub = localNet.SubscribeState(OnState);
            localNet.Connect(localPlayerId);
            localNet.QuickMatch(mode, 0);
        }

        public void DrawStock()
        {
            Send(OkeyActionType.DrawStock);
        }

        public void TakeDiscard()
        {
            Send(OkeyActionType.TakeDiscard);
        }

        public void Discard(int tileId)
        {
            Send(OkeyActionType.Discard, tileId);
        }

        public void DeclareWin()
        {
            Send(OkeyActionType.DeclareWin);
        }

        public void DeclareWin(int finalDiscardTileId)
        {
            Send(OkeyActionType.DeclareWin, finalDiscardTileId);
        }

        public void SortHand()
        {
            var player = Match?.players.Find(p => p.playerId == localPlayerId);
            if (player == null) return;
            player.hand = MeldSolver.SortByColorNumber(player.hand, Match.realOkeyTile);
            StateChanged?.Invoke(Match);
        }

        public void SortPairs()
        {
            var player = Match?.players.Find(p => p.playerId == localPlayerId);
            if (player == null) return;
            player.hand = MeldSolver.SortByPairs(player.hand, Match.realOkeyTile);
            StateChanged?.Invoke(Match);
        }

        public void SortMelds()
        {
            var player = Match?.players.Find(p => p.playerId == localPlayerId);
            if (player == null) return;
            player.hand = MeldSolver.SortByMeldHints(player.hand, Match.realOkeyTile);
            StateChanged?.Invoke(Match);
        }

        public void CifteGit()
        {
            Send(OkeyActionType.CifteGit);
        }

        private void Send(OkeyActionType type, int tileId = -1)
        {
            localNet.SendAction(new OkeyAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                playerId = localPlayerId,
                actionType = type,
                tileId = tileId,
                clientTime = Time.realtimeSinceStartupAsDouble
            });
        }

        private void OnState(OkeyStateMsg msg)
        {
            Match = msg.match;
            Phase = ResolvePhase(Match);
            StateChanged?.Invoke(Match);
        }

        private static OkeyGamePhase ResolvePhase(OkeyMatch match)
        {
            if (match == null) return OkeyGamePhase.Loading;
            if (match.roundState == OkeyMatchState.RoundEnding) return OkeyGamePhase.RoundEnd;
            if (match.roundState == OkeyMatchState.MatchEnding) return OkeyGamePhase.MatchEnd;
            if (match.roundState == OkeyMatchState.Dealing) return OkeyGamePhase.Dealing;
            var player = match.CurrentPlayer;
            return player != null && player.isBot ? OkeyGamePhase.BotTurn : OkeyGamePhase.PlayerTurn;
        }
    }
}
