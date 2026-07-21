using System;
using System.Collections;
using System.Text;
using MahjongGame.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class AdminPanelUI : MonoBehaviour
    {
        private const string OwnerEmail = "mykhaylov.artem@gmail.com";
        private const string KeySessionToken = "symbiosis_server_session_token";

        private Canvas canvas;
        private Button toggleButton;
        private GameObject panelRoot;
        private TMP_InputField searchInput;
        private TMP_Text statusText;
        private TMP_Text profileText;
        private Transform resultsRoot;
        private TMP_InputField currencyAmountInput;
        private TMP_InputField currencyIdInput;
        private TMP_InputField stoneIdInput;
        private TMP_InputField stoneRarityInput;
        private TMP_InputField durationInput;
        private TMP_InputField reasonInput;
        private AdminPlayerDto selectedPlayer;

        public static void CreateInScene()
        {
            GameObject root = new GameObject("AdminPanelUI", typeof(RectTransform));
            AdminPanelUI ui = root.AddComponent<AdminPanelUI>();
            ui.Build();
        }

        public static bool IsOwnerProfile()
        {
            if (ProfileService.I == null)
                ProfileRuntimeBootstrap.EnsureServices();

            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null && profile.IsDeveloper)
                return true;

            string email = ProfileService.I != null ? ProfileService.I.CurrentAccountEmail : string.Empty;
            return string.Equals(email, OwnerEmail, StringComparison.OrdinalIgnoreCase);
        }

        public void RefreshOwnerVisibility()
        {
            bool visible = IsOwnerProfile();
            if (toggleButton != null)
                toggleButton.gameObject.SetActive(visible);
            if (panelRoot != null && !visible)
                ClosePanel();
        }

        private void OnDisable()
        {
            MainHubStateController.NotifyMainWindowClosed();
        }

        private void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 420;
            MainLobbyUiCoordinator.ConfigureOverlayScaler(gameObject.AddComponent<CanvasScaler>());
            gameObject.AddComponent<GraphicRaycaster>();

            toggleButton = CreateButton(transform, "ADMIN", new Color32(86, 28, 132, 235), TogglePanel);
            RectTransform toggleRect = toggleButton.transform as RectTransform;
            toggleRect.anchorMin = new Vector2(1f, 1f);
            toggleRect.anchorMax = new Vector2(1f, 1f);
            toggleRect.pivot = new Vector2(1f, 1f);
            toggleRect.anchoredPosition = new Vector2(-28f, -28f);
            toggleRect.sizeDelta = new Vector2(270f, 86f);

            panelRoot = CreatePanel(transform, "AdminPanelRoot", new Color32(8, 18, 33, 248));
            RectTransform panelRect = panelRoot.transform as RectTransform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRoot.SetActive(false);

            BuildPanelContent(panelRoot.transform);
            RefreshOwnerVisibility();
        }

        private void TogglePanel()
        {
            if (!IsOwnerProfile())
                return;

            bool show = !panelRoot.activeSelf;
            if (show && !MainHubStateController.CanOpenMainWindow("AdminPanel"))
                return;

            if (show)
            {
                MainLobbyUiCoordinator.SetRightStackSuppressed(true);
                SettingsMenuUI.SetMainSettingsButtonSuppressed(true);
            }
            panelRoot.SetActive(show);
            if (show)
            {
                SetStatus("Admin panel готов.");
                MainGameLaunchBootstrap.RefreshVisibilityNow();
            }
            else
            {
                MainHubStateController.NotifyMainWindowClosed();
            }
        }

        private void ClosePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            MainHubStateController.NotifyMainWindowClosed();
        }

        private void BuildPanelContent(Transform parent)
        {
            TMP_Text title = CreateText(parent, "ADMIN PANEL", 52, FontStyles.Bold, TextAlignmentOptions.Left);
            RectTransform titleRect = title.transform as RectTransform;
            Anchor(titleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(56f, -48f), new Vector2(-190f, -116f));

            Button close = CreateButton(parent, "X", new Color32(36, 62, 96, 235), ClosePanel);
            Anchor(close.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-132f, -42f), new Vector2(-44f, -130f));

            GameObject left = CreatePanel(parent, "AdminLeft", new Color32(6, 14, 26, 230));
            Anchor(left.transform as RectTransform, new Vector2(0f, 0f), new Vector2(0.38f, 1f), new Vector2(54f, 58f), new Vector2(-20f, -150f));

            GameObject right = CreatePanel(parent, "AdminRight", new Color32(12, 27, 48, 220));
            Anchor(right.transform as RectTransform, new Vector2(0.38f, 0f), new Vector2(1f, 1f), new Vector2(18f, 58f), new Vector2(-54f, -150f));

            TMP_Text searchLabel = CreateText(left.transform, "Поиск игрока по Nick / ID", 30, FontStyles.Bold, TextAlignmentOptions.Left);
            Anchor(searchLabel.transform as RectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -26f), new Vector2(-28f, -76f));

            searchInput = CreateInput(left.transform, "Nick или ID");
            Anchor(searchInput.transform as RectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -96f), new Vector2(-420f, -172f));

            Button searchButton = CreateButton(left.transform, "Найти", new Color32(25, 88, 144, 235), Search);
            Anchor(searchButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-400f, -96f), new Vector2(-220f, -172f));

            Button inboxButton = CreateButton(left.transform, "Письма", new Color32(86, 62, 128, 235), LoadInbox);
            Anchor(inboxButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-200f, -96f), new Vector2(-28f, -172f));

            ScrollRect resultsScroll = CreateScroll(left.transform, "ResultsScroll", out resultsRoot);
            Anchor(resultsScroll.transform as RectTransform, new Vector2(0f, 0.18f), new Vector2(1f, 1f), new Vector2(28f, 28f), new Vector2(-28f, -198f));

            statusText = CreateText(left.transform, string.Empty, 24, FontStyles.Normal, TextAlignmentOptions.Left);
            statusText.color = new Color32(255, 220, 120, 255);
            Anchor(statusText.transform as RectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.18f), new Vector2(28f, 22f), new Vector2(-28f, -12f));

            profileText = CreateText(right.transform, "Выбери игрока слева.", 30, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Anchor(profileText.transform as RectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 1f), new Vector2(34f, 28f), new Vector2(-34f, -30f));

            BuildGrantControls(right.transform);
            BuildModerationControls(right.transform);
        }

        private void BuildGrantControls(Transform parent)
        {
            TMP_Text title = CreateText(parent, "Подарок игроку", 32, FontStyles.Bold, TextAlignmentOptions.Left);
            Anchor(title.transform as RectTransform, new Vector2(0f, 0.34f), new Vector2(1f, 0.48f), new Vector2(34f, 0f), new Vector2(-34f, -20f));

            currencyIdInput = CreateInput(parent, "oz_gold / oz_amethyst / oz_tile");
            Anchor(currencyIdInput.transform as RectTransform, new Vector2(0f, 0.34f), new Vector2(0f, 0.34f), new Vector2(34f, -94f), new Vector2(374f, -22f));

            currencyAmountInput = CreateInput(parent, "Amount");
            currencyAmountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            Anchor(currencyAmountInput.transform as RectTransform, new Vector2(0f, 0.34f), new Vector2(0f, 0.34f), new Vector2(392f, -94f), new Vector2(586f, -22f));

            Button sendCurrency = CreateButton(parent, "Отправить валюту", new Color32(36, 112, 86, 235), SendCurrency);
            Anchor(sendCurrency.transform as RectTransform, new Vector2(0f, 0.34f), new Vector2(0f, 0.34f), new Vector2(606f, -94f), new Vector2(920f, -22f));

            stoneIdInput = CreateInput(parent, "epic_stone_gift");
            Anchor(stoneIdInput.transform as RectTransform, new Vector2(0f, 0.34f), new Vector2(0f, 0.34f), new Vector2(34f, -188f), new Vector2(354f, -116f));

            stoneRarityInput = CreateInput(parent, "epic / rare / legendary");
            Anchor(stoneRarityInput.transform as RectTransform, new Vector2(0f, 0.34f), new Vector2(0f, 0.34f), new Vector2(372f, -188f), new Vector2(642f, -116f));

            Button sendStone = CreateButton(parent, "Отправить камень", new Color32(98, 56, 156, 235), SendStone);
            Anchor(sendStone.transform as RectTransform, new Vector2(0f, 0.34f), new Vector2(0f, 0.34f), new Vector2(662f, -188f), new Vector2(960f, -116f));
        }

        private void BuildModerationControls(Transform parent)
        {
            TMP_Text title = CreateText(parent, "Модерация", 32, FontStyles.Bold, TextAlignmentOptions.Left);
            Anchor(title.transform as RectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.24f), new Vector2(34f, 180f), new Vector2(-34f, 124f));

            durationInput = CreateInput(parent, "60 мин");
            durationInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            Anchor(durationInput.transform as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 94f), new Vector2(208f, 166f));

            reasonInput = CreateInput(parent, "Причина");
            Anchor(reasonInput.transform as RectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(228f, 94f), new Vector2(-34f, 166f));

            Button mute = CreateButton(parent, "Mute", new Color32(154, 106, 28, 235), () => Moderate("mute"));
            Anchor(mute.transform as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 18f), new Vector2(220f, 82f));

            Button unmute = CreateButton(parent, "Unmute", new Color32(52, 76, 112, 235), () => Moderate("unmute"));
            Anchor(unmute.transform as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(238f, 18f), new Vector2(424f, 82f));

            Button ban = CreateButton(parent, "Ban", new Color32(150, 48, 48, 235), () => Moderate("ban"));
            Anchor(ban.transform as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(442f, 18f), new Vector2(628f, 82f));

            Button unban = CreateButton(parent, "Unban", new Color32(52, 76, 112, 235), () => Moderate("unban"));
            Anchor(unban.transform as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(646f, 18f), new Vector2(832f, 82f));
        }

        private void Search()
        {
            string query = searchInput != null ? searchInput.text.Trim() : string.Empty;
            if (query.Length < 2)
            {
                SetStatus("Введи минимум 2 символа.");
                return;
            }

            StartCoroutine(GetJson<AdminSearchResponse>("/admin/players/search?query=" + UnityWebRequest.EscapeURL(query), response =>
            {
                ClearResults();
                if (response == null || !response.success)
                {
                    SetStatus(response != null ? response.error : "Ошибка поиска.");
                    return;
                }

                AdminPlayerDto[] players = response.players ?? Array.Empty<AdminPlayerDto>();
                for (int i = 0; i < players.Length; i++)
                {
                    AdminPlayerDto player = players[i];
                    Button row = CreateButton(resultsRoot, $"{player.nickname}  ID:{player.id}", new Color32(18, 45, 76, 235), () => SelectPlayer(player));
                    RectTransform rect = row.transform as RectTransform;
                    rect.sizeDelta = new Vector2(0f, 84f);
                    LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
                    layout.preferredHeight = 84f;
                }

                SetStatus(players.Length == 0 ? "Игрок не найден." : $"Найдено: {players.Length}");
            }));
        }

        private void LoadInbox()
        {
            StartCoroutine(GetJson<AdminInboxResponse>("/admin/mail/inbox", response =>
            {
                ClearResults();
                if (response == null || !response.success)
                {
                    SetStatus(response != null ? response.error : "Ошибка загрузки писем.");
                    return;
                }

                AdminInboxMessageDto[] messages = response.messages ?? Array.Empty<AdminInboxMessageDto>();
                for (int i = 0; i < messages.Length; i++)
                {
                    AdminInboxMessageDto message = messages[i];
                    string title = $"{message.sender_name}: {message.subject}";
                    Button row = CreateButton(resultsRoot, title, new Color32(34, 42, 82, 235), () => ShowInboxMessage(message));
                    RectTransform rect = row.transform as RectTransform;
                    rect.sizeDelta = new Vector2(0f, 84f);
                    LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
                    layout.preferredHeight = 84f;
                }

                SetStatus(messages.Length == 0 ? "Писем от игроков пока нет." : $"Писем: {messages.Length}");
            }));
        }

        private void ShowInboxMessage(AdminInboxMessageDto message)
        {
            if (message == null || profileText == null)
                return;

            profileText.text =
                $"Письмо от игрока\n" +
                $"Sender: {message.sender_name}\n" +
                $"Nick: {message.nickname}\n" +
                $"ID: {message.sender_user_id} / {message.public_player_id}\n" +
                $"Email: {message.account_email}\n" +
                $"Date: {message.created_at}\n\n" +
                $"{message.subject}\n\n{message.body}";
        }

        private void SelectPlayer(AdminPlayerDto player)
        {
            if (player == null)
                return;

            selectedPlayer = player;
            StartCoroutine(GetJson<AdminProfileResponse>("/admin/players/" + player.id, response =>
            {
                if (response != null && response.success && response.player != null)
                    selectedPlayer = response.player;

                RenderSelectedPlayer();
            }));
        }

        private void SendCurrency()
        {
            if (!RequirePlayer())
                return;

            int amount = ParsePositiveInt(currencyAmountInput != null ? currencyAmountInput.text : string.Empty, 1);
            string currencyId = string.IsNullOrWhiteSpace(currencyIdInput.text) ? "oz_gold" : currencyIdInput.text.Trim();
            AdminAttachmentDto attachment = new AdminAttachmentDto
            {
                kind = "currency",
                currencyId = currencyId,
                amount = amount,
                label = currencyId,
                iconResourcePath = currencyId
            };
            SendGift("Admin gift: " + currencyId, $"Owner отправил подарок: {amount} {currencyId}.", attachment);
        }

        private void SendStone()
        {
            if (!RequirePlayer())
                return;

            string itemId = string.IsNullOrWhiteSpace(stoneIdInput.text) ? "epic_stone_gift" : stoneIdInput.text.Trim();
            string rarity = string.IsNullOrWhiteSpace(stoneRarityInput.text) ? "epic" : stoneRarityInput.text.Trim();
            AdminAttachmentDto attachment = new AdminAttachmentDto
            {
                kind = "battle_tile",
                itemId = itemId,
                amount = 1,
                rarity = rarity,
                label = rarity + " stone",
                iconResourcePath = itemId
            };
            SendGift("Admin gift: " + rarity + " stone", "Owner отправил камень. Забери вложение в почте.", attachment);
        }

        private void SendGift(string subject, string body, AdminAttachmentDto attachment)
        {
            AdminSendMailRequest payload = new AdminSendMailRequest
            {
                token = GetSessionToken(),
                targetUserId = selectedPlayer.id,
                senderName = "Owner",
                category = "official",
                subject = subject,
                body = body,
                attachments = new[] { attachment }
            };

            StartCoroutine(PostJson<AdminSimpleResponse>("/mailbox/admin/send", JsonUtility.ToJson(payload), response =>
            {
                SetStatus(response != null && response.success ? "Подарок отправлен." : response != null ? response.error : "Ошибка отправки.");
            }));
        }

        private void Moderate(string action)
        {
            if (!RequirePlayer())
                return;

            AdminModerationRequest payload = new AdminModerationRequest
            {
                token = GetSessionToken(),
                action = action,
                durationMinutes = ParsePositiveInt(durationInput != null ? durationInput.text : string.Empty, 60),
                reason = reasonInput != null ? reasonInput.text : string.Empty
            };

            StartCoroutine(PostJson<AdminProfileResponse>("/admin/players/" + selectedPlayer.id + "/moderation", JsonUtility.ToJson(payload), response =>
            {
                if (response != null && response.success)
                {
                    selectedPlayer = response.player ?? selectedPlayer;
                    RenderSelectedPlayer();
                    SetStatus("Модерация применена: " + action);
                    return;
                }

                SetStatus(response != null ? response.error : "Ошибка модерации.");
            }));
        }

        private void RenderSelectedPlayer()
        {
            if (profileText == null || selectedPlayer == null)
                return;

            profileText.text =
                $"Nick: {selectedPlayer.nickname}\n" +
                $"ID: {selectedPlayer.id} / {selectedPlayer.publicPlayerId}\n" +
                $"Email: {selectedPlayer.email}\n" +
                $"Dynasty: {selectedPlayer.dynastyName} ({selectedPlayer.dynastyId})\n" +
                $"Created: {selectedPlayer.createdAt}\n" +
                $"Last active: {selectedPlayer.lastActiveAt}\n" +
                $"Gold: {selectedPlayer.goldBalance}   Ametist: {selectedPlayer.amethystBalance}   OzTile: {selectedPlayer.ozTileBalance}\n" +
                $"Rank: {selectedPlayer.battleRankTier} {selectedPlayer.battleRankPoints}\n" +
                $"Mute until: {selectedPlayer.muteUntil}\n" +
                $"Ban until: {selectedPlayer.banUntil}";
        }

        private IEnumerator GetJson<T>(string path, Action<T> completed)
        {
            string url = BuildUrl(path + (path.Contains("?") ? "&" : "?") + "token=" + UnityWebRequest.EscapeURL(GetSessionToken()));
            using UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            completed?.Invoke(ParseResponse<T>(request));
        }

        private IEnumerator PostJson<T>(string path, string json, Action<T> completed)
        {
            using UnityWebRequest request = new UnityWebRequest(BuildUrl(path), "POST");
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            completed?.Invoke(ParseResponse<T>(request));
        }

        private static T ParseResponse<T>(UnityWebRequest request)
        {
            if (request == null || request.result != UnityWebRequest.Result.Success)
                return default;

            string text = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return default;

            try { return JsonUtility.FromJson<T>(text); }
            catch { return default; }
        }

        private bool RequirePlayer()
        {
            if (selectedPlayer != null && selectedPlayer.id > 0)
                return true;

            SetStatus("Сначала выбери игрока.");
            return false;
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text ?? string.Empty;
        }

        private void ClearResults()
        {
            if (resultsRoot == null)
                return;

            for (int i = resultsRoot.childCount - 1; i >= 0; i--)
                Destroy(resultsRoot.GetChild(i).gameObject);
        }

        private static int ParsePositiveInt(string raw, int fallback)
        {
            return int.TryParse(raw, out int value) ? Mathf.Max(1, value) : fallback;
        }

        private static string GetSessionToken()
        {
            return PlayerPrefs.GetString(KeySessionToken, string.Empty);
        }

        private static string BuildUrl(string path)
        {
            return BackendEndpoints.BuildUrl(BackendEndpoints.PrimaryBaseUrl, path);
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string text, int size, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TMP_Text label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject go = CreatePanel(parent, "Button", color);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            if (action != null)
                button.onClick.AddListener(action);

            TMP_Text label = CreateText(go.transform, text, 28, FontStyles.Bold, TextAlignmentOptions.Center);
            Anchor(label.transform as RectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            return button;
        }

        private static TMP_InputField CreateInput(Transform parent, string placeholder)
        {
            GameObject go = CreatePanel(parent, "Input", new Color32(3, 12, 24, 230));
            TMP_InputField input = go.AddComponent<TMP_InputField>();

            TMP_Text text = CreateText(go.transform, string.Empty, 27, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Anchor(text.transform as RectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 4f), new Vector2(-18f, -4f));
            TMP_Text hint = CreateText(go.transform, placeholder, 27, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            hint.color = new Color32(180, 198, 220, 160);
            Anchor(hint.transform as RectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 4f), new Vector2(-18f, -4f));

            input.textComponent = text;
            input.placeholder = hint;
            input.targetGraphic = go.GetComponent<Image>();
            return input;
        }

        private static ScrollRect CreateScroll(Transform parent, string name, out Transform content)
        {
            GameObject root = CreatePanel(parent, name, new Color32(2, 8, 18, 210));
            ScrollRect scroll = root.AddComponent<ScrollRect>();
            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(root.transform, false);
            RectTransform contentRect = contentGo.transform as RectTransform;
            Anchor(contentRect, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            content = contentGo.transform;
            return scroll;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
                return;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        [Serializable]
        private sealed class AdminSimpleResponse
        {
            public bool success;
            public string error;
        }

        [Serializable]
        private sealed class AdminSearchResponse
        {
            public bool success;
            public string error;
            public AdminPlayerDto[] players;
        }

        [Serializable]
        private sealed class AdminProfileResponse
        {
            public bool success;
            public string error;
            public AdminPlayerDto player;
        }

        [Serializable]
        private sealed class AdminInboxResponse
        {
            public bool success;
            public string error;
            public AdminInboxMessageDto[] messages;
        }

        [Serializable]
        private sealed class AdminInboxMessageDto
        {
            public int id;
            public string sender_name;
            public string subject;
            public string body;
            public string created_at;
            public int sender_user_id;
            public string nickname;
            public string public_player_id;
            public string account_email;
        }

        [Serializable]
        private sealed class AdminPlayerDto
        {
            public int id;
            public int accountId;
            public string email;
            public string nickname;
            public string publicPlayerId;
            public string dynastyName;
            public string dynastyId;
            public int goldBalance;
            public int amethystBalance;
            public int ozTileBalance;
            public string battleRankTier;
            public int battleRankPoints;
            public string banUntil;
            public string muteUntil;
            public string createdAt;
            public string updatedAt;
            public string lastActiveAt;
        }

        [Serializable]
        private sealed class AdminSendMailRequest
        {
            public string token;
            public int targetUserId;
            public string senderName;
            public string category;
            public string subject;
            public string body;
            public AdminAttachmentDto[] attachments;
        }

        [Serializable]
        private sealed class AdminAttachmentDto
        {
            public string kind;
            public string currencyId;
            public string itemId;
            public int amount;
            public string label;
            public string iconResourcePath;
            public string rarity;
        }

        [Serializable]
        private sealed class AdminModerationRequest
        {
            public string token;
            public string action;
            public int durationMinutes;
            public string reason;
        }
    }
}
