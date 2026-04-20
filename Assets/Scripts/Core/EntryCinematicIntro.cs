using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class EntryCinematicIntro : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(1f)] private float duration = 18f;
        [SerializeField, Min(0.05f)] private float fadeOutDuration = 0.75f;

        [Header("Look")]
        [SerializeField] private Color gold = new Color(1f, 0.72f, 0.26f, 1f);
        [SerializeField] private Color silver = new Color(0.86f, 0.9f, 0.94f, 1f);
        [SerializeField] private Color redPulse = new Color(1f, 0.08f, 0.055f, 1f);

        [Header("Options")]
        [SerializeField] private bool playSound = true;
        [SerializeField] private bool playGeneratedSoundOnMobile = false;
        [SerializeField] private bool showSkipButton = true;
        [SerializeField] private bool showIntroText = true;
        [SerializeField] private bool enableStarTwinkle = false;
        [SerializeField] private bool enableMotionParallax = false;

        [Header("Canva Sprite Replacements")]
        [SerializeField] private Sprite ozkullarCompanySprite;
        [SerializeField] private Sprite withSprite;
        [SerializeField] private Sprite ozkullarCreativeAgencySprite;
        [SerializeField] private Sprite productionCreditsSprite;
        [SerializeField] private Sprite presentsSprite;
        [SerializeField] private Sprite symbiyozSprite;
        [SerializeField] private Sprite gatewayTaglineSprite;
        [SerializeField] private Sprite madeForDynastySprite;

        [Header("Canva Background")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color backgroundTint = Color.white;
        [SerializeField, Range(0f, 1f)] private float backgroundMaxAlpha = 1f;
        [SerializeField, Min(0f)] private float backgroundFadeInDuration = 1.35f;
        [SerializeField] private bool backgroundScrollLoop = true;
        [SerializeField] private float backgroundScrollSpeed = 24f;
        [SerializeField, Min(1f)] private float backgroundLoopHeight = 1080f;
        [SerializeField] private float backgroundStartOffsetY = 0f;
        [SerializeField, Min(0f)] private float parallaxMaxOffset = 18f;
        [SerializeField, Min(0.01f)] private float parallaxSmooth = 3.25f;
        [SerializeField, Range(0f, 1f)] private float starTwinkleAlpha = 0.42f;

        private CanvasGroup rootGroup;
        private RectTransform rootRect;
        private CinematicIntroGraphic visual;
        private RawImage backgroundGraphic;
        private RawImage backgroundLoopGraphic;
        private StarTwinkleGraphic starTwinkleGraphic;
        private GameObject introRoot;
        private TextMeshProUGUI primaryText;
        private TextMeshProUGUI secondaryText;
        private TextMeshProUGUI creditText;
        private TextMeshProUGUI microText;
        private TextMeshProUGUI presentsText;
        private SpriteRuntimeGraphic primarySprite;
        private SpriteRuntimeGraphic secondarySprite;
        private SpriteRuntimeGraphic creditSprite;
        private SpriteRuntimeGraphic microSprite;
        private SpriteRuntimeGraphic presentsSpriteGraphic;
        private Button skipButton;
        private AudioSource audioSource;
        private AudioClip generatedIntroClip;
        private Coroutine playRoutine;
        private Vector2 parallaxOffset;
        private bool finished;
        private bool fadeComplete;
        private bool built;

        public bool IsFinished => finished;

        private void OnDisable()
        {
            ClearEditorSelection();
            StopIntroRuntime();
        }

        private void OnDestroy()
        {
            ClearEditorSelection();
            StopIntroRuntime();
            DestroyIntroCanvas();
        }

        public IEnumerator Play()
        {
            Build();

            if (!built || rootGroup == null || rootRect == null)
            {
                finished = true;
                fadeComplete = true;
                yield break;
            }

            finished = false;
            fadeComplete = false;
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
            rootGroup.gameObject.SetActive(true);
            rootRect.SetAsLastSibling();
            UpdateVisuals(0f);
            Debug.Log("[EntryCinematicIntro] Started. Background=" + (backgroundSprite != null ? backgroundSprite.name : "none") + ", Top overlay canvas enabled.");

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(PlayRoutine());

            while (!finished)
                yield return null;

            while (!fadeComplete)
                yield return null;
        }

        public void Skip()
        {
            if (finished)
                return;

            Finish(immediate: false);
        }

        public void ReleaseHeavyReferences()
        {
            ReleaseGeneratedAudioClip();
            ozkullarCompanySprite = null;
            withSprite = null;
            ozkullarCreativeAgencySprite = null;
            productionCreditsSprite = null;
            presentsSprite = null;
            symbiyozSprite = null;
            gatewayTaglineSprite = null;
            madeForDynastySprite = null;
            backgroundSprite = null;
        }

        private IEnumerator PlayRoutine()
        {
            if (playSound && (!Application.isMobilePlatform || playGeneratedSoundOnMobile))
                PlayIntroAudio();

            float time = 0f;
            while (time < duration && !finished)
            {
                time += Time.unscaledDeltaTime;
                UpdateVisuals(time);
                yield return null;
            }

            if (!finished)
                Finish(immediate: false);
        }

        private void Build()
        {
            if (built)
                return;

            EnsureEventSystem();
            DestroyStaleIntroCanvases();

            GameObject root = new GameObject("EntryCinematicIntroCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            introRoot = root;
            int uiLayer = LayerMask.NameToLayer("UI");
            root.layer = uiLayer >= 0 ? uiLayer : 0;
            root.hideFlags = HideFlags.DontSave;

            rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Canvas introCanvas = root.GetComponent<Canvas>();
            introCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            introCanvas.overrideSorting = true;
            introCanvas.sortingOrder = 32767;
            introCanvas.pixelPerfect = false;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
            rootGroup.ignoreParentGroups = true;

            backgroundGraphic = CreateBackgroundGraphic(root.transform, "CanvaBackground");
            backgroundLoopGraphic = CreateBackgroundGraphic(root.transform, "CanvaBackgroundLoop");
            starTwinkleGraphic = CreateStarTwinkle(root.transform);
            visual = CreateCinematicEffects(root.transform);
            visual.gameObject.SetActive(false);
            primaryText = CreateText(root.transform, "PrimaryTitle", 106f, FontStyles.Bold, TextAlignmentOptions.Center, gold);
            secondaryText = CreateText(root.transform, "SecondaryLine", 38f, FontStyles.Normal, TextAlignmentOptions.Center, silver);
            creditText = CreateText(root.transform, "CreditLine", 25f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.9f, 0.92f, 0.96f, 0.8f));
            microText = CreateText(root.transform, "MicroLore", 24f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.72f));
            presentsText = CreateText(root.transform, "PresentsLine", 27f, FontStyles.Bold, TextAlignmentOptions.Center, gold);
            primarySprite = CreateSpriteGraphic(root.transform, "PrimarySprite");
            secondarySprite = CreateSpriteGraphic(root.transform, "SecondarySprite");
            creditSprite = CreateSpriteGraphic(root.transform, "CreditSprite");
            microSprite = CreateSpriteGraphic(root.transform, "MicroSprite");
            presentsSpriteGraphic = CreateSpriteGraphic(root.transform, "PresentsSprite");

            SetTextRect(backgroundGraphic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(2400f, 1080f));
            SetTextRect(backgroundLoopGraphic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(2400f, 1080f));
            SetTextRect(starTwinkleGraphic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(2400f, 1080f));
            SetTextRect(primaryText.rectTransform, new Vector2(0.5f, 0.73f), new Vector2(1700f, 160f));
            SetTextRect(secondaryText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1500f, 80f));
            SetTextRect(creditText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(1500f, 86f));
            SetTextRect(microText.rectTransform, new Vector2(0.5f, 0.465f), new Vector2(1500f, 82f));
            SetTextRect(presentsText.rectTransform, new Vector2(0.5f, 0.39f), new Vector2(1000f, 54f));
            SetTextRect(primarySprite.rectTransform, new Vector2(0.5f, 0.73f), new Vector2(1700f, 160f));
            SetTextRect(secondarySprite.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1500f, 80f));
            SetTextRect(creditSprite.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(1500f, 86f));
            SetTextRect(microSprite.rectTransform, new Vector2(0.5f, 0.465f), new Vector2(1500f, 82f));
            SetTextRect(presentsSpriteGraphic.rectTransform, new Vector2(0.5f, 0.39f), new Vector2(1000f, 54f));

            if (showSkipButton)
                skipButton = CreateSkipButton(root.transform);

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.5f;

            built = true;
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            textObject.layer = parent.gameObject.layer;
            textObject.hideFlags = HideFlags.DontSave;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(14f, fontSize * 0.42f);
            text.fontSizeMax = fontSize;
            text.characterSpacing = 7f;
            text.extraPadding = true;
            text.outlineColor = new Color(0f, 0f, 0f, 0.72f);
            text.outlineWidth = 0.16f;
            return text;
        }

        private SpriteRuntimeGraphic CreateSpriteGraphic(Transform parent, string objectName)
        {
            GameObject spriteObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(SpriteRuntimeGraphic));
            spriteObject.transform.SetParent(parent, false);
            spriteObject.layer = parent.gameObject.layer;
            spriteObject.hideFlags = HideFlags.DontSave;

            SpriteRuntimeGraphic graphic = spriteObject.GetComponent<SpriteRuntimeGraphic>();
            graphic.raycastTarget = false;
            graphic.color = Color.clear;
            return graphic;
        }

        private RawImage CreateBackgroundGraphic(Transform parent, string objectName)
        {
            GameObject backgroundObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.layer = parent.gameObject.layer;
            backgroundObject.hideFlags = HideFlags.DontSave;

            RawImage graphic = backgroundObject.GetComponent<RawImage>();
            graphic.raycastTarget = false;
            graphic.color = Color.clear;
            graphic.uvRect = new Rect(0f, 0f, 1f, 1f);
            return graphic;
        }

        private StarTwinkleGraphic CreateStarTwinkle(Transform parent)
        {
            GameObject starObject = new GameObject("StarTwinkleOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(StarTwinkleGraphic));
            starObject.transform.SetParent(parent, false);
            starObject.layer = parent.gameObject.layer;
            starObject.hideFlags = HideFlags.DontSave;

            StarTwinkleGraphic graphic = starObject.GetComponent<StarTwinkleGraphic>();
            graphic.raycastTarget = false;
            graphic.color = Color.white;
            return graphic;
        }

        private Button CreateSkipButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("SkipButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(IntroSkipRaycastGraphic), typeof(Button), typeof(CanvasGroup));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.hideFlags = HideFlags.DontSave;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            rect.sizeDelta = new Vector2(220f, 58f);

            IntroSkipRaycastGraphic hitGraphic = buttonObject.GetComponent<IntroSkipRaycastGraphic>();
            hitGraphic.color = new Color(0f, 0f, 0f, 0f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = hitGraphic;
            button.onClick.AddListener(Skip);

            CanvasGroup buttonGroup = buttonObject.GetComponent<CanvasGroup>();
            buttonGroup.alpha = 0f;
            buttonGroup.blocksRaycasts = true;
            buttonGroup.interactable = true;

            TextMeshProUGUI label = CreateText(buttonObject.transform, "Label", 24f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.78f));
            label.text = "SKIP";
            label.characterSpacing = 5f;
            SetTextRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220f, 58f));

            return button;
        }

        private void SetTextRect(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private void UpdateVisuals(float time)
        {
            UpdateParallax(time);
            ApplyBackground(time);
            ApplyStarTwinkle(time);

            if (visual != null)
            {
                visual.gameObject.SetActive(false);
            }

            if (!showIntroText)
            {
                HideIntroContent();
            }
            else
            {
                TextCue cue = GetCue(time);
                ApplyCue(primaryText, primarySprite, cue.PrimarySprite, cue.Primary, cue.PrimaryAlpha, cue.PrimaryScale, cue.PrimaryColor);
                ApplyCue(secondaryText, secondarySprite, cue.SecondarySprite, cue.Secondary, cue.SecondaryAlpha, 1f, cue.SecondaryColor);
                ApplyCue(creditText, creditSprite, cue.CreditSprite, cue.Credit, cue.CreditAlpha, 1f, cue.CreditColor);
                ApplyCue(microText, microSprite, cue.MicroSprite, cue.Micro, cue.MicroAlpha, 1f, cue.MicroColor);
                ApplyCue(presentsText, presentsSpriteGraphic, cue.PresentsSprite, cue.Presents, cue.PresentsAlpha, 1f, cue.PresentsColor);
            }

            if (skipButton != null)
            {
                float alpha = Mathf.Clamp01((time - 1.1f) / 0.8f) * Mathf.Clamp01((duration - time) / 1.2f);
                CanvasGroup buttonGroup = skipButton.GetComponent<CanvasGroup>();
                if (buttonGroup != null)
                {
                    buttonGroup.alpha = alpha;
                    buttonGroup.blocksRaycasts = alpha > 0.05f;
                    buttonGroup.interactable = alpha > 0.05f;
                }
                TextMeshProUGUI label = skipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.color = new Color(1f, 1f, 1f, 0.35f + alpha * 0.45f);
            }
        }

        private void HideIntroContent()
        {
            SetLayerActive(primaryText, false);
            SetLayerActive(secondaryText, false);
            SetLayerActive(creditText, false);
            SetLayerActive(microText, false);
            SetLayerActive(presentsText, false);
            SetLayerActive(primarySprite, false);
            SetLayerActive(secondarySprite, false);
            SetLayerActive(creditSprite, false);
            SetLayerActive(microSprite, false);
            SetLayerActive(presentsSpriteGraphic, false);
        }

        private static void SetLayerActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }

        private void ApplyCue(TextMeshProUGUI text, SpriteRuntimeGraphic spriteGraphic, Sprite sprite, string value, float alpha, float scale, Color color)
        {
            bool useSprite = sprite != null && sprite.texture != null;

            if (text != null)
            {
                text.gameObject.SetActive(!useSprite);
                text.text = value;
                text.color = new Color(color.r, color.g, color.b, alpha);
                text.rectTransform.localScale = Vector3.one * scale;
            }

            if (spriteGraphic == null)
                return;

            spriteGraphic.gameObject.SetActive(useSprite);
            spriteGraphic.Sprite = sprite;
            spriteGraphic.color = new Color(color.r, color.g, color.b, alpha);
            spriteGraphic.rectTransform.localScale = Vector3.one * scale;
        }

        private void ApplyBackground(float time)
        {
            if (backgroundGraphic == null || backgroundLoopGraphic == null)
                return;

            bool hasBackground = backgroundSprite != null;
            if (visual != null)
                visual.SetExternalBackgroundActive(hasBackground);

            backgroundGraphic.gameObject.SetActive(hasBackground);
            backgroundLoopGraphic.gameObject.SetActive(hasBackground && backgroundScrollLoop);

            if (!hasBackground)
                return;

            float fadeDuration = Mathf.Max(0.01f, backgroundFadeInDuration);
            float fade = Mathf.Clamp01(time / fadeDuration);
            float alpha = Mathf.Lerp(0.88f, 1f, fade) * Mathf.Clamp01(backgroundMaxAlpha);

            backgroundGraphic.texture = backgroundSprite.texture;
            backgroundLoopGraphic.texture = backgroundSprite.texture;
            backgroundGraphic.color = new Color(backgroundTint.r, backgroundTint.g, backgroundTint.b, backgroundTint.a * alpha);
            backgroundLoopGraphic.color = new Color(backgroundTint.r, backgroundTint.g, backgroundTint.b, backgroundTint.a * alpha);

            float loopHeight = Mathf.Max(1f, backgroundLoopHeight);
            float scroll = backgroundScrollLoop ? Mathf.Repeat(time * Mathf.Abs(backgroundScrollSpeed), loopHeight) : 0f;
            float direction = backgroundScrollSpeed >= 0f ? -1f : 1f;
            float baseY = backgroundStartOffsetY + direction * scroll + parallaxOffset.y;

            backgroundGraphic.rectTransform.anchoredPosition = new Vector2(parallaxOffset.x, baseY);
            backgroundLoopGraphic.rectTransform.anchoredPosition = new Vector2(parallaxOffset.x, baseY - direction * loopHeight);
            backgroundGraphic.rectTransform.SetAsFirstSibling();
            backgroundLoopGraphic.rectTransform.SetSiblingIndex(1);
        }

        private void ApplyStarTwinkle(float time)
        {
            if (starTwinkleGraphic == null)
                return;

            bool visible = enableStarTwinkle && backgroundSprite != null;
            starTwinkleGraphic.gameObject.SetActive(visible);
            if (!visible)
                return;

            starTwinkleGraphic.rectTransform.anchoredPosition = parallaxOffset * 1.35f;
            starTwinkleGraphic.SetTwinkle(time, starTwinkleAlpha);
            starTwinkleGraphic.rectTransform.SetSiblingIndex(2);
        }

        private void UpdateParallax(float time)
        {
            if (!enableMotionParallax || parallaxMaxOffset <= 0f)
            {
                parallaxOffset = Vector2.Lerp(parallaxOffset, Vector2.zero, Time.unscaledDeltaTime * parallaxSmooth);
                return;
            }

            Vector2 input = Vector2.zero;

            TryReadMotionInput(ref input);

            input = Vector2.ClampMagnitude(input, 1f);
            Vector2 target = input * parallaxMaxOffset;
            parallaxOffset = Vector2.Lerp(parallaxOffset, target, Time.unscaledDeltaTime * parallaxSmooth);
        }

        private static void TryReadMotionInput(ref Vector2 input)
        {
            try
            {
                if (SystemInfo.supportsAccelerometer)
                {
                    Vector3 acceleration = Input.acceleration;
                    input = new Vector2(acceleration.x, acceleration.y);
                }

                if (Application.isEditor)
                {
                    Vector2 mouse = Input.mousePosition;
                    if (Screen.width > 0 && Screen.height > 0)
                        input = new Vector2((mouse.x / Screen.width - 0.5f) * 2f, (mouse.y / Screen.height - 0.5f) * 2f);
                }
            }
            catch (System.InvalidOperationException)
            {
                input = Vector2.zero;
            }
        }

        private CinematicIntroGraphic CreateCinematicEffects(Transform parent)
        {
            GameObject effectsObject = new GameObject("CinematicEffects", typeof(RectTransform), typeof(CanvasRenderer), typeof(CinematicIntroGraphic));
            effectsObject.transform.SetParent(parent, false);
            effectsObject.layer = parent.gameObject.layer;
            effectsObject.hideFlags = HideFlags.DontSave;

            RectTransform rect = effectsObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CinematicIntroGraphic effects = effectsObject.GetComponent<CinematicIntroGraphic>();
            effects.raycastTarget = false;
            effects.Configure(gold, silver, redPulse);
            return effects;
        }

        private TextCue GetCue(float time)
        {
            TextCue cue = default;
            cue.PrimaryColor = gold;
            cue.SecondaryColor = silver;
            cue.CreditColor = new Color(0.9f, 0.92f, 0.96f, 1f);
            cue.MicroColor = new Color(1f, 1f, 1f, 0.72f);

            if (time < 0.35f)
            {
                cue.Micro = string.Empty;
                cue.MicroAlpha = 0f;
                return cue;
            }

            if (time < 10.6f)
            {
                float outAlpha = Mathf.Clamp01((10.45f - time) / 0.85f);
                float companyAlpha = Mathf.Clamp01((time - 0.45f) / 0.75f) * outAlpha;
                float withAlpha = Mathf.Clamp01((time - 1.1f) / 0.75f) * outAlpha;
                float agencyAlpha = Mathf.Clamp01((time - 1.75f) / 0.85f) * outAlpha;
                float creditsAlpha = Mathf.Clamp01((time - 2.55f) / 0.9f) * Mathf.Clamp01((10.45f - time) / 0.85f);

                cue.Primary = "\u00D6ZKULLAR COMPANY";
                cue.PrimarySprite = ozkullarCompanySprite;
                cue.PrimaryAlpha = companyAlpha;
                cue.PrimaryScale = Mathf.Lerp(0.975f, 1.01f, Mathf.Clamp01((time - 0.45f) / 4.8f));
                cue.PrimaryColor = gold;

                cue.Secondary = "WITH";
                cue.SecondarySprite = withSprite;
                cue.SecondaryAlpha = withAlpha * 0.72f;
                cue.SecondaryColor = silver;

                cue.Credit = "\u00D6ZKULLAR CREATIVE AGENCY";
                cue.CreditSprite = ozkullarCreativeAgencySprite;
                cue.CreditAlpha = agencyAlpha;
                cue.CreditColor = new Color(0.9f, 0.93f, 0.98f, 1f);

                cue.Micro = "Development and construction: BlackYang.\nArt and design: WhiteYin.";
                cue.MicroSprite = productionCreditsSprite;
                cue.MicroAlpha = creditsAlpha * 0.86f;
                cue.MicroColor = new Color(0.95f, 0.92f, 0.82f, 1f);
                cue.Presents = "PRESENTS";
                cue.PresentsSprite = presentsSprite;
                cue.PresentsAlpha = creditsAlpha * 0.8f;
                cue.PresentsColor = gold;
                return cue;
            }

            if (time < 14.1f)
            {
                float a = PulseInOut(time, 10.85f, 13.95f);
                float pulse = 1f + Mathf.Sin(time * 5.4f) * 0.012f;
                cue.Primary = "SYMBIYOZ";
                cue.PrimarySprite = symbiyozSprite;
                cue.PrimaryAlpha = a;
                cue.PrimaryScale = pulse;
                cue.PrimaryColor = Color.Lerp(gold, Color.white, Mathf.Clamp01((time - 11.5f) / 1.4f) * 0.28f);

                cue.Secondary = "Gateway to the Universe";
                cue.SecondarySprite = gatewayTaglineSprite;
                cue.SecondaryAlpha = Mathf.Clamp01((time - 11.65f) / 0.9f) * Mathf.Clamp01((14f - time) / 0.65f);
                cue.SecondaryColor = new Color(0.95f, 0.96f, 1f, 1f);
                return cue;
            }

            cue.Micro = "MADE FOR DYNASTY: LEGACY";
            cue.MicroSprite = madeForDynastySprite;
            cue.MicroAlpha = PulseInOut(time, 14.35f, 17.8f) * 0.9f;
            cue.MicroColor = gold;
            return cue;
        }

        private float PulseInOut(float time, float start, float end)
        {
            return Mathf.Clamp01((time - start) / 0.65f) * Mathf.Clamp01((end - time) / 0.65f);
        }

        private void Finish(bool immediate)
        {
            if (finished)
                return;

            ClearEditorSelection();
            finished = true;

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            if (audioSource != null)
                audioSource.Stop();

            ReleaseGeneratedAudioClip();
            StartCoroutine(FadeOutAndDisable(immediate));
        }

        private void StopIntroRuntime()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            if (skipButton != null)
                skipButton.onClick.RemoveListener(Skip);

            if (audioSource != null)
                audioSource.Stop();

            ReleaseGeneratedAudioClip();
            DestroyIntroCanvas();
            finished = true;
            fadeComplete = true;
        }

        private IEnumerator FadeOutAndDisable(bool immediate)
        {
            if (rootGroup == null)
            {
                fadeComplete = true;
                yield break;
            }

            float startAlpha = rootGroup.alpha;
            float fadeTime = immediate ? 0f : fadeOutDuration;
            float time = 0f;

            while (time < fadeTime)
            {
                time += Time.unscaledDeltaTime;
                if (rootGroup == null)
                {
                    fadeComplete = true;
                    yield break;
                }

                rootGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(time / fadeTime));
                yield return null;
            }

            if (rootGroup == null)
            {
                fadeComplete = true;
                yield break;
            }

            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
            ClearEditorSelection();
            ReleaseGeneratedAudioClip();
            DestroyIntroCanvas();
            fadeComplete = true;
        }

        private void DestroyIntroCanvas()
        {
            if (skipButton != null)
                skipButton.onClick.RemoveListener(Skip);

            if (skipButton != null)
                skipButton.gameObject.SetActive(false);

            GameObject rootObject = introRoot != null ? introRoot : rootGroup != null ? rootGroup.gameObject : rootRect != null ? rootRect.gameObject : null;

            if (rootObject == null && skipButton != null)
                rootObject = skipButton.transform.root.gameObject;

            if (rootObject != null)
                rootObject.SetActive(false);

            ReleaseGeneratedAudioClip();
            skipButton = null;
            rootGroup = null;
            rootRect = null;
            visual = null;
            backgroundGraphic = null;
            backgroundLoopGraphic = null;
            starTwinkleGraphic = null;
            introRoot = null;
            primaryText = null;
            secondaryText = null;
            creditText = null;
            microText = null;
            presentsText = null;
            primarySprite = null;
            secondarySprite = null;
            creditSprite = null;
            microSprite = null;
            presentsSpriteGraphic = null;
            built = false;

            if (rootObject == null)
                return;

            if (Application.isPlaying)
                Destroy(rootObject);
            else
                DestroyImmediate(rootObject);

            DestroyStaleIntroCanvases(rootObject);
        }

        private static void DestroyStaleIntroCanvases(GameObject except = null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.gameObject == except || canvas.name != "EntryCinematicIntroCanvas")
                    continue;

                canvas.gameObject.SetActive(false);
                if (Application.isPlaying)
                    Destroy(canvas.gameObject);
                else
                    DestroyImmediate(canvas.gameObject);
            }
        }

        private void PlayIntroAudio()
        {
            if (audioSource == null)
                return;

            ReleaseGeneratedAudioClip();
            generatedIntroClip = CreateIntroClip();
            if (generatedIntroClip == null)
                return;

            audioSource.clip = generatedIntroClip;
            audioSource.Play();
        }

        private void ReleaseGeneratedAudioClip()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                if (audioSource.clip == generatedIntroClip)
                    audioSource.clip = null;
            }

            if (generatedIntroClip == null)
                return;

            AudioClip clip = generatedIntroClip;
            generatedIntroClip = null;

            if (Application.isPlaying)
                Destroy(clip);
            else
                DestroyImmediate(clip);
        }

        private AudioClip CreateIntroClip()
        {
            const int sampleRate = 44100;
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float rise = Mathf.Clamp01(t / 6f);
                float fall = Mathf.Clamp01((duration - t) / 3f);
                float envelope = rise * fall;
                float drone = Mathf.Sin(t * Mathf.PI * 2f * 38f) * 0.09f;
                float harmonic = Mathf.Sin(t * Mathf.PI * 2f * 76f + Mathf.Sin(t * 0.4f) * 0.8f) * 0.035f;
                float heartbeat = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(Mathf.Repeat(t, 1.7f) - 0.12f) * 10f), 5f) * 0.18f;
                float shimmer = Mathf.Sin(t * Mathf.PI * 2f * (410f + Mathf.Sin(t) * 22f)) * 0.018f * Mathf.Clamp01((t - 2f) / 10f);
                data[i] = Mathf.Clamp((drone + harmonic + heartbeat + shimmer) * envelope, -0.45f, 0.45f);
            }

            AudioClip clip = AudioClip.Create("Symbiyoz_EntryIntro_Tone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            eventSystem.transform.SetParent(null);
        }

        private static void ClearEditorSelection()
        {
#if UNITY_EDITOR
            if (Selection.activeObject != null)
                Selection.activeObject = null;
#endif
        }

        private struct TextCue
        {
            public string Primary;
            public string Secondary;
            public string Credit;
            public string Micro;
            public string Presents;
            public Sprite PrimarySprite;
            public Sprite SecondarySprite;
            public Sprite CreditSprite;
            public Sprite MicroSprite;
            public Sprite PresentsSprite;
            public float PrimaryAlpha;
            public float SecondaryAlpha;
            public float CreditAlpha;
            public float MicroAlpha;
            public float PresentsAlpha;
            public float PrimaryScale;
            public Color PrimaryColor;
            public Color SecondaryColor;
            public Color CreditColor;
            public Color MicroColor;
            public Color PresentsColor;
        }
    }

    public sealed class CinematicIntroGraphic : MaskableGraphic
    {
        private const int ParticleCount = 96;

        private readonly Vector2[] seeds = new Vector2[ParticleCount];
        private readonly float[] speeds = new float[ParticleCount];
        private readonly float[] sizes = new float[ParticleCount];

        private Color gold = Color.white;
        private Color silver = Color.white;
        private Color redPulse = Color.red;
        private float time;
        private float normalized;
        private bool seeded;
        private bool externalBackgroundActive;

        public void Configure(Color goldColor, Color silverColor, Color redColor)
        {
            gold = goldColor;
            silver = silverColor;
            redPulse = redColor;
            color = Color.white;
            SeedParticles();
        }

        public void SetTime(float newTime, float newNormalized)
        {
            time = newTime;
            normalized = newNormalized;
            SetVerticesDirty();
        }

        public void SetExternalBackgroundActive(bool active)
        {
            if (externalBackgroundActive == active)
                return;

            externalBackgroundActive = active;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            SeedParticles();

            Rect rect = rectTransform.rect;
            AddVignette(vh, rect);
            AddParticles(vh, rect);
            AddCore(vh, rect);
            AddLightSweep(vh, rect);
        }

        private void SeedParticles()
        {
            if (seeded)
                return;

            for (int i = 0; i < ParticleCount; i++)
            {
                float a = Mathf.Repeat(Mathf.Sin(i * 78.233f) * 43758.5453f, 1f);
                float b = Mathf.Repeat(Mathf.Sin(i * 19.871f + 3.1f) * 24634.6345f, 1f);
                seeds[i] = new Vector2(a, b);
                speeds[i] = Mathf.Lerp(0.25f, 1.15f, Mathf.Repeat(a + b, 1f));
                sizes[i] = Mathf.Lerp(2.2f, 8.5f, Mathf.Repeat(a * 1.7f + b, 1f));
            }

            seeded = true;
        }

        private void AddVignette(VertexHelper vh, Rect rect)
        {
            if (externalBackgroundActive)
                AddQuad(vh, rect.center, rect.size, new Color(0f, 0f, 0f, 0.04f));
            else
                AddQuad(vh, rect.center, rect.size, new Color(0f, 0f, 0f, 1f));

            float sweepAlpha = Mathf.Sin(time * 0.28f) * 0.035f + 0.055f;
            if (externalBackgroundActive)
                sweepAlpha *= 0.45f;

            AddQuad(vh, rect.center + new Vector2(0f, rect.height * 0.08f), new Vector2(rect.width, rect.height * 0.42f), new Color(0.08f, 0.1f, 0.13f, sweepAlpha));
        }

        private void AddParticles(VertexHelper vh, Rect rect)
        {
            float intro = Mathf.Clamp01(time / 2.2f);
            float outro = Mathf.Clamp01((18f - time) / 2f);
            float alphaBase = intro * outro;

            for (int i = 0; i < ParticleCount; i++)
            {
                Vector2 seed = seeds[i];
                float drift = time * speeds[i];
                float x = Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Repeat(seed.x + Mathf.Sin(drift * 0.09f) * 0.06f, 1f));
                float y = Mathf.Lerp(rect.yMin, rect.yMax, Mathf.Repeat(seed.y + drift * 0.012f, 1f));
                float centerBias = 1f - Mathf.Clamp01(Mathf.Abs((x - rect.center.x) / rect.width) * 1.6f);
                float alpha = alphaBase * Mathf.Lerp(0.06f, 0.32f, centerBias) * (0.65f + Mathf.Sin(time * 1.7f + i) * 0.35f);
                Color c = Color.Lerp(silver, gold, Mathf.Repeat(seed.x + seed.y, 1f));
                c.a = alpha;
                AddQuad(vh, new Vector2(x, y), Vector2.one * sizes[i], c);
            }
        }

        private void AddCore(VertexHelper vh, Rect rect)
        {
            float coreIntro = Mathf.Clamp01(time / 2.2f);
            float symbiozRise = Mathf.Clamp01((time - 8f) / 2.2f);
            float pulse = 0.65f + Mathf.Sin(time * 3.7f) * 0.35f;
            Vector2 center = rect.center + new Vector2(0f, rect.height * 0.02f);

            float coreSize = Mathf.Lerp(30f, 260f, Mathf.Max(coreIntro * 0.35f, symbiozRise)) + pulse * 22f;
            Color coreGold = gold;
            coreGold.a = (0.12f + pulse * 0.1f) * Mathf.Clamp01((18f - time) / 3f);
            AddQuad(vh, center, new Vector2(coreSize, coreSize * 0.025f), coreGold);
            AddQuad(vh, center, new Vector2(coreSize * 0.025f, coreSize), coreGold);

            Color red = redPulse;
            red.a = (0.05f + pulse * 0.12f) * coreIntro;
            float redSize = coreSize * (1.4f + pulse * 0.25f);
            AddQuad(vh, center, new Vector2(redSize, 2.5f), red);
            AddQuad(vh, center, new Vector2(2.5f, redSize), red);
        }

        private void AddLightSweep(VertexHelper vh, Rect rect)
        {
            bool active = (time > 2.45f && time < 5.1f) || (time > 8.7f && time < 11.8f);
            if (!active)
                return;

            float local = time > 8f ? Mathf.InverseLerp(8.7f, 11.8f, time) : Mathf.InverseLerp(2.45f, 5.1f, time);
            float x = Mathf.Lerp(rect.xMin - rect.width * 0.25f, rect.xMax + rect.width * 0.25f, local);
            Color c = Color.white;
            c.a = 0.16f * Mathf.Sin(local * Mathf.PI);
            AddQuad(vh, new Vector2(x, rect.center.y + rect.height * 0.05f), new Vector2(rect.width * 0.08f, rect.height * 0.7f), c);
        }

        private void AddQuad(VertexHelper vh, Vector2 center, Vector2 size, Color color)
        {
            int start = vh.currentVertCount;
            Vector2 half = size * 0.5f;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = center + new Vector2(-half.x, -half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(-half.x, half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(half.x, half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(half.x, -half.y);
            vh.AddVert(vertex);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }

    public sealed class IntroSkipRaycastGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }

    public sealed class StarTwinkleGraphic : MaskableGraphic
    {
        private const int StarCount = 80;

        private readonly Vector2[] seeds = new Vector2[StarCount];
        private readonly float[] phases = new float[StarCount];
        private readonly float[] sizes = new float[StarCount];
        private readonly float[] speeds = new float[StarCount];
        private bool seeded;
        private float time;
        private float intensity = 0.75f;

        public void SetTwinkle(float newTime, float newIntensity)
        {
            time = newTime;
            intensity = Mathf.Clamp01(newIntensity);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Seed();

            if (color.a <= 0f || intensity <= 0f)
                return;

            Rect rect = rectTransform.rect;
            for (int i = 0; i < StarCount; i++)
            {
                Vector2 pos = new Vector2(
                    Mathf.Lerp(rect.xMin, rect.xMax, seeds[i].x),
                    Mathf.Lerp(rect.yMin, rect.yMax, seeds[i].y));

                float pulse = 0.5f + Mathf.Sin(time * speeds[i] + phases[i]) * 0.5f;
                pulse = Mathf.SmoothStep(0f, 1f, pulse);
                float alpha = intensity * Mathf.Lerp(0.05f, 0.62f, pulse);
                float size = sizes[i] * Mathf.Lerp(0.72f, 1.55f, pulse);
                Color starColor = new Color(1f, 1f, 1f, alpha * color.a);

                AddQuad(vh, pos, new Vector2(size, size), starColor);

                if (i % 9 == 0)
                {
                    Color flareColor = new Color(1f, 1f, 1f, alpha * 0.38f * color.a);
                    AddQuad(vh, pos, new Vector2(size * 5.2f, 1.25f), flareColor);
                    AddQuad(vh, pos, new Vector2(1.25f, size * 5.2f), flareColor);
                }
            }
        }

        private void Seed()
        {
            if (seeded)
                return;

            for (int i = 0; i < StarCount; i++)
            {
                float x = Mathf.Repeat(Mathf.Sin(i * 37.719f + 1.7f) * 91637.57f, 1f);
                float y = Mathf.Repeat(Mathf.Sin(i * 19.113f + 6.4f) * 47291.31f, 1f);
                seeds[i] = new Vector2(x, y);
                phases[i] = Mathf.Repeat(Mathf.Sin(i * 11.91f) * 7931.17f, 1f) * Mathf.PI * 2f;
                speeds[i] = Mathf.Lerp(0.45f, 1.85f, Mathf.Repeat(x + y * 1.7f, 1f));
                sizes[i] = Mathf.Lerp(1.4f, 4.2f, Mathf.Repeat(x * 2.3f + y, 1f));
            }

            seeded = true;
        }

        private static void AddQuad(VertexHelper vh, Vector2 center, Vector2 size, Color color)
        {
            int start = vh.currentVertCount;
            Vector2 half = size * 0.5f;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = center + new Vector2(-half.x, -half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(-half.x, half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(half.x, half.y);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(half.x, -half.y);
            vh.AddVert(vertex);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }

    public sealed class SpriteRuntimeGraphic : MaskableGraphic
    {
        public enum SpriteFillMode
        {
            PreserveAspect,
            Cover,
            Stretch
        }

        [SerializeField] private Sprite sprite;
        [SerializeField] private SpriteFillMode fillMode = SpriteFillMode.PreserveAspect;

        public SpriteFillMode FillMode
        {
            get => fillMode;
            set
            {
                if (fillMode == value)
                    return;

                fillMode = value;
                SetVerticesDirty();
            }
        }

        public Sprite Sprite
        {
            get => sprite;
            set
            {
                if (sprite == value)
                    return;

                sprite = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (sprite != null && sprite.texture != null)
                    return sprite.texture;

                return Texture2D.whiteTexture;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (sprite == null || sprite.texture == null || color.a <= 0f)
                return;

            Rect drawRect = GetDrawRect(rectTransform.rect, sprite.rect.size, fillMode);
            Rect textureRect = sprite.textureRect;
            Texture texture = sprite.texture;

            Vector2 uvMin = new Vector2(textureRect.xMin / texture.width, textureRect.yMin / texture.height);
            Vector2 uvMax = new Vector2(textureRect.xMax / texture.width, textureRect.yMax / texture.height);

            int start = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector2(drawRect.xMin, drawRect.yMin);
            vertex.uv0 = new Vector2(uvMin.x, uvMin.y);
            vh.AddVert(vertex);

            vertex.position = new Vector2(drawRect.xMin, drawRect.yMax);
            vertex.uv0 = new Vector2(uvMin.x, uvMax.y);
            vh.AddVert(vertex);

            vertex.position = new Vector2(drawRect.xMax, drawRect.yMax);
            vertex.uv0 = new Vector2(uvMax.x, uvMax.y);
            vh.AddVert(vertex);

            vertex.position = new Vector2(drawRect.xMax, drawRect.yMin);
            vertex.uv0 = new Vector2(uvMax.x, uvMin.y);
            vh.AddVert(vertex);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

        private static Rect GetDrawRect(Rect container, Vector2 spriteSize, SpriteFillMode mode)
        {
            if (mode == SpriteFillMode.Stretch || spriteSize.x <= 0f || spriteSize.y <= 0f || container.width <= 0f || container.height <= 0f)
                return container;

            float spriteAspect = spriteSize.x / spriteSize.y;
            float containerAspect = container.width / container.height;
            Rect result = container;

            bool fitByWidth = mode == SpriteFillMode.Cover ? spriteAspect < containerAspect : spriteAspect > containerAspect;

            if (fitByWidth)
            {
                float height = container.width / spriteAspect;
                result.y = container.center.y - height * 0.5f;
                result.height = height;
            }
            else
            {
                float width = container.height * spriteAspect;
                result.x = container.center.x - width * 0.5f;
                result.width = width;
            }

            return result;
        }
    }

    public sealed class SolidRuntimeGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            int start = vh.currentVertCount;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector2(rect.xMin, rect.yMin);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMin, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMin);
            vh.AddVert(vertex);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}

