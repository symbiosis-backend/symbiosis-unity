using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OzGame.Okey
{
    public class OkeyDebugUI : MonoBehaviour
    {
        [SerializeField] private OkeyGame game;
        [SerializeField] private Text stateText;
        [SerializeField] private Text tableText;
        [SerializeField] private Transform handRoot;
        [SerializeField] private Transform controlRoot;
        [SerializeField] private Button tileButtonPrefab;
        [SerializeField] private Button commandButtonPrefab;

        private readonly List<Button> tileButtons = new List<Button>();
        private readonly List<Button> commandButtons = new List<Button>();
        private int selectedTileId = -1;

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<OkeyGame>();
        }

        private void OnEnable()
        {
            if (game != null) game.StateChanged += Render;
            BuildCommands();
            Render(game != null ? game.Match : null);
        }

        private void OnDisable()
        {
            if (game != null) game.StateChanged -= Render;
        }

        public void Render(OkeyMatch match)
        {
            if (match == null)
            {
                SetText(stateText, "OzGame Okey\nLoading...");
                SetText(tableText, "");
                ClearHand();
                return;
            }

            var local = match.players.FirstOrDefault(p => !p.isBot);
            SetText(stateText, BuildState(match, local, selectedTileId));
            SetText(tableText, BuildTable(match));
            RenderHand(match, local);
            RefreshCommands(match, local);
        }

        private void BuildCommands()
        {
            if (controlRoot == null || commandButtonPrefab == null) return;
            for (var i = controlRoot.childCount - 1; i >= 0; i--)
                Destroy(controlRoot.GetChild(i).gameObject);
            commandButtons.Clear();

            AddCommand("Draw", () => game.DrawStock());
            AddCommand("Take", () => game.TakeDiscard());
            AddCommand("Discard", () => { if (selectedTileId >= 0) game.Discard(selectedTileId); });
            AddCommand("Win", () => { if (selectedTileId >= 0) game.DeclareWin(selectedTileId); else game.DeclareWin(); });
            AddCommand("Sort", () => game.SortHand());
            AddCommand("Restart", () => game.StartLocalBots());
        }

        private void AddCommand(string label, UnityEngine.Events.UnityAction action)
        {
            var button = Instantiate(commandButtonPrefab, controlRoot);
            button.name = $"Btn_{label}";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
            commandButtons.Add(button);
        }

        private void RefreshCommands(OkeyMatch match, OkeyPlayer local)
        {
            if (commandButtons.Count < 6 || match == null || local == null) return;
            var localTurn = match.CurrentPlayer == local && match.roundState == OkeyMatchState.Playing;
            var waitingDraw = localTurn && match.turnPhase == TurnPhase.WaitingDraw;
            var waitingDiscard = localTurn && match.turnPhase == TurnPhase.WaitingDiscard;

            commandButtons[0].interactable = waitingDraw && match.stockPile.Count > 0;
            commandButtons[1].interactable = waitingDraw && PreviousDiscard(match) != null;
            commandButtons[2].interactable = waitingDiscard && selectedTileId >= 0;
            commandButtons[3].interactable = waitingDiscard;
            commandButtons[4].interactable = true;
            commandButtons[5].interactable = true;
        }

        private void RenderHand(OkeyMatch match, OkeyPlayer player)
        {
            ClearHand();
            if (handRoot == null || tileButtonPrefab == null || player == null) return;
            if (selectedTileId >= 0 && player.hand.All(t => t.id != selectedTileId)) selectedTileId = -1;

            foreach (var tile in player.hand)
            {
                var button = Instantiate(tileButtonPrefab, handRoot);
                button.name = $"Tile_{tile.id}";
                var text = button.GetComponentInChildren<Text>();
                if (text != null) text.text = TileLabel(tile, match.realOkeyTile);
                var colors = button.colors;
                colors.normalColor = tile.id == selectedTileId ? new Color(1f, 0.82f, 0.25f) : TileColor(tile);
                colors.selectedColor = new Color(1f, 0.82f, 0.25f);
                button.colors = colors;
                var id = tile.id;
                button.onClick.AddListener(() =>
                {
                    selectedTileId = id;
                    Render(game.Match);
                });
                tileButtons.Add(button);
            }
        }

        private void ClearHand()
        {
            foreach (var button in tileButtons)
            {
                if (button != null) Destroy(button.gameObject);
            }
            tileButtons.Clear();
        }

        private static string BuildState(OkeyMatch match, OkeyPlayer local, int selectedTileId)
        {
            var current = match.CurrentPlayer;
            var selected = local != null ? local.hand.FirstOrDefault(t => t.id == selectedTileId) : null;
            return
                $"OzGame Okey\n" +
                $"State: {match.roundState}\n" +
                $"Turn: seat {match.currentTurnSeat} {(current != null ? current.displayName : "")}\n" +
                $"Phase: {match.turnPhase}\n" +
                $"Stock: {match.stockPile.Count}\n" +
                $"Your hand: {(local != null ? local.hand.Count : 0)}\n" +
                $"Selected: {TileLabel(selected, match.realOkeyTile)}\n" +
                $"Hint: {BuildHint(match, local)}\n" +
                $"Last error: {match.lastError}";
        }

        private static string BuildTable(OkeyMatch match)
        {
            var lines = new List<string>
            {
                $"Indicator: {TileLabel(match.indicatorTile, match.realOkeyTile)}",
                $"Real Okey: {match.realOkeyTile.color} {match.realOkeyTile.number}",
                ""
            };

            foreach (var player in match.players.OrderBy(p => p.seatIndex))
            {
                var discard = player.discardPile.Count > 0 ? TileLabel(player.discardPile[player.discardPile.Count - 1], match.realOkeyTile) : "-";
                lines.Add($"Seat {player.seatIndex}: {player.displayName} | score {player.score} | hand {player.hand.Count} | discard {discard}");
            }
            return string.Join("\n", lines);
        }

        private static string TileLabel(OkeyTile tile, OkeyTile realOkey)
        {
            if (tile == null) return "-";
            if (tile.type == OkeyTileType.FakeJoker) return $"Fake({realOkey.color} {realOkey.number})";
            var mark = tile.isRealOkey ? "*" : tile.isIndicator ? "!" : "";
            return $"{tile.color.ToString()[0]}{tile.number}{mark}";
        }

        private static OkeyTile PreviousDiscard(OkeyMatch match)
        {
            var prevSeat = (match.currentTurnSeat - (int)match.direction + match.players.Count) % match.players.Count;
            var prev = match.players.FirstOrDefault(p => p.seatIndex == prevSeat);
            return prev != null && prev.discardPile.Count > 0 ? prev.discardPile[prev.discardPile.Count - 1] : null;
        }

        private static string BuildHint(OkeyMatch match, OkeyPlayer local)
        {
            if (local == null) return "waiting for local player";
            if (match.roundState == OkeyMatchState.RoundEnding) return "round finished";
            if (match.CurrentPlayer != local) return "wait for your turn";
            if (match.turnPhase == TurnPhase.WaitingDraw) return "draw from stock or take previous discard";
            if (match.turnPhase == TurnPhase.WaitingDiscard) return "select tile, discard or declare win with selected final discard";
            return "locked";
        }

        private static Color TileColor(OkeyTile tile)
        {
            if (tile == null) return Color.white;
            if (tile.type == OkeyTileType.FakeJoker) return new Color(0.9f, 0.9f, 0.9f);
            return tile.color switch
            {
                OkeyColor.Red => new Color(0.95f, 0.32f, 0.28f),
                OkeyColor.Yellow => new Color(1f, 0.82f, 0.25f),
                OkeyColor.Blue => new Color(0.32f, 0.55f, 1f),
                OkeyColor.Black => new Color(0.18f, 0.18f, 0.18f),
                _ => Color.white
            };
        }

        private static void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}
