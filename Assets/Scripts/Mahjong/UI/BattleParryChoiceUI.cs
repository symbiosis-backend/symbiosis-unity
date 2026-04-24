using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public enum BattleHitZone
    {
        Top = 0,
        Middle = 1,
        Bottom = 2
    }

    [DisallowMultipleComponent]
    public sealed class BattleParryChoiceUI : MonoBehaviour
    {
        private const int ZoneCount = 3;

        [Header("Layout")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text playerLabelText;
        [SerializeField] private TMP_Text opponentLabelText;
        [SerializeField] private Image playerCharacterImage;
        [SerializeField] private Image opponentCharacterImage;
        [SerializeField] private BattleCharacterModelView playerCharacterModelView;
        [SerializeField] private BattleCharacterModelView opponentCharacterModelView;
        [SerializeField] private Button[] playerZoneButtons = new Button[ZoneCount];
        [SerializeField] private Button[] opponentZoneButtons = new Button[ZoneCount];

        [Header("Parry Art")]
        [SerializeField] private Sprite parryPanelSprite;
        [SerializeField] private Sprite parryHeadSprite;
        [SerializeField] private Sprite parryBodySprite;
        [SerializeField] private Sprite parryLegsSprite;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float revealDuration = 0.9f;
        [SerializeField, Min(0f)] private float opponentMinThinkDelay = 0.45f;
        [SerializeField, Min(0f)] private float opponentMaxThinkDelay = 1.25f;

        [Header("Colors")]
        [SerializeField] private Color hiddenColor = new Color(1f, 1f, 1f, 0.34f);
        [SerializeField] private Color neutralColor = new Color(1f, 1f, 1f, 0.86f);
        [SerializeField] private Color chosenColor = new Color(0.28f, 0.72f, 1f, 1f);
        [SerializeField] private Color parriedColor = new Color(0.35f, 1f, 0.45f, 1f);
        [SerializeField] private Color damageColor = new Color(1f, 0.34f, 0.26f, 1f);

        private Action<BattleHitZone, BattleHitZone, bool> completed;
        private BattleBoardSide attackerSide;
        private BattleHitZone playerZone;
        private BattleHitZone opponentZone;
        private BattleHitZone attackZone;
        private BattleHitZone parryZone;
        private BattleCharacterDatabase.BattleCharacterData playerCharacterData;
        private BattleCharacterDatabase.BattleCharacterData opponentCharacterData;
        private Coroutine opponentRoutine;
        private Coroutine timerRoutine;
        private Coroutine revealRoutine;
        private bool awaitingChoice;
        private bool playerReady;
        private bool opponentReady;
        private bool flipOpponentCharacter;

        public static BattleParryChoiceUI CreateRuntime()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("BattleParryChoiceCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            GameObject uiObject = new GameObject("BattleParryChoiceUI", typeof(RectTransform), typeof(BattleParryChoiceUI));
            uiObject.transform.SetParent(canvas.transform, false);

            BattleParryChoiceUI ui = uiObject.GetComponent<BattleParryChoiceUI>();
            ui.BuildRuntimeUi(canvas);
            return ui;
        }

        private void Awake()
        {
            ResolveDefaultArtSprites();
            BuildRuntimeUi(FindAnyObjectByType<Canvas>(FindObjectsInactive.Include));
            BindButtons();
            Hide();
        }

        private void OnDisable()
        {
            awaitingChoice = false;
            StopRunningRoutines();
        }

        public void Show(
            BattleBoardSide attacker,
            BattleHitZone randomPlayerZone,
            BattleHitZone randomOpponentZone,
            float timeoutSeconds,
            Action<BattleHitZone, BattleHitZone, bool> onCompleted)
        {
            if (root == null)
                BuildRuntimeUi(FindAnyObjectByType<Canvas>(FindObjectsInactive.Include));

            StopRunningRoutines();

            attackerSide = attacker;
            playerZone = randomPlayerZone;
            opponentZone = randomOpponentZone;
            completed = onCompleted;
            awaitingChoice = true;
            playerReady = false;
            opponentReady = false;

            if (root != null)
                root.SetActive(true);

            ApplyPendingCharacters();
            RefreshTexts();
            RefreshZoneVisuals(false);
            SetSideInteractable(playerZoneButtons, true);
            SetSideInteractable(opponentZoneButtons, false);

            opponentRoutine = StartCoroutine(OpponentChoiceRoutine());
            timerRoutine = StartCoroutine(TimerRoutine(Mathf.Max(0.5f, timeoutSeconds)));
        }

        public void Cancel()
        {
            Hide();
        }

        public void SetCharacterSprites(Sprite playerSprite, Sprite opponentSprite, bool flipOpponent)
        {
            if (playerCharacterModelView != null)
                playerCharacterModelView.Hide();
            if (opponentCharacterModelView != null)
                opponentCharacterModelView.Hide();

            ApplyCharacterSprite(playerCharacterImage, playerSprite, false);
            ApplyCharacterSprite(opponentCharacterImage, opponentSprite, flipOpponent);
        }

        public void SetCharacters(
            BattleCharacterDatabase.BattleCharacterData player,
            BattleCharacterDatabase.BattleCharacterData opponent,
            bool flipOpponent)
        {
            playerCharacterData = player;
            opponentCharacterData = opponent;
            flipOpponentCharacter = flipOpponent;

            if (root == null || !root.activeInHierarchy)
                return;

            ApplyPendingCharacters();
        }

        private void ApplyPendingCharacters()
        {
            bool playerModelShown = ApplyCharacterModel(
                playerCharacterData,
                ref playerCharacterModelView,
                playerCharacterImage,
                false);

            bool opponentModelShown = ApplyCharacterModel(
                opponentCharacterData,
                ref opponentCharacterModelView,
                opponentCharacterImage,
                flipOpponentCharacter);

            if (!playerModelShown)
                ApplyCharacterSprite(playerCharacterImage, ResolveFallbackSprite(playerCharacterData), false);

            if (!opponentModelShown)
                ApplyCharacterSprite(opponentCharacterImage, ResolveFallbackSprite(opponentCharacterData), flipOpponentCharacter);
        }

        private void SelectPlayerZone(BattleHitZone zone)
        {
            if (!awaitingChoice || playerReady)
                return;

            playerZone = zone;
            playerReady = true;
            SetSideInteractable(playerZoneButtons, false);
            RefreshZoneVisuals(false);
            TryCompleteSelection(false);
        }

        private IEnumerator OpponentChoiceRoutine()
        {
            float maxDelay = Mathf.Max(opponentMinThinkDelay, opponentMaxThinkDelay);
            float delay = UnityEngine.Random.Range(Mathf.Max(0f, opponentMinThinkDelay), maxDelay);
            yield return new WaitForSeconds(delay);

            if (!awaitingChoice || opponentReady)
                yield break;

            opponentZone = GetRandomHitZone();
            opponentReady = true;
            RefreshZoneVisuals(false);
            TryCompleteSelection(false);
        }

        private IEnumerator TimerRoutine(float timeoutSeconds)
        {
            float remaining = timeoutSeconds;
            while (remaining > 0f && awaitingChoice)
            {
                if (timerText != null)
                    timerText.text = Mathf.CeilToInt(remaining).ToString();

                remaining -= Time.deltaTime;
                yield return null;
            }

            if (!awaitingChoice)
                yield break;

            if (!playerReady)
            {
                playerZone = GetRandomHitZone();
                playerReady = true;
            }

            if (!opponentReady)
            {
                opponentZone = GetRandomHitZone();
                opponentReady = true;
            }

            TryCompleteSelection(true);
        }

        private void TryCompleteSelection(bool forced)
        {
            if (!awaitingChoice || !playerReady || !opponentReady)
                return;

            awaitingChoice = false;
            StopChoiceRoutines();

            if (attackerSide == BattleBoardSide.Player)
            {
                attackZone = playerZone;
                parryZone = opponentZone;
            }
            else
            {
                attackZone = opponentZone;
                parryZone = playerZone;
            }

            bool parried = attackZone == parryZone;
            RefreshZoneVisuals(true);
            PlayResultCharacterAnimation(parried);

            if (titleText != null)
                titleText.text = parried ? "PARRIED" : "DAMAGE";

            if (hintText != null)
                hintText.text = forced
                    ? "Time is up"
                    : parried ? "Parry zone matched the attack" : "Parry zone missed the attack";

            if (timerText != null)
                timerText.text = string.Empty;

            revealRoutine = StartCoroutine(RevealRoutine(parried));
        }

        private void PlayResultCharacterAnimation(bool parried)
        {
            if (parried)
                return;

            BattleCharacterModelView attacker = attackerSide == BattleBoardSide.Player
                ? playerCharacterModelView
                : opponentCharacterModelView;
            BattleCharacterModelView defender = attackerSide == BattleBoardSide.Player
                ? opponentCharacterModelView
                : playerCharacterModelView;

            if (attacker != null)
                attacker.PlayAttackAnimation();

            if (defender != null)
                defender.PlayHitAnimation();
        }

        private IEnumerator RevealRoutine(bool parried)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, revealDuration));

            Action<BattleHitZone, BattleHitZone, bool> callback = completed;
            Hide();
            callback?.Invoke(attackZone, parryZone, parried);
        }

        private void RefreshTexts()
        {
            if (titleText != null)
                titleText.text = "Choose zone";

            if (hintText != null)
                hintText.text = attackerSide == BattleBoardSide.Player
                    ? "You attack, enemy parries"
                    : "Enemy attacks, you parry";

            if (playerLabelText != null)
                playerLabelText.text = attackerSide == BattleBoardSide.Player ? "You attack" : "You parry";

            if (opponentLabelText != null)
                opponentLabelText.text = attackerSide == BattleBoardSide.Player ? "Enemy parries" : "Enemy attacks";
        }

        private void RefreshZoneVisuals(bool reveal)
        {
            for (int i = 0; i < ZoneCount; i++)
            {
                BattleHitZone zone = (BattleHitZone)i;
                ApplyZoneVisual(playerZoneButtons, zone, reveal, playerReady, zone == playerZone, false);
                ApplyZoneVisual(opponentZoneButtons, zone, reveal, opponentReady, zone == opponentZone, true);
            }
        }

        private void ApplyZoneVisual(
            Button[] buttons,
            BattleHitZone zone,
            bool reveal,
            bool ready,
            bool selected,
            bool hideSelectionUntilReveal)
        {
            Button button = GetButton(buttons, zone);
            if (button == null)
                return;

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();

            if (image != null)
            {
                image.sprite = ResolveZoneSprite(zone);
                image.preserveAspect = image.sprite != null;
                if (!ready)
                    image.color = hiddenColor;
                else if (!reveal && hideSelectionUntilReveal)
                    image.color = hiddenColor;
                else if (!reveal)
                    image.color = selected ? chosenColor : neutralColor;
                else if (selected && attackZone == parryZone)
                    image.color = parriedColor;
                else if (selected)
                    image.color = damageColor;
                else
                    image.color = neutralColor;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = ResolveZoneLabel(zone);
        }

        private void BuildRuntimeUi(Canvas canvas)
        {
            if (canvas == null)
                return;

            ResolveDefaultArtSprites();

            RectTransform self = transform as RectTransform;
            if (self != null)
            {
                self.anchorMin = Vector2.zero;
                self.anchorMax = Vector2.one;
                self.offsetMin = Vector2.zero;
                self.offsetMax = Vector2.zero;
            }

            if (root == null)
            {
                root = new GameObject("BattleParryChoiceRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                root.transform.SetParent(transform, false);
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = parryPanelSprite != null ? new Vector2(760f, 520f) : new Vector2(900f, 430f);

            Image background = root.GetComponent<Image>();
            background.sprite = null;
            background.preserveAspect = false;
            background.color = parryPanelSprite != null ? new Color(0f, 0f, 0f, 0f) : new Color(0f, 0f, 0f, 0.82f);
            background.raycastTarget = true;

            titleText = titleText != null ? titleText : CreateText(root.transform, "Title", new Vector2(0f, 214f), new Vector2(440f, 42f), 34f, TextAlignmentOptions.Center);
            hintText = hintText != null ? hintText : CreateText(root.transform, "Hint", new Vector2(0f, 178f), new Vector2(440f, 28f), 20f, TextAlignmentOptions.Center);
            timerText = timerText != null ? timerText : CreateText(root.transform, "Timer", new Vector2(0f, 138f), new Vector2(120f, 34f), 28f, TextAlignmentOptions.Center);
            playerCharacterImage = playerCharacterImage != null ? playerCharacterImage : CreateCharacterImage(root.transform, "PlayerParryCharacter", new Vector2(-315f, -48f), new Vector2(118f, 270f), false);
            opponentCharacterImage = opponentCharacterImage != null ? opponentCharacterImage : CreateCharacterImage(root.transform, "OpponentParryCharacter", new Vector2(315f, -48f), new Vector2(118f, 270f), true);
            ConfigureRect(titleText != null ? titleText.rectTransform : null, new Vector2(0f, 214f), new Vector2(440f, 42f));
            ConfigureRect(hintText != null ? hintText.rectTransform : null, new Vector2(0f, 178f), new Vector2(440f, 28f));
            ConfigureRect(timerText != null ? timerText.rectTransform : null, new Vector2(0f, 138f), new Vector2(120f, 34f));
            ConfigureRect(playerCharacterImage != null ? playerCharacterImage.rectTransform : null, new Vector2(-315f, -48f), new Vector2(118f, 270f));
            ConfigureRect(opponentCharacterImage != null ? opponentCharacterImage.rectTransform : null, new Vector2(315f, -48f), new Vector2(118f, 270f));

            Transform playerPanel = CreateSidePanel(root.transform, "PlayerZones", new Vector2(-130f, -58f));
            Transform opponentPanel = CreateSidePanel(root.transform, "OpponentZones", new Vector2(130f, -58f));

            playerLabelText = playerLabelText != null ? playerLabelText : CreateText(playerPanel, "Label", new Vector2(0f, 130f), new Vector2(186f, 24f), 18f, TextAlignmentOptions.Center);
            opponentLabelText = opponentLabelText != null ? opponentLabelText : CreateText(opponentPanel, "Label", new Vector2(0f, 130f), new Vector2(186f, 24f), 18f, TextAlignmentOptions.Center);
            ConfigureRect(playerLabelText != null ? playerLabelText.rectTransform : null, new Vector2(0f, 130f), new Vector2(186f, 24f));
            ConfigureRect(opponentLabelText != null ? opponentLabelText.rectTransform : null, new Vector2(0f, 130f), new Vector2(186f, 24f));

            EnsureZoneButtons(playerPanel, playerZoneButtons);
            EnsureZoneButtons(opponentPanel, opponentZoneButtons);
            BindButtons();
            Hide();
        }

        private Transform CreateSidePanel(Transform parent, string objectName, Vector2 anchoredPosition)
        {
            Transform existing = parent.Find(objectName);
            GameObject panel = existing != null
                ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = parryPanelSprite != null ? new Vector2(190f, 285f) : new Vector2(270f, 260f);

            Image image = panel.GetComponent<Image>();
            image.sprite = parryPanelSprite;
            image.preserveAspect = parryPanelSprite != null;
            image.color = parryPanelSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.08f);
            image.raycastTarget = false;

            return panel.transform;
        }

        private Image CreateCharacterImage(
            Transform parent,
            string objectName,
            Vector2 anchoredPosition,
            Vector2 size,
            bool flipX)
        {
            Transform existing = parent.Find(objectName);
            GameObject imageObject = existing != null
                ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            imageObject.transform.SetParent(parent, false);
            imageObject.transform.SetSiblingIndex(1);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            ApplyCharacterSprite(image, image.sprite, flipX);

            return image;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static bool ApplyCharacterModel(
            BattleCharacterDatabase.BattleCharacterData data,
            ref BattleCharacterModelView modelView,
            Image anchorImage,
            bool flipX)
        {
            if (anchorImage == null || data == null)
            {
                if (modelView != null)
                    modelView.Hide();

                return false;
            }

            EnsureCharacterModelView(anchorImage, ref modelView);
            bool shown = modelView != null &&
                         modelView.Show(data, BattleCharacterModelView.ModelContext.Battle, flipX);

            if (shown)
            {
                anchorImage.enabled = false;
                anchorImage.raycastTarget = false;
            }

            return shown;
        }

        private static void EnsureCharacterModelView(Image anchorImage, ref BattleCharacterModelView modelView)
        {
            if (anchorImage == null)
                return;

            if (modelView == null)
                modelView = anchorImage.GetComponent<BattleCharacterModelView>();

            if (modelView == null)
                modelView = anchorImage.gameObject.AddComponent<BattleCharacterModelView>();
        }

        private static Sprite ResolveFallbackSprite(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return null;

            if (data.BattleSprite != null)
                return data.BattleSprite;
            if (data.LobbySprite != null)
                return data.LobbySprite;

            return data.SelectSprite;
        }

        private static void ApplyCharacterSprite(Image image, Sprite sprite, bool flipX)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;

            RectTransform rect = image.rectTransform;
            if (rect == null)
                return;

            Vector3 scale = rect.localScale;
            scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
            rect.localScale = scale;
        }

        private void EnsureZoneButtons(Transform parent, Button[] buttons)
        {
            for (int i = 0; i < ZoneCount; i++)
            {
                BattleHitZone zone = (BattleHitZone)i;
                string objectName = ResolveZoneLabel(zone) + "Zone";
                Transform existing = parent.Find(objectName);
                GameObject buttonObject = existing != null
                    ? existing.gameObject
                    : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

                buttonObject.transform.SetParent(parent, false);

                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 70f - i * 72f);
                rect.sizeDelta = ResolveZoneSprite(zone) != null ? new Vector2(66f, 66f) : new Vector2(58f, 58f);

                Image image = buttonObject.GetComponent<Image>();
                image.color = neutralColor;
                image.sprite = ResolveZoneSprite(zone);
                image.preserveAspect = image.sprite != null;
                image.raycastTarget = true;

                Button button = buttonObject.GetComponent<Button>();
                button.targetGraphic = image;
                buttons[i] = button;

                TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    label = CreateText(buttonObject.transform, "Label", Vector2.zero, Vector2.zero, 18f, TextAlignmentOptions.Center);

                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label.text = ResolveZoneLabel(zone);
                label.color = image.sprite != null ? new Color(1f, 1f, 1f, 0f) : Color.black;
            }
        }

        private TMP_Text CreateText(
            Transform parent,
            string objectName,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            return text;
        }

        private void BindButtons()
        {
            BindButtonSet(playerZoneButtons);
            BindButtonSet(opponentZoneButtons);
        }

        private void BindButtonSet(Button[] buttons)
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                BattleHitZone zone = (BattleHitZone)i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectPlayerZone(zone));
            }
        }

        private void SetSideInteractable(Button[] buttons, bool interactable)
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    buttons[i].interactable = interactable;
            }
        }

        private void Hide()
        {
            awaitingChoice = false;
            completed = null;
            StopRunningRoutines();

            if (root != null)
                root.SetActive(false);
        }

        private void StopRunningRoutines()
        {
            StopChoiceRoutines();

            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
        }

        private void StopChoiceRoutines()
        {
            if (opponentRoutine != null)
            {
                StopCoroutine(opponentRoutine);
                opponentRoutine = null;
            }

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }

        private static Button GetButton(Button[] buttons, BattleHitZone zone)
        {
            int index = (int)zone;
            if (buttons == null || index < 0 || index >= buttons.Length)
                return null;

            return buttons[index];
        }

        private static BattleHitZone GetRandomHitZone()
        {
            return (BattleHitZone)UnityEngine.Random.Range(0, ZoneCount);
        }

        private static string ResolveZoneLabel(BattleHitZone zone)
        {
            switch (zone)
            {
                case BattleHitZone.Top:
                    return "Top";
                case BattleHitZone.Middle:
                    return "Mid";
                case BattleHitZone.Bottom:
                    return "Low";
                default:
                    return zone.ToString();
            }
        }

        private Sprite ResolveZoneSprite(BattleHitZone zone)
        {
            switch (zone)
            {
                case BattleHitZone.Top:
                    return parryHeadSprite;
                case BattleHitZone.Middle:
                    return parryBodySprite;
                case BattleHitZone.Bottom:
                    return parryLegsSprite;
                default:
                    return null;
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ResolveDefaultArtSprites()
        {
#if UNITY_EDITOR
            parryPanelSprite = parryPanelSprite != null
                ? parryPanelSprite
                : LoadEditorSprite("Assets/Scripts/Mahjong/Sprites/Parry/ParryPanel.png");
            parryHeadSprite = parryHeadSprite != null
                ? parryHeadSprite
                : LoadEditorSprite("Assets/Scripts/Mahjong/Sprites/Parry/ParryHead.png");
            parryBodySprite = parryBodySprite != null
                ? parryBodySprite
                : LoadEditorSprite("Assets/Scripts/Mahjong/Sprites/Parry/ParryBody.png");
            parryLegsSprite = parryLegsSprite != null
                ? parryLegsSprite
                : LoadEditorSprite("Assets/Scripts/Mahjong/Sprites/Parry/ParryLegs.png");
#endif
        }

#if UNITY_EDITOR
        private static Sprite LoadEditorSprite(string path)
        {
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite nestedSprite)
                    return nestedSprite;
            }

            return null;
        }
#endif
    }
}
