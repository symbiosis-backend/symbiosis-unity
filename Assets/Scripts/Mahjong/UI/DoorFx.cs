using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class DoorFx : MonoBehaviour
    {
        public static DoorFx I { get; private set; }
        public static event Action<string> SceneTransitionStarted;

        [Header("Links")]
        [SerializeField] private Canvas doorCanvas;
        [SerializeField] private RectTransform leftDoor;
        [SerializeField] private RectTransform rightDoor;
        [SerializeField] private Image fadeImage;
        [SerializeField] private Sprite doorSprite;
        [SerializeField] private string doorSpriteResourcePath = "Mahjong/Sprites/BattleUI/BattleLobbyDoorLeaf";

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float closeDuration = 0.6f;
        [SerializeField, Min(0.05f)] private float openDuration = 0.6f;
        [SerializeField, Min(0f)] private float closedPause = 0.1f;

        [Header("Fade")]
        [SerializeField] private bool useFade = true;
        [SerializeField, Range(0f, 1f)] private float fadeClosedAlpha = 0.15f;

        [Header("Hidden State")]
        [SerializeField, Min(0f)] private float hiddenPadding = 80f;

        [Header("Options")]
        [SerializeField] private bool dontDestroyBetweenScenes = true;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip doorClip;
        [SerializeField] private string doorClipResourcePath = "Mahjong/Music/OpenCloseDoor";
        [SerializeField, Range(0f, 1f)] private float doorVolume = 1f;

        private float halfWidth;
        private float halfHeight;
        private float openedLeftX;
        private float openedRightX;
        private float openedBottomY;
        private float openedTopY;

        private bool isTransitioning;
        private bool shouldOpenAfterLoad;
        private bool keepCurrentDoorState;
        private bool openingAfterLoad;
        private Coroutine routine;
        private Coroutine postLoadRecoveryRoutine;
        private string transitionDoorSpriteResourcePath;
        private string activeDoorSpriteResourcePath;
        private string cachedDoorSpriteResourcePath;
        private Action transitionBeforeSceneLoad;
        private bool transitionReverseDoorMirroring;
        private bool activeReverseDoorMirroring;
        private bool transitionVerticalSplit;
        private bool activeVerticalSplit;
        private bool useRuntimeDoorSprites;

        private const int DoorCanvasSortingOrder = 32760;
        private const int PostLoadCanvasWaitFrames = 12;
        private const float PostLoadOpenRecoveryPaddingSeconds = 3f;
        private const float MaxAnimationDeltaTime = 1f / 30f;
        private const float DoorCenterOverlap = 6f;

        public bool IsBusy => isTransitioning;

        public static DoorFx EnsureRuntime()
        {
            if (I != null)
                return I;

            GameObject root = new GameObject("RuntimeDoorTransition", typeof(RectTransform));
            GameObject canvasObject = new GameObject("DoorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = DoorCanvasSortingOrder;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            Image fade = CreateRuntimeImage(canvasObject.transform, "DoorFade");
            fade.color = new Color(0f, 0f, 0f, 0f);
            RectTransform fadeRect = fade.rectTransform;
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;

            RectTransform left = CreateRuntimeImage(canvasObject.transform, "LeftDoor").rectTransform;
            RectTransform right = CreateRuntimeImage(canvasObject.transform, "RightDoor").rectTransform;

            DoorFx fx = root.AddComponent<DoorFx>();
            fx.ConfigureRuntime(canvas, left, right, fade);
            return fx;
        }

        private static Image CreateRuntimeImage(Transform parent, string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<Image>();
        }

        private void ConfigureRuntime(Canvas canvas, RectTransform left, RectTransform right, Image fade)
        {
            doorCanvas = canvas;
            leftDoor = left;
            rightDoor = right;
            fadeImage = fade;
            useRuntimeDoorSprites = true;
            Init();
            PlaceDoorsOpenedInstant();
        }

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;

            if (dontDestroyBetweenScenes)
                DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            EnsureAudio();
            Init();
            PlaceDoorsOpenedInstant();
        }

        private void OnDestroy()
        {
            if (I == this)
                I = null;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
                return;

            Init();

            if (!keepCurrentDoorState && !isTransitioning)
                PlaceDoorsOpenedInstant();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Init();

            if (!shouldOpenAfterLoad)
            {
                if (isTransitioning || keepCurrentDoorState)
                    RecoverOpenAfterUnexpectedLoad();

                return;
            }

            shouldOpenAfterLoad = false;

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(CoOpenAfterLoad());
        }

        private void Init()
        {
            if (doorCanvas == null)
                doorCanvas = GetComponentInChildren<Canvas>(true);

            if (doorCanvas == null || leftDoor == null || rightDoor == null)
                return;

            doorCanvas.overrideSorting = true;
            doorCanvas.sortingOrder = DoorCanvasSortingOrder;
            NormalizeDoorCanvasRect(doorCanvas.transform as RectTransform);

            if (fadeImage != null)
                fadeImage.raycastTarget = false;

            if (leftDoor.TryGetComponent(out Image leftImage))
            {
                ApplyDoorImage(leftImage, mirrored: activeReverseDoorMirroring);
                leftImage.raycastTarget = false;
            }

            if (rightDoor.TryGetComponent(out Image rightImage))
            {
                ApplyDoorImage(rightImage, mirrored: !activeReverseDoorMirroring);
                rightImage.raycastTarget = false;
            }

            RectTransform canvasRect = doorCanvas.GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();

            Vector2 canvasSize = ResolveLandscapeCanvasSize(canvasRect);
            float canvasWidth = canvasSize.x;
            halfWidth = Mathf.Max(1f, canvasWidth * 0.5f);
            halfHeight = Mathf.Max(1f, canvasSize.y * 0.5f);
            openedLeftX = -(halfWidth + hiddenPadding);
            openedRightX = halfWidth + hiddenPadding;
            openedBottomY = -(halfHeight + hiddenPadding);
            openedTopY = halfHeight + hiddenPadding;

            if (activeVerticalSplit)
            {
                leftDoor.anchorMin = new Vector2(0f, 0f);
                leftDoor.anchorMax = new Vector2(1f, 0.5f);
                leftDoor.pivot = new Vector2(0.5f, 0.5f);
                leftDoor.offsetMin = new Vector2(0f, -DoorCenterOverlap);
                leftDoor.offsetMax = new Vector2(0f, DoorCenterOverlap);

                rightDoor.anchorMin = new Vector2(0f, 0.5f);
                rightDoor.anchorMax = new Vector2(1f, 1f);
                rightDoor.pivot = new Vector2(0.5f, 0.5f);
                rightDoor.offsetMin = new Vector2(0f, -DoorCenterOverlap);
                rightDoor.offsetMax = new Vector2(0f, DoorCenterOverlap);
            }
            else
            {
                leftDoor.anchorMin = new Vector2(0f, 0f);
                leftDoor.anchorMax = new Vector2(0.5f, 1f);
                leftDoor.pivot = new Vector2(0.5f, 0.5f);
                leftDoor.offsetMin = Vector2.zero;
                leftDoor.offsetMax = Vector2.zero;

                rightDoor.anchorMin = new Vector2(0.5f, 0f);
                rightDoor.anchorMax = new Vector2(1f, 1f);
                rightDoor.pivot = new Vector2(0.5f, 0.5f);
                rightDoor.offsetMin = Vector2.zero;
                rightDoor.offsetMax = Vector2.zero;
            }
        }

        private static void NormalizeDoorCanvasRect(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Vector2 ResolveLandscapeCanvasSize(RectTransform canvasRect)
        {
            float rectWidth = canvasRect != null ? Mathf.Abs(canvasRect.rect.width) : 0f;
            float rectHeight = canvasRect != null ? Mathf.Abs(canvasRect.rect.height) : 0f;
            float screenWidth = Mathf.Max(Screen.width, Screen.height);
            float screenHeight = Mathf.Min(Screen.width, Screen.height);

            float width = Mathf.Max(rectWidth, rectHeight, screenWidth, 1f);
            float height = Mathf.Max(Mathf.Min(width, Mathf.Max(Mathf.Min(rectWidth, rectHeight), screenHeight)), 1f);
            return new Vector2(width, height);
        }

        private void ApplyDoorImage(Image image, bool mirrored)
        {
            if (image == null)
                return;

            bool hasExplicitTransitionSprite = !string.IsNullOrWhiteSpace(activeDoorSpriteResourcePath);
            bool shouldLoadSprite = useRuntimeDoorSprites || hasExplicitTransitionSprite || image.sprite == null;
            bool shouldMirror = shouldLoadSprite;
            if (shouldLoadSprite)
            {
                Sprite sprite = LoadDoorSprite();
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                    image.color = Color.white;
                }
            }

            if (shouldMirror)
            {
                RectTransform rect = image.rectTransform;
                Vector3 scale = rect.localScale;
                scale.x = Mathf.Abs(scale.x) * (mirrored ? -1f : 1f);
                rect.localScale = scale;
            }
        }

        private Sprite LoadDoorSprite()
        {
            string baseResourcePath = !string.IsNullOrWhiteSpace(activeDoorSpriteResourcePath)
                ? activeDoorSpriteResourcePath
                : doorSpriteResourcePath;
            string resourcePath = ResolveOrientationResourcePath(baseResourcePath);

            if (doorSprite != null && string.Equals(cachedDoorSpriteResourcePath, resourcePath, StringComparison.Ordinal))
                return doorSprite;

            if (string.IsNullOrWhiteSpace(resourcePath))
                return null;

            cachedDoorSpriteResourcePath = resourcePath;
            doorSprite = Resources.Load<Sprite>(resourcePath);
            if (doorSprite != null)
                return doorSprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                doorSprite = sprites[0];
                return doorSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
                doorSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

            return doorSprite;
        }

        private static string ResolveOrientationResourcePath(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath) ||
                resourcePath.EndsWith("_Landscape", StringComparison.Ordinal) ||
                resourcePath.EndsWith("_Portrait", StringComparison.Ordinal))
                return resourcePath;

            string primarySuffix = IsPortraitScreen() ? "_Portrait" : "_Landscape";
            string fallbackSuffix = IsPortraitScreen() ? "_Landscape" : "_Portrait";

            string orientedPath = resourcePath + primarySuffix;
            if (DoorResourceExists(orientedPath))
                return orientedPath;

            string fallbackPath = resourcePath + fallbackSuffix;
            if (DoorResourceExists(fallbackPath))
                return fallbackPath;

            return resourcePath;
        }

        private static bool IsPortraitScreen()
        {
            if (Screen.orientation == ScreenOrientation.Portrait ||
                Screen.orientation == ScreenOrientation.PortraitUpsideDown)
                return true;

            if (Screen.orientation == ScreenOrientation.LandscapeLeft ||
                Screen.orientation == ScreenOrientation.LandscapeRight)
                return false;

            return Screen.height > Screen.width;
        }

        private static bool DoorResourceExists(string resourcePath)
        {
            return Resources.Load<Sprite>(resourcePath) != null ||
                Resources.Load<Texture2D>(resourcePath) != null ||
                Resources.LoadAll<Sprite>(resourcePath).Length > 0;
        }

        public bool IsReady()
        {
            return doorCanvas != null && leftDoor != null && rightDoor != null;
        }

        public void ForceOpenNow()
        {
            if (routine != null)
                StopCoroutine(routine);

            if (postLoadRecoveryRoutine != null)
                StopCoroutine(postLoadRecoveryRoutine);

            shouldOpenAfterLoad = false;
            ResetTransitionState();
        }

        public static void ForceOpenAll()
        {
            DoorFx[] doors = FindObjectsByType<DoorFx>(FindObjectsInactive.Include);
            for (int i = 0; i < doors.Length; i++)
            {
                DoorFx door = doors[i];
                if (door != null)
                    door.ForceOpenNow();
            }
        }

        public void LoadScene(string sceneName)
        {
            LoadScene(sceneName, null, false);
        }

        public void LoadScene(string sceneName, string doorSpriteResourcePathOverride)
        {
            LoadScene(sceneName, doorSpriteResourcePathOverride, false);
        }

        public void LoadScene(string sceneName, string doorSpriteResourcePathOverride, bool reverseDoorMirroring)
        {
            LoadScene(sceneName, doorSpriteResourcePathOverride, reverseDoorMirroring, null);
        }

        public void LoadScene(string sceneName, string doorSpriteResourcePathOverride, bool reverseDoorMirroring, Action beforeSceneLoad)
        {
            LoadScene(sceneName, doorSpriteResourcePathOverride, reverseDoorMirroring, beforeSceneLoad, false);
        }

        public void LoadScene(string sceneName, string doorSpriteResourcePathOverride, bool reverseDoorMirroring, Action beforeSceneLoad, bool verticalSplit)
        {
            if (isTransitioning || string.IsNullOrWhiteSpace(sceneName))
                return;

            SceneTransitionStarted?.Invoke(sceneName);
            transitionDoorSpriteResourcePath = doorSpriteResourcePathOverride;
            transitionBeforeSceneLoad = beforeSceneLoad;
            transitionReverseDoorMirroring = reverseDoorMirroring;
            transitionVerticalSplit = verticalSplit;
            activeDoorSpriteResourcePath = string.IsNullOrWhiteSpace(transitionDoorSpriteResourcePath)
                ? doorSpriteResourcePath
                : transitionDoorSpriteResourcePath;
            activeReverseDoorMirroring = transitionReverseDoorMirroring;
            activeVerticalSplit = transitionVerticalSplit;
            Init();

            EnsurePersistentDuringTransition();

            if (!IsReady())
            {
                transitionBeforeSceneLoad?.Invoke();
                ResetTransitionState();
                SceneManager.LoadScene(sceneName);
                return;
            }

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(CoCloseThenLoad(sceneName));
        }

        public void RunBetweenLevels(Action action)
        {
            RunBetweenLevels(action, null, false);
        }

        public void RunBetweenLevels(Action action, string doorSpriteResourcePathOverride, bool reverseDoorMirroring = false)
        {
            if (isTransitioning || action == null || !IsReady())
                return;

            transitionDoorSpriteResourcePath = doorSpriteResourcePathOverride;
            transitionReverseDoorMirroring = reverseDoorMirroring;
            activeDoorSpriteResourcePath = string.IsNullOrWhiteSpace(transitionDoorSpriteResourcePath)
                ? doorSpriteResourcePath
                : transitionDoorSpriteResourcePath;
            activeReverseDoorMirroring = transitionReverseDoorMirroring;
            Init();

            EnsurePersistentDuringTransition();

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(CoCloseActionOpen(action));
        }

        private IEnumerator CoCloseThenLoad(string sceneName)
        {
            isTransitioning = true;
            keepCurrentDoorState = true;

            yield return AnimateDoors(isClosing: true);

            if (closedPause > 0f)
                yield return new WaitForSecondsRealtime(closedPause);

            shouldOpenAfterLoad = true;
            transitionBeforeSceneLoad?.Invoke();

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
                yield return null;
        }

        private IEnumerator CoOpenAfterLoad()
        {
            openingAfterLoad = true;
            keepCurrentDoorState = true;
            activeDoorSpriteResourcePath = string.IsNullOrWhiteSpace(transitionDoorSpriteResourcePath)
                ? doorSpriteResourcePath
                : transitionDoorSpriteResourcePath;
            activeReverseDoorMirroring = transitionReverseDoorMirroring;
            activeVerticalSplit = transitionVerticalSplit;

            yield return WaitForPostLoadCanvas();
            Init();
            PlaceDoorsClosedInstant();

            if (!IsReady())
            {
                ResetTransitionState();
                yield break;
            }

            yield return AnimateDoors(isClosing: false);

            ResetTransitionState();
        }

        private IEnumerator CoCloseActionOpen(Action action)
        {
            isTransitioning = true;
            keepCurrentDoorState = true;

            yield return AnimateDoors(isClosing: true);

            if (closedPause > 0f)
                yield return new WaitForSecondsRealtime(closedPause);

            action.Invoke();

            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            if (!IsReady())
            {
                ResetTransitionState();
                yield break;
            }

            yield return AnimateDoors(isClosing: false);

            ResetTransitionState();
        }

        private IEnumerator WaitForPostLoadCanvas()
        {
            for (int i = 0; i < PostLoadCanvasWaitFrames; i++)
            {
                Init();
                Canvas.ForceUpdateCanvases();

                yield return null;
            }

            yield return new WaitForEndOfFrame();
            Init();
        }

        private void RecoverOpenAfterUnexpectedLoad()
        {
            shouldOpenAfterLoad = false;

            if (routine != null)
                StopCoroutine(routine);

            openingAfterLoad = true;
            routine = StartCoroutine(CoOpenAfterLoad());
            StartPostLoadRecoveryWatchdog();
        }

        private void StartPostLoadRecoveryWatchdog()
        {
            if (postLoadRecoveryRoutine != null)
                StopCoroutine(postLoadRecoveryRoutine);

            postLoadRecoveryRoutine = StartCoroutine(CoPostLoadOpenRecoveryWatchdog());
        }

        private IEnumerator CoPostLoadOpenRecoveryWatchdog()
        {
            float maxWait = Mathf.Max(4.5f, openDuration * 4f + PostLoadOpenRecoveryPaddingSeconds);
            float deadline = Time.realtimeSinceStartup + maxWait;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (!openingAfterLoad && !isTransitioning && !keepCurrentDoorState)
                {
                    postLoadRecoveryRoutine = null;
                    yield break;
                }

                yield return null;
            }

            if (openingAfterLoad || isTransitioning || keepCurrentDoorState)
                ResetTransitionState();

            postLoadRecoveryRoutine = null;
        }

        private void ResetTransitionState()
        {
            isTransitioning = false;
            keepCurrentDoorState = false;
            openingAfterLoad = false;
            activeDoorSpriteResourcePath = null;
            transitionDoorSpriteResourcePath = null;
            transitionBeforeSceneLoad = null;
            activeReverseDoorMirroring = false;
            transitionReverseDoorMirroring = false;
            activeVerticalSplit = false;
            transitionVerticalSplit = false;
            Init();
            PlaceDoorsOpenedInstant();
            routine = null;
        }

        private void EnsurePersistentDuringTransition()
        {
            if (!dontDestroyBetweenScenes)
                return;

            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator AnimateDoors(bool isClosing)
        {
            if (!IsReady())
                yield break;

            SetDoorOverlayActive(true);
            SetDoorGraphicsVisible(true);
            PlayDoorSound();
            Init();

            float duration = isClosing ? closeDuration : openDuration;

            float leftFrom = isClosing ? (activeVerticalSplit ? openedBottomY : openedLeftX) : 0f;
            float leftTo = isClosing ? 0f : (activeVerticalSplit ? openedBottomY : openedLeftX);

            float rightFrom = isClosing ? (activeVerticalSplit ? openedTopY : openedRightX) : 0f;
            float rightTo = isClosing ? 0f : (activeVerticalSplit ? openedTopY : openedRightX);

            float fadeFrom = isClosing ? 0f : (useFade ? fadeClosedAlpha : 0f);
            float fadeTo = isClosing ? (useFade ? fadeClosedAlpha : 0f) : 0f;

            SetDoorPositions(leftFrom, rightFrom);
            SetFade(fadeFrom);

            float t = 0f;
            while (t < duration)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, MaxAnimationDeltaTime);
                float p = Mathf.Clamp01(t / duration);
                float e = Mathf.SmoothStep(0f, 1f, p);

                SetDoorPositions(Mathf.Lerp(leftFrom, leftTo, e), Mathf.Lerp(rightFrom, rightTo, e));
                SetFade(Mathf.Lerp(fadeFrom, fadeTo, e));

                yield return null;
            }

            SetDoorPositions(leftTo, rightTo);
            SetFade(fadeTo);

            if (!isClosing)
            {
                SetDoorGraphicsVisible(false);
                SetDoorOverlayActive(false);
            }
        }

        private void PlaceDoorsOpenedInstant()
        {
            if (!IsReady())
                return;

            SetDoorOverlayActive(true);

            if (activeVerticalSplit)
                SetDoorPositions(openedBottomY, openedTopY);
            else
                SetDoorPositions(openedLeftX, openedRightX);

            SetFade(0f);
            SetDoorGraphicsVisible(false);
            SetDoorOverlayActive(false);
        }

        private void PlaceDoorsClosedInstant()
        {
            if (!IsReady())
                return;

            SetDoorOverlayActive(true);
            SetDoorGraphicsVisible(true);
            SetDoorPositions(0f, 0f);
            SetFade(useFade ? fadeClosedAlpha : 0f);
        }

        private void SetDoorPositions(float leftOrBottom, float rightOrTop)
        {
            if (activeVerticalSplit)
            {
                leftDoor.anchoredPosition = new Vector2(0f, leftOrBottom);
                rightDoor.anchoredPosition = new Vector2(0f, rightOrTop);
                return;
            }

            leftDoor.anchoredPosition = new Vector2(leftOrBottom, 0f);
            rightDoor.anchoredPosition = new Vector2(rightOrTop, 0f);
        }

        private void SetDoorGraphicsVisible(bool visible)
        {
            SetDoorGraphicVisible(leftDoor, visible);
            SetDoorGraphicVisible(rightDoor, visible);
        }

        private void SetDoorOverlayActive(bool active)
        {
            if (doorCanvas != null)
                doorCanvas.enabled = active;

            if (doorCanvas != null && doorCanvas.TryGetComponent(out GraphicRaycaster raycaster))
                raycaster.enabled = active;

            if (!active && fadeImage != null)
                fadeImage.raycastTarget = false;
        }

        private static void SetDoorGraphicVisible(RectTransform door, bool visible)
        {
            if (door == null || !door.TryGetComponent(out Image image))
                return;

            image.enabled = visible;
            image.raycastTarget = visible;
        }

        private void SetFade(float alpha)
        {
            if (fadeImage == null)
                return;

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
            fadeImage.raycastTarget = alpha > 0.001f;
        }

        private void EnsureAudio()
        {
            if (doorClip == null && !string.IsNullOrWhiteSpace(doorClipResourcePath))
                doorClip = Resources.Load<AudioClip>(doorClipResourcePath);

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = doorVolume;
        }

        private void PlayDoorSound()
        {
            EnsureAudio();

            if (audioSource == null || doorClip == null)
                return;

            bool soundEnabled = AppSettings.I == null || AppSettings.I.SoundEnabled;
            if (!soundEnabled)
                return;

            audioSource.PlayOneShot(doorClip, doorVolume);
        }
    }
}
