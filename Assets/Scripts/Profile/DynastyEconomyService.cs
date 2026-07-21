using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class DynastyEconomyLoc
    {
        public static string T(string ru, string en, string tr)
        {
            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            switch (language)
            {
                case GameLanguage.English:
                    return en;
                case GameLanguage.Turkish:
                    return tr;
                case GameLanguage.German:
                    return en;
                default:
                    return ru;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class DynastyVaultService : MonoBehaviour
    {
        public static DynastyVaultService I { get; private set; }
        public static event Action VaultChanged;

        private const string PrefPrefix = "symbiosis_dynasty_vault_";
        private const string GoldSuffix = "_gold";
        private const string AmetistSuffix = "_ametist";

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        public int GetOzAltin()
        {
            return PlayerPrefs.GetInt(GetStorageKey(GoldSuffix), 0);
        }

        public int GetOzAmetist()
        {
            return PlayerPrefs.GetInt(GetStorageKey(AmetistSuffix), 0);
        }

        public bool DepositOzAltin(int amount)
        {
            if (amount <= 0 || CurrencyService.I == null || !CurrencyService.I.SpendOzAltin(amount))
                return false;

            SetOzAltin(GetOzAltin() + amount);
            return true;
        }

        public bool WithdrawOzAltin(int amount)
        {
            if (amount <= 0 || CurrencyService.I == null)
                return false;

            int current = GetOzAltin();
            if (current < amount)
                return false;

            SetOzAltin(current - amount);
            CurrencyService.I.AddOzAltin(amount);
            return true;
        }

        public bool DepositOzAmetist(int amount)
        {
            if (amount <= 0 || CurrencyService.I == null || !CurrencyService.I.SpendOzAmetist(amount))
                return false;

            SetOzAmetist(GetOzAmetist() + amount);
            return true;
        }

        public bool WithdrawOzAmetist(int amount)
        {
            if (amount <= 0 || CurrencyService.I == null)
                return false;

            int current = GetOzAmetist();
            if (current < amount)
                return false;

            SetOzAmetist(current - amount);
            CurrencyService.I.AddOzAmetist(amount);
            return true;
        }

        private void SetOzAltin(int value)
        {
            PlayerPrefs.SetInt(GetStorageKey(GoldSuffix), Mathf.Max(0, value));
            PlayerPrefs.Save();
            VaultChanged?.Invoke();
        }

        private void SetOzAmetist(int value)
        {
            PlayerPrefs.SetInt(GetStorageKey(AmetistSuffix), Mathf.Max(0, value));
            PlayerPrefs.Save();
            VaultChanged?.Invoke();
        }

        private static string GetStorageKey(string suffix)
        {
            return PrefPrefix + GetAccountKey() + suffix;
        }

        private static string GetAccountKey()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile == null)
                return "local_guest";

            profile.EnsureData();

            if (!string.IsNullOrWhiteSpace(profile.DynastyId))
                return Sanitize(profile.DynastyId);

            if (!string.IsNullOrWhiteSpace(profile.DynastyName))
                return "dynasty_" + Sanitize(profile.DynastyName);

            if (!string.IsNullOrWhiteSpace(profile.OnlinePlayerId))
                return "account_" + Sanitize(profile.OnlinePlayerId);

            return "profile_" + Sanitize(profile.LocalProfileId);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "empty";

            string trimmed = value.Trim().ToLowerInvariant();
            char[] buffer = new char[trimmed.Length];
            int count = 0;

            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-')
                    buffer[count++] = c;
            }

            return count > 0 ? new string(buffer, 0, count) : "empty";
        }
    }

    [DisallowMultipleComponent]
    public sealed class DynastyBankService : MonoBehaviour
    {
        public static DynastyBankService I { get; private set; }
        public static event Action BankChanged;
        public const int DefaultGoldPerAmetist = 100;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        public bool ExchangeProfileAmetistToGold(int ametistAmount, int goldPerAmetist)
        {
            if (ametistAmount <= 0 || goldPerAmetist <= 0 || CurrencyService.I == null)
                return false;

            long goldAmount = (long)ametistAmount * goldPerAmetist;
            int currentGold = CurrencyService.I.GetOzAltin();
            if (goldAmount > int.MaxValue - (long)Mathf.Max(0, currentGold))
                return false;

            if (!CurrencyService.I.SpendOzAmetist(ametistAmount))
                return false;

            CurrencyService.I.AddOzAltin((int)goldAmount);
            BankChanged?.Invoke();
            return true;
        }
    }

    public static class DynastyEconomyRuntime
    {
        public static void EnsureServices()
        {
            if (DynastyVaultService.I == null)
            {
                GameObject vault = new GameObject("DynastyVaultService", typeof(DynastyVaultService));
                vault.transform.SetParent(null, false);
            }

            if (DynastyBankService.I == null)
            {
                GameObject bank = new GameObject("DynastyBankService", typeof(DynastyBankService));
                bank.transform.SetParent(null, false);
            }
        }
    }

    public static class DynastyCentralEconomyBootstrap
    {
        private const string MainSceneName = "Main";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        public static void EnsureForCurrentScene()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void EnsureForScene(Scene scene)
        {
            DynastyEconomyRuntime.EnsureServices();

            if (scene.name != MainSceneName)
                return;

            Canvas canvas = ResolveMainCanvas();
            if (canvas == null)
                return;

            if (UnityEngine.Object.FindAnyObjectByType<DynastyVaultUI>(FindObjectsInactive.Include) == null)
            {
                GameObject vault = new GameObject("DynastyVaultUI", typeof(RectTransform), typeof(DynastyVaultUI));
                vault.transform.SetParent(canvas.transform, false);
            }

            if (UnityEngine.Object.FindAnyObjectByType<DynastyBankUI>(FindObjectsInactive.Include) == null)
            {
                GameObject bank = new GameObject("DynastyBankUI", typeof(RectTransform), typeof(DynastyBankUI));
                bank.transform.SetParent(canvas.transform, false);
            }
        }

        private static Canvas ResolveMainCanvas()
        {
            return CentralPointLayout.ResolveMainCanvas();
        }
    }

    public abstract class DynastyEconomyWindowBase : MonoBehaviour
    {
        protected Canvas rootCanvas;
        protected RectTransform buttonRect;
        protected RectTransform overlayRect;
        protected RectTransform windowRect;
        protected RectTransform contentPanelRect;
        protected Button openButton;
        protected Button closeButton;
        protected TextMeshProUGUI openButtonLabel;
        protected TextMeshProUGUI titleText;
        protected Image profileGoldIcon;
        protected Image profileAmetistIcon;
        protected TextMeshProUGUI profileGoldText;
        protected TextMeshProUGUI profileAmetistText;
        protected TextMeshProUGUI messageText;

        protected abstract string ButtonObjectName { get; }
        protected abstract string OverlayObjectName { get; }
        protected abstract string ButtonText { get; }
        protected abstract string TitleText { get; }
        protected abstract Vector2 ButtonPosition { get; }
        protected abstract Color AccentColor { get; }
        protected virtual MainLobbyLeftMenuSlot? MainMenuSlot => null;

        protected virtual void OnEnable()
        {
            CurrencyService.CurrencyChanged += RefreshValues;
            ProfileService.ProfileChanged += RefreshValues;
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            DynastyEconomyRuntime.EnsureServices();
            EnsureUi();
            RefreshText();
            RefreshValues();
            Layout();
        }

        protected virtual void OnDisable()
        {
            CurrencyService.CurrencyChanged -= RefreshValues;
            ProfileService.ProfileChanged -= RefreshValues;
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
        }

        protected virtual void OnRectTransformDimensionsChange()
        {
            Layout();
        }

        public void ForceMainMenuLayout()
        {
            EnsureUi();
            RefreshText();
            RefreshValues();
            Layout();
        }

        protected virtual void OnLanguageChanged(GameLanguage language)
        {
            RefreshText();
            RefreshValues();
        }

        protected virtual void EnsureUi()
        {
            if (rootCanvas == null)
                rootCanvas = CentralPointLayout.ResolveMainCanvas();

            Transform overlayParent = rootCanvas != null ? rootCanvas.transform : transform;
            Transform buttonParent = CentralPointLayout.ResolveLeftMenuRoot(rootCanvas);
            if (buttonParent == null)
                buttonParent = overlayParent;

            if (buttonRect == null)
            {
                GameObject buttonObject = new GameObject(ButtonObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(buttonParent, false);
                buttonRect = buttonObject.GetComponent<RectTransform>();

                Image image = buttonObject.GetComponent<Image>();
                image.color = AccentColor;
                image.raycastTarget = true;

                openButton = buttonObject.GetComponent<Button>();
                openButton.targetGraphic = image;
                openButton.onClick.AddListener(Open);
                MainLobbyButtonStyle.Apply(openButton);
                openButton.image.preserveAspect = false;

                openButtonLabel = CreateText(buttonObject.transform, "Label", ButtonText, 20f, FontStyles.Bold, Color.white);
                MainLobbyButtonStyle.ApplyButtonLabelLayout(openButtonLabel);
                AttachMainInfoHint();
            }
            else if (buttonParent != null && buttonRect.parent != buttonParent)
            {
                buttonRect.SetParent(buttonParent, false);
                AttachMainInfoHint();
            }

            if (overlayRect == null)
            {
                GameObject overlay = new GameObject(OverlayObjectName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                overlay.transform.SetParent(overlayParent, false);
                overlayRect = overlay.GetComponent<RectTransform>();

                Canvas overlayCanvas = overlay.GetComponent<Canvas>();
                overlayCanvas.overrideSorting = true;
                overlayCanvas.sortingOrder = 30000;

                Image overlayImage = overlay.GetComponent<Image>();
                overlayImage.color = new Color(0f, 0f, 0f, 0.7f);
                overlayImage.raycastTarget = true;

                Button overlayButton = overlay.GetComponent<Button>();
                overlayButton.targetGraphic = overlayImage;
                overlayButton.onClick.AddListener(Close);

                GameObject window = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                window.transform.SetParent(overlay.transform, false);
                windowRect = window.GetComponent<RectTransform>();

                Image windowImage = window.GetComponent<Image>();
                windowImage.color = new Color(0.05f, 0.07f, 0.105f, 0.99f);
                windowImage.raycastTarget = true;
                MainLobbyButtonStyle.ApplyDlsWindow(windowImage);

                Button blocker = window.GetComponent<Button>();
                blocker.targetGraphic = windowImage;
                blocker.onClick.RemoveAllListeners();

                contentPanelRect = CreatePanel(window.transform, "ContentPanel", new Color(0.02f, 0.03f, 0.055f, 0.86f));
                titleText = CreateText(window.transform, "Title", TitleText, 56f, FontStyles.Bold, Color.white);
                profileGoldIcon = CreateIcon(window.transform, "ProfileGoldIcon", MainLobbyButtonStyle.GoldCurrencySprite);
                profileAmetistIcon = CreateIcon(window.transform, "ProfileAmetistIcon", MainLobbyButtonStyle.AmetistCurrencySprite);
                profileGoldText = CreateText(window.transform, "ProfileGold", string.Empty, 36f, FontStyles.Bold, new Color(1f, 0.82f, 0.34f, 1f));
                profileAmetistText = CreateText(window.transform, "ProfileAmetist", string.Empty, 36f, FontStyles.Bold, new Color(0.78f, 0.62f, 1f, 1f));
                messageText = CreateText(window.transform, "Message", string.Empty, 34f, FontStyles.Bold, new Color(1f, 0.56f, 0.45f, 1f));
                closeButton = CreateButton(window.transform, "CloseButton", CloseText(), 34f);
                MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
                closeButton.onClick.AddListener(Close);

                BuildContent(window.transform);
                overlay.SetActive(false);
            }
            else if (overlayParent != null && overlayRect.parent != overlayParent)
            {
                overlayRect.SetParent(overlayParent, false);
            }
        }

        private void AttachMainInfoHint()
        {
            if (openButton == null)
                return;

            string titleKey = null;
            string bodyKey = null;
            switch (ButtonObjectName)
            {
                case "DynastyVaultButton":
                    titleKey = "main.info.vault.title";
                    bodyKey = "main.info.vault.body";
                    break;
                case "DynastyBankButton":
                    titleKey = "main.info.bank.title";
                    bodyKey = "main.info.bank.body";
                    break;
                case "ExchangeMonitorButton":
                    titleKey = "main.info.exchange.title";
                    bodyKey = "main.info.exchange.body";
                    break;
                case "MainWeeklyRewardButton":
                    titleKey = "main.info.rewards.title";
                    bodyKey = "main.info.rewards.body";
                    break;
            }

            if (!string.IsNullOrWhiteSpace(titleKey) && !string.IsNullOrWhiteSpace(bodyKey))
                MainInfoHintTarget.Attach(openButton, titleKey, bodyKey);
        }

        protected abstract void BuildContent(Transform window);
        protected abstract void LayoutContent(float width, float height, float pad);
        protected abstract void RefreshContentText();
        protected abstract void RefreshContentValues();

        protected virtual void Open()
        {
            if (!MainHubStateController.CanOpenMainWindow(GetType().Name))
            {
                Close();
                return;
            }

            SetMessage(string.Empty);
            if (overlayRect != null)
                overlayRect.SetAsLastSibling();
            SetObjectActive(overlayRect != null ? overlayRect.gameObject : null, true);
            MainLobbyUiCoordinator.SetRightStackSuppressed(true);
            RefreshText();
            RefreshValues();
            Layout();
            ShowFirstVisitDialogue();
        }

        private void ShowFirstVisitDialogue()
        {
            string dialogueId;
            string titleKey;
            string bodyKey;
            string whiteLineKey;

            switch (ButtonObjectName)
            {
                case "DynastyVaultButton":
                    dialogueId = "vault";
                    titleKey = "main.info.vault.title";
                    bodyKey = "main.info.vault.body";
                    whiteLineKey = "main.intro.vault.white";
                    break;
                case "DynastyBankButton":
                    dialogueId = "bank";
                    titleKey = "main.info.bank.title";
                    bodyKey = "main.info.bank.body";
                    whiteLineKey = "main.intro.bank.white";
                    break;
                case "ExchangeMonitorButton":
                    dialogueId = "exchange";
                    titleKey = "main.info.exchange.title";
                    bodyKey = "main.info.exchange.body";
                    whiteLineKey = "main.intro.exchange.white";
                    break;
                case "MainWeeklyRewardButton":
                    dialogueId = "rewards";
                    titleKey = "main.info.rewards.title";
                    bodyKey = "main.info.rewards.body";
                    whiteLineKey = "main.intro.rewards.white";
                    break;
                default:
                    return;
            }

            ChatFirstVisitDialogueUI intro = ChatFirstVisitDialogueUI.Ensure(overlayRect);
            if (intro != null)
                intro.TryShowForCurrentProfile(dialogueId, titleKey, bodyKey, whiteLineKey);
        }

        protected virtual void Close()
        {
            SetObjectActive(overlayRect != null ? overlayRect.gameObject : null, false);
            MainLobbyUiCoordinator.SetRightStackSuppressed(false);
            MainHubStateController.NotifyMainWindowClosed();
        }

        protected virtual void RefreshText()
        {
            SetLabel(openButtonLabel, ButtonText);
            SetLabel(titleText, TitleText);
            SetButtonLabel(closeButton, CloseText());
            MainLobbyButtonStyle.ApplyCloseIconButton(closeButton);
            RefreshContentText();
        }

        protected virtual void RefreshValues()
        {
            bool hasProfile = ProfileService.I != null && ProfileService.I.Current != null;
            int profileGold = hasProfile && CurrencyService.I != null ? CurrencyService.I.GetOzAltin() : 0;
            int profileAmetist = hasProfile && CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;
            SetLabel(profileGoldText, ProfileGoldText(profileGold));
            SetLabel(profileAmetistText, ProfileAmetistText(profileAmetist));

            if (openButton != null)
                openButton.interactable = hasProfile;

            RefreshContentValues();
        }

        protected virtual void Layout()
        {
            SetMainMenuButton(buttonRect, ButtonPosition, MainMenuSlot, MainLobbyUiCoordinator.LeftMenuWidth, MainLobbyUiCoordinator.LeftMenuButtonHeight);
            ConfigureMenuButtonLabel(openButtonLabel, 34f, 18f);
            Stretch(overlayRect);

            if (windowRect == null)
                return;

            RectTransform rootRect = overlayRect != null ? overlayRect : transform as RectTransform;
            float rootWidth = rootRect != null ? Mathf.Max(960f, rootRect.rect.width) : 1280f;
            float rootHeight = rootRect != null ? Mathf.Max(540f, rootRect.rect.height) : 720f;
            float width = Mathf.Clamp(rootWidth * 0.96f, 1120f, 1820f);
            float height = Mathf.Clamp(rootHeight * 0.94f, 620f, 980f);
            float pad = Mathf.Clamp(width * 0.09f, 108f, 160f);
            float headerTop = -118f;
            float closeWidth = 76f;
            float statsTop = -206f;
            float statsWidth = width - pad * 2f;
            float statsColumnWidth = (statsWidth - 44f) * 0.5f;

            SetTopLeft(windowRect, (rootWidth - width) * 0.5f, -(rootHeight - height) * 0.5f, width, height);
            SetObjectActive(contentPanelRect != null ? contentPanelRect.gameObject : null, false);
            SetTopLeft(contentPanelRect, pad - 34f, -170f, width - (pad - 34f) * 2f, height - 284f);
            SetTopLeft(titleText != null ? titleText.rectTransform : null, pad, headerTop, width - pad * 2f - closeWidth - 26f, 72f);
            SetTopLeft(closeButton != null ? closeButton.transform as RectTransform : null, width - closeWidth - 24f, -62f, closeWidth, closeWidth);
            SetIconLabelRow(profileGoldIcon, profileGoldText, pad, statsTop, statsColumnWidth, 54f, 44f, 16f);
            SetIconLabelRow(profileAmetistIcon, profileAmetistText, pad + statsColumnWidth + 44f, statsTop, statsColumnWidth, 54f, 44f, 16f);
            LayoutContent(width, height, pad);
            SetTopLeft(messageText != null ? messageText.rectTransform : null, pad, -height + 88f, width - pad * 2f, 52f);
        }

        protected void SetMessage(string value)
        {
            SetLabel(messageText, value);
        }

        protected static int ReadAmount(TMP_InputField input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.text))
                return 0;

            return int.TryParse(input.text.Trim(), out int value) ? Mathf.Max(0, value) : 0;
        }

        protected static TextMeshProUGUI CreateText(Transform parent, string objectName, string value, float fontSize, FontStyles style, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            MainLobbyButtonStyle.ApplyFont(text);
            text.fontSize = fontSize;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.65f);
            text.enableAutoSizing = true;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        protected static Button CreateButton(Transform parent, string objectName, string label, float fontSize)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.14f, 0.2f, 0.3f, 1f);
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            MainLobbyButtonStyle.Apply(button);

            TextMeshProUGUI text = CreateText(go.transform, "Label", label, fontSize, FontStyles.Bold, Color.white);
            MainLobbyButtonStyle.ApplyButtonLabelLayout(text);
            text.margin = new Vector4(20f, 4f, 20f, 6f);
            return button;
        }

        protected static TMP_InputField CreateInput(Transform parent, string objectName, string placeholder)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.035f, 0.05f, 0.08f, 1f);
            image.raycastTarget = true;

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(root.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(30f, 8f);
            textAreaRect.offsetMax = new Vector2(-30f, -8f);

            TextMeshProUGUI placeholderText = CreateText(textArea.transform, "Placeholder", placeholder, 38f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.5f));
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(placeholderText.rectTransform);

            TextMeshProUGUI text = CreateText(textArea.transform, "Text", string.Empty, 40f, FontStyles.Bold, Color.white);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(text.rectTransform);

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.textViewport = textAreaRect;
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.keyboardType = TouchScreenKeyboardType.NumberPad;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 9;
            return input;
        }

        protected static RectTransform CreatePanel(Transform parent, string objectName, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        protected static Image CreateIcon(Transform parent, string objectName, Sprite sprite)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        protected static void SetLabel(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value;
        }

        protected static void SetButtonLabel(Button button, string value)
        {
            TextMeshProUGUI label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label != null)
                label.text = value;
        }

        protected static void SetPlaceholder(TMP_InputField input, string value)
        {
            TextMeshProUGUI label = input != null && input.placeholder != null ? input.placeholder.GetComponent<TextMeshProUGUI>() : null;
            if (label != null)
                label.text = value;
        }

        protected static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        protected static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        protected static void SetIconLabelRow(Image icon, TextMeshProUGUI label, float x, float y, float width, float height, float iconSize, float gap)
        {
            if (icon != null)
                SetTopLeft(icon.rectTransform, x, y - (height - iconSize) * 0.5f, iconSize, iconSize);

            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            SetTopLeft(label.rectTransform, x + iconSize + gap, y, Mathf.Max(0f, width - iconSize - gap), height);
        }

        protected static void SetMainLeftMenuButton(RectTransform rect, Vector2 position, float width, float height)
        {
            if (rect == null)
                return;

            if (!MainLobbyUiCoordinator.IsPortraitLayout(MainLobbyUiCoordinator.ResolveScreenSize()) && position.x < 0f)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = position;
                rect.sizeDelta = new Vector2(width, height);
                return;
            }

            CentralPointLayout.SetTopLeft(rect, position, new Vector2(width, height));
        }

        protected static void SetMainMenuButton(RectTransform rect, Vector2 position, MainLobbyLeftMenuSlot? slot, float width, float height)
        {
            if (rect == null)
                return;

            if (slot.HasValue)
            {
                MainLobbyUiCoordinator.LayoutLeftMenuButton(rect, slot.Value);
                return;
            }

            SetMainLeftMenuButton(rect, position, width, height);
        }

        protected static void ConfigureMenuButtonLabel(TextMeshProUGUI label, float maxSize, float minSize)
        {
            if (label == null)
                return;

            label.fontSize = Mathf.Max(maxSize, 38f);
            label.fontSizeMax = Mathf.Max(maxSize, 38f);
            label.fontSizeMin = Mathf.Max(minSize, 22f);
            label.enableAutoSizing = true;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            MainLobbyButtonStyle.ApplyButtonLabelLayout(label);
        }

        protected static void CenterLabelRect(TextMeshProUGUI label, Vector4 margin)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.Center;
            label.margin = margin;

            RectTransform rect = label.rectTransform;
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.sizeDelta = Vector2.zero;
        }

        protected static void OffsetLabelRect(TextMeshProUGUI label, float offsetX, float offsetY)
        {
            RectTransform rect = label != null ? label.rectTransform : null;
            if (rect == null)
                return;

            rect.offsetMin += new Vector2(offsetX, offsetY);
            rect.offsetMax += new Vector2(offsetX, offsetY);
        }

        protected static void OffsetButtonLabel(Button button, float offsetX, float offsetY)
        {
            TextMeshProUGUI label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            OffsetLabelRect(label, offsetX, offsetY);
        }

        protected static string GoldAmountText() => DynastyEconomyLoc.T("\u0417\u043e\u043b\u043e\u0442\u043e", "Gold", "Altın");
        protected static string AmetistAmountText() => DynastyEconomyLoc.T("\u0410\u043c\u0435\u0442\u0438\u0441\u0442\u044b", "Amethysts", "Ametist");

        protected static void SetObjectActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        protected static string CloseText() => DynastyEconomyLoc.T("\u0417\u0430\u043a\u0440\u044b\u0442\u044c", "Close", "Kapat");
        protected static string AmountText() => DynastyEconomyLoc.T("\u0421\u0443\u043c\u043c\u0430", "Amount", "Miktar");
        protected static string DepositText() => DynastyEconomyLoc.T("\u041f\u043e\u043b\u043e\u0436\u0438\u0442\u044c", "Deposit", "Yatır");
        protected static string WithdrawText() => DynastyEconomyLoc.T("\u0417\u0430\u0431\u0440\u0430\u0442\u044c", "Withdraw", "Çek");
        protected static string DoneText() => DynastyEconomyLoc.T("\u0413\u043e\u0442\u043e\u0432\u043e.", "Done.", "Tamam.");
        protected static string NotEnoughText() => DynastyEconomyLoc.T("\u041d\u0435\u0434\u043e\u0441\u0442\u0430\u0442\u043e\u0447\u043d\u043e \u0432\u0430\u043b\u044e\u0442\u044b.", "Not enough currency.", "Yeterli para yok.");
        protected static string NotEnoughAmetistText() => DynastyEconomyLoc.T("\u041d\u0435\u0434\u043e\u0441\u0442\u0430\u0442\u043e\u0447\u043d\u043e \u0430\u043c\u0435\u0442\u0438\u0441\u0442\u043e\u0432.", "Not enough amethysts.", "Yeterli ametist yok.");
        protected static string ProfileGoldText(int value) => DynastyEconomyLoc.T($"\u041f\u0440\u043e\u0444\u0438\u043b\u044c \u0437\u043e\u043b\u043e\u0442\u043e: {value}", $"Profile gold: {value}", $"Profil altın: {value}");
        protected static string ProfileAmetistText(int value) => DynastyEconomyLoc.T($"\u041f\u0440\u043e\u0444\u0438\u043b\u044c \u0430\u043c\u0435\u0442\u0438\u0441\u0442\u044b: {value}", $"Profile amethysts: {value}", $"Profil ametist: {value}");
    }

    [DisallowMultipleComponent]
    public sealed class DynastyVaultUI : DynastyEconomyWindowBase
    {
        private const string HeaderDividerResourcePath = "Mahjong/Sprites/ShopUI/MainShopHeaderDividerV2_Cropped";
        private static Sprite cachedHeaderDividerSprite;
        private static Sprite cachedRoundedRectSprite;

        private RectTransform headerPanelRect;
        private RectTransform headerAccentRect;
        private RectTransform headerTopRailRect;
        private RectTransform headerBottomRailRect;
        private RectTransform headerTitleRuleRect;
        private RectTransform profileGoldPlateRect;
        private RectTransform profileAmetistPlateRect;
        private RectTransform profileWalletArtRect;
        private RectTransform profileGoldAccentRect;
        private RectTransform profileAmetistAccentRect;
        private RectTransform goldCardRect;
        private RectTransform ametistCardRect;
        private RectTransform goldAccentRect;
        private RectTransform ametistAccentRect;
        private RectTransform goldDividerRect;
        private RectTransform ametistDividerRect;
        private Image goldWatermarkIcon;
        private Image ametistWatermarkIcon;
        private TMP_InputField goldInput;
        private TMP_InputField ametistInput;
        private Button depositGoldButton;
        private Button withdrawGoldButton;
        private Button depositAmetistButton;
        private Button withdrawAmetistButton;
        private Image vaultGoldIcon;
        private Image vaultAmetistIcon;
        private TextMeshProUGUI vaultGoldText;
        private TextMeshProUGUI vaultAmetistText;
        private TextMeshProUGUI eyebrowText;
        private TextMeshProUGUI subtitleText;
        private TextMeshProUGUI profileCaptionText;
        private TextMeshProUGUI goldCardTitleText;
        private TextMeshProUGUI ametistCardTitleText;
        private TextMeshProUGUI goldAmountCaptionText;
        private TextMeshProUGUI ametistAmountCaptionText;

        protected override string ButtonObjectName => "DynastyVaultButton";
        protected override string OverlayObjectName => "DynastyVaultOverlay";
        protected override string ButtonText => DynastyEconomyLoc.T("\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435", "Vault", "Depo");
        protected override string TitleText => DynastyEconomyLoc.T("\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435 \u0434\u0438\u043d\u0430\u0441\u0442\u0438\u0438", "Dynasty Vault", "Hanedan Deposu");
        protected override Vector2 ButtonPosition => new Vector2(
            MainLobbyUiCoordinator.GetLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Vault).x,
            MainLobbyUiCoordinator.GetLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Vault).y);
        protected override Color AccentColor => new Color(0.11f, 0.16f, 0.24f, 0.96f);
        protected override MainLobbyLeftMenuSlot? MainMenuSlot => MainLobbyLeftMenuSlot.Vault;

        protected override void OnEnable()
        {
            DynastyVaultService.VaultChanged += RefreshValues;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            DynastyVaultService.VaultChanged -= RefreshValues;
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            base.OnDisable();
        }

        protected override void Open()
        {
            base.Open();
            if (overlayRect != null && overlayRect.gameObject.activeSelf)
                SettingsMenuUI.SetMainSettingsButtonSuppressed(true);
        }

        protected override void Close()
        {
            base.Close();
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
        }

        protected override void Layout()
        {
            SetMainMenuButton(buttonRect, ButtonPosition, MainMenuSlot, MainLobbyUiCoordinator.LeftMenuWidth, MainLobbyUiCoordinator.LeftMenuButtonHeight);
            ConfigureMenuButtonLabel(openButtonLabel, 34f, 18f);
            Stretch(overlayRect);

            if (windowRect == null)
                return;

            ResolveFullscreenMetrics(out float rootWidth, out float rootHeight, out float safeLeft, out float safeRight, out float safeTop, out float safeBottom);
            float horizontalInset = Mathf.Clamp(rootWidth * 0.045f, 60f, 112f);
            float verticalInset = Mathf.Clamp(rootHeight * 0.035f, 32f, 52f);
            float left = safeLeft + horizontalInset;
            float right = rootWidth - safeRight - horizontalInset;
            float contentWidth = Mathf.Max(1f, right - left);
            float headerTop = safeTop + verticalInset;
            float headerHeight = Mathf.Clamp(rootHeight * 0.19f, 124f, 184f);
            float closeSize = Mathf.Clamp(rootHeight * 0.075f, 54f, 78f);
            float closeEdgeInset = Mathf.Clamp(rootWidth * 0.025f, 40f, 64f);
            float closeX = rootWidth - safeRight - closeSize - closeEdgeInset;
            float headerRight = Mathf.Max(left + 1f, closeX - 18f);
            float headerWidth = Mathf.Max(1f, headerRight - left);
            float walletWidth = Mathf.Clamp(headerWidth * 0.39f, 370f, 600f);
            float walletRight = headerRight - 24f;
            float walletLeft = Mathf.Max(left + headerWidth * 0.48f, walletRight - walletWidth);
            walletWidth = Mathf.Max(250f, walletRight - walletLeft);
            float headerContentLeft = left + 30f;
            float titleWidth = Mathf.Max(220f, walletLeft - headerContentLeft - 34f);

            SetTopLeft(windowRect, 0f, 0f, rootWidth, rootHeight);
            SetObjectActive(contentPanelRect != null ? contentPanelRect.gameObject : null, false);
            SetTopLeft(headerPanelRect, left, -headerTop, headerWidth, headerHeight);
            SetObjectActive(headerTopRailRect != null ? headerTopRailRect.gameObject : null, false);
            float headerDividerHeight = Mathf.Clamp(headerHeight * 0.22f, 28f, 40f);
            SetTopLeft(
                headerBottomRailRect,
                headerContentLeft + 20f,
                -headerTop - headerHeight + headerDividerHeight * 0.72f,
                Mathf.Max(180f, titleWidth - 20f),
                headerDividerHeight);

            SetObjectActive(eyebrowText != null ? eyebrowText.gameObject : null, false);
            SetObjectActive(subtitleText != null ? subtitleText.gameObject : null, false);
            float headerTitleHeight = Mathf.Clamp(headerHeight * 0.4f, 50f, 66f);
            float headerTitleY = headerTop + (headerHeight - headerTitleHeight) * 0.5f - 4f;
            float titleAccentHeight = Mathf.Clamp(headerTitleHeight * 0.78f, 40f, 54f);
            SetObjectActive(headerAccentRect != null ? headerAccentRect.gameObject : null, true);
            SetTopLeft(headerAccentRect, headerContentLeft, -headerTitleY - (headerTitleHeight - titleAccentHeight) * 0.5f, 4f, titleAccentHeight);
            SetTopLeft(titleText != null ? titleText.rectTransform : null, headerContentLeft + 20f, -headerTitleY, titleWidth - 20f, headerTitleHeight);
            SetObjectActive(headerTitleRuleRect != null ? headerTitleRuleRect.gameObject : null, false);
            SetTopLeft(closeButton != null ? closeButton.transform as RectTransform : null, closeX, -safeTop - closeEdgeInset, closeSize, closeSize);

            float walletModuleTop = headerTop + 5f;
            float walletModuleHeight = headerHeight - 10f;
            SetObjectActive(profileGoldPlateRect != null ? profileGoldPlateRect.gameObject : null, true);
            SetObjectActive(profileAmetistPlateRect != null ? profileAmetistPlateRect.gameObject : null, false);
            SetTopLeft(profileGoldPlateRect, walletLeft - 12f, -walletModuleTop, walletWidth + 24f, walletModuleHeight);

            float walletRowHeight = Mathf.Clamp(headerHeight * 0.24f, 32f, 42f);
            float walletIconSize = Mathf.Clamp(walletRowHeight * 0.76f, 24f, 34f);
            SetObjectActive(profileCaptionText != null ? profileCaptionText.gameObject : null, false);
            SetObjectActive(profileGoldAccentRect != null ? profileGoldAccentRect.gameObject : null, false);
            SetObjectActive(profileAmetistAccentRect != null ? profileAmetistAccentRect.gameObject : null, false);
            float walletGap = 5f;
            float walletRowsHeight = walletRowHeight * 2f + walletGap;
            float goldWalletY = walletModuleTop + (walletModuleHeight - walletRowsHeight) * 0.5f;
            float ametistWalletY = goldWalletY + walletRowHeight + walletGap;
            SetIconLabelRow(profileGoldIcon, profileGoldText, walletLeft + 44f, -goldWalletY, walletWidth - 88f, walletRowHeight, walletIconSize, 12f);
            SetIconLabelRow(profileAmetistIcon, profileAmetistText, walletLeft + 44f, -ametistWalletY, walletWidth - 88f, walletRowHeight, walletIconSize, 12f);

            if (titleText != null)
            {
                titleText.fontSize = Mathf.Clamp(rootHeight * 0.055f, 38f, 60f);
                titleText.fontSizeMax = titleText.fontSize;
                titleText.fontSizeMin = 28f;
                titleText.alignment = TextAlignmentOptions.MidlineLeft;
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                MainLobbyButtonStyle.ApplySilverTextEffect(titleText);
            }

            ConfigureEyebrow(eyebrowText);
            ConfigureSubtitle(subtitleText);
            ConfigureProfileCaption(profileCaptionText);

            ConfigureHeaderLabel(profileGoldText);
            ConfigureHeaderLabel(profileAmetistText);
            LayoutContent(rootWidth, rootHeight, left);

            float messageHeight = Mathf.Clamp(rootHeight * 0.05f, 34f, 52f);
            float messageY = rootHeight - safeBottom - Mathf.Clamp(rootHeight * 0.018f, 10f, 22f) - messageHeight;
            SetTopLeft(messageText != null ? messageText.rectTransform : null, left, -messageY, contentWidth, messageHeight);
            if (messageText != null)
            {
                messageText.alignment = TextAlignmentOptions.Center;
                messageText.fontSize = Mathf.Clamp(rootHeight * 0.032f, 22f, 32f);
                messageText.fontSizeMax = messageText.fontSize;
            }

            if (closeButton != null)
                closeButton.transform.SetAsLastSibling();
        }

        protected override void BuildContent(Transform window)
        {
            Button fullscreenBlocker = window != null ? window.GetComponent<Button>() : null;
            if (fullscreenBlocker != null)
                fullscreenBlocker.transition = Selectable.Transition.None;

            headerPanelRect = CreatePanel(window, "VaultHeaderPanel", new Color(0.025f, 0.075f, 0.125f, 0.82f));
            headerAccentRect = CreatePanel(window, "VaultHeaderAccent", new Color(0.32f, 0.72f, 1f, 0.46f));
            headerTopRailRect = CreatePanel(window, "VaultHeaderTopRail", new Color(0.38f, 0.76f, 1f, 0.34f));
            headerBottomRailRect = CreatePanel(window, "VaultHeaderBottomRail", new Color(0.28f, 0.68f, 1f, 0.5f));
            headerTitleRuleRect = CreatePanel(window, "VaultHeaderTitleRule", new Color(0.35f, 0.74f, 1f, 0.34f));
            profileGoldPlateRect = CreatePanel(window, "ProfileGoldPlate", Color.white);
            profileAmetistPlateRect = CreatePanel(window, "ProfileAmetistPlate", Color.white);
            profileWalletArtRect = CreatePanel(profileGoldPlateRect, "ProfileWalletArt", Color.white);
            profileGoldAccentRect = CreatePanel(window, "ProfileGoldAccent", new Color(1f, 0.7f, 0.2f, 0.92f));
            profileAmetistAccentRect = CreatePanel(window, "ProfileAmetistAccent", new Color(0.72f, 0.42f, 1f, 0.92f));
            goldCardRect = CreatePanel(window, "GoldTransferCard", Color.white);
            ametistCardRect = CreatePanel(window, "AmetistTransferCard", Color.white);
            goldAccentRect = CreatePanel(window, "GoldCardAccent", new Color(1f, 0.76f, 0.3f, 0.9f));
            ametistAccentRect = CreatePanel(window, "AmetistCardAccent", new Color(0.82f, 0.58f, 1f, 0.9f));
            goldDividerRect = CreatePanel(window, "GoldCardDivider", new Color(1f, 0.76f, 0.32f, 0.24f));
            ametistDividerRect = CreatePanel(window, "AmetistCardDivider", new Color(0.82f, 0.6f, 1f, 0.24f));
            goldWatermarkIcon = CreateIcon(window, "GoldWatermark", MainLobbyButtonStyle.GoldCurrencySprite);
            ametistWatermarkIcon = CreateIcon(window, "AmetistWatermark", MainLobbyButtonStyle.AmetistCurrencySprite);

            goldWatermarkIcon.color = new Color(1f, 0.72f, 0.2f, 0.035f);
            ametistWatermarkIcon.color = new Color(0.72f, 0.38f, 1f, 0.04f);

            StyleHeaderPanel(headerPanelRect);
            StyleHeaderDivider(headerBottomRailRect);
            StyleWalletModule(profileGoldPlateRect, profileWalletArtRect);
            StyleCard(goldCardRect, new Color(1f, 0.82f, 0.48f, 0.96f), new Color(0.18f, 0.08f, 0.01f, 0.68f));
            StyleCard(ametistCardRect, new Color(0.82f, 0.56f, 1f, 0.96f), new Color(0.09f, 0.02f, 0.16f, 0.7f));
            headerPanelRect.SetSiblingIndex(0);
            headerAccentRect.SetSiblingIndex(1);
            headerTopRailRect.SetSiblingIndex(2);
            headerBottomRailRect.SetSiblingIndex(3);
            headerTitleRuleRect.SetSiblingIndex(4);
            profileGoldPlateRect.SetSiblingIndex(5);
            profileAmetistPlateRect.SetSiblingIndex(6);
            profileGoldAccentRect.SetSiblingIndex(7);
            profileAmetistAccentRect.SetSiblingIndex(8);
            goldCardRect.SetSiblingIndex(9);
            ametistCardRect.SetSiblingIndex(10);
            goldWatermarkIcon.transform.SetSiblingIndex(11);
            ametistWatermarkIcon.transform.SetSiblingIndex(12);
            goldAccentRect.SetSiblingIndex(13);
            ametistAccentRect.SetSiblingIndex(14);
            goldDividerRect.SetSiblingIndex(15);
            ametistDividerRect.SetSiblingIndex(16);

            eyebrowText = CreateText(window, "Eyebrow", EyebrowLabel(), 26f, FontStyles.Bold, new Color(0.34f, 0.72f, 1f, 1f));
            subtitleText = CreateText(window, "Subtitle", SubtitleLabel(), 24f, FontStyles.Normal, new Color(0.72f, 0.82f, 0.92f, 1f));
            profileCaptionText = CreateText(window, "ProfileCaption", ProfileCaptionLabel(), 24f, FontStyles.Bold, new Color(0.42f, 0.7f, 0.94f, 1f));
            goldCardTitleText = CreateText(window, "GoldCardTitle", GoldCardTitleLabel(), 30f, FontStyles.Bold, new Color(1f, 0.79f, 0.3f, 1f));
            ametistCardTitleText = CreateText(window, "AmetistCardTitle", AmetistCardTitleLabel(), 30f, FontStyles.Bold, new Color(0.83f, 0.62f, 1f, 1f));
            goldAmountCaptionText = CreateText(window, "GoldAmountCaption", AmountCaptionLabel(), 24f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.58f, 1f));
            ametistAmountCaptionText = CreateText(window, "AmetistAmountCaption", AmountCaptionLabel(), 24f, FontStyles.Bold, new Color(0.8f, 0.7f, 0.92f, 1f));

            vaultGoldIcon = CreateIcon(window, "VaultGoldIcon", MainLobbyButtonStyle.GoldCurrencySprite);
            vaultAmetistIcon = CreateIcon(window, "VaultAmetistIcon", MainLobbyButtonStyle.AmetistCurrencySprite);
            vaultGoldText = CreateText(window, "VaultGold", string.Empty, 36f, FontStyles.Bold, new Color(1f, 0.82f, 0.34f, 1f));
            vaultAmetistText = CreateText(window, "VaultAmetist", string.Empty, 36f, FontStyles.Bold, new Color(0.78f, 0.62f, 1f, 1f));
            goldInput = CreateInput(window, "GoldInput", GoldAmountText());
            ametistInput = CreateInput(window, "AmetistInput", AmetistAmountText());
            depositGoldButton = CreateButton(window, "DepositGoldButton", DepositText(), 36f);
            withdrawGoldButton = CreateButton(window, "WithdrawGoldButton", WithdrawText(), 36f);
            depositAmetistButton = CreateButton(window, "DepositAmetistButton", DepositText(), 36f);
            withdrawAmetistButton = CreateButton(window, "WithdrawAmetistButton", WithdrawText(), 36f);

            depositGoldButton.onClick.AddListener(() => TransferGold(true));
            withdrawGoldButton.onClick.AddListener(() => TransferGold(false));
            depositAmetistButton.onClick.AddListener(() => TransferAmetist(true));
            withdrawAmetistButton.onClick.AddListener(() => TransferAmetist(false));

            StyleInput(goldInput, new Color(0.075f, 0.06f, 0.025f, 1f), new Color(1f, 0.66f, 0.14f, 0.55f));
            StyleInput(ametistInput, new Color(0.055f, 0.03f, 0.085f, 1f), new Color(0.69f, 0.35f, 1f, 0.58f));
            StyleTransferButton(depositGoldButton, new Color(0.76f, 0.46f, 0.08f, 1f));
            StyleTransferButton(withdrawGoldButton, new Color(0.2f, 0.25f, 0.32f, 1f));
            StyleTransferButton(depositAmetistButton, new Color(0.48f, 0.22f, 0.72f, 1f));
            StyleTransferButton(withdrawAmetistButton, new Color(0.2f, 0.25f, 0.32f, 1f));
        }

        protected override void LayoutContent(float width, float height, float pad)
        {
            ResolveFullscreenMetrics(out _, out _, out float safeLeft, out float safeRight, out float safeTop, out float safeBottom);
            float horizontalInset = Mathf.Clamp(width * 0.04f, 48f, 96f);
            float verticalInset = Mathf.Clamp(height * 0.035f, 32f, 52f);
            float left = safeLeft + horizontalInset;
            float right = width - safeRight - horizontalInset;
            float contentWidth = Mathf.Max(1f, right - left);
            float headerTop = safeTop + verticalInset;
            float headerHeight = Mathf.Clamp(height * 0.19f, 124f, 184f);
            float bodyTop = headerTop + headerHeight + Mathf.Clamp(height * 0.04f, 22f, 38f);
            float messageReserve = Mathf.Clamp(height * 0.065f, 42f, 64f) + safeBottom + 10f;
            float bodyHeight = Mathf.Max(1f, height - bodyTop - messageReserve);
            float cardGap = Mathf.Clamp(contentWidth * 0.02f, 24f, 40f);
            float cardWidth = Mathf.Max(1f, (contentWidth - cardGap) * 0.5f);
            float cardPad = Mathf.Clamp(cardWidth * 0.07f, 26f, 58f);

            LayoutCurrencyCard(
                left, bodyTop, cardWidth, bodyHeight, cardPad,
                goldCardRect, goldAccentRect, goldDividerRect,
                goldWatermarkIcon,
                goldCardTitleText, vaultGoldIcon, vaultGoldText, goldAmountCaptionText,
                goldInput, depositGoldButton, withdrawGoldButton);

            LayoutCurrencyCard(
                left + cardWidth + cardGap, bodyTop, cardWidth, bodyHeight, cardPad,
                ametistCardRect, ametistAccentRect, ametistDividerRect,
                ametistWatermarkIcon,
                ametistCardTitleText, vaultAmetistIcon, vaultAmetistText, ametistAmountCaptionText,
                ametistInput, depositAmetistButton, withdrawAmetistButton);
        }

        protected override void RefreshContentText()
        {
            SetLabel(eyebrowText, EyebrowLabel());
            SetLabel(subtitleText, SubtitleLabel());
            SetLabel(profileCaptionText, ProfileCaptionLabel());
            SetLabel(goldCardTitleText, GoldCardTitleLabel());
            SetLabel(ametistCardTitleText, AmetistCardTitleLabel());
            SetLabel(goldAmountCaptionText, AmountCaptionLabel());
            SetLabel(ametistAmountCaptionText, AmountCaptionLabel());
            SetPlaceholder(goldInput, GoldAmountText());
            SetPlaceholder(ametistInput, AmetistAmountText());
            SetButtonLabel(depositGoldButton, DepositText());
            SetButtonLabel(withdrawGoldButton, WithdrawText());
            SetButtonLabel(depositAmetistButton, DepositText());
            SetButtonLabel(withdrawAmetistButton, WithdrawText());
        }

        protected override void RefreshContentValues()
        {
            int vaultGold = DynastyVaultService.I != null ? DynastyVaultService.I.GetOzAltin() : 0;
            int vaultAmetist = DynastyVaultService.I != null ? DynastyVaultService.I.GetOzAmetist() : 0;
            SetLabel(vaultGoldText, DynastyEconomyLoc.T($"\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435 \u0437\u043e\u043b\u043e\u0442\u043e: {vaultGold}", $"Vault gold: {vaultGold}", $"Depo altın: {vaultGold}"));
            SetLabel(vaultAmetistText, DynastyEconomyLoc.T($"\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435 \u0430\u043c\u0435\u0442\u0438\u0441\u0442\u044b: {vaultAmetist}", $"Vault amethysts: {vaultAmetist}", $"Depo ametist: {vaultAmetist}"));
        }

        private void TransferGold(bool deposit)
        {
            int amount = ReadAmount(goldInput);
            bool ok = amount > 0 && DynastyVaultService.I != null &&
                      (deposit ? DynastyVaultService.I.DepositOzAltin(amount) : DynastyVaultService.I.WithdrawOzAltin(amount));
            SetMessage(ok ? DoneText() : NotEnoughText());
            RefreshValues();
        }

        private void TransferAmetist(bool deposit)
        {
            int amount = ReadAmount(ametistInput);
            bool ok = amount > 0 && DynastyVaultService.I != null &&
                      (deposit ? DynastyVaultService.I.DepositOzAmetist(amount) : DynastyVaultService.I.WithdrawOzAmetist(amount));
            SetMessage(ok ? DoneText() : NotEnoughText());
            RefreshValues();
        }

        private static void LayoutCurrencyCard(
            float x,
            float y,
            float width,
            float height,
            float pad,
            RectTransform card,
            RectTransform accent,
            RectTransform divider,
            Image watermark,
            TextMeshProUGUI cardTitle,
            Image icon,
            TextMeshProUGUI balance,
            TextMeshProUGUI amountCaption,
            TMP_InputField input,
            Button depositButton,
            Button withdrawButton)
        {
            float frameBleedX = width * 0.065f;
            float frameBleedY = height * 0.12f;
            SetTopLeft(card, x - frameBleedX, -(y - frameBleedY), width + frameBleedX * 2f, height + frameBleedY * 2f);
            SetObjectActive(accent != null ? accent.gameObject : null, false);
            SetObjectActive(divider != null ? divider.gameObject : null, false);

            float watermarkSize = Mathf.Clamp(height * 0.32f, 110f, 230f);
            SetTopLeft(
                watermark != null ? watermark.rectTransform : null,
                x + width - watermarkSize - pad * 0.55f,
                -y - Mathf.Clamp(height * 0.15f, 44f, 106f),
                watermarkSize,
                watermarkSize);

            bool compact = height < 400f;
            float titleY = y + (compact ? 42f : Mathf.Clamp(height * 0.15f, 52f, 108f));
            float titleHeight = compact ? 34f : Mathf.Clamp(height * 0.075f, 36f, 54f);
            SetTopLeft(cardTitle != null ? cardTitle.rectTransform : null, x + pad, -titleY, width - pad * 2f, titleHeight);

            float balanceY = y + (compact ? 82f : Mathf.Clamp(height * 0.28f, 100f, 190f));
            float balanceHeight = compact ? 38f : Mathf.Clamp(height * 0.09f, 44f, 66f);
            float iconSize = compact ? 34f : Mathf.Clamp(balanceHeight * 0.86f, 40f, 58f);
            SetIconLabelRow(icon, balance, x + pad, -balanceY, width - pad * 2f, balanceHeight, iconSize, 16f);

            float inputHeight = Mathf.Clamp(height * 0.13f, 58f, 96f);
            float inputOffset = compact
                ? Mathf.Min(Mathf.Max(height * 0.49f, 165f), Mathf.Max(165f, height - 150f))
                : Mathf.Min(Mathf.Max(height * 0.49f, 150f), Mathf.Max(150f, height - 150f));
            float inputY = y + inputOffset;
            float captionHeight = Mathf.Clamp(height * 0.055f, 24f, 36f);
            float captionY = inputY - captionHeight - 10f;
            SetTopLeft(amountCaption != null ? amountCaption.rectTransform : null, x + pad, -captionY, width - pad * 2f, captionHeight);
            SetTopLeft(input != null ? input.transform as RectTransform : null, x + pad, -inputY, width - pad * 2f, inputHeight);

            float actionGap = Mathf.Clamp(width * 0.035f, 14f, 28f);
            float actionY = inputY + inputHeight + Mathf.Clamp(height * 0.045f, 14f, 34f);
            float actionHeight = Mathf.Clamp(height * 0.13f, 60f, 94f);
            float actionWidth = Mathf.Max(100f, (width - pad * 2f - actionGap) * 0.5f);
            SetTopLeft(depositButton != null ? depositButton.transform as RectTransform : null, x + pad, -actionY, actionWidth, actionHeight);
            SetTopLeft(withdrawButton != null ? withdrawButton.transform as RectTransform : null, x + pad + actionWidth + actionGap, -actionY, actionWidth, actionHeight);

            ConfigureCardTitle(cardTitle);
            ConfigureVaultBalance(balance);
            ConfigureAmountCaption(amountCaption);
            ConfigureActionLabel(depositButton, actionHeight);
            ConfigureActionLabel(withdrawButton, actionHeight);
        }

        private static void ConfigureHeaderLabel(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 24f;
            label.fontSizeMax = 24f;
            label.fontSizeMin = 18f;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureEyebrow(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 19f;
            label.fontSizeMax = 19f;
            label.fontSizeMin = 14f;
            label.characterSpacing = 2.5f;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureSubtitle(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 19f;
            label.fontSizeMax = 19f;
            label.fontSizeMin = 14f;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureProfileCaption(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 18f;
            label.fontSizeMax = 18f;
            label.fontSizeMin = 14f;
            label.characterSpacing = 2f;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureCardTitle(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 30f;
            label.fontSizeMax = 30f;
            label.fontSizeMin = 20f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureVaultBalance(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.fontSize = 36f;
            label.fontSizeMax = 36f;
            label.fontSizeMin = 20f;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureAmountCaption(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 24f;
            label.fontSizeMax = 24f;
            label.fontSizeMin = 18f;
            label.characterSpacing = 2f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureActionLabel(Button button, float buttonHeight)
        {
            TextMeshProUGUI label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label == null)
                return;

            float fontSize = Mathf.Clamp(buttonHeight * 0.4f, 24f, 34f);
            label.fontSize = fontSize;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = 18f;
            label.enableAutoSizing = true;
            MainLobbyButtonStyle.ApplyButtonLabelLayout(label);
        }

        private static void StyleHeaderPanel(RectTransform panel)
        {
            if (panel == null)
                return;

            Image image = panel.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = GetRoundedRectSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = new Color(0.012f, 0.045f, 0.078f, 0.46f);

            image.raycastTarget = false;
        }

        private static void StyleHeaderDivider(RectTransform divider)
        {
            if (divider == null)
                return;

            Image image = divider.GetComponent<Image>();
            if (image == null)
                return;

            if (cachedHeaderDividerSprite == null)
                cachedHeaderDividerSprite = Resources.Load<Sprite>(HeaderDividerResourcePath);

            image.sprite = cachedHeaderDividerSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = new Color(0.82f, 0.92f, 1f, 0.86f);
            image.raycastTarget = false;
        }

        private static void StyleWalletModule(RectTransform plate, RectTransform art)
        {
            if (plate == null)
                return;

            Image image = plate.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = GetRoundedRectSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
            image.raycastTarget = false;

            Mask mask = plate.GetComponent<Mask>();
            if (mask == null)
                mask = plate.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            if (art == null)
                return;

            Stretch(art);
            Image artImage = art.GetComponent<Image>();
            if (artImage == null)
                return;

            Sprite moduleSprite = MainLobbyButtonStyle.BankModuleSprite;
            artImage.sprite = moduleSprite;
            artImage.type = moduleSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            artImage.preserveAspect = false;
            artImage.pixelsPerUnitMultiplier = 1f;
            artImage.color = moduleSprite != null
                ? new Color(0.78f, 0.9f, 1f, 0.72f)
                : new Color(0.018f, 0.05f, 0.082f, 0.82f);
            artImage.raycastTarget = false;
        }

        private static void StyleCard(RectTransform card, Color tint, Color shadowColor)
        {
            if (card == null)
                return;

            Image image = card.GetComponent<Image>();
            if (image != null)
            {
                MainLobbyButtonStyle.ApplyStoreBankWindow(image);
                image.color = tint;
                image.raycastTarget = false;
            }

            Shadow shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = new Vector2(0f, -7f);
            shadow.useGraphicAlpha = true;
        }

        private static void StyleInput(TMP_InputField input, Color background, Color outlineColor)
        {
            if (input == null)
                return;

            Image image = input.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetRoundedRectSprite();
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = background;
            }

            Outline outline = input.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
            input.selectionColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.42f);
        }

        private static Sprite GetRoundedRectSprite()
        {
            if (cachedRoundedRectSprite != null)
                return cachedRoundedRectSprite;

            const int size = 64;
            const float radius = 26f;
            const float half = size * 0.5f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DynastyVaultRoundedRect",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float localX = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                    float localY = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                    float distance = Mathf.Sqrt(localX * localX + localY * localY) - radius;
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            cachedRoundedRectSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            cachedRoundedRectSprite.name = "DynastyVaultRoundedRectSprite";
            cachedRoundedRectSprite.hideFlags = HideFlags.HideAndDontSave;
            return cachedRoundedRectSprite;
        }

        private static void StyleTransferButton(Button button, Color normalColor)
        {
            if (button == null)
                return;

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.22f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.3f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.35f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private void ResolveFullscreenMetrics(
            out float rootWidth,
            out float rootHeight,
            out float safeLeft,
            out float safeRight,
            out float safeTop,
            out float safeBottom)
        {
            RectTransform measuredRect = overlayRect != null ? overlayRect : transform as RectTransform;
            float measuredWidth = measuredRect != null ? measuredRect.rect.width : 0f;
            float measuredHeight = measuredRect != null ? measuredRect.rect.height : 0f;
            Canvas canvas = rootCanvas != null ? rootCanvas.rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            float canvasWidth = canvasRect != null ? canvasRect.rect.width : 0f;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 0f;
            float scaleFactor = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            float screenWidth = Screen.width > 0 ? Screen.width / scaleFactor : 0f;
            float screenHeight = Screen.height > 0 ? Screen.height / scaleFactor : 0f;
            rootWidth = Mathf.Max(measuredWidth, Mathf.Max(canvasWidth, screenWidth));
            rootHeight = Mathf.Max(measuredHeight, Mathf.Max(canvasHeight, screenHeight));
            if (rootWidth <= 8f)
                rootWidth = 1920f;
            if (rootHeight <= 8f)
                rootHeight = 1080f;

            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
            safeLeft = safeArea.xMin / scaleFactor;
            safeRight = Mathf.Max(0f, Screen.width - safeArea.xMax) / scaleFactor;
            safeTop = Mathf.Max(0f, Screen.height - safeArea.yMax) / scaleFactor;
            safeBottom = safeArea.yMin / scaleFactor;
        }

        private static string EyebrowLabel() => DynastyEconomyLoc.T("ЭКОНОМИКА ДИНАСТИИ", "DYNASTY ECONOMY", "HANEDAN EKONOMİSİ");
        private static string SubtitleLabel() => DynastyEconomyLoc.T("Управляйте общими запасами безопасно и быстро", "Manage shared reserves safely and quickly", "Ortak rezervleri güvenle ve hızlıca yönetin");
        private static string ProfileCaptionLabel() => DynastyEconomyLoc.T("БАЛАНС ПРОФИЛЯ", "PROFILE BALANCE", "PROFİL BAKİYESİ");
        private static string GoldCardTitleLabel() => DynastyEconomyLoc.T("ЗОЛОТО ДИНАСТИИ", "DYNASTY GOLD", "HANEDAN ALTINI");
        private static string AmetistCardTitleLabel() => DynastyEconomyLoc.T("АМЕТИСТЫ ДИНАСТИИ", "DYNASTY AMETHYSTS", "HANEDAN AMETİSTİ");
        private static string AmountCaptionLabel() => DynastyEconomyLoc.T("СУММА ПЕРЕВОДА", "TRANSFER AMOUNT", "TRANSFER MİKTARI");
    }

    [DisallowMultipleComponent]
    public sealed class DynastyBankUI : DynastyEconomyWindowBase
    {
        [SerializeField, Min(1)] private int goldPerAmetist = DynastyBankService.DefaultGoldPerAmetist;

        private RectTransform headerPanelRect;
        private RectTransform headerAccentRect;
        private RectTransform profileGoldPlateRect;
        private RectTransform profileAmetistPlateRect;
        private RectTransform exchangeCardRect;
        private RectTransform sourcePanelRect;
        private RectTransform resultPanelRect;
        private RectTransform ratePillRect;
        private RectTransform connectorLineRect;
        private TMP_InputField exchangeInput;
        private Button exchangeButton;
        private Image exchangeAmetistIcon;
        private Image previewGoldIcon;
        private Image sourceWatermarkIcon;
        private Image resultWatermarkIcon;
        private TextMeshProUGUI rateText;
        private TextMeshProUGUI previewText;
        private TextMeshProUGUI eyebrowText;
        private TextMeshProUGUI subtitleText;
        private TextMeshProUGUI profileCaptionText;
        private TextMeshProUGUI exchangeCaptionText;
        private TextMeshProUGUI sourceCaptionText;
        private TextMeshProUGUI resultCaptionText;
        private TextMeshProUGUI connectorText;

        protected override string ButtonObjectName => "DynastyBankButton";
        protected override string OverlayObjectName => "DynastyBankOverlay";
        protected override string ButtonText => DynastyEconomyLoc.T("\u0411\u0430\u043d\u043a", "Bank", "Banka");
        protected override string TitleText => DynastyEconomyLoc.T("\u0411\u0430\u043d\u043a \u0434\u0438\u043d\u0430\u0441\u0442\u0438\u0438", "Dynasty Bank", "Hanedan Bankas\u0131");
        protected override Vector2 ButtonPosition => new Vector2(
            MainLobbyUiCoordinator.GetLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Bank).x,
            MainLobbyUiCoordinator.GetLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Bank).y);
        protected override Color AccentColor => new Color(0.18f, 0.16f, 0.08f, 0.96f);
        protected override MainLobbyLeftMenuSlot? MainMenuSlot => MainLobbyLeftMenuSlot.Bank;

        protected override void OnEnable()
        {
            DynastyBankService.BankChanged += RefreshValues;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            DynastyBankService.BankChanged -= RefreshValues;
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            base.OnDisable();
        }

        protected override void Open()
        {
            base.Open();
            if (overlayRect != null && overlayRect.gameObject.activeSelf)
                SettingsMenuUI.SetMainSettingsButtonSuppressed(true);
        }

        protected override void Close()
        {
            base.Close();
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
        }

        protected override void Layout()
        {
            SetMainMenuButton(buttonRect, ButtonPosition, MainMenuSlot, MainLobbyUiCoordinator.LeftMenuWidth, MainLobbyUiCoordinator.LeftMenuButtonHeight);
            ConfigureMenuButtonLabel(openButtonLabel, 34f, 18f);
            Stretch(overlayRect);

            if (windowRect == null)
                return;

            ApplyFullscreenSurface();
            ResolveFullscreenMetrics(out float rootWidth, out float rootHeight, out float safeLeft, out float safeRight, out float safeTop, out float safeBottom);

            float horizontalInset = Mathf.Clamp(rootWidth * 0.045f, 48f, 112f);
            float verticalInset = Mathf.Clamp(rootHeight * 0.035f, 24f, 52f);
            float left = safeLeft + horizontalInset;
            float right = rootWidth - safeRight - horizontalInset;
            float contentWidth = Mathf.Max(1f, right - left);
            float headerTop = safeTop + verticalInset;
            float headerHeight = Mathf.Clamp(rootHeight * 0.18f, 116f, 184f);
            float closeSize = Mathf.Clamp(rootHeight * 0.075f, 54f, 78f);
            float closeMargin = Mathf.Clamp(rootHeight * 0.018f, 10f, 20f);
            float closeX = rootWidth - safeRight - closeMargin - closeSize;
            float walletWidth = Mathf.Clamp(contentWidth * 0.36f, 330f, 660f);
            float walletRight = closeX - 24f;
            float walletLeft = Mathf.Max(left + contentWidth * 0.46f, walletRight - walletWidth);
            walletWidth = Mathf.Max(250f, walletRight - walletLeft);
            float headerContentLeft = left + 30f;
            float titleWidth = Mathf.Max(220f, walletLeft - headerContentLeft - 34f);
            float titleHeight = Mathf.Clamp(headerHeight * 0.42f, 48f, 72f);
            float titleY = headerTop + (headerHeight - titleHeight) * 0.5f;

            SetTopLeft(windowRect, 0f, 0f, rootWidth, rootHeight);
            SetObjectActive(contentPanelRect != null ? contentPanelRect.gameObject : null, false);
            SetTopLeft(headerPanelRect, left - 18f, -headerTop, contentWidth + 36f, headerHeight);
            SetTopLeft(headerAccentRect, headerContentLeft, -headerTop - headerHeight + 12f, Mathf.Clamp(titleWidth * 0.68f, 180f, 520f), 2f);
            SetTopLeft(titleText != null ? titleText.rectTransform : null, headerContentLeft, -titleY, titleWidth, titleHeight);
            SetObjectActive(eyebrowText != null ? eyebrowText.gameObject : null, false);
            SetObjectActive(subtitleText != null ? subtitleText.gameObject : null, false);
            SetObjectActive(profileCaptionText != null ? profileCaptionText.gameObject : null, false);
            SetObjectActive(profileGoldPlateRect != null ? profileGoldPlateRect.gameObject : null, false);
            SetObjectActive(profileAmetistPlateRect != null ? profileAmetistPlateRect.gameObject : null, false);
            SetTopLeft(closeButton != null ? closeButton.transform as RectTransform : null, closeX, -safeTop - closeMargin, closeSize, closeSize);

            float walletRowHeight = Mathf.Clamp(headerHeight * 0.25f, 30f, 44f);
            float walletIconSize = Mathf.Clamp(walletRowHeight * 0.76f, 24f, 34f);
            float walletGap = Mathf.Clamp(walletRowHeight * 0.16f, 5f, 8f);
            float walletStackHeight = walletRowHeight * 2f + walletGap;
            float goldWalletY = headerTop + (headerHeight - walletStackHeight) * 0.5f;
            float ametistWalletY = goldWalletY + walletRowHeight + walletGap;
            SetIconLabelRow(profileGoldIcon, profileGoldText, walletLeft, -goldWalletY, walletWidth, walletRowHeight, walletIconSize, 12f);
            SetIconLabelRow(profileAmetistIcon, profileAmetistText, walletLeft, -ametistWalletY, walletWidth, walletRowHeight, walletIconSize, 12f);

            if (titleText != null)
            {
                titleText.fontSize = Mathf.Clamp(rootHeight * 0.055f, 38f, 60f);
                titleText.fontSizeMax = titleText.fontSize;
                titleText.fontSizeMin = 28f;
                titleText.alignment = TextAlignmentOptions.MidlineLeft;
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                MainLobbyButtonStyle.ApplySilverTextEffect(titleText);
            }

            ConfigureHeaderLabel(profileGoldText);
            ConfigureHeaderLabel(profileAmetistText);
            LayoutContent(rootWidth, rootHeight, left);

            float messageHeight = Mathf.Clamp(rootHeight * 0.05f, 34f, 52f);
            float messageY = rootHeight - safeBottom - Mathf.Clamp(rootHeight * 0.018f, 10f, 22f) - messageHeight;
            SetTopLeft(messageText != null ? messageText.rectTransform : null, left, -messageY, contentWidth, messageHeight);
            if (messageText != null)
            {
                messageText.alignment = TextAlignmentOptions.Center;
                messageText.fontSize = Mathf.Clamp(rootHeight * 0.032f, 22f, 32f);
                messageText.fontSizeMax = messageText.fontSize;
            }

            if (closeButton != null)
                closeButton.transform.SetAsLastSibling();
        }

        protected override void BuildContent(Transform window)
        {
            Button fullscreenBlocker = window != null ? window.GetComponent<Button>() : null;
            if (fullscreenBlocker != null)
                fullscreenBlocker.transition = Selectable.Transition.None;

            ApplyFullscreenSurface();

            headerPanelRect = CreatePanel(window, "BankHeaderPanel", new Color(0.015f, 0.11f, 0.16f, 0.72f));
            headerAccentRect = CreatePanel(window, "BankHeaderAccent", new Color(0.3f, 0.68f, 1f, 0.62f));
            profileGoldPlateRect = CreatePanel(window, "ProfileGoldPlate", new Color(0.18f, 0.12f, 0.025f, 0.78f));
            profileAmetistPlateRect = CreatePanel(window, "ProfileAmetistPlate", new Color(0.11f, 0.04f, 0.18f, 0.78f));
            exchangeCardRect = CreatePanel(window, "BankExchangeCard", new Color(0.02f, 0.085f, 0.12f, 0.72f));
            sourcePanelRect = CreatePanel(window, "AmetistSourcePanel", new Color(0.1f, 0.03f, 0.16f, 0.76f));
            resultPanelRect = CreatePanel(window, "GoldResultPanel", new Color(0.15f, 0.09f, 0.018f, 0.76f));
            ratePillRect = CreatePanel(window, "ExchangeRatePill", new Color(0.025f, 0.16f, 0.22f, 0.88f));
            connectorLineRect = CreatePanel(window, "ExchangeConnectorLine", new Color(0.35f, 0.7f, 1f, 0.32f));

            StyleSpritePanel(headerPanelRect, MainLobbyButtonStyle.BankModuleSprite, new Color(0.56f, 0.84f, 1f, 0.98f));
            StylePanel(profileGoldPlateRect, new Color(1f, 0.68f, 0.18f, 0.42f));
            StylePanel(profileAmetistPlateRect, new Color(0.72f, 0.38f, 1f, 0.48f));
            StyleSpritePanel(exchangeCardRect, MainLobbyButtonStyle.BankWindowFrameSprite, Color.white);
            StyleSpritePanel(sourcePanelRect, MainLobbyButtonStyle.BankModuleSprite, new Color(0.72f, 0.42f, 1f, 1f));
            StyleSpritePanel(resultPanelRect, MainLobbyButtonStyle.BankModuleSprite, new Color(1f, 0.68f, 0.2f, 1f));
            StyleSpritePanel(ratePillRect, MainLobbyButtonStyle.BankButtonSprite, new Color(0.62f, 0.88f, 1f, 1f));

            headerPanelRect.SetSiblingIndex(0);
            headerAccentRect.SetSiblingIndex(1);
            profileGoldPlateRect.SetSiblingIndex(2);
            profileAmetistPlateRect.SetSiblingIndex(3);
            exchangeCardRect.SetSiblingIndex(4);
            sourcePanelRect.SetSiblingIndex(5);
            resultPanelRect.SetSiblingIndex(6);
            ratePillRect.SetSiblingIndex(7);
            connectorLineRect.SetSiblingIndex(8);

            sourceWatermarkIcon = CreateIcon(window, "AmetistWatermark", MainLobbyButtonStyle.AmetistCurrencySprite);
            resultWatermarkIcon = CreateIcon(window, "GoldWatermark", MainLobbyButtonStyle.GoldCurrencySprite);
            sourceWatermarkIcon.color = new Color(0.8f, 0.55f, 1f, 0.055f);
            resultWatermarkIcon.color = new Color(1f, 0.78f, 0.28f, 0.055f);
            sourceWatermarkIcon.transform.SetSiblingIndex(9);
            resultWatermarkIcon.transform.SetSiblingIndex(10);

            eyebrowText = CreateText(window, "Eyebrow", EyebrowLabel(), 26f, FontStyles.Bold, new Color(0.34f, 0.72f, 1f, 1f));
            subtitleText = CreateText(window, "Subtitle", SubtitleLabel(), 24f, FontStyles.Normal, new Color(0.72f, 0.82f, 0.92f, 1f));
            profileCaptionText = CreateText(window, "ProfileCaption", ProfileCaptionLabel(), 24f, FontStyles.Bold, new Color(0.42f, 0.7f, 0.94f, 1f));
            exchangeCaptionText = CreateText(window, "ExchangeCaption", ExchangeCaptionLabel(), 28f, FontStyles.Bold, Color.white);
            sourceCaptionText = CreateText(window, "SourceCaption", SourceCaptionLabel(), 24f, FontStyles.Bold, new Color(0.84f, 0.66f, 1f, 1f));
            resultCaptionText = CreateText(window, "ResultCaption", ResultCaptionLabel(), 24f, FontStyles.Bold, new Color(1f, 0.8f, 0.38f, 1f));
            connectorText = CreateText(window, "ExchangeConnector", "\u2192", 52f, FontStyles.Bold, new Color(0.58f, 0.82f, 1f, 1f));
            connectorText.alignment = TextAlignmentOptions.Center;

            exchangeAmetistIcon = CreateIcon(window, "ExchangeAmetistIcon", MainLobbyButtonStyle.AmetistCurrencySprite);
            previewGoldIcon = CreateIcon(window, "PreviewGoldIcon", MainLobbyButtonStyle.GoldCurrencySprite);
            rateText = CreateText(window, "Rate", string.Empty, 30f, FontStyles.Bold, new Color(0.82f, 0.92f, 1f, 1f));
            rateText.alignment = TextAlignmentOptions.Center;
            exchangeInput = CreateInput(window, "ExchangeInput", AmetistAmountText());
            exchangeButton = CreateButton(window, "ExchangeButton", ExchangeText(), 36f);
            previewText = CreateText(window, "Preview", string.Empty, 42f, FontStyles.Bold, new Color(1f, 0.84f, 0.38f, 1f));
            previewText.alignment = TextAlignmentOptions.MidlineLeft;
            exchangeButton.onClick.AddListener(Exchange);
            exchangeInput.onValueChanged.AddListener(_ => RefreshValues());

            StyleInput(exchangeInput);
            StyleExchangeButton(exchangeButton);
        }

        protected override void LayoutContent(float width, float height, float pad)
        {
            ResolveFullscreenMetrics(out _, out _, out float safeLeft, out float safeRight, out float safeTop, out float safeBottom);
            float horizontalInset = Mathf.Clamp(width * 0.045f, 48f, 112f);
            float verticalInset = Mathf.Clamp(height * 0.035f, 24f, 52f);
            float left = safeLeft + horizontalInset;
            float right = width - safeRight - horizontalInset;
            float contentWidth = Mathf.Max(1f, right - left);
            float headerTop = safeTop + verticalInset;
            float headerHeight = Mathf.Clamp(height * 0.18f, 116f, 184f);
            float bodyTop = headerTop + headerHeight + Mathf.Clamp(height * 0.024f, 14f, 28f);
            float messageReserve = Mathf.Clamp(height * 0.085f, 48f, 86f) + safeBottom + 16f;
            float availableHeight = Mathf.Max(1f, height - bodyTop - messageReserve);
            float cardHeight = Mathf.Min(availableHeight, 620f);
            float cardY = bodyTop + Mathf.Max(0f, (availableHeight - cardHeight) * 0.5f);
            float cardPad = Mathf.Clamp(contentWidth * 0.035f, 28f, 64f);

            SetTopLeft(exchangeCardRect, left, -cardY, contentWidth, cardHeight);

            float topRowY = cardY + Mathf.Clamp(cardHeight * 0.12f, 38f, 58f);
            float captionHeight = Mathf.Clamp(cardHeight * 0.09f, 32f, 52f);
            float rateWidth = Mathf.Clamp(contentWidth * 0.34f, 300f, 570f);
            float rateHeight = Mathf.Clamp(cardHeight * 0.105f, 52f, 64f);
            float rateX = left + contentWidth - cardPad - rateWidth;
            float captionInset = Mathf.Clamp(contentWidth * 0.012f, 12f, 22f);
            SetTopLeft(exchangeCaptionText != null ? exchangeCaptionText.rectTransform : null, left + cardPad + captionInset, -topRowY, contentWidth - cardPad * 2f - rateWidth - captionInset - 28f, captionHeight);
            SetTopLeft(ratePillRect, rateX, -topRowY + 4f, rateWidth, rateHeight);
            SetTopLeft(rateText != null ? rateText.rectTransform : null, rateX + 18f, -topRowY + 4f, rateWidth - 36f, rateHeight);

            float buttonHeight = Mathf.Clamp(cardHeight * 0.14f, 58f, 88f);
            float buttonWidth = Mathf.Clamp(contentWidth * 0.34f, 280f, 560f);
            float buttonY = cardY + cardHeight - buttonHeight - Mathf.Clamp(cardHeight * 0.055f, 18f, 34f);
            float panelTop = topRowY + rateHeight + Mathf.Clamp(cardHeight * 0.055f, 18f, 34f);
            float panelHeight = Mathf.Max(116f, buttonY - panelTop - Mathf.Clamp(cardHeight * 0.055f, 18f, 34f));
            float connectorWidth = Mathf.Clamp(contentWidth * 0.065f, 58f, 104f);
            float panelGap = Mathf.Clamp(contentWidth * 0.018f, 14f, 32f);
            float panelWidth = Mathf.Max(1f, (contentWidth - cardPad * 2f - connectorWidth - panelGap * 2f) * 0.5f);
            float sourceX = left + cardPad;
            float connectorX = sourceX + panelWidth + panelGap;
            float resultX = connectorX + connectorWidth + panelGap;

            SetTopLeft(sourcePanelRect, sourceX, -panelTop, panelWidth, panelHeight);
            SetTopLeft(resultPanelRect, resultX, -panelTop, panelWidth, panelHeight);
            SetTopLeft(connectorLineRect, connectorX, -panelTop - panelHeight * 0.5f + 1f, connectorWidth, 2f);
            SetTopLeft(connectorText != null ? connectorText.rectTransform : null, connectorX, -panelTop - (panelHeight - 64f) * 0.5f, connectorWidth, 64f);

            float watermarkSize = Mathf.Clamp(panelHeight * 0.9f, 104f, 260f);
            SetTopLeft(sourceWatermarkIcon != null ? sourceWatermarkIcon.rectTransform : null, sourceX + panelWidth - watermarkSize - 8f, -panelTop - 8f, watermarkSize, watermarkSize);
            SetTopLeft(resultWatermarkIcon != null ? resultWatermarkIcon.rectTransform : null, resultX + panelWidth - watermarkSize - 8f, -panelTop - 8f, watermarkSize, watermarkSize);

            float innerPad = Mathf.Clamp(panelWidth * 0.1f, 30f, 58f);
            float panelCaptionHeight = Mathf.Clamp(panelHeight * 0.18f, 24f, 40f);
            float panelCaptionY = panelTop + Mathf.Clamp(panelHeight * 0.16f, 22f, 38f);
            SetTopLeft(sourceCaptionText != null ? sourceCaptionText.rectTransform : null, sourceX + innerPad, -panelCaptionY, panelWidth - innerPad * 2f, panelCaptionHeight);
            SetTopLeft(resultCaptionText != null ? resultCaptionText.rectTransform : null, resultX + innerPad, -panelCaptionY, panelWidth - innerPad * 2f, panelCaptionHeight);

            float valueRowHeight = Mathf.Clamp(panelHeight * 0.28f, 48f, 82f);
            float valueRowY = panelTop + Mathf.Clamp(panelHeight * 0.48f, 58f, 150f);
            valueRowY = Mathf.Min(valueRowY, panelTop + panelHeight - valueRowHeight - 16f);
            float iconSize = Mathf.Clamp(valueRowHeight * 0.72f, 38f, 62f);
            float iconGap = Mathf.Clamp(panelWidth * 0.035f, 12f, 24f);
            float inputX = sourceX + innerPad + iconSize + iconGap;
            float inputWidth = Mathf.Max(100f, panelWidth - innerPad * 2f - iconSize - iconGap);
            SetTopLeft(exchangeAmetistIcon != null ? exchangeAmetistIcon.rectTransform : null, sourceX + innerPad, -valueRowY - (valueRowHeight - iconSize) * 0.5f, iconSize, iconSize);
            SetTopLeft(exchangeInput != null ? exchangeInput.transform as RectTransform : null, inputX, -valueRowY, inputWidth, valueRowHeight);
            SetIconLabelRow(previewGoldIcon, previewText, resultX + innerPad, -valueRowY, panelWidth - innerPad * 2f, valueRowHeight, iconSize, iconGap);

            if (exchangeInput != null && exchangeInput.textViewport != null)
            {
                float inputHorizontalInset = Mathf.Clamp(valueRowHeight * 0.7f, 30f, 44f);
                float inputVerticalInset = Mathf.Clamp(valueRowHeight * 0.2f, 9f, 14f);
                exchangeInput.textViewport.offsetMin = new Vector2(inputHorizontalInset, inputVerticalInset);
                exchangeInput.textViewport.offsetMax = new Vector2(-inputHorizontalInset, -inputVerticalInset);
            }

            float buttonX = left + (contentWidth - buttonWidth) * 0.5f;
            SetTopLeft(exchangeButton != null ? exchangeButton.transform as RectTransform : null, buttonX, -buttonY, buttonWidth, buttonHeight);
            ConfigureActionLabel(exchangeButton, buttonHeight);
            ConfigurePanelCaption(sourceCaptionText);
            ConfigurePanelCaption(resultCaptionText);
        }

        protected override void RefreshContentText()
        {
            SetLabel(eyebrowText, EyebrowLabel());
            SetLabel(subtitleText, SubtitleLabel());
            SetLabel(profileCaptionText, ProfileCaptionLabel());
            SetLabel(exchangeCaptionText, ExchangeCaptionLabel());
            SetLabel(sourceCaptionText, SourceCaptionLabel());
            SetLabel(resultCaptionText, ResultCaptionLabel());
            SetPlaceholder(exchangeInput, AmetistAmountText());
            SetButtonLabel(exchangeButton, ExchangeText());
            SetLabel(rateText, RateText());
        }

        protected override void RefreshContentValues()
        {
            int profileAmetist = CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;
            int profileGold = CurrencyService.I != null ? CurrencyService.I.GetOzAltin() : 0;
            int amount = ReadAmount(exchangeInput);
            long result = (long)amount * goldPerAmetist;
            SetLabel(previewText, ResultValueText(result));

            if (exchangeButton != null)
                exchangeButton.interactable = ProfileService.I != null && ProfileService.I.Current != null && amount > 0 && profileAmetist >= amount && result <= int.MaxValue - (long)Mathf.Max(0, profileGold);
        }

        private void Exchange()
        {
            int amount = ReadAmount(exchangeInput);
            bool ok = amount > 0 && DynastyBankService.I != null && DynastyBankService.I.ExchangeProfileAmetistToGold(amount, goldPerAmetist);
            SetMessage(ok ? DoneText() : NotEnoughAmetistText());
            RefreshValues();
        }

        private string RateText()
        {
            return DynastyEconomyLoc.T($"\u041a\u0443\u0440\u0441: 1 \u0430\u043c\u0435\u0442\u0438\u0441\u0442 = {goldPerAmetist} \u0437\u043e\u043b\u043e\u0442\u0430", $"Rate: 1 amethyst = {goldPerAmetist} gold", $"Kur: 1 ametist = {goldPerAmetist} altın");
        }

        private void ApplyFullscreenSurface()
        {
            Image overlayImage = overlayRect != null ? overlayRect.GetComponent<Image>() : null;
            if (overlayImage != null)
                overlayImage.color = Color.black;

            Image windowImage = windowRect != null ? windowRect.GetComponent<Image>() : null;
            if (windowImage == null)
                return;

            Sprite backgroundSprite = MainLobbyButtonStyle.BankFullscreenBackgroundSprite;
            windowImage.sprite = backgroundSprite;
            windowImage.type = Image.Type.Simple;
            windowImage.preserveAspect = false;
            windowImage.color = backgroundSprite != null ? Color.white : new Color(0.025f, 0.07f, 0.1f, 1f);
        }

        private void ResolveFullscreenMetrics(
            out float rootWidth,
            out float rootHeight,
            out float safeLeft,
            out float safeRight,
            out float safeTop,
            out float safeBottom)
        {
            RectTransform measuredRect = overlayRect != null ? overlayRect : transform as RectTransform;
            float measuredWidth = measuredRect != null ? measuredRect.rect.width : 0f;
            float measuredHeight = measuredRect != null ? measuredRect.rect.height : 0f;
            Canvas canvas = rootCanvas != null ? rootCanvas.rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            float canvasWidth = canvasRect != null ? canvasRect.rect.width : 0f;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 0f;
            float scaleFactor = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            float screenWidth = Screen.width > 0 ? Screen.width / scaleFactor : 0f;
            float screenHeight = Screen.height > 0 ? Screen.height / scaleFactor : 0f;
            rootWidth = Mathf.Max(measuredWidth, Mathf.Max(canvasWidth, screenWidth));
            rootHeight = Mathf.Max(measuredHeight, Mathf.Max(canvasHeight, screenHeight));
            if (rootWidth <= 8f)
                rootWidth = 1920f;
            if (rootHeight <= 8f)
                rootHeight = 1080f;

            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
            safeLeft = safeArea.xMin / scaleFactor;
            safeRight = Mathf.Max(0f, Screen.width - safeArea.xMax) / scaleFactor;
            safeTop = Mathf.Max(0f, Screen.height - safeArea.yMax) / scaleFactor;
            safeBottom = safeArea.yMin / scaleFactor;
        }

        private static void StylePanel(RectTransform panel, Color outlineColor)
        {
            if (panel == null)
                return;

            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
            }

            Outline outline = panel.GetComponent<Outline>();
            if (outline == null)
                outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        private static void StyleSpritePanel(RectTransform panel, Sprite sprite, Color tint)
        {
            if (panel == null)
                return;

            if (sprite == null)
            {
                StylePanel(panel, tint);
                return;
            }

            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = false;
                image.color = tint;
            }

            Outline outline = panel.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
        }

        private static void StyleInput(TMP_InputField input)
        {
            if (input == null)
                return;

            Image image = input.GetComponent<Image>();
            if (image != null)
            {
                Sprite sprite = MainLobbyButtonStyle.BankModuleSprite;
                image.sprite = sprite;
                image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = false;
                image.color = new Color(0.58f, 0.36f, 0.82f, 1f);
            }

            Outline outline = input.GetComponent<Outline>();
            if (outline == null && MainLobbyButtonStyle.BankModuleSprite == null)
                outline = input.gameObject.AddComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = MainLobbyButtonStyle.BankModuleSprite == null;
                outline.effectColor = new Color(0.72f, 0.38f, 1f, 0.72f);
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;
            }
            input.selectionColor = new Color(0.72f, 0.38f, 1f, 0.42f);
        }

        private static void StyleExchangeButton(Button button)
        {
            if (button == null)
                return;

            if (button.image != null)
            {
                Sprite sprite = MainLobbyButtonStyle.BankButtonSprite;
                button.image.sprite = sprite;
                button.image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                button.image.preserveAspect = false;
                button.image.color = sprite != null ? Color.white : new Color(0.055f, 0.18f, 0.27f, 1f);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.46f, 0.72f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.32f, 0.4f, 0.44f, 0.72f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Outline outline = button.GetComponent<Outline>();
            if (outline == null && MainLobbyButtonStyle.BankButtonSprite == null)
                outline = button.gameObject.AddComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = MainLobbyButtonStyle.BankButtonSprite == null;
                outline.effectColor = new Color(0.4f, 0.78f, 1f, 0.72f);
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;
            }
        }

        private static void ConfigureHeaderLabel(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.fontSize = 26f;
            label.fontSizeMax = 26f;
            label.fontSizeMin = 16f;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigurePanelCaption(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.fontSize = 24f;
            label.fontSizeMax = 24f;
            label.fontSizeMin = 16f;
            label.characterSpacing = 1.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureActionLabel(Button button, float buttonHeight)
        {
            TextMeshProUGUI label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label == null)
                return;

            float fontSize = Mathf.Clamp(buttonHeight * 0.42f, 24f, 36f);
            label.fontSize = fontSize;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = 18f;
            label.enableAutoSizing = true;
            MainLobbyButtonStyle.ApplyButtonLabelLayout(label);
        }

        private static string EyebrowLabel() => DynastyEconomyLoc.T("\u041e\u0411\u041c\u0415\u041d \u0420\u0415\u0421\u0423\u0420\u0421\u041e\u0412", "RESOURCE EXCHANGE", "KAYNAK TAKASI");
        private static string SubtitleLabel() => DynastyEconomyLoc.T("\u041e\u0431\u043c\u0435\u043d\u0438\u0432\u0430\u0439\u0442\u0435 \u0430\u043c\u0435\u0442\u0438\u0441\u0442\u044b \u043d\u0430 \u0437\u043e\u043b\u043e\u0442\u043e \u043f\u043e \u0444\u0438\u043a\u0441\u0438\u0440\u043e\u0432\u0430\u043d\u043d\u043e\u043c\u0443 \u043a\u0443\u0440\u0441\u0443", "Exchange amethysts for gold at a fixed rate", "Ametisti sabit kurla alt\u0131na d\u00f6n\u00fc\u015ft\u00fcr\u00fcn");
        private static string ProfileCaptionLabel() => DynastyEconomyLoc.T("\u0411\u0410\u041b\u0410\u041d\u0421 \u041f\u0420\u041e\u0424\u0418\u041b\u042f", "PROFILE BALANCE", "PROF\u0130L BAK\u0130YES\u0130");
        private static string ExchangeCaptionLabel() => DynastyEconomyLoc.T("\u041a\u041e\u041d\u0412\u0415\u0420\u0422\u0410\u0426\u0418\u042f \u0412\u0410\u041b\u042e\u0422\u042b", "CURRENCY CONVERSION", "PARA B\u0130R\u0130M\u0130 D\u00d6N\u00dc\u015e\u00dcM\u00dc");
        private static string SourceCaptionLabel() => DynastyEconomyLoc.T("\u041e\u0422\u0414\u0410\u0401\u0422\u0415", "YOU SPEND", "VERECE\u011e\u0130N\u0130Z");
        private static string ResultCaptionLabel() => DynastyEconomyLoc.T("\u041f\u041e\u041b\u0423\u0427\u0410\u0415\u0422\u0415", "YOU RECEIVE", "ALACA\u011eINIZ");
        private static string ResultValueText(long value) => DynastyEconomyLoc.T($"{value} \u0437\u043e\u043b\u043e\u0442\u0430", $"{value} gold", $"{value} alt\u0131n");
        private static string ExchangeText() => DynastyEconomyLoc.T("\u041e\u0431\u043c\u0435\u043d\u044f\u0442\u044c", "Exchange", "De\u011fi\u015ftir");
    }
}

