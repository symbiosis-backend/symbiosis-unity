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

            if (!CurrencyService.I.SpendOzAmetist(ametistAmount))
                return false;

            CurrencyService.I.AddOzAltin(ametistAmount * goldPerAmetist);
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
        }

        protected virtual void OnRectTransformDimensionsChange()
        {
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

                openButtonLabel = CreateText(buttonObject.transform, "Label", ButtonText, 20f, FontStyles.Bold, Color.white);
                CenterLabelRect(openButtonLabel, new Vector4(8f, 0f, 8f, 0f));
            }
            else if (buttonParent != null && buttonRect.parent != buttonParent)
            {
                buttonRect.SetParent(buttonParent, false);
            }

            if (overlayRect == null)
            {
                GameObject overlay = new GameObject(OverlayObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                overlay.transform.SetParent(overlayParent, false);
                overlayRect = overlay.GetComponent<RectTransform>();

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
                MainLobbyButtonStyle.ApplyStoreBankWindow(windowImage);

                Button blocker = window.GetComponent<Button>();
                blocker.targetGraphic = windowImage;
                blocker.onClick.RemoveAllListeners();

                contentPanelRect = CreatePanel(window.transform, "ContentPanel", new Color(0.02f, 0.03f, 0.055f, 0.86f));
                titleText = CreateText(window.transform, "Title", TitleText, 34f, FontStyles.Bold, Color.white);
                profileGoldIcon = CreateIcon(window.transform, "ProfileGoldIcon", MainLobbyButtonStyle.GoldCurrencySprite);
                profileAmetistIcon = CreateIcon(window.transform, "ProfileAmetistIcon", MainLobbyButtonStyle.AmetistCurrencySprite);
                profileGoldText = CreateText(window.transform, "ProfileGold", string.Empty, 22f, FontStyles.Bold, new Color(1f, 0.82f, 0.34f, 1f));
                profileAmetistText = CreateText(window.transform, "ProfileAmetist", string.Empty, 22f, FontStyles.Bold, new Color(0.78f, 0.62f, 1f, 1f));
                messageText = CreateText(window.transform, "Message", string.Empty, 19f, FontStyles.Bold, new Color(1f, 0.56f, 0.45f, 1f));
                closeButton = CreateButton(window.transform, "CloseButton", CloseText(), 21f);
                closeButton.onClick.AddListener(Close);

                BuildContent(window.transform);
                overlay.SetActive(false);
            }
            else if (overlayParent != null && overlayRect.parent != overlayParent)
            {
                overlayRect.SetParent(overlayParent, false);
            }
        }

        protected abstract void BuildContent(Transform window);
        protected abstract void LayoutContent(float width, float height, float pad);
        protected abstract void RefreshContentText();
        protected abstract void RefreshContentValues();

        protected virtual void Open()
        {
            SetMessage(string.Empty);
            if (overlayRect != null)
                overlayRect.SetAsLastSibling();
            SetObjectActive(overlayRect != null ? overlayRect.gameObject : null, true);
            RefreshText();
            RefreshValues();
            Layout();
        }

        protected virtual void Close()
        {
            SetObjectActive(overlayRect != null ? overlayRect.gameObject : null, false);
        }

        protected virtual void RefreshText()
        {
            SetLabel(openButtonLabel, ButtonText);
            SetLabel(titleText, TitleText);
            SetButtonLabel(closeButton, CloseText());
            RefreshContentText();
        }

        protected virtual void RefreshValues()
        {
            int profileGold = CurrencyService.I != null ? CurrencyService.I.GetOzAltin() : 0;
            int profileAmetist = CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;
            SetLabel(profileGoldText, ProfileGoldText(profileGold));
            SetLabel(profileAmetistText, ProfileAmetistText(profileAmetist));

            bool hasProfile = ProfileService.I != null && ProfileService.I.Current != null;
            if (openButton != null)
                openButton.interactable = hasProfile;

            RefreshContentValues();
        }

        protected virtual void Layout()
        {
            SetMainLeftMenuButton(buttonRect, ButtonPosition, CentralPointLayout.MenuWidth, CentralPointLayout.MenuButtonHeight);
            ConfigureMenuButtonLabel(openButtonLabel, 30f, 18f);
            Stretch(overlayRect);

            if (windowRect == null)
                return;

            RectTransform rootRect = overlayRect != null ? overlayRect : transform as RectTransform;
            float rootWidth = rootRect != null ? Mathf.Max(960f, rootRect.rect.width) : 1280f;
            float rootHeight = rootRect != null ? Mathf.Max(540f, rootRect.rect.height) : 720f;
            float width = Mathf.Clamp(rootWidth * 0.62f, 760f, 980f);
            float height = Mathf.Clamp(rootHeight * 0.78f, 560f, 720f);
            float pad = Mathf.Clamp(width * 0.1f, 76f, 104f);
            float headerTop = -56f;
            float closeWidth = 102f;
            float statsTop = -126f;
            float statsWidth = width - pad * 2f;
            float statsColumnWidth = (statsWidth - 24f) * 0.5f;

            SetTopLeft(windowRect, (rootWidth - width) * 0.5f, -(rootHeight - height) * 0.5f, width, height);
            SetTopLeft(contentPanelRect, pad - 18f, -112f, width - (pad - 18f) * 2f, height - 208f);
            SetTopLeft(titleText != null ? titleText.rectTransform : null, pad, headerTop, width - pad * 2f - closeWidth - 18f, 48f);
            SetTopLeft(closeButton != null ? closeButton.transform as RectTransform : null, width - pad - closeWidth, headerTop + 6f, closeWidth, 44f);
            SetIconLabelRow(profileGoldIcon, profileGoldText, pad, statsTop, statsColumnWidth, 34f, 28f, 12f);
            SetIconLabelRow(profileAmetistIcon, profileAmetistText, pad + statsColumnWidth + 24f, statsTop, statsColumnWidth, 34f, 28f, 12f);
            LayoutContent(width, height, pad);
            SetTopLeft(messageText != null ? messageText.rectTransform : null, pad, -height + 70f, width - pad * 2f, 30f);
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
            text.overflowMode = TextOverflowModes.Ellipsis;
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
            CenterLabelRect(text, new Vector4(10f, 0f, 10f, 0f));
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
            textAreaRect.offsetMin = new Vector2(18f, 4f);
            textAreaRect.offsetMax = new Vector2(-18f, -4f);

            TextMeshProUGUI placeholderText = CreateText(textArea.transform, "Placeholder", placeholder, 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.5f));
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(placeholderText.rectTransform);

            TextMeshProUGUI text = CreateText(textArea.transform, "Text", string.Empty, 24f, FontStyles.Bold, Color.white);
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
            label.overflowMode = TextOverflowModes.Ellipsis;
            SetTopLeft(label.rectTransform, x + iconSize + gap, y, Mathf.Max(0f, width - iconSize - gap), height);
        }

        protected static void SetMainLeftMenuButton(RectTransform rect, Vector2 position, float width, float height)
        {
            if (rect == null)
                return;

            CentralPointLayout.SetTopLeft(rect, position, new Vector2(width, height));
        }

        protected static void ConfigureMenuButtonLabel(TextMeshProUGUI label, float maxSize, float minSize)
        {
            if (label == null)
                return;

            label.fontSize = maxSize;
            label.fontSizeMax = maxSize;
            label.fontSizeMin = minSize;
            label.enableAutoSizing = true;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            CenterLabelRect(label, Vector4.zero);
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

        protected static string GoldAmountText() => DynastyEconomyLoc.T("\u0417\u043e\u043b\u043e\u0442\u043e", "Gold", "Altin");
        protected static string AmetistAmountText() => DynastyEconomyLoc.T("\u0410\u043c\u0435\u0442\u0438\u0441\u0442\u044b", "Amethysts", "Ametist");

        protected static void SetObjectActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        protected static string CloseText() => DynastyEconomyLoc.T("\u0417\u0430\u043a\u0440\u044b\u0442\u044c", "Close", "Kapat");
        protected static string AmountText() => DynastyEconomyLoc.T("\u0421\u0443\u043c\u043c\u0430", "Amount", "Miktar");
        protected static string DepositText() => DynastyEconomyLoc.T("\u041f\u043e\u043b\u043e\u0436\u0438\u0442\u044c", "Deposit", "Yatir");
        protected static string WithdrawText() => DynastyEconomyLoc.T("\u0417\u0430\u0431\u0440\u0430\u0442\u044c", "Withdraw", "Cek");
        protected static string DoneText() => DynastyEconomyLoc.T("\u0413\u043e\u0442\u043e\u0432\u043e.", "Done.", "Tamam.");
        protected static string NotEnoughText() => DynastyEconomyLoc.T("\u041d\u0435\u0434\u043e\u0441\u0442\u0430\u0442\u043e\u0447\u043d\u043e \u0432\u0430\u043b\u044e\u0442\u044b.", "Not enough currency.", "Yeterli para yok.");
        protected static string NotEnoughAmetistText() => DynastyEconomyLoc.T("\u041d\u0435\u0434\u043e\u0441\u0442\u0430\u0442\u043e\u0447\u043d\u043e \u0430\u043c\u0435\u0442\u0438\u0441\u0442\u043e\u0432.", "Not enough amethysts.", "Yeterli ametist yok.");
        protected static string ProfileGoldText(int value) => DynastyEconomyLoc.T($"\u041f\u0440\u043e\u0444\u0438\u043b\u044c \u0437\u043e\u043b\u043e\u0442\u043e: {value}", $"Profile gold: {value}", $"Profil altin: {value}");
        protected static string ProfileAmetistText(int value) => DynastyEconomyLoc.T($"\u041f\u0440\u043e\u0444\u0438\u043b\u044c \u0430\u043c\u0435\u0442\u0438\u0441\u0442\u044b: {value}", $"Profile amethysts: {value}", $"Profil ametist: {value}");
    }

    [DisallowMultipleComponent]
    public sealed class DynastyVaultUI : DynastyEconomyWindowBase
    {
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

        protected override string ButtonObjectName => "DynastyVaultButton";
        protected override string OverlayObjectName => "DynastyVaultOverlay";
        protected override string ButtonText => DynastyEconomyLoc.T("\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435", "Vault", "Depo");
        protected override string TitleText => DynastyEconomyLoc.T("\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435 \u0434\u0438\u043d\u0430\u0441\u0442\u0438\u0438", "Dynasty Vault", "Hanedan Deposu");
        protected override Vector2 ButtonPosition => new Vector2(
            CentralPointLayout.LeftX,
            CentralPointLayout.TopY - CentralPointLayout.ProfileHeight - CentralPointLayout.MenuGap);
        protected override Color AccentColor => new Color(0.11f, 0.16f, 0.24f, 0.96f);

        protected override void OnEnable()
        {
            DynastyVaultService.VaultChanged += RefreshValues;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            DynastyVaultService.VaultChanged -= RefreshValues;
            base.OnDisable();
        }

        protected override void BuildContent(Transform window)
        {
            vaultGoldIcon = CreateIcon(window, "VaultGoldIcon", MainLobbyButtonStyle.GoldCurrencySprite);
            vaultAmetistIcon = CreateIcon(window, "VaultAmetistIcon", MainLobbyButtonStyle.AmetistCurrencySprite);
            vaultGoldText = CreateText(window, "VaultGold", string.Empty, 22f, FontStyles.Bold, new Color(1f, 0.82f, 0.34f, 1f));
            vaultAmetistText = CreateText(window, "VaultAmetist", string.Empty, 22f, FontStyles.Bold, new Color(0.78f, 0.62f, 1f, 1f));
            goldInput = CreateInput(window, "GoldInput", GoldAmountText());
            ametistInput = CreateInput(window, "AmetistInput", AmetistAmountText());
            depositGoldButton = CreateButton(window, "DepositGoldButton", DepositText(), 20f);
            withdrawGoldButton = CreateButton(window, "WithdrawGoldButton", WithdrawText(), 20f);
            depositAmetistButton = CreateButton(window, "DepositAmetistButton", DepositText(), 20f);
            withdrawAmetistButton = CreateButton(window, "WithdrawAmetistButton", WithdrawText(), 20f);
            OffsetButtonLabel(depositGoldButton, -36f, 0f);
            OffsetButtonLabel(withdrawGoldButton, -36f, 0f);
            OffsetButtonLabel(depositAmetistButton, -36f, 0f);
            OffsetButtonLabel(withdrawAmetistButton, -36f, 0f);

            depositGoldButton.onClick.AddListener(() => TransferGold(true));
            withdrawGoldButton.onClick.AddListener(() => TransferGold(false));
            depositAmetistButton.onClick.AddListener(() => TransferAmetist(true));
            withdrawAmetistButton.onClick.AddListener(() => TransferAmetist(false));
        }

        protected override void LayoutContent(float width, float height, float pad)
        {
            float innerWidth = width - pad * 2f;
            float infoGap = 24f;
            float infoWidth = (innerWidth - infoGap) * 0.5f;
            float rowHeight = 56f;
            float rowGap = 22f;
            float inputWidth = Mathf.Clamp(innerWidth * 0.34f, 230f, 310f);
            float buttonGap = 16f;
            float buttonWidth = Mathf.Clamp((innerWidth - inputWidth - buttonGap * 2f) * 0.5f, 136f, 220f);
            float y = -264f;

            SetIconLabelRow(vaultGoldIcon, vaultGoldText, pad, -188f, infoWidth, 32f, 26f, 10f);
            SetIconLabelRow(vaultAmetistIcon, vaultAmetistText, pad + infoWidth + infoGap, -188f, infoWidth, 32f, 26f, 10f);

            SetTopLeft(goldInput != null ? goldInput.transform as RectTransform : null, pad, y, inputWidth, rowHeight);
            SetTopLeft(depositGoldButton != null ? depositGoldButton.transform as RectTransform : null, pad + inputWidth + buttonGap, y, buttonWidth, rowHeight);
            SetTopLeft(withdrawGoldButton != null ? withdrawGoldButton.transform as RectTransform : null, pad + inputWidth + buttonGap * 2f + buttonWidth, y, buttonWidth, rowHeight);

            y -= rowHeight + rowGap;
            SetTopLeft(ametistInput != null ? ametistInput.transform as RectTransform : null, pad, y, inputWidth, rowHeight);
            SetTopLeft(depositAmetistButton != null ? depositAmetistButton.transform as RectTransform : null, pad + inputWidth + buttonGap, y, buttonWidth, rowHeight);
            SetTopLeft(withdrawAmetistButton != null ? withdrawAmetistButton.transform as RectTransform : null, pad + inputWidth + buttonGap * 2f + buttonWidth, y, buttonWidth, rowHeight);
        }

        protected override void RefreshContentText()
        {
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
            SetLabel(vaultGoldText, DynastyEconomyLoc.T($"\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435 \u0437\u043e\u043b\u043e\u0442\u043e: {vaultGold}", $"Vault gold: {vaultGold}", $"Depo altin: {vaultGold}"));
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
    }

    [DisallowMultipleComponent]
    public sealed class DynastyBankUI : DynastyEconomyWindowBase
    {
        [SerializeField, Min(1)] private int goldPerAmetist = 100;

        private TMP_InputField exchangeInput;
        private Button exchangeButton;
        private Image exchangeAmetistIcon;
        private Image previewGoldIcon;
        private TextMeshProUGUI rateText;
        private TextMeshProUGUI previewText;

        protected override string ButtonObjectName => "DynastyBankButton";
        protected override string OverlayObjectName => "DynastyBankOverlay";
        protected override string ButtonText => DynastyEconomyLoc.T("\u0411\u0430\u043d\u043a", "Bank", "Banka");
        protected override string TitleText => DynastyEconomyLoc.T("\u0411\u0430\u043d\u043a", "Bank", "Banka");
        protected override Vector2 ButtonPosition => new Vector2(
            CentralPointLayout.LeftX,
            CentralPointLayout.TopY - CentralPointLayout.ProfileHeight - CentralPointLayout.MenuGap * 2f - CentralPointLayout.MenuButtonHeight);
        protected override Color AccentColor => new Color(0.18f, 0.16f, 0.08f, 0.96f);

        protected override void OnEnable()
        {
            DynastyBankService.BankChanged += RefreshValues;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            DynastyBankService.BankChanged -= RefreshValues;
            base.OnDisable();
        }

        protected override void BuildContent(Transform window)
        {
            exchangeAmetistIcon = CreateIcon(window, "ExchangeAmetistIcon", MainLobbyButtonStyle.AmetistCurrencySprite);
            previewGoldIcon = CreateIcon(window, "PreviewGoldIcon", MainLobbyButtonStyle.GoldCurrencySprite);
            rateText = CreateText(window, "Rate", string.Empty, 24f, FontStyles.Bold, new Color(0.82f, 0.9f, 1f, 1f));
            exchangeInput = CreateInput(window, "ExchangeInput", AmetistAmountText());
            exchangeButton = CreateButton(window, "ExchangeButton", ExchangeText(), 22f);
            OffsetButtonLabel(exchangeButton, -36f, 0f);
            previewText = CreateText(window, "Preview", string.Empty, 22f, FontStyles.Bold, new Color(0.78f, 0.9f, 1f, 1f));
            exchangeButton.onClick.AddListener(Exchange);
            exchangeInput.onValueChanged.AddListener(_ => RefreshValues());
        }

        protected override void LayoutContent(float width, float height, float pad)
        {
            float innerWidth = width - pad * 2f;
            float iconSize = 28f;
            float iconGap = 12f;
            float inputWidth = Mathf.Clamp(innerWidth * 0.34f, 228f, 320f);
            float buttonWidth = Mathf.Clamp(innerWidth * 0.28f, 176f, 240f);

            SetTopLeft(rateText != null ? rateText.rectTransform : null, pad, -192f, innerWidth, 38f);
            SetTopLeft(exchangeAmetistIcon != null ? exchangeAmetistIcon.rectTransform : null, pad, -299f, iconSize, iconSize);
            SetTopLeft(exchangeInput != null ? exchangeInput.transform as RectTransform : null, pad + iconSize + iconGap, -284f, inputWidth, 58f);
            SetTopLeft(exchangeButton != null ? exchangeButton.transform as RectTransform : null, pad + iconSize + iconGap + inputWidth + 18f, -284f, buttonWidth, 58f);
            SetIconLabelRow(previewGoldIcon, previewText, pad, -364f, innerWidth, 36f, 26f, 10f);
        }

        protected override void RefreshContentText()
        {
            SetPlaceholder(exchangeInput, AmetistAmountText());
            SetButtonLabel(exchangeButton, ExchangeText());
            SetLabel(rateText, RateText());
        }

        protected override void RefreshContentValues()
        {
            int profileAmetist = CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;
            int amount = ReadAmount(exchangeInput);
            SetLabel(previewText, DynastyEconomyLoc.T($"\u041f\u043e\u043b\u0443\u0447\u0438\u0442\u0435 \u0437\u043e\u043b\u043e\u0442\u043e: {amount * goldPerAmetist}", $"Gold to receive: {amount * goldPerAmetist}", $"Alinacak altin: {amount * goldPerAmetist}"));

            if (exchangeButton != null)
                exchangeButton.interactable = ProfileService.I != null && ProfileService.I.Current != null && amount > 0 && profileAmetist >= amount;
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
            return DynastyEconomyLoc.T($"\u041a\u0443\u0440\u0441: 1 \u0430\u043c\u0435\u0442\u0438\u0441\u0442 = {goldPerAmetist} \u0437\u043e\u043b\u043e\u0442\u0430", $"Rate: 1 amethyst = {goldPerAmetist} gold", $"Kur: 1 ametist = {goldPerAmetist} altin");
        }

        private static string ExchangeText() => DynastyEconomyLoc.T("\u041e\u0431\u043c\u0435\u043d\u044f\u0442\u044c", "Exchange", "Degistir");
    }
}

