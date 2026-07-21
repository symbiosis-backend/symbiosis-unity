using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private ProgressBarUI progressBar;
        [SerializeField] private ScorePanelUI scorePanel;
        [SerializeField] private DiceUI diceUI;
        [SerializeField] private StatusUI statusUI;
        [SerializeField] private ResultUI resultUI;

        public Button DiceButton => diceUI.DiceButton;

        public void UpdateScore(IReadOnlyList<PlayerState> players, BoardGraph graph)
        {
            scorePanel.UpdateScore(players, graph);
        }

        public void UpdateProgress(IReadOnlyList<PlayerState> players, BoardGraph graph)
        {
            progressBar.UpdateProgress(players, graph);
        }

        public void ShowDiceResult(int dice1, int dice2)
        {
            diceUI.ShowDiceResult(dice1, dice2);
            statusUI.ShowStatus(dice1 <= 0 || dice2 <= 0 ? "Ожидание броска..." : $"Бросок: {dice1} + {dice2}");
        }

        public void SetDiceInteractable(bool interactable)
        {
            diceUI.SetInteractable(interactable);
        }

        public void SetTurnTimer(float normalized)
        {
            diceUI.SetTurnTimer(normalized);
        }

        public void ShowStatus(string message)
        {
            statusUI.ShowStatus(message);
        }

        public void ShowEvent(string message)
        {
            statusUI.ShowEvent(message);
        }

        public void ShowResult(string title, IReadOnlyList<PlayerState> players = null)
        {
            resultUI.ShowResult(title, players);
        }

        public static HUDController Create(Canvas canvas)
        {
            Transform existing = canvas.transform.Find("CorrosionHUD");
            if (existing != null && existing.TryGetComponent(out HUDController existingController))
            {
                return existingController;
            }

            RectTransform root = CreateRect("CorrosionHUD", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            HUDController controller = root.gameObject.AddComponent<HUDController>();

            controller.progressBar = CreateTopBar(root);
            controller.scorePanel = CreateRightPanel(root);
            controller.diceUI = CreateDiceHud(root);
            controller.statusUI = CreateStatusAndOverlay(root);
            controller.resultUI = CreateResultPanel(root);

            return controller;
        }

        public static Color PlayerColor(int playerId)
        {
            return playerId switch
            {
                1 => new Color(0.18f, 0.62f, 0.95f, 1f),
                2 => new Color(0.58f, 0.32f, 0.92f, 1f),
                3 => new Color(0.25f, 0.9f, 0.48f, 1f),
                _ => new Color(0.95f, 0.82f, 0.28f, 1f)
            };
        }

        private static ProgressBarUI CreateTopBar(RectTransform root)
        {
            RectTransform topBar = CreatePanel("TopBar", root, new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.985f), new Color(0.025f, 0.025f, 0.04f, 0.72f));
            CreateImage("Background", topBar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.025f, 0.025f, 0.04f, 0.72f));

            RectTransform track = CreateImage("ProgressTrack", topBar, new Vector2(0.04f, 0.38f), new Vector2(0.88f, 0.62f), Vector2.zero, Vector2.zero, new Color(0.62f, 0.46f, 0.14f, 0.92f));
            Outline trackGlow = track.gameObject.AddComponent<Outline>();
            trackGlow.effectColor = new Color(0.78f, 0.54f, 0.16f, 0.35f);
            trackGlow.effectDistance = new Vector2(0f, 2f);

            RectTransform markers = CreateRect("PlayerMarkers", topBar, new Vector2(0.04f, 0.2f), new Vector2(0.88f, 0.8f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI finish = CreateText("FinishLabel", topBar, new Vector2(0.895f, 0f), new Vector2(0.995f, 1f), 24, TextAlignmentOptions.Center);
            finish.text = "FINISH";
            finish.color = new Color(0.95f, 0.76f, 0.22f, 1f);

            ProgressBarUI progress = topBar.gameObject.AddComponent<ProgressBarUI>();
            progress.Initialize(track, markers, finish);
            return progress;
        }

        private static ScorePanelUI CreateRightPanel(RectTransform root)
        {
            RectTransform panel = CreatePanel("RightPanel", root, new Vector2(0.735f, 0.62f), new Vector2(0.985f, 0.895f), new Color(0.025f, 0.025f, 0.04f, 0.78f));
            RectTransform list = CreateRect("PlayerList", panel, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f));
            VerticalLayoutGroup layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            ScorePanelUI score = panel.gameObject.AddComponent<ScorePanelUI>();
            score.Initialize(list);
            return score;
        }

        private static DiceUI CreateDiceHud(RectTransform root)
        {
            RectTransform diceRoot = CreateRect("DiceButtonRoot", root, new Vector2(0.855f, 0.035f), new Vector2(0.975f, 0.25f), Vector2.zero, Vector2.zero);

            RectTransform ring = CreateImage("TurnTimerRing", diceRoot, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 1f), Vector2.zero, Vector2.zero, new Color(0.46f, 0.17f, 0.72f, 0.82f));
            Image ringImage = ring.GetComponent<Image>();
            ringImage.type = Image.Type.Filled;
            ringImage.fillMethod = Image.FillMethod.Radial360;
            ringImage.fillOrigin = 2;
            ringImage.fillClockwise = false;

            RectTransform buttonRect = CreateImage("DiceButton", diceRoot, new Vector2(0.18f, 0.34f), new Vector2(0.82f, 0.94f), Vector2.zero, Vector2.zero, new Color(0.92f, 0.72f, 0.16f, 0.96f));
            Button button = buttonRect.gameObject.AddComponent<Button>();
            Outline buttonGlow = buttonRect.gameObject.AddComponent<Outline>();
            buttonGlow.effectColor = new Color(0.95f, 0.76f, 0.22f, 0.45f);
            buttonGlow.effectDistance = new Vector2(3f, -3f);

            TextMeshProUGUI icon = CreateText("DiceIcon", buttonRect, Vector2.zero, Vector2.one, 34, TextAlignmentOptions.Center);
            icon.text = "⚂ ⚄";
            icon.color = new Color(0.025f, 0.025f, 0.04f, 1f);

            TextMeshProUGUI values = CreateText("DiceValues", diceRoot, new Vector2(0f, 0f), new Vector2(1f, 0.28f), 22, TextAlignmentOptions.Center);
            values.color = new Color(0.95f, 0.92f, 0.82f, 1f);

            DiceUI dice = diceRoot.gameObject.AddComponent<DiceUI>();
            dice.Initialize(button, values, ringImage);
            return dice;
        }

        private static StatusUI CreateStatusAndOverlay(RectTransform root)
        {
            RectTransform bottomBar = CreatePanel("BottomBar", root, new Vector2(0.26f, 0.03f), new Vector2(0.74f, 0.1f), new Color(0.025f, 0.06f, 0.035f, 0.76f));
            TextMeshProUGUI status = CreateText("StatusText", bottomBar, Vector2.zero, Vector2.one, 24, TextAlignmentOptions.Center);
            status.color = new Color(0.94f, 0.93f, 0.86f, 1f);

            RectTransform center = CreateRect("CenterOverlay", root, new Vector2(0.32f, 0.42f), new Vector2(0.68f, 0.58f), Vector2.zero, Vector2.zero);
            CanvasGroup group = center.gameObject.AddComponent<CanvasGroup>();
            CreateImage("EventBackground", center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.035f, 0.02f, 0.055f, 0.86f));
            TextMeshProUGUI eventText = CreateText("EventText", center, Vector2.zero, Vector2.one, 34, TextAlignmentOptions.Center);
            eventText.color = new Color(0.92f, 0.76f, 0.98f, 1f);

            StatusUI statusUI = bottomBar.gameObject.AddComponent<StatusUI>();
            statusUI.Initialize(status, eventText, group);
            return statusUI;
        }

        private static ResultUI CreateResultPanel(RectTransform root)
        {
            RectTransform panel = CreateRect("ResultPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateImage("Background", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.01f, 0.01f, 0.018f, 0.88f));
            RectTransform card = CreatePanel("ResultCard", panel, new Vector2(0.31f, 0.28f), new Vector2(0.69f, 0.72f), new Color(0.035f, 0.025f, 0.055f, 0.96f));
            TextMeshProUGUI winner = CreateText("WinnerText", card, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.94f), 34, TextAlignmentOptions.Center);
            winner.color = new Color(0.95f, 0.76f, 0.22f, 1f);
            TextMeshProUGUI summary = CreateText("ScoreSummary", card, new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.68f), 20, TextAlignmentOptions.Top);
            summary.color = new Color(0.9f, 0.88f, 0.82f, 1f);

            RectTransform restartRect = CreateImage("RestartButton", card, new Vector2(0.34f, 0.07f), new Vector2(0.66f, 0.2f), Vector2.zero, Vector2.zero, new Color(0.64f, 0.46f, 0.14f, 1f));
            Button restart = restartRect.gameObject.AddComponent<Button>();
            TextMeshProUGUI restartText = CreateText("Text", restartRect, Vector2.zero, Vector2.one, 20, TextAlignmentOptions.Center);
            restartText.text = "RESTART";
            restartText.color = new Color(0.025f, 0.025f, 0.04f, 1f);

            ResultUI result = panel.gameObject.AddComponent<ResultUI>();
            result.Initialize(panel.gameObject, winner, summary, restart);
            return result;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = CreateImage(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, color);
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.68f, 0.48f, 0.16f, 0.22f);
            outline.effectDistance = new Vector2(1f, -1f);
            return rect;
        }

        private static RectTransform CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int fontSize, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }
    }
}
