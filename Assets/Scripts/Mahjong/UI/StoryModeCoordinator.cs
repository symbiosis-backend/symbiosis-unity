using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class StoryModeCoordinator : MonoBehaviour
    {
        private const string RuntimeRootName = "StoryModeRuntimeRoot";
        private const string WindowSpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_FullscreenPanel";
        private const string ButtonSpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_LongButton";
        private const string CellSpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_MediumButton";
        private const string StageCardSpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_StageCardThin_810x310";
        private const string DividerSpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Lobby_DecorativeDivider";
        private const string CarouselArrowSpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_CarouselArrow";
        private const string DifficultyTabEasySpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_DifficultyTab_Easy";
        private const string DifficultyTabMediumSpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_DifficultyTab_Medium";
        private const string DifficultyTabHardcoreSpritePath = "Mahjong/Sprites/BambooLobby/Mahjong_Bamboo_DifficultyTab_Hardcore";
        private const int TutorialLevelNumber = 1;
        private const int ChinaLevelNumber = 2;
        private const int TurkeyLevelNumber = 3;
        private const int TutorialStageCount = 5;
        private const float ReferenceLayoutWidth = 2400f;
        private const float ReferenceLayoutHeight = 1080f;

        private static readonly Vector2 PrimaryCellSize = new(760f, 438f);
        private static readonly Vector2 TutorialCellSize = new(760f, 438f);
        private static readonly Vector2 BranchCellSize = new(590f, 340f);
        private static readonly Vector2 StageCarouselCardSize = new(1900f, 728f);
        private static readonly Vector2 StageCarouselMainButtonSize = new(1700f, 420f);
        private static readonly Vector2 StageCarouselReplayButtonSize = new(520f, 230f);
        private static readonly Vector2 StageCarouselNavButtonSize = new(106f, 124f);
        private static readonly Vector2 HardcoreReplayCellSize = new(370f, 190f);
        private static readonly Vector2 TabCellSize = new(360f, 104f);
        private static readonly Vector2 DifficultyCellSize = new(425f, 230f);
        private static readonly Vector2 BackButtonMinSize = new(330f, 140f);
        private static readonly Vector2 BackButtonMaxSize = new(520f, 158f);

        private readonly List<Button> runtimeButtons = new();
        private readonly List<GameObject> dynamicObjects = new();

        private MahjongMenuUI owner;
        private GameObject panel;
        private RectTransform root;
        private RectTransform contentRoot;
        private RectTransform tabGrid;
        private RectTransform difficultyGrid;
        private RectTransform bodyGrid;
        private Image contentPanelImage;
        private TMP_Text titleText;
        private TMP_Text hintText;
        private Image dividerImage;
        private Button localBackButton;
        private Sprite cachedWindowSprite;
        private Sprite cachedButtonSprite;
        private Sprite cachedCellSprite;
        private Sprite cachedStageCardSprite;
        private Sprite cachedDividerSprite;
        private Sprite cachedCarouselArrowSprite;
        private Sprite cachedDifficultyTabEasySprite;
        private Sprite cachedDifficultyTabMediumSprite;
        private Sprite cachedDifficultyTabHardcoreSprite;

        public void Initialize(MahjongMenuUI menuOwner, GameObject levelSelectPanel, Canvas canvas)
        {
            owner = menuOwner;
            panel = levelSelectPanel;
            EnsureRoot();
            ShowChapters();
        }

        public void Refresh()
        {
            ShowChapters();
        }

        public void ShowChapters()
        {
            EnsureRoot();
            HideLegacyPanelChildren();
            ClearDynamicObjects();
            selectedView = StoryView.Root;
            selectedCarouselStage = 0;

            SetHeader(
                GameLocalization.Text("mahjong.story.categories.title"),
                ResolveRootHint());
            ResetHintFrame();
            SetHintVisible(true);

            CreateDifficultySelector();
            bool showTutorialEntry = ShouldShowTutorialEntry();
            Vector2 cellSize = showTutorialEntry ? TutorialCellSize : PrimaryCellSize;
            int columnCount = 1;
            SetBodyGridFrame(new Vector2(0f, -444f), new Vector2(1880f, 560f));
            ConfigureGrid(bodyGrid, cellSize, GridLayoutGroup.Constraint.FixedColumnCount, columnCount, new RectOffset(58, 58, 12, 20), new Vector2(72f, 30f));

            if (showTutorialEntry)
            {
                Button tutorialButton = CreateNodeButton(bodyGrid, StoryNode.Tutorial(), () => ShowTutorialStages(), TutorialCellSize);
                RectTransform tutorialRect = tutorialButton.transform as RectTransform;
                if (tutorialRect != null)
                    tutorialRect.anchoredPosition = new Vector2(0f, -42f);
            }
            else
                CreateNodeButton(bodyGrid, StoryNode.World(IsWorldUnlocked()), () => ShowWorldBranches());
        }

        public bool HandlesButton(Button button)
        {
            return button != null && runtimeButtons.Contains(button);
        }

        public bool TryNavigateBack()
        {
            switch (selectedView)
            {
                case StoryView.TutorialStages:
                case StoryView.WorldBranches:
                    ShowChapters();
                    return true;

                case StoryView.CountryBranches:
                    ShowWorldBranches();
                    return true;

                case StoryView.StoryLevelStages:
                    if (selectedStoryBranch == StoryThemeBranch.Countries)
                        ShowCountryBranches();
                    else
                        ShowWorldBranches();
                    return true;

                default:
                    return false;
            }
        }

        private StoryView selectedView = StoryView.Root;
        private MahjongStoryDifficulty selectedDifficulty = MahjongStoryDifficulty.Easy;
        private int selectedCarouselStage;
        private int selectedStoryLevel = ChinaLevelNumber;
        private StoryThemeBranch selectedStoryBranch = StoryThemeBranch.Countries;

        private void ShowTutorialStages()
        {
            EnsureRoot();
            HideLegacyPanelChildren();
            ClearDynamicObjects();
            selectedView = StoryView.TutorialStages;

            SetHeader(string.Empty, string.Empty);
            ResetHintFrame();
            SetHintVisible(false);

            CreateDifficultySelector();
            SetBodyGridFrame(new Vector2(0f, -260f), new Vector2(1880f, 760f));
            int stageCount = ResolveStageCount(TutorialLevelNumber, TutorialStageCount);
            CreateStageCarousel(TutorialLevelNumber, stageCount);
        }

        private void ShowWorldBranches()
        {
            EnsureRoot();
            HideLegacyPanelChildren();
            ClearDynamicObjects();
            selectedView = StoryView.WorldBranches;
            selectedCarouselStage = 0;

            if (!IsWorldUnlocked())
            {
                ShowChapters();
                return;
            }

            SetHeader(
                GameLocalization.Text("mahjong.story.chapter.world.title"),
                GameLocalization.Text("mahjong.story.chapter.world.subtitle"));
            SetHintFrame(new Vector2(0f, -274f), new Vector2(1840f, 104f), 38f, 30f, 44f);
            SetHintVisible(true);

            CreateDifficultySelector();
            SetBodyGridFrame(new Vector2(0f, -376f), new Vector2(1500f, 520f));
            ConfigureGrid(bodyGrid, BranchCellSize, GridLayoutGroup.Constraint.FixedColumnCount, 2, new RectOffset(34, 34, 0, 0), new Vector2(56f, -30f));

            CreateBranchButton(bodyGrid, GameLocalization.Text("mahjong.story.branch.countries.title"), GameLocalization.Text("mahjong.story.branch.countries.subtitle"), true, ShowCountryBranches);
            CreateBranchButton(bodyGrid, GameLocalization.Text("mahjong.story.branch.cosmos.title"), GameLocalization.Text("mahjong.story.branch.cosmos.subtitle"), IsBranchUnlocked(StoryThemeBranch.Cosmos), () => ShowFirstLevelInBranch(StoryThemeBranch.Cosmos));
            CreateBranchButton(bodyGrid, GameLocalization.Text("mahjong.story.branch.human.title"), GameLocalization.Text("mahjong.story.branch.human.subtitle"), IsBranchUnlocked(StoryThemeBranch.Human), () => ShowFirstLevelInBranch(StoryThemeBranch.Human));
            CreateBranchButton(bodyGrid, GameLocalization.Text("mahjong.story.branch.nature.title"), GameLocalization.Text("mahjong.story.branch.nature.subtitle"), IsBranchUnlocked(StoryThemeBranch.Nature), () => ShowFirstLevelInBranch(StoryThemeBranch.Nature));
        }

        private void ShowCountryBranches()
        {
            EnsureRoot();
            HideLegacyPanelChildren();
            ClearDynamicObjects();
            selectedView = StoryView.CountryBranches;
            selectedCarouselStage = 0;

            SetHeader(
                GameLocalization.Text("mahjong.story.countries.title"),
                GameLocalization.Text("mahjong.story.countries.subtitle"));
            SetHintFrame(new Vector2(0f, -274f), new Vector2(1840f, 104f), 38f, 30f, 44f);
            SetHintVisible(true);

            CreateDifficultySelector();
            SetBodyGridFrame(new Vector2(0f, -376f), new Vector2(1500f, 520f));
            ConfigureGrid(bodyGrid, BranchCellSize, GridLayoutGroup.Constraint.FixedColumnCount, 2, new RectOffset(34, 34, 0, 0), new Vector2(56f, -30f));

            CreateLevelBranchButton(ChinaLevelNumber, StoryThemeBranch.Countries);
            CreateLevelBranchButton(TurkeyLevelNumber, StoryThemeBranch.Countries);
        }

        private void ShowFirstLevelInBranch(StoryThemeBranch branch)
        {
            int[] levels = StoryThemedContentLibrary.GetLevelNumbers(branch);
            if (levels == null || levels.Length == 0)
            {
                ShowWorldBranches();
                return;
            }

            ShowStoryLevelStages(levels[0], branch);
        }

        private void ShowStoryLevelStages(int levelNumber, StoryThemeBranch branch)
        {
            EnsureRoot();
            HideLegacyPanelChildren();
            ClearDynamicObjects();
            selectedView = StoryView.StoryLevelStages;
            selectedStoryLevel = Mathf.Max(1, levelNumber);
            selectedStoryBranch = branch;

            SetHeader(
                ResolveStoryLevelTitle(selectedStoryLevel),
                ResolveStoryLevelSubtitle(selectedStoryLevel));
            ResetHintFrame();
            SetHintVisible(false);

            CreateDifficultySelector();
            SetBodyGridFrame(new Vector2(0f, -260f), new Vector2(1880f, 760f));
            int fallbackStageCount = StoryChinaContentLibrary.HasLevel(selectedStoryLevel)
                ? StoryChinaContentLibrary.StageCount
                : StoryThemedContentLibrary.GetStageCount(selectedStoryLevel);
            int stageCount = ResolveStageCount(selectedStoryLevel, fallbackStageCount);
            CreateStageCarousel(selectedStoryLevel, stageCount);
        }

        private void EnsureRoot()
        {
            if (panel == null)
                return;

            RectTransform panelRect = panel.transform as RectTransform;
            if (panelRect == null)
                return;

            Transform existing = panelRect.Find(RuntimeRootName);
            GameObject rootObject = existing != null
                ? existing.gameObject
                : new GameObject(RuntimeRootName, typeof(RectTransform));

            if (existing == null)
                rootObject.transform.SetParent(panelRect, false);

            root = rootObject.transform as RectTransform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;
            rootObject.SetActive(true);
            root.SetAsLastSibling();

            Transform oldInnerBack = root.Find("StoryModeShell/BackHint");
            if (oldInnerBack != null)
                oldInnerBack.gameObject.SetActive(false);

            RectTransform shade = EnsureRect(root, "StoryModeShade", Vector2.zero, Vector2.zero, typeof(CanvasRenderer), typeof(Image));
            shade.anchorMin = Vector2.zero;
            shade.anchorMax = Vector2.one;
            shade.offsetMin = Vector2.zero;
            shade.offsetMax = Vector2.zero;
            Image shadeImage = shade.GetComponent<Image>();
            shadeImage.color = new Color(0f, 0.008f, 0.004f, 0.94f);
            shadeImage.raycastTarget = false;

            RectTransform shell = EnsureRect(root, "StoryModeShell", Vector2.zero, Vector2.zero, typeof(CanvasRenderer), typeof(Image));
            shell.anchorMin = Vector2.zero;
            shell.anchorMax = Vector2.one;
            shell.offsetMin = Vector2.zero;
            shell.offsetMax = Vector2.zero;

            RectTransform shellFill = EnsureRect(shell, "StoryModeShellFill", Vector2.zero, Vector2.zero, typeof(CanvasRenderer), typeof(Image));
            shellFill.anchorMin = Vector2.zero;
            shellFill.anchorMax = Vector2.one;
            shellFill.offsetMin = new Vector2(10f, 10f);
            shellFill.offsetMax = new Vector2(-10f, -10f);
            shellFill.SetAsFirstSibling();
            Image shellFillImage = shellFill.GetComponent<Image>();
            shellFillImage.sprite = null;
            shellFillImage.type = Image.Type.Simple;
            shellFillImage.color = new Color(0f, 0f, 0f, 0f);
            shellFillImage.raycastTarget = false;

            Image shellImage = shell.GetComponent<Image>();
            shellImage.sprite = LoadWindowSprite();
            shellImage.type = shellImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            shellImage.preserveAspect = false;
            shellImage.pixelsPerUnitMultiplier = 1f;
            shellImage.color = shellImage.sprite != null ? new Color(1f, 1f, 1f, 0.98f) : new Color(0f, 0.055f, 0.035f, 0.96f);
            shellImage.raycastTarget = false;
            EnsureOutline(shellImage.gameObject, new Color(0.95f, 0.72f, 0.22f, 0.8f), new Vector2(2f, -2f));

            RectTransform contentPanel = EnsureRect(shell, "StoryContentPanel", Vector2.zero, Vector2.zero, typeof(CanvasRenderer), typeof(Image));
            contentPanel.anchorMin = Vector2.zero;
            contentPanel.anchorMax = Vector2.one;
            contentPanel.offsetMin = new Vector2(52f, 34f);
            contentPanel.offsetMax = new Vector2(-52f, -34f);
            contentPanel.SetSiblingIndex(1);
            contentPanelImage = contentPanel.GetComponent<Image>();
            contentPanelImage.sprite = null;
            contentPanelImage.type = Image.Type.Simple;
            contentPanelImage.preserveAspect = false;
            contentPanelImage.color = new Color(0f, 0f, 0f, 0f);
            contentPanelImage.raycastTarget = false;
            DisableOutline(contentPanel.gameObject);

            contentRoot = EnsureRect(shell, "StoryModeContentRoot", Vector2.zero, new Vector2(ReferenceLayoutWidth, ReferenceLayoutHeight));
            contentRoot.anchorMin = new Vector2(0.5f, 1f);
            contentRoot.anchorMax = new Vector2(0.5f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(ReferenceLayoutWidth, ReferenceLayoutHeight);
            float layoutScale = ResolveLayoutScale(panelRect);
            contentRoot.localScale = new Vector3(layoutScale, layoutScale, 1f);
            contentRoot.SetSiblingIndex(2);

            ReparentDirectChild(shell, contentRoot, "Title");
            ReparentDirectChild(shell, contentRoot, "Hint");
            ReparentDirectChild(shell, contentRoot, "StoryTabs");
            ReparentDirectChild(shell, contentRoot, "DifficultyGrid");
            ReparentDirectChild(shell, contentRoot, "BodyGrid");

            titleText = EnsureText(contentRoot, "Title", new Vector2(0f, -174f), new Vector2(1180f, 56f), 42f, TextAlignmentOptions.Center);
            AnchorToTop(titleText.rectTransform);
            dividerImage = EnsureDecorativeDivider(shell, dividerImage);
            hintText = EnsureText(contentRoot, "Hint", new Vector2(0f, -300f), new Vector2(1840f, 110f), 38f, TextAlignmentOptions.Center);
            hintText.fontSizeMin = 30f;
            hintText.fontSizeMax = 44f;
            AnchorToTop(hintText.rectTransform);
            tabGrid = EnsureRect(contentRoot, "StoryTabs", new Vector2(0f, 0f), new Vector2(1f, 1f), typeof(GridLayoutGroup));
            tabGrid.gameObject.SetActive(false);
            difficultyGrid = EnsureRect(contentRoot, "DifficultyGrid", new Vector2(0f, -4f), new Vector2(1450f, 250f), typeof(GridLayoutGroup));
            AnchorToTop(difficultyGrid);
            ConfigureGrid(difficultyGrid, DifficultyCellSize, GridLayoutGroup.Constraint.FixedColumnCount, 3, new RectOffset(24, 24, 6, 6), new Vector2(22f, 8f));
            bodyGrid = EnsureRect(contentRoot, "BodyGrid", new Vector2(0f, -260f), new Vector2(1880f, 760f), typeof(GridLayoutGroup));
            AnchorToTop(bodyGrid);

            string backLabel = GameLocalization.Text("common.back");
            Vector2 backButtonSize = ResolveBackButtonSize(backLabel);
            if (localBackButton == null)
                localBackButton = CreateStaticButton(shell, "StoryLocalBackButton", backButtonSize, new Vector2(-170f, 96f), HandleLocalBack);
            else
                PositionStaticButton(localBackButton, backButtonSize, new Vector2(-170f, 96f));

            StyleButton(localBackButton, backLabel, true, false);
        }

        private void SetBodyGridFrame(Vector2 anchoredPosition, Vector2 size)
        {
            if (bodyGrid == null)
                return;

            bodyGrid.anchoredPosition = anchoredPosition;
            bodyGrid.sizeDelta = size;
        }

        private void SetHintFrame(Vector2 anchoredPosition, Vector2 size, float fontSize, float minFontSize, float maxFontSize)
        {
            if (hintText == null)
                return;

            RectTransform rect = hintText.rectTransform;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            hintText.fontSize = fontSize;
            hintText.fontSizeMin = minFontSize;
            hintText.fontSizeMax = maxFontSize;
        }

        private void ResetHintFrame()
        {
            SetHintFrame(new Vector2(0f, -300f), new Vector2(1840f, 110f), 38f, 30f, 44f);
        }

        private void SetHintVisible(bool visible)
        {
            if (hintText != null)
                hintText.gameObject.SetActive(visible);
        }

        private void HandleLocalBack()
        {
            if (!TryNavigateBack())
                owner?.OnClickBackFromLevels();
        }

        private void HideLegacyPanelChildren()
        {
            if (panel == null)
                return;

            for (int i = 0; i < panel.transform.childCount; i++)
            {
                Transform child = panel.transform.GetChild(i);
                if (child == null)
                    continue;

                if (child == root || string.Equals(child.name, RuntimeRootName, StringComparison.Ordinal))
                    continue;

                child.gameObject.SetActive(false);
            }

            if (root != null)
                root.SetAsLastSibling();
        }

        private static bool IsBackControl(Transform child)
        {
            if (child == null || string.IsNullOrWhiteSpace(child.name))
                return false;

            return child.name.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0
                || child.name.IndexOf("BtnBack", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetHeader(string title, string hint)
        {
            if (titleText != null)
                titleText.text = title;

            if (hintText != null)
                hintText.text = hint;
        }

        private void CreateDifficultySelector()
        {
            if (difficultyGrid == null)
                return;

            if (!IsDifficultyUnlocked(selectedDifficulty))
                selectedDifficulty = MahjongStoryDifficulty.Easy;

            CreateDifficultyButton(MahjongStoryDifficulty.Easy);
            CreateDifficultyButton(MahjongStoryDifficulty.Medium);
            CreateDifficultyButton(MahjongStoryDifficulty.Hardcore);
        }

        private void CreateDifficultyButton(MahjongStoryDifficulty difficulty)
        {
            Button button = CreateRuntimeButton(difficultyGrid, "StoryDifficulty_" + difficulty, DifficultyCellSize);
            bool unlocked = IsDifficultyUnlocked(difficulty);
            bool selected = selectedDifficulty == difficulty;
            button.interactable = unlocked;
            if (unlocked)
            {
                button.onClick.AddListener(() =>
                {
                    selectedDifficulty = difficulty;
                    selectedCarouselStage = 0;
                    RebuildCurrentView();
                });
            }

            string title = GameLocalization.Text("mahjong.story.difficulty." + DifficultyKey(difficulty));
            StyleButton(button, title, unlocked, selected);
            ApplyDifficultyTabSprite(button, difficulty, selected, unlocked);
            StyleDifficultyButton(button, difficulty, unlocked, selected);
        }

        private void ApplyDifficultyTabSprite(Button button, MahjongStoryDifficulty difficulty, bool selected, bool unlocked)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            Sprite sprite = LoadDifficultyTabSprite(difficulty);
            if (sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
            DisableOutline(image.gameObject);
        }

        private void StyleDifficultyButton(Button button, MahjongStoryDifficulty difficulty, bool unlocked, bool selected)
        {
            if (button == null)
                return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
                return;

            RectTransform buttonRect = button.transform as RectTransform;
            Vector2 buttonSize = buttonRect != null ? buttonRect.sizeDelta : DifficultyCellSize;

            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.fontSize = 46f;
            text.fontSizeMin = 28f;
            text.fontSizeMax = difficulty == MahjongStoryDifficulty.Hardcore ? 50f : 54f;
            text.fontStyle = FontStyles.Bold;

            Color topColor;
            Color bottomColor;
            Color outlineColor;
            Color glowColor;
            switch (difficulty)
            {
                case MahjongStoryDifficulty.Medium:
                    topColor = new Color(1f, 0.97f, 0.55f, 1f);
                    bottomColor = new Color(1f, 0.68f, 0.22f, 1f);
                    outlineColor = new Color(0.35f, 0.13f, 0.02f, 0.96f);
                    glowColor = new Color(1f, 0.73f, 0.2f, selected ? 0.88f : 0.58f);
                    break;
                case MahjongStoryDifficulty.Hardcore:
                    topColor = new Color(1f, 0.92f, 0.56f, 1f);
                    bottomColor = new Color(1f, 0.36f, 0.18f, 1f);
                    outlineColor = new Color(0.24f, 0f, 0f, 0.98f);
                    glowColor = new Color(1f, 0.16f, 0.08f, selected ? 0.92f : 0.62f);
                    break;
                default:
                    topColor = new Color(0.78f, 1f, 0.72f, 1f);
                    bottomColor = new Color(0.3f, 1f, 0.42f, 1f);
                    outlineColor = new Color(0.02f, 0.18f, 0.03f, 0.96f);
                    glowColor = new Color(0.28f, 1f, 0.38f, selected ? 0.86f : 0.56f);
                    break;
            }

            if (!unlocked)
            {
                topColor = new Color(0.76f, 0.72f, 0.62f, 1f);
                bottomColor = new Color(0.45f, 0.43f, 0.37f, 1f);
                glowColor = new Color(0.12f, 0.1f, 0.06f, 0.46f);
            }

            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(topColor, topColor, bottomColor, bottomColor);
            text.color = Color.white;

            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
                outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2.2f, -2.2f);
            outline.useGraphicAlpha = false;

            Shadow glow = EnsureDifficultyGlow(text.gameObject);
            glow.effectColor = glowColor;
            glow.effectDistance = selected ? new Vector2(0f, -4f) : new Vector2(0f, -2.5f);
            glow.useGraphicAlpha = false;

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(buttonSize.x * 0.68f, buttonSize.y * 0.34f);
            textRect.anchoredPosition = new Vector2(0f, buttonSize.y * 0.105f);
            textRect.localScale = Vector3.one;
        }

        private static Shadow EnsureDifficultyGlow(GameObject target)
        {
            if (target == null)
                return null;

            Shadow[] shadows = target.GetComponents<Shadow>();
            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i] != null && !(shadows[i] is Outline))
                    return shadows[i];
            }

            return target.AddComponent<Shadow>();
        }

        private void RebuildCurrentView()
        {
            switch (selectedView)
            {
                case StoryView.TutorialStages:
                    ShowTutorialStages();
                    break;
                case StoryView.WorldBranches:
                    ShowWorldBranches();
                    break;
                case StoryView.CountryBranches:
                    ShowCountryBranches();
                    break;
                case StoryView.StoryLevelStages:
                    ShowStoryLevelStages(selectedStoryLevel, selectedStoryBranch);
                    break;
                default:
                    ShowChapters();
                    break;
            }
        }

        private Button CreateNodeButton(RectTransform parent, StoryNode node, Action onClick)
        {
            return CreateNodeButton(parent, node, onClick, PrimaryCellSize);
        }

        private Button CreateNodeButton(RectTransform parent, StoryNode node, Action onClick, Vector2 size)
        {
            Button button = CreateRuntimeButton(parent, "StoryNode_" + node.Id, size);
            button.interactable = node.Unlocked;
            button.onClick.AddListener(() =>
            {
                if (node.Unlocked)
                    onClick?.Invoke();
            });

            string status = node.Unlocked
                ? node.Status
                : GameLocalization.Text("mahjong.story.locked.tutorial");
            StyleCategoryButton(button, node.Title, status, node.Unlocked);
            return button;
        }

        private void StyleCategoryButton(Button button, string title, string subtitle, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = LoadDifficultyTabSprite(MahjongStoryDifficulty.Medium) ?? LoadCellSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = active ? Color.white : new Color(0.58f, 0.58f, 0.5f, 0.9f);
                image.raycastTarget = true;
                DisableOutline(image.gameObject);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = active ? new Color(1f, 0.96f, 0.76f, 1f) : Color.white;
            colors.pressedColor = active ? new Color(0.8f, 0.94f, 0.6f, 1f) : Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            RectTransform rect = button.transform as RectTransform;
            Vector2 size = rect != null ? rect.sizeDelta : PrimaryCellSize;
            float safeWidth = Mathf.Min(size.x * 0.72f, 560f);

            TMP_Text titleText = EnsureText(rect, "Title", new Vector2(0f, 76f), new Vector2(safeWidth, 88f), 62f, TextAlignmentOptions.Center);
            titleText.text = title;
            titleText.fontSizeMin = 42f;
            titleText.fontSizeMax = 70f;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Truncate;
            titleText.color = active ? new Color(1f, 0.92f, 0.42f, 1f) : new Color(0.76f, 0.72f, 0.6f, 1f);

            TMP_Text subtitleText = EnsureText(rect, "Subtitle", new Vector2(0f, -18f), new Vector2(safeWidth + 92f, 64f), 31f, TextAlignmentOptions.Center);
            subtitleText.text = subtitle;
            subtitleText.fontSizeMin = 22f;
            subtitleText.fontSizeMax = 34f;
            subtitleText.textWrappingMode = TextWrappingModes.NoWrap;
            subtitleText.overflowMode = TextOverflowModes.Truncate;
            subtitleText.color = active ? new Color(0.86f, 1f, 0.72f, 1f) : new Color(0.68f, 0.64f, 0.54f, 1f);

            Outline titleOutline = titleText.GetComponent<Outline>();
            if (titleOutline != null)
                titleOutline.effectDistance = new Vector2(2f, -2f);

            Outline subtitleOutline = subtitleText.GetComponent<Outline>();
            if (subtitleOutline != null)
                subtitleOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void CreateBranchButton(RectTransform parent, string title, string subtitle, bool active, Action onClick)
        {
            Button button = CreateRuntimeButton(parent, "StoryBranch_" + title, BranchCellSize);
            button.interactable = active;
            if (active)
                button.onClick.AddListener(() => onClick?.Invoke());

            string status = active ? subtitle : GameLocalization.Text("mahjong.story.chapter.soon");
            StyleBranchButton(button, title, status, active);
        }

        private void CreateLevelBranchButton(int levelNumber, StoryThemeBranch branch)
        {
            bool active = IsStoryLevelUnlocked(levelNumber);
            int fallbackStageCount = StoryChinaContentLibrary.HasLevel(levelNumber)
                ? StoryChinaContentLibrary.StageCount
                : StoryThemedContentLibrary.GetStageCount(levelNumber);
            string subtitle = active
                ? GameLocalization.Format("mahjong.story.chapter.stage_count", ResolveStageCount(levelNumber, fallbackStageCount))
                : GameLocalization.Text("mahjong.story.chapter.locked");

            CreateBranchButton(
                bodyGrid,
                ResolveStoryLevelTitle(levelNumber),
                subtitle,
                active,
                () => ShowStoryLevelStages(levelNumber, branch));
        }

        private void StyleBranchButton(Button button, string title, string subtitle, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = LoadBranchTabSprite(title, active);
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = active ? Color.white : new Color(0.58f, 0.58f, 0.5f, 0.92f);
                image.raycastTarget = true;
                DisableOutline(image.gameObject);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = active ? new Color(1f, 0.96f, 0.76f, 1f) : Color.white;
            colors.pressedColor = active ? new Color(0.8f, 0.94f, 0.6f, 1f) : Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            RectTransform rect = button.transform as RectTransform;
            Vector2 size = rect != null ? rect.sizeDelta : BranchCellSize;

            TMP_Text titleText = EnsureText(rect, "Title", new Vector2(0f, 30f), new Vector2(size.x * 0.68f, 112f), 46f, TextAlignmentOptions.Center);
            titleText.text = title;
            titleText.fontSizeMin = 28f;
            titleText.fontSizeMax = 54f;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Truncate;
            titleText.color = active ? new Color(1f, 0.9f, 0.42f, 1f) : new Color(0.76f, 0.72f, 0.6f, 1f);

            TMP_Text subtitleText = EnsureText(rect, "Subtitle", new Vector2(0f, -104f), new Vector2(size.x * 0.74f, 44f), 24f, TextAlignmentOptions.Center);
            subtitleText.text = string.Empty;
            subtitleText.gameObject.SetActive(false);

            Outline titleOutline = titleText.GetComponent<Outline>();
            if (titleOutline != null)
                titleOutline.effectDistance = new Vector2(1.8f, -1.8f);

            Outline subtitleOutline = subtitleText.GetComponent<Outline>();
            if (subtitleOutline != null)
                subtitleOutline.effectDistance = new Vector2(1.3f, -1.3f);
        }

        private void CreateStageCarousel(int level, int stageCount)
        {
            ConfigureStageCarouselRoot();

            if (stageCount <= 0)
                return;

            int stage = ResolveCarouselStage(level, stageCount);
            CreateCarouselNavButton("StoryStagePrev", new Vector2(-1000f, 96f), false, stage > 1, () =>
            {
                selectedCarouselStage = Mathf.Max(1, stage - 1);
                RebuildCurrentView();
            });

            CreateCarouselNavButton("StoryStageNext", new Vector2(1000f, 96f), true, stage < stageCount, () =>
            {
                selectedCarouselStage = Mathf.Min(stageCount, stage + 1);
                RebuildCurrentView();
            });

            CreateStageCarouselCard(bodyGrid, level, stage, stageCount);
        }

        private void CreateStageCarouselCard(RectTransform parent, int level, int stage, int stageCount)
        {
            RectTransform cell = CreateRuntimeCell(parent, $"StoryStageCarouselCard_{level:00}_{stage:00}", StageCarouselCardSize, LoadStageCardSprite());
            cell.anchoredPosition = new Vector2(0f, 80f);
            bool completed = MahjongProgress.IsStoryStageCompleted(selectedDifficulty, level, stage);
            int bestScore = MahjongProgress.GetStoryStageBestScore(selectedDifficulty, level, stage);
            bool active = CanPlayStage(level, stage);

            string score = GameLocalization.Format("mahjong.story.stage.best_score", bestScore);
            string action = ResolveStageActionText(active, completed);
            TMP_Text levelText = CreateRuntimeText(cell, $"StoryStageLevel_{level:00}_{stage:00}", new Vector2(0f, 198f), new Vector2(1120f, 78f), 48f, TextAlignmentOptions.Center);
            levelText.text = GameLocalization.Format("mahjong.story.stage.level", stage);
            levelText.fontSizeMin = 34f;
            levelText.fontSizeMax = 58f;

            TMP_Text descriptionText = CreateRuntimeText(cell, $"StoryStageDescription_{level:00}_{stage:00}", new Vector2(0f, 78f), new Vector2(1400f, 142f), 34f, TextAlignmentOptions.Center);
            descriptionText.text = ResolveStageCardDescription(level, stage);
            descriptionText.fontSizeMin = 22f;
            descriptionText.fontSizeMax = 38f;
            descriptionText.lineSpacing = 2f;
            descriptionText.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text scoreText = CreateRuntimeText(cell, $"StoryStageScore_{level:00}_{stage:00}", new Vector2(0f, -52f), new Vector2(1120f, 58f), 30f, TextAlignmentOptions.Center);
            scoreText.text = score;
            scoreText.fontSizeMin = 22f;
            scoreText.fontSizeMax = 36f;

            if (completed)
                CreateCompletedStamp(cell, level, stage);

            bool hasStageReplayButton = selectedDifficulty != MahjongStoryDifficulty.Hardcore;

            if (!hasStageReplayButton)
            {
                if (!active)
                    descriptionText.text = action;

                CreateHardcoreReplayLevelButton(parent, level);
                return;
            }

            Button actionButton = CreateRuntimeButton(cell, $"StoryStageAction_{level:00}_{stage:00}", StageCarouselReplayButtonSize);
            RectTransform actionRect = actionButton.transform as RectTransform;
            actionRect.anchoredPosition = new Vector2(0f, -218f);
            actionButton.gameObject.SetActive(active);
            actionButton.interactable = active;
            actionButton.onClick.AddListener(() =>
            {
                if (active)
                    owner?.OnClickStoryStage(level, stage, actionButton, selectedDifficulty);
            });

            string actionLabel = completed
                ? GameLocalization.Text("mahjong.story.stage.replay")
                : GameLocalization.Text("mahjong.story.stage.play");
            StyleButton(actionButton, actionLabel, active);
            StyleActionButtonLabel(actionButton);
        }

        private void CreateCarouselNavButton(string name, Vector2 position, bool mirrored, bool active, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateRuntimeButton(bodyGrid, name, StageCarouselNavButtonSize);
            RectTransform rect = button.transform as RectTransform;
            rect.anchoredPosition = position;
            rect.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = LoadCarouselArrowSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = active ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.62f);
                image.raycastTarget = true;
                DisableOutline(image.gameObject);
            }

            button.interactable = active;
            if (active)
                button.onClick.AddListener(action);
        }

        private void CreateCompletedStamp(RectTransform parent, int level, int stage)
        {
            TMP_Text stamp = CreateRuntimeText(parent, $"StoryStageCompletedStamp_{level:00}_{stage:00}", new Vector2(520f, 154f), new Vector2(360f, 92f), 38f, TextAlignmentOptions.Center);
            stamp.text = GameLocalization.Text("mahjong.story.stage.completed").ToUpperInvariant();
            stamp.fontSizeMin = 26f;
            stamp.fontSizeMax = 44f;
            stamp.color = new Color(0.92f, 0.18f, 0.12f, 0.9f);
            stamp.outlineColor = new Color(1f, 0.88f, 0.36f, 0.95f);
            stamp.outlineWidth = 0.18f;
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -9f);

            Outline outline = stamp.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = new Color(0.12f, 0.02f, 0f, 0.85f);
        }

        private void CreateHardcoreReplayLevelButton(RectTransform parent, int level)
        {
            if (selectedDifficulty != MahjongStoryDifficulty.Hardcore)
                return;

            Button button = CreateRuntimeButton(parent, $"StoryHardcoreReplayLevel_{level:00}", HardcoreReplayCellSize);
            RectTransform rect = button.transform as RectTransform;
            rect.anchoredPosition = new Vector2(0f, -228f);
            button.onClick.AddListener(() =>
            {
                MahjongProgress.ResetHardcoreRun(level);
                RebuildCurrentView();
            });

            StyleButton(button, GameLocalization.Text("mahjong.story.hardcore.replay_level"), true, false);
        }

        private void AppendScoreSummary(int level, int stageCount)
        {
            if (hintText == null)
                return;

            int total = MahjongProgress.GetStoryLevelBestScoreTotal(selectedDifficulty, level, stageCount);
            hintText.text += "\n" + GameLocalization.Format("mahjong.story.level.best_score_total", total);
        }

        private Button CreateRuntimeButton(RectTransform parent, string name, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            Button button = rect.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.targetGraphic = rect.GetComponent<Image>();
            button.transition = Selectable.Transition.ColorTint;
            button.gameObject.SetActive(true);

            runtimeButtons.Add(button);
            dynamicObjects.Add(button.gameObject);
            return button;
        }

        private Button CreateStaticButton(RectTransform parent, string name, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            PositionStaticButton(button, size, position);
            return button;
        }

        private static void PositionStaticButton(Button button, Vector2 size, Vector2 position)
        {
            if (button == null)
                return;

            RectTransform rect = button.transform as RectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            button.gameObject.SetActive(true);
        }

        private static Vector2 ResolveBackButtonSize(string label)
        {
            int length = string.IsNullOrWhiteSpace(label) ? 4 : label.Trim().Length;
            float width = Mathf.Clamp(190f + length * 20f, BackButtonMinSize.x, BackButtonMaxSize.x);
            float height = Mathf.Clamp(BackButtonMinSize.y + Mathf.Max(0, length - 10) * 2f, BackButtonMinSize.y, BackButtonMaxSize.y);
            return new Vector2(width, height);
        }

        private static void AnchorToTop(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }

        private static float ResolveLayoutScale(RectTransform panelRect)
        {
            Vector2 size = panelRect != null ? panelRect.rect.size : new Vector2(Screen.width, Screen.height);
            float width = Mathf.Max(1f, size.x);
            float height = Mathf.Max(1f, size.y);
            float aspect = width / height;
            const float fullLayoutAspect = 1.75f;
            if (aspect >= fullLayoutAspect)
                return 1f;

            return Mathf.Clamp(aspect / fullLayoutAspect, 0.68f, 1f);
        }

        private static void ReparentDirectChild(RectTransform oldParent, RectTransform newParent, string childName)
        {
            if (oldParent == null || newParent == null || string.IsNullOrEmpty(childName))
                return;

            Transform existing = oldParent.Find(childName);
            if (existing != null && existing.parent != newParent)
                existing.SetParent(newParent, false);
        }

        private RectTransform CreateRuntimeCell(RectTransform parent, string name, Vector2 size)
        {
            return CreateRuntimeCell(parent, name, size, LoadCellSprite());
        }

        private RectTransform CreateRuntimeCell(RectTransform parent, string name, Vector2 size, Sprite sprite)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            Image image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
                image.color = image.sprite != null ? Color.white : new Color(0f, 0.035f, 0.02f, 1f);
            image.raycastTarget = false;
            if (image.sprite != null)
                DisableOutline(go);
            else
                EnsureOutline(go, new Color(0.9f, 0.68f, 0.18f, 0.58f), new Vector2(1.5f, -1.5f));

            dynamicObjects.Add(go);
            return rect;
        }

        private TMP_Text CreateRuntimeText(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            TMP_Text text = rect.GetComponent<TMP_Text>();
            text.raycastTarget = false;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.fontSizeMin = 18f;
            text.fontSizeMax = fontSize + 8f;
            text.enableAutoSizing = true;
            text.color = new Color(1f, 0.92f, 0.54f, 1f);
            text.fontStyle = FontStyles.Bold;
            ApplyStoryFont(text);
            EnsureOutline(go, new Color(0f, 0f, 0f, 0.85f), new Vector2(1.25f, -1.25f));

            dynamicObjects.Add(go);
            return text;
        }

        private void StyleButton(Button button, string label, bool active)
        {
            StyleButton(button, label, active, false);
        }

        private void StyleButton(Button button, string label, bool active, bool selected)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = LoadButtonSprite();
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = image.sprite != null
                    ? (selected ? new Color(1f, 0.96f, 0.72f, 1f) : active ? Color.white : new Color(0.48f, 0.48f, 0.42f, 1f))
                    : (active ? new Color(0.025f, 0.15f, 0.095f, 1f) : new Color(0.04f, 0.05f, 0.045f, 1f));
                image.raycastTarget = true;
                EnsureOutline(image.gameObject, selected ? new Color(0.45f, 1f, 0.48f, 0.95f) : active ? new Color(1f, 0.74f, 0.18f, 0.9f) : new Color(0.48f, 0.46f, 0.36f, 0.72f), new Vector2(2f, -2f));
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = active ? new Color(1f, 0.97f, 0.76f, 1f) : Color.white;
            colors.pressedColor = active ? new Color(0.78f, 0.94f, 0.58f, 1f) : Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TMP_Text text = EnsureText(button.transform as RectTransform, "Label", Vector2.zero, Vector2.zero, 30f, TextAlignmentOptions.Center);
            text.text = label;
            RectTransform buttonRect = button.transform as RectTransform;
            Vector2 buttonSize = buttonRect != null ? buttonRect.sizeDelta : Vector2.zero;
            bool heroButton = buttonSize.y >= 220f;
            bool largeButton = buttonSize.x >= 300f || buttonSize.y >= 100f;
            text.fontSizeMin = heroButton ? 24f : largeButton ? 20f : 16f;
            text.fontSizeMax = heroButton ? 56f : buttonSize.y >= 130f ? 42f : largeButton ? 46f : 36f;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.color = selected ? new Color(0.56f, 1f, 0.58f, 1f) : active ? new Color(1f, 0.9f, 0.46f, 1f) : new Color(0.78f, 0.75f, 0.62f, 1f);
            text.fontStyle = FontStyles.Bold;

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            float horizontalPadding = buttonSize.x <= 230f ? 14f : heroButton ? 54f : largeButton ? 30f : 28f;
            float verticalPadding = buttonSize.y <= 44f ? 6f : buttonSize.y <= 105f ? 12f : heroButton ? 34f : 18f;
            textRect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            textRect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }

        private static void StyleActionButtonLabel(Button button)
        {
            if (button == null)
                return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
                return;

            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 42f;

            RectTransform textRect = text.rectTransform;
            if (textRect == null)
                return;

            textRect.offsetMin = new Vector2(42f, 28f);
            textRect.offsetMax = new Vector2(-42f, -28f);
        }

        private TMP_Text EnsureText(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            RectTransform rect = EnsureRect(parent, name, position, size, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            TMP_Text text = rect.GetComponent<TMP_Text>();
            text.raycastTarget = false;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.fontSizeMin = 12f;
            text.fontSizeMax = fontSize + 4f;
            text.enableAutoSizing = true;
            text.color = new Color(1f, 0.92f, 0.54f, 1f);
            ApplyStoryFont(text);
            EnsureOutline(text.gameObject, new Color(0f, 0f, 0f, 0.85f), new Vector2(1.25f, -1.25f));
            rect.gameObject.SetActive(true);
            return text;
        }

        private static void ApplyStoryFont(TMP_Text text)
        {
            LocalizedTextStyle.Apply(text);
        }

        private Image EnsureDecorativeDivider(RectTransform parent, Image current)
        {
            if (current != null)
                current.gameObject.SetActive(false);

            if (parent != null)
            {
                Transform existing = parent.Find("HeaderDivider");
                if (existing != null)
                    existing.gameObject.SetActive(false);
            }

            return current;
        }

        private static RectTransform EnsureRect(RectTransform parent, string name, Vector2 position, Vector2 size, params Type[] components)
        {
            Transform existing = parent != null ? parent.Find(name) : null;
            GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            if (existing == null)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] != typeof(RectTransform) && go.GetComponent(components[i]) == null)
                        go.AddComponent(components[i]);
                }

                go.transform.SetParent(parent, false);
            }
            else
            {
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] != typeof(RectTransform) && go.GetComponent(components[i]) == null)
                        go.AddComponent(components[i]);
                }
            }

            RectTransform rect = go.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            if (size != Vector2.zero)
                rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            go.SetActive(true);
            return rect;
        }

        private static void ConfigureGrid(RectTransform grid, Vector2 cellSize, GridLayoutGroup.Constraint constraint, int constraintCount, RectOffset padding, Vector2 spacing)
        {
            if (grid == null)
                return;

            GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
            layout.enabled = true;
            layout.cellSize = cellSize;
            layout.spacing = spacing;
            layout.padding = padding;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = constraint;
            layout.constraintCount = Mathf.Max(1, constraintCount);
        }

        private void ConfigureStageCarouselRoot()
        {
            if (bodyGrid == null)
                return;

            GridLayoutGroup layout = bodyGrid.GetComponent<GridLayoutGroup>();
            if (layout != null)
                layout.enabled = false;
        }

        private void ClearDynamicObjects()
        {
            for (int i = dynamicObjects.Count - 1; i >= 0; i--)
            {
                if (dynamicObjects[i] != null)
                {
                    dynamicObjects[i].SetActive(false);
                    Destroy(dynamicObjects[i]);
                }
            }

            dynamicObjects.Clear();
            runtimeButtons.Clear();
        }

        private static void EnsureOutline(GameObject go, Color color, Vector2 distance)
        {
            if (go == null)
                return;

            Outline outline = go.GetComponent<Outline>();
            if (outline == null)
                outline = go.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = false;
            outline.enabled = true;
        }

        private static void DisableOutline(GameObject go)
        {
            if (go == null)
                return;

            Outline outline = go.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
        }

        private Sprite LoadWindowSprite()
        {
            if (cachedWindowSprite == null)
                cachedWindowSprite = Resources.Load<Sprite>(WindowSpritePath);

            return cachedWindowSprite;
        }

        private Sprite LoadButtonSprite()
        {
            if (cachedButtonSprite == null)
                cachedButtonSprite = Resources.Load<Sprite>(ButtonSpritePath);

            return cachedButtonSprite;
        }

        private Sprite LoadCellSprite()
        {
            if (cachedCellSprite == null)
                cachedCellSprite = Resources.Load<Sprite>(CellSpritePath);

            return cachedCellSprite;
        }

        private Sprite LoadStageCardSprite()
        {
            if (cachedStageCardSprite == null)
                cachedStageCardSprite = Resources.Load<Sprite>(StageCardSpritePath);

            return cachedStageCardSprite;
        }

        private Sprite LoadDividerSprite()
        {
            if (cachedDividerSprite == null)
                cachedDividerSprite = Resources.Load<Sprite>(DividerSpritePath);

            return cachedDividerSprite;
        }

        private Sprite LoadCarouselArrowSprite()
        {
            if (cachedCarouselArrowSprite == null)
                cachedCarouselArrowSprite = Resources.Load<Sprite>(CarouselArrowSpritePath);

            return cachedCarouselArrowSprite;
        }

        private Sprite LoadDifficultyTabSprite(MahjongStoryDifficulty difficulty)
        {
            switch (difficulty)
            {
                case MahjongStoryDifficulty.Medium:
                    if (cachedDifficultyTabMediumSprite == null)
                        cachedDifficultyTabMediumSprite = Resources.Load<Sprite>(DifficultyTabMediumSpritePath);
                    return cachedDifficultyTabMediumSprite;

                case MahjongStoryDifficulty.Hardcore:
                    if (cachedDifficultyTabHardcoreSprite == null)
                        cachedDifficultyTabHardcoreSprite = Resources.Load<Sprite>(DifficultyTabHardcoreSpritePath);
                    return cachedDifficultyTabHardcoreSprite;

                default:
                    if (cachedDifficultyTabEasySprite == null)
                        cachedDifficultyTabEasySprite = Resources.Load<Sprite>(DifficultyTabEasySpritePath);
                    return cachedDifficultyTabEasySprite;
            }
        }

        private Sprite LoadBranchTabSprite(string title, bool active)
        {
            if (active)
                return LoadDifficultyTabSprite(MahjongStoryDifficulty.Easy) ?? LoadCellSprite();

            if (!string.IsNullOrWhiteSpace(title) &&
                (title.IndexOf("Do", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 title.IndexOf("Nature", StringComparison.OrdinalIgnoreCase) >= 0))
                return LoadDifficultyTabSprite(MahjongStoryDifficulty.Hardcore) ?? LoadCellSprite();

            return LoadDifficultyTabSprite(MahjongStoryDifficulty.Medium) ?? LoadCellSprite();
        }

        private int ResolveCarouselStage(int levelNumber, int stageCount)
        {
            if (selectedCarouselStage >= 1 && selectedCarouselStage <= stageCount)
                return selectedCarouselStage;

            if (selectedDifficulty == MahjongStoryDifficulty.Hardcore)
                return Mathf.Clamp(MahjongProgress.GetHardcoreUnlockedStage(levelNumber), 1, stageCount);

            for (int stage = 1; stage <= stageCount; stage++)
            {
                if (!MahjongProgress.IsStoryStageCompleted(selectedDifficulty, levelNumber, stage))
                    return stage;
            }

            return stageCount;
        }

        private static int ResolveStageCount(int levelNumber, int fallback)
        {
            if (levelNumber == TutorialLevelNumber)
                return TutorialStageCount;

            if (TileStore.I != null)
            {
                int count = TileStore.I.GetStageCount(levelNumber);
                if (count > 0)
                    return count;
            }

            return Mathf.Max(0, fallback);
        }

        private static bool IsWorldUnlocked()
        {
            return MahjongProgress.UnlockedLevel >= ChinaLevelNumber || MahjongProgress.TutorialCompleted;
        }

        private static bool IsStoryLevelUnlocked(int levelNumber)
        {
            if (levelNumber <= TutorialLevelNumber)
                return true;

            return MahjongProgress.UnlockedLevel >= levelNumber ||
                   (levelNumber == ChinaLevelNumber && MahjongProgress.TutorialCompleted);
        }

        private static bool IsBranchUnlocked(StoryThemeBranch branch)
        {
            int[] levels = StoryThemedContentLibrary.GetLevelNumbers(branch);
            if (levels == null || levels.Length == 0)
                return false;

            for (int i = 0; i < levels.Length; i++)
            {
                if (IsStoryLevelUnlocked(levels[i]))
                    return true;
            }

            return false;
        }

        private bool ShouldShowTutorialEntry()
        {
            return selectedDifficulty == MahjongStoryDifficulty.Easy && !MahjongProgress.TutorialCompleted;
        }

        private static bool IsDifficultyUnlocked(MahjongStoryDifficulty difficulty)
        {
            return difficulty == MahjongStoryDifficulty.Easy || MahjongProgress.TutorialCompleted;
        }

        private string ResolveRootHint()
        {
            return ShouldShowTutorialEntry()
                ? GameLocalization.Text("mahjong.story.categories.hint")
                : GameLocalization.Text("mahjong.story.chapter.world.subtitle");
        }

        private bool CanPlayStage(int level, int stage)
        {
            if (selectedDifficulty != MahjongStoryDifficulty.Hardcore)
                return true;

            int unlocked = MahjongProgress.GetHardcoreUnlockedStage(level);
            return stage == unlocked;
        }

        private string ResolveStageActionText(bool active, bool completed)
        {
            if (!active)
                return GameLocalization.Text("mahjong.story.hardcore.locked_stage");

            if (selectedDifficulty == MahjongStoryDifficulty.Hardcore)
                return GameLocalization.Text("mahjong.story.stage.play");

            return completed
                ? GameLocalization.Text("mahjong.story.stage.replay")
                : GameLocalization.Text("mahjong.story.stage.play");
        }

        private static string ResolveStageCardDescription(int level, int stage)
        {
            if (level == TutorialLevelNumber)
                return GameLocalization.Text("mahjong.story.tutorial.stage." + Mathf.Clamp(stage, 1, TutorialStageCount));

            if (TileStore.I != null && TileStore.I.TryGetStageContent(level, stage, out LevelStageContent content))
            {
                if (!string.IsNullOrWhiteSpace(content.Title))
                    return content.Title;

                if (!string.IsNullOrWhiteSpace(content.Description))
                    return TrimStageDescription(content.Description);
            }

            return GameLocalization.Text("mahjong.story.chapter.tutorial.subtitle");
        }

        private static string ResolveStoryLevelTitle(int levelNumber)
        {
            if (levelNumber == ChinaLevelNumber)
                return GameLocalization.Text("mahjong.story.chapter.china.title");

            if (StoryThemedContentLibrary.HasLevel(levelNumber))
                return StoryThemedContentLibrary.GetLevelDisplayName(levelNumber, ResolveLanguage());

            return GameLocalization.Format("mahjong.story.chapter.future.title", levelNumber);
        }

        private static string ResolveStoryLevelSubtitle(int levelNumber)
        {
            if (levelNumber == ChinaLevelNumber)
                return GameLocalization.Text("mahjong.story.chapter.china.subtitle");

            if (StoryThemedContentLibrary.HasLevel(levelNumber))
                return StoryThemedContentLibrary.GetLevelSubtitle(levelNumber, ResolveLanguage());

            return GameLocalization.Text("mahjong.story.chapter.future.subtitle");
        }

        private static GameLanguage ResolveLanguage()
        {
            return AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
        }

        private static string TrimStageDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            string clean = description.Trim();
            int dot = clean.IndexOf(". ", StringComparison.Ordinal);
            if (clean.StartsWith("Факт ", StringComparison.OrdinalIgnoreCase) && dot >= 0 && dot + 2 < clean.Length)
                clean = clean[(dot + 2)..].Trim();

            const int maxLength = 132;
            if (clean.Length <= maxLength)
                return clean;

            int cut = clean.LastIndexOf(' ', maxLength);
            if (cut < 48)
                cut = maxLength;

            return clean[..cut].TrimEnd('.', ',', ';', ':') + "...";
        }

        private static string DifficultyKey(MahjongStoryDifficulty difficulty)
        {
            return difficulty switch
            {
                MahjongStoryDifficulty.Medium => "medium",
                MahjongStoryDifficulty.Hardcore => "hardcore",
                _ => "easy"
            };
        }

        private readonly struct StoryNode
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string Status;
            public readonly bool Unlocked;

            private StoryNode(string id, string title, string status, bool unlocked)
            {
                Id = id;
                Title = title;
                Status = status;
                Unlocked = unlocked;
            }

            public static StoryNode Tutorial()
            {
                return new StoryNode(
                    "tutorial",
                    GameLocalization.Text("mahjong.story.chapter.tutorial.title"),
                    GameLocalization.Format("mahjong.story.chapter.stage_count", TutorialStageCount),
                    true);
            }

            public static StoryNode World(bool unlocked)
            {
                return new StoryNode(
                    "world",
                    GameLocalization.Text("mahjong.story.chapter.world.title"),
                    GameLocalization.Text("mahjong.story.chapter.world.status"),
                    unlocked);
            }
        }

        private enum StoryView
        {
            Root,
            TutorialStages,
            WorldBranches,
            CountryBranches,
            StoryLevelStages
        }
    }
}
