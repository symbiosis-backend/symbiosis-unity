using System;
using System.Collections;
using MahjongGame.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class DuelChallengeLobbyUI : MonoBehaviour
    {
        private const string RootName = "DuelChallengeOverlay";
        private const string IndicatorName = "DuelIncomingIndicator";
        private const string DuelWindowSpritePath = "Mahjong/Sprites/BattleLobbyUI/DuelWindow";
        private const string DuelButtonSpritePath = "Mahjong/Sprites/BattleLobbyUI/DuelButton";
        private static readonly Vector2 FullscreenPanelSize = new Vector2(2140f, 980f);

        private string battleGameSceneName = "GameMahjongBattle";
        private GameObject root;
        private TMP_InputField nicknameInput;
        private TMP_InputField stakeInput;
        private TMP_Text nicknameLabelText;
        private TMP_Text stakeLabelText;
        private TMP_Text titleText;
        private TMP_Text statusText;
        private TMP_Text hintText;
        private Button sendButton;
        private Button closeButton;
        private Button indicatorButton;
        private TMP_Text indicatorText;
        private Coroutine outgoingRoutine;
        private DuelChallengeService.DuelChallengeInfo incomingChallenge;
        private bool launching;
        private static Sprite duelWindowSprite;
        private static Sprite duelButtonSprite;
        private static Sprite duelInputSprite;

        public static DuelChallengeLobbyUI Ensure(string battleSceneName)
        {
            DuelChallengeLobbyUI existing = FindAnyObjectByType<DuelChallengeLobbyUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Configure(battleSceneName);
                existing.SyncIncomingChallengeFromService();
                existing.EnsureIndicator();
                existing.RefreshIndicator();
                return existing;
            }

            GameObject host = new GameObject("DuelChallengeLobbyUI");
            DuelChallengeLobbyUI ui = host.AddComponent<DuelChallengeLobbyUI>();
            ui.Configure(battleSceneName);
            ui.SyncIncomingChallengeFromService();
            ui.EnsureIndicator();
            ui.RefreshIndicator();
            return ui;
        }

        public static DuelChallengeLobbyUI Show(string battleSceneName)
        {
            if (!BattleTotemRequirementUI.EnsureBattleReady())
                return null;

            DuelChallengeLobbyUI ui = Ensure(battleSceneName);
            ui.OpenCreateWindow();
            return ui;
        }

        private void Configure(string battleSceneName)
        {
            if (!string.IsNullOrWhiteSpace(battleSceneName))
                battleGameSceneName = battleSceneName;
        }

        private void OnEnable()
        {
            DuelChallengeService service = DuelChallengeService.EnsureInstance();
            service.IncomingChallengeChanged -= HandleIncomingChallengeChanged;
            service.IncomingChallengeChanged += HandleIncomingChallengeChanged;
            SyncIncomingChallengeFromService();
            RefreshIndicator();
            service.StartIncomingPolling();
        }

        private void OnDestroy()
        {
            if (DuelChallengeService.I != null)
                DuelChallengeService.I.IncomingChallengeChanged -= HandleIncomingChallengeChanged;
        }

        private void OpenCreateWindow()
        {
            BuildUi();
            BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.DuelChallenge);

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            launching = false;
            RefreshCreateTexts();
        }

        private void Close()
        {
            if (launching)
                return;

            if (outgoingRoutine != null)
            {
                StopCoroutine(outgoingRoutine);
                outgoingRoutine = null;
            }

            if (root != null)
                root.SetActive(false);

            BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.DuelChallenge);
        }

        private void SendChallenge()
        {
            if (outgoingRoutine != null || launching)
                return;

            if (!BattleTotemRequirementUI.EnsureBattleReady())
                return;

            string nickname = nicknameInput != null ? nicknameInput.text.Trim() : string.Empty;
            if (nickname.Length < 2)
            {
                SetStatus(GameLocalization.Text("battle.duel.enter_nickname"));
                return;
            }

            int stake = ParseStake();
            int maxStake = DuelChallengeService.EnsureInstance().GetLocalMaxStakeOzTile();
            if (stake <= 0 || stake > maxStake)
            {
                SetStatus(GameLocalization.Format("battle.duel.max_stake", maxStake));
                return;
            }

            if (CurrencyService.I != null && !CurrencyService.I.CanSpendOzTile(stake))
            {
                SetStatus(GameLocalization.Format("battle.duel.need_oztile", stake));
                return;
            }

            outgoingRoutine = StartCoroutine(SendChallengeRoutine(nickname, stake));
        }

        private IEnumerator SendChallengeRoutine(string nickname, int stake)
        {
            SetButtonsInteractable(false);
            SetStatus(GameLocalization.Text("battle.duel.sending"));

            DuelChallengeService.DuelChallengeInfo challenge = null;
            yield return DuelChallengeService.EnsureInstance().SendChallenge(nickname, stake, (success, message, info) =>
            {
                if (success)
                    challenge = info;
                else
                    SetStatus(LocalizeError(message));
            });

            if (challenge == null)
            {
                SetButtonsInteractable(true);
                outgoingRoutine = null;
                yield break;
            }

            while (!launching && challenge != null && string.Equals(challenge.status, "pending", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus(GameLocalization.Format("battle.duel.waiting_seconds", challenge.remainingSeconds));
                yield return new WaitForSecondsRealtime(0.5f);

                yield return DuelChallengeService.EnsureInstance().PollChallengeStatus(challenge.id, (success, message, info) =>
                {
                    if (success && info != null)
                        challenge = info;
                    else if (!string.IsNullOrWhiteSpace(message))
                        SetStatus(LocalizeError(message));
                });
            }

            outgoingRoutine = null;

            if (challenge != null && string.Equals(challenge.status, "accepted", StringComparison.OrdinalIgnoreCase))
            {
                OnlineRankedBattleNetwork.RankedMatchInfo match = challenge.match;
                if (match != null)
                    yield return LaunchDuel(match, challenge.stakeOzTile);
            }
            else
            {
                SetStatus(GameLocalization.Text("battle.duel.not_accepted"));
                SetButtonsInteractable(true);
            }
        }

        private void HandleIncomingChallengeChanged(DuelChallengeService.DuelChallengeInfo challenge)
        {
            incomingChallenge = challenge;
            EnsureIndicator();
            RefreshIndicator();
        }

        private void SyncIncomingChallengeFromService()
        {
            if (DuelChallengeService.I == null)
                return;

            incomingChallenge = DuelChallengeService.I.CurrentIncomingChallenge;
        }

        private void OpenIncomingWindow()
        {
            if (incomingChallenge == null)
                return;

            BuildUi();
            BattleLobbyUiCoordinator.OpenModal(BattleLobbyModalKind.DuelChallenge);

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            string name = incomingChallenge.challenger != null && !string.IsNullOrWhiteSpace(incomingChallenge.challenger.displayName)
                ? incomingChallenge.challenger.displayName
                : GameLocalization.Text("battle.common.player");
            name = AllianceIdentityFormatter.FormatName(name, incomingChallenge.challenger != null ? incomingChallenge.challenger.allianceTag : string.Empty);

            if (titleText != null)
                titleText.text = GameLocalization.Text("battle.duel.incoming_title");
            if (hintText != null)
                hintText.text = GameLocalization.Format("battle.duel.incoming_body", name, incomingChallenge.stakeOzTile);
            if (nicknameLabelText != null)
                nicknameLabelText.gameObject.SetActive(false);
            if (stakeLabelText != null)
                stakeLabelText.gameObject.SetActive(false);
            if (nicknameInput != null)
                nicknameInput.gameObject.SetActive(false);
            if (stakeInput != null)
                stakeInput.gameObject.SetActive(false);

            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(() => RespondIncoming(true));
            SetButtonText(sendButton, GameLocalization.Text("battle.duel.accept"));
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => RespondIncoming(false));
            SetButtonText(closeButton, GameLocalization.Text("battle.duel.decline"));
            SetStatus(GameLocalization.Format("battle.duel.waiting_seconds", incomingChallenge.remainingSeconds));
        }

        private void RespondIncoming(bool accepted)
        {
            if (incomingChallenge == null || launching)
                return;

            if (accepted && !BattleTotemRequirementUI.EnsureBattleReady())
                return;

            StartCoroutine(RespondIncomingRoutine(incomingChallenge.id, accepted, incomingChallenge.stakeOzTile));
        }

        private IEnumerator RespondIncomingRoutine(string challengeId, bool accepted, int stake)
        {
            SetButtonsInteractable(false);
            SetStatus(accepted ? GameLocalization.Text("battle.duel.accepting") : GameLocalization.Text("battle.duel.declining"));

            OnlineRankedBattleNetwork.RankedMatchInfo match = null;
            yield return DuelChallengeService.EnsureInstance().RespondToChallenge(challengeId, accepted, (success, message, challenge, matchInfo) =>
            {
                if (success)
                    match = matchInfo;
                else
                    SetStatus(LocalizeError(message));
            });

            if (accepted && match != null)
                yield return LaunchDuel(match, stake);
            else
                Close();
        }

        private IEnumerator LaunchDuel(OnlineRankedBattleNetwork.RankedMatchInfo match, int stake)
        {
            if (!BattleTotemRequirementUI.EnsureBattleReady())
                yield break;

            launching = true;

            if (!RankedBattleService.TryStartDuelMatch(stake, out string reason))
            {
                launching = false;
                SetStatus(LocalizeError(reason));
                SetButtonsInteractable(true);
                yield break;
            }

            OnlineRankedBattleNetwork.EnsureInstance().ActivateDuelMatch(match);
            MahjongBattleLobbySession.SetMode(MahjongBattleLobbyMode.RankedMatch);
            RankedBattleService.MarkPendingMatchStarted();

            MahjongBattleOpponentData opponent = BuildOpponent(match.opponent);
            MahjongSession.StartBattle(opponent, stake, Mathf.Max(1, match.seed), MahjongBattleSource.Duel);
            SetStatus(GameLocalization.Text("battle.random.starting"));

            yield return new WaitForSecondsRealtime(0.6f);

            if (root != null)
                root.SetActive(false);
            BattleLobbyUiCoordinator.CloseModal(BattleLobbyModalKind.DuelChallenge);
            SceneManager.LoadScene(battleGameSceneName);
        }

        private static MahjongBattleOpponentData BuildOpponent(OnlineRankedBattleNetwork.RankedOpponentInfo info)
        {
            return new MahjongBattleOpponentData
            {
                Id = info != null && !string.IsNullOrWhiteSpace(info.id) ? info.id : "duel_peer",
                DisplayName = info != null && !string.IsNullOrWhiteSpace(info.displayName) ? info.displayName : GameLocalization.Text("battle.duel.online_player"),
                AllianceTag = info != null ? info.allianceTag : string.Empty,
                AllianceLevel = info != null ? Mathf.Max(0, info.allianceLevel) : 0,
                RankTier = info != null && !string.IsNullOrWhiteSpace(info.rankTier) ? LocalizeRankTier(info.rankTier) : GameLocalization.Text("battle.rank.unranked"),
                RankPoints = info != null ? Mathf.Max(0, info.rankPoints) : 0,
                Level = info != null ? Mathf.Max(1, 1 + Mathf.Max(0, info.rankPoints) / 100) : 1,
                AvatarId = info != null ? Mathf.Max(0, info.avatarId) : 0,
                Gender = info != null ? MahjongBattleOpponentData.ParseGender(info.gender) : PlayerGender.NotSpecified,
                CharacterId = info != null && !string.IsNullOrWhiteSpace(info.characterId) ? info.characterId.Trim() : string.Empty,
                IsBot = false,
                Loadout = info?.loadout?.Clone()
            };
        }

        private void BuildUi()
        {
            if (root != null)
                return;

            Canvas canvas = BattleLobbyUiCoordinator.GetOrCreatePopupCanvas();
            root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = root.GetComponent<Image>();
            dim.color = Color.black;
            dim.raycastTarget = true;

            GameObject panel = new GameObject("DuelChallengePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = FullscreenPanelSize;
            FitPanelInsideCanvas(panelRect, canvas, 30f);
            ApplyDuelWindow(panel.GetComponent<Image>());

            titleText = CreateText(panel.transform, "Title", GameLocalization.Text("battle.duel.title"), new Vector2(0f, 336f), new Vector2(1320f, 90f), 66f);
            hintText = CreateText(panel.transform, "Hint", string.Empty, new Vector2(0f, 256f), new Vector2(1320f, 64f), 38f);
            nicknameLabelText = CreateFormLabel(panel.transform, "NicknameLabel", GameLocalization.Text("battle.duel.nickname"), new Vector2(-480f, 134f));
            nicknameInput = CreateInput(panel.transform, "NicknameInput", GameLocalization.Text("battle.duel.nickname"), new Vector2(-480f, 48f), new Vector2(880f, 112f));
            stakeLabelText = CreateFormLabel(panel.transform, "StakeLabel", GameLocalization.Text("battle.duel.stake"), new Vector2(480f, 134f));
            stakeInput = CreateInput(panel.transform, "StakeInput", GameLocalization.Text("battle.duel.stake"), new Vector2(480f, 48f), new Vector2(880f, 112f));
            stakeInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            statusText = CreateText(panel.transform, "Status", string.Empty, new Vector2(0f, -130f), new Vector2(1580f, 72f), 40f);
            sendButton = CreateButton(panel.transform, "SendButton", GameLocalization.Text("battle.duel.send"), new Vector2(-330f, -360f), new Vector2(560f, 100f));
            closeButton = CreateButton(panel.transform, "CloseButton", GameLocalization.Text("battle.common.cancel"), new Vector2(330f, -360f), new Vector2(560f, 100f));

            sendButton.onClick.AddListener(SendChallenge);
            closeButton.onClick.AddListener(Close);
            root.SetActive(false);
        }

        private void RefreshCreateTexts()
        {
            int maxStake = DuelChallengeService.EnsureInstance().GetLocalMaxStakeOzTile();
            if (titleText != null)
                titleText.text = GameLocalization.Text("battle.duel.title");
            if (hintText != null)
                hintText.text = GameLocalization.Format("battle.duel.max_stake", maxStake);
            if (nicknameLabelText != null)
            {
                nicknameLabelText.gameObject.SetActive(true);
                nicknameLabelText.text = GameLocalization.Text("battle.duel.nickname");
            }
            if (stakeLabelText != null)
            {
                stakeLabelText.gameObject.SetActive(true);
                stakeLabelText.text = GameLocalization.Text("battle.duel.stake");
            }
            if (nicknameInput != null)
            {
                nicknameInput.gameObject.SetActive(true);
                if (nicknameInput.placeholder is TMP_Text placeholder)
                    placeholder.text = GameLocalization.Text("battle.duel.nickname");
            }
            if (stakeInput != null)
            {
                stakeInput.gameObject.SetActive(true);
                stakeInput.text = Mathf.Min(100, maxStake).ToString();
                if (stakeInput.placeholder is TMP_Text placeholder)
                    placeholder.text = GameLocalization.Text("battle.duel.stake");
            }
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(SendChallenge);
            SetButtonText(sendButton, GameLocalization.Text("battle.duel.send"));
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
            SetButtonText(closeButton, GameLocalization.Text("battle.common.cancel"));
            SetButtonsInteractable(true);
            SetStatus(string.Empty);
        }

        private void EnsureIndicator()
        {
            if (indicatorButton != null)
                return;

            Canvas canvas = BattleLobbyUiCoordinator.GetOrCreatePopupCanvas();
            if (canvas == null)
                return;

            indicatorButton = CreateButton(canvas.transform, IndicatorName, GameLocalization.Text("battle.duel.incoming_button"), new Vector2(0f, 390f), new Vector2(620f, 92f));
            indicatorText = indicatorButton.GetComponentInChildren<TMP_Text>(true);
            indicatorButton.onClick.AddListener(OpenIncomingWindow);
            RefreshIndicator();
        }

        private void RefreshIndicator()
        {
            if (indicatorButton == null)
                return;

            bool visible = incomingChallenge != null && string.Equals(incomingChallenge.status, "pending", StringComparison.OrdinalIgnoreCase);
            indicatorButton.gameObject.SetActive(visible);
            if (!visible)
                return;

            string name = incomingChallenge.challenger != null && !string.IsNullOrWhiteSpace(incomingChallenge.challenger.displayName)
                ? incomingChallenge.challenger.displayName
                : GameLocalization.Text("battle.common.player");
            name = AllianceIdentityFormatter.FormatName(name, incomingChallenge.challenger != null ? incomingChallenge.challenger.allianceTag : string.Empty);
            if (indicatorText != null)
                indicatorText.text = GameLocalization.Format("battle.duel.incoming_button_from", name, incomingChallenge.stakeOzTile);
        }

        private int ParseStake()
        {
            if (stakeInput == null)
                return 0;

            return int.TryParse(stakeInput.text, out int value) ? Mathf.Max(0, value) : 0;
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value ?? string.Empty;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (sendButton != null)
                sendButton.interactable = interactable;
            if (closeButton != null)
                closeButton.interactable = interactable;
        }

        private static void ApplyDuelWindow(Image image)
        {
            BattlePopupStyle.ApplyWindow(image);
        }

        private static void ApplyDuelButton(Button button)
        {
            BattlePopupStyle.ApplyButton(button);
        }

        private static void FitPanelInsideCanvas(RectTransform panel, Canvas canvas, float padding)
        {
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (panel == null || canvasRect == null)
                return;

            Vector2 available = canvasRect.rect.size - Vector2.one * Mathf.Max(0f, padding * 2f);
            if (available.x <= 1f || available.y <= 1f)
                return;

            Vector2 size = panel.sizeDelta;
            float scale = Mathf.Min(1f, available.x / Mathf.Max(1f, size.x), available.y / Mathf.Max(1f, size.y));
            panel.localScale = Vector3.one * scale;
        }

        private static Sprite LoadDuelWindowSprite()
        {
            if (duelWindowSprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(DuelWindowSpritePath);
                duelWindowSprite = texture != null
                    ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect)
                    : Resources.Load<Sprite>(DuelWindowSpritePath);
            }

            return duelWindowSprite;
        }

        private static Sprite LoadDuelButtonSprite()
        {
            if (duelButtonSprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(DuelButtonSpritePath);
                duelButtonSprite = texture != null
                    ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect)
                    : Resources.Load<Sprite>(DuelButtonSpritePath);
            }

            return duelButtonSprite;
        }

        private static TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(16f, fontSize * 0.62f);
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            BattlePopupStyle.ApplyText(text, true);
            MainLobbyButtonStyle.ApplyFont(text);
            return text;
        }

        private static TMP_Text CreateFormLabel(Transform parent, string objectName, string value, Vector2 position)
        {
            TMP_Text label = CreateText(parent, objectName, value, position, new Vector2(880f, 54f), 38f);
            label.alignment = TextAlignmentOptions.Left;
            label.color = new Color(1f, 0.82f, 0.42f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static TMP_InputField CreateInput(Transform parent, string objectName, string placeholder, Vector2 position, Vector2 size)
        {
            GameObject inputObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = inputObject.GetComponent<Image>();
            image.sprite = GetDuelInputSprite();
            image.type = image.sprite != null && image.sprite.border.sqrMagnitude > 0.01f ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;

            TMP_Text text = CreateText(inputObject.transform, "Text", string.Empty, Vector2.zero, size - new Vector2(128f, 26f), 46f);
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(18f, 8f, 18f, 0f);
            text.color = Color.white;
            TMP_Text placeholderText = CreateText(inputObject.transform, "Placeholder", placeholder, Vector2.zero, size - new Vector2(128f, 26f), 38f);
            placeholderText.alignment = TextAlignmentOptions.Left;
            placeholderText.margin = new Vector4(18f, 8f, 18f, 0f);
            placeholderText.color = new Color(1f, 0.92f, 0.72f, 0.72f);

            TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.characterLimit = 32;
            input.caretColor = new Color(1f, 0.82f, 0.42f, 1f);
            input.selectionColor = new Color(1f, 0.72f, 0.22f, 0.34f);
            return input;
        }

        private static Sprite GetDuelInputSprite()
        {
            if (duelInputSprite != null)
                return duelInputSprite;

            const int width = 256;
            const int height = 64;
            const float radius = 27f;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "DuelInputRoundedRuntime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = new Color(0.23f, 0.11f, 0.035f, 0.92f);
            Color inner = new Color(0.52f, 0.30f, 0.07f, 0.96f);
            Color gold = new Color(1f, 0.66f, 0.08f, 1f);
            Color darkGold = new Color(0.42f, 0.20f, 0.02f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = x < radius ? radius - x : x > width - radius ? x - (width - radius) : 0f;
                    float dy = y < radius ? radius - y : y > height - radius ? y - (height - radius) : 0f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    float edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    bool border = edge < 4f || Mathf.Abs(distance - radius) < 4f;
                    bool highlight = y > height - 11 || y < 7;
                    texture.SetPixel(x, y, border ? (highlight ? gold : darkGold) : (highlight ? inner : fill));
                }
            }

            texture.Apply(false, true);
            duelInputSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(36f, 18f, 36f, 18f));
            duelInputSprite.name = "DuelInputRoundedRuntime";
            return duelInputSprite;
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            TMP_Text labelText = CreateText(buttonObject.transform, "Label", label, Vector2.zero, size - new Vector2(84f, 20f), 42f);
            labelText.margin = new Vector4(30f, 8f, 30f, 10f);
            labelText.overflowMode = TextOverflowModes.Truncate;
            ApplyDuelButton(button);
            BattlePopupStyle.ApplyButtonLabel(button, 42f);
            MainLobbyButtonStyle.ApplyFont(labelText);
            return button;
        }

        private static void SetButtonText(Button button, string value)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null)
                text.text = value;
        }

        private static string LocalizeError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            if (message.IndexOf("Stake exceeds", StringComparison.OrdinalIgnoreCase) >= 0)
                return GameLocalization.Text("battle.duel.stake_exceeds");
            if (message.IndexOf("Player not found", StringComparison.OrdinalIgnoreCase) >= 0)
                return GameLocalization.Text("battle.duel.player_not_found");
            if (message.IndexOf("Need", StringComparison.OrdinalIgnoreCase) >= 0 && message.IndexOf("OzTile", StringComparison.OrdinalIgnoreCase) >= 0)
                return message;

            return message;
        }

        private static string LocalizeRankTier(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return GameLocalization.Text("battle.rank.unranked");

            string value = tier.Trim().ToLowerInvariant();
            if (value.Contains("master")) return GameLocalization.Text("battle.rank.master");
            if (value.Contains("platinum")) return GameLocalization.Text("battle.rank.platinum");
            if (value.Contains("gold")) return GameLocalization.Text("battle.rank.gold");
            if (value.Contains("silver")) return GameLocalization.Text("battle.rank.silver");
            if (value.Contains("bronze")) return GameLocalization.Text("battle.rank.bronze");
            if (value.Contains("unranked")) return GameLocalization.Text("battle.rank.unranked");
            return tier.Trim();
        }
    }
}
