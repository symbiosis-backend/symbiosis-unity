using System;
using System.Collections.Generic;
using UnityEngine;

namespace OzGame.Okey
{
    public class OkeyLocalNet : MonoBehaviour, IOkeyNet
    {
        [SerializeField] private TileMgr tileMgr;
        [SerializeField] private int turnSeconds = 30;

        private readonly List<Action<OkeyStateMsg>> stateHandlers = new List<Action<OkeyStateMsg>>();
        private readonly List<Action<IReadOnlyList<OkeyRoom>>> roomHandlers = new List<Action<IReadOnlyList<OkeyRoom>>>();
        private OkeyServerSim server;
        private string localPlayerId;
        private int lastPublishedActionCount = -1;

        public OkeyMatch Match => server?.Match;

        private void Awake()
        {
            if (tileMgr == null) tileMgr = GetComponent<TileMgr>();
        }

        private void Update()
        {
            if (server?.Match == null) return;
            server.TickBots();
            if (server.Match.actionLog.Count != lastPublishedActionCount) Publish();
        }

        public void Connect(string playerId)
        {
            localPlayerId = playerId;
        }

        public void Disconnect() { }

        public void CreateRoom(OkeyRoom room)
        {
            QuickMatch(room.mode, room.stake);
        }

        public void JoinRoom(string roomId) { }
        public void LeaveRoom() { }

        public void QuickMatch(OkeyMode mode, int stake)
        {
            var config = new OkeyRulesConfig { mode = mode, turnSeconds = turnSeconds };
            server = new OkeyServerSim(tileMgr, config);
            server.StartBotMatch(string.IsNullOrEmpty(localPlayerId) ? "local-player" : localPlayerId);
            lastPublishedActionCount = -1;
            Publish();
        }

        public void SendAction(OkeyAction action)
        {
            server?.Apply(action);
            Publish();
        }

        public IDisposable SubscribeState(Action<OkeyStateMsg> handler)
        {
            stateHandlers.Add(handler);
            return new OkeySub(() => stateHandlers.Remove(handler));
        }

        public IDisposable SubscribeRoomList(Action<IReadOnlyList<OkeyRoom>> handler)
        {
            roomHandlers.Add(handler);
            return new OkeySub(() => roomHandlers.Remove(handler));
        }

        public void ReconnectMatch(string matchId)
        {
            Publish();
        }

        private void Publish()
        {
            if (server?.Match == null) return;
            lastPublishedActionCount = server.Match.actionLog.Count;
            var msg = new OkeyStateMsg { match = server.Match, localPlayerId = localPlayerId };
            foreach (var handler in stateHandlers.ToArray()) handler?.Invoke(msg);
        }
    }
}
