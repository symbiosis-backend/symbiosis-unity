using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MahjongMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject levelSelectPanel;

        [Header("Buttons Root")]
        [SerializeField] private GameObject storyButtonRoot;
        [SerializeField] private GameObject battleButtonRoot;
        [SerializeField] private GameObject resetProgressButtonRoot;

        [Header("Buttons")]
        [SerializeField] private Button storyButton;
        [SerializeField] private Button battleButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetButton;

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "GameMahjong";
        [SerializeField] private string battleLobbySceneName = "LobbyMahjongBattle";

        [Header("Debug")]
        [SerializeField] private bool showResetProgressButton = true;
        [SerializeField] private bool debugLogs = true;

        [Header("Cloud Effect")]
        [SerializeField] private bool useCloudEffect = true;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Sprite cloudSprite;

        [Header("Cloud Visual")]
        [SerializeField] private Vector2 cloudStartSize = new Vector2(260f, 140f);
        [SerializeField] private Vector2 cloudAbsorbSize = new Vector2(420f, 220f);

        [Header("Cloud Path")]
        [SerializeField] private float cloudStartOffsetX = 220f;
        [SerializeField] private float cloudExitOffsetX = 900f;
        [SerializeField] private float cloudRevealStartOffsetX = 900f;
        [SerializeField] private float cloudRevealExitOffsetX = 220f;
        [SerializeField] private float cloudYOffset = 0f;

        [Header("Cloud Timing")]
        [SerializeField] private float cloudFlyInTime = 0.28f;
        [SerializeField] private float absorbTime = 0.18f;
        [SerializeField] private float cloudHoldTime = 0.04f;
        [SerializeField] private float cloudFlyOutTime = 0.55f;
        [SerializeField] private float revealFlyInTime = 0.32f;
        [SerializeField] private float revealTime = 0.20f;
        [SerializeField] private float revealFlyOutTime = 0.38f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Cloud Rotation")]
        [SerializeField] private bool rotateCloudWhileMoving = true;
        [SerializeField] private float cloudRotateSpeed = 360f;
        [SerializeField] private bool reverseRotationOnBackFlight = false;

        [Header("Overlay")]
        [SerializeField] private string overlayName = "CloudEffectOverlay";
        [SerializeField] private int overlaySortingOrder = 9999;
        [SerializeField] private string overlaySortingLayerName = "UI";

        private Canvas overlayCanvas;
        private RectTransform overlayRoot;
        private CloudFxRunner fxRunner;
        private bool transitionPlaying;

        private void Awake()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            EnsureOverlay();
        }

        private void OnEnable()
        {
            RefreshButtons();
            ShowMainPanelImmediate();
        }

        private void Start()
        {
            RefreshButtons();
            ShowMainPanelImmediate();
        }

        public void RefreshButtons()
        {
            if (storyButtonRoot != null)
                storyButtonRoot.SetActive(true);

            if (battleButtonRoot != null)
                battleButtonRoot.SetActive(true);

            if (resetProgressButtonRoot != null)
                resetProgressButtonRoot.SetActive(showResetProgressButton);

            RestoreButtonsInTree(mainPanel);
            RestoreButtonsInTree(levelSelectPanel);

            RestoreButtonVisual(storyButton);
            RestoreButtonVisual(battleButton);
            RestoreButtonVisual(backButton);
            RestoreButtonVisual(resetButton);

            Canvas.ForceUpdateCanvases();
            Log($"Menu ready | StoryScene={gameSceneName} | BattleLobbyScene={battleLobbySceneName}");
        }

        public void OnClickStory()
        {
            Log("Story button clicked");
            if (transitionPlaying)
                return;

            if (useCloudEffect && storyButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenReveal(storyButton, () =>
                {
                    SetPanel(mainPanel, false);
                    RestoreButtonsInTree(levelSelectPanel);
                    SetPanel(levelSelectPanel, true);
                });
                return;
            }

            SetPanel(mainPanel, false);
            RestoreButtonsInTree(levelSelectPanel);
            SetPanel(levelSelectPanel, true);
        }

        public void OnClickBattle()
        {
            Log("Battle button clicked");
            if (transitionPlaying)
                return;

            MahjongSession.Clear();

            if (useCloudEffect && battleButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenComplete(battleButton, () => LoadSceneWithDoor(battleLobbySceneName));
                return;
            }

            LoadSceneWithDoor(battleLobbySceneName);
        }

        public void OnClickBackFromLevels()
        {
            Log("Back button clicked");
            if (transitionPlaying)
                return;

            if (useCloudEffect && storyButton != null && cloudSprite != null)
            {
                PlayReturnToMainWithStoryReveal();
                return;
            }

            ShowMainPanelImmediate();
        }

        public void OnClickLevel(int level)
        {
            Log($"Level button clicked: {level}");
            if (transitionPlaying)
                return;

            MahjongSession.StartStory(level, 1);

            Button clickedButton = GetCurrentSelectedButton();
            if (useCloudEffect && clickedButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenComplete(clickedButton, () => LoadSceneWithDoor(gameSceneName));
                return;
            }

            LoadSceneWithDoor(gameSceneName);
        }

        public void OnClickResetProgress()
        {
            Log("Reset button clicked");
            if (transitionPlaying)
                return;

            if (useCloudEffect && resetButton != null && cloudSprite != null)
            {
                PlayAbsorbCloudThenReveal(resetButton, ResetProgressAndRefresh);
                return;
            }

            ResetProgressAndRefresh();
        }

        private void ResetProgressAndRefresh()
        {
            MahjongProgress.ResetAll();
            MahjongSession.Clear();
            RefreshButtons();
            ShowMainPanelImmediate();
        }

        private void ShowMainPanelImmediate()
        {
            RestoreButtonsInTree(mainPanel);

            if (storyButtonRoot != null)
                storyButtonRoot.SetActive(true);

            if (battleButtonRoot != null)
                battleButtonRoot.SetActive(true);

            if (resetProgressButtonRoot != null)
                resetProgressButtonRoot.SetActive(showResetProgressButton);

            SetPanel(mainPanel, true);
            SetPanel(levelSelectPanel, false);

            RestoreButtonVisual(storyButton);
            RestoreButtonVisual(battleButton);
            RestoreButtonVisual(backButton);
            RestoreButtonVisual(resetButton);
        }

        private void PlayReturnToMainWithStoryReveal()
        {
            if (transitionPlaying)
                return;

            EnsureOverlay();

            if (fxRunner == null)
            {
                ShowMainPanelImmediate();
                return;
            }

            transitionPlaying = true;
            fxRunner.StartCoroutine(ReturnToMainWithStoryRevealRoutine());
        }

        private IEnumerator ReturnToMainWithStoryRevealRoutine()
        {
            ShowMainPanelImmediate();

            if (storyButtonRoot != null)
                storyButtonRoot.SetActive(true);

            if (storyButton != null)
            {
                storyButton.gameObject.SetActive(true);

                RectTransform storyRect = storyButton.transform as RectTransform;
                if (storyRect != null)
                    storyRect.localScale = Vector3.zero;

                CanvasGroup storyGroup = storyButton.GetComponent<CanvasGroup>();
                if (storyGroup == null)
                    storyGroup = storyButton.gameObject.AddComponent<CanvasGroup>();

                storyGroup.alpha = 1f;
                storyGroup.interactable = false;
                storyGroup.blocksRaycasts = false;

                Graphic[] graphics = storyButton.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i] == null)
                        continue;

                    Color c = graphics[i].color;
                    c.a = 1f;
                    graphics[i].color = c;
                }
            }

            yield return null;
            Canvas.ForceUpdateCanvases();

            RectTransform targetRect = storyButton != null ? storyButton.transform as RectTransform : null;
            if (targetRect == null || overlayRoot == null || cloudSprite == null)
            {
                RestoreButtonVisual(storyButton);
                transitionPlaying = false;
                yield break;
            }

            RectTransform cloud = CreateCloud("RevealCloud", cloudSprite, cloudAbsorbSize);
            if (cloud == null)
            {
                RestoreButtonVisual(storyButton);
                transitionPlaying = false;
                yield break;
            }

            Vector2 center = GetTargetLocalPosition(targetRect);
            center.y += cloudYOffset;

            float halfWidth = overlayRoot.rect.width * 0.5f;
            Vector2 startPos = new Vector2(halfWidth + cloudAbsorbSize.x + cloudRevealStartOffsetX, center.y);
            Vector2 revealPos = center;
            Vector2 exitPos = new Vector2(-halfWidth - cloudStartSize.x - cloudRevealExitOffsetX, center.y);

            cloud.anchoredPosition = startPos;
            cloud.sizeDelta = cloudAbsorbSize;
            cloud.localScale = Vector3.one;
            cloud.SetAsLastSibling();

            Image cloudImage = cloud.GetComponent<Image>();
            if (cloudImage != null)
            {
                cloudImage.enabled = true;
                cloudImage.color = Color.white;
            }

            yield return AnimateCloudMove(cloud, startPos, revealPos, revealFlyInTime, reverseRotationOnBackFlight ? -1f : 1f);
            yield return AnimateReveal(cloud, targetRect, storyButton);
            yield return AnimateCloudMoveAndResize(cloud, revealPos, exitPos, cloudAbsorbSize, cloudStartSize, revealFlyOutTime, reverseRotationOnBackFlight ? -1f : 1f);

            if (cloud != null)
                Destroy(cloud.gameObject);

            RestoreButtonVisual(storyButton);
            transitionPlaying = false;
        }

        private void PlayAbsorbCloudThenReveal(Button targetButton, Action onReveal)
        {
            if (transitionPlaying)
                return;

            EnsureOverlay();

            if (fxRunner == null)
            {
                onReveal?.Invoke();
                return;
            }

            transitionPlaying = true;
            fxRunner.StartCoroutine(AbsorbCloudRoutine(targetButton, onReveal, null));
        }

        private void PlayAbsorbCloudThenComplete(Button targetButton, Action onComplete)
        {
            if (transitionPlaying)
                return;

            EnsureOverlay();

            if (fxRunner == null)
            {
                onComplete?.Invoke();
                return;
            }

            transitionPlaying = true;
            fxRunner.StartCoroutine(AbsorbCloudRoutine(targetButton, null, onComplete));
        }

        private IEnumerator AbsorbCloudRoutine(Button targetButton, Action onReveal, Action onComplete)
        {
            if (targetButton == null)
            {
                transitionPlaying = false;
                onReveal?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            RectTransform targetRect = targetButton.transform as RectTransform;
            if (targetRect == null || overlayRoot == null || cloudSprite == null)
            {
                transitionPlaying = false;
                onReveal?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            CanvasGroup targetGroup = targetButton.GetComponent<CanvasGroup>();
            if (targetGroup == null)
                targetGroup = targetButton.gameObject.AddComponent<CanvasGroup>();

            targetGroup.blocksRaycasts = false;

            Graphic[] targetGraphics = targetButton.GetComponentsInChildren<Graphic>(true);
            Color[] originalColors = CacheGraphicColors(targetGraphics);

            RectTransform cloud = CreateCloud("AbsorbCloud", cloudSprite, cloudStartSize);
            if (cloud == null)
            {
                transitionPlaying = false;
                targetGroup.blocksRaycasts = true;
                onReveal?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            Vector2 center = GetTargetLocalPosition(targetRect);
            center.y += cloudYOffset;

            float halfWidth = overlayRoot.rect.width * 0.5f;
            Vector2 startPos = new Vector2(-halfWidth - cloudStartSize.x - cloudStartOffsetX, center.y);
            Vector2 absorbPos = center;
            Vector2 exitPos = new Vector2(halfWidth + cloudAbsorbSize.x + cloudExitOffsetX, center.y);

            cloud.anchoredPosition = startPos;
            cloud.sizeDelta = cloudStartSize;
            cloud.localScale = Vector3.one;
            cloud.SetAsLastSibling();

            Image cloudImage = cloud.GetComponent<Image>();
            if (cloudImage != null)
            {
                cloudImage.enabled = true;
                cloudImage.color = Color.white;
            }

            yield return AnimateCloudMove(cloud, startPos, absorbPos, cloudFlyInTime, 1f);
            yield return AnimateAbsorb(cloud, targetRect, targetGroup, targetGraphics, originalColors);

            if (cloudHoldTime > 0f)
                yield return Wait(cloudHoldTime);

            targetButton.gameObject.SetActive(false);

            onReveal?.Invoke();

            if (cloud != null)
            {
                cloud.gameObject.SetActive(true);
                cloud.SetAsLastSibling();

                Image img = cloud.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.color = Color.white;
                }
            }

            yield return AnimateCloudMove(cloud, absorbPos, exitPos, cloudFlyOutTime, 1f);

            if (cloud != null)
                Destroy(cloud.gameObject);

            transitionPlaying = false;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateAbsorb(RectTransform cloud, RectTransform targetRect, CanvasGroup targetGroup, Graphic[] targetGraphics, Color[] originalColors)
        {
            if (cloud == null || targetRect == null || targetGroup == null)
                yield break;

            Vector2 startSize = cloud.sizeDelta;
            Vector2 endSize = cloudAbsorbSize;
            Vector3 startButtonScale = targetRect.localScale;

            float t = 0f;
            float duration = Mathf.Max(0.0001f, absorbTime);

            while (t < duration)
            {
                t += DeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                cloud.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, eased);
                targetRect.localScale = Vector3.LerpUnclamped(startButtonScale, Vector3.zero, eased);
                RestoreGraphicColors(targetGraphics, originalColors);

                yield return null;
            }

            cloud.sizeDelta = endSize;
            targetRect.localScale = Vector3.zero;
            RestoreGraphicColors(targetGraphics, originalColors);

            targetGroup.interactable = false;
            targetGroup.blocksRaycasts = false;
        }

        private IEnumerator AnimateReveal(RectTransform cloud, RectTransform targetRect, Button targetButton)
        {
            if (cloud == null || targetRect == null || targetButton == null)
                yield break;

            CanvasGroup targetGroup = targetButton.GetComponent<CanvasGroup>();
            if (targetGroup == null)
                targetGroup = targetButton.gameObject.AddComponent<CanvasGroup>();

            Graphic[] targetGraphics = targetButton.GetComponentsInChildren<Graphic>(true);
            Color[] originalColors = CacheGraphicColors(targetGraphics);

            Vector2 startSize = cloud.sizeDelta;
            Vector2 endSize = cloudStartSize;
            Vector3 startButtonScale = Vector3.zero;
            Vector3 endButtonScale = Vector3.one;

            targetRect.localScale = Vector3.zero;
            targetGroup.alpha = 1f;

            float t = 0f;
            float duration = Mathf.Max(0.0001f, revealTime);

            while (t < duration)
            {
                t += DeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                cloud.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, eased);
                targetRect.localScale = Vector3.LerpUnclamped(startButtonScale, endButtonScale, eased);
                RestoreGraphicColors(targetGraphics, originalColors);

                yield return null;
            }

            cloud.sizeDelta = endSize;
            targetRect.localScale = Vector3.one;
            RestoreGraphicColors(targetGraphics, originalColors);

            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
        }

        private IEnumerator AnimateCloudMove(RectTransform cloud, Vector2 from, Vector2 to, float duration, float rotationDirection)
        {
            if (cloud == null)
                yield break;

            float t = 0f;
            duration = Mathf.Max(0.0001f, duration);
            float startZ = cloud.localEulerAngles.z;

            while (t < duration)
            {
                if (cloud == null)
                    yield break;

                t += DeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                cloud.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);

                if (rotateCloudWhileMoving)
                {
                    float z = startZ - (cloudRotateSpeed * rotationDirection * t);
                    cloud.localRotation = Quaternion.Euler(0f, 0f, z);
                }

                yield return null;
            }

            if (cloud != null)
                cloud.anchoredPosition = to;
        }

        private IEnumerator AnimateCloudMoveAndResize(RectTransform cloud, Vector2 fromPos, Vector2 toPos, Vector2 fromSize, Vector2 toSize, float duration, float rotationDirection)
        {
            if (cloud == null)
                yield break;

            float t = 0f;
            duration = Mathf.Max(0.0001f, duration);
            float startZ = cloud.localEulerAngles.z;

            while (t < duration)
            {
                if (cloud == null)
                    yield break;

                t += DeltaTime();
                float p = Mathf.Clamp01(t / duration);
                float eased = Mathf.SmoothStep(0f, 1f, p);

                cloud.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, eased);
                cloud.sizeDelta = Vector2.LerpUnclamped(fromSize, toSize, eased);

                if (rotateCloudWhileMoving)
                {
                    float z = startZ - (cloudRotateSpeed * rotationDirection * t);
                    cloud.localRotation = Quaternion.Euler(0f, 0f, z);
                }

                yield return null;
            }

            if (cloud != null)
            {
                cloud.anchoredPosition = toPos;
                cloud.sizeDelta = toSize;
            }
        }

        private void EnsureOverlay()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (rootCanvas == null)
            {
                Log("Root canvas not found");
                return;
            }

            Transform existing = rootCanvas.transform.Find(overlayName);
            if (existing != null)
            {
                overlayRoot = existing as RectTransform;
                overlayCanvas = existing.GetComponent<Canvas>();
            }

            if (overlayRoot == null || overlayCanvas == null)
            {
                GameObject go = new GameObject(
                    overlayName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(GraphicRaycaster),
                    typeof(CloudFxRunner));

                go.transform.SetParent(rootCanvas.transform, false);

                overlayRoot = go.GetComponent<RectTransform>();
                overlayCanvas = go.GetComponent<Canvas>();
                fxRunner = go.GetComponent<CloudFxRunner>();

                overlayRoot.anchorMin = Vector2.zero;
                overlayRoot.anchorMax = Vector2.one;
                overlayRoot.offsetMin = Vector2.zero;
                overlayRoot.offsetMax = Vector2.zero;

                GraphicRaycaster raycaster = go.GetComponent<GraphicRaycaster>();
                raycaster.enabled = false;
            }
            else
            {
                fxRunner = overlayCanvas.GetComponent<CloudFxRunner>();
                if (fxRunner == null)
                    fxRunner = overlayCanvas.gameObject.AddComponent<CloudFxRunner>();
            }

            overlayCanvas.renderMode = rootCanvas.renderMode;
            overlayCanvas.worldCamera = rootCanvas.worldCamera;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = overlaySortingOrder;
            overlayCanvas.sortingLayerName = overlaySortingLayerName;

            overlayRoot.SetAsLastSibling();
            overlayCanvas.gameObject.SetActive(true);
        }

        private RectTransform CreateCloud(string objectName, Sprite sprite, Vector2 size)
        {
            if (sprite == null || overlayRoot == null)
                return null;

            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(overlayRoot, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            img.enabled = true;

            rt.SetAsLastSibling();
            return rt;
        }

        private Vector2 GetTargetLocalPosition(RectTransform target)
        {
            Camera cam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = rootCanvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPoint, cam, out Vector2 localPoint);
            return localPoint;
        }

        private Button GetCurrentSelectedButton()
        {
            GameObject selected = UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject
                : null;

            if (selected == null)
                return null;

            return selected.GetComponent<Button>();
        }

        private void LoadSceneWithDoor(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[MahjongMenuUI] Scene name is empty.");
                return;
            }

            if (DoorFx.I != null && DoorFx.I.IsReady())
                DoorFx.I.LoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }

        private void SetPanel(GameObject panel, bool value)
        {
            if (panel != null)
                panel.SetActive(value);
        }

        private void RestoreButtonVisual(Button targetButton)
        {
            if (targetButton == null)
                return;

            targetButton.gameObject.SetActive(true);

            RectTransform rt = targetButton.transform as RectTransform;
            if (rt != null)
                rt.localScale = Vector3.one;

            CanvasGroup cg = targetButton.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            Graphic[] graphics = targetButton.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;
                Color c = graphics[i].color;
                c.a = 1f;
                graphics[i].color = c;
            }

            targetButton.interactable = true;
        }

        private void RestoreButtonsInTree(GameObject root)
        {
            if (root == null)
                return;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                RestoreButtonVisual(buttons[i]);
        }

        private Color[] CacheGraphicColors(Graphic[] graphics)
        {
            if (graphics == null)
                return Array.Empty<Color>();

            Color[] colors = new Color[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                colors[i] = graphics[i] != null ? graphics[i].color : Color.white;

            return colors;
        }

        private void RestoreGraphicColors(Graphic[] graphics, Color[] colors)
        {
            if (graphics == null || colors == null)
                return;

            int count = Mathf.Min(graphics.Length, colors.Length);
            for (int i = 0; i < count; i++)
            {
                if (graphics[i] == null)
                    continue;

                graphics[i].color = colors[i];
            }
        }

        private float DeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private object Wait(float time)
        {
            if (useUnscaledTime)
                return new WaitForSecondsRealtime(time);

            return new WaitForSeconds(time);
        }

        private void Log(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[MahjongMenuUI] {message}", this);
        }
    }

}
