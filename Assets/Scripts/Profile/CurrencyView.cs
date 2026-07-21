using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class CurrencyView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI altinText;
        [SerializeField] private TextMeshProUGUI ozTileText;
        [SerializeField] private TextMeshProUGUI ametistText;

        [Header("Format")]
        [SerializeField] private string altinFormat = "{0}";
        [SerializeField] private string ozTileFormat = "{0}";
        [SerializeField] private string ametistFormat = "{0}";

        [Header("OzTile")]
        [SerializeField] private bool showOzTile = true;
        [SerializeField] private bool restrictOzTileToScenes = true;
        [SerializeField] private string[] ozTileVisibleSceneNames = { "LobbyMahjong", "LobbyMahjongBattle" };

        [Header("Mahjong Lobby Player Info")]
        [SerializeField] private bool usePlayerInfoInMahjongLobbies = true;
        [SerializeField] private string[] playerInfoSceneNames = { "LobbyMahjong", "LobbyMahjongBattle" };
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";
        [SerializeField] private string levelFormat = "Level {0}";
        [SerializeField] private string expFormat = "EXP {0}/{1}";
        [SerializeField] private string energyFormat = "Energy {0}/{1}";
        [SerializeField] private string infiniteEnergyText = "Energy INF";
        [SerializeField] private string ozTileLobbyFormat = "Oz Tiles {0}";
        [SerializeField] private Sprite ozTileIconSprite;
        [SerializeField] private Vector2 playerInfoOffsetMin = new Vector2(86f, 44f);
        [SerializeField] private Vector2 playerInfoOffsetMax = new Vector2(-86f, -42f);

        [Header("Layout")]
        [SerializeField] private Vector2 altinPositionWhenOzTileVisible = new Vector2(-240f, 0f);
        [SerializeField] private Vector2 altinPositionWhenOzTileHidden = new Vector2(-155f, 0f);
        [SerializeField] private Vector2 ametistPositionWhenOzTileVisible = new Vector2(240f, 0f);
        [SerializeField] private Vector2 ametistPositionWhenOzTileHidden = new Vector2(155f, 0f);

        private RectTransform altinRoot;
        private RectTransform ozTileRoot;
        private RectTransform ametistRoot;
        private RectTransform playerInfoRoot;
        private TextMeshProUGUI playerLevelText;
        private TextMeshProUGUI playerExpText;
        private TextMeshProUGUI playerEnergyText;
        private TextMeshProUGUI playerOzTileText;
        private Image playerOzTileIcon;
        private Image currencyPanelImage;
        private Sprite cachedOzTileIconSprite;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            CurrencyService.CurrencyChanged += Refresh;
            ProfileService.ProfileChanged += Refresh;
            EnergyService.EnergyChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            CurrencyService.CurrencyChanged -= Refresh;
            ProfileService.ProfileChanged -= Refresh;
            EnergyService.EnergyChanged -= Refresh;
        }

        private void HandleActiveSceneChanged(Scene previous, Scene current)
        {
            Refresh();
        }

        public void Refresh()
        {
            AutoResolveUiLinks();

            if (ShouldShowPlayerInfo())
            {
                CacheOzTileIconSprite();
                SetCurrencyBlocksVisible(false);
                EnsurePlayerInfoUi();
                RefreshPlayerInfo();
                return;
            }

            SetPlayerInfoVisible(false);
            SetCurrencyBlocksVisible(true);

            bool tileVisible = ShouldShowOzTile();
            ApplyOzTileVisibilityAndLayout(tileVisible);

            int altin = CurrencyService.I != null ? CurrencyService.I.GetOzAltin() : 0;
            int ozTile = CurrencyService.I != null ? CurrencyService.I.GetOzTile() : 0;
            int ametist = CurrencyService.I != null ? CurrencyService.I.GetOzAmetist() : 0;

            if (altinText != null)
                altinText.text = string.Format(altinFormat, altin);

            if (ozTileText != null)
                ozTileText.text = string.Format(ozTileFormat, ozTile);

            if (ametistText != null)
                ametistText.text = string.Format(ametistFormat, ametist);
        }

        private void AutoResolveUiLinks()
        {
            if (currencyPanelImage == null)
                currencyPanelImage = GetComponent<Image>();

            if (currencyPanelImage != null)
                MainLobbyButtonStyle.ApplyMainFrame(currencyPanelImage);

            if (altinText == null)
                altinText = FindText("AltinBlock/Text");

            if (ozTileText == null)
                ozTileText = FindText("TileBlock/Text");

            if (ametistText == null)
                ametistText = FindText("AmetistBlock/Text");

            if (altinRoot == null)
                altinRoot = ResolveRoot(altinText, "AltinBlock");

            if (ozTileRoot == null)
                ozTileRoot = ResolveRoot(ozTileText, "TileBlock");

            if (ametistRoot == null)
                ametistRoot = ResolveRoot(ametistText, "AmetistBlock");
        }

        private void ApplyOzTileVisibilityAndLayout(bool tileVisible)
        {
            if (ozTileRoot != null)
                ozTileRoot.gameObject.SetActive(tileVisible);

            if (altinRoot != null)
                altinRoot.anchoredPosition = tileVisible ? altinPositionWhenOzTileVisible : altinPositionWhenOzTileHidden;

            if (ametistRoot != null)
                ametistRoot.anchoredPosition = tileVisible ? ametistPositionWhenOzTileVisible : ametistPositionWhenOzTileHidden;
        }

        private void SetCurrencyBlocksVisible(bool visible)
        {
            if (altinRoot != null)
                altinRoot.gameObject.SetActive(visible);

            if (ozTileRoot != null)
                ozTileRoot.gameObject.SetActive(visible);

            if (ametistRoot != null)
                ametistRoot.gameObject.SetActive(visible);
        }

        private bool ShouldShowPlayerInfo()
        {
            if (!usePlayerInfoInMahjongLobbies)
                return false;

            if (playerInfoSceneNames == null || playerInfoSceneNames.Length == 0)
                return false;

            string sceneName = SceneManager.GetActiveScene().name;
            for (int i = 0; i < playerInfoSceneNames.Length; i++)
            {
                string candidate = playerInfoSceneNames[i];
                if (!string.IsNullOrWhiteSpace(candidate) &&
                    string.Equals(sceneName, candidate.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBattleLobbyScene()
        {
            return string.Equals(
                SceneManager.GetActiveScene().name,
                battleLobbySceneName,
                System.StringComparison.OrdinalIgnoreCase);
        }

        private void EnsurePlayerInfoUi()
        {
            if (playerInfoRoot == null)
            {
                Transform existing = transform.Find("PlayerInfoRoot");
                playerInfoRoot = existing as RectTransform;
            }

            if (playerInfoRoot == null)
            {
                GameObject rootObject = new GameObject("PlayerInfoRoot", typeof(RectTransform));
                rootObject.transform.SetParent(transform, false);
                playerInfoRoot = rootObject.GetComponent<RectTransform>();
            }

            playerInfoRoot.anchorMin = Vector2.zero;
            playerInfoRoot.anchorMax = Vector2.one;
            playerInfoRoot.offsetMin = playerInfoOffsetMin;
            playerInfoRoot.offsetMax = playerInfoOffsetMax;
            playerInfoRoot.pivot = new Vector2(0.5f, 0.5f);
            playerInfoRoot.localScale = Vector3.one;
            playerInfoRoot.SetAsLastSibling();

            playerLevelText = EnsurePlayerInfoText(playerInfoRoot, playerLevelText, "PlayerLevelText", 0);
            playerExpText = EnsurePlayerInfoText(playerInfoRoot, playerExpText, "PlayerExpText", 1);
            playerEnergyText = EnsurePlayerInfoText(playerInfoRoot, playerEnergyText, "PlayerEnergyText", 2);
            playerOzTileText = EnsurePlayerInfoText(playerInfoRoot, playerOzTileText, "PlayerOzTileText", 3);
            playerOzTileIcon = EnsurePlayerOzTileIcon(playerInfoRoot, playerOzTileIcon);

            if (playerOzTileText != null)
            {
                RectTransform ozTileTextRect = playerOzTileText.rectTransform;
                ozTileTextRect.offsetMin = new Vector2(38f, 0f);
                ozTileTextRect.offsetMax = new Vector2(-6f, 0f);
                playerOzTileText.alignment = TextAlignmentOptions.Left;
            }

            SetPlayerInfoVisible(true);
        }

        private TextMeshProUGUI EnsurePlayerInfoText(RectTransform parent, TextMeshProUGUI current, string objectName, int columnIndex)
        {
            if (current == null)
            {
                Transform existing = parent.Find(objectName);
                current = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            }

            if (current == null)
            {
                GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(parent, false);
                current = textObject.GetComponent<TextMeshProUGUI>();
                current.raycastTarget = false;
                current.color = Color.white;
                current.alignment = TextAlignmentOptions.Center;
                current.enableAutoSizing = true;
                current.fontSize = 22f;
                current.fontSizeMin = 12f;
                current.fontSizeMax = 22f;
                current.textWrappingMode = TextWrappingModes.NoWrap;
                current.overflowMode = TextOverflowModes.Truncate;
            }

            RectTransform rect = current.rectTransform;
            float minX = Mathf.Clamp01(columnIndex * 0.25f);
            float maxX = Mathf.Clamp01((columnIndex + 1) * 0.25f);
            rect.anchorMin = new Vector2(minX, 0f);
            rect.anchorMax = new Vector2(maxX, 1f);
            rect.offsetMin = new Vector2(6f, 0f);
            rect.offsetMax = new Vector2(-6f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            return current;
        }

        private Image EnsurePlayerOzTileIcon(RectTransform parent, Image current)
        {
            if (current == null)
            {
                Transform existing = parent.Find("PlayerOzTileIcon");
                current = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (current == null)
            {
                GameObject iconObject = new GameObject("PlayerOzTileIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(parent, false);
                current = iconObject.GetComponent<Image>();
                current.raycastTarget = false;
                current.preserveAspect = true;
            }

            RectTransform rect = current.rectTransform;
            rect.anchorMin = new Vector2(0.75f, 0.5f);
            rect.anchorMax = new Vector2(0.75f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(22f, 0f);
            rect.sizeDelta = new Vector2(28f, 28f);

            if (cachedOzTileIconSprite != null)
                current.sprite = cachedOzTileIconSprite;

            current.enabled = current.sprite != null;
            current.color = Color.white;
            current.transform.SetAsLastSibling();
            return current;
        }

        private void CacheOzTileIconSprite()
        {
            if (cachedOzTileIconSprite != null)
                return;

            if (ozTileIconSprite != null)
            {
                cachedOzTileIconSprite = ozTileIconSprite;
                return;
            }

            Image icon = ResolveIconImage(ozTileRoot);
            if (icon != null && icon.sprite != null)
                cachedOzTileIconSprite = icon.sprite;
        }

        private void RefreshPlayerInfo()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null)
                profile.EnsureData();

            int level = 1;
            int exp = 0;
            int nextExp = 100;

            if (profile != null && IsBattleLobbyScene())
            {
                MahjongBattleData battle = profile.Mahjong != null ? profile.Mahjong.Battle : null;
                level = battle != null ? Mathf.Max(1, battle.Level) : 1;
                exp = battle != null ? Mathf.Max(0, battle.Experience) : 0;
                nextExp = battle != null ? Mathf.Max(1, battle.GetExperienceRequiredForNextLevel()) : 100;
            }
            else if (profile != null)
            {
                level = Mathf.Max(1, profile.AccountLevel);
                exp = Mathf.Max(0, profile.AccountExp);
                nextExp = Mathf.Max(1, profile.GetAccountExpRequiredForNextLevel());
            }

            int ozTile = CurrencyService.I != null ? CurrencyService.I.GetOzTile() : 0;

            if (playerLevelText != null)
                playerLevelText.text = string.Format(levelFormat, level);

            if (playerExpText != null)
                playerExpText.text = string.Format(expFormat, exp, nextExp);

            if (playerEnergyText != null)
                playerEnergyText.text = EnergyService.HasInfiniteEnergy()
                    ? infiniteEnergyText
                    : string.Format(energyFormat, EnergyService.CurrentEnergy, EnergyService.CurrentMaxEnergy);

            if (playerOzTileText != null)
                playerOzTileText.text = string.Format(ozTileLobbyFormat, ozTile);

            if (playerOzTileIcon != null)
                playerOzTileIcon.enabled = playerOzTileIcon.sprite != null;
        }

        private void SetPlayerInfoVisible(bool visible)
        {
            if (playerInfoRoot != null)
                playerInfoRoot.gameObject.SetActive(visible);
        }

        private bool ShouldShowOzTile()
        {
            if (!showOzTile)
                return false;

            if (ozTileText == null && ozTileRoot == null)
                return false;

            if (!restrictOzTileToScenes || ozTileVisibleSceneNames == null || ozTileVisibleSceneNames.Length == 0)
                return true;

            string sceneName = SceneManager.GetActiveScene().name;
            for (int i = 0; i < ozTileVisibleSceneNames.Length; i++)
            {
                string candidate = ozTileVisibleSceneNames[i];
                if (!string.IsNullOrWhiteSpace(candidate) &&
                    string.Equals(sceneName, candidate.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private TextMeshProUGUI FindText(string relativePath)
        {
            Transform target = transform.Find(relativePath);
            return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
        }

        private RectTransform ResolveRoot(TextMeshProUGUI text, string fallbackObjectName)
        {
            RectTransform root = text != null ? text.transform.parent as RectTransform : null;
            if (root != null)
                return root;

            Transform target = transform.Find(fallbackObjectName);
            return target as RectTransform;
        }

        private static Image ResolveIconImage(RectTransform root)
        {
            if (root == null)
                return null;

            Transform iconTransform = root.Find("Icon");
            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();
                if (icon != null)
                    return icon;
            }

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.sprite != null)
                    return image;
            }

            return null;
        }
    }
}
