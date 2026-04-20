using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class FriendsUI : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button addButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text friendsText;
        [SerializeField] private TMP_Text requestsText;
        [SerializeField] private TMP_Text searchText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private float refreshSeconds = 6f;

        private Coroutine refreshRoutine;
        private bool busy;

        private void Awake()
        {
            if (toggleButton == null || panelRoot == null)
                Build(transform);
        }

        public static FriendsUI CreateInScene()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Exclude);
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(2400f, 1080f);
                scaler.matchWidthOrHeight = 0.65f;
            }

            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Exclude) == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<InputSystemUIInputModule>();
#else
                eventSystem.AddComponent<StandaloneInputModule>();
#endif
            }

            GameObject root = new GameObject("FriendsUI", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            FriendsUI ui = root.AddComponent<FriendsUI>();
            if (ui.toggleButton == null || ui.panelRoot == null)
                ui.Build(root.transform);

            return ui;
        }

        private void OnEnable()
        {
            Bind();

            if (FriendsService.I != null)
            {
                FriendsService.I.FriendsChanged += RefreshView;
                FriendsService.I.ErrorChanged += RefreshStatus;
            }

            RefreshView();
            RefreshStatus(FriendsService.I != null ? FriendsService.I.LastError : string.Empty);
        }

        private void OnDisable()
        {
            if (FriendsService.I != null)
            {
                FriendsService.I.FriendsChanged -= RefreshView;
                FriendsService.I.ErrorChanged -= RefreshStatus;
            }

            StopRefreshing();
            Unbind();
        }

        private void Build(Transform parent)
        {
            RectTransform rootRect = parent as RectTransform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            toggleButton = CreateButton(parent, "FriendsButton", "Friends", new Vector2(1f, 0f), new Vector2(-138f, 140f), new Vector2(180f, 56f));

            panelRoot = new GameObject("FriendsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            RectTransform panelRect = panelRoot.transform as RectTransform;
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-590f, 140f);
            panelRect.sizeDelta = new Vector2(560f, 610f);
            panelRoot.GetComponent<Image>().color = new Color(0.05f, 0.055f, 0.07f, 0.95f);

            TMP_Text title = CreateText(panelRoot.transform, "Title", "Friends", 30f, TextAlignmentOptions.Left);
            SetOffsets(title.rectTransform, new Vector2(22f, -58f), new Vector2(-82f, -14f), new Vector2(0f, 1f), new Vector2(1f, 1f));

            closeButton = CreateButton(panelRoot.transform, "CloseButton", "X", new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(48f, 48f));
            nicknameInput = CreateInput(panelRoot.transform, "NicknameInput", "Nickname", new Vector2(18f, -110f), new Vector2(-198f, -60f));
            addButton = CreateButton(panelRoot.transform, "AddButton", "Add", new Vector2(1f, 1f), new Vector2(-111f, -85f), new Vector2(150f, 50f));
            refreshButton = CreateButton(panelRoot.transform, "RefreshButton", "Refresh", new Vector2(1f, 1f), new Vector2(-111f, -145f), new Vector2(150f, 50f));

            requestsText = CreateText(panelRoot.transform, "RequestsText", "", 18f, TextAlignmentOptions.TopLeft);
            SetOffsets(requestsText.rectTransform, new Vector2(22f, 360f), new Vector2(-22f, 492f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            requestsText.enableAutoSizing = true;
            requestsText.fontSizeMin = 13f;
            requestsText.fontSizeMax = 18f;

            acceptButton = CreateButton(panelRoot.transform, "AcceptButton", "Accept", new Vector2(0f, 0f), new Vector2(96f, 336f), new Vector2(145f, 44f));
            declineButton = CreateButton(panelRoot.transform, "DeclineButton", "Decline", new Vector2(0f, 0f), new Vector2(248f, 336f), new Vector2(145f, 44f));

            friendsText = CreateText(panelRoot.transform, "FriendsText", "", 19f, TextAlignmentOptions.TopLeft);
            SetOffsets(friendsText.rectTransform, new Vector2(22f, 92f), new Vector2(-22f, 320f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            friendsText.enableAutoSizing = true;
            friendsText.fontSizeMin = 13f;
            friendsText.fontSizeMax = 19f;

            searchText = CreateText(panelRoot.transform, "SearchText", "", 16f, TextAlignmentOptions.TopLeft);
            SetOffsets(searchText.rectTransform, new Vector2(22f, 48f), new Vector2(-22f, 88f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            searchText.color = new Color(0.82f, 0.9f, 1f, 1f);

            statusText = CreateText(panelRoot.transform, "StatusText", "", 15f, TextAlignmentOptions.Left);
            SetOffsets(statusText.rectTransform, new Vector2(22f, 12f), new Vector2(-22f, 36f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            statusText.color = new Color(1f, 0.72f, 0.38f, 1f);

            panelRoot.SetActive(false);
            Bind();
        }

        private void Bind()
        {
            AddListener(toggleButton, TogglePanel);
            AddListener(closeButton, ClosePanel);
            AddListener(addButton, AddByNickname);
            AddListener(refreshButton, RefreshNow);
            AddListener(acceptButton, AcceptFirstRequest);
            AddListener(declineButton, DeclineFirstRequest);

            if (nicknameInput != null)
            {
                nicknameInput.onSubmit.RemoveListener(AddBySubmit);
                nicknameInput.onSubmit.AddListener(AddBySubmit);
                nicknameInput.onValueChanged.RemoveListener(SearchByNickname);
                nicknameInput.onValueChanged.AddListener(SearchByNickname);
            }
        }

        private void Unbind()
        {
            RemoveListener(toggleButton, TogglePanel);
            RemoveListener(closeButton, ClosePanel);
            RemoveListener(addButton, AddByNickname);
            RemoveListener(refreshButton, RefreshNow);
            RemoveListener(acceptButton, AcceptFirstRequest);
            RemoveListener(declineButton, DeclineFirstRequest);

            if (nicknameInput != null)
            {
                nicknameInput.onSubmit.RemoveListener(AddBySubmit);
                nicknameInput.onValueChanged.RemoveListener(SearchByNickname);
            }
        }

        private void TogglePanel()
        {
            if (panelRoot == null)
                return;

            bool show = !panelRoot.activeSelf;
            panelRoot.SetActive(show);

            if (show)
            {
                StartRefreshing();
                RefreshNow();
            }
            else
            {
                StopRefreshing();
            }
        }

        private void ClosePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            StopRefreshing();
        }

        private void StartRefreshing()
        {
            if (refreshRoutine != null)
                StopCoroutine(refreshRoutine);

            refreshRoutine = StartCoroutine(RefreshLoop());
        }

        private void StopRefreshing()
        {
            if (refreshRoutine == null)
                return;

            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }

        private IEnumerator RefreshLoop()
        {
            while (true)
            {
                if (FriendsService.I != null)
                    yield return FriendsService.I.Refresh();

                yield return new WaitForSecondsRealtime(Mathf.Max(2f, refreshSeconds));
            }
        }

        private void RefreshNow()
        {
            if (FriendsService.I != null)
                StartCoroutine(FriendsService.I.Refresh());
        }

        private void AddBySubmit(string _)
        {
            AddByNickname();
        }

        private void AddByNickname()
        {
            if (busy || FriendsService.I == null || nicknameInput == null)
                return;

            string nickname = nicknameInput.text;
            if (string.IsNullOrWhiteSpace(nickname))
                return;

            StartCoroutine(AddRoutine(nickname));
        }

        private IEnumerator AddRoutine(string nickname)
        {
            busy = true;
            SetButtonsInteractable(false);

            bool ok = false;
            string message = string.Empty;
            yield return FriendsService.I.SendRequestByNickname(nickname, (success, text) =>
            {
                ok = success;
                message = text;
            });

            RefreshStatus(ok ? (string.IsNullOrWhiteSpace(message) ? "Request sent." : message) : message);
            if (ok && nicknameInput != null)
                nicknameInput.text = string.Empty;

            SetButtonsInteractable(true);
            busy = false;
        }

        private void SearchByNickname(string value)
        {
            if (FriendsService.I == null || string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
            {
                if (searchText != null)
                    searchText.text = string.Empty;
                return;
            }

            StartCoroutine(SearchRoutine(value.Trim()));
        }

        private IEnumerator SearchRoutine(string nickname)
        {
            bool ok = false;
            FriendsService.FriendUser[] users = null;
            string error = string.Empty;

            yield return FriendsService.I.Search(nickname, (success, message, result) =>
            {
                ok = success;
                error = message;
                users = result;
            });

            if (!ok)
            {
                if (searchText != null)
                    searchText.text = error;
                yield break;
            }

            StringBuilder builder = new StringBuilder();
            if (users != null)
            {
                int count = Mathf.Min(users.Length, 3);
                for (int i = 0; i < count; i++)
                {
                    FriendsService.FriendUser user = users[i];
                    if (user == null)
                        continue;

                    builder.Append(user.nickname);
                    if (user.isFriend)
                        builder.Append(" - friend");
                    else if (user.hasPendingOutgoing)
                        builder.Append(" - requested");
                    else if (user.hasPendingIncoming)
                        builder.Append(" - wants to add you");

                    if (i + 1 < count)
                        builder.Append("   ");
                }
            }

            if (searchText != null)
                searchText.text = builder.Length == 0 ? "No players found." : builder.ToString();
        }

        private void AcceptFirstRequest()
        {
            if (FriendsService.I == null || FriendsService.I.IncomingRequests.Count == 0)
                return;

            StartCoroutine(FriendsService.I.Accept(FriendsService.I.IncomingRequests[0].id, RefreshStatus));
        }

        private void DeclineFirstRequest()
        {
            if (FriendsService.I == null || FriendsService.I.IncomingRequests.Count == 0)
                return;

            StartCoroutine(FriendsService.I.Decline(FriendsService.I.IncomingRequests[0].id, RefreshStatus));
        }

        private void RefreshView()
        {
            if (FriendsService.I == null)
                return;

            RefreshRequests();
            RefreshFriends();
        }

        private void RefreshRequests()
        {
            IReadOnlyList<FriendsService.IncomingFriendRequest> incoming = FriendsService.I.IncomingRequests;
            IReadOnlyList<FriendsService.OutgoingFriendRequest> outgoing = FriendsService.I.OutgoingRequests;
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Requests");
            if (incoming.Count == 0 && outgoing.Count == 0)
            {
                builder.Append("No pending requests.");
            }
            else
            {
                for (int i = 0; i < incoming.Count; i++)
                    builder.Append("Incoming: ").Append(Escape(incoming[i].senderNickname)).AppendLine();

                for (int i = 0; i < outgoing.Count; i++)
                    builder.Append("Sent: ").Append(Escape(outgoing[i].receiverNickname)).AppendLine();
            }

            if (requestsText != null)
                requestsText.text = builder.ToString();

            bool hasIncoming = incoming.Count > 0;
            if (acceptButton != null)
                acceptButton.gameObject.SetActive(hasIncoming);
            if (declineButton != null)
                declineButton.gameObject.SetActive(hasIncoming);
        }

        private void RefreshFriends()
        {
            IReadOnlyList<FriendsService.FriendUser> list = FriendsService.I.Friends;
            StringBuilder online = new StringBuilder();
            StringBuilder offline = new StringBuilder();

            for (int i = 0; i < list.Count; i++)
            {
                FriendsService.FriendUser friend = list[i];
                if (friend == null)
                    continue;

                StringBuilder target = friend.online ? online : offline;
                target.Append(friend.online ? "[ON] " : "[OFF] ");
                target.Append(Escape(string.IsNullOrWhiteSpace(friend.nickname) ? "Player" : friend.nickname));
                if (!string.IsNullOrWhiteSpace(friend.publicPlayerId))
                    target.Append("  ").Append(Escape(friend.publicPlayerId));
                target.AppendLine();
            }

            StringBuilder output = new StringBuilder();
            output.AppendLine("Online");
            output.Append(online.Length == 0 ? "No active friends.\n" : online.ToString());
            output.AppendLine();
            output.AppendLine("Offline");
            output.Append(offline.Length == 0 ? "No offline friends." : offline.ToString());

            if (friendsText != null)
                friendsText.text = output.ToString();
        }

        private void RefreshStatus(string value)
        {
            if (statusText != null)
                statusText.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        private void SetButtonsInteractable(bool value)
        {
            if (addButton != null)
                addButton.interactable = value;
            if (refreshButton != null)
                refreshButton.interactable = value;
        }

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholderText, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.transform as RectTransform;
            SetOffsets(rect, offsetMin, offsetMax, new Vector2(0f, 1f), new Vector2(1f, 1f));
            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            TMP_Text text = CreateText(root.transform, "Text", "", 20f, TextAlignmentOptions.Left);
            SetOffsets(text.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f), Vector2.zero, Vector2.one);

            TMP_Text placeholder = CreateText(root.transform, "Placeholder", placeholderText, 20f, TextAlignmentOptions.Left);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            SetOffsets(placeholder.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f), Vector2.zero, Vector2.one);

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 32;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.transform as RectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.16f, 0.45f, 0.54f, 0.96f);

            TMP_Text text = CreateText(root.transform, "Label", label, 21f, TextAlignmentOptions.Center);
            SetOffsets(text.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            return root.GetComponent<Button>();
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);

            TMP_Text text = root.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void SetOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
