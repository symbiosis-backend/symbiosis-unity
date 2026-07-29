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
        private const string GermanLanguageButtonResourcePath = "Mahjong/Sprites/ButtonDE";
        private const string SettingsWindowResourcePath = "Mahjong/Sprites/MainSettings/MainSettingsWindow";
        private const string ProfileAvatarFrameResourcePath = "ProfileAvatars/ProfileAvatarFrameGenerated";
        private const string DeveloperAccountLogin = "ozkullar";
        private const string DeveloperAccountEmail = "ozkullar@developer.symbiosis.local";
        private const float ProfileWindowSpriteWidth = 1513f;
        private const float ProfileWindowSpriteHeight = 1024f;
        private const float ProfileWindowInnerLeft = 70f / ProfileWindowSpriteWidth;
        private const float ProfileWindowInnerRight = 70f / ProfileWindowSpriteWidth;
        private const float ProfileWindowInnerTop = 88f / ProfileWindowSpriteHeight;
        private const float ProfileWindowInnerBottom = 74f / ProfileWindowSpriteHeight;
        private const float ProfileWindowFullscreenOverscanX = 1.16f;
        private const float ProfileWindowFullscreenOverscanY = 1.28f;

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
        [SerializeField, Min(0)] private int profileDeleteMinAgeHours = 24;
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
        private Button germanLanguageButton;
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
        private Toggle termsToggle;
        private Button termsButton;
        private Image slotOneAvatarImage;
        private Image slotTwoAvatarImage;
        private Image slotThreeAvatarImage;
        private TextMeshProUGUI registerStepText;
        private TextMeshProUGUI idPreviewText;
        private TextMeshProUGUI dynastyInputLabel;
        private TextMeshProUGUI emailInputLabel;
        private TextMeshProUGUI passwordInputLabel;
        private TextMeshProUGUI nicknameInputLabel;
        private TextMeshProUGUI ageInputLabel;
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
        private Sprite cachedGermanLanguageButtonSprite;
        private static Sprite cachedBuiltinUiSprite;
        private static Sprite cachedFallbackUiSprite;
#if UNITY_EDITOR
        private bool editorRefreshQueued;
#endif

        private enum RegisterStep
        {
            Account,
            Gender,
            Details
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                QueueEditorRefresh();
#endif
                return;
            }

            EnsureLandscapeHierarchy();

            BindButtons();
            ConfigureInput();
            ApplyAvatarVisual();
            RefreshGenderButtons();
            SetError(string.Empty);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                QueueEditorRefresh();
#endif
                return;
            }

            EnsureLandscapeHierarchy();
            SetGeneratedRootVisible(true);

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
            if (termsToggle != null)
                termsToggle.isOn = LegalConsent.HasAcceptedCurrentVersion;

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
            loadingRememberedAccountSlots = LegalConsent.HasAcceptedCurrentVersion;
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

            // Keep the remembered login visible while the player reviews a new Terms version.
            // Continue/Login will accept the checked consent and resume the normal slot picker.
            if (!LegalConsent.HasAcceptedCurrentVersion)
                return true;

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
            SetGeneratedRootVisible(false);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            ReleaseActiveInputs();
            UnsubscribeLanguageChanges();
            SetGeneratedRootVisible(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying)
            {
                if (ShouldApplyEditorLayout())
                {
#if UNITY_EDITOR
                    QueueEditorRefresh();
#endif
                }

                return;
            }

            ApplyResponsiveLayout();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

#if UNITY_EDITOR
            QueueEditorRefresh();
#endif
        }

#if UNITY_EDITOR
        private void QueueEditorRefresh()
        {
            if (editorRefreshQueued)
                return;

            editorRefreshQueued = true;
            EditorApplication.delayCall += RunQueuedEditorRefresh;
        }

        private void RunQueuedEditorRefresh()
        {
            editorRefreshQueued = false;
            if (this == null || Application.isPlaying)
                return;

            EnsureLandscapeHierarchy();
            RefreshLocalizedText();
            if (ShouldApplyEditorLayout())
                ApplyResponsiveLayout();
        }
