using System.Collections;
using System.Linq;
using UnityEngine;

namespace OzGame.Okey
{
    public class OkeySmokeTest : MonoBehaviour
    {
        [SerializeField] private OkeyGame game;
        [SerializeField] private bool runOnStart;
        [SerializeField] private float botWaitSeconds = 4f;

        public string lastReport;

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<OkeyGame>();
        }

        private IEnumerator Start()
        {
            if (!runOnStart) yield break;
            yield return Run();
        }

        public IEnumerator Run()
        {
            if (game == null)
            {
                Fail("missing OkeyGame");
                yield break;
            }

            game.StartLocalBots();
            yield return null;

            var match = game.Match;
            if (match == null) { Fail("match did not start"); yield break; }
            if (match.players.Count != 4) { Fail("expected 4 players"); yield break; }
            if (match.stockPile.Count != 49) { Fail($"expected stock 49 after deal, got {match.stockPile.Count}"); yield break; }

            var local = match.players.FirstOrDefault(p => !p.isBot);
            if (local == null) { Fail("missing local player"); yield break; }
            if (local.hand.Count != 15) { Fail($"local should start with 15 tiles, got {local.hand.Count}"); yield break; }
            if (match.players.Where(p => p.isBot).Any(p => p.hand.Count != 14)) { Fail("each bot should start with 14 tiles"); yield break; }

            var discard = local.hand[0].id;
            game.Discard(discard);
            yield return null;
            if (local.hand.Count != 14) { Fail("local discard did not remove tile"); yield break; }
            if (local.discardPile.Count != 1) { Fail("local discard pile not updated"); yield break; }
            if (match.turnPhase != TurnPhase.WaitingDraw) { Fail("turn should wait draw after discard"); yield break; }

            var startSeat = match.currentTurnSeat;
            var timeout = Time.realtimeSinceStartup + botWaitSeconds;
            while (Time.realtimeSinceStartup < timeout && match.currentTurnSeat == startSeat)
                yield return null;

            if (match.currentTurnSeat == startSeat) { Fail("bot did not advance turn"); yield break; }

            lastReport = "OK: deal, local discard, bot turn advance";
            Debug.Log($"[OkeySmokeTest] {lastReport}");
        }

        private void Fail(string message)
        {
            lastReport = $"FAIL: {message}";
            Debug.LogError($"[OkeySmokeTest] {lastReport}");
        }
    }
}
