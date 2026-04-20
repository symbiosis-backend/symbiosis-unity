using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CloudButtonReveal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform overlayRoot;

    [Header("Cloud Sprites")]
    [SerializeField] private Sprite leftCloud;
    [SerializeField] private Sprite rightCloud;

    [Header("Cloud Visual")]
    [SerializeField] private Vector2 cloudSize = new Vector2(900f, 500f);
    [SerializeField, Range(0f, 2f)] private float coverAmount = 1f;
    [SerializeField] private float extraCenterOffset = 20f;

    [Header("Timing")]
    [SerializeField] private float flyTime = 0.35f;
    [SerializeField] private float holdTime = 0.05f;
    [SerializeField] private float buttonHideTime = 0.20f;
    [SerializeField] private float cloudFadeTime = 0.25f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Sorting")]
    [SerializeField] private string overlayName = "CloudEffectOverlay";
    [SerializeField] private int sortingOrder = 9999;
    [SerializeField] private string sortingLayerName = "UI";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Canvas overlayCanvas;
    private bool isPlaying;

    private void Awake()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        EnsureOverlay();
        Log("Awake completed");
    }

    public void PlayForButton(Button targetButton)
    {
        if (targetButton == null)
        {
            Log("PlayForButton aborted: targetButton is null");
            return;
        }

        PlayForTarget(targetButton.transform as RectTransform, targetButton);
    }

    public void PlayForTarget(RectTransform target, Button targetButton = null, Action onComplete = null)
    {
        if (isPlaying)
        {
            Log("PlayForTarget aborted: already playing");
            return;
        }

        if (target == null)
        {
            Log("PlayForTarget aborted: target is null");
            return;
        }

        if (leftCloud == null || rightCloud == null)
        {
            Log("PlayForTarget aborted: cloud sprites missing");
            return;
        }

        EnsureOverlay();

        if (overlayCanvas == null || overlayRoot == null)
        {
            Log("PlayForTarget aborted: overlay not ready");
            return;
        }

        StartCoroutine(EffectRoutine(target, targetButton, onComplete));
    }

    private IEnumerator EffectRoutine(RectTransform target, Button targetButton, Action onComplete)
    {
        isPlaying = true;
        Log("EffectRoutine start");

        CanvasGroup targetCanvasGroup = target.GetComponent<CanvasGroup>();
        if (targetCanvasGroup == null)
            targetCanvasGroup = target.gameObject.AddComponent<CanvasGroup>();

        if (targetButton != null)
            targetButton.interactable = false;

        RectTransform left = CreateCloud("Cloud_Left", leftCloud);
        RectTransform right = CreateCloud("Cloud_Right", rightCloud);

        if (left == null || right == null)
        {
            Log("EffectRoutine aborted: failed to create clouds");
            isPlaying = false;
            yield break;
        }

        Vector2 center = GetTargetLocalPosition(target);
        float halfWidth = overlayRoot.rect.width * 0.5f;
        float closeOffset = Mathf.Max(0f, (cloudSize.x * 0.5f) * (1f - coverAmount) - extraCenterOffset);

        Vector2 startLeft = new Vector2(-halfWidth - cloudSize.x, center.y);
        Vector2 startRight = new Vector2(halfWidth + cloudSize.x, center.y);
        Vector2 targetLeft = new Vector2(center.x - closeOffset, center.y);
        Vector2 targetRight = new Vector2(center.x + closeOffset, center.y);

        left.anchoredPosition = startLeft;
        right.anchoredPosition = startRight;

        Log($"Target local position: {center}");
        Log($"StartLeft={startLeft}, StartRight={startRight}");
        Log($"TargetLeft={targetLeft}, TargetRight={targetRight}");

        yield return AnimateFly(left, right, startLeft, startRight, targetLeft, targetRight);

        if (holdTime > 0f)
            yield return Wait(holdTime);

        yield return AnimateTargetHide(target, targetCanvasGroup);
        yield return AnimateCloudHide(left, right);

        if (left != null) Destroy(left.gameObject);
        if (right != null) Destroy(right.gameObject);

        Log("EffectRoutine complete");

        isPlaying = false;
        onComplete?.Invoke();
    }

    private void EnsureOverlay()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
        {
            Log("EnsureOverlay failed: rootCanvas is null");
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
                typeof(GraphicRaycaster)
            );

            go.transform.SetParent(rootCanvas.transform, false);

            overlayRoot = go.GetComponent<RectTransform>();
            overlayCanvas = go.GetComponent<Canvas>();

            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;

            GraphicRaycaster raycaster = go.GetComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            Log("Overlay created");
        }

        overlayCanvas.renderMode = rootCanvas.renderMode;
        overlayCanvas.worldCamera = rootCanvas.worldCamera;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = sortingOrder;
        overlayCanvas.sortingLayerName = sortingLayerName;

        overlayRoot.SetAsLastSibling();
        overlayCanvas.gameObject.SetActive(true);

        Log($"Overlay ready. sortingOrder={sortingOrder}, sortingLayer={sortingLayerName}");
    }

    private RectTransform CreateCloud(string objectName, Sprite sprite)
    {
        if (sprite == null || overlayRoot == null)
            return null;

        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(overlayRoot, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = cloudSize;
        rt.localScale = Vector3.one;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = Color.white;

        rt.SetAsLastSibling();

        Log($"Cloud created: {objectName}");
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

    private IEnumerator AnimateFly(
        RectTransform left,
        RectTransform right,
        Vector2 startLeft,
        Vector2 startRight,
        Vector2 endLeft,
        Vector2 endRight)
    {
        float t = 0f;
        float duration = Mathf.Max(0.0001f, flyTime);

        Log("AnimateFly start");

        while (t < duration)
        {
            t += DeltaTime();
            float p = Mathf.Clamp01(t / duration);
            float k = p;

            if (left != null)
                left.anchoredPosition = Vector2.LerpUnclamped(startLeft, endLeft, k);

            if (right != null)
                right.anchoredPosition = Vector2.LerpUnclamped(startRight, endRight, k);

            yield return null;
        }

        if (left != null) left.anchoredPosition = endLeft;
        if (right != null) right.anchoredPosition = endRight;

        Log("AnimateFly complete");
    }

    private IEnumerator AnimateTargetHide(RectTransform target, CanvasGroup targetCanvasGroup)
    {
        Vector3 startScale = target.localScale;
        float startAlpha = targetCanvasGroup.alpha;

        float t = 0f;
        float duration = Mathf.Max(0.0001f, buttonHideTime);

        Log("AnimateTargetHide start");

        while (t < duration)
        {
            t += DeltaTime();
            float p = Mathf.Clamp01(t / duration);

            target.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, p);
            targetCanvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, 0f, p);

            yield return null;
        }

        target.localScale = Vector3.zero;
        targetCanvasGroup.alpha = 0f;
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = false;

        Log("AnimateTargetHide complete");
    }

    private IEnumerator AnimateCloudHide(RectTransform left, RectTransform right)
    {
        Image leftImage = left != null ? left.GetComponent<Image>() : null;
        Image rightImage = right != null ? right.GetComponent<Image>() : null;

        Color leftBase = leftImage != null ? leftImage.color : Color.white;
        Color rightBase = rightImage != null ? rightImage.color : Color.white;

        float t = 0f;
        float duration = Mathf.Max(0.0001f, cloudFadeTime);

        Log("AnimateCloudHide start");

        while (t < duration)
        {
            t += DeltaTime();
            float p = Mathf.Clamp01(t / duration);
            float alpha = 1f - p;

            if (left != null)
                left.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, p);

            if (right != null)
                right.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, p);

            if (leftImage != null)
            {
                Color c = leftBase;
                c.a = leftBase.a * alpha;
                leftImage.color = c;
            }

            if (rightImage != null)
            {
                Color c = rightBase;
                c.a = rightBase.a * alpha;
                rightImage.color = c;
            }

            yield return null;
        }

        Log("AnimateCloudHide complete");
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
        if (!debugLogs) return;
        Debug.Log($"[CloudButtonReveal:{name}] {message}", this);
    }
}
