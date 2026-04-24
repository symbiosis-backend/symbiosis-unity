using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MahjongGame
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ProfileSetupUI : MonoBehaviour
    {
        [Serializable]
        private sealed class LayoutTuningSettings
        {
            [Header("Window")]
            [Range(0.6f, 1f)] public float windowWidthPercent = 0.985f;
            [Range(0.6f, 1f)] public float windowHeightPercent = 0.98f;
            public Vector2 windowMinSize = new Vector2(1180f, 700f);
            public Vector2 windowMaxSize = new Vector2(1700f, 920f);
            [Min(0f)] public float windowPadding = 56f;
            [Min(0f)] public float bodyGap = 24f;
            [Range(0.2f, 0.45f)] public float leftPaneWidthPercent = 0.26f;
            public Vector2 leftPaneWidthRange = new Vector2(300f, 400f);

            [Header("Header")]
            [Min(1f)] public float titleHeight = 62f;
            [Min(1f)] public float subtitleHeight = 36f;
            [Min(0f)] public float titleTopOffset = 86f;
            [Min(0f)] public float subtitleTopOffset = 132f;
            [Min(1f)] public float languageButtonWidth = 64f;
            [Min(1f)] public float languageButtonHeight = 46f;
            [Min(0f)] public float languageButtonGap = 8f;

            [Header("Avatar Pane")]
            [Range(0.3f, 0.8f)] public float avatarCenterYPercent = 0.56f;
            [Range(0.3f, 0.8f)] public float slotPreviewCenterYPercent = 0.62f;
            public Vector2 avatarSizeRange = new Vector2(260f, 400f);
            public Vector2 slotPreviewAvatarSizeRange = new Vector2(250f, 380f);
            [Min(0f)] public float avatarFramePadding = 24f;
            [Min(1f)] public float avatarArrowSize = 70f;
            [Min(0f)] public float avatarArrowGap = 14f;

            [Header("Details Pane")]
            [Min(0f)] public float detailsPaddingX = 18f;
            [Min(0f)] public float tabsGap = 12f;
            [Min(1f)] public float tabHeight = 62f;
            [Min(1f)] public float fieldHeight = 62f;
            [Min(1f)] public float compactFieldHeight = 56f;
            [Min(1f)] public float registerSlotButtonHeight = 76f;
            [Min(1f)] public float loginSlotButtonHeight = 92f;
            [Min(1f)] public float primaryButtonHeight = 62f;
        }

        private const string RussianLanguageButtonResourcePath = "Mahjong/Sprites/RuButton";
        private const string EnglishLanguageButtonResourcePath = "Mahjong/Sprites/EngButton";
        private const string TurkishLanguageButtonResourcePath = "Mahjong/Sprites/TrButton";
        private const string SettingsWindowResourcePath = "Mahjong/Sprites/MainSettings/MainSettingsWindow";
        private const float ProfileWindowSpriteWidth = 1513f;
        private const float ProfileWindowSpriteHeight = 1024f;
        private const float ProfileWindowInnerLeft = 70f / ProfileWindowSpriteWidth;
        private const float ProfileWindowInnerRight = 70f / ProfileWindowSpriteWidth;
        private const float ProfileWindowInnerTop = 88f / ProfileWindowSpriteHeight;
        private const float ProfileWindowInnerBottom = 74f / ProfileWindowSpriteHeight;

        [Header("Links")]
        [SerializeField] private ProfileBootstrap bootstrap;
        [SerializeField] private TMP_InputField dynastyInput;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField ageInput;
        [SerializeField] private Image avatarPreview;
        [SerializeField] private TextMeshProUGUI errorText;

        [Header("Avatar Data")]
        [SerializeField] private Sprite[] avatarSprites;

        [Header("Rules")]
        [SerializeField, Min(1)] private int minNameLength = 2;
        [SerializeField, Min(1)] private int maxNameLength = 16;
        [SerializeField, Min(0)] private int minAge = 0;
        [SerializeField, Min(1)] private int maxAge = 120;
        [SerializeField] private string fallbackPlayerName = "Player";

        [Header("Layout Tuning")]
        [SerializeField] private LayoutTuningSettings layoutTuning = new LayoutTuningSettings();

        [Header("Editor Hierarchy")]
        [SerializeField] private bool generateObjectsInHierarchy = true;
        [SerializeField] private bool autoLayoutInEditor;

        private RectTransform generatedRoot;
        private RectTransform windowRect;
        private RectTransform windowBackgroundRect;
        private RectTransform leftPaneRect;
        private RectTransform rightPaneRect;
        private Image avatarPreviewFrame;
        private Button previousAvatarButton;
        private Button nextAvatarButton;
        private Button russianLanguageButton;
        private Button englishLanguageButton;
        private Button turkishLanguageButton;
        private Button dynastyTabButton;
        private Button profileTabButton;
        private Button slotOneButton;
        private Button slotTwoButton;
        private Button slotThreeButton;
        private Button maleButton;
        private Button femaleButton;
        private Button otherButton;
        private Button continueButton;
        private Button loginButton;
        private Button forgotPasswordButton;
        private Button deleteSlotButton;
        private Button backButton;
        private Toggle rememberToggle;
        private Image slotOneAvatarImage;
        private Image slotTwoAvatarImage;
        private Image slotThreeAvatarImage;
        private TextMeshProUGUI registerStepText;
        private TextMeshProUGUI idPreviewText;
        private TextMeshProUGUI avatarIndexText;
        private TextMeshProUGUI slotProfileNameText;
        private TextMeshProUGUI slotProfileLevelText;
        private TextMeshProUGUI slotProfileAgeText;
        private TextMeshProUGUI slotLabelText;
        private BattleCharacterModelView avatarModelView;
        private PlayerGender selectedGender = PlayerGender.NotSpecified;
        private int currentAvatarIndex;
        private int selectedSlotIndex = 1;
        private bool loginMode;
        private bool loginSlotsLoaded;
        private bool creatingSlotForExistingAccount;
        private string cachedLoginEmail = string.Empty;
        private string cachedLoginPassword = string.Empty;
        private string cachedLoginDynastyName = string.Empty;
        private ProfileService.AccountSlotInfo[] loginSlots = Array.Empty<ProfileService.AccountSlotInfo>();
        private RegisterStep registerStep = RegisterStep.Account;
        private bool continueInProgress;
        private bool loadingRememberedAccountSlots;
        private bool confirmingDeleteSlot;
        private bool sanitizingNameInput;
        private bool subscribedToLanguageChanges;
        private Sprite cachedRussianLanguageButtonSprite;
        private Sprite cachedEnglishLanguageButtonSprite;
        private Sprite cachedTurkishLanguageButtonSprite;
        private static Sprite cachedBuiltinUiSprite;

        private enum RegisterStep
        {
            Account,
            Gender,
            Details
        }

        private void Awake()
        {
            EnsureLandscapeHierarchy();

            if (!Application.isPlaying)
            {
                RefreshLocalizedText();
                if (ShouldApplyEditorLayout())
                    ApplyResponsiveLayout();
                return;
            }

            BindButtons();
            ConfigureInput();
            ApplyAvatarVisual();
            RefreshGenderButtons();
            SetError(string.Empty);
        }

        private void OnEnable()
        {
            EnsureLandscapeHierarchy();

            if (!Application.isPlaying)
            {
                RefreshLocalizedText();
                if (ShouldApplyEditorLayout())
                    ApplyResponsiveLayout();
                return;
            }

            SubscribeLanguageChanges();
            RefreshLocalizedText();
            SetError(string.Empty);
            continueInProgress = false;

            if (continueButton != null)
                continueButton.interactable = true;

            if (loginButton != null)
                loginButton.interactable = true;

            if (nameInput != null)
                nameInput.text = string.Empty;

            if (dynastyInput != null)
            {
                dynastyInput.text = string.Empty;

                if (!Application.isMobilePlatform)
                    dynastyInput.ActivateInputField();
            }

            if (ageInput != null)
                ageInput.text = string.Empty;

            if (emailInput != null)
                emailInput.text = string.Empty;

            if (passwordInput != null)
                passwordInput.text = string.Empty;

            selectedGender = PlayerGender.NotSpecified;
            selectedSlotIndex = 1;
            loginMode = false;
            loginSlotsLoaded = false;
            loadingRememberedAccountSlots = false;
            confirmingDeleteSlot = false;
            creatingSlotForExistingAccount = false;
            cachedLoginEmail = string.Empty;
            cachedLoginPassword = string.Empty;
            cachedLoginDynastyName = string.Empty;
            loginSlots = Array.Empty<ProfileService.AccountSlotInfo>();
            registerStep = RegisterStep.Account;
            currentAvatarIndex = Mathf.Clamp(currentAvatarIndex, 0, GetLastAvatarIndex());
            if (rememberToggle != null)
                rememberToggle.isOn = ProfileService.I == null || !ProfileService.I.HasProfile() || ProfileService.I.RememberProfile;

            if (TryStartRememberedAccountSlotPicker())
                return;

            RefreshGenderButtons();
            RefreshSlotButtons();
            RefreshTabButtons();
            ApplyAvatarVisual();
            ApplyResponsiveLayout();
        }

        private bool TryStartRememberedAccountSlotPicker()
        {
            if (ProfileService.I == null)
                return false;

            if (!ProfileService.I.TryGetRememberedAccountCredentials(out string rememberedEmail, out string rememberedPassword))
                return false;

            loginMode = true;
            loginSlotsLoaded = false;
            loadingRememberedAccountSlots = true;
            confirmingDeleteSlot = false;
            creatingSlotForExistingAccount = false;
            registerStep = RegisterStep.Account;
            cachedLoginEmail = rememberedEmail;
            cachedLoginPassword = rememberedPassword;

            if (emailInput != null)
                emailInput.SetTextWithoutNotify(rememberedEmail);

            if (passwordInput != null)
                passwordInput.SetTextWithoutNotify(rememberedPassword);

            if (rememberToggle != null)
                rememberToggle.isOn = true;

            RefreshGenderButtons();
            RefreshSlotButtons();
            RefreshTabButtons();
            ApplyAvatarVisual();
            ApplyResponsiveLayout();

            continueInProgress = true;
            SetAccountButtonsInteractable(false);
            StartCoroutine(LoadAccountSlotsAndShow(rememberedEmail, rememberedPassword));
            return true;
        }

        private void OnDestroy()
        {
            ReleaseActiveInputs();
            UnsubscribeLanguageChanges();
            UnbindButtons();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            ReleaseActiveInputs();
            UnsubscribeLanguageChanges();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying && !ShouldApplyEditorLayout())
                return;

            ApplyResponsiveLayout();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            EnsureLandscapeHierarchy();
            RefreshLocalizedText();
            if (ShouldApplyEditorLayout())
                ApplyResponsiveLayout();
        }

        private void EnsureLandscapeHierarchy()
        {
            if (!generateObjectsInHierarchy && !Application.isPlaying)
                return;

            RestoreGeneratedReferences();
            if (generatedRoot == null)
            {
                BuildLandscapeProfileWindow();
                MarkEditorHierarchyDirty();
            }
        }

        private bool ShouldApplyEditorLayout()
        {
            return autoLayoutInEditor || NeedsInitialEditorLayout();
        }

        private bool NeedsInitialEditorLayout()
        {
            if (windowRect == null)
                return true;

            return windowRect.sizeDelta.x < 64f || windowRect.sizeDelta.y < 64f;
        }

        [ContextMenu("Regenerate Profile Setup Hierarchy")]
        private void RegenerateProfileSetupHierarchy()
        {
            if (Application.isPlaying)
                return;

            RestoreGeneratedReferences();
            if (generatedRoot != null)
                DestroyImmediate(generatedRoot.gameObject);

            generatedRoot = null;
            windowRect = null;
            windowBackgroundRect = null;
            leftPaneRect = null;
            rightPaneRect = null;

            BuildLandscapeProfileWindow();
            RefreshLocalizedText();
            ApplyResponsiveLayout();
            MarkEditorHierarchyDirty();
        }

        [ContextMenu("Apply Profile Setup Auto Layout")]
        private void ApplyProfileSetupAutoLayout()
        {
            EnsureLandscapeHierarchy();
            RefreshLocalizedText();
            ApplyResponsiveLayout();
            MarkEditorHierarchyDirty();
        }

        private void RestoreGeneratedReferences()
        {
            generatedRoot = transform.Find("ProfileSetupLandscapeRoot") as RectTransform;
            if (generatedRoot == null)
                return;

            windowRect = generatedRoot.Find("ProfileSetupWindow") as RectTransform;
            if (windowRect == null)
                return;

            Image windowImage = windowRect.GetComponent<Image>();
            if (windowImage != null)
            {
                MainLobbyButtonStyle.ApplyProfileWindow(windowImage);
                windowImage.raycastTarget = false;
            }

            windowBackgroundRect = windowRect.Find("WindowBackground") as RectTransform;
            Image windowBackgroundImage = windowBackgroundRect != null ? windowBackgroundRect.GetComponent<Image>() : null;
            if (windowBackgroundImage != null)
            {
                windowBackgroundImage.sprite = null;
                windowBackgroundImage.color = Color.clear;
                windowBackgroundImage.raycastTarget = false;
            }

            leftPaneRect = windowRect.Find("AvatarPane") as RectTransform;
            rightPaneRect = windowRect.Find("DetailsPane") as RectTransform;
            MakePaneTransparent(leftPaneRect);
            MakePaneTransparent(rightPaneRect);

            avatarPreview = FindComponentByPath<Image>(leftPaneRect, "AvatarPreview");
            avatarPreviewFrame = FindComponentByPath<Image>(leftPaneRect, "AvatarFrame");
            previousAvatarButton = FindComponentByPath<Button>(leftPaneRect, "PreviousAvatarButton");
            nextAvatarButton = FindComponentByPath<Button>(leftPaneRect, "NextAvatarButton");
            avatarIndexText = FindComponentByPath<TextMeshProUGUI>(leftPaneRect, "AvatarCounter");
            slotProfileNameText = FindComponentByPath<TextMeshProUGUI>(leftPaneRect, "SlotProfileName");
            slotProfileLevelText = FindComponentByPath<TextMeshProUGUI>(leftPaneRect, "SlotProfileLevel");
            slotProfileAgeText = FindComponentByPath<TextMeshProUGUI>(leftPaneRect, "SlotProfileAge");

            russianLanguageButton = FindComponentByPath<Button>(windowRect, "LanguageRuButton");
            englishLanguageButton = FindComponentByPath<Button>(windowRect, "LanguageEnButton");
            turkishLanguageButton = FindComponentByPath<Button>(windowRect, "LanguageTrButton");

            dynastyTabButton = FindComponentByPath<Button>(rightPaneRect, "RegisterTabButton");
            profileTabButton = FindComponentByPath<Button>(rightPaneRect, "LoginTabButton");
            registerStepText = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "RegisterStepText");
            idPreviewText = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "IdPreview");

            dynastyInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "DynastyInput");
            nameInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "NameInput");
            emailInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "EmailInput");
            passwordInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "PasswordInput");
            ageInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "AgeInput");

            rememberToggle = FindComponentByPath<Toggle>(rightPaneRect, "RememberProfileToggle");
            slotLabelText = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "SlotLabel");
            slotOneButton = FindComponentByPath<Button>(rightPaneRect, "SlotOneButton");
            slotTwoButton = FindComponentByPath<Button>(rightPaneRect, "SlotTwoButton");
            slotThreeButton = FindComponentByPath<Button>(rightPaneRect, "SlotThreeButton");
            slotOneAvatarImage = FindComponentByPath<Image>(slotOneButton != null ? slotOneButton.transform : null, "SlotOneAvatar");
            slotTwoAvatarImage = FindComponentByPath<Image>(slotTwoButton != null ? slotTwoButton.transform : null, "SlotTwoAvatar");
            slotThreeAvatarImage = FindComponentByPath<Image>(slotThreeButton != null ? slotThreeButton.transform : null, "SlotThreeAvatar");
            maleButton = FindComponentByPath<Button>(rightPaneRect, "MaleButton");
            femaleButton = FindComponentByPath<Button>(rightPaneRect, "FemaleButton");
            otherButton = FindComponentByPath<Button>(rightPaneRect, "OtherButton");
            continueButton = FindComponentByPath<Button>(rightPaneRect, "ContinueButton");
            loginButton = FindComponentByPath<Button>(rightPaneRect, "LoginButton");
            forgotPasswordButton = FindComponentByPath<Button>(rightPaneRect, "ForgotPasswordButton");
            deleteSlotButton = FindComponentByPath<Button>(rightPaneRect, "DeleteSlotButton");
            backButton = FindComponentByPath<Button>(rightPaneRect, "BackButton");
            errorText = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "ErrorText");
        }

        private static T FindComponentByPath<T>(Transform root, string path) where T : Component
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            Transform child = root.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void MarkEditorHierarchyDirty()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            EditorUtility.SetDirty(gameObject);
            if (generatedRoot != null)
                EditorUtility.SetDirty(generatedRoot.gameObject);

            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        private void SubscribeLanguageChanges()
        {
            if (subscribedToLanguageChanges)
                return;

            AppSettings.OnLanguageChanged += OnLanguageChanged;
            subscribedToLanguageChanges = true;
        }

        private void UnsubscribeLanguageChanges()
        {
            if (!subscribedToLanguageChanges)
                return;

            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            subscribedToLanguageChanges = false;
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            RefreshLocalizedText();
            ApplyResponsiveLayout();
        }

        private void SelectLanguage(GameLanguage language)
        {
            if (AppSettings.I != null)
                AppSettings.I.SetLanguage(language);

            RefreshLocalizedText();
            ApplyResponsiveLayout();
        }

        private void RefreshLocalizedText()
        {
            SetNamedText(windowRect, "Title", ProfileTitleText());
            SetNamedText(windowRect, "Subtitle", ProfileSubtitleText());
            SetNamedText(leftPaneRect, "AvatarTitle", AvatarText());
            SetNamedText(rightPaneRect, "GenderLabel", GenderText());
            SetNamedText(rightPaneRect, "SlotLabel", ProfileSlotText());

            if (idPreviewText != null)
                idPreviewText.text = AutoIdText();

            SetInputPlaceholder(dynastyInput, DynastyNameText());
            SetInputPlaceholder(nameInput, NicknameText());
            SetInputPlaceholder(emailInput, EmailText());
            SetInputPlaceholder(passwordInput, PasswordText());
            SetInputPlaceholder(ageInput, AgeInputText());
            SetToggleLabel(rememberToggle, RememberProfileText());
            SetButtonLabel(russianLanguageButton, "RU");
            SetButtonLabel(englishLanguageButton, "EN");
            SetButtonLabel(turkishLanguageButton, "TR");
            SetButtonLabel(dynastyTabButton, RegisterText());
            SetButtonLabel(profileTabButton, LoginText());
            SetButtonLabel(maleButton, MaleText());
            SetButtonLabel(femaleButton, FemaleText());
            SetButtonLabel(otherButton, OtherText());

            RefreshLanguageButtons();
            RefreshGenderButtons();
            RefreshSlotButtons();
            RefreshTabButtons();
        }

        private void BuildLandscapeProfileWindow()
        {
            RestoreGeneratedReferences();
            if (generatedRoot != null)
                return;

            HideLegacyChildren();

            GameObject root = new GameObject("ProfileSetupLandscapeRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(transform, false);
            root.transform.SetAsLastSibling();
            generatedRoot = root.GetComponent<RectTransform>();
            Stretch(generatedRoot);

            Image rootImage = root.GetComponent<Image>();
            rootImage.sprite = LoadBuiltinUiSprite();
            rootImage.type = Image.Type.Simple;
            rootImage.preserveAspect = false;
            rootImage.color = new Color(0.025f, 0.035f, 0.055f, 0.94f);
            rootImage.raycastTarget = true;

            GameObject window = new GameObject("ProfileSetupWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            window.transform.SetParent(root.transform, false);
            windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;

            Image windowImage = window.GetComponent<Image>();
            MainLobbyButtonStyle.ApplyProfileWindow(windowImage);
            windowImage.raycastTarget = false;

            GameObject windowBackground = new GameObject("WindowBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            windowBackground.transform.SetParent(window.transform, false);
            windowBackground.transform.SetAsFirstSibling();
            windowBackgroundRect = windowBackground.GetComponent<RectTransform>();
            Stretch(windowBackgroundRect);
            Image windowBackgroundImage = windowBackground.GetComponent<Image>();
            windowBackgroundImage.sprite = null;
            windowBackgroundImage.type = Image.Type.Simple;
            windowBackgroundImage.preserveAspect = false;
            windowBackgroundImage.color = Color.clear;
            windowBackgroundImage.raycastTarget = false;

            TextMeshProUGUI title = CreateText(window.transform, "Title", ProfileTitleText(), 46f, FontStyles.Bold, Color.white);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            MainLobbyButtonStyle.ApplyFont(title);
            MainLobbyButtonStyle.ApplySilverTextEffect(title);

            TextMeshProUGUI subtitle = CreateText(window.transform, "Subtitle", ProfileSubtitleText(), 24f, FontStyles.Normal, new Color(0.76f, 0.84f, 0.94f, 1f));
            Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            MainLobbyButtonStyle.ApplyFont(subtitle);

            russianLanguageButton = CreateButton(window.transform, "LanguageRuButton", "RU", 20f);
            englishLanguageButton = CreateButton(window.transform, "LanguageEnButton", "EN", 20f);
            turkishLanguageButton = CreateButton(window.transform, "LanguageTrButton", "TR", 20f);
            ApplyLanguageButtonSprites();

            GameObject leftPane = CreatePane(window.transform, "AvatarPane", new Color(0.115f, 0.14f, 0.185f, 1f));
            leftPaneRect = leftPane.GetComponent<RectTransform>();

            TextMeshProUGUI avatarTitle = CreateText(leftPane.transform, "AvatarTitle", AvatarText(), 30f, FontStyles.Bold, Color.white);
            Anchor(avatarTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            avatarPreview = CreateImage(leftPane.transform, "AvatarPreview", Color.white);
            avatarPreview.preserveAspect = true;
            avatarPreviewFrame = CreateImage(leftPane.transform, "AvatarFrame", Color.white);
            MainLobbyButtonStyle.ApplyAvatarCard(avatarPreviewFrame);
            avatarPreviewFrame.raycastTarget = false;

            previousAvatarButton = CreateButton(leftPane.transform, "PreviousAvatarButton", "<", 34f);
            nextAvatarButton = CreateButton(leftPane.transform, "NextAvatarButton", ">", 34f);

            avatarIndexText = CreateText(leftPane.transform, "AvatarCounter", string.Empty, 22f, FontStyles.Normal, new Color(0.78f, 0.84f, 0.94f, 1f));
            Anchor(avatarIndexText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            slotProfileNameText = CreateText(leftPane.transform, "SlotProfileName", string.Empty, 30f, FontStyles.Bold, Color.white);
            Anchor(slotProfileNameText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            slotProfileLevelText = CreateText(leftPane.transform, "SlotProfileLevel", string.Empty, 24f, FontStyles.Bold, new Color(0.78f, 0.86f, 1f, 1f));
            Anchor(slotProfileLevelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            slotProfileAgeText = CreateText(leftPane.transform, "SlotProfileAge", string.Empty, 22f, FontStyles.Normal, new Color(0.72f, 0.80f, 0.92f, 1f));
            Anchor(slotProfileAgeText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            GameObject rightPane = CreatePane(window.transform, "DetailsPane", new Color(0.095f, 0.115f, 0.155f, 1f));
            rightPaneRect = rightPane.GetComponent<RectTransform>();

            dynastyTabButton = CreateButton(rightPane.transform, "RegisterTabButton", RegisterText(), 22f);
            profileTabButton = CreateButton(rightPane.transform, "LoginTabButton", LoginText(), 22f);

            registerStepText = CreateText(rightPane.transform, "RegisterStepText", string.Empty, 22f, FontStyles.Bold, new Color(0.78f, 0.86f, 1f));
            Anchor(registerStepText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            idPreviewText = CreateText(rightPane.transform, "IdPreview", AutoIdText(), 22f, FontStyles.Normal, new Color(0.68f, 0.78f, 0.92f, 1f));
            Anchor(idPreviewText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            dynastyInput = CreateInputField(rightPane.transform, "DynastyInput", DynastyNameText());
            nameInput = CreateInputField(rightPane.transform, "NameInput", NicknameText());
            emailInput = CreateInputField(rightPane.transform, "EmailInput", EmailText());
            emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
            emailInput.keyboardType = TouchScreenKeyboardType.EmailAddress;
            emailInput.characterLimit = 64;

            passwordInput = CreateInputField(rightPane.transform, "PasswordInput", PasswordText());
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.characterLimit = 64;

            rememberToggle = CreateToggle(rightPane.transform, "RememberProfileToggle", RememberProfileText());
            rememberToggle.isOn = true;

            ageInput = CreateInputField(rightPane.transform, "AgeInput", AgeInputText());
            ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            ageInput.characterLimit = 3;

            slotLabelText = CreateText(rightPane.transform, "SlotLabel", ProfileSlotText(), 24f, FontStyles.Bold, Color.white);
            Anchor(slotLabelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            slotOneButton = CreateButton(rightPane.transform, "SlotOneButton", "1", 24f);
            slotTwoButton = CreateButton(rightPane.transform, "SlotTwoButton", "2", 24f);
            slotThreeButton = CreateButton(rightPane.transform, "SlotThreeButton", "3", 24f);
            slotOneAvatarImage = CreateSlotAvatarImage(slotOneButton.transform, "SlotOneAvatar");
            slotTwoAvatarImage = CreateSlotAvatarImage(slotTwoButton.transform, "SlotTwoAvatar");
            slotThreeAvatarImage = CreateSlotAvatarImage(slotThreeButton.transform, "SlotThreeAvatar");

            TextMeshProUGUI genderLabel = CreateText(rightPane.transform, "GenderLabel", GenderText(), 24f, FontStyles.Bold, Color.white);
            Anchor(genderLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));

            maleButton = CreateButton(rightPane.transform, "MaleButton", MaleText(), 22f);
            femaleButton = CreateButton(rightPane.transform, "FemaleButton", FemaleText(), 22f);
            otherButton = CreateButton(rightPane.transform, "OtherButton", OtherText(), 22f);

            errorText = CreateText(rightPane.transform, "ErrorText", string.Empty, 22f, FontStyles.Bold, new Color(1f, 0.48f, 0.42f, 1f));
            Anchor(errorText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            continueButton = CreateButton(rightPane.transform, "ContinueButton", ContinueText(), 24f);
            loginButton = CreateButton(rightPane.transform, "LoginButton", LoginText(), 24f);
            forgotPasswordButton = CreateButton(rightPane.transform, "ForgotPasswordButton", ForgotPasswordText(), 20f);
            deleteSlotButton = CreateButton(rightPane.transform, "DeleteSlotButton", DeleteProfileText(), 22f);
            backButton = CreateButton(rightPane.transform, "BackButton", BackText(), 24f);

            RefreshTabButtons();
            RefreshSlotButtons();
            ApplyResponsiveLayout();
        }

        private void BindButtons()
        {
            if (previousAvatarButton != null)
                previousAvatarButton.onClick.AddListener(OnClickLeft);

            if (nextAvatarButton != null)
                nextAvatarButton.onClick.AddListener(OnClickRight);

            if (russianLanguageButton != null)
                russianLanguageButton.onClick.AddListener(() => SelectLanguage(GameLanguage.Russian));

            if (englishLanguageButton != null)
                englishLanguageButton.onClick.AddListener(() => SelectLanguage(GameLanguage.English));

            if (turkishLanguageButton != null)
                turkishLanguageButton.onClick.AddListener(() => SelectLanguage(GameLanguage.Turkish));

            if (dynastyTabButton != null)
                dynastyTabButton.onClick.AddListener(ShowRegisterMode);

            if (profileTabButton != null)
                profileTabButton.onClick.AddListener(ShowLoginMode);

            if (slotOneButton != null)
                slotOneButton.onClick.AddListener(() => SelectSlot(1));

            if (slotTwoButton != null)
                slotTwoButton.onClick.AddListener(() => SelectSlot(2));

            if (slotThreeButton != null)
                slotThreeButton.onClick.AddListener(() => SelectSlot(3));

            if (maleButton != null)
                maleButton.onClick.AddListener(() => SelectGender(PlayerGender.Male));

            if (femaleButton != null)
                femaleButton.onClick.AddListener(() => SelectGender(PlayerGender.Female));

            if (otherButton != null)
                otherButton.onClick.AddListener(() => SelectGender(PlayerGender.Other));

            if (continueButton != null)
                continueButton.onClick.AddListener(OnClickContinue);

            if (loginButton != null)
                loginButton.onClick.AddListener(OnClickLogin);

            if (forgotPasswordButton != null)
                forgotPasswordButton.onClick.AddListener(OnClickForgotPassword);

            if (deleteSlotButton != null)
                deleteSlotButton.onClick.AddListener(OnClickDeleteSlot);

            if (backButton != null)
                backButton.onClick.AddListener(OnClickBack);

            if (nameInput != null)
                nameInput.onValueChanged.AddListener(SanitizeNameInput);
        }

        private void UnbindButtons()
        {
            if (previousAvatarButton != null)
                previousAvatarButton.onClick.RemoveListener(OnClickLeft);

            if (nextAvatarButton != null)
                nextAvatarButton.onClick.RemoveListener(OnClickRight);

            if (russianLanguageButton != null)
                russianLanguageButton.onClick.RemoveAllListeners();

            if (englishLanguageButton != null)
                englishLanguageButton.onClick.RemoveAllListeners();

            if (turkishLanguageButton != null)
                turkishLanguageButton.onClick.RemoveAllListeners();

            if (dynastyTabButton != null)
                dynastyTabButton.onClick.RemoveListener(ShowRegisterMode);

            if (profileTabButton != null)
                profileTabButton.onClick.RemoveListener(ShowLoginMode);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnClickContinue);

            if (loginButton != null)
                loginButton.onClick.RemoveListener(OnClickLogin);

            if (forgotPasswordButton != null)
                forgotPasswordButton.onClick.RemoveListener(OnClickForgotPassword);

            if (deleteSlotButton != null)
                deleteSlotButton.onClick.RemoveListener(OnClickDeleteSlot);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnClickBack);

            if (nameInput != null)
                nameInput.onValueChanged.RemoveListener(SanitizeNameInput);
        }

        private void SelectGender(PlayerGender gender)
        {
            selectedGender = gender;
            currentAvatarIndex = 0;
            RefreshGenderButtons();
            ApplyAvatarVisual();
            SetError(string.Empty);
        }

        private void SelectSlot(int slotIndex)
        {
            int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, 3);
            if (creatingSlotForExistingAccount && loginSlotsLoaded && GetSlotInfo(clampedSlotIndex).Occupied)
            {
                SetError(SlotOccupiedText());
                return;
            }

            confirmingDeleteSlot = false;
            selectedSlotIndex = clampedSlotIndex;
            RefreshSlotButtons();
            RefreshTabButtons();
            SetError(string.Empty);

            if (loginMode && loginSlotsLoaded && IsSelectedLoginSlotInUseByOtherDevice())
            {
                SetError(ProfileInUseText());
                return;
            }

            if (loginMode && loginSlotsLoaded && !IsSelectedLoginSlotOccupied())
                StartCreateProfileInSelectedLoginSlot();
        }

        private void ShowRegisterMode()
        {
            loginMode = false;
            loginSlotsLoaded = false;
            loadingRememberedAccountSlots = false;
            confirmingDeleteSlot = false;
            creatingSlotForExistingAccount = false;
            registerStep = RegisterStep.Account;
            RefreshTabButtons();
            ApplyResponsiveLayout();
        }

        private void ShowLoginMode()
        {
            loginMode = true;
            loginSlotsLoaded = false;
            loadingRememberedAccountSlots = false;
            confirmingDeleteSlot = false;
            creatingSlotForExistingAccount = false;
            registerStep = RegisterStep.Account;
            RefreshTabButtons();
            ApplyResponsiveLayout();
        }

        private void ShowRegisterStep(RegisterStep step)
        {
            registerStep = step;
            RefreshTabButtons();
            ApplyAvatarVisual();
            ApplyResponsiveLayout();
        }

        private void OnClickLeft()
        {
            int avatarCount = GetAvatarCount();
            if (avatarCount <= 0)
            {
                SetError(NoAvatarsText());
                return;
            }

            currentAvatarIndex = currentAvatarIndex <= 0 ? avatarCount - 1 : currentAvatarIndex - 1;
            ApplyAvatarVisual();
            SetError(string.Empty);
        }

        private void OnClickRight()
        {
            int avatarCount = GetAvatarCount();
            if (avatarCount <= 0)
            {
                SetError(NoAvatarsText());
                return;
            }

            currentAvatarIndex = currentAvatarIndex >= avatarCount - 1 ? 0 : currentAvatarIndex + 1;
            ApplyAvatarVisual();
            SetError(string.Empty);
        }

        private void OnClickContinue()
        {
            if (continueInProgress)
                return;

            try
            {
                continueInProgress = true;
                if (continueButton != null)
                    continueButton.interactable = false;

                ProfileBootstrap.LogRuntime("ProfileSetup continue clicked");
                ReleaseActiveInputs();

                if (ProfileService.I == null)
                {
                    SetError(GameLocalization.Text("profile.error.service_missing"));
                    ProfileBootstrap.LogRuntime("ProfileService missing on continue");
                    ResetContinueState();
                    return;
                }

                if (loginMode)
                {
                    ResetContinueState();
                    StartLoginFlow();
                    return;
                }

                string validatedDynastyName = creatingSlotForExistingAccount
                    ? cachedLoginDynastyName
                    : ValidateAndNormalizeDynastyName(dynastyInput != null ? dynastyInput.text : string.Empty);
                string validatedEmail = creatingSlotForExistingAccount
                    ? cachedLoginEmail
                    : ValidateAndNormalizeEmail(emailInput != null ? emailInput.text : string.Empty);
                string validatedPassword = creatingSlotForExistingAccount
                    ? cachedLoginPassword
                    : ValidatePassword(passwordInput != null ? passwordInput.text : string.Empty);
                if (validatedDynastyName == null || validatedEmail == null || validatedPassword == null)
                {
                    ResetContinueState();
                    return;
                }

                if (registerStep == RegisterStep.Account)
                {
                    ShowRegisterStep(RegisterStep.Gender);
                    ResetContinueState();
                    return;
                }

                if (registerStep == RegisterStep.Gender)
                {
                    if (selectedGender != PlayerGender.Male && selectedGender != PlayerGender.Female)
                    {
                        SetError(ChooseMaleOrFemaleText());
                        ResetContinueState();
                        return;
                    }

                    ShowRegisterStep(RegisterStep.Details);
                    ResetContinueState();
                    return;
                }

                string validatedName = ValidateAndNormalizeName(nameInput != null ? nameInput.text : string.Empty);
                if (validatedName == null)
                {
                    ResetContinueState();
                    return;
                }

                if (!TryValidateAge(ageInput != null ? ageInput.text : string.Empty, out int age))
                {
                    ResetContinueState();
                    return;
                }

                int avatarId = GetSelectedAvatarId();

                if (bootstrap == null)
                    bootstrap = FindAnyObjectByType<ProfileBootstrap>();

                if (bootstrap == null)
                {
                    SetError(GameLocalization.Text("profile.error.bootstrap_missing"));
                    ProfileBootstrap.LogRuntime("Bootstrap missing after profile complete");
                    ResetContinueState();
                    return;
                }

                ProfileBootstrap.LogRuntime($"CompleteProfileOnServer start. Avatar={avatarId}, Age={age}, Gender={selectedGender}");
                StartCoroutine(CompleteProfileOnServerAndContinue(validatedDynastyName, validatedEmail, validatedPassword, validatedName, avatarId, age, ShouldRememberProfile()));
            }
            catch (Exception ex)
            {
                ProfileBootstrap.LogRuntime("ProfileSetup continue exception: " + ex);
                Debug.LogError("[ProfileSetupUI] Continue failed: " + ex);
                SetError(ServerErrorText());
                ResetContinueState();
            }
        }

        private void OnClickLogin()
        {
            if (confirmingDeleteSlot)
            {
                StartDeleteSelectedSlotFlow();
                return;
            }

            if (!loginMode || !loginSlotsLoaded)
                ShowLoginMode();

            StartLoginFlow();
        }

        private void OnClickForgotPassword()
        {
            if (continueInProgress)
                return;

            try
            {
                continueInProgress = true;
                SetAccountButtonsInteractable(false);
                ReleaseActiveInputs();

                if (ProfileService.I == null)
                {
                    SetError(GameLocalization.Text("profile.error.service_missing"));
                    ResetContinueState();
                    return;
                }

                string validatedEmail = ValidateAndNormalizeEmail(emailInput != null ? emailInput.text : string.Empty);
                if (validatedEmail == null)
                {
                    ResetContinueState();
                    return;
                }

                StartCoroutine(RequestPasswordRecoveryAndShow(validatedEmail));
            }
            catch (Exception ex)
            {
                ProfileBootstrap.LogRuntime("ProfileSetup password recovery exception: " + ex);
                Debug.LogError("[ProfileSetupUI] Password recovery failed: " + ex);
                SetError(PasswordRecoveryFailedText());
                ResetContinueState();
            }
        }

        private void OnClickDeleteSlot()
        {
            if (continueInProgress)
                return;

            if (!loginMode || !loginSlotsLoaded || !IsSelectedLoginSlotOccupied())
                return;

            if (IsSelectedLoginSlotInUseByOtherDevice())
            {
                SetError(ProfileInUseText());
                return;
            }

            confirmingDeleteSlot = true;
            if (passwordInput != null)
                passwordInput.text = string.Empty;

            RefreshTabButtons();
            ApplyResponsiveLayout();
            SetError(EnterPasswordToDeleteText());
        }

        private void OnClickBack()
        {
            if (continueInProgress)
                return;

            ReleaseActiveInputs();
            SetError(string.Empty);

            if (confirmingDeleteSlot)
            {
                confirmingDeleteSlot = false;
                if (passwordInput != null)
                    passwordInput.text = cachedLoginPassword;

                RefreshTabButtons();
                ApplyResponsiveLayout();
                return;
            }

            if (loginMode)
            {
                if (loginSlotsLoaded)
                {
                    loginSlotsLoaded = false;
                    confirmingDeleteSlot = false;
                    loginSlots = Array.Empty<ProfileService.AccountSlotInfo>();
                    selectedSlotIndex = 1;
                    RefreshSlotButtons();
                    RefreshTabButtons();
                    ApplyResponsiveLayout();
                    return;
                }

                ShowRegisterMode();
                return;
            }

            if (creatingSlotForExistingAccount)
            {
                if (registerStep == RegisterStep.Details)
                {
                    ShowRegisterStep(RegisterStep.Gender);
                    return;
                }

                loginMode = true;
                creatingSlotForExistingAccount = false;
                registerStep = RegisterStep.Account;
                loginSlotsLoaded = true;
                RefreshSlotButtons();
                RefreshTabButtons();
                ApplyResponsiveLayout();
                return;
            }

            if (registerStep == RegisterStep.Details)
            {
                ShowRegisterStep(RegisterStep.Gender);
                return;
            }

            if (registerStep == RegisterStep.Gender)
            {
                ShowRegisterStep(RegisterStep.Account);
                return;
            }

            ShowLoginMode();
        }

        private void StartLoginFlow()
        {
            if (continueInProgress)
                return;

            try
            {
                continueInProgress = true;
                SetAccountButtonsInteractable(false);
                ReleaseActiveInputs();

                if (ProfileService.I == null)
                {
                    SetError(GameLocalization.Text("profile.error.service_missing"));
                    ResetContinueState();
                    return;
                }

                string validatedEmail = ValidateAndNormalizeEmail(emailInput != null ? emailInput.text : string.Empty);
                string validatedPassword = ValidatePassword(passwordInput != null ? passwordInput.text : string.Empty);
                if (validatedEmail == null || validatedPassword == null)
                {
                    ResetContinueState();
                    return;
                }

                cachedLoginEmail = validatedEmail;
                cachedLoginPassword = validatedPassword;

                if (bootstrap == null)
                    bootstrap = FindAnyObjectByType<ProfileBootstrap>();

                if (bootstrap == null)
                {
                    SetError(GameLocalization.Text("profile.error.bootstrap_missing"));
                    ResetContinueState();
                    return;
                }

                if (!loginSlotsLoaded)
                    StartCoroutine(LoadAccountSlotsAndShow(validatedEmail, validatedPassword));
                else if (IsSelectedLoginSlotInUseByOtherDevice())
                {
                    SetError(ProfileInUseText());
                    ResetContinueState();
                }
                else if (IsSelectedLoginSlotOccupied())
                    StartCoroutine(LoginOnServerAndContinue(validatedEmail, validatedPassword, ShouldRememberProfile()));
                else
                    StartCreateProfileInSelectedLoginSlot();
            }
            catch (Exception ex)
            {
                ProfileBootstrap.LogRuntime("ProfileSetup login exception: " + ex);
                Debug.LogError("[ProfileSetupUI] Login failed: " + ex);
                SetError(LoginFailedText());
                ResetContinueState();
            }
        }

        private void StartDeleteSelectedSlotFlow()
        {
            if (continueInProgress)
                return;

            try
            {
                continueInProgress = true;
                SetAccountButtonsInteractable(false);
                ReleaseActiveInputs();

                if (ProfileService.I == null)
                {
                    SetError(GameLocalization.Text("profile.error.service_missing"));
                    ResetContinueState();
                    return;
                }

                if (!loginSlotsLoaded || !IsSelectedLoginSlotOccupied())
                {
                    SetError(ChooseOccupiedSlotText());
                    ResetContinueState();
                    return;
                }

                if (IsSelectedLoginSlotInUseByOtherDevice())
                {
                    SetError(ProfileInUseText());
                    ResetContinueState();
                    return;
                }

                string validatedPassword = ValidatePassword(passwordInput != null ? passwordInput.text : string.Empty);
                if (validatedPassword == null)
                {
                    ResetContinueState();
                    return;
                }

                StartCoroutine(DeleteSelectedSlotAndRefresh(cachedLoginEmail, validatedPassword));
            }
            catch (Exception ex)
            {
                ProfileBootstrap.LogRuntime("ProfileSetup delete slot exception: " + ex);
                Debug.LogError("[ProfileSetupUI] Delete slot failed: " + ex);
                SetError(DeleteFailedText());
                ResetContinueState();
            }
        }

        private System.Collections.IEnumerator CompleteProfileOnServerAndContinue(string dynastyName, string email, string password, string name, int avatarId, int age, bool rememberProfile)
        {
            bool ok = false;
            string error = string.Empty;
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;

            yield return ProfileService.I.CompleteProfileOnServer(
                dynastyName,
                email,
                password,
                name,
                selectedSlotIndex,
                avatarId,
                age,
                selectedGender,
                language,
                rememberProfile,
                (success, message) =>
                {
                    ok = success;
                    error = message;
                }
            );

            if (!ok)
            {
                ProfileBootstrap.LogRuntime("CompleteProfileOnServer failed: " + error);
                SetError(FormatRegisterError(error));
                ResetContinueState();
                yield break;
            }

            ProfileBootstrap.LogRuntime("CompleteProfileOnServer done");
            yield return ContinueAfterInputSettles();
        }

        private System.Collections.IEnumerator LoadAccountSlotsAndShow(string email, string password)
        {
            bool ok = false;
            string error = string.Empty;
            ProfileService.AccountSlotInfo[] slots = Array.Empty<ProfileService.AccountSlotInfo>();
            string dynastyName = string.Empty;

            yield return ProfileService.I.LoadAccountSlotsOnServer(
                email,
                password,
                (success, message, accountSlots, accountDynastyName) =>
                {
                    ok = success;
                    error = message;
                    slots = accountSlots ?? Array.Empty<ProfileService.AccountSlotInfo>();
                    dynastyName = accountDynastyName ?? string.Empty;
                }
            );

            if (!ok)
            {
                loadingRememberedAccountSlots = false;
                SetError(FormatLoginError(error));
                ApplyResponsiveLayout();
                ResetContinueState();
                yield break;
            }

            loadingRememberedAccountSlots = false;
            loginSlots = NormalizeSlots(slots);
            cachedLoginDynastyName = string.IsNullOrWhiteSpace(dynastyName) ? cachedLoginEmail.Split('@')[0] : dynastyName;
            loginSlotsLoaded = true;
            selectedSlotIndex = FindFirstOccupiedSlot(loginSlots);
            RefreshSlotButtons();
            RefreshTabButtons();
            ApplyResponsiveLayout();
            SetError(string.Empty);
            ResetContinueState();
        }

        private System.Collections.IEnumerator LoginOnServerAndContinue(string email, string password, bool rememberProfile)
        {
            bool ok = false;
            string error = string.Empty;

            yield return ProfileService.I.LoginOnServer(
                email,
                password,
                selectedSlotIndex,
                rememberProfile,
                (success, message) =>
                {
                    ok = success;
                    error = message;
                }
            );

            if (!ok)
            {
                SetError(FormatLoginError(error));
                ResetContinueState();
                yield break;
            }

            yield return ContinueAfterInputSettles();
        }

        private System.Collections.IEnumerator RequestPasswordRecoveryAndShow(string email)
        {
            bool ok = false;
            string error = string.Empty;
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;

            yield return ProfileService.I.RequestPasswordRecovery(
                email,
                language,
                (success, message) =>
                {
                    ok = success;
                    error = message;
                }
            );

            SetError(ok ? PasswordRecoverySentText() : FormatPasswordRecoveryError(error));
            ResetContinueState();
        }

        private System.Collections.IEnumerator DeleteSelectedSlotAndRefresh(string email, string password)
        {
            bool ok = false;
            string error = string.Empty;
            ProfileService.AccountSlotInfo[] slots = Array.Empty<ProfileService.AccountSlotInfo>();
            string dynastyName = string.Empty;

            yield return ProfileService.I.DeleteProfileSlotOnServer(
                email,
                password,
                selectedSlotIndex,
                (success, message, accountSlots, accountDynastyName) =>
                {
                    ok = success;
                    error = message;
                    slots = accountSlots ?? Array.Empty<ProfileService.AccountSlotInfo>();
                    dynastyName = accountDynastyName ?? string.Empty;
                }
            );

            if (!ok)
            {
                SetError(FormatLoginError(error));
                ResetContinueState();
                yield break;
            }

            loginSlots = NormalizeSlots(slots);
            cachedLoginDynastyName = string.IsNullOrWhiteSpace(dynastyName) ? cachedLoginDynastyName : dynastyName;
            selectedSlotIndex = FindFirstOccupiedSlot(loginSlots);
            confirmingDeleteSlot = false;

            if (passwordInput != null)
                passwordInput.text = cachedLoginPassword;

            RefreshSlotButtons();
            RefreshTabButtons();
            ApplyResponsiveLayout();
            SetError(ProfileDeletedText());
            ResetContinueState();
        }

        private string FormatLoginError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return LoginFailedText();

            if (error.IndexOf("profile not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("no profile", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AccountNotFoundText();
            }

            return error;
        }

        private string FormatRegisterError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return ServerErrorText();

            if (error.IndexOf("profile not found", StringComparison.OrdinalIgnoreCase) >= 0)
                return RegistrationExpiredText();

            return error;
        }

        private string FormatPasswordRecoveryError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return PasswordRecoveryFailedText();

            if (error.IndexOf("not configured", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("Cannot POST", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return PasswordRecoveryNotConfiguredText();
            }

            return error;
        }

        private System.Collections.IEnumerator ContinueAfterInputSettles()
        {
            ReleaseActiveInputs();
            yield return null;
            yield return new WaitForEndOfFrame();
            ReleaseActiveInputs();

            ProfileBootstrap.LogRuntime("ProfileSetup input settled, continuing to lobby");
            bootstrap.ContinueAfterProfileSetup();
        }

        private void ResetContinueState()
        {
            continueInProgress = false;
            SetAccountButtonsInteractable(true);
        }

        private void StartCreateProfileInSelectedLoginSlot()
        {
            loginMode = false;
            creatingSlotForExistingAccount = true;
            registerStep = RegisterStep.Gender;
            currentAvatarIndex = 0;
            if (nameInput != null)
                nameInput.text = string.Empty;
            if (ageInput != null)
                ageInput.text = string.Empty;
            RefreshTabButtons();
            RefreshGenderButtons();
            ApplyAvatarVisual();
            ApplyResponsiveLayout();
            SetError(CreateProfileInSlotText());
            ResetContinueState();
        }

        private bool IsSelectedLoginSlotOccupied()
        {
            ProfileService.AccountSlotInfo slot = GetSlotInfo(selectedSlotIndex);
            return slot.Occupied;
        }

        private bool IsSelectedLoginSlotInUseByOtherDevice()
        {
            ProfileService.AccountSlotInfo slot = GetSlotInfo(selectedSlotIndex);
            return slot.InUseByOtherDevice;
        }

        private ProfileService.AccountSlotInfo GetSlotInfo(int slotIndex)
        {
            if (loginSlots != null)
            {
                for (int i = 0; i < loginSlots.Length; i++)
                {
                    if (loginSlots[i].SlotIndex == slotIndex)
                        return loginSlots[i];
                }
            }

            return ProfileService.AccountSlotInfo.Empty(slotIndex);
        }

        private static ProfileService.AccountSlotInfo[] NormalizeSlots(ProfileService.AccountSlotInfo[] source)
        {
            ProfileService.AccountSlotInfo[] slots =
            {
                ProfileService.AccountSlotInfo.Empty(1),
                ProfileService.AccountSlotInfo.Empty(2),
                ProfileService.AccountSlotInfo.Empty(3)
            };

            if (source == null)
                return slots;

            for (int i = 0; i < source.Length; i++)
            {
                int index = Mathf.Clamp(source[i].SlotIndex <= 0 ? i + 1 : source[i].SlotIndex, 1, 3) - 1;
                slots[index] = source[i];
                slots[index].SlotIndex = index + 1;
            }

            return slots;
        }

        private static int FindFirstOccupiedSlot(ProfileService.AccountSlotInfo[] slots)
        {
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].Occupied)
                        return Mathf.Clamp(slots[i].SlotIndex, 1, 3);
                }
            }

            return 1;
        }

        private bool ShouldRememberProfile()
        {
            return rememberToggle == null || rememberToggle.isOn;
        }

        private void SetAccountButtonsInteractable(bool value)
        {
            if (continueButton != null)
                continueButton.interactable = value;

            if (loginButton != null)
                loginButton.interactable = value;

            if (forgotPasswordButton != null)
                forgotPasswordButton.interactable = value;

            if (deleteSlotButton != null)
                deleteSlotButton.interactable = value;

            if (backButton != null)
                backButton.interactable = value;
        }

        private string ValidateAndNormalizeName(string rawName)
        {
            string value = string.IsNullOrWhiteSpace(rawName) ? string.Empty : rawName.Trim();

            if (string.IsNullOrEmpty(value))
            {
                SetError(EnterNameText());
                return null;
            }

            if (value.Length < minNameLength)
            {
                SetError(NameTooShortText());
                return null;
            }

            if (!IsLatinLettersOnly(value))
            {
                SetError(NameLatinOnlyText());
                return null;
            }

            if (value.Length > maxNameLength)
                value = value.Substring(0, maxNameLength);

            return string.IsNullOrWhiteSpace(value) ? fallbackPlayerName : value;
        }

        private string ValidateAndNormalizeDynastyName(string rawName)
        {
            string value = string.IsNullOrWhiteSpace(rawName) ? string.Empty : rawName.Trim();
            value = System.Text.RegularExpressions.Regex.Replace(value, "\\s+", " ");

            if (string.IsNullOrEmpty(value))
            {
                SetError(EnterDynastyNameText());
                return null;
            }

            if (value.Length < 2)
            {
                SetError(DynastyNameTooShortText());
                return null;
            }

            return value.Length > 48 ? value.Substring(0, 48) : value;
        }

        private string ValidateAndNormalizeEmail(string rawEmail)
        {
            string value = string.IsNullOrWhiteSpace(rawEmail) ? string.Empty : rawEmail.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(value))
            {
                SetError(EnterEmailText());
                return null;
            }

            int at = value.IndexOf('@');
            int dot = value.LastIndexOf('.');
            if (at <= 0 || dot <= at + 1 || dot >= value.Length - 1)
            {
                SetError(EmailInvalidText());
                return null;
            }

            return value;
        }

        private string ValidatePassword(string rawPassword)
        {
            string value = rawPassword ?? string.Empty;
            if (value.Length < 6)
            {
                SetError(PasswordShortText());
                return null;
            }

            return value;
        }

        private bool TryValidateAge(string rawAge, out int age)
        {
            age = 0;

            if (string.IsNullOrWhiteSpace(rawAge))
                return true;

            if (!int.TryParse(rawAge.Trim(), out age))
            {
                SetError(AgeInvalidText());
                return false;
            }

            if (age < minAge || age > maxAge)
            {
                SetError(AgeInvalidText());
                return false;
            }

            return true;
        }

        private void ConfigureInput()
        {
            if (nameInput != null)
            {
                nameInput.characterLimit = Mathf.Max(1, maxNameLength);
                nameInput.lineType = TMP_InputField.LineType.SingleLine;
                nameInput.contentType = TMP_InputField.ContentType.Standard;
                nameInput.characterValidation = TMP_InputField.CharacterValidation.None;
                nameInput.inputValidator = null;
            }

            if (dynastyInput != null)
            {
                dynastyInput.characterLimit = 48;
                dynastyInput.lineType = TMP_InputField.LineType.SingleLine;
                dynastyInput.contentType = TMP_InputField.ContentType.Standard;
            }

            if (ageInput != null)
            {
                ageInput.characterLimit = 3;
                ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                ageInput.lineType = TMP_InputField.LineType.SingleLine;
            }

            if (emailInput != null)
            {
                emailInput.characterLimit = 64;
                emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
                emailInput.lineType = TMP_InputField.LineType.SingleLine;
            }

            if (passwordInput != null)
            {
                passwordInput.characterLimit = 64;
                passwordInput.contentType = TMP_InputField.ContentType.Password;
                passwordInput.lineType = TMP_InputField.LineType.SingleLine;
            }
        }

        private void ApplyAvatarVisual()
        {
            if (avatarPreview == null)
                return;

            if (TryApplyResourceAvatarVisual())
                return;

            BattleCharacterDatabase.BattleCharacterData characterData = GetAvatarCharacterData();
            if (characterData != null)
            {
                currentAvatarIndex = Mathf.Clamp(currentAvatarIndex, 0, Mathf.Max(0, GetAvatarCount() - 1));
                avatarPreview.sprite = null;
                avatarPreview.enabled = false;

                if (avatarModelView == null)
                    avatarModelView = avatarPreview.GetComponent<BattleCharacterModelView>();

                if (avatarModelView == null)
                    avatarModelView = avatarPreview.gameObject.AddComponent<BattleCharacterModelView>();

                if (avatarModelView.Show(characterData, BattleCharacterModelView.ModelContext.Profile))
                {
                    if (avatarIndexText != null)
                        avatarIndexText.text = $"{currentAvatarIndex + 1} / {GetAvatarCount()}";

                    return;
                }
            }

            int spriteCount = GetFilteredSpriteAvatarCount();
            if (avatarSprites == null || spriteCount == 0)
            {
                avatarPreview.sprite = null;
                avatarPreview.enabled = false;
                if (avatarModelView != null)
                    avatarModelView.Hide();

                if (avatarIndexText != null)
                    avatarIndexText.text = NoAvatarsText();
                return;
            }

            currentAvatarIndex = Mathf.Clamp(currentAvatarIndex, 0, spriteCount - 1);
            if (avatarModelView != null)
                avatarModelView.Hide();

            avatarPreview.enabled = true;
            avatarPreview.sprite = avatarSprites[GetFilteredSpriteAvatarGlobalIndex()];

            if (avatarIndexText != null)
                avatarIndexText.text = $"{currentAvatarIndex + 1} / {spriteCount}";
        }

        private void RefreshGenderButtons()
        {
            ApplyGenderButton(maleButton, PlayerGender.Male);
            ApplyGenderButton(femaleButton, PlayerGender.Female);
            ApplyGenderButton(otherButton, PlayerGender.Other);
        }

        private void RefreshLanguageButtons()
        {
            ApplyLanguageButtonSprites();
            ApplyLanguageButton(russianLanguageButton, GameLanguage.Russian);
            ApplyLanguageButton(englishLanguageButton, GameLanguage.English);
            ApplyLanguageButton(turkishLanguageButton, GameLanguage.Turkish);
        }

        private void ApplyLanguageButton(Button button, GameLanguage language)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            GameLanguage current = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
            bool hasFlagSprite = GetLanguageButtonSprite(language) != null;
            if (hasFlagSprite)
            {
                image.color = Color.white;
                return;
            }

            image.color = current == language
                ? new Color(0.22f, 0.52f, 0.86f, 1f)
                : new Color(0.13f, 0.16f, 0.22f, 1f);
        }

        private void ApplyLanguageButtonSprites()
        {
            ApplyLanguageButtonSprite(russianLanguageButton, LoadRussianLanguageButtonSprite());
            ApplyLanguageButtonSprite(englishLanguageButton, LoadEnglishLanguageButtonSprite());
            ApplyLanguageButtonSprite(turkishLanguageButton, LoadTurkishLanguageButtonSprite());
        }

        private void ApplyLanguageButtonSprite(Button button, Sprite sprite)
        {
            if (button == null || button.image == null)
                return;

            if (sprite != null)
            {
                button.image.sprite = sprite;
                button.image.type = Image.Type.Simple;
                button.image.preserveAspect = true;
                button.image.color = Color.white;
            }

            SetButtonLabelVisible(button, sprite == null);
        }

        private Sprite GetLanguageButtonSprite(GameLanguage language)
        {
            return language switch
            {
                GameLanguage.Russian => LoadRussianLanguageButtonSprite(),
                GameLanguage.English => LoadEnglishLanguageButtonSprite(),
                GameLanguage.Turkish => LoadTurkishLanguageButtonSprite(),
                _ => null
            };
        }

        private Sprite LoadRussianLanguageButtonSprite()
        {
            if (cachedRussianLanguageButtonSprite != null)
                return cachedRussianLanguageButtonSprite;

            cachedRussianLanguageButtonSprite = LoadFirstSprite(RussianLanguageButtonResourcePath);
            return cachedRussianLanguageButtonSprite;
        }

        private Sprite LoadEnglishLanguageButtonSprite()
        {
            if (cachedEnglishLanguageButtonSprite != null)
                return cachedEnglishLanguageButtonSprite;

            cachedEnglishLanguageButtonSprite = LoadFirstSprite(EnglishLanguageButtonResourcePath);
            return cachedEnglishLanguageButtonSprite;
        }

        private Sprite LoadTurkishLanguageButtonSprite()
        {
            if (cachedTurkishLanguageButtonSprite != null)
                return cachedTurkishLanguageButtonSprite;

            cachedTurkishLanguageButtonSprite = LoadFirstSprite(TurkishLanguageButtonResourcePath);
            return cachedTurkishLanguageButtonSprite;
        }

        private static Sprite LoadFirstSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        private static Sprite LoadBuiltinUiSprite()
        {
            if (cachedBuiltinUiSprite != null)
                return cachedBuiltinUiSprite;

            cachedBuiltinUiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (cachedBuiltinUiSprite == null)
                cachedBuiltinUiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");

            return cachedBuiltinUiSprite;
        }

        private void RefreshSlotButtons()
        {
            ApplySlotButton(slotOneButton, 1);
            ApplySlotButton(slotTwoButton, 2);
            ApplySlotButton(slotThreeButton, 3);
            ApplySelectedSlotProfilePreview();
        }

        private void ApplySelectedSlotProfilePreview()
        {
            if (!HasAccountSlotOverview())
            {
                SetSlotProfileText(string.Empty, string.Empty, string.Empty);
                return;
            }

            if (avatarModelView != null)
                avatarModelView.Hide();

            ProfileService.AccountSlotInfo slot = GetSlotInfo(selectedSlotIndex);
            if (!slot.Occupied)
            {
                if (avatarPreview != null)
                {
                    avatarPreview.sprite = null;
                    avatarPreview.enabled = false;
                }

                SetSlotProfileText(ProfileUiText("Свободный слот", "Free Slot", "Bos Yuva"), LevelText("-"), AgeText("-"));
                return;
            }

            if (avatarPreview != null)
            {
                Sprite sprite = ProfileAvatarResources.GetSprite(slot.Gender, slot.AvatarId);
                avatarPreview.sprite = sprite;
                avatarPreview.enabled = sprite != null;
            }

            string nickname = string.IsNullOrWhiteSpace(slot.Nickname) ? ProfileLabelText() : slot.Nickname.Trim();
            string age = slot.Age > 0 ? slot.Age.ToString() : "-";
            SetSlotProfileText(nickname, LevelText("1"), AgeText(age));
        }

        private void SetSlotProfileText(string profileName, string level, string age)
        {
            if (slotProfileNameText != null)
                slotProfileNameText.text = profileName;

            if (slotProfileLevelText != null)
                slotProfileLevelText.text = level;

            if (slotProfileAgeText != null)
                slotProfileAgeText.text = age;
        }

        private void ApplySlotButton(Button button, int slotIndex)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            image.color = selectedSlotIndex == slotIndex
                ? new Color(0.22f, 0.52f, 0.86f, 1f)
                : new Color(0.13f, 0.16f, 0.22f, 1f);

            SetButtonLabel(button, GetSlotButtonLabel(slotIndex));
            ApplySlotTextLayout(button, slotIndex);
            ApplySlotAvatarImage(GetSlotAvatarImage(slotIndex), slotIndex);
        }

        private string GetSlotButtonLabel(int slotIndex)
        {
            if (HasAccountSlotOverview())
            {
                ProfileService.AccountSlotInfo slot = GetSlotInfo(slotIndex);
                if (slot.InUseByOtherDevice)
                    return $"{slotIndex}\n{BusyText()}";

                if (slot.Occupied)
                {
                    string nickname = string.IsNullOrWhiteSpace(slot.Nickname) ? ProfileLabelText() : slot.Nickname;
                    return $"{slotIndex}\n{nickname}";
                }

                return $"+\n{FreeText()}";
            }

            return slotIndex.ToString();
        }

        private Image GetSlotAvatarImage(int slotIndex)
        {
            switch (slotIndex)
            {
                case 1:
                    return slotOneAvatarImage;
                case 2:
                    return slotTwoAvatarImage;
                case 3:
                    return slotThreeAvatarImage;
                default:
                    return null;
            }
        }

        private void ApplySlotAvatarImage(Image image, int slotIndex)
        {
            if (image == null)
                return;

            bool showAvatar = HasAccountSlotOverview();
            ProfileService.AccountSlotInfo slot = GetSlotInfo(slotIndex);
            Sprite sprite = showAvatar && slot.Occupied
                ? ProfileAvatarResources.GetSprite(slot.Gender, slot.AvatarId)
                : null;

            image.sprite = sprite;
            image.enabled = sprite != null;
            SetObjectActive(image.gameObject, sprite != null);
        }

        private void ApplySlotTextLayout(Button button, int slotIndex)
        {
            if (button == null)
                return;

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
                return;

            if (HasAccountSlotOverview() && GetSlotInfo(slotIndex).Occupied)
            {
                text.alignment = TextAlignmentOptions.Bottom;
                text.margin = new Vector4(12f, 52f, 12f, 8f);
            }
            else if (HasAccountSlotOverview())
            {
                text.alignment = TextAlignmentOptions.Center;
                text.margin = new Vector4(12f, 0f, 12f, 0f);
            }
            else
            {
                text.alignment = TextAlignmentOptions.Center;
                text.margin = new Vector4(12f, 0f, 12f, 0f);
            }
        }

        private string ProfileUiText(string russian, string english, string turkish)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
            return language switch
            {
                GameLanguage.Russian => russian,
                GameLanguage.English => english,
                _ => turkish
            };
        }

        private string ProfileTitleText() => ProfileUiText("Создать профиль", "Create Profile", "Profil Olustur");
        private string ProfileSubtitleText() => ProfileUiText("Выберите аватар и заполните данные профиля.", "Choose your avatar and fill in the profile details.", "Avatarini sec ve profil bilgilerini doldur.");
        private string AvatarText() => ProfileUiText("Аватар", "Avatar", "Avatar");
        private string RegisterText() => ProfileUiText("Регистрация", "Register", "Kayit");
        private string LoginText() => ProfileUiText("Войти", "Login", "Giris");
        private string ForgotPasswordText() => ProfileUiText("Восстановить пароль", "Recover Password", "Sifreyi Kurtar");
        private string DynastyNameText() => ProfileUiText("Название династии", "Dynasty Name", "Hanedan Adi");
        private string NicknameText() => ProfileUiText("Никнейм", "Nickname", "Takma ad");
        private string EmailText() => ProfileUiText("Почта", "Email", "E-posta");
        private string PasswordText() => ProfileUiText("Пароль", "Password", "Sifre");
        private string AgeInputText() => ProfileUiText("Возраст", "Age", "Yas");
        private string GenderText() => ProfileUiText("Пол", "Gender", "Cinsiyet");
        private string MaleText() => ProfileUiText("Мужчина", "Male", "Erkek");
        private string FemaleText() => ProfileUiText("Женщина", "Female", "Kadin");
        private string OtherText() => ProfileUiText("Другое", "Other", "Diger");
        private string RememberProfileText() => ProfileUiText("Запомнить профиль", "Remember Profile", "Profili Hatirla");
        private string ProfileSlotText() => ProfileUiText("Слот профиля", "Profile Slot", "Profil Yuvasi");
        private string AutoIdText() => ProfileUiText("ID будет назначен автоматически", "ID will be assigned automatically", "ID otomatik atanacak");
        private string BackText() => ProfileUiText("Назад", "Back", "Geri");
        private string CancelText() => ProfileUiText("Отмена", "Cancel", "Iptal");
        private string ContinueText() => ProfileUiText("Далее", "Next", "Ileri");
        private string DeleteProfileText() => ProfileUiText("Удалить профиль", "Delete Profile", "Profili Sil");
        private string ConfirmDeleteText() => ProfileUiText("Подтвердите удаление", "Confirm Delete", "Silmeyi Onayla");
        private string DynastyAccountText() => ProfileUiText("Аккаунт династии", "Dynasty Account", "Hanedan Hesabi");
        private string ChooseGenderText() => ProfileUiText("Выберите пол", "Choose Gender", "Cinsiyet Sec");
        private string ProfileDetailsText() => ProfileUiText("Данные профиля", "Profile Details", "Profil Bilgileri");
        private string EnterSlotText() => ProfileUiText("Войти в слот", "Enter Slot", "Yuvaya Gir");
        private string CreateSlotText() => ProfileUiText("Создать слот", "Create Slot", "Yuva Olustur");
        private string BusyText() => ProfileUiText("Занят", "Busy", "Mesgul");
        private string FreeText() => ProfileUiText("Свободно", "Free", "Bos");
        private string FreeSlotText() => ProfileUiText("Свободный слот", "Free Slot", "Bos Yuva");
        private string ProfileLabelText() => ProfileUiText("Профиль", "Profile", "Profil");
        private string ProfileDeletedText() => ProfileUiText("Профиль удален.", "Profile deleted.", "Profil silindi.");
        private string CreateProfileInSlotText() => ProfileUiText("Создайте профиль в свободном слоте.", "Create a profile in this free slot.", "Bos yuvada profil olustur.");
        private string SlotOccupiedText() => ProfileUiText("Этот слот уже занят.", "This slot is already occupied.", "Bu yuva zaten dolu.");
        private string ProfileInUseText() => ProfileUiText("Этот профиль используется на другом устройстве.", "This profile is in use on another device.", "Bu profil baska cihazda kullaniliyor.");
        private string ChooseOccupiedSlotText() => ProfileUiText("Выберите занятый слот профиля.", "Choose an occupied profile slot.", "Dolu bir profil yuvasi sec.");
        private string EnterPasswordToDeleteText() => ProfileUiText("Введите пароль аккаунта для удаления профиля.", "Enter account password to delete this profile.", "Profili silmek icin hesap sifresini gir.");
        private string ChooseMaleOrFemaleText() => ProfileUiText("Выберите мужской или женский пол.", "Choose male or female.", "Erkek veya kadin sec.");
        private string LoginFailedText() => ProfileUiText("Вход не выполнен. Попробуйте еще раз.", "Login failed. Please try again.", "Giris basarisiz. Tekrar dene.");
        private string PasswordRecoverySentText() => ProfileUiText("Письмо для восстановления отправлено.", "Recovery email sent.", "Kurtarma e-postasi gonderildi.");
        private string PasswordRecoveryFailedText() => ProfileUiText("Не удалось отправить письмо восстановления.", "Could not send recovery email.", "Kurtarma e-postasi gonderilemedi.");
        private string PasswordRecoveryNotConfiguredText() => ProfileUiText("Восстановление пароля пока не настроено на сервере.", "Password recovery is not configured on the server yet.", "Sifre kurtarma henuz sunucuda ayarlanmadi.");
        private string DeleteFailedText() => ProfileUiText("Удаление не выполнено. Попробуйте еще раз.", "Delete failed. Please try again.", "Silme basarisiz. Tekrar dene.");
        private string AccountNotFoundText() => ProfileUiText("Аккаунт не найден. Откройте регистрацию, чтобы создать профиль.", "Account not found. Open Register to create a new profile.", "Hesap bulunamadi. Profil olusturmak icin Kayit ac.");
        private string RegistrationExpiredText() => ProfileUiText("Сессия регистрации истекла. Нажмите Регистрация еще раз.", "Registration session expired. Please press Register again.", "Kayit oturumu doldu. Lutfen Kayit'a tekrar bas.");
        private string ServerErrorText() => ProfileUiText("Ошибка сервера.", "Server error.", "Sunucu hatasi.");
        private string EnterDynastyNameText() => ProfileUiText("Введите название династии.", "Enter Dynasty Name.", "Hanedan adini gir.");
        private string DynastyNameTooShortText() => ProfileUiText("Название династии слишком короткое.", "Dynasty Name is too short.", "Hanedan adi cok kisa.");
        private string NoAvatarsText() => ProfileUiText("Нет аватаров", "No avatars", "Avatar yok");
        private string EnterNameText() => ProfileUiText("Введите никнейм.", "Enter nickname.", "Takma ad gir.");
        private string NameTooShortText() => ProfileUiText($"Никнейм должен быть минимум {minNameLength} символа.", $"Nickname must be at least {minNameLength} characters.", $"Takma ad en az {minNameLength} karakter olmali.");
        private string NameLatinOnlyText() => ProfileUiText("Никнейм должен быть латиницей.", "Nickname must use Latin letters only.", "Takma ad sadece Latin harfleri olmali.");
        private string EnterEmailText() => ProfileUiText("Введите почту.", "Enter email.", "E-posta gir.");
        private string EmailInvalidText() => ProfileUiText("Почта введена неверно.", "Email is invalid.", "E-posta gecersiz.");
        private string PasswordShortText() => ProfileUiText("Пароль должен быть минимум 6 символов.", "Password must be at least 6 characters.", "Sifre en az 6 karakter olmali.");
        private string AgeInvalidText() => ProfileUiText("Возраст введен неверно.", "Age is invalid.", "Yas gecersiz.");

        private string LevelText(string value)
        {
            string localizedFormat = ProfileUiText("Уровень: {0}", "Level: {0}", "Seviye: {0}");
            if (!string.IsNullOrEmpty(localizedFormat))
                return string.Format(localizedFormat, value);
            return string.Format(ProfileUiText("Уровень: {0}", "Level: {0}", "Seviye: {0}"), value);
        }

        private string AgeText(string value)
        {
            string localizedFormat = ProfileUiText("Возраст: {0}", "Age: {0}", "Yas: {0}");
            if (!string.IsNullOrEmpty(localizedFormat))
                return string.Format(localizedFormat, value);
            return string.Format(ProfileUiText("Возраст: {0}", "Age: {0}", "Yas: {0}"), value);
        }

        private void RefreshTabButtons()
        {
            if (TryRefreshLocalizedTabButtons())
                return;

            ApplyTabButton(dynastyTabButton, !loginMode);
            ApplyTabButton(profileTabButton, loginMode);

            if (registerStepText != null)
            {
                registerStepText.text = confirmingDeleteSlot
                    ? ProfileUiText("Подтвердите удаление", "Confirm Delete", "Silmeyi Onayla")
                    : loginMode ? GameLocalization.Text("profile.setup.login")
                    : registerStep == RegisterStep.Account ? ProfileUiText("Аккаунт династии", "Dynasty Account", "Hanedan Hesabi")
                    : registerStep == RegisterStep.Gender ? ProfileUiText("Выберите пол", "Choose Gender", "Cinsiyet Sec")
                    : ProfileUiText("Данные профиля", "Profile Details", "Profil Bilgileri");
            }

            SetButtonLabel(continueButton, loginMode
                ? GameLocalization.Text("profile.setup.login")
                : registerStep == RegisterStep.Details
                    ? GameLocalization.Text("profile.setup.register")
                    : ProfileUiText("Далее", "Next", "Ileri"));
            SetButtonLabel(loginButton, confirmingDeleteSlot
                ? ProfileUiText("Удалить профиль", "Delete Profile", "Profili Sil")
                : loginMode && loginSlotsLoaded
                    ? IsSelectedLoginSlotInUseByOtherDevice() ? ProfileUiText("Занят", "Busy", "Mesgul")
                    : IsSelectedLoginSlotOccupied() ? ProfileUiText("Войти в слот", "Enter Slot", "Yuvaya Gir")
                    : ProfileUiText("Создать слот", "Create Slot", "Yuva Olustur")
                : GameLocalization.Text("profile.setup.login"));
            SetButtonLabel(deleteSlotButton, ProfileUiText("Удалить профиль", "Delete Profile", "Profili Sil"));
            SetButtonLabel(forgotPasswordButton, ForgotPasswordText());
            SetButtonLabel(backButton, confirmingDeleteSlot ? ProfileUiText("Отмена", "Cancel", "Iptal") : GameLocalization.Text("mahjong.back"));
        }

        private bool TryRefreshLocalizedTabButtons()
        {
            SetButtonLabel(dynastyTabButton, RegisterText());
            SetButtonLabel(profileTabButton, LoginText());
            ApplyTabButton(dynastyTabButton, !loginMode);
            ApplyTabButton(profileTabButton, loginMode);

            if (registerStepText != null)
            {
                registerStepText.text = confirmingDeleteSlot
                    ? ConfirmDeleteText()
                    : loginMode ? LoginText()
                    : registerStep == RegisterStep.Account ? DynastyAccountText()
                    : registerStep == RegisterStep.Gender ? ChooseGenderText()
                    : ProfileDetailsText();
            }

            SetButtonLabel(continueButton, loginMode
                ? LoginText()
                : registerStep == RegisterStep.Details
                    ? RegisterText()
                    : ContinueText());

            SetButtonLabel(loginButton, confirmingDeleteSlot
                ? DeleteProfileText()
                : loginMode && loginSlotsLoaded
                    ? IsSelectedLoginSlotInUseByOtherDevice() ? BusyText()
                    : IsSelectedLoginSlotOccupied() ? EnterSlotText()
                    : CreateSlotText()
                : LoginText());

            SetButtonLabel(deleteSlotButton, DeleteProfileText());
            SetButtonLabel(forgotPasswordButton, ForgotPasswordText());
            SetButtonLabel(backButton, confirmingDeleteSlot ? CancelText() : BackText());
            return true;
        }

        private bool HasAccountSlotOverview()
        {
            return loginSlotsLoaded && (loginMode || creatingSlotForExistingAccount);
        }

        private void ApplyTabButton(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            image.color = active
                ? new Color(0.22f, 0.52f, 0.86f, 1f)
                : new Color(0.13f, 0.16f, 0.22f, 1f);
        }

        private void ApplyGenderButton(Button button, PlayerGender gender)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            image.color = selectedGender == gender
                ? new Color(0.22f, 0.52f, 0.86f, 1f)
                : new Color(0.13f, 0.16f, 0.22f, 1f);
        }

        private void SetError(string message)
        {
            if (errorText == null)
                return;

            errorText.text = message;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));

            if (generatedRoot != null)
                ApplyResponsiveLayout();
        }

        private int GetLastAvatarIndex()
        {
            return Mathf.Max(0, GetAvatarCount() - 1);
        }

        private int GetAvatarCount()
        {
            int resourceCount = GetResourceAvatarCount();
            if (resourceCount > 0)
                return resourceCount;

            BattleCharacterDatabase database = ResolveCharacterDatabase();
            int characterCount = database != null ? GetFilteredAvatarCharacters(database).Count : 0;
            int spriteCount = GetFilteredSpriteAvatarCount();
            return Mathf.Max(characterCount, spriteCount);
        }

        private bool TryApplyResourceAvatarVisual()
        {
            Sprite[] sprites = GetCurrentResourceAvatarSprites();
            if (sprites == null || sprites.Length == 0)
                return false;

            currentAvatarIndex = Mathf.Clamp(currentAvatarIndex, 0, sprites.Length - 1);

            if (avatarModelView != null)
                avatarModelView.Hide();

            avatarPreview.enabled = true;
            avatarPreview.sprite = sprites[currentAvatarIndex];

            if (avatarIndexText != null)
                avatarIndexText.text = $"{currentAvatarIndex + 1} / {sprites.Length}";

            return true;
        }

        private int GetResourceAvatarCount()
        {
            Sprite[] sprites = GetCurrentResourceAvatarSprites();
            return sprites != null ? sprites.Length : 0;
        }

        private Sprite[] GetCurrentResourceAvatarSprites()
        {
            if (selectedGender != PlayerGender.Male && selectedGender != PlayerGender.Female)
                return Array.Empty<Sprite>();

            return ProfileAvatarResources.GetSprites(selectedGender);
        }

        private BattleCharacterDatabase.BattleCharacterData GetAvatarCharacterData()
        {
            BattleCharacterDatabase database = ResolveCharacterDatabase();
            if (database == null)
                return null;

            List<BattleCharacterDatabase.BattleCharacterData> characters = GetFilteredAvatarCharacters(database);
            if (characters == null || characters.Count == 0)
                return null;

            currentAvatarIndex = Mathf.Clamp(currentAvatarIndex, 0, characters.Count - 1);
            return characters[currentAvatarIndex];
        }

        private int GetSelectedAvatarId()
        {
            if (GetResourceAvatarCount() > 0)
                return Mathf.Clamp(currentAvatarIndex, 0, GetResourceAvatarCount() - 1);

            BattleCharacterDatabase database = ResolveCharacterDatabase();
            if (database != null)
            {
                BattleCharacterDatabase.BattleCharacterData selected = GetAvatarCharacterData();
                List<BattleCharacterDatabase.BattleCharacterData> all = database.GetEnabledCharacters();
                for (int i = 0; i < all.Count; i++)
                {
                    if (ReferenceEquals(all[i], selected))
                        return i;
                }
            }

            return GetFilteredSpriteAvatarGlobalIndex();
        }

        private List<BattleCharacterDatabase.BattleCharacterData> GetFilteredAvatarCharacters(BattleCharacterDatabase database)
        {
            List<BattleCharacterDatabase.BattleCharacterData> result = new List<BattleCharacterDatabase.BattleCharacterData>();
            if (database == null)
                return result;

            List<BattleCharacterDatabase.BattleCharacterData> characters = database.GetEnabledCharacters();
            BattleCharacterDatabase.CharacterGender? gender = ToBattleCharacterGender(selectedGender);

            for (int i = 0; i < characters.Count; i++)
            {
                BattleCharacterDatabase.BattleCharacterData data = characters[i];
                if (data == null)
                    continue;

                if (!gender.HasValue || data.Gender == gender.Value)
                    result.Add(data);
            }

            return result;
        }

        private int GetFilteredSpriteAvatarCount()
        {
            if (avatarSprites == null || avatarSprites.Length == 0)
                return 0;

            if (selectedGender != PlayerGender.Male && selectedGender != PlayerGender.Female)
                return avatarSprites.Length;

            int half = Mathf.CeilToInt(avatarSprites.Length * 0.5f);
            return selectedGender == PlayerGender.Male
                ? half
                : Mathf.Max(0, avatarSprites.Length - half);
        }

        private int GetFilteredSpriteAvatarGlobalIndex()
        {
            if (avatarSprites == null || avatarSprites.Length == 0)
                return 0;

            if (selectedGender != PlayerGender.Female)
                return Mathf.Clamp(currentAvatarIndex, 0, avatarSprites.Length - 1);

            int half = Mathf.CeilToInt(avatarSprites.Length * 0.5f);
            int femaleCount = Mathf.Max(0, avatarSprites.Length - half);
            return half + Mathf.Clamp(currentAvatarIndex, 0, Mathf.Max(0, femaleCount - 1));
        }

        private static BattleCharacterDatabase.CharacterGender? ToBattleCharacterGender(PlayerGender gender)
        {
            return gender switch
            {
                PlayerGender.Male => BattleCharacterDatabase.CharacterGender.Male,
                PlayerGender.Female => BattleCharacterDatabase.CharacterGender.Female,
                _ => null
            };
        }

        private BattleCharacterDatabase ResolveCharacterDatabase()
        {
            if (BattleCharacterDatabase.HasInstance)
                return BattleCharacterDatabase.Instance;

            BattleCharacterDatabase database = FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include);
            if (database != null)
                return database;

            GameObject prefab = Resources.Load<GameObject>("BattleCharacters/BattleCharasterDatabase");
            if (prefab == null)
                return null;

            GameObject instance = Instantiate(prefab);
            instance.name = "BattleCharasterDatabase";
            return instance.GetComponent<BattleCharacterDatabase>();
        }

        private bool IsLatinLettersOnly(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (!IsLatinLetter(value[i]))
                    return false;
            }

            return true;
        }

        private void SanitizeNameInput(string value)
        {
            if (sanitizingNameInput || nameInput == null || string.IsNullOrEmpty(value))
                return;

            string clean = value;
            for (int i = clean.Length - 1; i >= 0; i--)
            {
                if (!IsLatinLetter(clean[i]))
                    clean = clean.Remove(i, 1);
            }

            if (clean == value)
                return;

            sanitizingNameInput = true;
            int caretPosition = Mathf.Clamp(nameInput.stringPosition - (value.Length - clean.Length), 0, clean.Length);
            nameInput.SetTextWithoutNotify(clean);
            nameInput.stringPosition = caretPosition;
            nameInput.caretPosition = caretPosition;
            sanitizingNameInput = false;
        }

        private static bool IsLatinLetter(char value)
        {
            return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
        }

        private void ApplyResponsiveLayout()
        {
            RectTransform root = transform as RectTransform;
            if (root == null || root.rect.width < 1f || root.rect.height < 1f)
                root = transform.parent as RectTransform;

            if ((root == null || root.rect.width < 1f || root.rect.height < 1f) && generatedRoot != null)
                root = generatedRoot.parent as RectTransform;

            if (root == null || root.rect.width < 1f || root.rect.height < 1f)
            {
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                    root = parentCanvas.GetComponent<RectTransform>();
            }

            if (root == null || windowRect == null || leftPaneRect == null || rightPaneRect == null)
                return;

            float rootWidth = Mathf.Max(1f, root.rect.width);
            float rootHeight = Mathf.Max(1f, root.rect.height);
            LayoutTuningSettings layout = layoutTuning ?? new LayoutTuningSettings();

            float targetWindowWidthPercent = Mathf.Clamp(layout.windowWidthPercent, 0.7f, 0.995f);
            float targetWindowHeightPercent = Mathf.Clamp(layout.windowHeightPercent, 0.7f, 0.99f);
            float windowWidth = rootWidth * targetWindowWidthPercent;
            float windowHeight = rootHeight * targetWindowHeightPercent;
            windowRect.sizeDelta = new Vector2(windowWidth, windowHeight);

            Stretch(windowBackgroundRect);

            Rect contentRect = GetProfileWindowContentRect(windowWidth, windowHeight);
            float headerPaddingX = Mathf.Clamp(contentRect.width * 0.018f, 10f, 22f);
            float bodyPaddingX = Mathf.Clamp(contentRect.width * 0.02f, 14f, 28f);
            float languageButtonWidth = Mathf.Clamp(contentRect.width * 0.034f, 40f, 48f);
            float languageButtonGap = Mathf.Clamp(layout.languageButtonGap, 4f, 8f);
            float languageButtonHeight = Mathf.Clamp(languageButtonWidth * 0.72f, 28f, 34f);
            float languageRowWidth = languageButtonWidth * 3f + languageButtonGap * 2f;
            float headerTop = contentRect.y + contentRect.height;
            float languageOffsetX = Mathf.Max(languageRowWidth * 0.45f, contentRect.width * 0.085f);
            float languageOffsetY = Mathf.Max(languageButtonHeight * 0.7f, contentRect.height * 0.12f);
            float languageX = contentRect.x + contentRect.width - languageRowWidth - headerPaddingX - languageOffsetX;
            float languageY = headerTop - languageButtonHeight - 14f - languageOffsetY;
            SetRect(russianLanguageButton != null ? russianLanguageButton.transform as RectTransform : null, languageX, languageY, languageButtonWidth, languageButtonHeight);
            SetRect(englishLanguageButton != null ? englishLanguageButton.transform as RectTransform : null, languageX + languageButtonWidth + languageButtonGap, languageY, languageButtonWidth, languageButtonHeight);
            SetRect(turkishLanguageButton != null ? turkishLanguageButton.transform as RectTransform : null, languageX + (languageButtonWidth + languageButtonGap) * 2f, languageY, languageButtonWidth, languageButtonHeight);

            float titleWidth = Mathf.Min(Mathf.Max(320f, contentRect.width - languageRowWidth - headerPaddingX * 4f), 720f);
            float titleX = contentRect.x + (contentRect.width - titleWidth) * 0.5f;
            float titleLift = Mathf.Max(layout.titleHeight * 0.36f, contentRect.height * 0.02f);
            float titleY = headerTop - layout.titleHeight - 14f + titleLift;
            SetRect(windowRect.Find("Title") as RectTransform, titleX, titleY, titleWidth, layout.titleHeight);

            float subtitleWidth = Mathf.Min(Mathf.Max(320f, contentRect.width - headerPaddingX * 4f), 780f);
            float subtitleX = contentRect.x + (contentRect.width - subtitleWidth) * 0.5f;
            float subtitleY = titleY - layout.subtitleHeight - 8f;
            SetRect(windowRect.Find("Subtitle") as RectTransform, subtitleX, subtitleY, subtitleWidth, layout.subtitleHeight);

            float bodyBottom = contentRect.y + Mathf.Clamp(contentRect.height * 0.02f, 10f, 20f);
            float bodyTop = subtitleY - 24f;
            float bodyHeight = Mathf.Max(180f, bodyTop - bodyBottom);
            float gap = Mathf.Clamp(contentRect.width * 0.012f, 6f, layout.bodyGap);
            float bodyX = contentRect.x + bodyPaddingX;
            float bodyWidth = contentRect.width - bodyPaddingX * 2f;
            float availableBodyWidth = Mathf.Max(260f, bodyWidth);
            float minLeftWidth = Mathf.Min(layout.leftPaneWidthRange.x, layout.leftPaneWidthRange.y);
            float maxLeftWidth = Mathf.Max(layout.leftPaneWidthRange.x, layout.leftPaneWidthRange.y);
            float leftWidth = Mathf.Clamp(availableBodyWidth * layout.leftPaneWidthPercent, minLeftWidth, maxLeftWidth);
            float rightWidth = availableBodyWidth - leftWidth - gap;
            float minRightWidth = Mathf.Min(420f, availableBodyWidth * 0.6f);
            if (rightWidth < minRightWidth)
            {
                leftWidth = Mathf.Max(minLeftWidth, leftWidth - (minRightWidth - rightWidth));
                rightWidth = availableBodyWidth - leftWidth - gap;
            }

            SetRect(leftPaneRect, bodyX, bodyBottom, leftWidth, bodyHeight);
            SetRect(rightPaneRect, bodyX + leftWidth + gap, bodyBottom, rightWidth, bodyHeight);

            LayoutLeftPane(leftWidth, bodyHeight);
            LayoutRightPane(rightWidth, bodyHeight);
        }

        private void LayoutLeftPane(float width, float height)
        {
            if (leftPaneRect == null)
                return;

            LayoutTuningSettings layout = layoutTuning ?? new LayoutTuningSettings();
            bool showSlotPreview = loginMode && loginSlotsLoaded;
            float sidePadding = Mathf.Clamp(width * 0.06f, 14f, 24f);
            float topPadding = Mathf.Clamp(height * 0.02f, 8f, 14f);
            float avatarMax = showSlotPreview
                ? Mathf.Max(layout.slotPreviewAvatarSizeRange.x, layout.slotPreviewAvatarSizeRange.y)
                : Mathf.Max(layout.avatarSizeRange.x, layout.avatarSizeRange.y);
            float avatarMin = showSlotPreview ? 150f : 140f;
            float bottomPadding = showSlotPreview ? 16f : 14f;
            float nameHeight = showSlotPreview ? 32f : 36f;
            float statHeight = showSlotPreview ? 20f : 24f;
            float detailsGap = showSlotPreview ? 0f : 4f;
            float detailsSpacing = showSlotPreview ? 4f : 12f;
            float detailsAreaHeight = showSlotPreview
                ? detailsSpacing + nameHeight + statHeight * 2f + detailsGap * 2f
                : detailsSpacing + 32f;
            float buttonSize = Mathf.Clamp(layout.avatarArrowSize, 54f, 68f);
            float sideGap = Mathf.Clamp(layout.avatarArrowGap, 8f, 14f);
            float horizontalButtonReserve = showSlotPreview ? 0f : buttonSize * 2f + sideGap * 2f;
            float frameBottom = bottomPadding + detailsAreaHeight;
            float frameTop = height - topPadding;
            float frameMaxWidth = Mathf.Max(120f, width - sidePadding * 2f - horizontalButtonReserve);
            float frameMaxHeight = Mathf.Max(120f, frameTop - frameBottom);
            float frameSize = Mathf.Clamp(Mathf.Min(frameMaxWidth, frameMaxHeight), avatarMin + 44f, avatarMax + 56f);
            const float avatarFillRatio = 0.78f;
            float avatarSize = Mathf.Clamp(frameSize * avatarFillRatio, avatarMin, avatarMax);
            float avatarInset = (frameSize - avatarSize) * 0.5f;
            float frameShiftX = Mathf.Clamp(frameSize * 0.20f, 24f, 56f);
            float frameShiftY = Mathf.Clamp(frameSize * 0.35f, 36f, 84f);
            float frameBaseX = (width - frameSize) * 0.5f;
            float frameBaseY = Mathf.Max(frameBottom, frameTop - frameSize);
            float frameX = Mathf.Clamp(frameBaseX + frameShiftX, 0f, width - frameSize);
            float maxFrameY = Mathf.Max(frameBottom, height - frameSize - 4f);
            float frameY = Mathf.Clamp(frameBaseY + frameShiftY, frameBottom, maxFrameY);
            float frameVisualSize = frameSize * 0.95f;
            float frameVisualX = frameX + (frameSize - frameVisualSize);
            float frameVisualY = frameY;
            float previewX = frameX + avatarInset;
            float previewY = frameY + avatarInset;
            float buttonY = frameY + frameSize * 0.5f - buttonSize * 0.5f;
            float leftButtonX = Mathf.Max(8f, frameX - buttonSize - sideGap);
            float rightButtonX = Mathf.Min(width - buttonSize - 8f, frameX + frameSize + sideGap);

            SetObjectActive(leftPaneRect.Find("AvatarTitle") != null ? leftPaneRect.Find("AvatarTitle").gameObject : null, false);
            SetRect(avatarPreviewFrame != null ? avatarPreviewFrame.rectTransform : null, frameVisualX, frameVisualY, frameVisualSize, frameVisualSize);
            SetRect(avatarPreview != null ? avatarPreview.rectTransform : null, previewX, previewY, avatarSize, avatarSize);
            SetRect(previousAvatarButton != null ? previousAvatarButton.transform as RectTransform : null, leftButtonX, buttonY, buttonSize, buttonSize);
            SetRect(nextAvatarButton != null ? nextAvatarButton.transform as RectTransform : null, rightButtonX, buttonY, buttonSize, buttonSize);

            if (avatarPreview != null)
                avatarPreview.transform.SetAsFirstSibling();

            if (avatarPreviewFrame != null && avatarPreview != null)
                avatarPreviewFrame.transform.SetSiblingIndex(avatarPreview.transform.GetSiblingIndex() + 1);

            float detailsWidth = Mathf.Max(160f, frameVisualSize);
            float textShiftX = detailsWidth * 0.10f;
            float detailsX = Mathf.Max(sidePadding, frameVisualX + (frameVisualSize - detailsWidth) * 0.5f - textShiftX);
            float safeBottomY = bottomPadding + 10f;
            float profileTextBlockHeight = nameHeight + statHeight * 2f + detailsGap * 2f;
            float profileTextBottom = Mathf.Max(safeBottomY, frameVisualY - profileTextBlockHeight - detailsSpacing);
            float ageY = profileTextBottom;
            float levelY = ageY + statHeight + detailsGap;
            float nameY = levelY + statHeight + detailsGap;
            float counterY = Mathf.Max(safeBottomY, frameVisualY - 32f - detailsSpacing);
            SetRect(avatarIndexText != null ? avatarIndexText.rectTransform : null, detailsX, counterY, detailsWidth, 32f);
            SetRect(slotProfileNameText != null ? slotProfileNameText.rectTransform : null, detailsX, nameY, detailsWidth, nameHeight);
            SetRect(slotProfileLevelText != null ? slotProfileLevelText.rectTransform : null, detailsX, levelY, detailsWidth, statHeight);
            SetRect(slotProfileAgeText != null ? slotProfileAgeText.rectTransform : null, detailsX, ageY, detailsWidth, statHeight);
        }

        private void LayoutRightPane(float width, float height)
        {
            if (rightPaneRect == null)
                return;

            LayoutTuningSettings layout = layoutTuning ?? new LayoutTuningSettings();
            float x = Mathf.Clamp(width * 0.035f, 10f, layout.detailsPaddingX);
            float fieldWidth = Mathf.Max(180f, width - x * 2f);
            float controlWidth = Mathf.Min(fieldWidth, Mathf.Clamp(width * 0.82f, 360f, 700f));
            float controlX = x + (fieldWidth - controlWidth) * 0.5f;
            float topInset = Mathf.Clamp(height * 0.032f, 12f, 24f);
            float smallGap = Mathf.Clamp(height * 0.016f, 8f, 14f);
            float mediumGap = Mathf.Clamp(height * 0.026f, 12f, 18f);
            float tabGap = Mathf.Clamp(width * 0.018f, 8f, layout.tabsGap);
            float tabHeight = Mathf.Clamp(height * 0.095f, 46f, layout.tabHeight);
            float fieldHeight = Mathf.Clamp(height * 0.1f, 48f, layout.fieldHeight);
            float compactFieldHeight = Mathf.Clamp(height * 0.088f, 44f, layout.compactFieldHeight);
            float loginSlotHeight = Mathf.Clamp(height * 0.16f, 72f, layout.loginSlotButtonHeight);
            float registerSlotHeight = Mathf.Clamp(height * 0.13f, 60f, layout.registerSlotButtonHeight);
            float primaryHeight = Mathf.Clamp(height * 0.095f, 50f, layout.primaryButtonHeight);
            float stepLabelHeight = Mathf.Clamp(height * 0.075f, 32f, 46f);
            float supportTextHeight = Mathf.Clamp(height * 0.065f, 28f, 42f);
            float sectionLabelHeight = Mathf.Clamp(height * 0.058f, 26f, 36f);
            float errorHeight = Mathf.Clamp(height * 0.12f, 44f, 72f);
            float bottomY = Mathf.Clamp(height * 0.018f, 12f, 18f);
            float errorY = bottomY + primaryHeight + 14f;
            bool hasError = errorText != null && !string.IsNullOrWhiteSpace(errorText.text);
            float contentBottomLimit = errorY + (hasError ? errorHeight + 10f : 24f);
            float y = height - topInset - tabHeight;
            float tabsAreaWidth = Mathf.Min(controlWidth, 460f);
            float tabWidth = (tabsAreaWidth - tabGap) * 0.5f;
            float tabsX = controlX + (controlWidth - tabsAreaWidth) * 0.5f;
            float fieldX = controlX;
            float fieldAreaWidth = controlWidth;

            SetRect(dynastyTabButton != null ? dynastyTabButton.transform as RectTransform : null, tabsX, y, tabWidth, tabHeight);
            SetRect(profileTabButton != null ? profileTabButton.transform as RectTransform : null, tabsX + tabWidth + tabGap, y, tabWidth, tabHeight);
            y -= tabHeight + mediumGap;

            SetRect(registerStepText != null ? registerStepText.rectTransform : null, fieldX, y, fieldAreaWidth, stepLabelHeight);
            y -= stepLabelHeight + smallGap;

            SetRect(idPreviewText != null ? idPreviewText.rectTransform : null, fieldX, y, fieldAreaWidth, supportTextHeight);
            y -= supportTextHeight + mediumGap;

            bool showLoginSlotPicker = loginMode && loginSlotsLoaded;
            bool showAccountSlotOverview = HasAccountSlotOverview();
            bool showAccountFields = (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots) || (!loginMode && registerStep == RegisterStep.Account);
            bool showGenderFields = !loginMode && registerStep == RegisterStep.Gender;
            bool showSlotFields = showLoginSlotPicker || registerStep == RegisterStep.Details;
            bool showProfileFields = !loginMode && registerStep == RegisterStep.Details;
            bool showSlotPreview = loginMode && loginSlotsLoaded;
            bool showRemember = (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots) || (!loginMode && registerStep == RegisterStep.Account);
            bool showForgotPassword = loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots && !confirmingDeleteSlot;
            bool showDeleteButton = loginMode && loginSlotsLoaded && !confirmingDeleteSlot && IsSelectedLoginSlotOccupied() && !IsSelectedLoginSlotInUseByOtherDevice();
            bool showDeletePassword = loginMode && loginSlotsLoaded && confirmingDeleteSlot;
            bool showAvatar = showGenderFields || showProfileFields || showSlotPreview;
            bool showAvatarPicker = showProfileFields || (showGenderFields && (selectedGender == PlayerGender.Male || selectedGender == PlayerGender.Female));
            bool showBack = loginSlotsLoaded || (!loginMode && registerStep != RegisterStep.Account) || creatingSlotForExistingAccount;

            SetObjectActive(avatarPreview != null ? avatarPreview.gameObject : null, showAvatar);
            SetObjectActive(avatarPreviewFrame != null ? avatarPreviewFrame.gameObject : null, showAvatar);
            SetObjectActive(previousAvatarButton != null ? previousAvatarButton.gameObject : null, showAvatarPicker);
            SetObjectActive(nextAvatarButton != null ? nextAvatarButton.gameObject : null, showAvatarPicker);
            SetObjectActive(avatarIndexText != null ? avatarIndexText.gameObject : null, showAvatarPicker);
            SetObjectActive(slotProfileNameText != null ? slotProfileNameText.gameObject : null, showSlotPreview);
            SetObjectActive(slotProfileLevelText != null ? slotProfileLevelText.gameObject : null, showSlotPreview);
            SetObjectActive(slotProfileAgeText != null ? slotProfileAgeText.gameObject : null, showSlotPreview);

            SetObjectActive(dynastyInput != null ? dynastyInput.gameObject : null, !loginMode && registerStep == RegisterStep.Account);
            SetObjectActive(emailInput != null ? emailInput.gameObject : null, showAccountFields);
            SetObjectActive(passwordInput != null ? passwordInput.gameObject : null, showAccountFields || showDeletePassword);
            SetObjectActive(rememberToggle != null ? rememberToggle.gameObject : null, showRemember);
            SetObjectActive(continueButton != null ? continueButton.gameObject : null, !loginMode);
            SetObjectActive(loginButton != null ? loginButton.gameObject : null, loginMode);
            SetObjectActive(forgotPasswordButton != null ? forgotPasswordButton.gameObject : null, showForgotPassword);
            SetObjectActive(deleteSlotButton != null ? deleteSlotButton.gameObject : null, showDeleteButton);
            SetObjectActive(backButton != null ? backButton.gameObject : null, showBack);

            SetObjectActive(slotLabelText != null ? slotLabelText.gameObject : null, showSlotFields);
            SetObjectActive(slotOneButton != null ? slotOneButton.gameObject : null, showSlotFields);
            SetObjectActive(slotTwoButton != null ? slotTwoButton.gameObject : null, showSlotFields);
            SetObjectActive(slotThreeButton != null ? slotThreeButton.gameObject : null, showSlotFields);
            SetObjectActive(slotOneAvatarImage != null ? slotOneAvatarImage.gameObject : null, showAccountSlotOverview && GetSlotInfo(1).Occupied);
            SetObjectActive(slotTwoAvatarImage != null ? slotTwoAvatarImage.gameObject : null, showAccountSlotOverview && GetSlotInfo(2).Occupied);
            SetObjectActive(slotThreeAvatarImage != null ? slotThreeAvatarImage.gameObject : null, showAccountSlotOverview && GetSlotInfo(3).Occupied);
            SetObjectActive(nameInput != null ? nameInput.gameObject : null, showProfileFields);
            SetObjectActive(ageInput != null ? ageInput.gameObject : null, showProfileFields);
            SetObjectActive(rightPaneRect.Find("GenderLabel") != null ? rightPaneRect.Find("GenderLabel").gameObject : null, showGenderFields);
            SetObjectActive(maleButton != null ? maleButton.gameObject : null, showGenderFields);
            SetObjectActive(femaleButton != null ? femaleButton.gameObject : null, showGenderFields);
            SetObjectActive(otherButton != null ? otherButton.gameObject : null, false);

            if (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots)
            {
                SetRect(emailInput != null ? emailInput.transform as RectTransform : null, fieldX, y, fieldAreaWidth, fieldHeight);
                y -= fieldHeight + mediumGap;
                SetRect(passwordInput != null ? passwordInput.transform as RectTransform : null, fieldX, y, fieldAreaWidth, fieldHeight);
                y -= fieldHeight + mediumGap;
                float authActionWidth = Mathf.Min(Mathf.Max(220f, fieldAreaWidth * 0.62f), 320f);
                float forgotWidth = authActionWidth;
                float forgotX = fieldX + (fieldAreaWidth - forgotWidth) * 0.5f;
                float forgotHeight = Mathf.Clamp(height * 0.08f, 42f, 54f);
                SetRect(forgotPasswordButton != null ? forgotPasswordButton.transform as RectTransform : null, forgotX, Mathf.Max(contentBottomLimit, y), forgotWidth, forgotHeight);
            }
            else if (loginMode)
            {
                SetRect(slotLabelText != null ? slotLabelText.rectTransform : null, fieldX, y, fieldAreaWidth, sectionLabelHeight);
                y -= sectionLabelHeight + mediumGap;

                float slotButtonGap = Mathf.Clamp(width * 0.012f, 6f, 12f);
                float slotAreaWidth = Mathf.Min(fieldAreaWidth, 600f);
                float slotButtonWidth = (slotAreaWidth - slotButtonGap * 2f) / 3f;
                float slotRowWidth = slotButtonWidth * 3f + slotButtonGap * 2f;
                float slotX = fieldX + (fieldAreaWidth - slotRowWidth) * 0.5f;
                SetRect(slotOneButton != null ? slotOneButton.transform as RectTransform : null, slotX, y, slotButtonWidth, loginSlotHeight);
                SetRect(slotTwoButton != null ? slotTwoButton.transform as RectTransform : null, slotX + slotButtonWidth + slotButtonGap, y, slotButtonWidth, loginSlotHeight);
                SetRect(slotThreeButton != null ? slotThreeButton.transform as RectTransform : null, slotX + (slotButtonWidth + slotButtonGap) * 2f, y, slotButtonWidth, loginSlotHeight);
                y -= loginSlotHeight + mediumGap;

                if (confirmingDeleteSlot)
                {
                    SetRect(passwordInput != null ? passwordInput.transform as RectTransform : null, fieldX, Mathf.Max(contentBottomLimit, y), fieldAreaWidth, fieldHeight);
                }
                else
                {
                    float deleteWidth = Mathf.Min(Mathf.Max(220f, fieldAreaWidth * 0.7f), 420f);
                    float deleteX = fieldX + (fieldAreaWidth - deleteWidth) * 0.5f;
                    float deleteHeight = Mathf.Clamp(height * 0.082f, 44f, 56f);
                    SetRect(deleteSlotButton != null ? deleteSlotButton.transform as RectTransform : null, deleteX, Mathf.Max(contentBottomLimit + 8f, y), deleteWidth, deleteHeight);
                }

                SetRect(rememberToggle != null ? rememberToggle.transform as RectTransform : null, fieldX, contentBottomLimit, fieldAreaWidth, 40f);
            }
            else if (registerStep == RegisterStep.Account)
            {
                SetRect(dynastyInput != null ? dynastyInput.transform as RectTransform : null, fieldX, y, fieldAreaWidth, fieldHeight);
                y -= fieldHeight + mediumGap;
                SetRect(emailInput != null ? emailInput.transform as RectTransform : null, fieldX, y, fieldAreaWidth, fieldHeight);
                y -= fieldHeight + mediumGap;
                SetRect(passwordInput != null ? passwordInput.transform as RectTransform : null, fieldX, y, fieldAreaWidth, fieldHeight);
                SetRect(rememberToggle != null ? rememberToggle.transform as RectTransform : null, fieldX, contentBottomLimit, fieldAreaWidth, 40f);
            }
            else if (registerStep == RegisterStep.Gender)
            {
                SetRect(rightPaneRect.Find("GenderLabel") as RectTransform, fieldX, y, fieldAreaWidth, sectionLabelHeight);
                y -= sectionLabelHeight + mediumGap;

                float buttonGap = Mathf.Clamp(width * 0.024f, 10f, 16f);
                float genderAreaWidth = Mathf.Min(fieldAreaWidth, 400f);
                float genderButtonWidth = (genderAreaWidth - buttonGap) * 0.5f;
                float genderRowWidth = genderButtonWidth * 2f + buttonGap;
                float genderX = fieldX + (fieldAreaWidth - genderRowWidth) * 0.5f;
                float genderHeight = Mathf.Clamp(height * 0.11f, 54f, 70f);
                float genderOffsetY = Mathf.Max(genderHeight * 0.55f, height * 0.10f);
                SetRect(maleButton != null ? maleButton.transform as RectTransform : null, genderX, y - genderOffsetY, genderButtonWidth, genderHeight);
                SetRect(femaleButton != null ? femaleButton.transform as RectTransform : null, genderX + genderButtonWidth + buttonGap, y - genderOffsetY, genderButtonWidth, genderHeight);
            }
            else
            {
                SetRect(slotLabelText != null ? slotLabelText.rectTransform : null, fieldX, y, fieldAreaWidth, sectionLabelHeight);
                y -= sectionLabelHeight + mediumGap;

                float slotButtonGap = Mathf.Clamp(width * 0.012f, 6f, 12f);
                float slotAreaWidth = Mathf.Min(fieldAreaWidth, 600f);
                float slotButtonWidth = (slotAreaWidth - slotButtonGap * 2f) / 3f;
                float slotRowWidth = slotButtonWidth * 3f + slotButtonGap * 2f;
                float slotX = fieldX + (fieldAreaWidth - slotRowWidth) * 0.5f;
                SetRect(slotOneButton != null ? slotOneButton.transform as RectTransform : null, slotX, y, slotButtonWidth, registerSlotHeight);
                SetRect(slotTwoButton != null ? slotTwoButton.transform as RectTransform : null, slotX + slotButtonWidth + slotButtonGap, y, slotButtonWidth, registerSlotHeight);
                SetRect(slotThreeButton != null ? slotThreeButton.transform as RectTransform : null, slotX + (slotButtonWidth + slotButtonGap) * 2f, y, slotButtonWidth, registerSlotHeight);
                y -= registerSlotHeight + mediumGap;

                SetRect(nameInput != null ? nameInput.transform as RectTransform : null, fieldX, y, fieldAreaWidth, fieldHeight);
                y -= fieldHeight + mediumGap;
                SetRect(ageInput != null ? ageInput.transform as RectTransform : null, fieldX, Mathf.Max(contentBottomLimit, y), Mathf.Min(240f, fieldAreaWidth), compactFieldHeight);
            }

            float errorWidth = Mathf.Min(fieldAreaWidth, 560f);
            float errorX = fieldX + (fieldAreaWidth - errorWidth) * 0.5f;
            SetRect(errorText != null ? errorText.rectTransform : null, errorX, errorY, errorWidth, errorHeight);

            float bottomGap = Mathf.Clamp(width * 0.024f, 10f, 16f);
            float bottomAreaWidth = Mathf.Min(fieldAreaWidth, 520f);
            float bottomButtonWidth = (bottomAreaWidth - bottomGap) * 0.5f;
            float bottomRowWidth = bottomButtonWidth * 2f + bottomGap;
            float bottomX = fieldX + (fieldAreaWidth - bottomRowWidth) * 0.5f;
            RectTransform primaryButtonRect = loginMode
                ? loginButton != null ? loginButton.transform as RectTransform : null
                : continueButton != null ? continueButton.transform as RectTransform : null;

            if (showBack)
            {
                SetRect(backButton != null ? backButton.transform as RectTransform : null, bottomX, bottomY, bottomButtonWidth, primaryHeight);
                SetRect(primaryButtonRect, bottomX + bottomButtonWidth + bottomGap, bottomY, bottomButtonWidth, primaryHeight);
            }
            else
            {
                float singleButtonWidth = loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots
                    ? Mathf.Min(Mathf.Max(220f, fieldAreaWidth * 0.62f), 320f)
                    : Mathf.Min(fieldAreaWidth, 420f);
                float singleButtonX = fieldX + (fieldAreaWidth - singleButtonWidth) * 0.5f;
                SetRect(loginButton != null ? loginButton.transform as RectTransform : null, singleButtonX, bottomY, singleButtonWidth, primaryHeight);
                SetRect(continueButton != null ? continueButton.transform as RectTransform : null,
                    loginMode || registerStep != RegisterStep.Account ? singleButtonX : bottomX + bottomButtonWidth + bottomGap,
                    bottomY,
                    loginMode || registerStep != RegisterStep.Account ? singleButtonWidth : bottomButtonWidth,
                    primaryHeight);
            }
        }

        private GameObject CreatePane(Transform parent, string objectName, Color color)
        {
            GameObject pane = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pane.transform.SetParent(parent, false);

            RectTransform rect = pane.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);

            Image image = pane.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.clear;
            image.raycastTarget = false;
            return pane;
        }

        private Rect GetProfileWindowContentRect(float windowWidth, float windowHeight)
        {
            if (windowRect == null)
                return new Rect(56f, 92f, Mathf.Max(100f, windowWidth - 112f), Mathf.Max(100f, windowHeight - 252f));

            float x = windowWidth * ProfileWindowInnerLeft;
            float y = windowHeight * ProfileWindowInnerBottom;
            float width = windowWidth * (1f - ProfileWindowInnerLeft - ProfileWindowInnerRight);
            float height = windowHeight * (1f - ProfileWindowInnerTop - ProfileWindowInnerBottom);
            return new Rect(x, y, Mathf.Max(100f, width), Mathf.Max(100f, height));
        }

        private static void MakePaneTransparent(RectTransform paneRect)
        {
            if (paneRect == null)
                return;

            Image paneImage = paneRect.GetComponent<Image>();
            if (paneImage != null)
            {
                paneImage.sprite = null;
                paneImage.color = Color.clear;
                paneImage.raycastTarget = false;
            }

            Transform background = paneRect.Find("Background");
            if (background != null)
                background.gameObject.SetActive(false);
        }

        private TMP_InputField CreateInputField(Transform parent, string objectName, string placeholder)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SafeProfileInputField), typeof(SafeMobileInputDragBlocker));
            root.transform.SetParent(parent, false);

            Image image = root.GetComponent<Image>();
            image.sprite = LoadBuiltinUiSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = new Color(0.11f, 0.15f, 0.22f, 0.97f);
            image.raycastTarget = true;

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(root.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            StretchWithPadding(textAreaRect, 22f, 6f, 22f, 6f);

            TextMeshProUGUI placeholderText = CreateText(textArea.transform, "Placeholder", placeholder, 28f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.52f));
            Stretch(placeholderText.rectTransform);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            MainLobbyButtonStyle.ApplyFont(placeholderText);

            TextMeshProUGUI text = CreateText(textArea.transform, "Text", string.Empty, 28f, FontStyles.Normal, Color.white);
            Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            MainLobbyButtonStyle.ApplyFont(text);

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textViewport = textAreaRect;
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.targetGraphic = image;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 18;
            input.shouldHideMobileInput = false;
            input.shouldHideSoftKeyboard = false;
            input.resetOnDeActivation = false;
            return input;
        }

        private Button CreateButton(Transform parent, string objectName, string label, float fontSize)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, fontSize, FontStyles.Bold, Color.white);
            Stretch(text.rectTransform);
            text.margin = new Vector4(10f, 2f, 10f, 4f);
            text.alignment = TextAlignmentOptions.Center;
            MainLobbyButtonStyle.Apply(button);
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.55f);
            text.fontSizeMax = fontSize;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return button;
        }

        private Toggle CreateToggle(Transform parent, string objectName, string label)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);

            Toggle toggle = root.GetComponent<Toggle>();

            GameObject box = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.transform.SetParent(root.transform, false);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(0f, 0f);
            boxRect.sizeDelta = new Vector2(40f, 40f);

            Image boxImage = box.GetComponent<Image>();
            boxImage.sprite = LoadBuiltinUiSprite();
            boxImage.type = Image.Type.Simple;
            boxImage.preserveAspect = false;
            boxImage.color = new Color(0.13f, 0.16f, 0.22f, 1f);
            boxImage.raycastTarget = true;

            GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            check.transform.SetParent(box.transform, false);
            RectTransform checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.anchoredPosition = Vector2.zero;
            checkRect.sizeDelta = new Vector2(24f, 24f);

            Image checkImage = check.GetComponent<Image>();
            checkImage.color = new Color(0.22f, 0.52f, 0.86f, 1f);
            checkImage.raycastTarget = false;

            TextMeshProUGUI text = CreateText(root.transform, "Label", label, 22f, FontStyles.Bold, Color.white);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.margin = new Vector4(56f, 0f, 0f, 0f);
            Stretch(text.rectTransform);

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;
            return toggle;
        }

        private Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Image CreateSlotAvatarImage(Transform parent, string objectName)
        {
            Image image = CreateImage(parent, objectName, Color.white);
            image.preserveAspect = true;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 16f);
            rect.sizeDelta = new Vector2(40f, 40f);
            image.gameObject.SetActive(false);
            return image;
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, float fontSize, FontStyles style, Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(18f, fontSize * 0.72f);
            label.fontSizeMax = fontSize;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private void ReleaseActiveInputs()
        {
            if (dynastyInput != null)
                dynastyInput.DeactivateInputField();

            if (nameInput != null)
                nameInput.DeactivateInputField();

            if (ageInput != null)
                ageInput.DeactivateInputField();

            if (emailInput != null)
                emailInput.DeactivateInputField();

            if (passwordInput != null)
                passwordInput.DeactivateInputField();

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(null);
        }

        private void HideLegacyChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWithPadding(RectTransform rect, float left, float bottom, float right, float top)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            if (rect == null)
                return;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetObjectActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
                return;

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = label;
        }

        private static void SetButtonLabelVisible(Button button, bool visible)
        {
            if (button == null)
                return;

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.gameObject.SetActive(visible);
        }

        private static void SetNamedText(RectTransform root, string childName, string value)
        {
            if (root == null)
                return;

            Transform child = root.Find(childName);
            if (child == null)
                return;

            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = value;
        }

        private static void SetInputPlaceholder(TMP_InputField input, string value)
        {
            if (input == null)
                return;

            Transform placeholder = input.transform.Find("Text Area/Placeholder");
            TextMeshProUGUI text = placeholder != null
                ? placeholder.GetComponent<TextMeshProUGUI>()
                : null;

            if (text != null)
                text.text = value;
        }

        private static void SetToggleLabel(Toggle toggle, string value)
        {
            if (toggle == null)
                return;

            Transform label = toggle.transform.Find("Label");
            TextMeshProUGUI text = label != null ? label.GetComponent<TextMeshProUGUI>() : null;
            if (text != null)
                text.text = value;
        }
    }
}
