using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OzGame.Okey
{
    public class OkeyTouchUI : MonoBehaviour
    {
        [SerializeField] private OkeyGame game;

        private RectTransform root;
        private RectTransform rackTopRoot;
        private RectTransform rackBottomRoot;
        private RectTransform tableRoot;
        private RectTransform controlsRoot;
        private Text statusText;
        private Text centerText;
        private RectTransform dragGhost;
        private List<int> activeDragGroup = new List<int>();
        private bool activeDragIsGroup;
        private string lastUiEvent = "ready";
        private readonly List<GameObject> dynamicObjects = new List<GameObject>();
        private readonly HashSet<int> topRowTiles = new HashSet<int>();
        private readonly List<int> manualOrder = new List<int>();
        private readonly int[] rackSlots = Enumerable.Repeat(-1, RackTotalSlots).ToArray();
        private readonly Dictionary<string, Sprite> tileSprites = new Dictionary<string, Sprite>();
        private int selectedTileId = -1;

        private static readonly Color Bg = new Color(0.02f, 0.25f, 0.13f, 1f);
        private static readonly Color Wood = new Color(0.72f, 0.36f, 0.045f, 1f);
        private static readonly Color Felt = new Color(0.04f, 0.58f, 0.25f, 1f);
        private static readonly Color Gold = new Color(0.95f, 0.72f, 0.28f, 1f);
        private static readonly Color Cream = new Color(1f, 0.92f, 0.74f, 1f);
        private const int RackSlotsPerRow = 14;
        private const int RackTotalSlots = 28;

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<OkeyGame>();
            EnsureEventInputModule();
            BuildStatic();
        }

        private void OnEnable()
        {
            if (game != null) game.StateChanged += Render;
            Render(game != null ? game.Match : null);
        }

        private void OnDisable()
        {
            if (game != null) game.StateChanged -= Render;
        }

        private void BuildStatic()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;

            var old = transform.Find("OkeyTouchRoot");
            if (old != null) DestroyImmediate(old.gameObject);

            root = Panel(transform, "OkeyTouchRoot", Vector2.zero, Vector2.one, Bg);
            Panel(root, "FeltGlow", new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.98f), new Color(0.035f, 0.42f, 0.20f, 0.86f));
            BuildFeltPattern(root);

            var coin = Panel(root, "CoinBadge", new Vector2(0.018f, 0.855f), new Vector2(0.17f, 0.955f), new Color(0.035f, 0.10f, 0.045f, 0.92f));
            TextBlock(coin, "CoinText", "Öz  2.500", 24, Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            var top = Panel(root, "TopBar", new Vector2(0.30f, 0.915f), new Vector2(0.70f, 0.982f), new Color(0.025f, 0.14f, 0.075f, 0.76f));
            TextBlock(top, "Title", "ÖzGame OKEY", 22, Cream, TextAnchor.MiddleCenter, new Vector2(0.02f, 0f), new Vector2(0.34f, 1f));
            statusText = TextBlock(top, "Status", "", 15, Gold, TextAnchor.MiddleRight, new Vector2(0.34f, 0f), new Vector2(0.98f, 1f));

            tableRoot = Panel(root, "Table", new Vector2(0.035f, 0.355f), new Vector2(0.965f, 0.90f), new Color(0f, 0f, 0f, 0f));
            centerText = TextBlock(tableRoot, "CenterText", "", 16, Cream, TextAnchor.MiddleCenter, new Vector2(0.39f, 0.40f), new Vector2(0.61f, 0.55f));

            controlsRoot = Panel(root, "Controls", new Vector2(0.81f, 0.78f), new Vector2(0.97f, 0.965f), new Color(0f, 0f, 0f, 0f));
            var rack = Panel(root, "IstakaRack", new Vector2(0.005f, 0.025f), new Vector2(0.995f, 0.33f), new Color(0.78f, 0.41f, 0.055f, 1f));
            Panel(rack, "RackTopShine", new Vector2(0f, 0.91f), new Vector2(1f, 1f), new Color(1f, 0.80f, 0.20f, 0.70f));
            Panel(rack, "RackMidLine", new Vector2(0f, 0.49f), new Vector2(1f, 0.515f), new Color(1f, 0.84f, 0.18f, 0.95f));
            Panel(rack, "RackBottomLine", new Vector2(0f, 0.01f), new Vector2(1f, 0.04f), new Color(1f, 0.84f, 0.18f, 0.95f));
            Panel(rack, "RackGrooveTop", new Vector2(0.24f, 0.525f), new Vector2(0.76f, 0.91f), new Color(0.34f, 0.14f, 0.02f, 0.66f));
            Panel(rack, "RackGrooveBottom", new Vector2(0.24f, 0.075f), new Vector2(0.76f, 0.465f), new Color(0.34f, 0.14f, 0.02f, 0.66f));
            Panel(rack, "RackTopLeftCap", new Vector2(0.232f, 0.525f), new Vector2(0.239f, 0.91f), new Color(1f, 0.84f, 0.18f, 0.82f));
            Panel(rack, "RackTopRightCap", new Vector2(0.761f, 0.525f), new Vector2(0.768f, 0.91f), new Color(1f, 0.84f, 0.18f, 0.82f));
            Panel(rack, "RackBottomLeftCap", new Vector2(0.232f, 0.075f), new Vector2(0.239f, 0.465f), new Color(1f, 0.84f, 0.18f, 0.82f));
            Panel(rack, "RackBottomRightCap", new Vector2(0.761f, 0.075f), new Vector2(0.768f, 0.465f), new Color(1f, 0.84f, 0.18f, 0.82f));
            rackTopRoot = Panel(rack, "RackTopTiles", new Vector2(0.24f, 0.525f), new Vector2(0.76f, 0.91f), new Color(0f, 0f, 0f, 0f));
            rackBottomRoot = Panel(rack, "RackBottomTiles", new Vector2(0.24f, 0.075f), new Vector2(0.76f, 0.465f), new Color(0f, 0f, 0f, 0f));
        }

        private void BuildFeltPattern(RectTransform parent)
        {
            for (var y = 0; y < 6; y++)
            {
                for (var x = 0; x < 12; x++)
                {
                    var min = new Vector2(0.06f + x * 0.08f, 0.38f + y * 0.085f);
                    var max = min + new Vector2(0.026f, 0.026f);
                    var mark = Panel(parent, $"FeltPattern_{x}_{y}", min, max, new Color(1f, 1f, 1f, 0.018f));
                    mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
            }
        }

        private void EnsureEventInputModule()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = go.GetComponent<EventSystem>();
            }

            var newInputType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newInputType == null) return;

            if (eventSystem.GetComponent(newInputType) == null) eventSystem.gameObject.AddComponent(newInputType);

            var legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null) legacy.enabled = false;
        }

        public void Render(OkeyMatch match)
        {
            if (root == null) BuildStatic();
            ClearDynamic();
            if (match == null)
            {
                if (statusText != null) statusText.text = "Loading local Okey table...";
                return;
            }

            var local = match.players.FirstOrDefault(p => !p.isBot);
            if (local != null && selectedTileId >= 0 && local.hand.All(t => t.id != selectedTileId)) selectedTileId = -1;

            RenderStatus(match, local);
            RenderSeats(match, local);
            RenderCenter(match, local);
            RenderControls(match, local);
            RenderHand(match, local);
        }

        private void RenderStatus(OkeyMatch match, OkeyPlayer local)
        {
            if (match.roundState == OkeyMatchState.RoundEnding)
            {
                var winner = match.players.FirstOrDefault(p => p.seatIndex == match.winnerSeat);
                statusText.text = $"Round finished: {(winner != null ? winner.displayName : "winner")}    Stock {match.stockPile.Count}    UI {lastUiEvent}";
                return;
            }

            var current = match.CurrentPlayer;
            var hint = current == local
                ? match.turnPhase == TurnPhase.WaitingDraw ? "Your turn: draw or take discard" : "Your turn: discard or finish"
                : $"Waiting: {(current != null ? current.displayName : "-")}";
            statusText.text = $"{hint}    Stock {match.stockPile.Count}    UI {lastUiEvent}    {(local != null && local.cifteGit ? "Çifte Git" : "")}    {match.lastError}";
        }

        private void RenderSeats(OkeyMatch match, OkeyPlayer local)
        {
            foreach (var player in match.players)
            {
                var pos = SeatRect(player.seatIndex);
                var seat = DynPanel(tableRoot, $"Seat_{player.seatIndex}", pos.min, pos.max, new Color(0.035f, 0.02f, 0.012f, 0.82f));
                var turn = match.currentTurnSeat == player.seatIndex ? " ◀" : "";
                TextBlock(seat, "Name", $"{player.displayName}{turn}\nScore {player.score}\nTiles {player.hand.Count}", 18, player.isBot ? new Color(0.78f, 0.88f, 1f) : Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

                var last = player.discardPile.Count > 0 ? player.discardPile[player.discardPile.Count - 1] : null;
                if (last != null)
                {
                    var discard = TileView(tableRoot, $"Discard_{player.seatIndex}", last, match.realOkeyTile, DiscardRect(player.seatIndex), false, null, -1, false, -1, true, player.seatIndex);
                    if (CanTakeFromSeat(match, local, player.seatIndex))
                    {
                        Frame(discard.transform, "TakeFrame", new Color(1f, 0.08f, 0.04f, 1f), 0.05f);
                        TextBlock(discard.transform, "TakeHint", "TAKE", 12, new Color(1f, 0.05f, 0.02f, 1f), TextAnchor.UpperCenter, new Vector2(0f, 0.76f), new Vector2(1f, 1.05f));
                    }
                }
            }

            if (local != null)
            {
                var canDrop = match.CurrentPlayer == local && match.turnPhase == TurnPhase.WaitingDiscard;
                var dropColor = canDrop ? new Color(0.85f, 0.14f, 0.08f, 0.32f) : new Color(0.9f, 0.62f, 0.20f, 0.14f);
                var dropRect = DiscardRect(local.seatIndex);
                var drop = DynPanel(tableRoot, "LocalDiscardDrop", dropRect.min, dropRect.max, dropColor);
                Frame(drop, "DropFrame", canDrop ? new Color(1f, 0.08f, 0.04f, 1f) : new Color(0.95f, 0.72f, 0.28f, 0.55f), 0.035f);
                TextBlock(drop, "DropText", canDrop ? "DROP TILE HERE" : "DISCARD SLOT", 16, canDrop ? Color.white : Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
                var zone = drop.gameObject.AddComponent<OkeyDropZone>();
                zone.Init(this, OkeyDropKind.Discard);
            }
        }

        private void RenderCenter(OkeyMatch match, OkeyPlayer local)
        {
            centerText.text = "";
            var centerPanel = DynPanel(tableRoot, "CenterOkeyPanel", new Vector2(0.43f, 0.42f), new Vector2(0.57f, 0.82f), new Color(0.015f, 0.08f, 0.045f, 0.62f));
            Frame(centerPanel, "CenterPanelFrame", new Color(0.95f, 0.72f, 0.28f, 0.48f), 0.018f);
            TextBlock(centerPanel, "IndicatorLabel", "INDICATOR", 12, Cream, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.98f));
            TileView(centerPanel, "IndicatorTile", match.indicatorTile, match.realOkeyTile, new Rect(0.30f, 0.36f, 0.40f, 0.42f), false, null);
            TextBlock(centerPanel, "IndicatorHint", "okey = next tile", 10, new Color(1f, 0.92f, 0.74f, 0.55f), TextAnchor.MiddleCenter, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.32f));

            var canDraw = CanDraw(match, local);
            RenderStockPile(match, canDraw);
        }

        private void RenderStockPile(OkeyMatch match, bool canDraw)
        {
            var stock = DynPanel(tableRoot, "StockPile", new Vector2(0.455f, 0.18f), new Vector2(0.545f, 0.37f), new Color(0f, 0f, 0f, 0f));
            var back = SpecialSprite("Back");
            for (var i = 0; i < 4; i++)
            {
                var offset = i * 0.035f;
                var card = DynPanel(stock, $"StockBack_{i}", new Vector2(0.18f + offset, 0.05f + offset), new Vector2(0.78f + offset, 0.86f + offset), Color.white);
                var image = card.GetComponent<Image>();
                if (back != null)
                {
                    image.sprite = back;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = true;
                    image.color = Color.white;
                }
                else
                {
                    image.color = new Color(0.96f, 0.86f, 0.66f, 1f);
                }
            }

            Frame(stock, "StockFrame", canDraw ? new Color(1f, 0.05f, 0.02f, 1f) : new Color(0.95f, 0.72f, 0.28f, 0.55f), canDraw ? 0.045f : 0.025f);
            TextBlock(stock, "StockCount", $"{match.stockPile.Count}", 20, canDraw ? Color.white : Gold, TextAnchor.LowerCenter, new Vector2(0.20f, -0.08f), new Vector2(0.82f, 0.18f));
            TextBlock(stock, "StockHint", canDraw ? "DRAW" : "STOCK", 12, canDraw ? Color.white : Gold, TextAnchor.UpperCenter, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 1.04f));
            var stockDrag = stock.gameObject.AddComponent<OkeyStockDrag>();
            stockDrag.Init(this);
        }

        private void RenderControls(OkeyMatch match, OkeyPlayer local)
        {
            AddCommand("WIN", new Rect(0.04f, 0.50f, 0.29f, 0.95f), CanFinish(match, local), () =>
            {
                NoteUiEvent("win clicked");
                if (selectedTileId >= 0) game.DeclareWin(selectedTileId);
                else game.DeclareWin();
            });
            AddCommand("CHAT", new Rect(0.37f, 0.50f, 0.62f, 0.95f), true, () => NoteUiEvent("chat tapped"));
            AddCommand("MENU", new Rect(0.70f, 0.50f, 0.95f, 0.95f), true, () => NoteUiEvent("menu tapped"));
            AddCommand("SORT", new Rect(0.04f, 0.02f, 0.29f, 0.42f), true, () => { NoteUiEvent("sort clicked"); ResetRackLayout(); game.SortHand(); });
            AddCommand("PAIR", new Rect(0.37f, 0.02f, 0.62f, 0.42f), true, () => { NoteUiEvent("pairs clicked"); ResetRackLayout(); game.SortPairs(); });
            AddCommand("MELD", new Rect(0.70f, 0.02f, 0.95f, 0.42f), true, () => { NoteUiEvent("melds clicked"); ResetRackLayout(); game.SortMelds(); });
        }

        private void RenderHand(OkeyMatch match, OkeyPlayer local)
        {
            if (local == null) return;
            SyncRackSlots(local.hand);
            var byId = local.hand.ToDictionary(t => t.id);
            RenderRackRow(rackBottomRoot, false, byId, match);
            RenderRackRow(rackTopRoot, true, byId, match);
        }

        private void RenderRackRow(RectTransform rowRoot, bool top, Dictionary<int, OkeyTile> byId, OkeyMatch match)
        {
            var width = 1f / RackSlotsPerRow;
            for (var slot = 0; slot < RackSlotsPerRow; slot++)
            {
                var x = slot * width;
                var slotRect = DynPanel(rowRoot, $"Slot_{(rowRoot == rackTopRoot ? "T" : "B")}_{slot}", new Vector2(x + 0.0005f, 0.01f), new Vector2(x + width - 0.0005f, 0.99f), new Color(0.05f, 0.025f, 0.01f, 0.20f));
                Frame(slotRect, "SlotFrame", new Color(1f, 0.78f, 0.22f, 0.08f), 0.012f);
            }

            var offset = top ? RackSlotsPerRow : 0;
            for (var i = 0; i < RackSlotsPerRow; i++)
            {
                var tileId = rackSlots[offset + i];
                if (tileId < 0 || !byId.TryGetValue(tileId, out var tile)) continue;
                var rect = new Rect(i * width - 0.004f, 0.005f, width + 0.008f, 0.99f);
                TileView(rowRoot, $"Hand_{tile.id}", tile, match.realOkeyTile, rect, tile.id == selectedTileId, () =>
                {
                    NoteUiEvent($"tile click {tile.id}");
                    selectedTileId = selectedTileId == tile.id ? -1 : tile.id;
                    Render(game.Match);
                }, tile.id, top, i, true, -1, tile.isRealOkey);
            }
        }

        private void AddCommand(string label, Rect rect, bool enabled, UnityEngine.Events.UnityAction action)
        {
            var button = Button(controlsRoot, label, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMax), enabled);
            button.gameObject.AddComponent<OkeyTap>().Init(this, label.ToLowerInvariant(), enabled, action);
        }

        private Button TileView(RectTransform parent, string name, OkeyTile tile, OkeyTile realOkey, Rect rect, bool selected, UnityEngine.Events.UnityAction action, int tileId = -1, bool topRow = false, int rowIndex = -1, bool draggable = false, int discardSeat = -1, bool faceDown = false)
        {
            var button = Button(parent, name, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMax), true);
            var img = button.GetComponent<Image>();
            var label = button.transform.Find("Label");
            if (label != null) label.gameObject.SetActive(false);

            var sprite = faceDown ? SpecialSprite("Back") : TileSprite(tile);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = selected ? new Color(1f, 0.93f, 0.62f, 1f) : Color.white;
                if (!faceDown && (tile.isRealOkey || tile.isIndicator))
                    TextBlock(button.transform, "TileMark", tile.isRealOkey ? "★" : "!", 18, Gold, TextAnchor.UpperRight, new Vector2(0.62f, 0.64f), new Vector2(0.98f, 0.98f));
            }
            else
            {
                img.color = selected ? new Color(1f, 0.88f, 0.46f, 1f) : new Color(0.985f, 0.965f, 0.90f, 1f);
                TextBlock(button.transform, "TileText", TileLabel(tile, realOkey), 25, TileColor(tile), TextAnchor.MiddleCenter, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.86f));
                var foot = Panel(button.transform, "TileFoot", new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.09f), TileColor(tile));
                dynamicObjects.Add(foot.gameObject);
            }

            if (selected) Frame(button.transform, "SelectedTileFrame", Gold, 0.045f);
            if (draggable && tileId >= 0)
            {
                var drag = button.gameObject.AddComponent<OkeyTileDrag>();
                drag.Init(this, tileId, topRow, rowIndex);
            }
            else if (discardSeat >= 0)
            {
                var drag = button.gameObject.AddComponent<OkeyDiscardDrag>();
                drag.Init(this, discardSeat);
            }
            if (action != null) button.gameObject.AddComponent<OkeyTap>().Init(this, $"tile {tileId}", true, action);
            return button;
        }

        public void MoveTileByDrag(int tileId, bool fromTopRow, Vector2 screenPosition)
        {
            if (game == null || game.Match == null)
            {
                NoteUiEvent("drag ignored no match");
                return;
            }
            var local = game.Match.players.FirstOrDefault(p => !p.isBot);
            if (local == null || local.hand.All(t => t.id != tileId))
            {
                NoteUiEvent($"drag ignored missing {tileId}");
                return;
            }

            var toTop = IsScreenPointInside(rackTopRoot, screenPosition);
            var toBottom = IsScreenPointInside(rackBottomRoot, screenPosition);
            if (IsLocalDiscardPoint(screenPosition))
            {
                selectedTileId = tileId;
                NoteUiEvent($"drag discard {tileId}");
                game.Discard(tileId);
                return;
            }
            if (!toTop && !toBottom)
            {
                NoteUiEvent($"drag outside rack {tileId}");
                return;
            }

            var targetRoot = toTop ? rackTopRoot : rackBottomRoot;
            var rowSlot = IndexFromPoint(targetRoot, screenPosition, RackSlotsPerRow);
            var targetSlot = (toTop ? RackSlotsPerRow : 0) + rowSlot;
            MoveTileGroupToSlot(tileId, targetSlot);
            selectedTileId = -1;
            NoteUiEvent($"{(activeDragIsGroup ? "drag group" : "drag tile")} {activeDragGroup.Count} {(toTop ? "top" : "bottom")}:{rowSlot + 1}");
            Render(game.Match);
        }

        public void NoteUiEvent(string value)
        {
            lastUiEvent = value;
            if (game != null && game.Match != null) RenderStatus(game.Match, game.Match.players.FirstOrDefault(p => !p.isBot));
        }

        public void BeginTileDrag(int tileId, Vector2 screenPosition)
        {
            if (game == null || game.Match == null || root == null) return;
            selectedTileId = -1;
            ClearSelectionFrames();
            EndTileDrag();
            activeDragIsGroup = false;
            activeDragGroup = new List<int> { tileId };
            BuildDragGhost(activeDragGroup, screenPosition);
            NoteUiEvent($"drag tile {tileId}");
        }

        public bool ActivateTileGroupDrag(int tileId, Vector2 screenPosition)
        {
            if (game == null || game.Match == null || root == null) return false;
            var group = ContiguousRackGroup(tileId);
            if (group.Count <= 1) return false;

            activeDragIsGroup = true;
            activeDragGroup = group;
            BuildDragGhost(activeDragGroup, screenPosition);
            NoteUiEvent($"hold group {group.Count}");
            return true;
        }

        private void ClearSelectionFrames()
        {
            if (root == null) return;
            var frames = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t != null && t.name == "SelectedTileFrame")
                .Select(t => t.gameObject)
                .ToList();
            foreach (var frame in frames)
            {
                dynamicObjects.Remove(frame);
                Destroy(frame);
            }
        }

        private void BuildDragGhost(List<int> tileIds, Vector2 screenPosition)
        {
            EndTileDrag();
            var hand = game.Match.players.FirstOrDefault(p => !p.isBot)?.hand;
            if (hand == null) return;
            var groupTiles = tileIds.Select(id => hand.FirstOrDefault(t => t.id == id)).Where(t => t != null).ToList();
            if (groupTiles.Count == 0) return;

            var ghostWidth = Mathf.Clamp(0.08f * groupTiles.Count, 0.08f, 0.42f);
            dragGhost = DynPanel(root, "DragTileGhost", new Vector2(0.5f - ghostWidth * 0.5f, 0.46f), new Vector2(0.5f + ghostWidth * 0.5f, 0.62f), new Color(1f, 0.97f, 0.90f, 0f));
            dragGhost.SetAsLastSibling();

            var width = 1f / groupTiles.Count;
            for (var i = 0; i < groupTiles.Count; i++)
            {
                var tile = groupTiles[i];
                var ghostTile = DynPanel(dragGhost, $"DragGhostTile_{i}", new Vector2(i * width + 0.01f, 0f), new Vector2((i + 1) * width - 0.01f, 1f), Color.white);
                var ghostImage = ghostTile.GetComponent<Image>();
                var sprite = tile.isRealOkey ? SpecialSprite("Back") : TileSprite(tile);
                if (sprite != null)
                {
                    ghostImage.sprite = sprite;
                    ghostImage.type = Image.Type.Simple;
                    ghostImage.preserveAspect = true;
                    ghostImage.color = Color.white;
                }
                else
                {
                    TextBlock(ghostTile, "DragGhostText", TileLabel(tile, game.Match.realOkeyTile), 30, TileColor(tile), TextAnchor.MiddleCenter, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.88f));
                    Panel(ghostTile, "DragGhostFoot", new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.09f), TileColor(tile));
                }
            }
            UpdateTileDrag(screenPosition);
        }

        public void UpdateTileDrag(Vector2 screenPosition)
        {
            if (dragGhost != null) dragGhost.position = screenPosition;
        }

        public void EndTileDrag()
        {
            if (dragGhost == null) return;
            dynamicObjects.Remove(dragGhost.gameObject);
            Destroy(dragGhost.gameObject);
            dragGhost = null;
        }

        public void DrawStockByDrag()
        {
            game.DrawStock();
        }

        public void TakeDiscardByDrag(int fromSeat)
        {
            game.TakeDiscard();
        }

        public void DropSelectedToDiscard()
        {
            if (selectedTileId >= 0) game.Discard(selectedTileId);
        }

        public void DropTileToDiscard(int tileId)
        {
            selectedTileId = tileId;
            game.Discard(tileId);
        }

        private void ResetRackLayout()
        {
            for (var i = 0; i < rackSlots.Length; i++) rackSlots[i] = -1;
            topRowTiles.Clear();
            manualOrder.Clear();
        }

        private void SyncRackSlots(List<OkeyTile> hand)
        {
            var ids = new HashSet<int>(hand.Select(t => t.id));
            for (var i = 0; i < rackSlots.Length; i++)
                if (rackSlots[i] >= 0 && !ids.Contains(rackSlots[i])) rackSlots[i] = -1;

            foreach (var tile in hand)
            {
                if (rackSlots.Contains(tile.id)) continue;
                var free = FirstFreeRackSlot();
                if (free >= 0) rackSlots[free] = tile.id;
            }
        }

        private int FirstFreeRackSlot()
        {
            for (var i = 0; i < rackSlots.Length; i++)
                if (rackSlots[i] < 0) return i;
            return -1;
        }

        private void MoveTileGroupToSlot(int tileId, int targetSlot)
        {
            if (targetSlot < 0 || targetSlot >= rackSlots.Length) return;
            var group = activeDragIsGroup && activeDragGroup.Count > 0 && activeDragGroup.Contains(tileId) ? activeDragGroup.ToList() : new List<int> { tileId };
            if (group.Count == 0) group.Add(tileId);
            var sourceSlots = group.Select(id => Array.IndexOf(rackSlots, id)).ToArray();
            if (sourceSlots.Any(slot => slot < 0)) return;

            var rowStart = targetSlot < RackSlotsPerRow ? 0 : RackSlotsPerRow;
            var rowEnd = rowStart + RackSlotsPerRow - 1;
            if (targetSlot + group.Count - 1 > rowEnd) targetSlot = rowEnd - group.Count + 1;
            if (targetSlot < rowStart) return;

            var snapshot = rackSlots.ToArray();
            foreach (var slot in sourceSlots) rackSlots[slot] = -1;

            if (RangeIsEmpty(targetSlot, targetSlot + group.Count - 1))
            {
                for (var i = 0; i < group.Count; i++) rackSlots[targetSlot + i] = group[i];
                return;
            }

            var freeStart = targetSlot;
            while (freeStart <= rowEnd && rackSlots[freeStart] >= 0) freeStart++;
            if (freeStart > rowEnd)
            {
                Array.Copy(snapshot, rackSlots, rackSlots.Length);
                NoteUiEvent("no room for group");
                return;
            }

            var shift = group.Count;
            if (!RangeIsEmpty(freeStart, freeStart + shift - 1))
            {
                Array.Copy(snapshot, rackSlots, rackSlots.Length);
                NoteUiEvent("no room for group");
                return;
            }

            for (var i = freeStart - 1; i >= targetSlot; i--) rackSlots[i + shift] = rackSlots[i];
            for (var i = 0; i < group.Count; i++) rackSlots[targetSlot + i] = group[i];
        }

        private bool RangeIsEmpty(int start, int end)
        {
            if (start < 0 || end >= rackSlots.Length) return false;
            var rowStart = start < RackSlotsPerRow ? 0 : RackSlotsPerRow;
            var rowEnd = rowStart + RackSlotsPerRow - 1;
            if (end > rowEnd) return false;
            for (var i = start; i <= end; i++)
                if (rackSlots[i] >= 0) return false;
            return true;
        }

        private List<int> ContiguousRackGroup(int tileId)
        {
            var slot = Array.IndexOf(rackSlots, tileId);
            if (slot < 0) return new List<int>();
            var rowStart = slot < RackSlotsPerRow ? 0 : RackSlotsPerRow;
            var rowEnd = rowStart + RackSlotsPerRow - 1;
            var left = slot;
            var right = slot;

            while (left > rowStart && rackSlots[left - 1] >= 0) left--;
            while (right < rowEnd && rackSlots[right + 1] >= 0) right++;

            var result = new List<int>();
            for (var i = left; i <= right; i++) result.Add(rackSlots[i]);
            return result;
        }

        private List<int> OrderedIds(OkeyPlayer local, bool top)
        {
            var ids = new HashSet<int>(local.hand.Select(t => t.id));
            return manualOrder.Where(ids.Contains).Where(id => topRowTiles.Contains(id) == top).ToList();
        }

        private void ReorderWithinRow(int tileId, List<int> targetIds, int insertIndex)
        {
            manualOrder.Remove(tileId);
            targetIds.Remove(tileId);
            insertIndex = Mathf.Clamp(insertIndex, 0, targetIds.Count);
            var anchor = insertIndex < targetIds.Count ? targetIds[insertIndex] : -1;
            if (anchor >= 0)
            {
                var globalIndex = manualOrder.IndexOf(anchor);
                manualOrder.Insert(Mathf.Max(0, globalIndex), tileId);
            }
            else
            {
                manualOrder.Add(tileId);
            }
        }

        private void SyncManualOrder(List<OkeyTile> hand)
        {
            var ids = new HashSet<int>(hand.Select(t => t.id));
            manualOrder.RemoveAll(id => !ids.Contains(id));
            topRowTiles.RemoveWhere(id => !ids.Contains(id));
            foreach (var tile in hand)
                if (!manualOrder.Contains(tile.id)) manualOrder.Add(tile.id);
        }

        private void ClampRackRows(IEnumerable<OkeyTile> hand)
        {
            var ids = new HashSet<int>(hand.Select(t => t.id));
            var orderedIds = manualOrder.Where(ids.Contains).ToList();
            var bottom = orderedIds.Where(id => !topRowTiles.Contains(id)).ToList();
            var top = orderedIds.Where(id => topRowTiles.Contains(id)).ToList();

            foreach (var id in bottom.Skip(RackSlotsPerRow)) topRowTiles.Add(id);
            foreach (var id in top.Skip(RackSlotsPerRow)) topRowTiles.Remove(id);
        }

        private static bool IsScreenPointInside(RectTransform rect, Vector2 screenPosition)
        {
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition);
        }

        private bool IsLocalDiscardPoint(Vector2 screenPosition)
        {
            var localDrop = tableRoot != null ? tableRoot.Find("LocalDiscardDrop") as RectTransform : null;
            return localDrop != null && IsScreenPointInside(localDrop, screenPosition);
        }

        private static int IndexFromPoint(RectTransform rect, Vector2 screenPosition, int count)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, null, out var local);
            var normalized = Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, local.x);
            return Mathf.Clamp(Mathf.RoundToInt(normalized * count), 0, count);
        }

        private Button Button(Transform parent, string name, Vector2 min, Vector2 max, bool enabled)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            dynamicObjects.Add(go);
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = enabled ? new Color(0.90f, 0.64f, 0.22f, 1f) : new Color(0.22f, 0.18f, 0.14f, 0.85f);
            var button = go.GetComponent<Button>();
            button.interactable = enabled;
            TextBlock(go.transform, "Label", name, labelSize(name), enabled ? new Color(0.08f, 0.04f, 0.02f, 1f) : new Color(0.55f, 0.48f, 0.38f, 1f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            return button;
        }

        private void Frame(Transform parent, string name, Color color, float thickness)
        {
            var rootFrame = new GameObject(name, typeof(RectTransform));
            dynamicObjects.Add(rootFrame);
            rootFrame.transform.SetParent(parent, false);
            var rect = rootFrame.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            FrameSide(rootFrame.transform, $"{name}_Top", color, new Vector2(0f, 1f - thickness), Vector2.one);
            FrameSide(rootFrame.transform, $"{name}_Bottom", color, Vector2.zero, new Vector2(1f, thickness));
            FrameSide(rootFrame.transform, $"{name}_Left", color, Vector2.zero, new Vector2(thickness, 1f));
            FrameSide(rootFrame.transform, $"{name}_Right", color, new Vector2(1f - thickness, 0f), Vector2.one);
        }

        private void FrameSide(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static int labelSize(string name) => name.Length > 6 ? 18 : 20;

        private static RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private RectTransform DynPanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            dynamicObjects.Add(go);
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text TextBlock(Transform parent, string name, string value, int size, Color color, TextAnchor anchor, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(4f, 3f);
            rect.offsetMax = new Vector2(-4f, -3f);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void ClearDynamic()
        {
            foreach (var go in dynamicObjects)
                if (go != null) Destroy(go);
            dynamicObjects.Clear();
        }

        private static Rect SeatRect(int seat)
        {
            return seat switch
            {
                0 => new Rect(0.36f, 0.03f, 0.28f, 0.12f),
                1 => new Rect(0.03f, 0.39f, 0.18f, 0.20f),
                2 => new Rect(0.36f, 0.84f, 0.28f, 0.13f),
                _ => new Rect(0.79f, 0.39f, 0.18f, 0.20f)
            };
        }

        private static Rect DiscardRect(int seat)
        {
            return seat switch
            {
                0 => new Rect(0.68f, 0.12f, 0.07f, 0.16f),
                1 => new Rect(0.25f, 0.43f, 0.07f, 0.16f),
                2 => new Rect(0.46f, 0.66f, 0.07f, 0.16f),
                _ => new Rect(0.68f, 0.43f, 0.07f, 0.16f)
            };
        }

        private static bool CanDraw(OkeyMatch match, OkeyPlayer local) => local != null && match.CurrentPlayer == local && match.turnPhase == TurnPhase.WaitingDraw && match.stockPile.Count > 0;
        private static bool CanTake(OkeyMatch match, OkeyPlayer local) => local != null && match.CurrentPlayer == local && match.turnPhase == TurnPhase.WaitingDraw && PreviousDiscard(match) != null;
        private static bool CanTakeFromSeat(OkeyMatch match, OkeyPlayer local, int seat)
        {
            if (!CanTake(match, local)) return false;
            var prevSeat = (match.currentTurnSeat - (int)match.direction + match.players.Count) % match.players.Count;
            return seat == prevSeat;
        }
        private bool CanDiscard(OkeyMatch match, OkeyPlayer local) => local != null && match.CurrentPlayer == local && match.turnPhase == TurnPhase.WaitingDiscard && selectedTileId >= 0;
        private bool CanFinish(OkeyMatch match, OkeyPlayer local) => local != null && match.CurrentPlayer == local && match.turnPhase == TurnPhase.WaitingDiscard;
        private static bool CanCifte(OkeyMatch match, OkeyPlayer local) => local != null && match.CurrentPlayer == local && !local.cifteGit && match.roundState == OkeyMatchState.Playing;

        private static OkeyTile PreviousDiscard(OkeyMatch match)
        {
            var prevSeat = (match.currentTurnSeat - (int)match.direction + match.players.Count) % match.players.Count;
            var prev = match.players.FirstOrDefault(p => p.seatIndex == prevSeat);
            return prev != null && prev.discardPile.Count > 0 ? prev.discardPile[prev.discardPile.Count - 1] : null;
        }

        private static string TileLabel(OkeyTile tile, OkeyTile realOkey)
        {
            if (tile == null) return "-";
            if (tile.type == OkeyTileType.FakeJoker) return "★";
            var mark = tile.isRealOkey ? "★" : tile.isIndicator ? "!" : "";
            return $"{tile.number}{mark}";
        }

        private Sprite TileSprite(OkeyTile tile)
        {
            if (tile == null) return null;
            if (tile.type == OkeyTileType.FakeJoker) return SpecialSprite("SahteOK");
            if (tile.type != OkeyTileType.Number || tile.number < 1 || tile.number > 13) return null;
            var color = tile.color switch
            {
                OkeyColor.Red => "Red",
                OkeyColor.Yellow => "Yellow",
                OkeyColor.Blue => "Blue",
                OkeyColor.Black => "Black",
                _ => null
            };
            if (string.IsNullOrEmpty(color)) return null;

            var key = $"{color}/{tile.number}";
            if (tileSprites.TryGetValue(key, out var cached)) return cached;

            var texture = Resources.Load<Texture2D>($"Okey/Tiles/{key}");
            if (texture == null)
            {
                tileSprites[key] = null;
                return null;
            }

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            tileSprites[key] = sprite;
            return sprite;
        }

        private Sprite SpecialSprite(string name)
        {
            var key = $"Special/{name}";
            if (tileSprites.TryGetValue(key, out var cached)) return cached;

            var texture = Resources.Load<Texture2D>($"Okey/Tiles/{name}");
            if (texture == null)
            {
                tileSprites[key] = null;
                return null;
            }

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            tileSprites[key] = sprite;
            return sprite;
        }

        private static Color TileColor(OkeyTile tile)
        {
            if (tile == null) return Color.black;
            if (tile.type == OkeyTileType.FakeJoker) return new Color(0.26f, 0.18f, 0.65f, 1f);
            return tile.color switch
            {
                OkeyColor.Red => new Color(0.82f, 0.05f, 0.04f, 1f),
                OkeyColor.Yellow => new Color(0.92f, 0.62f, 0.02f, 1f),
                OkeyColor.Blue => new Color(0.04f, 0.25f, 0.88f, 1f),
                OkeyColor.Black => new Color(0.02f, 0.02f, 0.02f, 1f),
                _ => Color.black
            };
        }
    }
}
