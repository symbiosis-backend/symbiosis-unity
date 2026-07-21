using System;
using System.Collections.Generic;

namespace OzGame.Okey
{
    public enum OkeyMode { DuzOkey, Okey101 }
    public enum OkeyColor { Red, Yellow, Blue, Black, None }
    public enum OkeyTileType { Number, FakeJoker }
    public enum OkeyRoomStatus { Waiting, FillingWithBots, Starting, Playing, Finished }
    public enum OkeyActionType { Ready, DrawStock, TakeDiscard, Discard, Sort, SortPairs, SortMelds, ShowIndicatorMatch, DeclareWin, CifteGit, Leave, Reconnect, BotMove }
    public enum OkeyMatchState { None, WaitingPlayers, Preparing, Dealing, Playing, RoundEnding, MatchEnding, Canceled, Error }
    public enum TurnPhase { WaitingDraw, WaitingDiscard, WaitingDeclare, Locked }
    public enum OkeyDirection { Clockwise = 1, Anticlockwise = -1 }
    public enum OkeyRunWrap { NoWrap, Allow12_13_1 }
    public enum BotLevel { Easy, Normal, Hard }

    [Serializable]
    public class OkeyTile
    {
        public int id;
        public OkeyColor color;
        public int number;
        public int copyIndex;
        public OkeyTileType type;
        public bool isRealOkey;
        public bool isIndicator;
        public string runtimeGuid;

        public bool IsNumber => type == OkeyTileType.Number;

        public OkeyTile Clone()
        {
            return (OkeyTile)MemberwiseClone();
        }

        public bool SameFace(OkeyTile other)
        {
            if (other == null) return false;
            return type == other.type && color == other.color && number == other.number;
        }
    }

    [Serializable]
    public class OkeyPlayer
    {
        public string playerId;
        public string displayName;
        public int seatIndex;
        public bool isBot;
        public bool isConnected = true;
        public bool isReady;
        public int score = 20;
        public string avatarId;
        public int rank;
        public List<OkeyTile> hand = new List<OkeyTile>();
        public List<OkeyTile> discardPile = new List<OkeyTile>();
        public double lastActionTime;
        public string autoPlayState;
        public bool cifteGit;
    }

    [Serializable]
    public class OkeyRoom
    {
        public string roomId;
        public string title;
        public OkeyMode mode = OkeyMode.DuzOkey;
        public int stake;
        public int maxPlayers = 4;
        public int currentPlayers;
        public bool hasPassword;
        public int speed = 30;
        public bool isPrivate;
        public bool allowBots = true;
        public OkeyRoomStatus status = OkeyRoomStatus.Waiting;
    }

    [Serializable]
    public class OkeyAction
    {
        public string actionId;
        public string playerId;
        public OkeyActionType actionType;
        public int tileId = -1;
        public string payload;
        public double clientTime;
        public double serverTime;
    }

    [Serializable]
    public class OkeyMatch
    {
        public string matchId;
        public string roomId;
        public OkeyMode mode = OkeyMode.DuzOkey;
        public List<OkeyPlayer> players = new List<OkeyPlayer>();
        public int dealerSeat;
        public int currentTurnSeat;
        public OkeyDirection direction = OkeyDirection.Anticlockwise;
        public List<OkeyTile> stockPile = new List<OkeyTile>();
        public OkeyTile indicatorTile;
        public OkeyTile realOkeyTile;
        public int roundNumber = 1;
        public OkeyMatchState roundState = OkeyMatchState.None;
        public TurnPhase turnPhase = TurnPhase.Locked;
        public double turnDeadline;
        public int matchSeed;
        public List<OkeyAction> actionLog = new List<OkeyAction>();
        public int winnerSeat = -1;
        public string lastError;

        public OkeyPlayer CurrentPlayer => players.Find(p => p.seatIndex == currentTurnSeat);
    }

    [Serializable]
    public class OkeyRulesConfig
    {
        public OkeyMode mode = OkeyMode.DuzOkey;
        public int playerCount = 4;
        public int startingScore = 20;
        public OkeyRunWrap runWrap = OkeyRunWrap.NoWrap;
        public bool allowSevenPairs = true;
        public bool allowGosterme = true;
        public bool allowSameColorBonus;
        public int turnSeconds = 30;
    }

    public class OkeyMove
    {
        public OkeyActionType type;
        public int tileId = -1;
        public string reason;
    }

    public class OkeyRoundResult
    {
        public bool finished;
        public int winnerSeat = -1;
        public bool sevenPairs;
        public bool jokerFinish;
        public Dictionary<int, int> scoreDelta = new Dictionary<int, int>();
    }
}
