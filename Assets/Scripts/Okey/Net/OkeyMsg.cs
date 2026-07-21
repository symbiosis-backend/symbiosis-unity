using System;

namespace OzGame.Okey
{
    public class OkeyStateMsg
    {
        public OkeyMatch match;
        public string localPlayerId;
    }

    public class OkeyActionMsg
    {
        public OkeyAction action;
    }

    public class OkeyRoomMsg
    {
        public OkeyRoom room;
    }

    public class OkeyErrorMsg
    {
        public string code;
        public string message;
    }

    public class OkeySub : IDisposable
    {
        private readonly Action dispose;
        public OkeySub(Action dispose) => this.dispose = dispose;
        public void Dispose() => dispose?.Invoke();
    }
}
