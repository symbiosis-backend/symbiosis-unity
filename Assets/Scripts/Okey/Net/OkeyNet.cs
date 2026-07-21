using System;
using System.Collections.Generic;

namespace OzGame.Okey
{
    public interface IOkeyNet
    {
        void Connect(string playerId);
        void Disconnect();
        void CreateRoom(OkeyRoom room);
        void JoinRoom(string roomId);
        void LeaveRoom();
        void QuickMatch(OkeyMode mode, int stake);
        void SendAction(OkeyAction action);
        IDisposable SubscribeState(Action<OkeyStateMsg> handler);
        IDisposable SubscribeRoomList(Action<IReadOnlyList<OkeyRoom>> handler);
        void ReconnectMatch(string matchId);
    }
}
