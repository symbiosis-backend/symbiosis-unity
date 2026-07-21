using UnityEngine;

namespace OzGame.Okey
{
    public class TurnMgr : MonoBehaviour
    {
        [SerializeField] private OkeyGame game;

        public int CurrentSeat => game != null && game.Match != null ? game.Match.currentTurnSeat : -1;
        public TurnPhase Phase => game != null && game.Match != null ? game.Match.turnPhase : TurnPhase.Locked;

        private void Awake()
        {
            if (game == null) game = GetComponent<OkeyGame>();
        }

        public bool IsLocalTurn()
        {
            if (game == null || game.Match == null) return false;
            var player = game.Match.CurrentPlayer;
            return player != null && !player.isBot;
        }
    }
}
