namespace MahjongGame
{
    public enum MahjongBattleLobbyMode
    {
        None = 0,
        RandomMatch = 1,
        RankedMatch = 2,
        FriendMatch = 3,
        LocalWifiMatch = 4
    }

    public static class MahjongBattleLobbySession
    {
        public static MahjongBattleLobbyMode SelectedMode { get; private set; } = MahjongBattleLobbyMode.None;

        public static void SetMode(MahjongBattleLobbyMode mode)
        {
            SelectedMode = mode;
        }

        public static void Clear()
        {
            SelectedMode = MahjongBattleLobbyMode.None;
        }
    }
}