using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ChatFirstVisitDialogueUI : MonoBehaviour
    {
        private const string LegacyChatSeenKeyPrefix = "symbiosis.chat_intro_seen.";
        private const string SeenKeyPrefix = "symbiosis.main_intro_seen.";
        private const string BlackYangTexturePath = "Monetization/MainRewardedBonus/BlackYang";
        private const string WhiteYinTexturePath = "Monetization/MainRewardedBonus/WhiteYin";
        private const string ProfileAvatarFrameResourcePath = "ProfileAvatars/ProfileAvatarFrameGenerated";
        private const int OverlaySortingOrder = 32766;

        private static readonly Color FullscreenBlack = new Color(0.002f, 0.003f, 0.008f, 1f);
        private static readonly Color CyanAccent = new Color(0.19f, 0.78f, 0.96f, 1f);
        private static readonly Color VioletAccent = new Color(0.72f, 0.36f, 0.96f, 1f);
        private static readonly Color TextColor = new Color(0.95f, 0.97f, 1f, 1f);
        private static Sprite cachedProfileAvatarFrameSprite;

        private RectTransform root;
        private RectTransform safeAreaRoot;
        private RectTransform stage;
        private RectTransform stageBackground;
        private RectTransform stageFrame;
        private RectTransform titlePlate;
        private RectTransform leftPortraitGroup;
        private RectTransform rightPortraitGroup;
        private RectTransform leftBubble;
        private RectTransform rightBubble;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI blackYangNameText;
        private TextMeshProUGUI whiteYinNameText;
        private TextMeshProUGUI blackYangLineText;
        private TextMeshProUGUI whiteYinLineText;
        private TextMeshProUGUI continueButtonLabel;
        private Button continueButton;
        private Vector2 lastRootSize = new Vector2(-1f, -1f);
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private float nextSafeAreaCheckTime;
        private string activeProfileKey = string.Empty;
        private string activeSeenKey = string.Empty;
        private string activeDialogueId = "chat";
        private string activeTitleKey = "chat.intro.title";
        private string activeBlackLineKey = "chat.intro.black_line";
        private string activeWhiteLineKey = "chat.intro.white_line";
        private string activeContinueKey = "chat.intro.continue";
        private string activeWhiteLineArgumentKey = "chat.channel.developer_support";
        private Action activeCompleteAction;

        public static ChatFirstVisitDialogueUI Ensure(Transform parent)
        {
            if (parent == null)
                return null;

            Transform existing = parent.Find("ChatFirstVisitDialogue");
            if (existing != null)
            {
                ChatFirstVisitDialogueUI existingUi = existing.GetComponent<ChatFirstVisitDialogueUI>();
                if (existingUi != null)
                    return existingUi;
            }

            GameObject host = new GameObject(
                "ChatFirstVisitDialogue",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            host.transform.SetParent(parent, false);
            return host.AddComponent<ChatFirstVisitDialogueUI>();
        }

        public bool TryShowForCurrentProfile()
        {
            return TryShowForCurrentProfile(
                "chat",
                "chat.intro.title",
                "chat.intro.black_line",
                "chat.intro.white_line",
                "chat.intro.continue",
                "chat.channel.developer_support");
        }

        public bool TryShowForCurrentProfile(
            string dialogueId,
            string titleKey,
            string blackLineKey,
            string whiteLineKey,
            string continueKey = "main.intro.continue",
            string whiteLineArgumentKey = null,
            Action onCompleted = null)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return false;

            activeDialogueId = NormalizeDialogueId(dialogueId);
            activeTitleKey = titleKey;
            activeBlackLineKey = blackLineKey;
            activeWhiteLineKey = whiteLineKey;
            activeContinueKey = continueKey;
            activeWhiteLineArgumentKey = whiteLineArgumentKey;
            activeCompleteAction = onCompleted;

            profile.EnsureData();
            string localProfileKey = string.IsNullOrWhiteSpace(profile.LocalProfileId)
                ? "default"
                : profile.LocalProfileId.Trim();
            activeProfileKey = string.IsNullOrWhiteSpace(profile.PublicPlayerId)
                ? localProfileKey
                : profile.PublicPlayerId.Trim();
            string seenKey = BuildSeenKey(activeDialogueId, activeProfileKey);
            bool alreadySeen = PlayerPrefs.GetInt(seenKey, 0) != 0;
            if (!alreadySeen && string.Equals(activeDialogueId, "chat", StringComparison.Ordinal))
            {
                bool legacySeen = PlayerPrefs.GetInt(LegacyChatSeenKeyPrefix + localProfileKey, 0) != 0;
                if (legacySeen)
                {
                    PlayerPrefs.SetInt(seenKey, 1);
                    PlayerPrefs.Save();
                    alreadySeen = true;
                }
            }

            if (alreadySeen)
            {
                activeSeenKey = string.Empty;
                activeCompleteAction = null;
                gameObject.SetActive(false);
                return false;
            }

            activeSeenKey = seenKey;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            RefreshTexts();
            RefreshLayout();
            return true;
        }

        public void HideWithoutCompleting()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void Awake()
        {
            root = transform as RectTransform;
            Stretch(root);

            Canvas canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortingOrder;

            BuildUi();
            RefreshLayout();
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            RefreshTexts();
            RefreshLayout();
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnDestroy()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            if (continueButton != null)
                continueButton.onClick.RemoveListener(Complete);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (root == null || root.rect.size == lastRootSize)
                return;

            RefreshLayout();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSafeAreaCheckTime)
                return;

            nextSafeAreaCheckTime = Time.unscaledTime + 0.5f;
            if (lastSafeArea != Screen.safeArea)
                RefreshLayout();
        }

        private void BuildUi()
        {
            Image backdrop = CreateImage(transform, "BlackBackdrop", FullscreenBlack);
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true;
            Button backdropBlocker = backdrop.gameObject.AddComponent<Button>();
            backdropBlocker.targetGraphic = backdrop;
            backdropBlocker.transition = Selectable.Transition.None;
            backdropBlocker.navigation = new Navigation { mode = Navigation.Mode.None };

            safeAreaRoot = CreateRect(transform, "SafeArea");
            Stretch(safeAreaRoot);
            stage = CreateRect(safeAreaRoot, "ChatIntroductionStage");

            Image backgroundImage = CreateImage(stage, "MainBankBackground", Color.white);
            stageBackground = backgroundImage.rectTransform;
            ApplySprite(backgroundImage, MainLobbyButtonStyle.BankFullscreenBackgroundSprite, Image.Type.Simple, new Color(0.33f, 0.38f, 0.43f, 0.9f));

            Image frameImage = CreateImage(stage, "MainBankWindowFrame", Color.white);
            stageFrame = frameImage.rectTransform;
            ApplySprite(frameImage, MainLobbyButtonStyle.BankWindowFrameSprite, Image.Type.Sliced, Color.white);

            Image titlePlateImage = CreateImage(stage, "ChatIntroductionTitlePlate", Color.white);
            titlePlate = titlePlateImage.rectTransform;
            ApplySprite(titlePlateImage, MainLobbyButtonStyle.BankModuleSprite, Image.Type.Sliced, Color.white);

            titleText = CreateLabel(stage, "Title", string.Empty, 56f, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
            MainLobbyButtonStyle.ApplySilverTextEffect(titleText);

            leftPortraitGroup = CreatePortrait(
                stage,
                "BlackYangPortrait",
                BlackYangTexturePath,
                new Rect(0.069f, 0.002f, 0.862f, 0.958f),
                CyanAccent,
                out blackYangNameText);
            rightPortraitGroup = CreatePortrait(
                stage,
                "WhiteYinPortrait",
                WhiteYinTexturePath,
                new Rect(0.11f, 0.05f, 0.776f, 0.905f),
                VioletAccent,
                out whiteYinNameText);

            leftBubble = CreateSpeechBubble(stage, "BlackYangChatSpeech", CyanAccent, true, out blackYangLineText);
            rightBubble = CreateSpeechBubble(stage, "WhiteYinChatSpeech", VioletAccent, false, out whiteYinLineText);

            continueButton = CreateButton(stage, "ButtonEnterChat", string.Empty, 38f);
            ApplyBankButton(continueButton);
            continueButton.onClick.AddListener(Complete);
            continueButtonLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void Complete()
        {
            Action completed = activeCompleteAction;
            activeCompleteAction = null;
            if (!string.IsNullOrWhiteSpace(activeSeenKey))
            {
                PlayerPrefs.SetInt(activeSeenKey, 1);
                PlayerPrefs.Save();
                activeSeenKey = string.Empty;
            }
            gameObject.SetActive(false);
            completed?.Invoke();
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshTexts();
        }

        private void RefreshTexts()
        {
            SetTextIfChanged(titleText, GameLocalization.Text(activeTitleKey));
            SetTextIfChanged(blackYangNameText, GameLocalization.Text("main.reward_bonus.black_yang"));
            SetTextIfChanged(whiteYinNameText, GameLocalization.Text("main.reward_bonus.white_yin"));
            SetTextIfChanged(blackYangLineText, GameLocalization.Text(activeBlackLineKey));
            string whiteLine = string.IsNullOrWhiteSpace(activeWhiteLineArgumentKey)
                ? GameLocalization.Text(activeWhiteLineKey)
                : GameLocalization.Format(activeWhiteLineKey, GameLocalization.Text(activeWhiteLineArgumentKey));
            SetTextIfChanged(whiteYinLineText, whiteLine);
            SetTextIfChanged(continueButtonLabel, GameLocalization.Text(activeContinueKey));
        }

        private static string NormalizeDialogueId(string dialogueId)
        {
            return string.IsNullOrWhiteSpace(dialogueId)
                ? "window"
                : dialogueId.Trim().ToLowerInvariant();
        }

        private static string BuildSeenKey(string dialogueId, string profileKey)
        {
            if (string.Equals(dialogueId, "chat", StringComparison.Ordinal))
                return LegacyChatSeenKeyPrefix + profileKey;

            return SeenKeyPrefix + dialogueId + "." + profileKey;
        }

        public void RefreshLayout()
        {
            if (root == null)
                root = transform as RectTransform;

            Stretch(root);
            lastRootSize = root != null ? root.rect.size : Vector2.zero;
            lastSafeArea = Screen.safeArea;
            ApplySafeArea(safeAreaRoot, lastRootSize);
            if (safeAreaRoot == null || stage == null)
                return;

            bool portrait = MainLobbyUiCoordinator.IsPortraitLayout(MainLobbyUiCoordinator.ResolveScreenSize());
            Vector2 reference = portrait ? new Vector2(1080f, 1920f) : new Vector2(2400f, 1080f);
            Vector2 available = safeAreaRoot.rect.size;
            float scale = Mathf.Min(
                Mathf.Max(1f, available.x) / reference.x,
                Mathf.Max(1f, available.y) / reference.y);
            SetRect(stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, reference);
            stage.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            if (portrait)
                LayoutPortrait();
            else
                LayoutLandscape();
        }

        private void LayoutLandscape()
        {
            SetRect(stageBackground, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2350f, 1035f));
            SetRect(stageFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2370f, 1050f));
            SetRect(titlePlate, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1120f, 136f));
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(940f, 74f));

            SetRect(leftPortraitGroup, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(82f, 75f), new Vector2(500f, 590f));
            SetRect(rightPortraitGroup, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-82f, 75f), new Vector2(500f, 590f));

            SetRect(leftBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 180f), new Vector2(1120f, 300f));
            SetRect(rightBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, -150f), new Vector2(1120f, 300f));
            SetRect(continueButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(820f, 104f));
        }

        private void LayoutPortrait()
        {
            SetRect(stageBackground, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 1880f));
            SetRect(stageFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 1900f));
            SetRect(titlePlate, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(800f, 140f));
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(660f, 76f));

            SetRect(leftPortraitGroup, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(58f, -190f), new Vector2(460f, 542f));
            SetRect(rightPortraitGroup, Vector2.one, Vector2.one, Vector2.one, new Vector2(-58f, -190f), new Vector2(460f, 542f));

            SetRect(leftBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(900f, 280f));
            SetRect(rightBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(900f, 280f));
            SetRect(continueButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(760f, 104f));
        }

        private static RectTransform CreatePortrait(Transform parent, string objectName, string texturePath, Rect uvRect, Color accent, out TextMeshProUGUI nameLabel)
        {
            RectTransform group = CreateRect(parent, objectName);
            RectTransform portraitHolder = CreateRect(group, "PortraitViewport");
            SetRect(portraitHolder, new Vector2(0.1f, 0.21f), new Vector2(0.9f, 0.89f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            portraitObject.transform.SetParent(portraitHolder, false);
            RawImage portrait = portraitObject.GetComponent<RawImage>();
            Texture2D portraitTexture = Resources.Load<Texture2D>(texturePath);
            portrait.texture = portraitTexture;
            portrait.uvRect = uvRect;
            portrait.raycastTarget = false;
            Stretch(portrait.rectTransform);
            AspectRatioFitter aspect = portraitObject.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = portraitTexture != null && portraitTexture.height > 0
                ? portraitTexture.width * uvRect.width / (portraitTexture.height * uvRect.height)
                : 1f;

            Image frame = CreateImage(group, "ProfileAvatarFrame", Color.white);
            SetRect(frame.rectTransform, new Vector2(0f, 0.15f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ApplySprite(frame, LoadProfileAvatarFrameSprite(), Image.Type.Simple, Color.white);

            Image namePlate = CreateImage(group, "NamePlate", Color.white);
            SetRect(namePlate.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(440f, 96f));
            ApplySprite(namePlate, MainLobbyButtonStyle.BankButtonSprite, Image.Type.Sliced, Color.white);

            nameLabel = CreateLabel(namePlate.transform, "Name", string.Empty, 38f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
            Stretch(nameLabel.rectTransform, 48f, 14f);
            Shadow shadow = nameLabel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return group;
        }

        private static RectTransform CreateSpeechBubble(Transform parent, string objectName, Color accent, bool pointsLeft, out TextMeshProUGUI speechLabel)
        {
            RectTransform bubble = CreateRect(parent, objectName);
            Image image = bubble.gameObject.AddComponent<Image>();
            ApplySprite(image, MainLobbyButtonStyle.BankModuleSprite, Image.Type.Sliced, Color.white);

            Image directionAccent = CreateImage(bubble, "SpeakerAccent", accent);
            SetRect(
                directionAccent.rectTransform,
                new Vector2(pointsLeft ? 0f : 1f, 0.5f),
                new Vector2(pointsLeft ? 0f : 1f, 0.5f),
                new Vector2(pointsLeft ? 0f : 1f, 0.5f),
                new Vector2(pointsLeft ? 48f : -48f, 0f),
                new Vector2(5f, 112f));

            speechLabel = CreateLabel(
                bubble,
                "Speech",
                string.Empty,
                50f,
                FontStyles.Normal,
                TextColor,
                pointsLeft ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);
            speechLabel.fontSizeMin = 30f;
            Stretch(speechLabel.rectTransform, 72f, 42f);
            return bubble;
        }

        private static RectTransform CreateRect(Transform parent, string objectName)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Button CreateButton(Transform parent, string objectName, string text, float fontSize)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            TextMeshProUGUI label = CreateLabel(buttonObject.transform, "Label", text, fontSize, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 32f, 14f);
            return button;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string objectName, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(16f, fontSize * 0.58f);
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            MainLobbyButtonStyle.ApplyFont(label);
            return label;
        }

        private static void ApplyBankButton(Button button)
        {
            if (button == null || button.image == null)
                return;

            ApplySprite(button.image, MainLobbyButtonStyle.BankButtonSprite, Image.Type.Sliced, Color.white);
            button.image.raycastTarget = true;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                MainLobbyButtonStyle.ApplySilverTextEffect(label);
                Stretch(label.rectTransform, 52f, 16f);
            }
        }

        private static void ApplySprite(Image image, Sprite sprite, Image.Type type, Color color)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = sprite != null ? type : Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
            image.raycastTarget = false;
        }

        private static Sprite LoadProfileAvatarFrameSprite()
        {
            if (cachedProfileAvatarFrameSprite != null)
                return cachedProfileAvatarFrameSprite;

            cachedProfileAvatarFrameSprite = Resources.Load<Sprite>(ProfileAvatarFrameResourcePath);
            if (cachedProfileAvatarFrameSprite != null)
                return cachedProfileAvatarFrameSprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(ProfileAvatarFrameResourcePath);
            if (sprites != null && sprites.Length > 0)
                cachedProfileAvatarFrameSprite = sprites[0];
            return cachedProfileAvatarFrameSprite;
        }

        private static void ApplySafeArea(RectTransform rect, Vector2 canvasSize)
        {
            if (rect == null)
                return;

            Stretch(rect);
            Rect safe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0 || safe.width <= 0f || safe.height <= 0f)
                return;

            float scaleX = canvasSize.x / Screen.width;
            float scaleY = canvasSize.y / Screen.height;
            rect.offsetMin = new Vector2(Mathf.Max(0f, safe.xMin) * scaleX, Mathf.Max(0f, safe.yMin) * scaleY);
            rect.offsetMax = new Vector2(-Mathf.Max(0f, Screen.width - safe.xMax) * scaleX, -Mathf.Max(0f, Screen.height - safe.yMax) * scaleY);
        }

        private static void SetTextIfChanged(TMP_Text target, string value)
        {
            if (target != null && !string.Equals(target.text, value, StringComparison.Ordinal))
                target.text = value;
        }

        private static void Stretch(RectTransform rect, float horizontalInset = 0f, float verticalInset = 0f)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(horizontalInset, verticalInset);
            rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }
    }
}
