using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class DeveloperSupportRequestCardUI : MonoBehaviour, IPointerClickHandler
    {
        private const float DoubleTapSeconds = 0.45f;
        private const float HorizontalPadding = 22f;
        private const float TopPadding = 18f;
        private const float BottomPadding = 18f;
        private const float SectionSpacing = 12f;
        private static readonly Dictionary<string, Sprite> VoteIconSprites = new Dictionary<string, Sprite>();

        private sealed class CommentVisual
        {
            public RectTransform Root;
            public TMP_Text Author;
            public TMP_Text Body;
            public TMP_Text Original;
        }

        private sealed class StatusChipVisual
        {
            public RectTransform Root;
            public Image Background;
            public Outline Border;
            public TMP_Text Label;
            public Button RemoveButton;
            public string Status;
        }

        private RectTransform rootRect;
        private LayoutElement layoutElement;
        private TMP_Text authorText;
        private TMP_Text createdAtText;
        private TMP_Text bodyText;
        private TMP_Text originalBodyText;
        private RectTransform statusChipsRoot;
        private readonly List<StatusChipVisual> statusChipVisuals = new List<StatusChipVisual>();
        private RectTransform votingRoot;
        private Button likeButton;
        private Image likeBackground;
        private Outline likeBorder;
        private Image likeIcon;
        private TMP_Text likeLabel;
        private Button dislikeButton;
        private Image dislikeBackground;
        private Outline dislikeBorder;
        private Image dislikeIcon;
        private TMP_Text dislikeLabel;
        private RectTransform commentsRoot;
        private readonly List<CommentVisual> commentVisuals = new List<CommentVisual>();

        private GlobalChatUI owner;
        private long messageId;
        private bool actionable;
        private long boundMessageId = long.MinValue;
        private int boundVersion = int.MinValue;
        private float boundWidth = -1f;
        private GameLanguage boundLanguage;
        private int boundCommentCount = -1;
        private bool boundCanManage;
        private int boundLikeCount = -1;
        private int boundDislikeCount = -1;
        private int boundMyVote = int.MinValue;
        private bool boundVotingActive;
        private bool boundVotePending;
        private bool boundAutoTranslation;
        private string boundTranslationSignature = string.Empty;
        private float lastTapTime = -10f;
        private int lastTapPointerId = int.MinValue;
        private Vector2 lastTapPosition;

        public static DeveloperSupportRequestCardUI Create(Transform parent)
        {
            GameObject root = new GameObject(
                "DeveloperSupportRequestCard",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(LayoutElement),
                typeof(DeveloperSupportRequestCardUI));
            root.transform.SetParent(parent, false);

            DeveloperSupportRequestCardUI card = root.GetComponent<DeveloperSupportRequestCardUI>();
            card.Build();
            return card;
        }

        public bool Bind(GlobalChatUI cardOwner, GlobalChatService.GlobalChatMessage message, float availableWidth, bool canManage)
        {
            if (message == null)
                return false;

            owner = cardOwner;
            messageId = message.id;
            actionable = canManage;
            gameObject.SetActive(true);

            GameLanguage language = AppSettings.I != null ? AppSettings.I.Language : GameLanguage.Russian;
            int commentCount = message.comments != null ? message.comments.Length : 0;
            float width = Mathf.Max(360f, availableWidth);
            bool votingActive = GlobalChatService.HasDeveloperSupportStatus(message, "voting");
            bool votePending = cardOwner != null && cardOwner.IsSupportVotePending(message.id);
            bool autoTranslation = cardOwner == null || cardOwner.AutoTranslationEnabled;
            string translationSignature = BuildTranslationSignature(message);
            bool unchanged = boundMessageId == message.id &&
                             boundVersion == message.version &&
                             Mathf.Abs(boundWidth - width) < 0.5f &&
                             boundLanguage == language &&
                             boundCommentCount == commentCount &&
                             boundCanManage == canManage &&
                             boundLikeCount == message.likeCount &&
                             boundDislikeCount == message.dislikeCount &&
                             boundMyVote == message.myVote &&
                             boundVotingActive == votingActive &&
                             boundVotePending == votePending &&
                             boundAutoTranslation == autoTranslation &&
                             boundTranslationSignature == translationSignature;
            if (unchanged)
                return false;

            boundMessageId = message.id;
            boundVersion = message.version;
            boundWidth = width;
            boundLanguage = language;
            boundCommentCount = commentCount;
            boundCanManage = canManage;
            boundLikeCount = message.likeCount;
            boundDislikeCount = message.dislikeCount;
            boundMyVote = message.myVote;
            boundVotingActive = votingActive;
            boundVotePending = votePending;
            boundAutoTranslation = autoTranslation;
            boundTranslationSignature = translationSignature;
            gameObject.name = "SupportRequest_" + message.id;

            string playerName = string.IsNullOrWhiteSpace(message.nickname)
                ? GameLocalization.Text("common.player")
                : message.nickname.Trim();
            string displayName = AllianceIdentityFormatter.FormatName(playerName, message.allianceTag);
            authorText.text = message.isDeveloper ? displayName + "  OWNER" : displayName;
            authorText.color = message.isDeveloper
                ? new Color(1f, 0.79f, 0.25f, 1f)
                : new Color(0.45f, 0.85f, 1f, 1f);
            createdAtText.text = FormatCreatedAt(message.createdAt);
            string originalRequestText = string.IsNullOrWhiteSpace(message.text) ? string.Empty : message.text.Trim();
            bool showRequestTranslation = autoTranslation &&
                                          message.isTranslated &&
                                          GlobalChatService.IsTranslationForCurrentLanguage(message.translatedLanguage) &&
                                          !string.IsNullOrWhiteSpace(message.translatedText);
            bodyText.text = showRequestTranslation ? message.translatedText.Trim() : originalRequestText;
            originalBodyText.gameObject.SetActive(false);
            originalBodyText.text = string.Empty;
            float widthScale = Mathf.Clamp(width / 1050f, 0.92f, 1.16f);
            createdAtText.fontSize = Mathf.Clamp(22f * widthScale, 19f, 24f);
            authorText.fontSize = Mathf.Clamp(29f * widthScale, 26f, 34f);
            bodyText.lineSpacing = 6f;

            float dateWidth = Mathf.Clamp(width * 0.24f, 190f, 250f);
            float authorWidth = Mathf.Max(150f, width - HorizontalPadding * 2f - dateWidth - 22f);
            float authorHeight = Mathf.Max(34f, authorText.GetPreferredValues(authorText.text, authorWidth, 0f).y + 4f);
            float headerHeight = authorHeight;

            SetTopRect(authorText.rectTransform, HorizontalPadding + 10f, TopPadding, -(HorizontalPadding + dateWidth + 12f), authorHeight);
            SetTopRightRect(createdAtText.rectTransform, HorizontalPadding, TopPadding, dateWidth, authorHeight);

            float bodyTop = TopPadding + headerHeight + SectionSpacing;
            float bodyWidth = width - HorizontalPadding * 2f;
            float voteHeight = 50f;
            float voteGap = 8f;
            float voteRowWidth = votingActive ? Mathf.Clamp(width * 0.27f, 250f, 310f) : 0f;
            float bodyVoteGap = votingActive ? 18f : 0f;
            float textBodyWidth = Mathf.Max(240f, bodyWidth - voteRowWidth - bodyVoteGap);
            bodyText.fontSize = CalculateAdaptiveFontSize(bodyText.text, textBodyWidth, 32f, 23f, 120f, 950f);
            originalBodyText.fontSize = Mathf.Clamp(bodyText.fontSize * 0.76f, 18f, 23f);
            float mainBodyHeight = Mathf.Max(44f, bodyText.GetPreferredValues(bodyText.text, textBodyWidth, 0f).y + 6f);
            float originalBodyHeight = 0f;
            float bodyHeight = mainBodyHeight + originalBodyHeight;
            float bodyRight = votingActive ? -(HorizontalPadding + voteRowWidth + bodyVoteGap) : -HorizontalPadding;
            SetTopRect(bodyText.rectTransform, HorizontalPadding, bodyTop, bodyRight, mainBodyHeight);

            votingRoot.gameObject.SetActive(votingActive);
            if (votingActive)
            {
                float voteButtonWidth = (voteRowWidth - voteGap) * 0.5f;
                float voteLeft = width - HorizontalPadding - voteRowWidth;
                SetTopLeftRect(votingRoot, voteLeft, bodyTop, voteRowWidth, voteHeight);
                SetTopLeftRect(likeButton.transform as RectTransform, 0f, 0f, voteButtonWidth, voteHeight);
                SetTopLeftRect(dislikeButton.transform as RectTransform, voteButtonWidth + voteGap, 0f, voteButtonWidth, voteHeight);
                likeIcon.rectTransform.sizeDelta = new Vector2(36f, 36f);
                dislikeIcon.rectTransform.sizeDelta = new Vector2(36f, 36f);
                likeLabel.text = Mathf.Max(0, message.likeCount).ToString();
                dislikeLabel.text = Mathf.Max(0, message.dislikeCount).ToString();
                likeLabel.fontSize = 25f;
                dislikeLabel.fontSize = 25f;
                ConfigureVoteButton(likeButton, likeBackground, likeBorder, message.myVote == 1, true, !votePending);
                ConfigureVoteButton(dislikeButton, dislikeBackground, dislikeBorder, message.myVote == -1, false, !votePending);
            }

            float cursorY = bodyTop + Mathf.Max(bodyHeight, votingActive ? voteHeight : 0f) + SectionSpacing;
            string[] activeStatuses = ResolveActiveStatuses(message);
            int statusCount = activeStatuses.Length > 0 ? activeStatuses.Length : 1;
            float chipX = HorizontalPadding;
            float chipTop = cursorY;
            float chipHeight = 46f;
            float chipGap = 10f;
            float chipRight = width - HorizontalPadding;
            for (int i = 0; i < statusCount; i++)
            {
                string status = activeStatuses.Length > 0 ? activeStatuses[i] : string.Empty;
                StatusChipVisual chip = EnsureStatusChipVisual(i);
                chip.Root.gameObject.SetActive(true);
                chip.Status = status;
                string label = GlobalChatUI.GetSupportStatusLabel(status);
                Color color = GlobalChatUI.GetSupportStatusColorValue(status);
                bool showRemove = canManage && !string.IsNullOrWhiteSpace(status);
                chip.Label.text = label;
                chip.Label.fontSize = Mathf.Clamp(23f * widthScale, 21f, 26f);
                chip.Label.color = color;
                chip.Background.color = new Color(color.r * 0.18f, color.g * 0.18f, color.b * 0.18f, 0.96f);
                chip.Border.effectColor = new Color(color.r, color.g, color.b, 0.52f);
                chip.RemoveButton.gameObject.SetActive(showRemove);
                chip.RemoveButton.interactable = showRemove;

                float removeWidth = showRemove ? 44f : 0f;
                float chipWidth = Mathf.Clamp(
                    chip.Label.GetPreferredValues(label).x + 34f + removeWidth,
                    138f,
                    Mathf.Min(340f, bodyWidth));
                if (chipX > HorizontalPadding && chipX + chipWidth > chipRight)
                {
                    chipX = HorizontalPadding;
                    chipTop += chipHeight + chipGap;
                }

                SetTopLeftRect(chip.Root, chipX, chipTop, chipWidth, chipHeight);
                SetStretchRect(chip.Label.rectTransform, 16f, 3f, -(16f + removeWidth), -3f);
                if (showRemove)
                    SetTopRightRect(chip.RemoveButton.transform as RectTransform, 3f, 3f, 40f, 40f);
                chipX += chipWidth + chipGap;
            }
            for (int i = statusCount; i < statusChipVisuals.Count; i++)
                statusChipVisuals[i].Root.gameObject.SetActive(false);
            cursorY = chipTop + chipHeight + SectionSpacing;

            for (int i = 0; i < commentCount; i++)
            {
                GlobalChatService.DeveloperSupportComment comment = message.comments[i];
                CommentVisual visual = EnsureCommentVisual(i);
                if (comment == null || string.IsNullOrWhiteSpace(comment.text))
                {
                    visual.Root.gameObject.SetActive(false);
                    continue;
                }

                visual.Root.gameObject.SetActive(true);
                string developerName = string.IsNullOrWhiteSpace(comment.developerNickname)
                    ? "Ozkullar"
                    : comment.developerNickname.Trim();
                visual.Author.text = comment.isDeveloper
                    ? developerName + "  OWNER"
                    : developerName;
                string originalCommentText = comment.text.Trim();
                bool showCommentTranslation = autoTranslation &&
                                              comment.isTranslated &&
                                              GlobalChatService.IsTranslationForCurrentLanguage(comment.translatedLanguage) &&
                                              !string.IsNullOrWhiteSpace(comment.translatedText);
                visual.Body.text = showCommentTranslation ? comment.translatedText.Trim() : originalCommentText;
                visual.Original.gameObject.SetActive(false);
                visual.Original.text = string.Empty;
                visual.Author.fontSize = Mathf.Clamp(24f * widthScale, 22f, 28f);
                visual.Body.fontSize = CalculateAdaptiveFontSize(visual.Body.text, width, 28f, 21f, 100f, 720f);
                visual.Original.fontSize = Mathf.Clamp(visual.Body.fontSize * 0.76f, 17f, 22f);
                visual.Body.lineSpacing = 5f;

                float commentLeft = HorizontalPadding + 22f;
                float commentWidth = Mathf.Max(260f, width - commentLeft - HorizontalPadding);
                float commentBodyWidth = commentWidth - 24f;
                float commentBodyHeight = Mathf.Max(36f, visual.Body.GetPreferredValues(visual.Body.text, commentBodyWidth, 0f).y + 4f);
                float commentOriginalHeight = 0f;
                float commentAuthorHeight = 36f;
                float commentHeight = 56f + commentBodyHeight + commentOriginalHeight;
                SetTopRect(visual.Root, commentLeft, cursorY, -HorizontalPadding, commentHeight);
                SetTopRect(visual.Author.rectTransform, 14f, 9f, -14f, commentAuthorHeight);
                SetTopRect(visual.Body.rectTransform, 14f, 45f, -14f, commentBodyHeight);
                cursorY += commentHeight + 10f;
            }

            for (int i = commentCount; i < commentVisuals.Count; i++)
                commentVisuals[i].Root.gameObject.SetActive(false);

            float preferredHeight = Mathf.Max(168f, cursorY + BottomPadding - (commentCount > 0 ? 10f : SectionSpacing));
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.minHeight = 168f;
            return true;
        }

        public void Release()
        {
            owner = null;
            messageId = 0L;
            actionable = false;
            lastTapTime = -10f;
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!actionable || owner == null || eventData == null || eventData.dragging)
                return;
            if (eventData.pointerPress != null && eventData.pointerPress.GetComponentInParent<Button>() != null)
                return;

            float now = Time.unscaledTime;
            bool doubleTap = eventData.pointerId == lastTapPointerId &&
                             now - lastTapTime <= DoubleTapSeconds &&
                             (eventData.position - lastTapPosition).sqrMagnitude <= 96f * 96f;
            lastTapTime = now;
            lastTapPointerId = eventData.pointerId;
            lastTapPosition = eventData.position;
            if (!doubleTap)
                return;

            lastTapTime = -10f;
            owner.HandleSupportCardDoubleTap(messageId);
        }

        private StatusChipVisual EnsureStatusChipVisual(int index)
        {
            while (statusChipVisuals.Count <= index)
            {
                RectTransform root = CreateImage(statusChipsRoot, "StatusChip", new Color(0.06f, 0.20f, 0.28f, 0.96f));
                Image background = root.GetComponent<Image>();
                GlobalChatUI.ApplyRoundedSurface(background);
                Outline border = root.gameObject.AddComponent<Outline>();
                border.effectDistance = new Vector2(1f, -1f);
                TMP_Text label = CreateText(root, "Label", 18f, Color.white, FontStyles.Bold);
                label.alignment = TextAlignmentOptions.Center;

                GameObject removeRoot = new GameObject("RemoveStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                removeRoot.transform.SetParent(root, false);
                Image removeImage = removeRoot.GetComponent<Image>();
                GlobalChatUI.ApplyRoundedSurface(removeImage);
                removeImage.color = new Color(0.02f, 0.03f, 0.05f, 0.58f);
                Button removeButton = removeRoot.GetComponent<Button>();
                removeButton.targetGraphic = removeImage;
                TMP_Text removeLabel = CreateText(removeRoot.transform, "Cross", 26f, Color.white, FontStyles.Bold);
                removeLabel.text = "X";
                removeLabel.alignment = TextAlignmentOptions.Center;
                removeLabel.rectTransform.anchorMin = Vector2.zero;
                removeLabel.rectTransform.anchorMax = Vector2.one;
                removeLabel.rectTransform.offsetMin = Vector2.zero;
                removeLabel.rectTransform.offsetMax = Vector2.zero;

                StatusChipVisual visual = new StatusChipVisual
                {
                    Root = root,
                    Background = background,
                    Border = border,
                    Label = label,
                    RemoveButton = removeButton,
                    Status = string.Empty
                };
                removeButton.onClick.AddListener(() => RequestStatusRemoval(visual));
                statusChipVisuals.Add(visual);
            }

            return statusChipVisuals[index];
        }

        private void RequestStatusRemoval(StatusChipVisual visual)
        {
            if (!actionable || owner == null || visual == null || string.IsNullOrWhiteSpace(visual.Status))
                return;
            owner.HandleSupportStatusRemoval(messageId, visual.Status);
        }

        private static string[] ResolveActiveStatuses(GlobalChatService.GlobalChatMessage message)
        {
            if (message == null)
                return new string[0];
            if (message.statuses != null && message.statuses.Length > 0)
                return message.statuses;
            if (!string.IsNullOrWhiteSpace(message.status))
                return new[] { message.status };
            return new string[0];
        }

        private void Build()
        {
            rootRect = transform as RectTransform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(0f, 168f);

            Image background = GetComponent<Image>();
            GlobalChatUI.ApplyRoundedSurface(background);
            background.color = new Color(0.025f, 0.075f, 0.105f, 0.98f);
            background.raycastTarget = true;
            Outline outline = GetComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.68f, 0.86f, 0.34f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            layoutElement = GetComponent<LayoutElement>();
            layoutElement.minHeight = 168f;
            layoutElement.preferredHeight = 168f;

            RectTransform accent = CreateImage(transform, "AccentStrip", new Color(0.16f, 0.72f, 0.95f, 1f));
            GlobalChatUI.ApplyRoundedSurface(accent.GetComponent<Image>());
            accent.anchorMin = new Vector2(0f, 1f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 1f);
            accent.anchoredPosition = new Vector2(14f, -18f);
            accent.sizeDelta = new Vector2(5f, 48f);

            authorText = CreateText(transform, "Author", 21f, new Color(0.45f, 0.85f, 1f, 1f), FontStyles.Bold);
            createdAtText = CreateText(transform, "CreatedAt", 20f, new Color(0.66f, 0.76f, 0.84f, 1f), FontStyles.Normal);
            createdAtText.alignment = TextAlignmentOptions.TopRight;
            createdAtText.textWrappingMode = TextWrappingModes.NoWrap;
            createdAtText.overflowMode = TextOverflowModes.Ellipsis;
            bodyText = CreateText(transform, "RequestBody", 22f, new Color(0.92f, 0.96f, 0.98f, 1f), FontStyles.Normal);
            originalBodyText = CreateText(transform, "RequestOriginal", 18f, new Color(0.55f, 0.66f, 0.74f, 1f), FontStyles.Italic);
            originalBodyText.gameObject.SetActive(false);

            GameObject chips = new GameObject("StatusChips", typeof(RectTransform));
            chips.transform.SetParent(transform, false);
            statusChipsRoot = chips.transform as RectTransform;
            statusChipsRoot.anchorMin = new Vector2(0f, 1f);
            statusChipsRoot.anchorMax = new Vector2(1f, 1f);
            statusChipsRoot.pivot = new Vector2(0.5f, 1f);
            statusChipsRoot.anchoredPosition = Vector2.zero;
            statusChipsRoot.sizeDelta = Vector2.zero;

            GameObject comments = new GameObject("Comments", typeof(RectTransform));
            comments.transform.SetParent(transform, false);
            commentsRoot = comments.transform as RectTransform;
            commentsRoot.anchorMin = new Vector2(0f, 1f);
            commentsRoot.anchorMax = new Vector2(1f, 1f);
            commentsRoot.pivot = new Vector2(0.5f, 1f);
            commentsRoot.anchoredPosition = Vector2.zero;
            commentsRoot.sizeDelta = Vector2.zero;

            votingRoot = new GameObject("VotingButtons", typeof(RectTransform)).transform as RectTransform;
            votingRoot.SetParent(transform, false);
            CreateVoteButton(votingRoot, "LikeButton", "UI/Chat/support-vote-like", out likeButton, out likeBackground, out likeBorder, out likeIcon, out likeLabel);
            CreateVoteButton(votingRoot, "DislikeButton", "UI/Chat/support-vote-dislike", out dislikeButton, out dislikeBackground, out dislikeBorder, out dislikeIcon, out dislikeLabel);
            likeButton.onClick.AddListener(() => RequestVote(1));
            dislikeButton.onClick.AddListener(() => RequestVote(-1));
            votingRoot.gameObject.SetActive(false);

            votingRoot.SetSiblingIndex(commentsRoot.GetSiblingIndex());
        }

        private void RequestVote(int vote)
        {
            if (owner == null || boundVotePending)
                return;
            owner.HandleSupportVote(messageId, vote);
        }

        private static void CreateVoteButton(
            Transform parent,
            string name,
            string iconResourcePath,
            out Button button,
            out Image background,
            out Outline border,
            out Image icon,
            out TMP_Text label)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button));
            root.transform.SetParent(parent, false);
            background = root.GetComponent<Image>();
            GlobalChatUI.ApplyRoundedSurface(background);
            border = root.GetComponent<Outline>();
            border.effectDistance = new Vector2(2f, -2f);
            button = root.GetComponent<Button>();
            button.targetGraphic = background;

            GameObject iconRoot = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconRoot.transform.SetParent(root.transform, false);
            icon = iconRoot.GetComponent<Image>();
            icon.sprite = LoadVoteIconSprite(iconResourcePath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(14f, 0f);
            iconRect.sizeDelta = new Vector2(48f, 48f);

            label = CreateText(root.transform, "Count", 28f, Color.white, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(68f, 4f);
            label.rectTransform.offsetMax = new Vector2(-18f, -4f);
        }

        private static Sprite LoadVoteIconSprite(string resourcePath)
        {
            if (VoteIconSprites.TryGetValue(resourcePath, out Sprite cached))
                return cached;

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
                if (sprites != null && sprites.Length > 0)
                    sprite = sprites[0];
            }
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            VoteIconSprites[resourcePath] = sprite;
            return sprite;
        }

        private static void ConfigureVoteButton(
            Button button,
            Image background,
            Outline border,
            bool selected,
            bool isLike,
            bool interactable)
        {
            Color accent = isLike
                ? new Color(0.28f, 0.90f, 0.62f, 1f)
                : new Color(1f, 0.38f, 0.46f, 1f);
            background.color = selected
                ? new Color(accent.r * 0.42f, accent.g * 0.42f, accent.b * 0.42f, 1f)
                : new Color(accent.r * 0.14f, accent.g * 0.14f, accent.b * 0.14f, 0.98f);
            border.effectColor = new Color(accent.r, accent.g, accent.b, selected ? 0.96f : 0.55f);
            button.interactable = interactable;
        }

        private CommentVisual EnsureCommentVisual(int index)
        {
            while (commentVisuals.Count <= index)
            {
                RectTransform row = CreateImage(commentsRoot, "DeveloperComment", new Color(0.16f, 0.11f, 0.25f, 0.92f));
                GlobalChatUI.ApplyRoundedSurface(row.GetComponent<Image>());
                TMP_Text author = CreateText(row, "CommentAuthor", 18f, new Color(0.76f, 0.67f, 1f, 1f), FontStyles.Bold);
                TMP_Text body = CreateText(row, "CommentBody", 19f, new Color(0.92f, 0.89f, 1f, 1f), FontStyles.Normal);
                TMP_Text original = CreateText(row, "CommentOriginal", 17f, new Color(0.62f, 0.57f, 0.74f, 1f), FontStyles.Italic);
                original.gameObject.SetActive(false);
                commentVisuals.Add(new CommentVisual { Root = row, Author = author, Body = body, Original = original });
            }

            return commentVisuals[index];
        }

        private static string BuildTranslationSignature(GlobalChatService.GlobalChatMessage message)
        {
            if (message == null)
                return string.Empty;
            System.Text.StringBuilder signature = new System.Text.StringBuilder();
            signature.Append(message.translatedLanguage).Append('|').Append(message.translatedText).Append('|').Append(message.translationStatus);
            if (message.comments != null)
            {
                for (int i = 0; i < message.comments.Length; i++)
                {
                    GlobalChatService.DeveloperSupportComment comment = message.comments[i];
                    if (comment != null)
                        signature.Append('|').Append(comment.id).Append(':').Append(comment.translatedLanguage).Append(':').Append(comment.translatedText).Append(':').Append(comment.translationStatus);
                }
            }
            return signature.ToString();
        }

        private static float CalculateAdaptiveFontSize(
            string value,
            float availableWidth,
            float maximumSize,
            float minimumSize,
            float shrinkStartLength,
            float shrinkEndLength)
        {
            int characterCount = string.IsNullOrEmpty(value) ? 0 : value.Length;
            float contentDensity = Mathf.InverseLerp(shrinkStartLength, shrinkEndLength, characterCount);
            float sizeByLength = Mathf.Lerp(maximumSize, minimumSize, contentDensity);
            float widthScale = Mathf.Clamp(availableWidth / 1050f, 0.9f, 1.12f);
            return Mathf.Clamp(sizeByLength * widthScale, minimumSize, maximumSize * 1.12f);
        }

        private static string FormatCreatedAt(string value)
        {
            if (System.DateTimeOffset.TryParse(
                    value,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out System.DateTimeOffset createdAt))
            {
                return createdAt.ToLocalTime().ToString("dd.MM.yyyy  HH:mm");
            }

            return string.Empty;
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, Color color, FontStyles style)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            TMP_Text text = root.GetComponent<TMP_Text>();
            MainLobbyButtonStyle.ApplyFont(text);
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateImage(Transform parent, string name, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return root.transform as RectTransform;
        }

        private static void SetTopRect(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(right, -top);
        }

        private static void SetTopRightRect(RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopLeftRect(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetStretchRect(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }
    }
}
