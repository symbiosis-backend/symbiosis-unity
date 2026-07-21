using Dynasty.Legacy.CorrosionCollapse.Networking;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Core
{
    public readonly struct DiceRoll
    {
        public readonly int dice1;
        public readonly int dice2;
        public int Sum => dice1 + dice2;

        public DiceRoll(int dice1, int dice2)
        {
            this.dice1 = dice1;
            this.dice2 = dice2;
        }
    }

    public sealed class DiceSystem
    {
        private readonly IServerAuthority serverAuthority;

        public DiceSystem(IServerAuthority serverAuthority)
        {
            this.serverAuthority = serverAuthority;
        }

        public DiceRoll RollDice(int playerId)
        {
            if (!serverAuthority.IsServer)
            {
                Debug.LogWarning("[Dice] Roll rejected: only server can generate dice");
                return new DiceRoll(0, 0);
            }

            int dice1 = Random.Range(1, 7);
            int dice2 = Random.Range(1, 7);
            serverAuthority.BroadcastDiceResult(playerId, dice1, dice2);
            return new DiceRoll(dice1, dice2);
        }
    }
}
