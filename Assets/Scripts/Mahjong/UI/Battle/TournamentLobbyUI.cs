using System;
using System.Collections;
using System.Globalization;
using MahjongGame.Tournaments;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class TournamentLobbyUI : MonoBehaviour
    {
        private const string RootName = "TournamentLobbyOverlay";
        private const string TournamentWindowResourcePath = "Mahjong/Sprites/BattleLobbyUI/TournamentWindow";
        private const string TournamentButtonResourcePath = "Mahjong/Sprites/BattleLobbyUI/Battlebutton";
        private const string TournamentInfoPanelResourcePath = "Mahjong/Sprites/BattleLobbyUI/InfoPanel";
        private const string TournamentDividerResourcePath = "Mahjong/Sprites/BattleLobbyUI/Divider";
        private const string PlayersIconResourcePath = "Mahjong/Sprites/BattleLobbyUI/PlayersIcon";
        private const string EnterIconResourcePath = "Mahjong/Sprites/BattleLobbyUI/enterIcon";
        private const string FondIconResourcePath = "Mahjong/Sprites/BattleLobbyUI/FondIcon";
        private const string TimerIconResourcePath = "Mahjong/Sprites/BattleLobbyUI/TimerIcon";
        private const string CupIconResourcePath = "Mahjong/Sprites/BattleLobbyUI/CupIcon";
        private const string OzTileIconResourcePath = "Mahjong/Sprites/BattleLobby/OzTileTopBar";
        private static readonly Vector4 TournamentWindowBorder = new Vector4(62f, 62f, 62f, 62f);
        private static readonly Vector4 TournamentButtonBorder = new Vector4(96f, 46f, 96f, 46f);
        private static readonly Vector4 TournamentInfoPanelBorder = new Vector4(48f, 48f, 48f, 48f);
        private static Sprite cachedTournamentWindowSprite;
        private static Sprite cachedTournamentButtonSprite;
        private static Sprite cachedTournamentInfoPanelSprite;
        private static Sprite cachedTournamentDividerSprite;

        private string battleGameSceneName = "GameMahjongBattle";
        private GameObject root;
        private RectTransform panelRect;
        private TMP_Text titleText;
        private TMP_Text statusText;
        private TMP_Text balanceText;
        private TMP_Text bodyText;
        private TMP_Text tabTitleText;
        private GameObject actionCard;
        private TMP_Text actionCardText;
        private TMP_Text playersChipText;
        private TMP_Text entryChipText;
        private TMP_Text poolChipText;
        private TMP_Text timerChipText;
        private Image playerProgressFill;
        private GameObject resultBanner;
        private TMP_Text resultBannerText;
        private GameObject rewardsView;
        private RectTransform rewardsContent;
        private GameObject bracketView;
        private RectTransform bracketContent;
        private Button joinButton;
        private Button leaveButton;
        private Button continueButton;
        private Button claimButton;
        private Button closeButton;
        private readonly Button[] tabButtons = new Button[4];
        private int selectedTab;
        private float nextTimerRenderAt;
        private float nextPollAt;

        public static TournamentLobbyUI Show(string battleSceneName)
        {
            if (!BattleTotemRequirementUI.EnsureBattleReady())
                return null;

            TournamentLobbyUI existing = FindAnyObjectByType<TournamentLobbyUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Configure(battleSceneName);
                existing.Open();
                return existing;
            }

            GameObject host = new GameObject("TournamentLobbyUI");
            TournamentLobbyUI ui = host.AddComponent<TournamentLobbyUI>();
            ui.Configure(battleSceneName);
            ui.Open();
            return ui;
        }

        public static void Ensure(string battleSceneName)
        {
            TournamentLobbyUI existing = FindAnyObjectByType<TournamentLobbyUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Configure(battleSceneName);
                return;
            }

            GameObject host = new GameObject("TournamentLobbyUI");
            host.AddComponent<TournamentLobbyUI>().Configure(battleSceneName);
        }

        private void Configure(string battleSceneName)
        {
            if (!string.IsNullOrWhiteSpace(battleSceneName))
                battleGameSceneName = battleSceneName;
        }

        private void Awake()
        {
            BuildUi();
        }

        private void OnEnable()
        {
            TournamentService service = TournamentService.EnsureInstance();
            service.ActiveChanged += HandleActiveChanged;
            service.ListChanged += HandleListChanged;
            service.BracketChanged += HandleBracketChanged;
            service.FundsChanged += HandleFundsChanged;
            service.ErrorChanged += HandleErrorChanged;
        }

        private void OnDisable()
        {
            if (TournamentService.I == null)
                return;

            TournamentService.I.ActiveChanged -= HandleActiveChanged;
            TournamentService.I.ListChanged -= HandleListChanged;
            TournamentService.I.BracketChanged -= HandleBracketChanged;
            TournamentService.I.FundsChanged -= HandleFundsChanged;
            TournamentService.I.ErrorChanged -= HandleErrorChanged;
        }

        private void OnDestroy()
        {
            BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Tournament);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf || Time.unscaledTime < nextTimerRenderAt)
                return;

            nextTimerRenderAt = Time.unscaledTime + 1f;
            Render();
            if (Time.unscaledTime >= nextPollAt)
            {
                nextPollAt = Time.unscaledTime + 7f;
                TournamentService service = TournamentService.I;
                if (service != null)
                {
                    service.RefreshActive();
                    if (selectedTab == 1)
                        service.RefreshBracket(CurrentTournamentId());
                    else if (selectedTab == 3)
                        service.RefreshFunds();
                }
            }
        }

        public void Open()
        {
            BuildUi();
            if (root == null)
                return;

            BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.Tournament);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            selectedTab = 0;
            nextPollAt = Time.unscaledTime + 2f;
            SetStatusText(Text("Загрузка турниров...", "Loading tournaments...", "Turnuvalar yukleniyor...", "Turniere werden geladen..."));
            TournamentService.EnsureInstance().RefreshAll();
            Render();
        }

        private void Close()
        {
            BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.Tournament);
            if (root != null)
                root.SetActive(false);
        }

        private void BuildUi()
        {
            if (root != null)
                return;

            Canvas canvas = BattleLobbyUiCoordinator.GetOrCreatePopupCanvas();
            if (canvas == null)
                return;

            root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image backdrop = root.GetComponent<Image>();
            backdrop.color = new Color(0.015f, 0.022f, 0.025f, 0.94f);
            backdrop.raycastTarget = true;

            GameObject panel = CreatePanel(root.transform, "TournamentPanel", new Vector2(0.5f, 0.5f), new Vector2(2040f, 900f), Vector2.zero);
            panelRect = (RectTransform)panel.transform;
            ApplyMobileSafePanel(panelRect);
            Image panelImage = panel.GetComponent<Image>();
            ApplyTournamentWindowGraphic(panelImage);

            titleText = CreateText(panel.transform, "Title", Text("Турниры", "Tournaments", "Turnuvalar", "Turniere"), 48f, TextAlignmentOptions.Center, new Vector2(0f, 382f), new Vector2(620f, 64f));
            CreateIcon(panel.transform, "OzTileIcon", OzTileIconResourcePath, new Vector2(610f, 354f), new Vector2(58f, 58f));
            balanceText = CreateText(panel.transform, "Balance", "0", 32f, TextAlignmentOptions.Left, new Vector2(750f, 354f), new Vector2(190f, 54f));
            closeButton = CreateButton(panel.transform, "CloseButton", "X", new Vector2(910f, 376f), new Vector2(135f, 135f), Close);

            statusText = CreateText(panel.transform, "Status", "", 24f, TextAlignmentOptions.Right, new Vector2(510f, 296f), new Vector2(700f, 58f));
            statusText.gameObject.SetActive(false);
            CreateStatsStrip(panel.transform);
            CreateResultBanner(panel.transform);

            string[] tabs =
            {
                Text("Кубки", "Cups", "Kupalar", "Pokale"),
                Text("Сетка", "Bracket", "Eşleşme", "Turnierbaum"),
                Text("Награды", "Rewards", "Ödüller", "Belohnungen"),
                Text("Фонд", "Grand Fund", "Buyuk Fon", "Grosser Fonds")
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                int tabIndex = i;
                Button tabButton = CreateButton(panel.transform, "TournamentTab" + i, tabs[i], new Vector2(-835f, 232f - i * 108f), new Vector2(360f, 94f), () =>
                {
                    selectedTab = tabIndex;
                    RequestTabData();
                    Render();
                });
                tabButtons[i] = tabButton;
            }

            tabTitleText = CreateText(panel.transform, "TabTitle", "", 32f, TextAlignmentOptions.Left, new Vector2(120f, 124f), new Vector2(1260f, 54f));
            CreateActionCard(panel.transform);
            bodyText = CreateText(panel.transform, "Body", "", 28f, TextAlignmentOptions.TopLeft, new Vector2(120f, -44f), new Vector2(1260f, 260f));
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            CreateRewardsView(panel.transform);
            CreateBracketView(panel.transform);

            joinButton = CreateButton(panel.transform, "TournamentJoinButton", Text("Вступить", "Join", "Katil", "Beitreten"), new Vector2(-310f, -254f), new Vector2(300f, 82f), () => TournamentService.EnsureInstance().Join(CurrentTournamentId()));
            leaveButton = CreateButton(panel.transform, "TournamentLeaveButton", Text("Выйти", "Leave", "Ayril", "Verlassen"), new Vector2(50f, -254f), new Vector2(300f, 82f), () => TournamentService.EnsureInstance().Leave(CurrentTournamentId()));
            continueButton = CreateButton(panel.transform, "TournamentContinueButton", Text("Бой", "Match", "Maç", "Match"), new Vector2(410f, -254f), new Vector2(320f, 82f), () => TournamentService.EnsureInstance().ContinueCurrentMatch(battleGameSceneName));
            claimButton = CreateButton(panel.transform, "TournamentClaimButton", Text("Забрать", "Claim", "Al", "Abholen"), new Vector2(790f, -254f), new Vector2(300f, 82f), ClaimFirstReward);

            root.SetActive(false);
        }

        private void CreateStatsStrip(Transform parent)
        {
            GameObject strip = new GameObject("StatsStrip", typeof(RectTransform));
            strip.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)strip.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -342f);
            rect.sizeDelta = new Vector2(1600f, 94f);

            playersChipText = CreateChip(strip.transform, "PlayersChip", 245f, PlayersIconResourcePath, new Vector2(-455f, 0f));
            entryChipText = CreateChip(strip.transform, "EntryChip", 220f, EnterIconResourcePath, new Vector2(-120f, 0f));
            poolChipText = CreateChip(strip.transform, "PoolChip", 240f, FondIconResourcePath, new Vector2(215f, 0f));
            timerChipText = CreateChip(strip.transform, "TimerChip", 390f, TimerIconResourcePath, new Vector2(590f, 0f));

            GameObject progress = new GameObject("PlayersProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            progress.transform.SetParent(parent, false);
            RectTransform progressRect = (RectTransform)progress.transform;
            progressRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressRect.pivot = new Vector2(0.5f, 0.5f);
            progressRect.anchoredPosition = new Vector2(120f, -396f);
            progressRect.sizeDelta = new Vector2(1180f, 8f);
            Image track = progress.GetComponent<Image>();
            track.color = Color.white;
            track.raycastTarget = false;
            ApplyTournamentDividerGraphic(track);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(progress.transform, false);
            RectTransform fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            playerProgressFill = fill.GetComponent<Image>();
            playerProgressFill.color = new Color(1f, 0.74f, 0.16f, 0.95f);
            playerProgressFill.raycastTarget = false;
        }

        private TMP_Text CreateChip(Transform parent, string name, float width, string iconResourcePath, Vector2 position)
        {
            GameObject chip = new GameObject(name, typeof(RectTransform));
            chip.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)chip.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 94f);

            CreateIcon(chip.transform, "Icon", iconResourcePath, new Vector2(-width * 0.5f + 44f, 0f), new Vector2(78f, 78f));

            TMP_Text text = CreateText(chip.transform, "Text", "-", 30f, TextAlignmentOptions.Left, new Vector2(56f, 0f), new Vector2(width - 112f, 72f));
            text.fontSizeMin = 18f;
            text.fontSizeMax = 30f;
            text.margin = new Vector4(2f, 0f, 4f, 0f);
            return text;
        }

        private void CreateIcon(Transform parent, string name, string iconResourcePath, Vector2 position, Vector2 size)
        {
            GameObject iconObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)iconObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = iconObject.GetComponent<Image>();
            image.sprite = LoadTournamentSprite(iconResourcePath, Vector4.zero);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private void CreateResultBanner(Transform parent)
        {
            resultBanner = new GameObject("ResultBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            resultBanner.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)resultBanner.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-858f, -342f);
            rect.sizeDelta = new Vector2(285f, 62f);
            Image image = resultBanner.GetComponent<Image>();
            image.color = new Color(0.030f, 0.090f, 0.145f, 0.96f);
            image.raycastTarget = false;
            ApplyTournamentButtonGraphic(image);
            resultBannerText = CreateText(resultBanner.transform, "Text", "", 21f, TextAlignmentOptions.Center, Vector2.zero, new Vector2(255f, 54f));
            resultBanner.SetActive(false);
        }

        private void CreateActionCard(Transform parent)
        {
            actionCard = new GameObject("ActionCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            actionCard.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)actionCard.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(120f, -44f);
            rect.sizeDelta = new Vector2(1260f, 258f);

            Image image = actionCard.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            ApplyTournamentInfoPanelGraphic(image);

            Outline outline = actionCard.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.52f, 0.08f, 0.18f);
            outline.effectDistance = new Vector2(1f, -1f);

            CreateIcon(actionCard.transform, "CupIcon", CupIconResourcePath, new Vector2(-500f, 0f), new Vector2(128f, 128f));
            actionCardText = CreateText(actionCard.transform, "Text", "", 36f, TextAlignmentOptions.Left, new Vector2(130f, 0f), new Vector2(920f, 210f));
            actionCardText.richText = true;
            actionCardText.fontSizeMin = 23f;
            actionCard.SetActive(false);
        }

        private void CreateRewardsView(Transform parent)
        {
            rewardsView = new GameObject("RewardsView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect));
            rewardsView.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)rewardsView.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(120f, -44f);
            rect.sizeDelta = new Vector2(1260f, 258f);
            Image image = rewardsView.GetComponent<Image>();
            image.color = new Color(0.006f, 0.014f, 0.024f, 0.64f);
            image.raycastTarget = true;
            BattlePopupStyle.ApplyFront(image);
            rewardsView.GetComponent<Mask>().showMaskGraphic = true;

            GameObject content = new GameObject("RewardsContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(rewardsView.transform, false);
            rewardsContent = (RectTransform)content.transform;
            rewardsContent.anchorMin = new Vector2(0f, 0.5f);
            rewardsContent.anchorMax = new Vector2(0f, 0.5f);
            rewardsContent.pivot = new Vector2(0f, 0.5f);
            rewardsContent.anchoredPosition = new Vector2(16f, 0f);
            rewardsContent.sizeDelta = new Vector2(1220f, 226f);

            HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            ScrollRect scroll = rewardsView.GetComponent<ScrollRect>();
            scroll.content = rewardsContent;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            rewardsView.SetActive(false);
        }

        private static void ApplyMobileSafePanel(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.035f, 0.055f);
            rect.anchorMax = new Vector2(0.965f, 0.955f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyTournamentWindowGraphic(Image image)
        {
            BattlePopupStyle.ApplyWindow(image);
        }

        private static void ApplyTournamentButtonGraphic(Image image)
        {
            ApplyTournamentGraphic(image, BattlePopupStyle.ButtonSprite, Color.white);
        }

        private static void ApplyTournamentInfoPanelGraphic(Image image)
        {
            BattlePopupStyle.ApplyFront(image);
        }

        private static void ApplyTournamentDividerGraphic(Image image)
        {
            ApplyTournamentGraphic(image, LoadTournamentDividerSprite(), Color.white);
        }

        private static void ApplyTournamentGraphic(Image image, Sprite sprite, Color color)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = sprite.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
        }

        private static Sprite LoadTournamentWindowSprite()
        {
            if (cachedTournamentWindowSprite != null)
                return cachedTournamentWindowSprite;

            cachedTournamentWindowSprite = LoadTournamentSprite(TournamentWindowResourcePath, TournamentWindowBorder);
            return cachedTournamentWindowSprite;
        }

        private static Sprite LoadTournamentButtonSprite()
        {
            if (cachedTournamentButtonSprite != null)
                return cachedTournamentButtonSprite;

            cachedTournamentButtonSprite = LoadTournamentSprite(TournamentButtonResourcePath, TournamentButtonBorder);
            return cachedTournamentButtonSprite;
        }

        private static Sprite LoadTournamentInfoPanelSprite()
        {
            if (cachedTournamentInfoPanelSprite != null)
                return cachedTournamentInfoPanelSprite;

            cachedTournamentInfoPanelSprite = LoadTournamentSprite(TournamentInfoPanelResourcePath, TournamentInfoPanelBorder);
            return cachedTournamentInfoPanelSprite;
        }

        private static Sprite LoadTournamentDividerSprite()
        {
            if (cachedTournamentDividerSprite != null)
                return cachedTournamentDividerSprite;

            cachedTournamentDividerSprite = LoadTournamentSprite(TournamentDividerResourcePath, Vector4.zero);
            return cachedTournamentDividerSprite;
        }

        private static Sprite LoadTournamentSprite(string resourcePath, Vector4 border)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                Rect rect = new Rect(0f, 0f, texture.width, texture.height);
                return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
                return sprites[0];

            return null;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 position)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return panel;
        }

        private TMP_Text CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(14f, size * 0.55f);
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            BattlePopupStyle.ApplyText(text);
            ApplyMainLobbyFont(text);
            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            BattlePopupStyle.ApplyButton(button);
            float fontSize = !string.IsNullOrWhiteSpace(name) && name.StartsWith("TournamentTab", StringComparison.Ordinal) ? 46f : 28f;
            CreateText(buttonObject.transform, "Label", label, fontSize, TextAlignmentOptions.Center, Vector2.zero, size);
            BattlePopupStyle.ApplyButtonLabel(button, fontSize);
            TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
            if (labelText != null)
            {
                ApplyMainLobbyFont(labelText);
                labelText.margin = new Vector4(16f, 0f, 16f, 0f);
            }
            if (!string.IsNullOrWhiteSpace(name) && name.Contains("Close"))
                BattlePopupStyle.ApplyCloseIconButton(button);
            return button;
        }

        private static void ApplyMainLobbyFont(TMP_Text text)
        {
            if (text == null)
                return;

            MainLobbyButtonStyle.ApplyFont(text);
        }

        private void CreateBracketView(Transform parent)
        {
            bracketView = new GameObject("BracketView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bracketView.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)bracketView.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(120f, -44f);
            rect.sizeDelta = new Vector2(1260f, 258f);

            Image image = bracketView.GetComponent<Image>();
            image.color = new Color(0.006f, 0.014f, 0.024f, 0.78f);
            image.raycastTarget = true;
            BattlePopupStyle.ApplyFront(image);

            GameObject content = new GameObject("BracketContent", typeof(RectTransform));
            content.transform.SetParent(bracketView.transform, false);
            bracketContent = (RectTransform)content.transform;
            bracketContent.anchorMin = Vector2.zero;
            bracketContent.anchorMax = Vector2.one;
            bracketContent.pivot = new Vector2(0.5f, 0.5f);
            bracketContent.offsetMin = new Vector2(26f, 20f);
            bracketContent.offsetMax = new Vector2(-26f, -20f);

            bracketView.SetActive(false);
        }

        private void HandleActiveChanged(TournamentActiveResponse response)
        {
            Render();
        }

        private void HandleListChanged(TournamentListResponse response)
        {
            Render();
        }

        private void HandleBracketChanged(TournamentBracketResponse response)
        {
            Render();
        }

        private void HandleFundsChanged(TournamentFundsResponse response)
        {
            Render();
        }

        private void HandleErrorChanged(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
                SetStatusText(error);
            Render();
        }

        private void Render()
        {
            if (root == null || !root.activeSelf)
                return;

            TournamentService service = TournamentService.EnsureInstance();
            TournamentActiveResponse active = service.LastActive;
            TournamentInfo tournament = DisplayTournament(active, service.LastList);
            TournamentFeatureFlags feature = active != null && active.feature != null ? active.feature : service.LastList != null ? service.LastList.feature : null;
            bool enabled = feature == null || feature.enabled;
            bool hasActive = active != null && active.active != null;
            bool hasMatch = active != null && active.currentMatch != null && !string.IsNullOrWhiteSpace(active.battleMatchId);
            bool hasReward = active != null && active.pendingRewards != null && active.pendingRewards.Length > 0;

            int displayOzTile = ResolveDisplayOzTileBalance(active);
            if (balanceText != null)
                balanceText.text = displayOzTile.ToString();
            UpdateStatsStrip(tournament);

            string status = !enabled
                ? Text("Турниры сейчас в разработке. Доступ открыт только тестовому аккаунту.", "Tournaments are in development. Access is limited to the test account.", "Turnuvalar şu anda geliştirme aşamasında. Erişim yalnızca test hesabı için açık.", "Turniere sind aktuell in Entwicklung. Zugriff ist nur fuer das Testkonto offen.")
                : BuildStatus(active, tournament);
            if (!hasActive && !hasMatch && !hasReward && !string.IsNullOrWhiteSpace(service.LastError))
                status = service.LastError;
            SetStatusText(status);

            string[] tabTitles =
            {
                Text("Bronze Small Cup", "Bronze Small Cup", "Bronze Small Cup", "Bronze Small Cup"),
                Text("Турнирная сетка", "Bracket", "Eşleşme", "Turnierbaum"),
                Text("Призы", "Rewards", "Ödüller", "Belohnungen"),
                Text("Большой фонд", "Grand Fund", "Buyuk Fon", "Grosser Fonds")
            };
            if (tabTitleText != null)
            {
                tabTitleText.gameObject.SetActive(selectedTab != 0);
                tabTitleText.text = tabTitles[Mathf.Clamp(selectedTab, 0, tabTitles.Length - 1)];
            }
            RefreshTabButtons();
            if (actionCard != null)
            {
                bool showActionCard = selectedTab == 0;
                actionCard.SetActive(showActionCard);
                if (showActionCard && actionCardText != null)
                    actionCardText.text = BuildActionCard(active, tournament);
            }
            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(selectedTab == 3);
                bodyText.text = BuildBody(active, tournament);
                bodyText.alignment = TextAlignmentOptions.Center;
                bodyText.fontSize = 30f;
                bodyText.fontSizeMin = 20f;
                bodyText.textWrappingMode = TextWrappingModes.Normal;
            }
            if (rewardsView != null)
            {
                rewardsView.SetActive(selectedTab == 2);
                if (selectedTab == 2)
                    RenderRewardsView(tournament);
            }
            if (bracketView != null)
            {
                bracketView.SetActive(selectedTab == 1);
                if (selectedTab == 1)
                    RenderBracketView(tournament);
            }
            UpdateResultBanner(active, tournament);

            bool canJoin = enabled && !hasActive && tournament != null && string.Equals(tournament.status, "RegistrationOpen", System.StringComparison.Ordinal) && displayOzTile >= tournament.entryFeeOzTile;
            string joinReason = JoinDisabledReason(enabled, hasActive, active, tournament);
            if (!canJoin && !hasActive && !string.IsNullOrWhiteSpace(joinReason))
                SetStatusText(ShortUiLine(status) + " | " + joinReason);
            SetButtonState(joinButton, canJoin, joinReason);
            SetButtonState(leaveButton, hasActive && tournament != null && string.Equals(tournament.status, "RegistrationOpen", System.StringComparison.Ordinal), Text("Взнос уже зафиксирован после набора 16 игроков", "Entry locks after 16 players join", "16 oyuncudan sonra giris kilitlenir", "Der Einsatz wird nach 16 Spielern gesperrt"));
            SetButtonState(continueButton, hasMatch, Text("Нет назначенного боя", "No assigned match", "Atanmis maç yok", "Kein zugewiesenes Match"));
            SetButtonState(claimButton, hasReward, Text("Нет награды", "No reward", "Ödül yok", "Keine Belohnung"));
        }

        private void UpdateStatsStrip(TournamentInfo tournament)
        {
            int registered = tournament != null ? Mathf.Max(0, tournament.registeredCount) : 0;
            int maxPlayers = tournament != null ? Mathf.Max(1, tournament.maxPlayers) : 16;
            int entry = tournament != null ? Mathf.Max(0, tournament.entryFeeOzTile) : 0;
            int pool = tournament != null ? Mathf.Max(0, tournament.totalPoolOzTile) : entry * maxPlayers;
            string timer = tournament != null ? TournamentTimingShort(tournament) : "-";

            if (playersChipText != null)
                playersChipText.text = registered + "/" + maxPlayers;
            if (entryChipText != null)
                entryChipText.text = entry.ToString();
            if (poolChipText != null)
                poolChipText.text = pool.ToString();
            if (timerChipText != null)
                timerChipText.text = timer;
            if (playerProgressFill != null)
                playerProgressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)registered / maxPlayers), 1f);
        }

        private string BuildStatus(TournamentActiveResponse active, TournamentInfo tournament)
        {
            if (active != null && active.currentMatch != null)
            {
                string opponent = active.opponent != null && !string.IsNullOrWhiteSpace(active.opponent.displayName)
                    ? active.opponent.displayName
                    : Text("соперник", "opponent", "rakip", "Gegner");
                int round = active.currentMatch != null ? Mathf.Max(1, active.currentMatch.roundIndex) : tournament != null ? Mathf.Max(1, tournament.currentRound) : 1;
                return string.Format(Text("ТУР {0}\n{1}\nЖМИ БОЙ", "ROUND {0}\n{1}\nPRESS MATCH", "TUR {0}\n{1}\nMAC'A BAS", "RUNDE {0}\n{1}\nMATCH DRUECKEN"), round, ShortName(opponent));
            }

            if (active != null && active.pendingRewards != null && active.pendingRewards.Length > 0)
                return Text("Доступна награда турнира.", "Tournament reward available.", "Turnuva ödülu hazır.", "Turnierbelohnung verfuegbar.");

            if (active != null && active.active != null)
                return BuildActiveStatus(active.active, active.participantStatus);

            if (tournament != null)
            {
                string timing = TournamentTimingLine(tournament);
                string line = string.Format(Text("{0}: {1}/{2} игроков | взнос {3} OzTile", "{0}: {1}/{2} players | entry {3} OzTile", "{0}: {1}/{2} oyuncu | giris {3} OzTile", "{0}: {1}/{2} Spieler | Einsatz {3} OzTile"),
                    StatusLabel(tournament.status), tournament.registeredCount, tournament.maxPlayers, tournament.entryFeeOzTile);
                return string.IsNullOrWhiteSpace(timing) ? line : line + "\n" + timing;
            }

            return Text("Открытый Bronze Cup пока не создан.", "No open Bronze Cup yet.", "Açık Bronze Cup yok.", "Noch kein offener Bronze Cup.");
        }

        private string BuildBody(TournamentActiveResponse active, TournamentInfo tournament)
        {
            if (selectedTab == 1)
                return BuildBracketBody(tournament);

            if (selectedTab == 2)
                return BuildRewardsBody(tournament);

            if (selectedTab == 3)
                return BuildGrandFundBody();

            if (active != null && !string.IsNullOrWhiteSpace(active.lockReason))
                return active.lockReason;

            return tournament == null
                ? Text("Нажми Join, когда турнир будет доступен.", "Join when a cup is available.", "Kupa acilinca Katil.", "Tritt bei, sobald ein Pokal verfuegbar ist.")
                : BuildCupBody(tournament);
        }

        private string BuildCupBody(TournamentInfo tournament)
        {
            int missing = Mathf.Max(0, tournament.maxPlayers - tournament.registeredCount);
            int progressPercent = tournament.maxPlayers > 0 ? Mathf.RoundToInt(100f * tournament.registeredCount / tournament.maxPlayers) : 0;
            return string.Format(Text(
                "Bronze Small Cup\nИгроки {0}/{1} ({2}%). Еще {3} до старта.\n{5}\nВзнос {4} OzTile. До 16 игроков - полный возврат.\nReal-time PvP. Single elimination. Ranked RP не меняется.",
                "Bronze Small Cup\nPlayers {0}/{1} ({2}%). {3} more to start.\n{5}\nEntry {4} OzTile. Full refund before 16 players.\nReal-time PvP. Single elimination. Ranked RP does not change.",
                "Bronze Small Cup\nOyuncu {0}/{1} ({2}%). Başlamak için {3} daha.\n{5}\nGiriş {4} OzTile. 16 oyuncudan önce tam iade.\nReal-time PvP. Tek eleme. Ranked RP değişmez.",
                "Bronze Small Cup\nSpieler {0}/{1} ({2}%). Noch {3} bis Start.\n{5}\nEinsatz {4} OzTile. Volle Rueckerstattung vor 16 Spielern.\nEchtzeit-PvP. K.-o.-System. Ranked RP bleibt gleich."),
                tournament.registeredCount, tournament.maxPlayers, progressPercent, missing, tournament.entryFeeOzTile, TournamentTimingLine(tournament));
        }

        private string BuildActionCard(TournamentActiveResponse active, TournamentInfo tournament)
        {
            if (active != null && active.currentMatch != null)
            {
                string opponent = active.opponent != null && !string.IsNullOrWhiteSpace(active.opponent.displayName)
                    ? active.opponent.displayName
                    : Text("соперник назначен", "opponent assigned", "rakip hazır", "Gegner bereit");
                int round = Mathf.Max(1, active.currentMatch.roundIndex);
                return string.Format(Text(
                    "<size=44><b>Тур {0}: бой готов</b></size>\n<size=34>Соперник: {1}</size>\n<size=30>Главное действие сейчас - нажать Бой.</size>",
                    "<size=44><b>Round {0}: match ready</b></size>\n<size=34>Opponent: {1}</size>\n<size=30>Main action now: press Match.</size>",
                    "<size=44><b>Tur {0}: maç hazır</b></size>\n<size=34>Rakip: {1}</size>\n<size=30>Şimdi ana eylem: Maç'a bas.</size>",
                    "<size=44><b>Runde {0}: Match bereit</b></size>\n<size=34>Gegner: {1}</size>\n<size=30>Jetzt wichtig: Match druecken.</size>"),
                    round, opponent);
            }

            if (active != null && active.pendingRewards != null && active.pendingRewards.Length > 0)
                return Text("<size=42><b>Награда готова</b></size>\n<size=30>Забери приз турнира.</size>", "<size=42><b>Reward ready</b></size>\n<size=30>Claim your tournament prize.</size>", "<size=42><b>Ödül hazır</b></size>\n<size=30>Turnuva ödülunu al.</size>", "<size=42><b>Belohnung bereit</b></size>\n<size=30>Hole deinen Turnierpreis ab.</size>");

            if (active != null && active.active != null)
                return Text("<size=42><b>Ты в турнире</b></size>\n<size=30>Ждем следующий раунд или назначение боя.</size>\n<size=27>Оверлей сам обновит состояние.</size>", "<size=42><b>You are in</b></size>\n<size=30>Waiting for the next round or match assignment.</size>\n<size=27>The overlay updates automatically.</size>", "<size=42><b>Turnuvadasın</b></size>\n<size=30>Sonraki tur veya maç bekleniyor.</size>\n<size=27>Ekran otomatik güncellenir.</size>", "<size=42><b>Du bist dabei</b></size>\n<size=30>Warte auf die naechste Runde oder ein Match.</size>\n<size=27>Das Fenster aktualisiert sich automatisch.</size>");

            if (tournament == null)
                return Text("<size=42><b>Bronze Small Cup</b></size>\n<size=30>Открытый кубок пока не создан.</size>", "<size=42><b>Bronze Small Cup</b></size>\n<size=30>No open cup yet.</size>", "<size=42><b>Bronze Small Cup</b></size>\n<size=30>Açık kupa yok.</size>", "<size=42><b>Bronze Small Cup</b></size>\n<size=30>Noch kein offener Pokal.</size>");

            int missing = Mathf.Max(0, tournament.maxPlayers - tournament.registeredCount);
            string timing = TournamentTimingLine(tournament);
            return string.Format(Text(
                "<size=42><b>Bronze Small Cup</b></size>\n<size=34>{0}/{1} игроков</size>   <size=28>Еще {2} до старта</size>\n<size=28>Вход {3} OzTile. До набора 16 - полный возврат.</size>\n<size=25>{4}</size>",
                "<size=42><b>Bronze Small Cup</b></size>\n<size=34>{0}/{1} players</size>   <size=28>{2} more to start</size>\n<size=28>Entry {3} OzTile. Full refund before 16 players.</size>\n<size=25>{4}</size>",
                "<size=42><b>Bronze Small Cup</b></size>\n<size=34>{0}/{1} oyuncu</size>   <size=28>Başlamak için {2}</size>\n<size=28>Giriş {3} OzTile. 16 oyuncudan önce tam iade.</size>\n<size=25>{4}</size>",
                "<size=42><b>Bronze Small Cup</b></size>\n<size=34>{0}/{1} Spieler</size>   <size=28>Noch {2} bis Start</size>\n<size=28>Einsatz {3} OzTile. Volle Rueckerstattung vor 16 Spielern.</size>\n<size=25>{4}</size>"),
                tournament.registeredCount,
                tournament.maxPlayers,
                missing,
                tournament.entryFeeOzTile,
                string.IsNullOrWhiteSpace(timing) ? Text("Real-time PvP. Ranked RP не меняется.", "Real-time PvP. Ranked RP does not change.", "Real-time PvP. Ranked RP değişmez.", "Echtzeit-PvP. Ranked RP bleibt gleich.") : timing);
        }

        private string BuildRewardsBody(TournamentInfo tournament)
        {
            if (tournament == null)
                return Text("Призы появятся после закрытия регистрации.", "Rewards appear after registration locks.", "Ödüller kayit kapaninca gorunur.", "Belohnungen erscheinen, sobald die Registrierung gesperrt ist.");

            return string.Format(Text(
                "Формула фиксируется при 16 игроках.\nКотел {0} | Grand Fund {1} | Призы {2}\n1 место: {3} OzTile\n2 место: {4} OzTile\n3-4 место: возврат {5} OzTile",
                "Formula locks at 16 players.\nPool {0} | Grand Fund {1} | Rewards {2}\n1st: {3} OzTile\n2nd: {4} OzTile\n3rd-4th: refund {5} OzTile",
                "Formul 16 oyuncuda kilitlenir.\nHavuz {0} | Buyuk Fon {1} | Ödül {2}\n1.: {3} OzTile\n2.: {4} OzTile\n3-4: iade {5} OzTile",
                "Formel wird bei 16 Spielern gesperrt.\nPool {0} | Grosser Fonds {1} | Preise {2}\n1. Platz: {3} OzTile\n2. Platz: {4} OzTile\n3.-4.: Rueckgabe {5} OzTile"),
                tournament.totalPoolOzTile, tournament.grandFundOzTile, tournament.rewardPoolOzTile, tournament.firstRewardOzTile, tournament.secondRewardOzTile, tournament.semifinalRefundOzTile);
        }

        private void RenderRewardsView(TournamentInfo tournament)
        {
            if (rewardsContent == null)
                return;

            for (int i = rewardsContent.childCount - 1; i >= 0; i--)
                Destroy(rewardsContent.GetChild(i).gameObject);

            if (tournament == null)
            {
                AddRewardCard(Text("Награды", "Rewards", "Ödüller", "Belohnungen"), Text("После набора 16", "After 16 players", "16 oyuncudan sonra", "Nach 16 Spielern"), "-", new Color(0.022f, 0.060f, 0.105f, 0.92f));
                return;
            }

            AddRewardCard(Text("1", "1", "1", "1"), Text("Победитель", "Winner", "Kazanan", "Sieger"), tournament.firstRewardOzTile.ToString(), new Color(0.074f, 0.104f, 0.035f, 0.96f));
            AddRewardCard(Text("2", "2", "2", "2"), Text("Финалист", "Finalist", "Finalist", "Finalist"), tournament.secondRewardOzTile.ToString(), new Color(0.050f, 0.085f, 0.125f, 0.96f));
            AddRewardCard(Text("3-4", "3-4", "3-4", "3-4"), Text("Возврат", "Refund", "Iade", "Rueckgabe"), tournament.semifinalRefundOzTile.ToString(), new Color(0.055f, 0.070f, 0.105f, 0.96f));
            AddRewardCard(Text("Фонд", "Fund", "Fon", "Fonds"), "10%", tournament.grandFundOzTile.ToString(), new Color(0.080f, 0.052f, 0.110f, 0.96f));
        }

        private void AddRewardCard(string title, string subtitle, string value, Color color)
        {
            GameObject card = new GameObject("RewardCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(rewardsContent, false);
            Image image = card.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            LayoutElement layout = card.GetComponent<LayoutElement>();
            layout.minWidth = 280f;
            layout.preferredWidth = 280f;
            layout.flexibleWidth = 0f;
            layout.preferredHeight = 214f;

            TMP_Text titleText = CreateText(card.transform, "Title", title, 46f, TextAlignmentOptions.Center, new Vector2(0f, 58f), new Vector2(240f, 58f));
            titleText.fontSizeMin = 28f;
            titleText.fontSizeMax = 46f;
            TMP_Text subtitleText = CreateText(card.transform, "Subtitle", subtitle, 24f, TextAlignmentOptions.Center, new Vector2(0f, 12f), new Vector2(240f, 40f));
            subtitleText.fontSizeMin = 16f;
            subtitleText.fontSizeMax = 24f;
            subtitleText.color = new Color(0.82f, 0.90f, 1f, 1f);
            CreateIcon(card.transform, "OzTileIcon", OzTileIconResourcePath, new Vector2(-42f, -58f), new Vector2(46f, 46f));
            TMP_Text valueText = CreateText(card.transform, "Value", value, 34f, TextAlignmentOptions.Left, new Vector2(50f, -58f), new Vector2(130f, 48f));
            valueText.fontSizeMin = 20f;
            valueText.fontSizeMax = 34f;
        }

        private string BuildGrandFundBody()
        {
            int bronzeFund = SumGrandFund("bronze");
            return string.Format(Text(
                "Bronze Grand Fund: {0} OzTile\n10% каждого Bronze Small Cup добавляется автоматически.\nЭто общий прогресс режима и база для больших турниров.",
                "Bronze Grand Fund: {0} OzTile\n10% of every Bronze Small Cup is added automatically.\nThis is the mode's shared progress and future big-cup base.",
                "Bronze Buyuk Fon: {0} OzTile\nHer Bronze Small Cup'in %10'u otomatik eklenir.\nBu modun ortak ilerlemesi ve buyuk kupa temelidir.",
                "Bronze Grosser Fonds: {0} OzTile\n10% jedes Bronze Small Cup werden automatisch addiert.\nDas ist der gemeinsame Fortschritt fuer grosse Turniere."),
                bronzeFund);
        }

        private string BuildBracketBody(TournamentInfo tournament)
        {
            TournamentBracketResponse bracket = TournamentService.I != null ? TournamentService.I.LastBracket : null;
            if (tournament == null)
                return Text("Сетка появится после создания турнира.", "Bracket appears after a cup is created.", "Kupa olusunca eslesme gorunur.", "Der Turnierbaum erscheint nach Erstellung des Pokals.");
            if (bracket == null || bracket.tournament == null || bracket.tournament.id != tournament.id)
                return Text("Загружаем сетку. До набора 16 игроков здесь будет подготовка первого раунда.", "Loading bracket. Before 16 players join, this tab shows first-round preparation.", "Eşleşme yükleniyor. 16 oyuncudan önce ilk tur hazırlığı görünür.", "Turnierbaum wird geladen. Vor 16 Spielern zeigt dieser Tab die Vorbereitung.");

            int round1 = CountMatches(bracket, 1);
            int round2 = CountMatches(bracket, 2);
            int round3 = CountMatches(bracket, 3);
            int round4 = CountMatches(bracket, 4);
            int completed = CountStatus(bracket, "Completed");
            int active = CountStatus(bracket, "Active") + CountStatus(bracket, "Running") + CountStatus(bracket, "Scheduled");
            return string.Format(Text(
                "Участники {0}/{1}\nРаунд 1: {2} | 1/4: {3} | 1/2: {4} | Финал: {5}\nГотово: {6}. Ждет/играет: {7}. Раунд: {8}.",
                "Participants {0}/{1}\nRound 1: {2} | Quarter: {3} | Semi: {4} | Final: {5}\nDone: {6}. Waiting/playing: {7}. Round: {8}.",
                "Katilimci {0}/{1}\n1. Tur: {2} | Ceyrek: {3} | Yari: {4} | Final: {5}\nBitti: {6}. Bekleyen/oynayan: {7}. Tur: {8}.",
                "Teilnehmer {0}/{1}\nRunde 1: {2} | Viertel: {3} | Halb: {4} | Finale: {5}\nFertig: {6}. Wartet/spielt: {7}. Runde: {8}."),
                bracket.participants != null ? bracket.participants.Length : 0, tournament.maxPlayers, round1, round2, round3, round4, completed, active, tournament.currentRound);
        }

        private void UpdateResultBanner(TournamentActiveResponse active, TournamentInfo tournament)
        {
            if (resultBanner == null || resultBannerText == null)
                return;

            string text = string.Empty;
            Color color = new Color(0.030f, 0.090f, 0.145f, 0.96f);
            if (active != null && active.pendingRewards != null && active.pendingRewards.Length > 0)
            {
                text = Text("Награда\nдоступна", "Reward\nready", "Ödül\nhazır", "Belohnung\nbereit");
                color = new Color(0.050f, 0.120f, 0.070f, 0.96f);
            }
            else if (active != null && active.currentMatch != null)
            {
                text = Text("Бой\nназначен", "Match\nready", "Maç\nhazır", "Match\nbereit");
                color = new Color(0.050f, 0.120f, 0.210f, 0.96f);
            }
            else if (active != null && string.Equals(active.participantStatus, "Eliminated", StringComparison.OrdinalIgnoreCase))
            {
                text = Text("Ты\nвыбыл", "Eliminated", "Elendin", "Ausgeschieden");
                color = new Color(0.120f, 0.055f, 0.050f, 0.96f);
            }
            else if (active != null && string.Equals(active.recentParticipantStatus, "Eliminated", StringComparison.OrdinalIgnoreCase))
            {
                text = active.recentFinalPlace > 0
                    ? string.Format(Text("Место\n#{0}", "Place\n#{0}", "Sira\n#{0}", "Platz\n#{0}"), active.recentFinalPlace)
                    : Text("Ты\nвыбыл", "Eliminated", "Elendin", "Ausgeschieden");
                color = new Color(0.120f, 0.055f, 0.050f, 0.96f);
            }
            else if (active != null && active.recentTournament != null && string.Equals(active.recentTournament.status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                text = Text("Турнир\nзавершен", "Cup\ncomplete", "Kupa\nbitti", "Pokal\nfertig");
                color = new Color(0.050f, 0.100f, 0.080f, 0.96f);
            }
            else if (active != null && active.active != null)
            {
                text = Text("В сетке\nждем", "In bracket\nwaiting", "Kupada\nbekle", "Im Baum\nwarten");
            }
            else if (tournament != null && string.Equals(tournament.status, "RegistrationOpen", StringComparison.Ordinal))
            {
                text = Text("Набор\nигроков", "Filling\ncup", "Kupa\ndoluyor", "Pokal\nfuellt sich");
            }

            resultBanner.SetActive(!string.IsNullOrWhiteSpace(text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                resultBanner.GetComponent<Image>().color = color;
                resultBannerText.text = text;
            }
        }

        private void RenderBracketView(TournamentInfo tournament)
        {
            if (bracketContent == null)
                return;

            for (int i = bracketContent.childCount - 1; i >= 0; i--)
                Destroy(bracketContent.GetChild(i).gameObject);

            TournamentBracketResponse bracket = TournamentService.I != null ? TournamentService.I.LastBracket : null;
            if (tournament == null)
            {
                AddBracketEmpty(Text("Сетка появится после создания турнира.", "Bracket appears after a cup is created.", "Kupa olusunca eslesme gorunur.", "Der Turnierbaum erscheint nach Erstellung des Pokals."));
                return;
            }

            if (bracket == null || bracket.tournament == null || bracket.tournament.id != tournament.id)
            {
                AddBracketEmpty(Text("Загрузка сетки...", "Loading bracket...", "Eşleşme yükleniyor...", "Turnierbaum wird geladen..."));
                return;
            }

            DrawBracketWeb(bracket);
        }

        private void RefreshTabButtons()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                Button button = tabButtons[i];
                if (button == null)
                    continue;

                bool selected = i == selectedTab;
                if (selected)
                    BattlePopupStyle.ApplyPremiumButton(button);
                else
                    BattlePopupStyle.ApplyButton(button);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    ApplyMainLobbyFont(label);
                    label.fontSize = selected ? 48f : 44f;
                    label.fontSizeMax = label.fontSize;
                    label.fontSizeMin = 24f;
                    label.color = selected ? new Color(1f, 0.86f, 0.42f, 1f) : Color.white;
                }
            }
        }

        private void DrawBracketWeb(TournamentBracketResponse bracket)
        {
            Vector2[][] positions =
            {
                new Vector2[]
                {
                    new Vector2(-500f, 88f), new Vector2(-500f, 63f), new Vector2(-500f, 38f), new Vector2(-500f, 13f),
                    new Vector2(-500f, -13f), new Vector2(-500f, -38f), new Vector2(-500f, -63f), new Vector2(-500f, -88f)
                },
                new Vector2[] { new Vector2(-180f, 76f), new Vector2(-180f, 25f), new Vector2(-180f, -25f), new Vector2(-180f, -76f) },
                new Vector2[] { new Vector2(160f, 52f), new Vector2(160f, -52f) },
                new Vector2[] { new Vector2(492f, 0f) }
            };

            for (int round = 0; round < positions.Length - 1; round++)
            {
                for (int slot = 0; slot < positions[round + 1].Length; slot++)
                {
                    CreateWebLine(bracketContent, positions[round][slot * 2], positions[round + 1][slot]);
                    CreateWebLine(bracketContent, positions[round][slot * 2 + 1], positions[round + 1][slot]);
                }
            }

            string[] roundTitles =
            {
                Text("Старт", "Start", "Başlangic", "Start"),
                Text("1/4", "Quarter", "Ceyrek", "Viertel"),
                Text("1/2", "Semi", "Yari", "Halbfinale"),
                Text("Финал", "Final", "Final", "Finale")
            };

            for (int round = 0; round < positions.Length; round++)
                CreateWebTitle(bracketContent, roundTitles[round], new Vector2(positions[round][0].x, 112f));

            for (int round = 0; round < positions.Length; round++)
            {
                int displayRound = round + 1;
                for (int slot = 0; slot < positions[round].Length; slot++)
                {
                    TournamentMatchInfo match = MatchAtRoundSlot(bracket, displayRound, slot);
                    bool mine = IsMyMatch(match);
                    string label = match != null ? BuildWebMatchLabel(bracket, match) : PlaceholderWebLabel(bracket, displayRound, slot);
                    Color color = match != null ? MatchColor(match.status) : new Color(0.023f, 0.045f, 0.060f, 0.90f);
                    if (mine)
                        color = new Color(0.120f, 0.100f, 0.018f, 0.98f);
                    Vector2 size = round == 0 ? new Vector2(220f, 29f) : new Vector2(238f, 38f);
                    if (round == 3)
                        size = new Vector2(250f, 54f);
                    CreateWebNode(bracketContent, "BracketWebNode", label, positions[round][slot], size, color, mine);
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        private static TournamentMatchInfo MatchAtRoundSlot(TournamentBracketResponse bracket, int roundIndex, int slot)
        {
            if (bracket == null || bracket.matches == null)
                return null;

            int seen = 0;
            for (int i = 0; i < bracket.matches.Length; i++)
            {
                TournamentMatchInfo match = bracket.matches[i];
                if (match == null || DisplayRoundIndex(match) != roundIndex)
                    continue;
                if (seen == slot)
                    return match;
                seen++;
            }
            return null;
        }

        private string PlaceholderWebLabel(TournamentBracketResponse bracket, int roundIndex, int slot)
        {
            if (roundIndex <= 1)
                return ShortName(ParticipantSlotLabel(bracket, slot));
            return Text("ожидает", "waiting", "bekliyor", "wartet");
        }

        private string BuildWebMatchLabel(TournamentBracketResponse bracket, TournamentMatchInfo match)
        {
            if (match == null)
                return Text("ожидает", "waiting", "bekliyor", "wartet");

            string a = ShortName(ParticipantName(bracket, match.playerAUserId));
            string b = ShortName(ParticipantName(bracket, match.playerBUserId));
            if (match.winnerUserId > 0)
                return ShortName(ParticipantName(bracket, match.winnerUserId));
            return a + " / " + b;
        }

        private void CreateWebTitle(Transform parent, string label, Vector2 position)
        {
            TMP_Text text = CreateText(parent, "BracketWebTitle", label, 20f, TextAlignmentOptions.Center, position, new Vector2(190f, 28f));
            text.color = new Color(1f, 0.80f, 0.34f, 0.95f);
            text.fontSizeMin = 14f;
        }

        private void CreateWebLine(Transform parent, Vector2 from, Vector2 to)
        {
            GameObject line = new GameObject("BracketWebLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            line.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)line.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Vector2 delta = to - from;
            rect.anchoredPosition = from + delta * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, 3.2f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Image image = line.GetComponent<Image>();
            image.color = new Color(0.95f, 0.55f, 0.08f, 0.74f);
            image.raycastTarget = false;
        }

        private void CreateWebNode(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, bool mine)
        {
            GameObject node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            node.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)node.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = node.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            ApplyTournamentButtonGraphic(image);

            Outline outline = node.AddComponent<Outline>();
            outline.effectColor = mine ? new Color(1f, 0.84f, 0.28f, 1f) : new Color(0.62f, 0.36f, 0.08f, 0.55f);
            outline.effectDistance = mine ? new Vector2(2.2f, -2.2f) : new Vector2(1f, -1f);

            TMP_Text text = CreateText(node.transform, "Text", label, size.y > 40f ? 22f : 18f, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(18f, 8f));
            text.fontSizeMin = 12f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void AddBracketFocus(TournamentBracketResponse bracket)
        {
            GameObject column = new GameObject("BracketFocus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            column.transform.SetParent(bracketContent, false);
            Image image = column.GetComponent<Image>();
            image.color = new Color(0.035f, 0.020f, 0.010f, 0.92f);
            image.raycastTarget = false;
            LayoutElement element = column.GetComponent<LayoutElement>();
            element.preferredWidth = 350f;
            element.minWidth = 350f;
            element.flexibleHeight = 1f;

            VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 10);
            layout.spacing = 9f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TournamentActiveResponse active = TournamentService.I != null ? TournamentService.I.LastActive : null;
            string title = Text("Сейчас", "Now", "Şimdi", "Jetzt");
            string body = Text("Следи за желтой рамкой в сетке.", "Follow the yellow card in the bracket.", "Sari karti takip et.", "Folge der gelben Karte.");
            Color bodyColor = new Color(0.020f, 0.060f, 0.105f, 0.94f);
            if (active != null && active.currentMatch != null)
            {
                string opponent = active.opponent != null && !string.IsNullOrWhiteSpace(active.opponent.displayName) ? active.opponent.displayName : Text("соперник", "opponent", "rakip", "Gegner");
                title = string.Format(Text("Тур {0}", "Round {0}", "Tur {0}", "Runde {0}"), Mathf.Max(1, active.currentMatch.roundIndex));
                body = string.Format(Text("Твой бой\n{0}\nНажми Бой", "Your match\n{0}\nPress Match", "Senin maçin\n{0}\nMaç'a bas", "Dein Match\n{0}\nMatch druecken"), ShortName(opponent));
                bodyColor = new Color(0.100f, 0.080f, 0.018f, 0.96f);
            }
            else if (active != null && active.pendingRewards != null && active.pendingRewards.Length > 0)
            {
                title = Text("Награда", "Reward", "Ödül", "Belohnung");
                body = Text("Приз готов\nНажми Забрать", "Prize ready\nPress Claim", "Ödül hazır\nAl'a bas", "Preis bereit\nAbholen druecken");
                bodyColor = new Color(0.035f, 0.115f, 0.075f, 0.94f);
            }

            GameObject header = CreateBracketCard("FocusHeader", new Vector2(320f, 40f), new Color(0.105f, 0.065f, 0.018f, 0.96f));
            header.transform.SetParent(column.transform, false);
            AddBracketText(header.transform, title, 26f, TextAlignmentOptions.Center, Color.white);

            GameObject card = CreateBracketCard("FocusBody", new Vector2(320f, 128f), bodyColor);
            card.transform.SetParent(column.transform, false);
            AddBracketText(card.transform, body, 27f, TextAlignmentOptions.Center, Color.white);

            GameObject hint = CreateBracketCard("FocusHint", new Vector2(320f, 34f), new Color(0.020f, 0.045f, 0.075f, 0.78f));
            hint.transform.SetParent(column.transform, false);
            AddBracketText(hint.transform, Text("Желтая рамка = твой матч", "Yellow outline = your match", "Sari cizgi = senin maçin", "Gelber Rand = dein Match"), 18f, TextAlignmentOptions.Center, new Color(0.86f, 0.76f, 0.42f, 1f));
        }

        private void AddBracketLegend()
        {
            GameObject column = new GameObject("BracketLegend", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            column.transform.SetParent(bracketContent, false);
            Image image = column.GetComponent<Image>();
            image.color = new Color(0.010f, 0.026f, 0.046f, 0.90f);
            image.raycastTarget = false;
            LayoutElement element = column.GetComponent<LayoutElement>();
            element.preferredWidth = 260f;
            element.minWidth = 260f;
            element.flexibleHeight = 1f;

            VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 12, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject header = CreateBracketCard("LegendHeader", new Vector2(230f, 38f), new Color(0.055f, 0.125f, 0.20f, 0.96f));
            header.transform.SetParent(column.transform, false);
            AddBracketText(header.transform, Text("Статус", "Status", "Durum", "Status"), 21f, TextAlignmentOptions.Center, new Color(0.82f, 0.93f, 1f, 1f));
            AddLegendRow(column.transform, Text("Идет", "Live", "Canli", "Live"), MatchColor("Active"));
            AddLegendRow(column.transform, Text("Готово", "Done", "Bitti", "Fertig"), MatchColor("Completed"));
            AddLegendRow(column.transform, Text("Проверка", "Review", "Inceleme", "Pruefung"), MatchColor("NeedsReview"));
            AddLegendRow(column.transform, Text("Ожидание", "Waiting", "Bekliyor", "Wartet"), MatchColor("Scheduled"));
            AddBracketText(column.transform, Text("Свайп вбок", "Swipe sideways", "Yana kaydir", "Seitlich wischen"), 18f, TextAlignmentOptions.Center, new Color(0.66f, 0.78f, 0.92f, 1f));
        }

        private void AddLegendRow(Transform parent, string label, Color color)
        {
            GameObject row = CreateBracketCard("LegendRow", new Vector2(230f, 34f), color);
            row.transform.SetParent(parent, false);
            AddBracketText(row.transform, label, 18f, TextAlignmentOptions.Center, Color.white);
        }

        private void AddBracketEmpty(string text)
        {
            GameObject card = CreateBracketCard("BracketEmpty", new Vector2(1080f, 170f), new Color(0.012f, 0.035f, 0.075f, 0.92f));
            card.transform.SetParent(bracketContent, false);
            RectTransform rect = (RectTransform)card.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            AddBracketText(card.transform, text, 30f, TextAlignmentOptions.Center, Color.white);
        }

        private void AddBracketColumn(string title, TournamentBracketResponse bracket, int roundIndex, int fallbackSlots)
        {
            GameObject column = new GameObject("BracketColumn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            column.transform.SetParent(bracketContent, false);
            RectTransform rect = (RectTransform)column.transform;
            rect.sizeDelta = new Vector2(340f, 220f);
            Image image = column.GetComponent<Image>();
            image.color = new Color(0.012f, 0.030f, 0.052f, 0.86f);
            image.raycastTarget = false;

            LayoutElement element = column.GetComponent<LayoutElement>();
            element.preferredWidth = 340f;
            element.minWidth = 340f;
            element.flexibleHeight = 1f;

            VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject header = CreateBracketCard("RoundHeader", new Vector2(312f, 34f), new Color(0.055f, 0.125f, 0.20f, 0.96f));
            header.transform.SetParent(column.transform, false);
            AddBracketText(header.transform, title, 24f, TextAlignmentOptions.Center, new Color(0.82f, 0.93f, 1f, 1f));

            int added = 0;
            if (roundIndex > 0 && bracket.matches != null)
            {
                for (int i = 0; i < bracket.matches.Length; i++)
                {
                    TournamentMatchInfo match = bracket.matches[i];
                    if (match == null || DisplayRoundIndex(match) != roundIndex)
                        continue;
                    AddMatchCard(column.transform, bracket, match);
                    added++;
                }
            }

            if (added == 0)
            {
                int slots = Mathf.Clamp(fallbackSlots, 1, 8);
                for (int i = 0; i < slots; i++)
                    AddPlaceholderCard(column.transform, roundIndex <= 1 ? ParticipantSlotLabel(bracket, i) : Text("Ожидает", "Waiting", "Bekliyor", "Wartet"));
            }
        }

        private void AddMatchCard(Transform parent, TournamentBracketResponse bracket, TournamentMatchInfo match)
        {
            Color color = MatchColor(match.status);
            bool mine = IsMyMatch(match);
            if (mine)
                color = new Color(0.110f, 0.145f, 0.040f, 0.98f);
            GameObject card = CreateBracketCard("MatchCard", new Vector2(312f, 46f), color);
            card.transform.SetParent(parent, false);
            if (mine)
            {
                Outline outline = card.AddComponent<Outline>();
                outline.effectColor = new Color(0.95f, 0.82f, 0.34f, 1f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
            string a = ParticipantName(bracket, match.playerAUserId);
            string b = ParticipantName(bracket, match.playerBUserId);
            string winner = match.winnerUserId > 0 ? ParticipantName(bracket, match.winnerUserId) : StatusLabel(match.status);
            string prefix = mine ? Text("ТЫ ", "YOU ", "SEN ", "DU ") : "";
            string result = match.winnerUserId > 0 ? Text("победил", "won", "kazandı", "gewonnen") + ": " + ShortName(winner) : StatusLabel(match.status);
            string text = prefix + "<b>" + ShortName(a) + "</b>  vs  <b>" + ShortName(b) + "</b>\n<color=#A8C7EA>" + result + "</color>";
            AddBracketText(card.transform, text, 20f, TextAlignmentOptions.Left, Color.white);
        }

        private static bool IsMyMatch(TournamentMatchInfo match)
        {
            TournamentActiveResponse active = TournamentService.I != null ? TournamentService.I.LastActive : null;
            int userId = active != null ? active.userId : 0;
            return userId > 0 && match != null && (match.playerAUserId == userId || match.playerBUserId == userId);
        }

        private void AddPlaceholderCard(Transform parent, string label)
        {
            GameObject card = CreateBracketCard("MatchPlaceholder", new Vector2(312f, 32f), new Color(0.020f, 0.045f, 0.075f, 0.78f));
            card.transform.SetParent(parent, false);
            AddBracketText(card.transform, ShortName(label), 18f, TextAlignmentOptions.Center, new Color(0.66f, 0.78f, 0.92f, 1f));
        }

        private GameObject CreateBracketCard(string name, Vector2 size, Color color)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            RectTransform rect = (RectTransform)card.transform;
            rect.sizeDelta = size;
            Image image = card.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            LayoutElement element = card.GetComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;
            element.minHeight = size.y;
            return card;
        }

        private TMP_Text AddBracketText(Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
        {
            TMP_Text text = CreateText(parent, "Text", value, size, alignment, Vector2.zero, Vector2.zero);
            RectTransform rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 4f);
            rect.offsetMax = new Vector2(-10f, -4f);
            text.color = color;
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static int DisplayRoundIndex(TournamentMatchInfo match)
        {
            if (match == null)
                return 0;
            return match.roundIndex <= 0 ? match.roundIndex + 1 : match.roundIndex;
        }

        private static Color MatchColor(string status)
        {
            if (string.Equals(status, "Completed", System.StringComparison.OrdinalIgnoreCase))
                return new Color(0.035f, 0.115f, 0.075f, 0.94f);
            if (string.Equals(status, "Active", System.StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Running", System.StringComparison.OrdinalIgnoreCase))
                return new Color(0.050f, 0.120f, 0.210f, 0.96f);
            if (string.Equals(status, "NeedsReview", System.StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Recoverable", System.StringComparison.OrdinalIgnoreCase))
                return new Color(0.160f, 0.090f, 0.035f, 0.96f);
            return new Color(0.022f, 0.060f, 0.105f, 0.92f);
        }

        private static string ParticipantName(TournamentBracketResponse bracket, int userId)
        {
            if (userId <= 0)
                return "BYE";
            if (bracket != null && bracket.participants != null)
            {
                for (int i = 0; i < bracket.participants.Length; i++)
                {
                    TournamentParticipantInfo participant = bracket.participants[i];
                    if (participant != null && participant.userId == userId)
                        return string.IsNullOrWhiteSpace(participant.nickname) ? "Player" : participant.nickname;
                }
            }
            return "#" + userId;
        }

        private static string ParticipantSlotLabel(TournamentBracketResponse bracket, int index)
        {
            if (bracket != null && bracket.participants != null && index >= 0 && index < bracket.participants.Length)
            {
                TournamentParticipantInfo participant = bracket.participants[index];
                if (participant != null && !string.IsNullOrWhiteSpace(participant.nickname))
                    return participant.nickname;
            }
            return Text("Свободный слот", "Open slot", "Bos slot", "Freier Slot");
        }

        private static string ShortName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "-";
            value = value.Trim();
            return value.Length <= 22 ? value : value.Substring(0, 21) + ".";
        }

        private static string StatusLabel(string status)
        {
            if (string.Equals(status, "RegistrationOpen", System.StringComparison.Ordinal))
                return Text("Регистрация открыта", "Registration open", "Kayıt açık", "Registrierung offen");
            if (string.Equals(status, "RegistrationFull", System.StringComparison.Ordinal))
                return Text("Набор закрыт", "Registration full", "Kayıt dolu", "Registrierung voll");
            if (string.Equals(status, "Running", System.StringComparison.Ordinal))
                return Text("Бои идут", "Matches running", "Maçlar sürüyor", "Matches laufen");
            if (string.Equals(status, "Completed", System.StringComparison.Ordinal))
                return Text("Завершен", "Completed", "Tamamlandi", "Abgeschlossen");
            if (string.Equals(status, "NeedsReview", System.StringComparison.Ordinal))
                return Text("Проверка", "Needs review", "Inceleme", "Pruefung");
            if (string.Equals(status, "ExpiredRegistration", System.StringComparison.Ordinal))
                return Text("Регистрация истекла", "Registration expired", "Kayıt süresi doldu", "Registrierung abgelaufen");
            return string.IsNullOrWhiteSpace(status) ? Text("Статус неизвестен", "Unknown status", "Bilinmeyen durum", "Unbekannter Status") : status;
        }

        private void RequestTabData()
        {
            TournamentService service = TournamentService.I;
            if (service == null)
                return;

            if (selectedTab == 1)
                service.RefreshBracket(CurrentTournamentId());
            else if (selectedTab == 3)
                service.RefreshFunds();
        }

        private static string BuildActiveStatus(TournamentInfo tournament, string participantStatus)
        {
            string state = string.IsNullOrWhiteSpace(participantStatus) ? tournament.status : participantStatus;
            if (string.Equals(participantStatus, "Eliminated", System.StringComparison.Ordinal))
                return Text("Ты выбыл из турнира. Проверь вкладку наград.", "You are eliminated. Check rewards.", "Turnuvadan elendin. Ödülleri kontrol et.", "Du bist ausgeschieden. Pruefe Belohnungen.");
            if (string.Equals(tournament.status, "NeedsReview", System.StringComparison.Ordinal))
                return Text("Турнир на проверке. Выплаты остановлены до решения сервера.", "Tournament needs review. Rewards are paused until server resolution.", "Turnuva incelemede. Sunucu kararina kadar ödüller durdu.", "Turnier in Pruefung. Belohnungen pausieren bis zur Serverentscheidung.");
            if (string.Equals(tournament.status, "RegistrationOpen", System.StringComparison.Ordinal))
                return Text("Ты зарегистрирован. До набора 16 игроков можно выйти с возвратом.", "You are registered. Before 16 players join, you can leave with refund.", "Kayıt oldun. 16 oyuncudan önce iade ile ayrılabilirsin.", "Du bist registriert. Vor 16 Spielern kannst du mit Rueckerstattung austreten.");
            if (string.Equals(tournament.status, "RegistrationFull", System.StringComparison.Ordinal))
                return Text("Набор закрыт. Ждем стартовый отсчет и первый бой.", "Registration is full. Waiting for countdown and first match.", "Kayıt dolu. Geri sayım ve ilk maç bekleniyor.", "Registrierung voll. Warte auf Countdown und erstes Match.");
            return Text("Ты в турнире. Ждем следующий шаг сетки.", "You are in the cup. Waiting for the next bracket step.", "Kupadasin. Sonraki eslesme bekleniyor.", "Du bist im Pokal. Warte auf den naechsten Schritt im Turnierbaum.");
        }

        private static int CountMatches(TournamentBracketResponse bracket, int roundIndex)
        {
            if (bracket == null || bracket.matches == null)
                return 0;
            int count = 0;
            for (int i = 0; i < bracket.matches.Length; i++)
            {
                if (bracket.matches[i] != null && DisplayRoundIndex(bracket.matches[i]) == roundIndex)
                    count++;
            }
            return count;
        }

        private static string TournamentTimingLine(TournamentInfo tournament)
        {
            if (tournament == null)
                return string.Empty;

            if (string.Equals(tournament.status, "RegistrationOpen", StringComparison.Ordinal))
            {
                TimeSpan remaining = Remaining(tournament.registrationExpiresAt);
                if (remaining.TotalSeconds > 0)
                    return string.Format(Text("Регистрация истекает через {0}", "Registration expires in {0}", "Kayıt bitiş: {0}", "Registrierung endet in {0}"), FormatDuration(remaining));
            }

            if (!string.IsNullOrWhiteSpace(tournament.startsAt))
            {
                TimeSpan remaining = Remaining(tournament.startsAt);
                if (remaining.TotalSeconds > 0)
                    return string.Format(Text("Старт через {0}", "Starts in {0}", "Başlangic: {0}", "Start in {0}"), FormatDuration(remaining));
            }

            return Text("Таймер обновляется сервером", "Timer updates from server", "Süre sunucudan güncellenir", "Timer kommt vom Server");
        }

        private static string TournamentTimingShort(TournamentInfo tournament)
        {
            if (tournament == null)
                return "-";

            if (string.Equals(tournament.status, "RegistrationOpen", StringComparison.Ordinal))
            {
                TimeSpan remaining = Remaining(tournament.registrationExpiresAt);
                if (remaining.TotalSeconds > 0)
                    return string.Format(Text("Рег {0}", "Reg {0}", "Kayıt {0}", "Reg {0}"), FormatDuration(remaining));
            }

            if (!string.IsNullOrWhiteSpace(tournament.startsAt))
            {
                TimeSpan remaining = Remaining(tournament.startsAt);
                if (remaining.TotalSeconds > 0)
                    return string.Format(Text("Старт {0}", "Start {0}", "Başla {0}", "Start {0}"), FormatDuration(remaining));
            }

            return StatusLabel(tournament.status);
        }

        private static TimeSpan Remaining(string timestamp)
        {
            if (string.IsNullOrWhiteSpace(timestamp))
                return TimeSpan.Zero;

            if (!DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime target))
                return TimeSpan.Zero;

            TimeSpan remaining = target - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        private static string FormatDuration(TimeSpan value)
        {
            if (value.TotalHours >= 1)
                return string.Format("{0:0}h {1:00}m", Math.Floor(value.TotalHours), value.Minutes);
            return string.Format("{0:00}:{1:00}", Mathf.Max(0, value.Minutes), Mathf.Max(0, value.Seconds));
        }

        private static int CountStatus(TournamentBracketResponse bracket, string status)
        {
            if (bracket == null || bracket.matches == null)
                return 0;
            int count = 0;
            for (int i = 0; i < bracket.matches.Length; i++)
            {
                if (bracket.matches[i] != null && string.Equals(bracket.matches[i].status, status, System.StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        private static int SumGrandFund(string league)
        {
            TournamentFundsResponse funds = TournamentService.I != null ? TournamentService.I.LastFunds : null;
            if (funds == null || funds.funds == null)
                return 0;

            int sum = 0;
            for (int i = 0; i < funds.funds.Length; i++)
            {
                TournamentFundInfo item = funds.funds[i];
                if (item == null)
                    continue;
                if (!string.Equals(item.league, league, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(item.currency) && !string.Equals(item.currency, "OzTile", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                sum += Mathf.Max(0, item.amount);
            }
            return sum;
        }

        private void ClaimFirstReward()
        {
            TournamentActiveResponse active = TournamentService.I != null ? TournamentService.I.LastActive : null;
            if (active == null || active.pendingRewards == null || active.pendingRewards.Length == 0)
                return;

            TournamentService.I.Claim(active.pendingRewards[0].id);
        }

        private int CurrentTournamentId()
        {
            TournamentActiveResponse active = TournamentService.I != null ? TournamentService.I.LastActive : null;
            if (active != null && active.active != null)
                return active.active.id;
            if (active != null && active.recentTournament != null)
                return active.recentTournament.id;

            TournamentInfo first = FirstTournament(TournamentService.I != null ? TournamentService.I.LastList : null);
            return first != null ? first.id : 0;
        }

        private static TournamentInfo DisplayTournament(TournamentActiveResponse active, TournamentListResponse list)
        {
            if (active != null && active.active != null)
                return active.active;
            if (active != null && active.recentTournament != null)
                return active.recentTournament;
            return FirstTournament(list);
        }

        private static TournamentInfo FirstTournament(TournamentListResponse list)
        {
            return list != null && list.tournaments != null && list.tournaments.Length > 0 ? list.tournaments[0] : null;
        }

        private void SetStatusText(string value)
        {
            if (statusText != null)
                statusText.text = ShortUiLine(value);
        }

        private static string ShortUiLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Replace("\r", " ").Replace("\n", " | ").Trim();
            return normalized.Length <= 92 ? normalized : normalized.Substring(0, 89) + "...";
        }

        private static void SetButtonState(Button button, bool enabled, string disabledReason)
        {
            if (button == null)
                return;

            button.interactable = enabled;
            BattlePopupStyle.ApplyButton(button);

            ApplyTournamentButtonGraphic(button.image);
            if (!enabled && button.image != null)
                button.image.color = new Color(0.78f, 0.78f, 0.78f, 0.92f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                ApplyMainLobbyFont(label);
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = 22f;
                label.fontSizeMax = 36f;
                label.fontSize = 34f;
                label.margin = new Vector4(18f, 0f, 18f, 0f);
                label.color = enabled
                    ? new Color(1f, 0.96f, 0.82f, 1f)
                    : new Color(0.94f, 0.88f, 0.72f, 1f);
                label.alpha = 1f;
            }
        }

        private string JoinDisabledReason(bool enabled, bool hasActive, TournamentActiveResponse active, TournamentInfo tournament)
        {
            if (!enabled) return Text("Сейчас в разработке", "In development", "Gelistiriliyor", "In Entwicklung");
            if (hasActive) return Text("Уже есть активный путь", "Already active", "Zaten aktif", "Bereits aktiv");
            if (tournament == null) return Text("Нет открытого кубка", "No open cup", "Açık kupa yok", "Kein offener Pokal");
            if (!string.Equals(tournament.status, "RegistrationOpen", System.StringComparison.Ordinal)) return Text("Регистрация закрыта", "Registration closed", "Kayıt kapalı", "Registrierung geschlossen");
            if (ResolveDisplayOzTileBalance(active) < tournament.entryFeeOzTile) return Text("Не хватает OzTile", "Not enough OzTile", "OzTile yetersiz", "Nicht genug OzTile");
            return string.Empty;
        }

        private static int ResolveDisplayOzTileBalance(TournamentActiveResponse active)
        {
            int serverBalance = active != null ? Mathf.Max(0, active.ozTileBalance) : 0;
            bool hasServerTournamentState = active != null && (active.active != null || active.currentMatch != null || (active.pendingRewards != null && active.pendingRewards.Length > 0));
            if (hasServerTournamentState)
                return serverBalance;

            int localBalance = CurrencyService.I != null ? Mathf.Max(0, CurrencyService.I.GetOzTile()) : 0;
            return Mathf.Max(serverBalance, localBalance);
        }

        private static string Text(string russian, string english, string turkish, string german = null)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.English;
            switch (language)
            {
                case GameLanguage.Russian:
                    return string.IsNullOrWhiteSpace(russian) ? english : russian;
                case GameLanguage.Turkish:
                    return string.IsNullOrWhiteSpace(turkish) ? english : turkish;
                case GameLanguage.German:
                    return string.IsNullOrWhiteSpace(german) ? english : german;
                default:
                    return string.IsNullOrWhiteSpace(english) ? russian : english;
            }
        }
    }
}
