using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class ProfileSetupUI : MonoBehaviour
    {
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

        private RectTransform generatedRoot;
        private RectTransform windowRect;
        private RectTransform leftPaneRect;
        private RectTransform rightPaneRect;
        private Button previousAvatarButton;
        private Button nextAvatarButton;
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
        private Button backButton;
        private Toggle rememberToggle;
        private Image slotOneAvatarImage;
        private Image slotTwoAvatarImage;
        private Image slotThreeAvatarImage;
        private TextMeshProUGUI registerStepText;
        private TextMeshProUGUI idPreviewText;
        private TextMeshProUGUI avatarIndexText;
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
        private bool sanitizingNameInput;

        private enum RegisterStep
        {
            Account,
            Gender,
            Details
        }

        private void Awake()
        {
            BuildLandscapeProfileWindow();
            BindButtons();
            ConfigureInput();
            ApplyAvatarVisual();
            RefreshGenderButtons();
            SetError(string.Empty);
        }

        private void OnEnable()
        {
            BuildLandscapeProfileWindow();
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
            UnbindButtons();
        }

        private void OnDisable()
        {
            ReleaseActiveInputs();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyResponsiveLayout();
        }

        private void BuildLandscapeProfileWindow()
        {
            if (generatedRoot != null)
                return;

            HideLegacyChildren();

            GameObject root = new GameObject("ProfileSetupLandscapeRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(transform, false);
            root.transform.SetAsLastSibling();
            generatedRoot = root.GetComponent<RectTransform>();
            Stretch(generatedRoot);

            Image rootImage = root.GetComponent<Image>();
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
            windowImage.color = new Color(0.075f, 0.095f, 0.13f, 0.98f);
            windowImage.raycastTarget = true;

            TextMeshProUGUI title = CreateText(window.transform, "Title", GameLocalization.Text("profile.setup.title"), 46f, FontStyles.Bold, Color.white);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            TextMeshProUGUI subtitle = CreateText(window.transform, "Subtitle", GameLocalization.Text("profile.setup.subtitle"), 24f, FontStyles.Normal, new Color(0.76f, 0.84f, 0.94f, 1f));
            Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            GameObject leftPane = CreatePane(window.transform, "AvatarPane", new Color(0.115f, 0.14f, 0.185f, 1f));
            leftPaneRect = leftPane.GetComponent<RectTransform>();

            TextMeshProUGUI avatarTitle = CreateText(leftPane.transform, "AvatarTitle", GameLocalization.Text("profile.setup.avatar"), 30f, FontStyles.Bold, Color.white);
            Anchor(avatarTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            avatarPreview = CreateImage(leftPane.transform, "AvatarPreview", Color.white);
            avatarPreview.preserveAspect = true;

            previousAvatarButton = CreateButton(leftPane.transform, "PreviousAvatarButton", "<", 34f);
            nextAvatarButton = CreateButton(leftPane.transform, "NextAvatarButton", ">", 34f);

            avatarIndexText = CreateText(leftPane.transform, "AvatarCounter", string.Empty, 22f, FontStyles.Normal, new Color(0.78f, 0.84f, 0.94f, 1f));
            Anchor(avatarIndexText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            GameObject rightPane = CreatePane(window.transform, "DetailsPane", new Color(0.095f, 0.115f, 0.155f, 1f));
            rightPaneRect = rightPane.GetComponent<RectTransform>();

            dynastyTabButton = CreateButton(rightPane.transform, "RegisterTabButton", GameLocalization.Text("profile.setup.register"), 22f);
            profileTabButton = CreateButton(rightPane.transform, "LoginTabButton", GameLocalization.Text("profile.setup.login"), 22f);

            registerStepText = CreateText(rightPane.transform, "RegisterStepText", string.Empty, 22f, FontStyles.Bold, new Color(0.78f, 0.86f, 1f));
            Anchor(registerStepText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            idPreviewText = CreateText(rightPane.transform, "IdPreview", GameLocalization.Text("profile.setup.id_auto"), 22f, FontStyles.Normal, new Color(0.68f, 0.78f, 0.92f, 1f));
            Anchor(idPreviewText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            dynastyInput = CreateInputField(rightPane.transform, "DynastyInput", GameLocalization.Text("profile.setup.dynasty"));
            nameInput = CreateInputField(rightPane.transform, "NameInput", GameLocalization.Text("profile.setup.nickname"));
            emailInput = CreateInputField(rightPane.transform, "EmailInput", GameLocalization.Text("profile.setup.email"));
            emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
            emailInput.keyboardType = TouchScreenKeyboardType.EmailAddress;
            emailInput.characterLimit = 64;

            passwordInput = CreateInputField(rightPane.transform, "PasswordInput", GameLocalization.Text("profile.setup.password"));
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.characterLimit = 64;

            rememberToggle = CreateToggle(rightPane.transform, "RememberProfileToggle", GameLocalization.Text("profile.setup.remember"));
            rememberToggle.isOn = true;

            ageInput = CreateInputField(rightPane.transform, "AgeInput", GameLocalization.Text("profile.setup.age"));
            ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            ageInput.characterLimit = 3;

            slotLabelText = CreateText(rightPane.transform, "SlotLabel", GameLocalization.Text("profile.setup.slot"), 24f, FontStyles.Bold, Color.white);
            Anchor(slotLabelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            slotOneButton = CreateButton(rightPane.transform, "SlotOneButton", "1", 24f);
            slotTwoButton = CreateButton(rightPane.transform, "SlotTwoButton", "2", 24f);
            slotThreeButton = CreateButton(rightPane.transform, "SlotThreeButton", "3", 24f);
            slotOneAvatarImage = CreateSlotAvatarImage(slotOneButton.transform, "SlotOneAvatar");
            slotTwoAvatarImage = CreateSlotAvatarImage(slotTwoButton.transform, "SlotTwoAvatar");
            slotThreeAvatarImage = CreateSlotAvatarImage(slotThreeButton.transform, "SlotThreeAvatar");

            TextMeshProUGUI genderLabel = CreateText(rightPane.transform, "GenderLabel", GameLocalization.Text("profile.setup.gender"), 24f, FontStyles.Bold, Color.white);
            Anchor(genderLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));

            maleButton = CreateButton(rightPane.transform, "MaleButton", GameLocalization.Text("profile.setup.male"), 22f);
            femaleButton = CreateButton(rightPane.transform, "FemaleButton", GameLocalization.Text("profile.setup.female"), 22f);
            otherButton = CreateButton(rightPane.transform, "OtherButton", GameLocalization.Text("profile.setup.other"), 22f);

            errorText = CreateText(rightPane.transform, "ErrorText", string.Empty, 22f, FontStyles.Bold, new Color(1f, 0.48f, 0.42f, 1f));
            Anchor(errorText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            continueButton = CreateButton(rightPane.transform, "ContinueButton", GameLocalization.Text("common.continue"), 24f);
            loginButton = CreateButton(rightPane.transform, "LoginButton", GameLocalization.Text("profile.setup.login"), 24f);
            backButton = CreateButton(rightPane.transform, "BackButton", GameLocalization.Text("mahjong.back"), 24f);

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

            if (dynastyTabButton != null)
                dynastyTabButton.onClick.RemoveListener(ShowRegisterMode);

            if (profileTabButton != null)
                profileTabButton.onClick.RemoveListener(ShowLoginMode);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnClickContinue);

            if (loginButton != null)
                loginButton.onClick.RemoveListener(OnClickLogin);

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
                SetError("This slot is already occupied.");
                return;
            }

            selectedSlotIndex = clampedSlotIndex;
            RefreshSlotButtons();
            RefreshTabButtons();
            SetError(string.Empty);

            if (loginMode && loginSlotsLoaded && IsSelectedLoginSlotInUseByOtherDevice())
            {
                SetError("This profile is in use on another device.");
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
                SetError(GameLocalization.Text("profile.error.avatars_missing"));
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
                SetError(GameLocalization.Text("profile.error.avatars_missing"));
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
                        SetError("Choose male or female.");
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
                SetError(GameLocalization.Text("profile.error.setup_failed"));
                ResetContinueState();
            }
        }

        private void OnClickLogin()
        {
            if (!loginMode || !loginSlotsLoaded)
                ShowLoginMode();

            StartLoginFlow();
        }

        private void OnClickBack()
        {
            if (continueInProgress)
                return;

            ReleaseActiveInputs();
            SetError(string.Empty);

            if (loginMode)
            {
                if (loginSlotsLoaded)
                {
                    loginSlotsLoaded = false;
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
                    SetError("This profile is in use on another device.");
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
                SetError("Login failed. Please try again.");
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

        private string FormatLoginError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return "Login failed.";

            if (error.IndexOf("profile not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("no profile", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Account not found. Open Register to create a new profile.";
            }

            return error;
        }

        private string FormatRegisterError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return GameLocalization.Text("profile.error.server");

            if (error.IndexOf("profile not found", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Registration session expired. Please press Register again.";

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
            SetError("Create a profile in this free slot.");
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

            if (backButton != null)
                backButton.interactable = value;
        }

        private string ValidateAndNormalizeName(string rawName)
        {
            string value = string.IsNullOrWhiteSpace(rawName) ? string.Empty : rawName.Trim();

            if (string.IsNullOrEmpty(value))
            {
                SetError(GameLocalization.Text("profile.error.enter_name"));
                return null;
            }

            if (value.Length < minNameLength)
            {
                SetError(GameLocalization.Format("profile.error.name_too_short", minNameLength));
                return null;
            }

            if (!IsLatinLettersOnly(value))
            {
                SetError(GameLocalization.Text("profile.error.name_latin_only"));
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
                SetError("Enter Dynasty Name.");
                return null;
            }

            if (value.Length < 2)
            {
                SetError("Dynasty Name is too short.");
                return null;
            }

            return value.Length > 48 ? value.Substring(0, 48) : value;
        }

        private string ValidateAndNormalizeEmail(string rawEmail)
        {
            string value = string.IsNullOrWhiteSpace(rawEmail) ? string.Empty : rawEmail.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(value))
            {
                SetError(GameLocalization.Text("profile.error.enter_email"));
                return null;
            }

            int at = value.IndexOf('@');
            int dot = value.LastIndexOf('.');
            if (at <= 0 || dot <= at + 1 || dot >= value.Length - 1)
            {
                SetError(GameLocalization.Text("profile.error.email_invalid"));
                return null;
            }

            return value;
        }

        private string ValidatePassword(string rawPassword)
        {
            string value = rawPassword ?? string.Empty;
            if (value.Length < 6)
            {
                SetError(GameLocalization.Text("profile.error.password_short"));
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
                SetError(GameLocalization.Text("profile.error.age_invalid"));
                return false;
            }

            if (age < minAge || age > maxAge)
            {
                SetError(GameLocalization.Text("profile.error.age_invalid"));
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
                    avatarIndexText.text = GameLocalization.Text("profile.error.no_avatars");
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

        private void RefreshSlotButtons()
        {
            ApplySlotButton(slotOneButton, 1);
            ApplySlotButton(slotTwoButton, 2);
            ApplySlotButton(slotThreeButton, 3);
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
                    return $"{slotIndex}\nBusy";

                if (slot.Occupied)
                {
                    string nickname = string.IsNullOrWhiteSpace(slot.Nickname) ? "Profile" : slot.Nickname;
                    return $"{slotIndex}\n{nickname}";
                }

                return "+\nFree";
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
                text.margin = new Vector4(8f, 42f, 8f, 6f);
            }
            else if (HasAccountSlotOverview())
            {
                text.alignment = TextAlignmentOptions.Center;
                text.margin = new Vector4(8f, 0f, 8f, 0f);
            }
            else
            {
                text.alignment = TextAlignmentOptions.Center;
                text.margin = new Vector4(8f, 0f, 8f, 0f);
            }
        }

        private void RefreshTabButtons()
        {
            ApplyTabButton(dynastyTabButton, !loginMode);
            ApplyTabButton(profileTabButton, loginMode);

            if (registerStepText != null)
            {
                registerStepText.text = loginMode
                    ? "Login"
                    : registerStep == RegisterStep.Account
                        ? "Dynasty Account"
                        : registerStep == RegisterStep.Gender
                            ? "Choose Gender"
                            : "Profile Details";
            }

            SetButtonLabel(continueButton, loginMode
                ? GameLocalization.Text("profile.setup.login")
                : registerStep == RegisterStep.Details
                    ? GameLocalization.Text("profile.setup.register")
                    : "Next");
            SetButtonLabel(loginButton, loginMode && loginSlotsLoaded
                ? IsSelectedLoginSlotInUseByOtherDevice()
                    ? "Busy"
                    : IsSelectedLoginSlotOccupied() ? "Enter Slot" : "Create Slot"
                : GameLocalization.Text("profile.setup.login"));
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
            if (root == null || windowRect == null || leftPaneRect == null || rightPaneRect == null)
                return;

            float rootWidth = Mathf.Max(1280f, root.rect.width);
            float rootHeight = Mathf.Max(720f, root.rect.height);

            float windowWidth = Mathf.Clamp(rootWidth * 0.94f, 1120f, 1760f);
            float windowHeight = Mathf.Clamp(rootHeight * 0.88f, 620f, 900f);
            windowRect.sizeDelta = new Vector2(windowWidth, windowHeight);

            float pad = 42f;
            SetRect(windowRect.Find("Title") as RectTransform, pad, windowHeight - 78f, windowWidth - pad * 2f, 58f);
            SetRect(windowRect.Find("Subtitle") as RectTransform, pad, windowHeight - 120f, windowWidth - pad * 2f, 34f);

            float bodyTop = windowHeight - 146f;
            float bodyHeight = windowHeight - 196f;
            float gap = 32f;
            float leftWidth = Mathf.Clamp(windowWidth * 0.34f, 380f, 540f);
            float rightWidth = windowWidth - leftWidth - gap - pad * 2f;

            SetRect(leftPaneRect, pad, bodyTop - bodyHeight, leftWidth, bodyHeight);
            SetRect(rightPaneRect, pad + leftWidth + gap, bodyTop - bodyHeight, rightWidth, bodyHeight);

            LayoutLeftPane(leftWidth, bodyHeight);
            LayoutRightPane(rightWidth, bodyHeight);
        }

        private void LayoutLeftPane(float width, float height)
        {
            if (leftPaneRect == null)
                return;

            float avatarSize = Mathf.Min(width - 144f, height - 170f);
            avatarSize = Mathf.Clamp(avatarSize, 240f, 390f);
            float centerY = height * 0.52f - avatarSize * 0.5f;

            SetRect(leftPaneRect.Find("AvatarTitle") as RectTransform, 28f, height - 70f, width - 56f, 42f);
            SetRect(avatarPreview != null ? avatarPreview.rectTransform : null, (width - avatarSize) * 0.5f, centerY, avatarSize, avatarSize);
            SetRect(previousAvatarButton != null ? previousAvatarButton.transform as RectTransform : null, 26f, centerY + avatarSize * 0.5f - 40f, 80f, 80f);
            SetRect(nextAvatarButton != null ? nextAvatarButton.transform as RectTransform : null, width - 106f, centerY + avatarSize * 0.5f - 40f, 80f, 80f);
            SetRect(avatarIndexText != null ? avatarIndexText.rectTransform : null, 28f, 34f, width - 56f, 34f);
        }

        private void LayoutRightPane(float width, float height)
        {
            if (rightPaneRect == null)
                return;

            float x = 34f;
            float fieldWidth = width - 68f;
            float y = height - 64f;
            float tabGap = 16f;
            float tabWidth = (fieldWidth - tabGap) * 0.5f;

            SetRect(dynastyTabButton != null ? dynastyTabButton.transform as RectTransform : null, x, y, tabWidth, 54f);
            SetRect(profileTabButton != null ? profileTabButton.transform as RectTransform : null, x + tabWidth + tabGap, y, tabWidth, 54f);
            y -= 66f;

            SetRect(registerStepText != null ? registerStepText.rectTransform : null, x, y, fieldWidth, 32f);
            y -= 38f;

            SetRect(idPreviewText != null ? idPreviewText.rectTransform : null, x, y, fieldWidth, 32f);
            y -= 52f;

            bool showLoginSlotPicker = loginMode && loginSlotsLoaded;
            bool showAccountSlotOverview = HasAccountSlotOverview();
            bool showAccountFields = (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots) || (!loginMode && registerStep == RegisterStep.Account);
            bool showGenderFields = !loginMode && registerStep == RegisterStep.Gender;
            bool showSlotFields = showLoginSlotPicker || registerStep == RegisterStep.Details;
            bool showProfileFields = !loginMode && registerStep == RegisterStep.Details;
            bool showRemember = (loginMode && !loginSlotsLoaded && !loadingRememberedAccountSlots) || (!loginMode && registerStep == RegisterStep.Account);
            bool showAvatar = showProfileFields;
            bool showBack = loginSlotsLoaded || (!loginMode && registerStep != RegisterStep.Account) || creatingSlotForExistingAccount;

            SetObjectActive(avatarPreview != null ? avatarPreview.gameObject : null, showAvatar);
            SetObjectActive(previousAvatarButton != null ? previousAvatarButton.gameObject : null, showAvatar);
            SetObjectActive(nextAvatarButton != null ? nextAvatarButton.gameObject : null, showAvatar);
            SetObjectActive(avatarIndexText != null ? avatarIndexText.gameObject : null, showAvatar);

            SetObjectActive(dynastyInput != null ? dynastyInput.gameObject : null, !loginMode && registerStep == RegisterStep.Account);
            SetObjectActive(emailInput != null ? emailInput.gameObject : null, showAccountFields);
            SetObjectActive(passwordInput != null ? passwordInput.gameObject : null, showAccountFields);
            SetObjectActive(rememberToggle != null ? rememberToggle.gameObject : null, showRemember);
            SetObjectActive(continueButton != null ? continueButton.gameObject : null, !loginMode);
            SetObjectActive(loginButton != null ? loginButton.gameObject : null, loginMode);
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
                SetRect(emailInput != null ? emailInput.transform as RectTransform : null, x, y, fieldWidth, 64f);
                y -= 74f;
                SetRect(passwordInput != null ? passwordInput.transform as RectTransform : null, x, y, fieldWidth, 64f);
                y -= 74f;
            }
            else if (loginMode)
            {
                SetRect(slotLabelText != null ? slotLabelText.rectTransform : null, x, y, fieldWidth, 38f);
                y -= 56f;

                float slotButtonGap = 16f;
                float slotButtonWidth = (fieldWidth - slotButtonGap * 2f) / 3f;
                SetRect(slotOneButton != null ? slotOneButton.transform as RectTransform : null, x, y, slotButtonWidth, 112f);
                SetRect(slotTwoButton != null ? slotTwoButton.transform as RectTransform : null, x + slotButtonWidth + slotButtonGap, y, slotButtonWidth, 112f);
                SetRect(slotThreeButton != null ? slotThreeButton.transform as RectTransform : null, x + (slotButtonWidth + slotButtonGap) * 2f, y, slotButtonWidth, 112f);
                y -= 132f;

                SetRect(rememberToggle != null ? rememberToggle.transform as RectTransform : null, x, y, fieldWidth, 48f);
            }
            else if (registerStep == RegisterStep.Account)
            {
                SetRect(dynastyInput != null ? dynastyInput.transform as RectTransform : null, x, y, fieldWidth, 64f);
                y -= 74f;
                SetRect(emailInput != null ? emailInput.transform as RectTransform : null, x, y, fieldWidth, 64f);
                y -= 74f;
                SetRect(passwordInput != null ? passwordInput.transform as RectTransform : null, x, y, fieldWidth, 64f);
                y -= 74f;
                SetRect(rememberToggle != null ? rememberToggle.transform as RectTransform : null, x, y, fieldWidth, 48f);
            }
            else if (registerStep == RegisterStep.Gender)
            {
                SetRect(rightPaneRect.Find("GenderLabel") as RectTransform, x, y, fieldWidth, 40f);
                y -= 76f;

                float buttonGap = 18f;
                float genderButtonWidth = (fieldWidth - buttonGap) * 0.5f;
                SetRect(maleButton != null ? maleButton.transform as RectTransform : null, x, y, genderButtonWidth, 76f);
                SetRect(femaleButton != null ? femaleButton.transform as RectTransform : null, x + genderButtonWidth + buttonGap, y, genderButtonWidth, 76f);
            }
            else
            {
                SetRect(slotLabelText != null ? slotLabelText.rectTransform : null, x, y, fieldWidth, 34f);
                y -= 58f;

                float slotButtonGap = 16f;
                float slotButtonWidth = (fieldWidth - slotButtonGap * 2f) / 3f;
                SetRect(slotOneButton != null ? slotOneButton.transform as RectTransform : null, x, y, slotButtonWidth, 58f);
                SetRect(slotTwoButton != null ? slotTwoButton.transform as RectTransform : null, x + slotButtonWidth + slotButtonGap, y, slotButtonWidth, 58f);
                SetRect(slotThreeButton != null ? slotThreeButton.transform as RectTransform : null, x + (slotButtonWidth + slotButtonGap) * 2f, y, slotButtonWidth, 58f);
                y -= 74f;

                SetRect(nameInput != null ? nameInput.transform as RectTransform : null, x, y, fieldWidth, 64f);
                y -= 74f;
                SetRect(ageInput != null ? ageInput.transform as RectTransform : null, x, y, Mathf.Min(320f, fieldWidth), 60f);
                y -= 48f;
            }

            SetRect(errorText != null ? errorText.rectTransform : null, x, 100f, fieldWidth, 40f);

            float bottomGap = 18f;
            float bottomButtonWidth = (fieldWidth - bottomGap) * 0.5f;
            RectTransform primaryButtonRect = loginMode
                ? loginButton != null ? loginButton.transform as RectTransform : null
                : continueButton != null ? continueButton.transform as RectTransform : null;

            if (showBack)
            {
                SetRect(backButton != null ? backButton.transform as RectTransform : null, x, 28f, bottomButtonWidth, 64f);
                SetRect(primaryButtonRect, x + bottomButtonWidth + bottomGap, 28f, bottomButtonWidth, 64f);
            }
            else
            {
                SetRect(loginButton != null ? loginButton.transform as RectTransform : null, x, 28f, fieldWidth, 64f);
                SetRect(continueButton != null ? continueButton.transform as RectTransform : null,
                    loginMode || registerStep != RegisterStep.Account ? x : x + bottomButtonWidth + bottomGap,
                    28f,
                    loginMode || registerStep != RegisterStep.Account ? fieldWidth : bottomButtonWidth,
                    64f);
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
            image.color = color;
            image.raycastTarget = true;
            return pane;
        }

        private TMP_InputField CreateInputField(Transform parent, string objectName, string placeholder)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SafeProfileInputField), typeof(SafeMobileInputDragBlocker));
            root.transform.SetParent(parent, false);

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.055f, 0.07f, 0.1f, 1f);
            image.raycastTarget = true;

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(root.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            StretchWithPadding(textAreaRect, 22f, 6f, 22f, 6f);

            TextMeshProUGUI placeholderText = CreateText(textArea.transform, "Placeholder", placeholder, 28f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.52f));
            Stretch(placeholderText.rectTransform);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

            TextMeshProUGUI text = CreateText(textArea.transform, "Text", string.Empty, 28f, FontStyles.Normal, Color.white);
            Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;

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
            image.color = new Color(0.13f, 0.16f, 0.22f, 1f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, fontSize, FontStyles.Bold, Color.white);
            Stretch(text.rectTransform);
            text.margin = new Vector4(8f, 0f, 8f, 0f);
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
            rect.anchoredPosition = new Vector2(0f, 13f);
            rect.sizeDelta = new Vector2(46f, 46f);
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
    }
}
