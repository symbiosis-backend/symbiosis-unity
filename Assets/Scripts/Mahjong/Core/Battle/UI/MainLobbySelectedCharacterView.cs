using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MainLobbySelectedCharacterView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image lobbyCharacterImage;
        [SerializeField] private BattleCharacterModelView lobbyCharacterModelView;

        [Header("Buttons")]
        [SerializeField] private Button selectCharacterButton;
        [SerializeField] private Button changeCharacterButton;
        [SerializeField] private bool hideExplicitCharacterButtons = true;
        [SerializeField] private bool openSelectionByLobbyCharacterClick = true;

        [Header("Optional Button Texts")]
        [SerializeField] private TMP_Text selectCharacterButtonText;
        [SerializeField] private TMP_Text changeCharacterButtonText;

        [Header("Optional")]
        [SerializeField] private TMP_Text selectedCharacterNameText;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject selectedStateRoot;

        [Header("Scene Navigation")]
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";
        [SerializeField] private bool loadSceneOnClick = true;

        [Header("Text")]
        [SerializeField] private string selectButtonTextValue = "Выбрать персонажа";
        [SerializeField] private string changeButtonTextValue = "Сменить персонажа";
        [SerializeField] private string emptyNameText = "";

        [Header("Fallback")]
        [SerializeField] private Sprite fallbackLobbySprite;
        [SerializeField] private bool useSelectSpriteIfLobbySpriteMissing = true;

        [Header("Image Fit")]
        [SerializeField] private bool preserveAspect = true;
        [SerializeField] private bool useNativeSize = false;
        [SerializeField] private bool limitSize = true;
        [SerializeField] private float maxWidth = 420f;
        [SerializeField] private float maxHeight = 420f;

        [Header("Image Alignment")]
        [SerializeField] private bool anchorToBottomLeft = true;
        [SerializeField] private float imageOffsetX = 12f;
        [SerializeField] private float imageOffsetY = 0f;

        private bool subscribed;
        private Button lobbyCharacterClickButton;

        private void Awake()
        {
            ApplyLobbyAnchor();
            BindButtons();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            AppSettings.OnLanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            AppSettings.OnLanguageChanged -= OnLanguageChanged;
            Unsubscribe();
        }

        public void Refresh()
        {
            BattleCharacterDatabase.BattleCharacterData data = GetSelectedCharacterData();
            bool hasCharacter = data != null;
            Sprite targetSprite = GetLobbyDisplaySprite(data);

            bool showingModel = ApplyLobbyModel(data);
            ApplyLobbyImage(showingModel ? null : targetSprite);
            ApplyLobbyAnchor();
            ConfigureLobbyCharacterClickTarget();

            if (selectedCharacterNameText != null)
                selectedCharacterNameText.text = hasCharacter ? GetSafeName(data) : emptyNameText;

            if (selectCharacterButtonText != null)
                selectCharacterButtonText.text = GetLocalizedText("battle.character.select_character", selectButtonTextValue);

            if (changeCharacterButtonText != null)
                changeCharacterButtonText.text = GetLocalizedText("battle.character.change_character", changeButtonTextValue);

            ApplyCharacterButtonVisibility(hasCharacter);

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(!hasCharacter);

            if (selectedStateRoot != null)
                selectedStateRoot.SetActive(hasCharacter);
        }

        public void OpenBattleLobby()
        {
            if (!loadSceneOnClick)
                return;

            if (string.IsNullOrWhiteSpace(battleLobbySceneName))
                return;

            SceneManager.LoadScene(battleLobbySceneName);
        }

        private void ApplyLobbyImage(Sprite sprite)
        {
            if (lobbyCharacterImage == null)
                return;

            lobbyCharacterImage.sprite = sprite;
            lobbyCharacterImage.enabled = sprite != null;
            lobbyCharacterImage.preserveAspect = preserveAspect;

            Color c = lobbyCharacterImage.color;
            c.a = sprite != null ? 1f : 0f;
            lobbyCharacterImage.color = c;

            if (sprite == null)
                return;

            ApplyLobbyAnchor();
        }

        private void ApplyLobbyAnchor()
        {
            if (lobbyCharacterImage == null)
                return;

            RectTransform rt = lobbyCharacterImage.rectTransform;

            if (anchorToBottomLeft)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
            }

            if (useNativeSize)
                lobbyCharacterImage.SetNativeSize();

            if (limitSize)
            {
                float width = Mathf.Max(1f, rt.rect.width);
                float height = Mathf.Max(1f, rt.rect.height);
                FitInside(rt, width, height, maxWidth, maxHeight);
            }

            rt.anchoredPosition = new Vector2(imageOffsetX, imageOffsetY);
        }

        private bool ApplyLobbyModel(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (lobbyCharacterModelView == null && lobbyCharacterImage != null)
                lobbyCharacterModelView = lobbyCharacterImage.GetComponent<BattleCharacterModelView>();

            if (lobbyCharacterModelView == null && lobbyCharacterImage != null)
                lobbyCharacterModelView = lobbyCharacterImage.gameObject.AddComponent<BattleCharacterModelView>();

            if (lobbyCharacterModelView == null || data == null)
            {
                if (lobbyCharacterModelView != null)
                    lobbyCharacterModelView.Hide();

                return false;
            }

            return lobbyCharacterModelView.Show(data, BattleCharacterModelView.ModelContext.Lobby);
        }

        private void FitInside(RectTransform rt, float sourceW, float sourceH, float maxW, float maxH)
        {
            if (rt == null || sourceW <= 0f || sourceH <= 0f)
                return;

            float ratio = Mathf.Min(maxW / sourceW, maxH / sourceH);
            ratio = Mathf.Min(ratio, 1f);

            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sourceW * ratio);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sourceH * ratio);
        }

        private void BindButtons()
        {
            if (selectCharacterButton != null)
            {
                selectCharacterButton.onClick.RemoveListener(OpenBattleLobby);
                selectCharacterButton.onClick.AddListener(OpenBattleLobby);
            }

            if (changeCharacterButton != null)
            {
                changeCharacterButton.onClick.RemoveListener(OpenBattleLobby);
                changeCharacterButton.onClick.AddListener(OpenBattleLobby);
            }

            ConfigureLobbyCharacterClickTarget();
        }

        private void ConfigureLobbyCharacterClickTarget()
        {
            if (!openSelectionByLobbyCharacterClick || lobbyCharacterImage == null)
                return;

            if (lobbyCharacterClickButton == null)
                lobbyCharacterClickButton = lobbyCharacterImage.GetComponent<Button>();

            if (lobbyCharacterClickButton == null)
                lobbyCharacterClickButton = lobbyCharacterImage.gameObject.AddComponent<Button>();

            lobbyCharacterClickButton.transition = Selectable.Transition.None;
            lobbyCharacterClickButton.targetGraphic = lobbyCharacterImage;
            lobbyCharacterClickButton.interactable = true;
            lobbyCharacterClickButton.onClick.RemoveListener(OpenBattleLobby);
            lobbyCharacterClickButton.onClick.AddListener(OpenBattleLobby);

            lobbyCharacterImage.raycastTarget = true;
        }

        private void ApplyCharacterButtonVisibility(bool hasCharacter)
        {
            if (selectCharacterButton != null)
                selectCharacterButton.gameObject.SetActive(!hasCharacter);

            if (changeCharacterButton == null)
                return;

            bool buttonOwnsLobbyImage = lobbyCharacterImage != null && lobbyCharacterImage.transform.IsChildOf(changeCharacterButton.transform);
            if (!buttonOwnsLobbyImage)
            {
                changeCharacterButton.gameObject.SetActive(!hideExplicitCharacterButtons && hasCharacter);
                return;
            }

            changeCharacterButton.gameObject.SetActive(true);
            changeCharacterButton.interactable = hasCharacter;
            changeCharacterButton.transition = Selectable.Transition.None;
            changeCharacterButton.onClick.RemoveListener(OpenBattleLobby);
            changeCharacterButton.onClick.AddListener(OpenBattleLobby);

            Graphic targetGraphic = changeCharacterButton.targetGraphic;
            if (targetGraphic != null)
            {
                Color color = targetGraphic.color;
                color.a = hideExplicitCharacterButtons ? 0f : 1f;
                targetGraphic.color = color;
                targetGraphic.raycastTarget = true;
            }

            TMP_Text[] labels = changeCharacterButton.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (selectedCharacterNameText != null && labels[i] == selectedCharacterNameText)
                    continue;

                labels[i].gameObject.SetActive(!hideExplicitCharacterButtons);
            }
        }

        private void Subscribe()
        {
            if (subscribed)
                return;

            if (!BattleCharacterSelectionService.HasInstance)
                return;

            BattleCharacterSelectionService.Instance.SelectedCharacterChanged += OnSelectedCharacterChanged;
            BattleCharacterSelectionService.Instance.SelectionStateChanged += OnSelectionStateChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (BattleCharacterSelectionService.HasInstance)
            {
                BattleCharacterSelectionService.Instance.SelectedCharacterChanged -= OnSelectedCharacterChanged;
                BattleCharacterSelectionService.Instance.SelectionStateChanged -= OnSelectionStateChanged;
            }

            subscribed = false;
        }

        private void OnSelectedCharacterChanged(string _)
        {
            Refresh();
        }

        private void OnSelectionStateChanged()
        {
            Refresh();
        }

        private void OnLanguageChanged(GameLanguage language)
        {
            Refresh();
        }

        private BattleCharacterDatabase.BattleCharacterData GetSelectedCharacterData()
        {
            if (!BattleCharacterSelectionService.HasInstance)
                return null;

            return BattleCharacterSelectionService.Instance.GetSelectedCharacter();
        }

        private Sprite GetLobbyDisplaySprite(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return fallbackLobbySprite;

            if (data.LobbySprite != null)
                return data.LobbySprite;

            if (useSelectSpriteIfLobbySpriteMissing && data.SelectSprite != null)
                return data.SelectSprite;

            return fallbackLobbySprite;
        }

        private string GetSafeName(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return string.Empty;

            return data.LocalizedDisplayName;
        }

        private static string GetLocalizedText(string key, string fallback)
        {
            string value = GameLocalization.Text(key);
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, System.StringComparison.Ordinal)
                ? fallback
                : value;
        }
    }
}