#endif

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

            EnsureLegalConsentControls();
        }

        private void EnsureLegalConsentControls()
        {
            if (rightPaneRect == null)
                return;

            if (termsToggle == null)
                termsToggle = FindComponentByPath<Toggle>(rightPaneRect, "LegalConsentToggle");
            if (termsButton == null)
                termsButton = FindComponentByPath<Button>(rightPaneRect, "ViewTermsButton");

            if (termsToggle == null)
                termsToggle = CreateToggle(rightPaneRect, "LegalConsentToggle", LegalConsent.ConsentLabel());
            if (termsButton == null)
                termsButton = CreateButton(rightPaneRect, "ViewTermsButton", LegalConsent.ViewTermsLabel(), 18f);

            termsToggle.isOn = LegalConsent.HasAcceptedCurrentVersion;
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
            Transform fullscreenParent = GetProfileFullscreenParent();
            if (generatedRoot == null && fullscreenParent != null)
                generatedRoot = fullscreenParent.Find("ProfileSetupLandscapeRoot") as RectTransform;

            if (generatedRoot == null)
                return;

            if (fullscreenParent != null && generatedRoot.parent != fullscreenParent)
            {
                generatedRoot.SetParent(fullscreenParent, false);
                generatedRoot.SetAsLastSibling();
            }

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
            ApplyProfileAvatarFrame();
            previousAvatarButton = FindComponentByPath<Button>(leftPaneRect, "PreviousAvatarButton");
            nextAvatarButton = FindComponentByPath<Button>(leftPaneRect, "NextAvatarButton");
            avatarIndexText = FindComponentByPath<TextMeshProUGUI>(leftPaneRect, "AvatarCounter");
            slotProfileNameText = FindComponentByPath<TextMeshProUGUI>(leftPaneRect, "SlotProfileName");
            slotProfileLevelText = FindComponentByPath<TextMeshProUGUI>(leftPaneRect, "SlotProfileLevel");
            slotProfileAgeText = FindComponentByPath<TextMeshProUGUI>(leftPaneRect, "SlotProfileAge");

            russianLanguageButton = FindComponentByPath<Button>(windowRect, "LanguageRuButton");
            englishLanguageButton = FindComponentByPath<Button>(windowRect, "LanguageEnButton");
            turkishLanguageButton = FindComponentByPath<Button>(windowRect, "LanguageTrButton");
            germanLanguageButton = FindComponentByPath<Button>(windowRect, "LanguageDeButton");
            if (germanLanguageButton == null)
            {
                germanLanguageButton = CreateButton(windowRect, "LanguageDeButton", "DE", 20f);
                ApplyLanguageButtonSprite(germanLanguageButton, LoadGermanLanguageButtonSprite());
            }

            dynastyTabButton = FindComponentByPath<Button>(windowRect, "RegisterTabButton") ?? FindComponentByPath<Button>(rightPaneRect, "RegisterTabButton");
            profileTabButton = FindComponentByPath<Button>(windowRect, "LoginTabButton") ?? FindComponentByPath<Button>(rightPaneRect, "LoginTabButton");
            if (dynastyTabButton != null && dynastyTabButton.transform.parent != windowRect)
                dynastyTabButton.transform.SetParent(windowRect, false);
            if (profileTabButton != null && profileTabButton.transform.parent != windowRect)
                profileTabButton.transform.SetParent(windowRect, false);
            registerStepText = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "RegisterStepText");
            idPreviewText = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "IdPreview");
            dynastyInputLabel = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "DynastyInputLabel");
            emailInputLabel = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "EmailInputLabel");
            passwordInputLabel = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "PasswordInputLabel");
            nicknameInputLabel = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "NicknameInputLabel");
            ageInputLabel = FindComponentByPath<TextMeshProUGUI>(rightPaneRect, "AgeInputLabel");

            dynastyInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "DynastyInput");
            nameInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "NameInput");
            emailInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "EmailInput");
            passwordInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "PasswordInput");
            ageInput = FindComponentByPath<TMP_InputField>(rightPaneRect, "AgeInput");
            EnsureAccountInputLabels();

            rememberToggle = FindComponentByPath<Toggle>(rightPaneRect, "RememberProfileToggle");
            termsToggle = FindComponentByPath<Toggle>(rightPaneRect, "LegalConsentToggle");
            termsButton = FindComponentByPath<Button>(rightPaneRect, "ViewTermsButton");
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

        private void EnsureAccountInputLabels()
        {
            if (rightPaneRect == null)
                return;

            if (dynastyInputLabel == null)
                dynastyInputLabel = CreateAccountInputLabel(rightPaneRect, "DynastyInputLabel", DynastyNameText());

            if (emailInputLabel == null)
                emailInputLabel = CreateAccountInputLabel(rightPaneRect, "EmailInputLabel", EmailText());

            if (passwordInputLabel == null)
                passwordInputLabel = CreateAccountInputLabel(rightPaneRect, "PasswordInputLabel", PasswordText());

            if (nicknameInputLabel == null)
                nicknameInputLabel = CreateAccountInputLabel(rightPaneRect, "NicknameInputLabel", NicknameText());

            if (ageInputLabel == null)
                ageInputLabel = CreateAccountInputLabel(rightPaneRect, "AgeInputLabel", AgeInputText());
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
            SetNamedText(windowRect, "Title", CurrentProfileTitleText());
            SetNamedText(windowRect, "Subtitle", CurrentProfileSubtitleText());
            SetNamedText(leftPaneRect, "AvatarTitle", AvatarText());
            SetNamedText(rightPaneRect, "GenderLabel", GenderText());
            SetNamedText(rightPaneRect, "SlotLabel", ProfileSlotText());
            SetTextValue(dynastyInputLabel, DynastyNameText());
            SetTextValue(emailInputLabel, CurrentAccountIdentifierText());
            SetTextValue(passwordInputLabel, PasswordText());
            SetTextValue(nicknameInputLabel, NicknameText());
            SetTextValue(ageInputLabel, AgeInputText());

            if (idPreviewText != null)
                idPreviewText.text = AutoIdText();

            SetInputPlaceholder(dynastyInput, DynastyNameText());
            SetInputPlaceholder(nameInput, NicknameText());
            SetInputPlaceholder(emailInput, CurrentAccountIdentifierText());
            SetInputPlaceholder(passwordInput, PasswordText());
            SetInputPlaceholder(ageInput, AgeInputText());
            SetToggleLabel(rememberToggle, RememberProfileText());
            SetToggleLabel(termsToggle, LegalConsent.ConsentLabel());
            SetButtonLabel(termsButton, LegalConsent.ViewTermsLabel());
            SetButtonLabel(russianLanguageButton, "RU");
            SetButtonLabel(englishLanguageButton, "EN");
            SetButtonLabel(turkishLanguageButton, "TR");
            SetButtonLabel(germanLanguageButton, "DE");
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
            root.transform.SetParent(GetProfileFullscreenParent(), false);
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
            germanLanguageButton = CreateButton(window.transform, "LanguageDeButton", "DE", 20f);
            ApplyLanguageButtonSprites();

            GameObject leftPane = CreatePane(window.transform, "AvatarPane", new Color(0.115f, 0.14f, 0.185f, 1f));
            leftPaneRect = leftPane.GetComponent<RectTransform>();

            TextMeshProUGUI avatarTitle = CreateText(leftPane.transform, "AvatarTitle", AvatarText(), 30f, FontStyles.Bold, Color.white);
            Anchor(avatarTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            avatarPreview = CreateImage(leftPane.transform, "AvatarPreview", Color.white);
            avatarPreview.preserveAspect = true;
            avatarPreviewFrame = CreateImage(leftPane.transform, "AvatarFrame", Color.white);
            ApplyProfileAvatarFrame();

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

            dynastyTabButton = CreateButton(window.transform, "RegisterTabButton", RegisterText(), 22f);
            profileTabButton = CreateButton(window.transform, "LoginTabButton", LoginText(), 22f);

            registerStepText = CreateText(rightPane.transform, "RegisterStepText", string.Empty, 22f, FontStyles.Bold, new Color(0.78f, 0.86f, 1f));
            Anchor(registerStepText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            idPreviewText = CreateText(rightPane.transform, "IdPreview", AutoIdText(), 22f, FontStyles.Normal, new Color(0.68f, 0.78f, 0.92f, 1f));
            Anchor(idPreviewText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            dynastyInputLabel = CreateAccountInputLabel(rightPane.transform, "DynastyInputLabel", DynastyNameText());
            emailInputLabel = CreateAccountInputLabel(rightPane.transform, "EmailInputLabel", EmailText());
            passwordInputLabel = CreateAccountInputLabel(rightPane.transform, "PasswordInputLabel", PasswordText());
            nicknameInputLabel = CreateAccountInputLabel(rightPane.transform, "NicknameInputLabel", NicknameText());
            ageInputLabel = CreateAccountInputLabel(rightPane.transform, "AgeInputLabel", AgeInputText());

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
            termsToggle = CreateToggle(rightPane.transform, "LegalConsentToggle", LegalConsent.ConsentLabel());
            termsToggle.isOn = LegalConsent.HasAcceptedCurrentVersion;
            termsButton = CreateButton(rightPane.transform, "ViewTermsButton", LegalConsent.ViewTermsLabel(), 18f);

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

            if (germanLanguageButton != null)
                germanLanguageButton.onClick.AddListener(() => SelectLanguage(GameLanguage.German));

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

            if (termsButton != null)
                termsButton.onClick.AddListener(OpenTerms);

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

            if (germanLanguageButton != null)
                germanLanguageButton.onClick.RemoveAllListeners();

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

            if (termsButton != null)
                termsButton.onClick.RemoveListener(OpenTerms);

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

            if (creatingSlotForExistingAccount && registerStep == RegisterStep.Gender)
                ShowRegisterStep(RegisterStep.Details);
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
                    if (!EnsureLegalConsentAccepted())
                    {
                        ResetContinueState();
                        return;
                    }

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

            if (IsSelectedLoginSlotDeletionLocked())
            {
                SetError(ProfileDeleteLockedText());
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
                if (!EnsureLegalConsentAccepted())
                    return;

                continueInProgress = true;
                SetAccountButtonsInteractable(false);
                ReleaseActiveInputs();

                if (ProfileService.I == null)
                {
                    SetError(GameLocalization.Text("profile.error.service_missing"));
                    ResetContinueState();
                    return;
                }

                string validatedEmail = ValidateAndNormalizeAccountIdentifier(emailInput != null ? emailInput.text : string.Empty);
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

                if (IsSelectedLoginSlotDeletionLocked())
                {
                    SetError(ProfileDeleteLockedText());
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
                ResetLoginStateAfterInvalidCredentials(error);
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
                ResetLoginStateAfterInvalidCredentials(error);
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

            if (IsInvalidCredentialsError(error))
                return InvalidCredentialsText();

            if (error.IndexOf("profile not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("no profile", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AccountNotFoundText();
            }

            if (error.IndexOf("24", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("after creation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("deleted", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ProfileDeleteLockedText();
            }

            return error;
        }

        private void ResetLoginStateAfterInvalidCredentials(string error)
        {
            if (!IsInvalidCredentialsError(error))
                return;

            ProfileService.I?.ClearRememberedLogin();
            loginSlotsLoaded = false;
            loadingRememberedAccountSlots = false;
            loginSlots = Array.Empty<ProfileService.AccountSlotInfo>();
            cachedLoginPassword = string.Empty;
            confirmingDeleteSlot = false;

            if (rememberToggle != null)
                rememberToggle.isOn = false;

            if (passwordInput != null)
                passwordInput.text = string.Empty;
        }

        private static bool IsInvalidCredentialsError(string error)
        {
            return !string.IsNullOrWhiteSpace(error) &&
                   error.IndexOf("Invalid credentials", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string FormatRegisterError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return ServerErrorText();

            if (error.IndexOf("profile not found", StringComparison.OrdinalIgnoreCase) >= 0)
                return RegistrationExpiredText();

            if (error.IndexOf("No free profile slots", StringComparison.OrdinalIgnoreCase) >= 0)
                return NoFreeProfileSlotsText();

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
            if (loginSlotsLoaded)
            {
                int freeSlotIndex = FindFirstFreeSlot(loginSlots);
                if (freeSlotIndex == 0)
                {
                    SetError(NoFreeProfileSlotsText());
                    ResetContinueState();
                    return;
                }

                selectedSlotIndex = freeSlotIndex;
            }

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
            SetError(string.Empty);
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

        private bool IsSelectedLoginSlotDeletionLocked()
        {
            ProfileService.AccountSlotInfo slot = GetSlotInfo(selectedSlotIndex);
            return IsSlotDeletionLocked(slot);
        }

        private bool IsSlotDeletionLocked(ProfileService.AccountSlotInfo slot)
        {
            if (!slot.Occupied || profileDeleteMinAgeHours <= 0)
                return false;

            if (string.IsNullOrWhiteSpace(slot.CreatedAt))
                return true;

            if (!DateTimeOffset.TryParse(slot.CreatedAt, out DateTimeOffset createdAt))
                return true;

            return DateTimeOffset.UtcNow - createdAt.ToUniversalTime() < TimeSpan.FromHours(profileDeleteMinAgeHours);
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

        private static int FindFirstFreeSlot(ProfileService.AccountSlotInfo[] slots)
        {
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].Occupied)
                        return Mathf.Clamp(slots[i].SlotIndex, 1, 3);
                }
            }

            return 0;
        }

        private bool ShouldRememberProfile()
        {
            return rememberToggle == null || rememberToggle.isOn;
        }

        private bool EnsureLegalConsentAccepted()
        {
            if (LegalConsent.HasAcceptedCurrentVersion)
                return true;

            if (termsToggle == null || !termsToggle.isOn)
            {
                SetError(LegalConsent.ConsentRequiredError());
                return false;
            }

            LegalConsent.AcceptCurrentVersion();
            return true;
        }

        private static void OpenTerms()
        {
            Application.OpenURL(LegalConsent.TermsUrl);
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

        private string ValidateAndNormalizeAccountIdentifier(string rawIdentifier)
        {
            string value = string.IsNullOrWhiteSpace(rawIdentifier) ? string.Empty : rawIdentifier.Trim();
            if (string.IsNullOrEmpty(value))
            {
                SetError(EnterAccountIdentifierText());
                return null;
            }

            if (value.IndexOf('@') >= 0)
                return ValidateAndNormalizeEmail(value);

            if (value.Length < 2 || value.Length > 64)
            {
                SetError(AccountIdentifierInvalidText());
                return null;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                {
                    SetError(AccountIdentifierInvalidText());
                    return null;
                }
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
                emailInput.lineType = TMP_InputField.LineType.SingleLine;
                ConfigureAccountIdentifierInput();
            }

            if (passwordInput != null)
            {
                passwordInput.characterLimit = 64;
                passwordInput.contentType = TMP_InputField.ContentType.Password;
                passwordInput.lineType = TMP_InputField.LineType.SingleLine;
            }
        }

        private void ConfigureAccountIdentifierInput()
        {
            if (emailInput == null)
                return;

            bool acceptsDynastyLogin = loginMode;
            emailInput.contentType = acceptsDynastyLogin
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.EmailAddress;
            emailInput.keyboardType = acceptsDynastyLogin
                ? TouchScreenKeyboardType.Default
                : TouchScreenKeyboardType.EmailAddress;
            emailInput.characterValidation = TMP_InputField.CharacterValidation.None;

            string label = CurrentAccountIdentifierText();
            SetTextValue(emailInputLabel, label);
            SetInputPlaceholder(emailInput, label);
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
            ApplyLanguageButton(germanLanguageButton, GameLanguage.German);
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
            ApplyLanguageButtonSprite(germanLanguageButton, LoadGermanLanguageButtonSprite());
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
                GameLanguage.German => LoadGermanLanguageButtonSprite(),
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

        private Sprite LoadGermanLanguageButtonSprite()
        {
            if (cachedGermanLanguageButtonSprite != null)
                return cachedGermanLanguageButtonSprite;

            cachedGermanLanguageButtonSprite = LoadFirstSprite(GermanLanguageButtonResourcePath);
            return cachedGermanLanguageButtonSprite;
        }

        private static Sprite LoadFirstSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        private void ApplyProfileAvatarFrame()
        {
            if (avatarPreviewFrame == null)
                return;

            Sprite frameSprite = LoadFirstSprite(ProfileAvatarFrameResourcePath);
            if (frameSprite != null)
            {
                avatarPreviewFrame.sprite = frameSprite;
                avatarPreviewFrame.type = Image.Type.Simple;
                avatarPreviewFrame.preserveAspect = false;
                avatarPreviewFrame.color = Color.white;
            }
            else
            {
                MainLobbyButtonStyle.ApplyAvatarCard(avatarPreviewFrame);
            }

            avatarPreviewFrame.raycastTarget = false;

            if (avatarPreview != null &&
                avatarPreview.transform.parent == avatarPreviewFrame.transform.parent &&
                avatarPreviewFrame.transform.GetSiblingIndex() <= avatarPreview.transform.GetSiblingIndex())
            {
                avatarPreviewFrame.transform.SetSiblingIndex(avatarPreview.transform.GetSiblingIndex() + 1);
            }
        }

        private static Sprite LoadBuiltinUiSprite()
        {
            if (cachedBuiltinUiSprite != null)
                return cachedBuiltinUiSprite;

            cachedBuiltinUiSprite = LoadCompatibleBuiltinUiSprite();
            if (cachedBuiltinUiSprite == null)
                cachedBuiltinUiSprite = CreateFallbackUiSprite();

            return cachedBuiltinUiSprite;
        }

        private static Sprite LoadCompatibleBuiltinUiSprite()
        {
#if UNITY_EDITOR
            cachedBuiltinUiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (cachedBuiltinUiSprite != null)
                return cachedBuiltinUiSprite;

            cachedBuiltinUiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (cachedBuiltinUiSprite != null)
                return cachedBuiltinUiSprite;
#endif

            return null;
        }

        private static Sprite CreateFallbackUiSprite()
        {
            if (cachedFallbackUiSprite != null)
                return cachedFallbackUiSprite;

            Texture2D texture = Texture2D.whiteTexture;
            if (texture == null)
                return null;

            cachedFallbackUiSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            cachedFallbackUiSprite.name = "ProfileSetupUI_FallbackSprite";
            return cachedFallbackUiSprite;
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
                Sprite sprite = GetAccountSlotAvatar(slot);
                avatarPreview.sprite = sprite;
                avatarPreview.enabled = sprite != null;
            }

            string nickname = string.IsNullOrWhiteSpace(slot.Nickname) ? ProfileLabelText() : slot.Nickname.Trim();
            string age = slot.Age > 0 ? slot.Age.ToString() : "-";
            SetSlotProfileText(nickname, LevelText("1"), IsSlotDeletionLocked(slot) ? ProfileDeleteLockedShortText() : AgeText(age));
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
                    return $"{nickname}\n{ProfileUiText("Слот", "Slot", "Yuva", "Platz")} {slotIndex}";
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
                ? GetAccountSlotAvatar(slot)
                : null;

            image.sprite = sprite;
            image.enabled = sprite != null;
            SetObjectActive(image.gameObject, sprite != null);
        }

        private Sprite GetAccountSlotAvatar(ProfileService.AccountSlotInfo slot)
        {
            if (IsDeveloperAccountIdentifier(cachedLoginEmail))
            {
                Sprite creatorSprite = ProfileAvatarResources.GetCreatorSprite(slot.Nickname);
                if (creatorSprite != null)
                    return creatorSprite;
            }

            return ProfileAvatarResources.GetRegularSprite(slot.Gender, slot.AvatarId);
        }

        private static bool IsDeveloperAccountIdentifier(string identifier)
        {
            string normalized = string.IsNullOrWhiteSpace(identifier)
                ? string.Empty
                : identifier.Trim().ToLowerInvariant();

            return normalized == DeveloperAccountLogin || normalized == DeveloperAccountEmail;
        }

        private void ApplySlotTextLayout(Button button, int slotIndex)
        {
            if (button == null)
                return;

            RectTransform buttonRect = button.transform as RectTransform;
            float buttonWidth = buttonRect != null ? Mathf.Max(1f, buttonRect.rect.width) : 1f;
            float buttonHeight = buttonRect != null ? Mathf.Max(1f, buttonRect.rect.height) : 1f;
            bool largeSlot = buttonHeight >= 100f;

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            Image avatarImage = GetSlotAvatarImage(slotIndex);
            RectTransform avatarRect = avatarImage != null ? avatarImage.rectTransform : null;
            if (avatarRect != null)
            {
                float avatarSize = Mathf.Clamp(Mathf.Min(buttonWidth * 0.5f, buttonHeight * 0.48f), largeSlot ? 112f : 52f, largeSlot ? 148f : 68f);
                avatarRect.anchorMin = new Vector2(0.5f, 1f);
                avatarRect.anchorMax = new Vector2(0.5f, 1f);
                avatarRect.pivot = new Vector2(0.5f, 1f);
                avatarRect.anchoredPosition = new Vector2(0f, largeSlot ? -22f : -12f);
                avatarRect.sizeDelta = new Vector2(avatarSize, avatarSize);
            }

            if (text == null)
                return;

            if (HasAccountSlotOverview() && GetSlotInfo(slotIndex).Occupied)
            {
                text.alignment = TextAlignmentOptions.Bottom;
                float avatarSize = avatarRect != null ? avatarRect.sizeDelta.y : Mathf.Clamp(buttonHeight * 0.48f, 88f, 132f);
                float topMargin = largeSlot ? Mathf.Clamp(avatarSize + 34f, 138f, buttonHeight * 0.66f) : 58f;
                text.margin = new Vector4(14f, topMargin, 14f, largeSlot ? 18f : 8f);
                SetTextSize(text, largeSlot ? 32f : 22f);
            }
            else if (HasAccountSlotOverview())
            {
                text.alignment = TextAlignmentOptions.Center;
                text.margin = new Vector4(16f, largeSlot ? 18f : 0f, 16f, largeSlot ? 18f : 0f);
                SetTextSize(text, largeSlot ? 32f : 22f);
            }
            else
            {
                text.alignment = TextAlignmentOptions.Center;
                text.margin = new Vector4(16f, 0f, 16f, 0f);
            }

            text.textWrappingMode = HasAccountSlotOverview() && GetSlotInfo(slotIndex).Occupied
                ? TextWrappingModes.Normal
                : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        private string ProfileUiText(string russian, string english, string turkish, string german = null)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Turkish;
            return language switch
            {
                GameLanguage.Russian => russian,
                GameLanguage.English => english,
                GameLanguage.German => german ?? english,
                _ => turkish
            };
        }

        private string CurrentProfileTitleText()
        {
            if (confirmingDeleteSlot)
                return ConfirmDeleteText();

            if (loginMode)
                return LoginText();

            return ProfileTitleText();
        }

        private string CurrentProfileSubtitleText()
        {
            if (confirmingDeleteSlot)
                return ProfileUiText("Введите пароль, чтобы удалить выбранный профиль.", "Enter the password to delete the selected profile.", "Secili profili silmek icin sifreyi gir.", "Gib das Passwort ein, um das gewahlte Profil zu loschen.");

            if (loginMode && loginSlotsLoaded)
                return ProfileUiText("Выберите профиль или создайте новый слот.", "Choose a profile or create a new slot.", "Bir profil sec veya yeni yuva olustur.", "Wahle ein Profil oder erstelle einen neuen Platz.");

            if (loginMode)
                return ProfileUiText("Введите почту и пароль династии.", "Enter your dynasty email and password.", "Hanedan e-posta ve sifreni gir.", "Gib E-Mail und Passwort deiner Dynastie ein.");

            if (registerStep == RegisterStep.Account)
                return ProfileUiText("Заполните данные для создания профиля.", "Fill in the details to create your profile.", "Profil olusturmak icin bilgileri doldur.", "Full die Daten aus, um dein Profil zu erstellen.");

            if (registerStep == RegisterStep.Gender)
                return ProfileUiText("Выберите пол профиля.", "Choose the profile gender.", "Profil cinsiyetini sec.", "Wahle das Geschlecht des Profils.");

            if (registerStep == RegisterStep.Details)
                return ProfileUiText("Выберите аватар, никнейм и возраст профиля.", "Choose the profile avatar, nickname, and age.", "Profil avatari, takma adi ve yasi sec.", "Wahle Avatar, Nickname und Alter des Profils.");

            return ProfileSubtitleText();
        }

        private string ProfileTitleText() => ProfileUiText("Создать профиль", "Create Profile", "Profil Olustur", "Profil erstellen");
        private string ProfileSubtitleText() => ProfileUiText("Выберите аватар и заполните данные профиля.", "Choose your avatar and fill in the profile details.", "Avatarini sec ve profil bilgilerini doldur.", "Wahle deinen Avatar und fulle die Profildaten aus.");
        private string AvatarText() => ProfileUiText("Аватар", "Avatar", "Avatar");
        private string RegisterText() => ProfileUiText("Регистрация", "Register", "Kayit", "Registrieren");
        private string LoginText() => ProfileUiText("Войти", "Login", "Giris", "Anmelden");
        private string ForgotPasswordText() => ProfileUiText("Восстановить пароль", "Recover Password", "Sifreyi Kurtar", "Passwort wiederherstellen");
        private string DynastyNameText() => ProfileUiText("Название династии", "Dynasty Name", "Hanedan Adi", "Dynastie-Name");
        private string NicknameText() => ProfileUiText("Никнейм", "Nickname", "Takma ad");
        private string EmailText() => ProfileUiText("Почта", "Email", "E-posta");
        private string AccountIdentifierText() => ProfileUiText("Ханедан / почта", "Dynasty / Email", "Hanedan / E-posta", "Dynastie / E-Mail");
        private string CurrentAccountIdentifierText() => loginMode ? AccountIdentifierText() : EmailText();
        private string PasswordText() => ProfileUiText("Пароль", "Password", "Sifre", "Passwort");
        private string AgeInputText() => ProfileUiText("Возраст", "Age", "Yas", "Alter");
        private string GenderText() => ProfileUiText("Пол", "Gender", "Cinsiyet", "Geschlecht");
        private string MaleText() => ProfileUiText("Мужчина", "Male", "Erkek", "Mann");
        private string FemaleText() => ProfileUiText("Женщина", "Female", "Kadin", "Frau");
        private string OtherText() => ProfileUiText("Другое", "Other", "Diger", "Andere");
        private string RememberProfileText() => ProfileUiText("Запомнить профиль", "Remember Profile", "Profili Hatirla", "Profil merken");
        private string ProfileSlotText() => ProfileUiText("Слот профиля", "Profile Slot", "Profil Yuvasi", "Profilplatz");
        private string AutoIdText() => ProfileUiText("ID будет назначен автоматически", "ID will be assigned automatically", "ID otomatik atanacak", "ID wird automatisch vergeben");
        private string BackText() => ProfileUiText("Назад", "Back", "Geri", "Zuruck");
        private string CancelText() => ProfileUiText("Отмена", "Cancel", "Iptal", "Abbrechen");
        private string ContinueText() => ProfileUiText("Далее", "Next", "Ileri", "Weiter");
        private string DeleteProfileText() => ProfileUiText("Удалить профиль", "Delete Profile", "Profili Sil", "Profil loschen");
        private string ConfirmDeleteText() => ProfileUiText("Подтвердите удаление", "Confirm Delete", "Silmeyi Onayla", "Loschen bestatigen");
        private string DynastyAccountText() => ProfileUiText("Аккаунт династии", "Dynasty Account", "Hanedan Hesabi", "Dynastie-Konto");
        private string ChooseGenderText() => ProfileUiText("Выберите пол", "Choose Gender", "Cinsiyet Sec", "Geschlecht wahlen");
        private string ProfileDetailsText() => ProfileUiText("Данные профиля", "Profile Details", "Profil Bilgileri", "Profildaten");
        private string EnterSlotText() => ProfileUiText("Войти в слот", "Enter Slot", "Yuvaya Gir", "Platz offnen");
        private string CreateSlotText() => ProfileUiText("Создать слот", "Create Slot", "Yuva Olustur", "Platz erstellen");
        private string BusyText() => ProfileUiText("Занят", "Busy", "Mesgul", "Belegt");
        private string FreeText() => ProfileUiText("Свободно", "Free", "Bos", "Frei");
        private string FreeSlotText() => ProfileUiText("Свободный слот", "Free Slot", "Bos Yuva", "Freier Platz");
        private string ProfileLabelText() => ProfileUiText("Профиль", "Profile", "Profil");
        private string ProfileDeletedText() => ProfileUiText("Профиль удален.", "Profile deleted.", "Profil silindi.");
        private string CreateProfileInSlotText() => ProfileUiText("Создайте профиль в свободном слоте.", "Create a profile in this free slot.", "Bos yuvada profil olustur.");
        private string NoFreeProfileSlotsText() => ProfileUiText("Все слоты профилей заняты.", "All profile slots are occupied.", "Tum profil yuvalari dolu.", "Alle Profilplaetze sind belegt.");
        private string SlotOccupiedText() => ProfileUiText("Этот слот уже занят.", "This slot is already occupied.", "Bu yuva zaten dolu.");
        private string ProfileInUseText() => ProfileUiText("Этот профиль используется на другом устройстве.", "This profile is in use on another device.", "Bu profil baska cihazda kullaniliyor.");
        private string ChooseOccupiedSlotText() => ProfileUiText("Выберите занятый слот профиля.", "Choose an occupied profile slot.", "Dolu bir profil yuvasi sec.");
        private string EnterPasswordToDeleteText() => ProfileUiText("Введите пароль аккаунта для удаления профиля.", "Enter account password to delete this profile.", "Profili silmek icin hesap sifresini gir.");
        private string ProfileDeleteLockedText() => ProfileUiText("Профиль можно удалить через 24 часа после создания.", "Profile can be deleted 24 hours after creation.", "Profil olusturulduktan 24 saat sonra silinebilir.");
        private string ProfileDeleteLockedShortText() => ProfileUiText("Удаление через 24ч", "Delete after 24h", "24 saat sonra silinebilir");
        private string ChooseMaleOrFemaleText() => ProfileUiText("Выберите мужской или женский пол.", "Choose male or female.", "Erkek veya kadin sec.");
        private string LoginFailedText() => ProfileUiText("Вход не выполнен. Попробуйте еще раз.", "Login failed. Please try again.", "Giris basarisiz. Tekrar dene.");
        private string InvalidCredentialsText() => ProfileUiText("Неверный ханедан, email или пароль. Введите пароль заново.", "Invalid dynasty, email, or password. Enter the password again.", "Hanedan, e-posta veya sifre hatali. Sifreyi tekrar gir.", "Dynastie, E-Mail oder Passwort ist falsch.");
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
        private string EnterAccountIdentifierText() => ProfileUiText("Введите ханедан или почту.", "Enter dynasty or email.", "Hanedan veya e-posta gir.", "Dynastie oder E-Mail eingeben.");
        private string AccountIdentifierInvalidText() => ProfileUiText("Ханедан или почта введены неверно.", "Dynasty or email is invalid.", "Hanedan veya e-posta gecersiz.", "Dynastie oder E-Mail ist ungueltig.");
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
            SetNamedText(windowRect, "Title", CurrentProfileTitleText());
            SetNamedText(windowRect, "Subtitle", CurrentProfileSubtitleText());

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
            SetNamedText(windowRect, "Title", CurrentProfileTitleText());
            SetNamedText(windowRect, "Subtitle", CurrentProfileSubtitleText());

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
                return ProfileAvatarResources.GetAvatarId(selectedGender, currentAvatarIndex);

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
            ConfigureAccountIdentifierInput();

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            RectTransform root = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;

            if (root == null || root.rect.width < 1f || root.rect.height < 1f)
                root = transform as RectTransform;

            if (root == null || root.rect.width < 1f || root.rect.height < 1f)
                root = transform.parent as RectTransform;

            if (root == null || root.rect.width < 1f || root.rect.height < 1f)
            {
                if (generatedRoot != null)
                    root = generatedRoot.parent as RectTransform;
            }

            if (root == null || windowRect == null || leftPaneRect == null || rightPaneRect == null)
                return;

            float rootWidth = Mathf.Max(1f, root.rect.width);
            float rootHeight = Mathf.Max(1f, root.rect.height);
            LayoutTuningSettings layout = layoutTuning ?? new LayoutTuningSettings();
            bool wideLandscape = rootWidth >= rootHeight * 1.45f;
            bool mobileLandscape = rootWidth >= rootHeight * 1.12f;
            bool compactLandscape = mobileLandscape && rootHeight < 620f;

            float windowWidth = rootWidth * ProfileWindowFullscreenOverscanX;
            float windowHeight = rootHeight * ProfileWindowFullscreenOverscanY;
            float screenOffsetX = (windowWidth - rootWidth) * 0.5f;
            float screenOffsetY = (windowHeight - rootHeight) * 0.5f;
            if (generatedRoot != null)
            {
                generatedRoot.anchorMin = new Vector2(0.5f, 0.5f);
                generatedRoot.anchorMax = new Vector2(0.5f, 0.5f);
                generatedRoot.pivot = new Vector2(0.5f, 0.5f);
                generatedRoot.anchoredPosition = Vector2.zero;
                generatedRoot.sizeDelta = new Vector2(rootWidth, rootHeight);
            }

            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(windowWidth, windowHeight);

            Stretch(windowBackgroundRect);

            Rect contentRect = GetProfileWindowContentRect(rootWidth, rootHeight);
            contentRect.x += screenOffsetX;
            contentRect.y += screenOffsetY;
            float headerPaddingX = Mathf.Clamp(contentRect.width * 0.018f, 10f, 22f);
            float bodyPaddingX = Mathf.Clamp(contentRect.width * (mobileLandscape ? 0.012f : 0.02f), mobileLandscape ? 8f : 14f, mobileLandscape ? 18f : 28f);
            float languageButtonWidth = Mathf.Clamp(contentRect.width * 0.04f, mobileLandscape ? 56f : 40f, mobileLandscape ? 76f : 48f);
            float languageButtonGap = Mathf.Clamp(layout.languageButtonGap, 4f, 8f);
            float languageButtonHeight = Mathf.Clamp(languageButtonWidth * 0.72f, mobileLandscape ? 40f : 28f, mobileLandscape ? 54f : 34f);
            float languageRowWidth = languageButtonWidth * 4f + languageButtonGap * 3f;
            float headerTop = contentRect.y + contentRect.height;
            float languageOffsetX = mobileLandscape
                ? Mathf.Max(languageRowWidth * 0.06f, contentRect.width * 0.012f)
                : Mathf.Max(languageRowWidth * 0.16f, contentRect.width * 0.026f);
            float languageOffsetY = mobileLandscape
                ? Mathf.Max(4f, contentRect.height * 0.018f)
                : Mathf.Max(6f, contentRect.height * 0.04f);
            float languageX = contentRect.x + contentRect.width - languageRowWidth - headerPaddingX - languageOffsetX;
            float languageY = headerTop - languageButtonHeight - 6f - languageOffsetY;
            bool showLanguageButtons = (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots) || (!loginMode && registerStep == RegisterStep.Account);
            SetObjectActive(russianLanguageButton != null ? russianLanguageButton.gameObject : null, showLanguageButtons);
            SetObjectActive(englishLanguageButton != null ? englishLanguageButton.gameObject : null, showLanguageButtons);
            SetObjectActive(turkishLanguageButton != null ? turkishLanguageButton.gameObject : null, showLanguageButtons);
            SetObjectActive(germanLanguageButton != null ? germanLanguageButton.gameObject : null, showLanguageButtons);
            SetRect(russianLanguageButton != null ? russianLanguageButton.transform as RectTransform : null, languageX, languageY, languageButtonWidth, languageButtonHeight);
            SetRect(englishLanguageButton != null ? englishLanguageButton.transform as RectTransform : null, languageX + languageButtonWidth + languageButtonGap, languageY, languageButtonWidth, languageButtonHeight);
            SetRect(turkishLanguageButton != null ? turkishLanguageButton.transform as RectTransform : null, languageX + (languageButtonWidth + languageButtonGap) * 2f, languageY, languageButtonWidth, languageButtonHeight);
            SetRect(germanLanguageButton != null ? germanLanguageButton.transform as RectTransform : null, languageX + (languageButtonWidth + languageButtonGap) * 3f, languageY, languageButtonWidth, languageButtonHeight);
            bool accountEntryLayout = (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots) || (!loginMode && registerStep == RegisterStep.Account);
            float accountTabGap = Mathf.Clamp(contentRect.width * 0.01f, 8f, 16f);
            float accountTabHeight = Mathf.Clamp(languageButtonHeight * 1.2f, mobileLandscape ? 56f : 34f, mobileLandscape ? 76f : 46f);
            float accountTabWidth = Mathf.Clamp(contentRect.width * 0.145f, mobileLandscape ? 130f : 90f, mobileLandscape ? 210f : 150f);
            float accountTabsX = contentRect.x + headerPaddingX + Mathf.Clamp(contentRect.width * 0.02f, 12f, 28f);
            float accountTabsY = languageY;
            SetObjectActive(dynastyTabButton != null ? dynastyTabButton.gameObject : null, accountEntryLayout);
            SetObjectActive(profileTabButton != null ? profileTabButton.gameObject : null, accountEntryLayout);
            if (accountEntryLayout)
            {
                SetRect(dynastyTabButton != null ? dynastyTabButton.transform as RectTransform : null, accountTabsX, accountTabsY, accountTabWidth, accountTabHeight);
                SetRect(profileTabButton != null ? profileTabButton.transform as RectTransform : null, accountTabsX + accountTabWidth + accountTabGap, accountTabsY, accountTabWidth, accountTabHeight);
            }

            float titleHeight = compactLandscape ? Mathf.Clamp(rootHeight * 0.075f, 34f, 46f) : layout.titleHeight;
            float titleWidth = Mathf.Min(Mathf.Max(320f, contentRect.width - languageRowWidth - headerPaddingX * 4f), 720f);
            float titleX = contentRect.x + (contentRect.width - titleWidth) * 0.5f;
            float titleLift = compactLandscape ? 8f : mobileLandscape ? 28f : Mathf.Max(layout.titleHeight * 0.36f, contentRect.height * 0.02f);
            float titleY = headerTop - titleHeight - (compactLandscape ? 8f : 14f) + titleLift;
            SetRect(windowRect.Find("Title") as RectTransform, titleX, titleY, titleWidth, titleHeight);

            float subtitleWidth = Mathf.Min(Mathf.Max(320f, contentRect.width - headerPaddingX * 4f), 780f);
            float subtitleX = contentRect.x + (contentRect.width - subtitleWidth) * 0.5f;
            float subtitleHeight = compactLandscape ? Mathf.Clamp(rootHeight * 0.052f, 24f, 32f) : mobileLandscape ? layout.subtitleHeight + 10f : layout.subtitleHeight;
            float subtitleY = titleY - subtitleHeight - (compactLandscape ? 6f : mobileLandscape ? 16f : 8f);
            SetRect(windowRect.Find("Subtitle") as RectTransform, subtitleX, subtitleY, subtitleWidth, subtitleHeight);

            float bodyBottom = contentRect.y + Mathf.Clamp(contentRect.height * (compactLandscape ? 0.01f : 0.02f), compactLandscape ? 4f : 10f, compactLandscape ? 10f : 20f);
            float bodyTop = subtitleY - (compactLandscape ? 8f : mobileLandscape ? 28f : 24f);
            float bodyHeight = Mathf.Max(180f, bodyTop - bodyBottom);
            float gap = Mathf.Clamp(contentRect.width * (mobileLandscape ? 0.006f : 0.012f), 6f, mobileLandscape ? 12f : layout.bodyGap);
            float bodyX = contentRect.x + bodyPaddingX;
            float bodyWidth = contentRect.width - bodyPaddingX * 2f;
            float availableBodyWidth = Mathf.Max(260f, bodyWidth);
            float minLeftWidth = Mathf.Min(layout.leftPaneWidthRange.x, layout.leftPaneWidthRange.y);
            float maxLeftWidth = Mathf.Max(layout.leftPaneWidthRange.x, layout.leftPaneWidthRange.y);
            float leftWidth = mobileLandscape
                ? Mathf.Clamp(availableBodyWidth * 0.32f, 440f, 620f)
                : Mathf.Clamp(availableBodyWidth * layout.leftPaneWidthPercent, minLeftWidth, maxLeftWidth);
            float rightWidth = availableBodyWidth - leftWidth - gap;
            float minRightWidth = Mathf.Min(mobileLandscape ? 520f : 420f, availableBodyWidth * 0.6f);
            if (accountEntryLayout)
            {
                SetObjectActive(leftPaneRect != null ? leftPaneRect.gameObject : null, false);
                rightWidth = availableBodyWidth;
                SetRect(rightPaneRect, bodyX, bodyBottom, rightWidth, bodyHeight);
            }
            else if (loginMode && loginSlotsLoaded)
            {
                SetObjectActive(leftPaneRect != null ? leftPaneRect.gameObject : null, true);
                leftWidth = Mathf.Clamp(availableBodyWidth * (mobileLandscape ? 0.24f : 0.28f), mobileLandscape ? 320f : minLeftWidth, mobileLandscape ? 410f : maxLeftWidth);
                rightWidth = availableBodyWidth - leftWidth - gap;
                SetRect(leftPaneRect, bodyX, bodyBottom, leftWidth, bodyHeight);
                SetRect(rightPaneRect, bodyX + leftWidth + gap, bodyBottom, rightWidth, bodyHeight);
            }
            else if (!loginMode && registerStep == RegisterStep.Gender)
            {
                SetObjectActive(leftPaneRect != null ? leftPaneRect.gameObject : null, false);
                rightWidth = availableBodyWidth;
                SetRect(rightPaneRect, bodyX, bodyBottom, rightWidth, bodyHeight);
            }
            else
            {
                SetObjectActive(leftPaneRect != null ? leftPaneRect.gameObject : null, true);
                if (rightWidth < minRightWidth)
                {
                    leftWidth = Mathf.Max(minLeftWidth, leftWidth - (minRightWidth - rightWidth));
                    rightWidth = availableBodyWidth - leftWidth - gap;
                }

                SetRect(leftPaneRect, bodyX, bodyBottom, leftWidth, bodyHeight);
                SetRect(rightPaneRect, bodyX + leftWidth + gap, bodyBottom, rightWidth, bodyHeight);
            }

            ApplyReadableTextSizes(mobileLandscape);
            if (!accountEntryLayout)
                LayoutLeftPane(leftWidth, bodyHeight, mobileLandscape);
            LayoutRightPane(rightWidth, bodyHeight, mobileLandscape);
        }

        private void ApplyReadableTextSizes(bool mobileLandscape)
        {
            SetTextSize(windowRect != null ? windowRect.Find("Title") as RectTransform : null, mobileLandscape ? 58f : 46f);
            SetTextSize(windowRect != null ? windowRect.Find("Subtitle") as RectTransform : null, mobileLandscape ? 32f : 24f);
            SetTextSize(dynastyInputLabel, mobileLandscape ? 34f : 26f);
            SetTextSize(emailInputLabel, mobileLandscape ? 34f : 26f);
            SetTextSize(passwordInputLabel, mobileLandscape ? 34f : 26f);
            SetTextSize(nicknameInputLabel, mobileLandscape ? 34f : 26f);
            SetTextSize(ageInputLabel, mobileLandscape ? 34f : 26f);
            SetTextSize(avatarIndexText, mobileLandscape ? 30f : 22f);
            SetTextSize(slotProfileNameText, mobileLandscape ? 40f : 30f);
            SetTextSize(slotProfileLevelText, mobileLandscape ? 32f : 24f);
            SetTextSize(slotProfileAgeText, mobileLandscape ? 30f : 22f);
            SetTextSize(registerStepText, mobileLandscape ? 30f : 22f);
            SetTextSize(idPreviewText, mobileLandscape ? 28f : 22f);
            SetTextSize(slotLabelText, mobileLandscape ? 36f : 24f);
            SetTextSize(errorText, mobileLandscape ? 31f : 22f);

            SetButtonTextSize(russianLanguageButton, mobileLandscape ? 28f : 20f);
            SetButtonTextSize(englishLanguageButton, mobileLandscape ? 28f : 20f);
            SetButtonTextSize(turkishLanguageButton, mobileLandscape ? 28f : 20f);
            SetButtonTextSize(germanLanguageButton, mobileLandscape ? 28f : 20f);
            SetButtonTextSize(dynastyTabButton, mobileLandscape ? 36f : 22f);
            SetButtonTextSize(profileTabButton, mobileLandscape ? 36f : 22f);
            SetButtonTextSize(slotOneButton, mobileLandscape ? 35f : 24f);
            SetButtonTextSize(slotTwoButton, mobileLandscape ? 35f : 24f);
            SetButtonTextSize(slotThreeButton, mobileLandscape ? 35f : 24f);
            SetButtonTextSize(maleButton, mobileLandscape ? 36f : 22f);
            SetButtonTextSize(femaleButton, mobileLandscape ? 36f : 22f);
            SetButtonTextSize(continueButton, mobileLandscape ? 38f : 24f);
            SetButtonTextSize(loginButton, mobileLandscape ? 38f : 24f);
            SetButtonTextSize(forgotPasswordButton, mobileLandscape ? 32f : 20f);
            SetButtonTextSize(deleteSlotButton, mobileLandscape ? 34f : 22f);
            SetButtonTextSize(backButton, mobileLandscape ? 38f : 24f);

            SetInputTextSize(dynastyInput, mobileLandscape ? 38f : 28f);
            SetInputTextSize(nameInput, mobileLandscape ? 38f : 28f);
            SetInputTextSize(emailInput, mobileLandscape ? 38f : 28f);
            SetInputTextSize(passwordInput, mobileLandscape ? 38f : 28f);
            SetInputTextSize(ageInput, mobileLandscape ? 38f : 28f);

            TMP_Text rememberLabel = rememberToggle != null ? rememberToggle.GetComponentInChildren<TMP_Text>(true) : null;
            SetTextSize(rememberLabel, mobileLandscape ? 32f : 22f);
            SetTextSize(rightPaneRect != null ? rightPaneRect.Find("GenderLabel") as RectTransform : null, mobileLandscape ? 36f : 24f);
        }

        private static void SetButtonTextSize(Button button, float fontSize)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            SetTextSize(label, fontSize);
            if (label != null)
            {
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Truncate;
                label.margin = new Vector4(18f, 2f, 18f, 5f);
            }
        }

        private static void SetInputTextSize(TMP_InputField input, float fontSize)
        {
            if (input == null)
                return;

            SetTextSize(input.textComponent, fontSize);
            SetTextSize(input.placeholder as TMP_Text, fontSize);
        }

        private static void SetTextSize(RectTransform rect, float fontSize)
        {
            TMP_Text label = rect != null ? rect.GetComponent<TMP_Text>() : null;
            SetTextSize(label, fontSize);
        }

        private static void SetTextSize(TMP_Text label, float fontSize)
        {
            if (label == null)
                return;

            label.fontSize = fontSize;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(16f, fontSize * 0.72f);
        }

        private void LayoutLeftPane(float width, float height, bool mobileLandscape)
        {
            if (leftPaneRect == null)
                return;

            LayoutTuningSettings layout = layoutTuning ?? new LayoutTuningSettings();
            bool showSlotPreview = loginMode && loginSlotsLoaded;
            float sidePadding = Mathf.Clamp(width * (mobileLandscape ? 0.12f : 0.07f), mobileLandscape ? 46f : 20f, mobileLandscape ? 72f : 34f);
            float topPadding = Mathf.Clamp(height * 0.02f, 8f, 14f);
            float avatarMax = showSlotPreview
                ? Mathf.Max(layout.slotPreviewAvatarSizeRange.x, layout.slotPreviewAvatarSizeRange.y)
                : Mathf.Max(layout.avatarSizeRange.x, layout.avatarSizeRange.y);
            float avatarMin = mobileLandscape ? (showSlotPreview ? 190f : 180f) : (showSlotPreview ? 150f : 140f);
            float bottomPadding = showSlotPreview ? 16f : 14f;
            float nameHeight = mobileLandscape ? (showSlotPreview ? 44f : 48f) : (showSlotPreview ? 32f : 36f);
            float statHeight = mobileLandscape ? 34f : (showSlotPreview ? 20f : 24f);
            float detailsGap = showSlotPreview ? 0f : 4f;
            float detailsSpacing = showSlotPreview ? 4f : 12f;
            float detailsAreaHeight = showSlotPreview
                ? detailsSpacing + nameHeight + statHeight * 2f + detailsGap * 2f
                : detailsSpacing + 32f;
            bool avatarSelectionStep = registerStep == RegisterStep.Details
                || (registerStep == RegisterStep.Gender && (selectedGender == PlayerGender.Male || selectedGender == PlayerGender.Female));
            float buttonSize = Mathf.Clamp(layout.avatarArrowSize, mobileLandscape ? 72f : 54f, mobileLandscape ? 88f : 68f);
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
            float frameShiftX = mobileLandscape
                ? Mathf.Clamp(frameSize * 0.34f, 68f, 108f)
                : Mathf.Clamp(frameSize * 0.20f, 24f, 56f);
            float frameShiftY = Mathf.Clamp(frameSize * 0.35f, 36f, 84f);
            float frameBaseX = (width - frameSize) * 0.5f;
            float frameBaseY = Mathf.Max(frameBottom, frameTop - frameSize);
            float frameMinX = sidePadding;
            float frameMaxX = Mathf.Max(frameMinX, width - frameSize - sidePadding);
            float frameX = Mathf.Clamp(frameBaseX + frameShiftX, frameMinX, frameMaxX);
            float maxFrameY = Mathf.Max(frameBottom, height - frameSize - 4f);
            float frameY = Mathf.Clamp(frameBaseY + frameShiftY, frameBottom, maxFrameY);
            if (mobileLandscape && avatarSelectionStep)
            {
                buttonSize = Mathf.Clamp(Mathf.Min(width * 0.14f, height * 0.16f), 78f, 104f);
                sideGap = Mathf.Clamp(width * 0.038f, 28f, 48f);
                float maxFrameByWidth = Mathf.Max(180f, width - sidePadding * 2f - buttonSize * 2f - sideGap * 2f);
                float maxFrameByHeight = Mathf.Max(180f, height * 0.78f);
                frameSize = Mathf.Clamp(Mathf.Min(maxFrameByWidth, maxFrameByHeight), 390f, 520f);
                avatarSize = Mathf.Clamp(frameSize * 0.7f, 270f, 360f);
                avatarInset = (frameSize - avatarSize) * 0.5f;
                float fullPickerWidth = frameSize + buttonSize * 2f + sideGap * 2f;
                float pickerX = Mathf.Clamp(width * 0.5f - fullPickerWidth * 0.5f, sidePadding, Mathf.Max(sidePadding, width - fullPickerWidth - sidePadding));
                frameX = pickerX + buttonSize + sideGap;
                frameY = Mathf.Clamp(height * 0.53f - frameSize * 0.5f, frameBottom + 24f, maxFrameY);
            }
            float frameVisualSize = frameSize;
            float frameVisualX = frameX;
            float frameVisualY = frameY;
            if (avatarSelectionStep)
            {
                avatarSize = Mathf.Clamp(frameSize * 0.76f, mobileLandscape ? 300f : 190f, mobileLandscape ? 390f : 280f);
                float frameBorder = Mathf.Clamp(avatarSize * 0.11f, mobileLandscape ? 34f : 22f, mobileLandscape ? 48f : 34f);
                frameVisualSize = avatarSize + frameBorder * 2f;
                frameVisualX = frameX + (frameSize - frameVisualSize) * 0.5f;
                frameVisualY = frameY + (frameSize - frameVisualSize) * 0.5f;
                avatarInset = frameBorder;
            }

            float previewX = frameVisualX + avatarInset;
            float previewY = frameVisualY + avatarInset;
            float buttonY = frameY + frameSize * 0.5f - buttonSize * 0.5f;
            float leftButtonX = frameX - buttonSize - sideGap;
            float rightButtonX = frameX + frameSize + sideGap;

            SetObjectActive(leftPaneRect.Find("AvatarTitle") != null ? leftPaneRect.Find("AvatarTitle").gameObject : null, false);
            SetRect(avatarPreviewFrame != null ? avatarPreviewFrame.rectTransform : null, frameVisualX, frameVisualY, frameVisualSize, frameVisualSize);
            SetRect(avatarPreview != null ? avatarPreview.rectTransform : null, previewX, previewY, avatarSize, avatarSize);
            SetRect(previousAvatarButton != null ? previousAvatarButton.transform as RectTransform : null, leftButtonX, buttonY, buttonSize, buttonSize);
            SetRect(nextAvatarButton != null ? nextAvatarButton.transform as RectTransform : null, rightButtonX, buttonY, buttonSize, buttonSize);

            if (avatarPreview != null)
                avatarPreview.transform.SetAsFirstSibling();

            if (avatarPreviewFrame != null && avatarPreview != null)
                avatarPreviewFrame.transform.SetAsLastSibling();

            float detailsWidth = mobileLandscape ? Mathf.Max(220f, frameVisualSize + 36f) : Mathf.Max(160f, frameVisualSize);
            float textShiftX = mobileLandscape ? 0f : detailsWidth * 0.10f;
            float detailsX = Mathf.Clamp(
                frameVisualX + (frameVisualSize - detailsWidth) * 0.5f - textShiftX,
                sidePadding,
                Mathf.Max(sidePadding, width - detailsWidth - sidePadding));
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

        private void LayoutRightPane(float width, float height, bool mobileLandscape)
        {
            if (rightPaneRect == null)
                return;

            LayoutTuningSettings layout = layoutTuning ?? new LayoutTuningSettings();
            float x = Mathf.Clamp(width * (mobileLandscape ? 0.018f : 0.035f), mobileLandscape ? 8f : 10f, mobileLandscape ? 18f : layout.detailsPaddingX);
            float fieldWidth = Mathf.Max(180f, width - x * 2f);
            float controlWidth = Mathf.Min(fieldWidth, Mathf.Clamp(width * (mobileLandscape ? 0.98f : 0.9f), mobileLandscape ? 900f : 420f, mobileLandscape ? 1100f : 740f));
            float controlX = x + (fieldWidth - controlWidth) * 0.5f;
            float topInset = Mathf.Clamp(height * (mobileLandscape ? 0.02f : 0.032f), mobileLandscape ? 8f : 12f, mobileLandscape ? 16f : 24f);
            float smallGap = Mathf.Clamp(height * (mobileLandscape ? 0.012f : 0.016f), mobileLandscape ? 6f : 8f, mobileLandscape ? 10f : 14f);
            float mediumGap = Mathf.Clamp(height * (mobileLandscape ? 0.018f : 0.026f), mobileLandscape ? 8f : 12f, mobileLandscape ? 14f : 18f);
            float tabGap = Mathf.Clamp(width * (mobileLandscape ? 0.02f : 0.018f), mobileLandscape ? 18f : 8f, mobileLandscape ? 28f : layout.tabsGap);
            float tabHeight = Mathf.Clamp(height * (mobileLandscape ? 0.11f : 0.095f), mobileLandscape ? 70f : 46f, mobileLandscape ? 86f : layout.tabHeight);
            float fieldHeight = Mathf.Clamp(height * (mobileLandscape ? 0.15f : 0.11f), mobileLandscape ? 88f : 52f, mobileLandscape ? 112f : layout.fieldHeight + 12f);
            float compactFieldHeight = Mathf.Clamp(height * (mobileLandscape ? 0.11f : 0.088f), mobileLandscape ? 68f : 44f, mobileLandscape ? 84f : layout.compactFieldHeight);
            float loginSlotHeight = Mathf.Clamp(height * (mobileLandscape ? 0.38f : 0.24f), mobileLandscape ? 220f : 110f, mobileLandscape ? 292f : layout.loginSlotButtonHeight + 58f);
            float registerSlotHeight = Mathf.Clamp(height * (mobileLandscape ? 0.17f : 0.13f), mobileLandscape ? 96f : 60f, mobileLandscape ? 126f : layout.registerSlotButtonHeight);
            float primaryHeight = Mathf.Clamp(height * (mobileLandscape ? 0.12f : 0.095f), mobileLandscape ? 76f : 50f, mobileLandscape ? 94f : layout.primaryButtonHeight);
            float stepLabelHeight = Mathf.Clamp(height * (mobileLandscape ? 0.075f : 0.075f), mobileLandscape ? 42f : 32f, mobileLandscape ? 56f : 46f);
            float supportTextHeight = Mathf.Clamp(height * (mobileLandscape ? 0.07f : 0.065f), mobileLandscape ? 40f : 28f, mobileLandscape ? 54f : 42f);
            float sectionLabelHeight = Mathf.Clamp(height * (mobileLandscape ? 0.07f : 0.058f), mobileLandscape ? 40f : 26f, mobileLandscape ? 56f : 36f);
            float errorHeight = Mathf.Clamp(height * (mobileLandscape ? 0.12f : 0.12f), mobileLandscape ? 58f : 44f, mobileLandscape ? 84f : 72f);
            float bottomY = Mathf.Clamp(height * (mobileLandscape ? 0.022f : 0.018f), mobileLandscape ? 16f : 12f, mobileLandscape ? 24f : 18f);
            float errorY = bottomY + primaryHeight + (mobileLandscape ? 18f : 14f);
            bool hasError = errorText != null && !string.IsNullOrWhiteSpace(errorText.text);
            float contentBottomLimit = errorY + (hasError ? errorHeight + 10f : 24f);
            float y = height - topInset - tabHeight;
            float fieldX = controlX;
            float fieldAreaWidth = controlWidth;
            bool showAccountModeTabs = !creatingSlotForExistingAccount && !loginSlotsLoaded && registerStep == RegisterStep.Account;
            if (!showAccountModeTabs)
                y = height - topInset - stepLabelHeight - (mobileLandscape ? 18f : 8f);

            bool showLoginSlotPicker = loginMode && loginSlotsLoaded;
            bool showAccountSlotOverview = HasAccountSlotOverview();
            bool showAccountFields = (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots) || (!loginMode && registerStep == RegisterStep.Account);
            bool showGenderFields = !loginMode && registerStep == RegisterStep.Gender;
            bool showCreateSlotDetails = creatingSlotForExistingAccount && registerStep == RegisterStep.Details;
            bool showSlotFields = showLoginSlotPicker;
            bool showProfileFields = !loginMode && registerStep == RegisterStep.Details;
            bool showSlotPreview = loginMode && loginSlotsLoaded;
            bool showRemember = (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots) || (!loginMode && registerStep == RegisterStep.Account);
            bool showForgotPassword = loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots && !confirmingDeleteSlot;
            bool showDeleteButton = loginMode && loginSlotsLoaded && !confirmingDeleteSlot && IsSelectedLoginSlotOccupied() && !IsSelectedLoginSlotInUseByOtherDevice() && !IsSelectedLoginSlotDeletionLocked();
            bool showDeletePassword = loginMode && loginSlotsLoaded && confirmingDeleteSlot;
            bool genderAvatarSelected = selectedGender == PlayerGender.Male || selectedGender == PlayerGender.Female;
            bool showAvatar = showProfileFields || showSlotPreview || (showGenderFields && genderAvatarSelected);
            bool showAvatarPicker = showProfileFields || (showGenderFields && genderAvatarSelected);
            bool showBack = loginSlotsLoaded || (!loginMode && registerStep != RegisterStep.Account) || creatingSlotForExistingAccount;
            bool showAccountRegistrationFields = !loginMode && registerStep == RegisterStep.Account;
            bool showExistingSlotDetails = creatingSlotForExistingAccount && registerStep == RegisterStep.Details;
            bool compactProfileDetails = mobileLandscape && height < 440f && !loginMode && registerStep == RegisterStep.Details;
            bool showLoginAccountFields = loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots;
            bool showStepLabel = !loginMode && !showGenderFields && !showAccountRegistrationFields && !showExistingSlotDetails && !compactProfileDetails;
            bool showIdPreview = !loginMode && !showGenderFields && !showAccountRegistrationFields && !showExistingSlotDetails && !compactProfileDetails;

            SetObjectActive(registerStepText != null ? registerStepText.gameObject : null, showStepLabel);
            SetObjectActive(idPreviewText != null ? idPreviewText.gameObject : null, showIdPreview);
            if (showStepLabel)
            {
                SetRect(registerStepText != null ? registerStepText.rectTransform : null, fieldX, y, fieldAreaWidth, stepLabelHeight);
                y -= stepLabelHeight + (mobileLandscape ? mediumGap : smallGap);
            }

            if (showIdPreview)
            {
                SetRect(idPreviewText != null ? idPreviewText.rectTransform : null, fieldX, y, fieldAreaWidth, supportTextHeight);
                y -= supportTextHeight + (mobileLandscape ? mediumGap + 14f : mediumGap);
            }

            SetObjectActive(avatarPreview != null ? avatarPreview.gameObject : null, showAvatar);
            SetObjectActive(avatarPreviewFrame != null ? avatarPreviewFrame.gameObject : null, showAvatar);
            SetObjectActive(previousAvatarButton != null ? previousAvatarButton.gameObject : null, showAvatarPicker);
            SetObjectActive(nextAvatarButton != null ? nextAvatarButton.gameObject : null, showAvatarPicker);
            SetObjectActive(avatarIndexText != null ? avatarIndexText.gameObject : null, showAvatarPicker && registerStep != RegisterStep.Details);
            SetObjectActive(slotProfileNameText != null ? slotProfileNameText.gameObject : null, showSlotPreview);
            SetObjectActive(slotProfileLevelText != null ? slotProfileLevelText.gameObject : null, showSlotPreview);
            SetObjectActive(slotProfileAgeText != null ? slotProfileAgeText.gameObject : null, showSlotPreview);

            SetObjectActive(dynastyInput != null ? dynastyInput.gameObject : null, !loginMode && registerStep == RegisterStep.Account);
            SetObjectActive(emailInput != null ? emailInput.gameObject : null, showAccountFields);
            SetObjectActive(passwordInput != null ? passwordInput.gameObject : null, showAccountFields || showDeletePassword);
            bool showAccountInputLabels = showAccountRegistrationFields || showLoginAccountFields;
            SetObjectActive(dynastyInputLabel != null ? dynastyInputLabel.gameObject : null, showAccountRegistrationFields);
            SetObjectActive(emailInputLabel != null ? emailInputLabel.gameObject : null, showAccountInputLabels);
            SetObjectActive(passwordInputLabel != null ? passwordInputLabel.gameObject : null, showAccountInputLabels);
            SetObjectActive(nicknameInputLabel != null ? nicknameInputLabel.gameObject : null, showProfileFields);
            SetObjectActive(ageInputLabel != null ? ageInputLabel.gameObject : null, showProfileFields);
            SetObjectActive(rememberToggle != null ? rememberToggle.gameObject : null, showRemember);
            SetObjectActive(termsToggle != null ? termsToggle.gameObject : null, showAccountFields);
            SetObjectActive(termsButton != null ? termsButton.gameObject : null, showAccountFields);
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
            SetObjectActive(rightPaneRect.Find("GenderLabel") != null ? rightPaneRect.Find("GenderLabel").gameObject : null, false);
            SetObjectActive(maleButton != null ? maleButton.gameObject : null, showGenderFields);
            SetObjectActive(femaleButton != null ? femaleButton.gameObject : null, showGenderFields);
            SetObjectActive(otherButton != null ? otherButton.gameObject : null, false);

            if (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots)
            {
                float rememberHeight = mobileLandscape ? 54f : 40f;
                float legalHeight = mobileLandscape ? 58f : 44f;
                float legalGap = mobileLandscape ? 10f : 6f;
                float accountFooterHeight = rememberHeight + legalGap + legalHeight;
                LayoutAccountLoginFields(
                    fieldX,
                    y,
                    fieldAreaWidth,
                    contentBottomLimit + Mathf.Max(primaryHeight, accountFooterHeight) + mediumGap,
                    fieldHeight,
                    mediumGap,
                    mobileLandscape);
                float forgotWidth = Mathf.Min(Mathf.Max(mobileLandscape ? 300f : 180f, fieldAreaWidth * (mobileLandscape ? 0.32f : 0.42f)), mobileLandscape ? 420f : 260f);
                float entryEdgePadding = Mathf.Clamp(width * (mobileLandscape ? 0.026f : 0.032f), mobileLandscape ? 22f : 14f, mobileLandscape ? 38f : 24f);
                float forgotX = entryEdgePadding;
                float forgotHeight = Mathf.Clamp(height * (mobileLandscape ? 0.1f : 0.08f), mobileLandscape ? 68f : 42f, mobileLandscape ? 84f : 54f);
                SetRect(forgotPasswordButton != null ? forgotPasswordButton.transform as RectTransform : null, forgotX, bottomY, forgotWidth, forgotHeight);
                float rememberWidth = Mathf.Min(fieldAreaWidth * 0.5f, mobileLandscape ? 360f : 260f);
                float rememberX = fieldX + (fieldAreaWidth - rememberWidth) * 0.5f;
                SetRect(rememberToggle != null ? rememberToggle.transform as RectTransform : null, rememberX, contentBottomLimit, rememberWidth, mobileLandscape ? 54f : 40f);
                LayoutLegalConsent(fieldX, contentBottomLimit + rememberHeight + legalGap, fieldAreaWidth, legalHeight, mobileLandscape);
            }
            else if (loginMode)
            {
                SetObjectActive(slotLabelText != null ? slotLabelText.gameObject : null, false);

                float slotButtonGap = Mathf.Clamp(width * (mobileLandscape ? 0.026f : 0.016f), mobileLandscape ? 24f : 8f, mobileLandscape ? 46f : 18f);
                float slotAreaWidth = Mathf.Min(fieldAreaWidth, mobileLandscape ? 1080f : 680f);
                float slotButtonWidth = (slotAreaWidth - slotButtonGap * 2f) / 3f;
                float slotRowWidth = slotButtonWidth * 3f + slotButtonGap * 2f;
                float slotX = fieldX + (fieldAreaWidth - slotRowWidth) * 0.5f;
                float slotY = Mathf.Clamp(height * (mobileLandscape ? 0.48f : 0.44f), contentBottomLimit + primaryHeight + mediumGap + 20f, height - loginSlotHeight - topInset - 18f);
                SetRect(slotOneButton != null ? slotOneButton.transform as RectTransform : null, slotX, slotY, slotButtonWidth, loginSlotHeight);
                SetRect(slotTwoButton != null ? slotTwoButton.transform as RectTransform : null, slotX + slotButtonWidth + slotButtonGap, slotY, slotButtonWidth, loginSlotHeight);
                SetRect(slotThreeButton != null ? slotThreeButton.transform as RectTransform : null, slotX + (slotButtonWidth + slotButtonGap) * 2f, slotY, slotButtonWidth, loginSlotHeight);
                y = slotY - mediumGap;

                if (confirmingDeleteSlot)
                {
                    SetRect(passwordInput != null ? passwordInput.transform as RectTransform : null, fieldX, Mathf.Max(contentBottomLimit, y), fieldAreaWidth, fieldHeight);
                }
                else
                {
                    float deleteWidth = Mathf.Min(Mathf.Max(mobileLandscape ? 300f : 180f, fieldAreaWidth * (mobileLandscape ? 0.38f : 0.5f)), mobileLandscape ? 420f : 260f);
                    float deleteX = fieldX + (fieldAreaWidth - deleteWidth) * 0.5f;
                    float deleteHeight = Mathf.Clamp(height * (mobileLandscape ? 0.095f : 0.082f), mobileLandscape ? 66f : 44f, mobileLandscape ? 82f : 56f);
                    float deleteY = Mathf.Clamp(slotY - deleteHeight - (mobileLandscape ? 32f : 18f), contentBottomLimit + primaryHeight + mediumGap, slotY - deleteHeight - 8f);
                    SetRect(deleteSlotButton != null ? deleteSlotButton.transform as RectTransform : null, deleteX, deleteY, deleteWidth, deleteHeight);
                }

                SetRect(rememberToggle != null ? rememberToggle.transform as RectTransform : null, fieldX, contentBottomLimit, fieldAreaWidth, mobileLandscape ? 54f : 40f);
            }
            else if (registerStep == RegisterStep.Account)
            {
                float rememberHeight = mobileLandscape ? 54f : 40f;
                float legalHeight = mobileLandscape ? 58f : 44f;
                float legalGap = mobileLandscape ? 10f : 6f;
                LayoutAccountRegistrationFields(
                    fieldX,
                    y,
                    fieldAreaWidth,
                    contentBottomLimit + rememberHeight + legalGap + legalHeight + mediumGap,
                    fieldHeight,
                    mediumGap,
                    mobileLandscape);
                float rememberWidth = Mathf.Min(fieldAreaWidth * 0.42f, mobileLandscape ? 420f : 280f);
                float rememberX = fieldX + (fieldAreaWidth - rememberWidth) * 0.5f;
                SetRect(rememberToggle != null ? rememberToggle.transform as RectTransform : null, rememberX, contentBottomLimit, rememberWidth, rememberHeight);
                LayoutLegalConsent(fieldX, contentBottomLimit + rememberHeight + legalGap, fieldAreaWidth, legalHeight, mobileLandscape);
            }
            else if (registerStep == RegisterStep.Gender)
            {
                float buttonGap = Mathf.Clamp(width * (mobileLandscape ? 0.055f : 0.032f), mobileLandscape ? 48f : 16f, mobileLandscape ? 86f : 28f);
                float genderAreaWidth = Mathf.Min(width * 0.72f, mobileLandscape ? 940f : 520f);
                float genderButtonWidth = (genderAreaWidth - buttonGap) * 0.5f;
                float genderRowWidth = genderButtonWidth * 2f + buttonGap;
                float genderX = (width - genderRowWidth) * 0.5f;
                float genderHeight = Mathf.Clamp(height * (mobileLandscape ? 0.23f : 0.15f), mobileLandscape ? 136f : 72f, mobileLandscape ? 180f : 102f);
                if (creatingSlotForExistingAccount)
                {
                    float groupCenterY = Mathf.Clamp(height * 0.54f, contentBottomLimit + genderHeight + 70f, height - topInset - 150f);
                    float buttonsY = Mathf.Min(groupCenterY - genderHeight * 0.5f, Mathf.Max(contentBottomLimit, y - genderHeight));
                    SetRect(maleButton != null ? maleButton.transform as RectTransform : null, genderX, buttonsY, genderButtonWidth, genderHeight);
                    SetRect(femaleButton != null ? femaleButton.transform as RectTransform : null, genderX + genderButtonWidth + buttonGap, buttonsY, genderButtonWidth, genderHeight);
                }
                else
                {
                    float groupCenterY = Mathf.Clamp(height * 0.54f, contentBottomLimit + genderHeight + 54f, height - topInset - 130f);
                    float buttonsY = Mathf.Min(groupCenterY - genderHeight * 0.5f, Mathf.Max(contentBottomLimit, y - genderHeight));
                    SetRect(maleButton != null ? maleButton.transform as RectTransform : null, genderX, buttonsY, genderButtonWidth, genderHeight);
                    SetRect(femaleButton != null ? femaleButton.transform as RectTransform : null, genderX + genderButtonWidth + buttonGap, buttonsY, genderButtonWidth, genderHeight);
                }
            }
            else if (showCreateSlotDetails)
            {
                float formWidth = Mathf.Min(fieldAreaWidth, mobileLandscape ? 860f : 620f);
                float formX = fieldX + (fieldAreaWidth - formWidth) * 0.5f;
                float formCenterY = Mathf.Clamp(height * 0.52f, contentBottomLimit + fieldHeight + 80f, height - topInset - 130f);
                float blockTop = formCenterY + fieldHeight * 1.25f;
                LayoutNameAgeInputs(formX, blockTop, formWidth, Mathf.Min(mobileLandscape ? 420f : 260f, formWidth), fieldHeight, compactFieldHeight, mediumGap, contentBottomLimit, mobileLandscape);
            }
            else
            {
                float formWidth = Mathf.Min(fieldAreaWidth, mobileLandscape ? 960f : 660f);
                float formX = fieldX + (fieldAreaWidth - formWidth) * 0.5f;
                float formCenterY = Mathf.Clamp(height * 0.52f, contentBottomLimit + fieldHeight + 90f, height - topInset - 120f);
                float blockTop = formCenterY + fieldHeight * 1.1f;
                LayoutNameAgeInputs(formX, blockTop, formWidth, Mathf.Min(mobileLandscape ? 520f : 320f, formWidth), fieldHeight, compactFieldHeight, mediumGap * 1.25f, contentBottomLimit, mobileLandscape);
            }

            float errorWidth = Mathf.Min(fieldAreaWidth, mobileLandscape ? 760f : 560f);
            float errorX = fieldX + (fieldAreaWidth - errorWidth) * 0.5f;
            SetRect(errorText != null ? errorText.rectTransform : null, errorX, errorY, errorWidth, errorHeight);

            float bottomGap = Mathf.Clamp(width * (mobileLandscape ? 0.026f : 0.024f), mobileLandscape ? 22f : 10f, mobileLandscape ? 34f : 16f);
            float bottomAreaWidth = Mathf.Min(fieldAreaWidth, mobileLandscape ? 700f : 520f);
            float bottomButtonWidth = (bottomAreaWidth - bottomGap) * 0.5f;
            float bottomRowWidth = bottomButtonWidth * 2f + bottomGap;
            float bottomX = fieldX + (fieldAreaWidth - bottomRowWidth) * 0.5f;
            RectTransform primaryButtonRect = loginMode
                ? loginButton != null ? loginButton.transform as RectTransform : null
                : continueButton != null ? continueButton.transform as RectTransform : null;

            if (showBack)
            {
                if ((loginMode && loginSlotsLoaded) || (!loginMode && (registerStep == RegisterStep.Gender || registerStep == RegisterStep.Details)))
                {
                    float edgeButtonWidth = Mathf.Min(Mathf.Max(mobileLandscape ? 270f : 190f, width * (mobileLandscape ? 0.24f : 0.22f)), mobileLandscape ? 340f : 260f);
                    float edgePadding = Mathf.Clamp(width * (mobileLandscape ? 0.026f : 0.032f), mobileLandscape ? 22f : 14f, mobileLandscape ? 38f : 24f);
                    float usefulLeftInRightPane = ((loginMode && loginSlotsLoaded) || (!loginMode && registerStep == RegisterStep.Details)) && leftPaneRect != null
                        ? leftPaneRect.anchoredPosition.x - rightPaneRect.anchoredPosition.x
                        : 0f;
                    float usefulRightInRightPane = width;
                    SetRect(backButton != null ? backButton.transform as RectTransform : null, usefulLeftInRightPane + edgePadding, bottomY, edgeButtonWidth, primaryHeight);
                    SetRect(primaryButtonRect, usefulRightInRightPane - edgePadding - edgeButtonWidth, bottomY, edgeButtonWidth, primaryHeight);
                }
                else
                {
                    SetRect(backButton != null ? backButton.transform as RectTransform : null, bottomX, bottomY, bottomButtonWidth, primaryHeight);
                    SetRect(primaryButtonRect, bottomX + bottomButtonWidth + bottomGap, bottomY, bottomButtonWidth, primaryHeight);
                }
            }
            else
            {
                bool registrationAccountStep = !loginMode && registerStep == RegisterStep.Account;
                float singleButtonWidth = loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots
                    ? Mathf.Min(Mathf.Max(mobileLandscape ? 260f : 190f, fieldAreaWidth * (mobileLandscape ? 0.32f : 0.38f)), mobileLandscape ? 360f : 260f)
                    : registrationAccountStep
                        ? Mathf.Min(Mathf.Max(mobileLandscape ? 270f : 190f, fieldAreaWidth * (mobileLandscape ? 0.26f : 0.34f)), mobileLandscape ? 360f : 260f)
                        : Mathf.Min(fieldAreaWidth, mobileLandscape ? 620f : 420f);
                float singleButtonX = registrationAccountStep
                    ? width - Mathf.Clamp(width * (mobileLandscape ? 0.026f : 0.032f), mobileLandscape ? 22f : 14f, mobileLandscape ? 38f : 24f) - singleButtonWidth
                    : loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots
                        ? width - Mathf.Clamp(width * (mobileLandscape ? 0.026f : 0.032f), mobileLandscape ? 22f : 14f, mobileLandscape ? 38f : 24f) - singleButtonWidth
                        : fieldX + (fieldAreaWidth - singleButtonWidth) * 0.5f;
                SetRect(loginButton != null ? loginButton.transform as RectTransform : null, singleButtonX, bottomY, singleButtonWidth, primaryHeight);
                SetRect(continueButton != null ? continueButton.transform as RectTransform : null,
                    loginMode || registerStep != RegisterStep.Account ? singleButtonX : singleButtonX,
                    bottomY,
                    loginMode || registerStep != RegisterStep.Account ? singleButtonWidth : singleButtonWidth,
                    primaryHeight);
            }

            ApplySlotTextLayout(slotOneButton, 1);
            ApplySlotTextLayout(slotTwoButton, 2);
            ApplySlotTextLayout(slotThreeButton, 3);
        }

        private struct LayoutItem
        {
            public RectTransform Rect;
            public float DesiredHeight;
            public float MinimumHeight;

            public LayoutItem(RectTransform rect, float desiredHeight, float minimumHeight)
            {
                Rect = rect;
                DesiredHeight = desiredHeight;
                MinimumHeight = minimumHeight;
            }
        }

        private void LayoutVerticalControls(float x, float topY, float width, float bottomLimit, float desiredGap, bool mobileLandscape, params LayoutItem[] items)
        {
            if (items == null || items.Length == 0)
                return;

            float gap = Mathf.Max(mobileLandscape ? 5f : 4f, desiredGap);
            float available = Mathf.Max(1f, topY - bottomLimit);
            int visibleCount = 0;
            float desiredTotal = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Rect == null)
                    continue;

                visibleCount++;
                desiredTotal += items[i].DesiredHeight;
            }

            if (visibleCount == 0)
                return;

            desiredTotal += gap * Mathf.Max(0, visibleCount - 1);
            float scale = desiredTotal > available ? Mathf.Clamp01(available / desiredTotal) : 1f;
            float[] heights = new float[items.Length];
            float total = gap * Mathf.Max(0, visibleCount - 1) * scale;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Rect == null)
                    continue;

                heights[i] = Mathf.Max(items[i].MinimumHeight, items[i].DesiredHeight * scale);
                total += heights[i];
            }

            float overflow = total - available;
            if (overflow > 0.01f)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].Rect == null)
                        continue;

                    ReduceLayoutOverflow(ref heights[i], items[i].MinimumHeight, ref overflow);
                }
            }

            float actualGap = Mathf.Max(mobileLandscape ? 5f : 4f, gap * scale);
            ReduceLayoutOverflow(ref actualGap, mobileLandscape ? 5f : 4f, ref overflow);
            if (overflow > 0.01f)
            {
                float squeeze = Mathf.Clamp01(available / Mathf.Max(1f, total));
                actualGap *= squeeze;
                for (int i = 0; i < heights.Length; i++)
                    heights[i] *= squeeze;
            }

            float y = topY;
            for (int i = 0; i < items.Length; i++)
            {
                RectTransform rect = items[i].Rect;
                if (rect == null)
                    continue;

                y -= heights[i];
                SetCompactControlTextSize(rect, heights[i], mobileLandscape);
                SetRect(rect, x, y, width, heights[i]);
                y -= actualGap;
            }
        }

        private void SetCompactControlTextSize(RectTransform rect, float height, bool mobileLandscape)
        {
            if (rect == null)
                return;

            float maxSize = mobileLandscape ? 38f : 28f;
            float fontSize = Mathf.Min(maxSize, Mathf.Max(18f, height * 0.58f));
            TMP_InputField input = rect.GetComponent<TMP_InputField>();
            if (input != null)
            {
                SetInputTextSize(input, fontSize);
                return;
            }

            TMP_Text label = rect.GetComponentInChildren<TMP_Text>(true);
            SetTextSize(label, fontSize);
        }

        private void LayoutNameAgeInputs(float x, float topY, float width, float ageWidth, float desiredNameHeight, float desiredAgeHeight, float desiredGap, float bottomLimit, bool mobileLandscape)
        {
            float labelWidth = Mathf.Clamp(width * 0.22f, mobileLandscape ? 200f : 130f, mobileLandscape ? 280f : 190f);
            float labelGap = Mathf.Clamp(width * 0.028f, mobileLandscape ? 24f : 14f, mobileLandscape ? 40f : 26f);
            float inputX = x + labelWidth + labelGap;
            float inputWidth = Mathf.Max(mobileLandscape ? 620f : 260f, width - labelWidth - labelGap);
            float gap = Mathf.Max(mobileLandscape ? 12f : 7f, desiredGap);
            float nameHeight = Mathf.Clamp(desiredNameHeight * 1.08f, mobileLandscape ? 86f : 48f, mobileLandscape ? 120f : 72f);
            float ageHeight = Mathf.Clamp(desiredAgeHeight * 1.12f, mobileLandscape ? 78f : 44f, mobileLandscape ? 108f : 66f);
            float labelHeight = Mathf.Min(nameHeight, mobileLandscape ? 78f : 52f);
            float available = Mathf.Max(1f, topY - bottomLimit);
            float required = nameHeight + gap + ageHeight;

            if (required > available)
            {
                float scale = Mathf.Clamp01(available / required);
                float minNameHeight = mobileLandscape ? 46f : 36f;
                float minAgeHeight = mobileLandscape ? 40f : 32f;
                float minGap = mobileLandscape ? 6f : 5f;

                nameHeight = Mathf.Max(minNameHeight, nameHeight * scale);
                ageHeight = Mathf.Max(minAgeHeight, ageHeight * scale);
                gap = Mathf.Max(minGap, gap * scale);

                float overflow = nameHeight + gap + ageHeight - available;
                ReduceLayoutOverflow(ref nameHeight, minNameHeight, ref overflow);
                ReduceLayoutOverflow(ref ageHeight, minAgeHeight, ref overflow);
                ReduceLayoutOverflow(ref gap, minGap, ref overflow);

                if (overflow > 0.01f)
                {
                    float squeeze = Mathf.Clamp01(available / Mathf.Max(1f, nameHeight + gap + ageHeight));
                    nameHeight *= squeeze;
                    ageHeight *= squeeze;
                    gap *= squeeze;
                    labelHeight *= squeeze;
                }
            }

            float nameY = topY - nameHeight;
            float ageY = nameY - gap - ageHeight;
            if (ageY < bottomLimit)
            {
                float shift = bottomLimit - ageY;
                nameY += shift;
                ageY += shift;
            }

            LayoutAccountRow(nicknameInputLabel, nameInput, x, inputX, nameY, labelWidth, inputWidth, nameHeight, labelHeight, mobileLandscape);
            LayoutAccountRow(ageInputLabel, ageInput, x, inputX, ageY, labelWidth, Mathf.Min(ageWidth, inputWidth), ageHeight, Mathf.Min(labelHeight, ageHeight), mobileLandscape);
        }

        private void LayoutAccountRegistrationFields(float x, float topY, float width, float bottomLimit, float desiredFieldHeight, float desiredGap, bool mobileLandscape)
        {
            float labelWidth = Mathf.Clamp(width * 0.22f, mobileLandscape ? 230f : 150f, mobileLandscape ? 300f : 220f);
            float rowGap = Mathf.Clamp(desiredGap * 1.45f, mobileLandscape ? 24f : 14f, mobileLandscape ? 42f : 26f);
            float labelGap = Mathf.Clamp(width * 0.026f, mobileLandscape ? 24f : 16f, mobileLandscape ? 38f : 26f);
            float inputX = x + labelWidth + labelGap;
            float inputWidth = Mathf.Max(mobileLandscape ? 760f : 300f, width - labelWidth - labelGap);
            float fieldHeight = Mathf.Clamp(desiredFieldHeight * 1.12f, mobileLandscape ? 96f : 58f, mobileLandscape ? 126f : 76f);
            float labelHeight = Mathf.Min(fieldHeight, mobileLandscape ? 86f : 58f);
            float required = fieldHeight * 3f + rowGap * 2f;
            float available = Mathf.Max(1f, topY - bottomLimit);

            if (required > available)
            {
                float scale = Mathf.Clamp01(available / required);
                fieldHeight = Mathf.Max(mobileLandscape ? 70f : 44f, fieldHeight * scale);
                rowGap = Mathf.Max(mobileLandscape ? 12f : 8f, rowGap * scale);
                labelHeight = Mathf.Min(fieldHeight, labelHeight * scale);
            }

            float blockHeight = fieldHeight * 3f + rowGap * 2f;
            float blockTop = Mathf.Clamp(topY - Mathf.Max(0f, available - blockHeight) * 0.28f, bottomLimit + blockHeight, topY);
            LayoutAccountRow(dynastyInputLabel, dynastyInput, x, inputX, blockTop - fieldHeight, labelWidth, inputWidth, fieldHeight, labelHeight, mobileLandscape);
            LayoutAccountRow(emailInputLabel, emailInput, x, inputX, blockTop - fieldHeight * 2f - rowGap, labelWidth, inputWidth, fieldHeight, labelHeight, mobileLandscape);
            LayoutAccountRow(passwordInputLabel, passwordInput, x, inputX, blockTop - fieldHeight * 3f - rowGap * 2f, labelWidth, inputWidth, fieldHeight, labelHeight, mobileLandscape);
        }

        private void LayoutLegalConsent(float x, float y, float width, float height, bool mobileLandscape)
        {
            float gap = mobileLandscape ? 12f : 8f;
            float buttonWidth = Mathf.Clamp(width * 0.24f, mobileLandscape ? 180f : 120f, mobileLandscape ? 280f : 190f);
            float toggleWidth = Mathf.Max(120f, width - buttonWidth - gap);

            SetRect(termsToggle != null ? termsToggle.transform as RectTransform : null, x, y, toggleWidth, height);
            SetRect(termsButton != null ? termsButton.transform as RectTransform : null, x + toggleWidth + gap, y, buttonWidth, height);

            TMP_Text label = termsToggle != null ? termsToggle.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = mobileLandscape ? 14f : 12f;
                label.fontSizeMax = mobileLandscape ? 23f : 18f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }

        private void LayoutAccountLoginFields(float x, float topY, float width, float bottomLimit, float desiredFieldHeight, float desiredGap, bool mobileLandscape)
        {
            float formWidth = Mathf.Min(width, mobileLandscape ? 1080f : 720f);
            float formX = x + (width - formWidth) * 0.5f;
            float labelWidth = Mathf.Clamp(formWidth * 0.15f, mobileLandscape ? 150f : 100f, mobileLandscape ? 210f : 160f);
            float rowGap = Mathf.Clamp(desiredGap * 1.75f, mobileLandscape ? 34f : 18f, mobileLandscape ? 58f : 34f);
            float labelGap = Mathf.Clamp(formWidth * 0.026f, mobileLandscape ? 26f : 16f, mobileLandscape ? 42f : 28f);
            float labelX = formX;
            float inputX = formX + labelWidth + labelGap;
            float inputWidth = Mathf.Max(mobileLandscape ? 760f : 320f, formWidth - labelWidth - labelGap);
            float fieldHeight = Mathf.Clamp(desiredFieldHeight * 1.5f, mobileLandscape ? 124f : 66f, mobileLandscape ? 156f : 92f);
            float labelHeight = Mathf.Min(fieldHeight, mobileLandscape ? 86f : 58f);
            float required = fieldHeight * 2f + rowGap;
            float available = Mathf.Max(1f, topY - bottomLimit);

            if (required > available)
            {
                float scale = Mathf.Clamp01(available / required);
                fieldHeight = Mathf.Max(mobileLandscape ? 90f : 50f, fieldHeight * scale);
                rowGap = Mathf.Max(mobileLandscape ? 18f : 10f, rowGap * scale);
                labelHeight = Mathf.Min(fieldHeight, labelHeight * scale);
            }

            float blockHeight = fieldHeight * 2f + rowGap;
            float blockTop = Mathf.Clamp(topY - Mathf.Max(0f, available - blockHeight) * 0.35f, bottomLimit + blockHeight, topY);
            LayoutAccountRow(emailInputLabel, emailInput, labelX, inputX, blockTop - fieldHeight, labelWidth, inputWidth, fieldHeight, labelHeight, mobileLandscape);
            LayoutAccountRow(passwordInputLabel, passwordInput, labelX, inputX, blockTop - fieldHeight * 2f - rowGap, labelWidth, inputWidth, fieldHeight, labelHeight, mobileLandscape);
        }

        private void LayoutAccountRow(TextMeshProUGUI label, TMP_InputField input, float labelX, float inputX, float y, float labelWidth, float inputWidth, float fieldHeight, float labelHeight, bool mobileLandscape)
        {
            SetTextSize(label, mobileLandscape ? 34f : 26f);
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.MidlineRight;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Truncate;
            }

            ApplyInputTextViewportPadding(input, inputWidth, fieldHeight, mobileLandscape);
            SetInputTextSize(input, Mathf.Min(mobileLandscape ? 36f : 26f, Mathf.Max(22f, fieldHeight * 0.5f)));
            SetRect(label != null ? label.rectTransform : null, labelX, y + (fieldHeight - labelHeight) * 0.5f, labelWidth, labelHeight);
            SetRect(input != null ? input.transform as RectTransform : null, inputX, y, inputWidth, fieldHeight);
        }

        private static void ApplyInputTextViewportPadding(TMP_InputField input, float inputWidth, float fieldHeight, bool mobileLandscape)
        {
            if (input == null || input.textViewport == null)
                return;

            float horizontalPadding = Mathf.Clamp(inputWidth * 0.16f, mobileLandscape ? 92f : 54f, mobileLandscape ? 150f : 92f);
            float verticalPadding = Mathf.Clamp(fieldHeight * 0.12f, 6f, mobileLandscape ? 16f : 12f);
            StretchWithPadding(input.textViewport, horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        }

        private void LayoutProfileDetailsInputs(float x, float topY, float width, float slotX, float slotButtonWidth, float slotButtonGap, float ageWidth, float desiredLabelHeight, float desiredSlotHeight, float desiredNameHeight, float desiredAgeHeight, float desiredGap, float bottomLimit, bool mobileLandscape)
        {
            float labelHeight = desiredLabelHeight;
            float slotHeight = desiredSlotHeight;
            float nameHeight = desiredNameHeight;
            float ageHeight = desiredAgeHeight;
            float gap = Mathf.Max(mobileLandscape ? 5f : 4f, desiredGap);
            float available = Mathf.Max(1f, topY - bottomLimit);
            float required = labelHeight + slotHeight + nameHeight + ageHeight + gap * 3f;

            if (required > available)
            {
                float scale = Mathf.Clamp01(available / required);
                float minLabelHeight = mobileLandscape ? 0f : 18f;
                float minSlotHeight = mobileLandscape ? 42f : 34f;
                float minNameHeight = mobileLandscape ? 36f : 30f;
                float minAgeHeight = mobileLandscape ? 32f : 28f;
                float minGap = mobileLandscape ? 4f : 3f;

                labelHeight = Mathf.Max(minLabelHeight, labelHeight * scale);
                slotHeight = Mathf.Max(minSlotHeight, slotHeight * scale);
                nameHeight = Mathf.Max(minNameHeight, nameHeight * scale);
                ageHeight = Mathf.Max(minAgeHeight, ageHeight * scale);
                gap = Mathf.Max(minGap, gap * scale);

                float overflow = labelHeight + slotHeight + nameHeight + ageHeight + gap * 3f - available;
                ReduceLayoutOverflow(ref labelHeight, minLabelHeight, ref overflow);
                ReduceLayoutOverflow(ref slotHeight, minSlotHeight, ref overflow);
                ReduceLayoutOverflow(ref nameHeight, minNameHeight, ref overflow);
                ReduceLayoutOverflow(ref ageHeight, minAgeHeight, ref overflow);
                ReduceLayoutOverflow(ref gap, minGap, ref overflow);

                if (overflow > 0.01f)
                {
                    float squeeze = Mathf.Clamp01(available / Mathf.Max(1f, labelHeight + slotHeight + nameHeight + ageHeight + gap * 3f));
                    labelHeight *= squeeze;
                    slotHeight *= squeeze;
                    nameHeight *= squeeze;
                    ageHeight *= squeeze;
                    gap *= squeeze;
                }
            }

            float currentTop = topY;
            float labelY = currentTop - labelHeight;
            SetTextSize(slotLabelText, Mathf.Min(mobileLandscape ? 36f : 24f, Mathf.Max(16f, labelHeight * 0.58f)));
            SetRect(slotLabelText != null ? slotLabelText.rectTransform : null, x, labelY, width, labelHeight);
            currentTop = labelY - gap;

            float slotY = currentTop - slotHeight;
            SetButtonTextSize(slotOneButton, Mathf.Min(mobileLandscape ? 35f : 24f, Mathf.Max(16f, slotHeight * 0.42f)));
            SetButtonTextSize(slotTwoButton, Mathf.Min(mobileLandscape ? 35f : 24f, Mathf.Max(16f, slotHeight * 0.42f)));
            SetButtonTextSize(slotThreeButton, Mathf.Min(mobileLandscape ? 35f : 24f, Mathf.Max(16f, slotHeight * 0.42f)));
            SetRect(slotOneButton != null ? slotOneButton.transform as RectTransform : null, slotX, slotY, slotButtonWidth, slotHeight);
            SetRect(slotTwoButton != null ? slotTwoButton.transform as RectTransform : null, slotX + slotButtonWidth + slotButtonGap, slotY, slotButtonWidth, slotHeight);
            SetRect(slotThreeButton != null ? slotThreeButton.transform as RectTransform : null, slotX + (slotButtonWidth + slotButtonGap) * 2f, slotY, slotButtonWidth, slotHeight);
            currentTop = slotY - gap;

            SetInputTextSize(nameInput, Mathf.Min(mobileLandscape ? 38f : 28f, Mathf.Max(18f, nameHeight * 0.58f)));
            SetInputTextSize(ageInput, Mathf.Min(mobileLandscape ? 38f : 28f, Mathf.Max(18f, ageHeight * 0.58f)));
            float nameY = currentTop - nameHeight;
            SetRect(nameInput != null ? nameInput.transform as RectTransform : null, x, nameY, width, nameHeight);
            currentTop = nameY - gap;

            float ageY = Mathf.Max(bottomLimit, currentTop - ageHeight);
            SetRect(ageInput != null ? ageInput.transform as RectTransform : null, x, ageY, ageWidth, ageHeight);
        }

        private static void ReduceLayoutOverflow(ref float value, float minimum, ref float overflow)
        {
            if (overflow <= 0f)
                return;

            float reduction = Mathf.Min(value - minimum, overflow);
            if (reduction <= 0f)
                return;

            value -= reduction;
            overflow -= reduction;
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
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SafeProfileInputField), typeof(MobileTmpInputKeyboardBridge));
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
            StretchWithPadding(textAreaRect, 96f, 10f, 96f, 10f);

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

        private TextMeshProUGUI CreateAccountInputLabel(Transform parent, string objectName, string label)
        {
            TextMeshProUGUI text = CreateText(parent, objectName, label, 22f, FontStyles.Bold, new Color(0.84f, 0.9f, 1f, 0.94f));
            Anchor(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
            text.alignment = TextAlignmentOptions.MidlineRight;
            MainLobbyButtonStyle.ApplyFont(text);
            return text;
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
            text.overflowMode = TextOverflowModes.Truncate;
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
            label.overflowMode = TextOverflowModes.Truncate;
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

        private void SetGeneratedRootVisible(bool visible)
        {
            if (generatedRoot != null && generatedRoot.gameObject.activeSelf != visible)
                generatedRoot.gameObject.SetActive(visible);
        }

        private Transform GetProfileFullscreenParent()
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            return parentCanvas != null ? parentCanvas.transform : transform;
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

        private static void SetTextValue(TMP_Text text, string value)
        {
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
