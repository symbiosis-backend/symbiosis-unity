using TMPro;
using System;
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
        [SerializeField] private TMP_InputField nameInput;
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
        private Button maleButton;
        private Button femaleButton;
        private Button otherButton;
        private Button continueButton;
        private TextMeshProUGUI idPreviewText;
        private TextMeshProUGUI avatarIndexText;
        private PlayerGender selectedGender = PlayerGender.NotSpecified;
        private int currentAvatarIndex;
        private bool continueInProgress;

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

            if (nameInput != null)
            {
                nameInput.text = string.Empty;

                if (!Application.isMobilePlatform)
                    nameInput.ActivateInputField();
            }

            if (ageInput != null)
                ageInput.text = string.Empty;

            selectedGender = PlayerGender.NotSpecified;
            currentAvatarIndex = Mathf.Clamp(currentAvatarIndex, 0, GetLastAvatarIndex());
            RefreshGenderButtons();
            ApplyAvatarVisual();
            ApplyResponsiveLayout();
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

            TextMeshProUGUI title = CreateText(window.transform, "Title", "Create Profile", 38f, FontStyles.Bold, Color.white);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            TextMeshProUGUI subtitle = CreateText(window.transform, "Subtitle", "Choose your avatar and fill in the profile details.", 20f, FontStyles.Normal, new Color(0.76f, 0.84f, 0.94f, 1f));
            Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            GameObject leftPane = CreatePane(window.transform, "AvatarPane", new Color(0.115f, 0.14f, 0.185f, 1f));
            leftPaneRect = leftPane.GetComponent<RectTransform>();

            TextMeshProUGUI avatarTitle = CreateText(leftPane.transform, "AvatarTitle", "Avatar", 26f, FontStyles.Bold, Color.white);
            Anchor(avatarTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            avatarPreview = CreateImage(leftPane.transform, "AvatarPreview", Color.white);
            avatarPreview.preserveAspect = true;

            previousAvatarButton = CreateButton(leftPane.transform, "PreviousAvatarButton", "<", 34f);
            nextAvatarButton = CreateButton(leftPane.transform, "NextAvatarButton", ">", 34f);

            avatarIndexText = CreateText(leftPane.transform, "AvatarCounter", string.Empty, 18f, FontStyles.Normal, new Color(0.78f, 0.84f, 0.94f, 1f));
            Anchor(avatarIndexText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            GameObject rightPane = CreatePane(window.transform, "DetailsPane", new Color(0.095f, 0.115f, 0.155f, 1f));
            rightPaneRect = rightPane.GetComponent<RectTransform>();

            idPreviewText = CreateText(rightPane.transform, "IdPreview", "ID will be assigned automatically", 18f, FontStyles.Normal, new Color(0.68f, 0.78f, 0.92f, 1f));
            Anchor(idPreviewText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            nameInput = CreateInputField(rightPane.transform, "NameInput", "Nickname");
            ageInput = CreateInputField(rightPane.transform, "AgeInput", "Age");
            ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            ageInput.characterLimit = 3;

            TextMeshProUGUI genderLabel = CreateText(rightPane.transform, "GenderLabel", "Gender", 20f, FontStyles.Bold, Color.white);
            Anchor(genderLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));

            maleButton = CreateButton(rightPane.transform, "MaleButton", "Male", 20f);
            femaleButton = CreateButton(rightPane.transform, "FemaleButton", "Female", 20f);
            otherButton = CreateButton(rightPane.transform, "OtherButton", "Other", 20f);

            errorText = CreateText(rightPane.transform, "ErrorText", string.Empty, 18f, FontStyles.Normal, new Color(1f, 0.45f, 0.42f, 1f));
            Anchor(errorText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            continueButton = CreateButton(rightPane.transform, "ContinueButton", "Continue", 24f);

            ApplyResponsiveLayout();
        }

        private void BindButtons()
        {
            if (previousAvatarButton != null)
                previousAvatarButton.onClick.AddListener(OnClickLeft);

            if (nextAvatarButton != null)
                nextAvatarButton.onClick.AddListener(OnClickRight);

            if (maleButton != null)
                maleButton.onClick.AddListener(() => SelectGender(PlayerGender.Male));

            if (femaleButton != null)
                femaleButton.onClick.AddListener(() => SelectGender(PlayerGender.Female));

            if (otherButton != null)
                otherButton.onClick.AddListener(() => SelectGender(PlayerGender.Other));

            if (continueButton != null)
                continueButton.onClick.AddListener(OnClickContinue);
        }

        private void UnbindButtons()
        {
            if (previousAvatarButton != null)
                previousAvatarButton.onClick.RemoveListener(OnClickLeft);

            if (nextAvatarButton != null)
                nextAvatarButton.onClick.RemoveListener(OnClickRight);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnClickContinue);
        }

        private void SelectGender(PlayerGender gender)
        {
            selectedGender = gender;
            RefreshGenderButtons();
            SetError(string.Empty);
        }

        private void OnClickLeft()
        {
            if (avatarSprites == null || avatarSprites.Length == 0)
            {
                SetError(GameLocalization.Text("profile.error.avatars_missing"));
                return;
            }

            currentAvatarIndex = currentAvatarIndex <= 0 ? avatarSprites.Length - 1 : currentAvatarIndex - 1;
            ApplyAvatarVisual();
            SetError(string.Empty);
        }

        private void OnClickRight()
        {
            if (avatarSprites == null || avatarSprites.Length == 0)
            {
                SetError(GameLocalization.Text("profile.error.avatars_missing"));
                return;
            }

            currentAvatarIndex = currentAvatarIndex >= avatarSprites.Length - 1 ? 0 : currentAvatarIndex + 1;
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

                int avatarId = avatarSprites == null || avatarSprites.Length == 0
                    ? 0
                    : Mathf.Clamp(currentAvatarIndex, 0, avatarSprites.Length - 1);

                ProfileBootstrap.LogRuntime($"CompleteProfile start. Avatar={avatarId}, Age={age}, Gender={selectedGender}");
                ProfileService.I.CompleteProfile(validatedName, avatarId, age, selectedGender, string.Empty);
                ProfileBootstrap.LogRuntime("CompleteProfile done");

                if (bootstrap == null)
                    bootstrap = FindAnyObjectByType<ProfileBootstrap>();

                if (bootstrap == null)
                {
                    SetError(GameLocalization.Text("profile.error.bootstrap_missing"));
                    ProfileBootstrap.LogRuntime("Bootstrap missing after profile complete");
                    ResetContinueState();
                    return;
                }

                StartCoroutine(ContinueAfterInputSettles());
            }
            catch (Exception ex)
            {
                ProfileBootstrap.LogRuntime("ProfileSetup continue exception: " + ex);
                Debug.LogError("[ProfileSetupUI] Continue failed: " + ex);
                SetError("Profile setup failed. Please restart the game.");
                ResetContinueState();
            }
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
            if (continueButton != null)
                continueButton.interactable = true;
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

            if (value.Length > maxNameLength)
                value = value.Substring(0, maxNameLength);

            return string.IsNullOrWhiteSpace(value) ? fallbackPlayerName : value;
        }

        private bool TryValidateAge(string rawAge, out int age)
        {
            age = 0;

            if (string.IsNullOrWhiteSpace(rawAge))
                return true;

            if (!int.TryParse(rawAge.Trim(), out age))
            {
                SetError("Age must be a number.");
                return false;
            }

            if (age < minAge || age > maxAge)
            {
                SetError($"Age must be from {minAge} to {maxAge}.");
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
            }

            if (ageInput != null)
            {
                ageInput.characterLimit = 3;
                ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                ageInput.lineType = TMP_InputField.LineType.SingleLine;
            }
        }

        private void ApplyAvatarVisual()
        {
            if (avatarPreview == null)
                return;

            if (avatarSprites == null || avatarSprites.Length == 0)
            {
                avatarPreview.sprite = null;
                avatarPreview.enabled = false;
                if (avatarIndexText != null)
                    avatarIndexText.text = "No avatars configured";
                return;
            }

            currentAvatarIndex = Mathf.Clamp(currentAvatarIndex, 0, avatarSprites.Length - 1);
            avatarPreview.enabled = true;
            avatarPreview.sprite = avatarSprites[currentAvatarIndex];

            if (avatarIndexText != null)
                avatarIndexText.text = $"{currentAvatarIndex + 1} / {avatarSprites.Length}";
        }

        private void RefreshGenderButtons()
        {
            ApplyGenderButton(maleButton, PlayerGender.Male);
            ApplyGenderButton(femaleButton, PlayerGender.Female);
            ApplyGenderButton(otherButton, PlayerGender.Other);
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
            return avatarSprites == null || avatarSprites.Length == 0 ? 0 : avatarSprites.Length - 1;
        }

        private void ApplyResponsiveLayout()
        {
            RectTransform root = transform as RectTransform;
            if (root == null || windowRect == null || leftPaneRect == null || rightPaneRect == null)
                return;

            float rootWidth = Mathf.Max(960f, root.rect.width);
            float rootHeight = Mathf.Max(540f, root.rect.height);

            float windowWidth = Mathf.Clamp(rootWidth * 0.82f, 900f, 1180f);
            float windowHeight = Mathf.Clamp(rootHeight * 0.74f, 460f, 640f);
            windowRect.sizeDelta = new Vector2(windowWidth, windowHeight);

            float pad = 34f;
            SetRect(windowRect.Find("Title") as RectTransform, pad, windowHeight - 72f, windowWidth - pad * 2f, 44f);
            SetRect(windowRect.Find("Subtitle") as RectTransform, pad, windowHeight - 112f, windowWidth - pad * 2f, 30f);

            float bodyTop = windowHeight - 134f;
            float bodyHeight = windowHeight - 184f;
            float gap = 28f;
            float leftWidth = Mathf.Clamp(windowWidth * 0.39f, 340f, 460f);
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

            float avatarSize = Mathf.Min(width - 130f, height - 150f);
            avatarSize = Mathf.Clamp(avatarSize, 190f, 310f);
            float centerY = height * 0.5f - avatarSize * 0.5f + 8f;

            SetRect(leftPaneRect.Find("AvatarTitle") as RectTransform, 24f, height - 62f, width - 48f, 34f);
            SetRect(avatarPreview != null ? avatarPreview.rectTransform : null, (width - avatarSize) * 0.5f, centerY, avatarSize, avatarSize);
            SetRect(previousAvatarButton != null ? previousAvatarButton.transform as RectTransform : null, 24f, centerY + avatarSize * 0.5f - 34f, 68f, 68f);
            SetRect(nextAvatarButton != null ? nextAvatarButton.transform as RectTransform : null, width - 92f, centerY + avatarSize * 0.5f - 34f, 68f, 68f);
            SetRect(avatarIndexText != null ? avatarIndexText.rectTransform : null, 24f, 28f, width - 48f, 28f);
        }

        private void LayoutRightPane(float width, float height)
        {
            if (rightPaneRect == null)
                return;

            float x = 28f;
            float fieldWidth = width - 56f;
            float y = height - 58f;

            SetRect(idPreviewText != null ? idPreviewText.rectTransform : null, x, y, fieldWidth, 28f);
            y -= 74f;
            SetRect(nameInput != null ? nameInput.transform as RectTransform : null, x, y, fieldWidth, 58f);
            y -= 78f;
            SetRect(ageInput != null ? ageInput.transform as RectTransform : null, x, y, Mathf.Min(260f, fieldWidth), 58f);
            y -= 46f;
            SetRect(rightPaneRect.Find("GenderLabel") as RectTransform, x, y, fieldWidth, 28f);
            y -= 62f;

            float buttonGap = 12f;
            float genderButtonWidth = (fieldWidth - buttonGap * 2f) / 3f;
            SetRect(maleButton != null ? maleButton.transform as RectTransform : null, x, y, genderButtonWidth, 54f);
            SetRect(femaleButton != null ? femaleButton.transform as RectTransform : null, x + genderButtonWidth + buttonGap, y, genderButtonWidth, 54f);
            SetRect(otherButton != null ? otherButton.transform as RectTransform : null, x + (genderButtonWidth + buttonGap) * 2f, y, genderButtonWidth, 54f);

            SetRect(errorText != null ? errorText.rectTransform : null, x, 84f, fieldWidth, 30f);
            SetRect(continueButton != null ? continueButton.transform as RectTransform : null, width - 268f, 24f, 240f, 58f);
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
            StretchWithPadding(textAreaRect, 18f, 4f, 18f, 4f);

            TextMeshProUGUI placeholderText = CreateText(textArea.transform, "Placeholder", placeholder, 24f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.42f));
            Stretch(placeholderText.rectTransform);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

            TextMeshProUGUI text = CreateText(textArea.transform, "Text", string.Empty, 24f, FontStyles.Normal, Color.white);
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
            return button;
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
            label.fontSizeMin = 12f;
            label.fontSizeMax = fontSize;
            label.raycastTarget = false;
            return label;
        }

        private void ReleaseActiveInputs()
        {
            if (nameInput != null)
                nameInput.DeactivateInputField();

            if (ageInput != null)
                ageInput.DeactivateInputField();

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
    }
}
