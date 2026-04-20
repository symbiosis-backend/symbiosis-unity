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

        [Header("Links")]
        [SerializeField] private Canvas doorCanvas;
        [SerializeField] private RectTransform leftDoor;
        [SerializeField] private RectTransform rightDoor;
        [SerializeField] private Image fadeImage;

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

        private float halfWidth;
        private float openedLeftX;
        private float openedRightX;

        private bool isTransitioning;
        private bool shouldOpenAfterLoad;
        private bool keepCurrentDoorState;
        private Coroutine routine;

        public bool IsBusy => isTransitioning;

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
                return;

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
            doorCanvas.sortingOrder = 9999;

            if (fadeImage != null)
                fadeImage.raycastTarget = false;

            if (leftDoor.TryGetComponent(out Image leftImage))
                leftImage.raycastTarget = false;

            if (rightDoor.TryGetComponent(out Image rightImage))
                rightImage.raycastTarget = false;

            RectTransform canvasRect = doorCanvas.GetComponent<RectTransform>();
            float canvasWidth = canvasRect.rect.width;
            halfWidth = canvasWidth * 0.5f;

            leftDoor.anchorMin = new Vector2(0.5f, 0f);
            leftDoor.anchorMax = new Vector2(0.5f, 1f);
            leftDoor.pivot = new Vector2(1f, 0.5f);
            leftDoor.sizeDelta = new Vector2(halfWidth, 0f);

            rightDoor.anchorMin = new Vector2(0.5f, 0f);
            rightDoor.anchorMax = new Vector2(0.5f, 1f);
            rightDoor.pivot = new Vector2(0f, 0.5f);
            rightDoor.sizeDelta = new Vector2(halfWidth, 0f);

            openedLeftX = -(halfWidth + hiddenPadding);
            openedRightX = halfWidth + hiddenPadding;
        }

        public bool IsReady()
        {
            return doorCanvas != null && leftDoor != null && rightDoor != null;
        }

        public void LoadScene(string sceneName)
        {
            if (isTransitioning || string.IsNullOrWhiteSpace(sceneName))
                return;

            if (!IsReady())
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(CoCloseThenLoad(sceneName));
        }

        public void RunBetweenLevels(Action action)
        {
            if (isTransitioning || action == null || !IsReady())
                return;

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

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
                yield return null;
        }

        private IEnumerator CoOpenAfterLoad()
        {
            keepCurrentDoorState = true;
            PlaceDoorsClosedInstant();

            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            if (!IsReady())
            {
                isTransitioning = false;
                keepCurrentDoorState = false;
                routine = null;
                yield break;
            }

            yield return AnimateDoors(isClosing: false);

            isTransitioning = false;
            keepCurrentDoorState = false;
            routine = null;
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
                isTransitioning = false;
                keepCurrentDoorState = false;
                routine = null;
                yield break;
            }

            yield return AnimateDoors(isClosing: false);

            isTransitioning = false;
            keepCurrentDoorState = false;
            routine = null;
        }

        private IEnumerator AnimateDoors(bool isClosing)
        {
            if (!IsReady())
                yield break;

            float duration = isClosing ? closeDuration : openDuration;

            float leftFrom = isClosing ? openedLeftX : 0f;
            float leftTo = isClosing ? 0f : openedLeftX;

            float rightFrom = isClosing ? openedRightX : 0f;
            float rightTo = isClosing ? 0f : openedRightX;

            float fadeFrom = isClosing ? 0f : (useFade ? fadeClosedAlpha : 0f);
            float fadeTo = isClosing ? (useFade ? fadeClosedAlpha : 0f) : 0f;

            leftDoor.anchoredPosition = new Vector2(leftFrom, 0f);
            rightDoor.anchoredPosition = new Vector2(rightFrom, 0f);
            SetFade(fadeFrom);

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float e = Mathf.SmoothStep(0f, 1f, p);

                leftDoor.anchoredPosition = new Vector2(Mathf.Lerp(leftFrom, leftTo, e), 0f);
                rightDoor.anchoredPosition = new Vector2(Mathf.Lerp(rightFrom, rightTo, e), 0f);
                SetFade(Mathf.Lerp(fadeFrom, fadeTo, e));

                yield return null;
            }

            leftDoor.anchoredPosition = new Vector2(leftTo, 0f);
            rightDoor.anchoredPosition = new Vector2(rightTo, 0f);
            SetFade(fadeTo);
        }

        private void PlaceDoorsOpenedInstant()
        {
            if (!IsReady())
                return;

            leftDoor.anchoredPosition = new Vector2(openedLeftX, 0f);
            rightDoor.anchoredPosition = new Vector2(openedRightX, 0f);
            SetFade(0f);
        }

        private void PlaceDoorsClosedInstant()
        {
            if (!IsReady())
                return;

            leftDoor.anchoredPosition = Vector2.zero;
            rightDoor.anchoredPosition = Vector2.zero;
            SetFade(useFade ? fadeClosedAlpha : 0f);
        }

        private void SetFade(float alpha)
        {
            if (fadeImage == null)
                return;

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }
    }
}