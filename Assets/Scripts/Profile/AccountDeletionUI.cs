using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    public sealed class AccountDeletionUI : MonoBehaviour
    {
        private const string DeletionInfoUrl = "https://dlsymbiosis.com/account-deletion";

        private string entrySceneName = "Entry";
        private RectTransform safeAreaRoot;
        private TMP_InputField passwordInput;
        private TMP_InputField confirmationInput;
        private TextMeshProUGUI warningText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI deleteButtonLabel;
        private Button deleteButton;
        private bool finalConfirmation;
        private bool deleting;

        public static void Show(string targetEntrySceneName)
        {
            AccountDeletionUI existing = FindAnyObjectByType<AccountDeletionUI>();
            if (existing != null)
            {
                existing.entrySceneName = string.IsNullOrWhiteSpace(targetEntrySceneName) ? "Entry" : targetEntrySceneName;
                existing.gameObject.SetActive(true);
                existing.transform.SetAsLastSibling();
                return;
            }

            GameObject host = new GameObject(
                "AccountDeletionOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(AccountDeletionUI));

            Canvas canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            CanvasScaler scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            AccountDeletionUI ui = host.GetComponent<AccountDeletionUI>();
            ui.entrySceneName = string.IsNullOrWhiteSpace(targetEntrySceneName) ? "Entry" : targetEntrySceneName;
        }

        private void Awake()
        {
            Build();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (safeAreaRoot != null)
                ApplySafeArea();
        }

        private void Build()
        {
            RectTransform hostRect = GetComponent<RectTransform>();
            Stretch(hostRect);

            GameObject backdrop = CreatePanel(transform, "Backdrop", new Color(0.005f, 0.008f, 0.02f, 0.93f));
            Stretch(backdrop.GetComponent<RectTransform>());

            GameObject safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(transform, false);
            safeAreaRoot = safe.GetComponent<RectTransform>();
            Stretch(safeAreaRoot);

            GameObject window = CreatePanel(safe.transform, "Window", new Color(0.055f, 0.07f, 0.11f, 0.99f));
            RectTransform windowRect = window.GetComponent<RectTransform>();
            Center(windowRect, Vector2.zero, new Vector2(1160f, 760f));

            Image windowImage = window.GetComponent<Image>();
            windowImage.outline();

            CreateText(window.transform, "Title", Title(), 46f, FontStyles.Bold, new Vector2(0f, 286f), new Vector2(1020f, 74f), Color.white);
            warningText = CreateText(window.transform, "Warning", InitialWarning(), 27f, FontStyles.Normal, new Vector2(0f, 166f), new Vector2(980f, 150f), new Color(0.94f, 0.88f, 0.76f, 1f));

            CreateText(window.transform, "PasswordLabel", PasswordLabel(), 24f, FontStyles.Bold, new Vector2(-300f, 54f), new Vector2(400f, 42f), Color.white);
            passwordInput = CreateInput(window.transform, "PasswordInput", PasswordPlaceholder(), new Vector2(-300f, -10f), new Vector2(430f, 72f), true);

            CreateText(window.transform, "ConfirmationLabel", ConfirmationLabel(), 24f, FontStyles.Bold, new Vector2(270f, 54f), new Vector2(470f, 42f), Color.white);
            confirmationInput = CreateInput(window.transform, "ConfirmationInput", "DELETE", new Vector2(270f, -10f), new Vector2(430f, 72f), false);
            confirmationInput.characterLimit = 12;

            statusText = CreateText(window.transform, "Status", string.Empty, 24f, FontStyles.Bold, new Vector2(0f, -105f), new Vector2(980f, 70f), new Color(1f, 0.5f, 0.42f, 1f));

            Button infoButton = CreateButton(window.transform, "DeletionInfoButton", InfoLabel(), new Vector2(-350f, -205f), new Vector2(280f, 76f), new Color(0.12f, 0.28f, 0.48f, 1f));
            infoButton.onClick.AddListener(() => Application.OpenURL(DeletionInfoUrl));

            Button cancelButton = CreateButton(window.transform, "CancelButton", CancelLabel(), new Vector2(0f, -205f), new Vector2(280f, 76f), new Color(0.16f, 0.19f, 0.25f, 1f));
            cancelButton.onClick.AddListener(Close);

            deleteButton = CreateButton(window.transform, "DeleteAccountButton", ContinueLabel(), new Vector2(350f, -205f), new Vector2(320f, 76f), new Color(0.65f, 0.08f, 0.08f, 1f));
            deleteButtonLabel = deleteButton.GetComponentInChildren<TextMeshProUGUI>(true);
            deleteButton.onClick.AddListener(OnDeleteClicked);

            CreateText(
                window.transform,
                "RetentionNote",
                RetentionNote(),
                20f,
                FontStyles.Normal,
                new Vector2(0f, -300f),
                new Vector2(1000f, 86f),
                new Color(0.68f, 0.76f, 0.9f, 1f));
        }

        private void OnDeleteClicked()
        {
            if (deleting)
                return;

            string confirmation = confirmationInput != null ? confirmationInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;
            if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal))
            {
                SetStatus(TypeDeleteError());
                return;
            }

            if (!string.IsNullOrWhiteSpace(ProfileService.I != null ? ProfileService.I.CurrentAccountEmail : string.Empty) &&
                string.IsNullOrEmpty(password))
            {
                SetStatus(PasswordRequiredError());
                return;
            }

            if (!finalConfirmation)
            {
                finalConfirmation = true;
                warningText.text = FinalWarning();
                if (deleteButtonLabel != null)
                    deleteButtonLabel.text = DeletePermanentlyLabel();
                SetStatus(string.Empty);
                return;
            }

            StartCoroutine(DeleteRoutine(password));
        }

        private IEnumerator DeleteRoutine(string password)
        {
            if (ProfileService.I == null)
            {
                SetStatus(ServiceMissingError());
                yield break;
            }

            deleting = true;
            deleteButton.interactable = false;
            SetStatus(DeletingLabel(), new Color(1f, 0.78f, 0.25f, 1f));

            bool success = false;
            string error = string.Empty;
            yield return ProfileService.I.DeleteAccountOnServer(password, (ok, message) =>
            {
                success = ok;
                error = message;
            });

            if (!success)
            {
                deleting = false;
                deleteButton.interactable = true;
                finalConfirmation = false;
                warningText.text = InitialWarning();
                if (deleteButtonLabel != null)
                    deleteButtonLabel.text = ContinueLabel();
                SetStatus(string.IsNullOrWhiteSpace(error) ? DeleteFailedError() : error);
                yield break;
            }

            SetStatus(DeletedLabel(), new Color(0.35f, 0.95f, 0.58f, 1f));
            yield return new WaitForSecondsRealtime(0.8f);
            SceneManager.LoadScene(string.IsNullOrWhiteSpace(entrySceneName) ? "Entry" : entrySceneName);
        }

        private void Close()
        {
            if (!deleting)
                Destroy(gameObject);
        }

        private void SetStatus(string value)
        {
            SetStatus(value, new Color(1f, 0.5f, 0.42f, 1f));
        }

        private void SetStatus(string value, Color color)
        {
            if (statusText == null)
                return;
            statusText.text = value ?? string.Empty;
            statusText.color = color;
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safeArea = Screen.safeArea;
            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, FontStyles style, Vector2 position, Vector2 dimensions, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            Center(rect, position, dimensions);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            MainLobbyButtonStyle.ApplyFont(text);
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholderValue, Vector2 position, Vector2 dimensions, bool password)
        {
            GameObject root = CreatePanel(parent, name, new Color(0.025f, 0.035f, 0.065f, 1f));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Center(rootRect, position, dimensions);

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(root.transform, false);
            RectTransform areaRect = textArea.GetComponent<RectTransform>();
            Stretch(areaRect);
            areaRect.offsetMin = new Vector2(22f, 10f);
            areaRect.offsetMax = new Vector2(-22f, -10f);

            TextMeshProUGUI placeholder = CreateText(textArea.transform, "Placeholder", placeholderValue, 24f, FontStyles.Italic, Vector2.zero, Vector2.zero, new Color(0.55f, 0.62f, 0.75f, 1f));
            Stretch(placeholder.rectTransform);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            TextMeshProUGUI inputText = CreateText(textArea.transform, "Text", string.Empty, 26f, FontStyles.Normal, Vector2.zero, Vector2.zero, Color.white);
            Stretch(inputText.rectTransform);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            TMP_InputField field = root.AddComponent<TMP_InputField>();
            field.textViewport = areaRect;
            field.textComponent = inputText;
            field.placeholder = placeholder;
            field.targetGraphic = root.GetComponent<Image>();
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            field.characterLimit = 64;
            field.navigation = new Navigation { mode = Navigation.Mode.None };
            return field;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 dimensions, Color color)
        {
            GameObject go = CreatePanel(parent, name, color);
            RectTransform rect = go.GetComponent<RectTransform>();
            Center(rect, position, dimensions);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            TextMeshProUGUI text = CreateText(go.transform, "Label", label, 25f, FontStyles.Bold, Vector2.zero, Vector2.zero, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        private static void Center(RectTransform rect, Vector2 position, Vector2 dimensions)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameLanguage Language => AppSettings.I != null ? AppSettings.I.Language : GameLanguage.English;
        private static string Localized(string ru, string en, string tr, string de)
        {
            switch (Language)
            {
                case GameLanguage.Russian: return ru;
                case GameLanguage.Turkish: return tr;
                case GameLanguage.German: return de;
                default: return en;
            }
        }

        private static string Title() => Localized("Удаление аккаунта", "Delete Account", "Hesabı Sil", "Konto löschen");
        private static string InitialWarning() => Localized(
            "Будут безвозвратно удалены весь аккаунт, все 3 профиля, прогресс, друзья и сообщения. Это действие нельзя отменить.",
            "Your entire account, all 3 profiles, progress, friends, and messages will be permanently deleted. This cannot be undone.",
            "Tüm hesabınız, 3 profiliniz, ilerlemeniz, arkadaşlarınız ve mesajlarınız kalıcı olarak silinir. Bu işlem geri alınamaz.",
            "Dein gesamtes Konto, alle 3 Profile, Fortschritt, Freunde und Nachrichten werden dauerhaft gelöscht.");
        private static string FinalWarning() => Localized(
            "Последнее подтверждение: нажмите «Удалить навсегда».",
            "Final confirmation: select Delete Permanently.",
            "Son onay: Kalıcı Olarak Sil'i seçin.",
            "Letzte Bestätigung: Wähle Dauerhaft löschen.");
        private static string PasswordLabel() => Localized("Пароль аккаунта", "Account password", "Hesap şifresi", "Kontopasswort");
        private static string PasswordPlaceholder() => Localized("Введите пароль", "Enter password", "Şifreyi girin", "Passwort eingeben");
        private static string ConfirmationLabel() => Localized("Введите DELETE", "Type DELETE", "DELETE yazın", "DELETE eingeben");
        private static string InfoLabel() => Localized("Что удаляется", "Deletion details", "Silme ayrıntıları", "Details zur Löschung");
        private static string CancelLabel() => Localized("Отмена", "Cancel", "İptal", "Abbrechen");
        private static string ContinueLabel() => Localized("Продолжить", "Continue", "Devam", "Weiter");
        private static string DeletePermanentlyLabel() => Localized("Удалить навсегда", "Delete Permanently", "Kalıcı Olarak Sil", "Dauerhaft löschen");
        private static string RetentionNote() => Localized(
            "Для безопасности и требований магазина могут храниться только обезличенные записи покупок и защиты от мошенничества.",
            "Only de-identified purchase and fraud-prevention records may be retained for legal and platform obligations.",
            "Yalnızca kimliksizleştirilmiş satın alma ve dolandırıcılık önleme kayıtları yasal yükümlülükler için saklanabilir.",
            "Nur anonymisierte Kauf- und Betrugspräventionsdaten dürfen für rechtliche Pflichten aufbewahrt werden.");
        private static string TypeDeleteError() => Localized("Введите слово DELETE.", "Type DELETE exactly.", "DELETE kelimesini yazın.", "Gib DELETE exakt ein.");
        private static string PasswordRequiredError() => Localized("Введите пароль аккаунта.", "Enter your account password.", "Hesap şifrenizi girin.", "Gib dein Kontopasswort ein.");
        private static string ServiceMissingError() => Localized("Сервис профиля недоступен.", "Profile service is unavailable.", "Profil hizmeti kullanılamıyor.", "Profildienst ist nicht verfügbar.");
        private static string DeletingLabel() => Localized("Удаление аккаунта…", "Deleting account…", "Hesap siliniyor…", "Konto wird gelöscht…");
        private static string DeletedLabel() => Localized("Аккаунт удалён.", "Account deleted.", "Hesap silindi.", "Konto gelöscht.");
        private static string DeleteFailedError() => Localized("Не удалось удалить аккаунт.", "Account deletion failed.", "Hesap silinemedi.", "Kontolöschung fehlgeschlagen.");
    }

    internal static class ImageOutlineExtension
    {
        public static void outline(this Image image)
        {
            if (image == null || image.GetComponent<Outline>() != null)
                return;
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.65f, 1f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }
}
