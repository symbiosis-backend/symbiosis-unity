using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TrayFX : MonoBehaviour
    {
        [Header("Flight Animation")]
        [SerializeField] private float flyDuration = 0.28f;
        [SerializeField] private float arcHeight = 85f;
        [SerializeField] private float landScalePunch = 0.08f;

        [Header("Match Break Animation")]
        [SerializeField] private float matchWindupDistance = 18f;
        [SerializeField] private float matchWindupTime = 0.06f;
        [SerializeField] private float matchHitTime = 0.06f;
        [SerializeField] private float matchShakeTime = 0.04f;
        [SerializeField] private float matchShakeAmount = 5f;
        [SerializeField] private float matchBreakTime = 0.09f;
        [SerializeField] private float matchBreakOffsetX = 26f;
        [SerializeField] private float matchBreakOffsetY = 10f;
        [SerializeField] private float matchBreakRotation = 18f;
        [SerializeField] private float matchHitScale = 1.12f;
        [SerializeField] private float matchEndScale = 0.72f;

        [Header("Tracer Trail")]
        [SerializeField] private bool useTracerTrail = true;
        [SerializeField] private Sprite tracerSprite;
        [SerializeField] private Color tracerColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private float tracerWidth = 22f;
        [SerializeField] private float tracerFadeTime = 0.12f;
        [SerializeField] private float tracerFollowLerp = 18f;

        [Header("Audio")]
        [SerializeField] private AudioClip tileLandClip;
        [SerializeField, Range(0f, 1f)] private float tileLandVolume = 0.9f;
        [SerializeField] private AudioClip tileMatchClip;
        [SerializeField, Range(0f, 1f)] private float tileMatchVolume = 1f;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            flyDuration = Mathf.Max(0.01f, flyDuration);
            arcHeight = Mathf.Max(0f, arcHeight);
            landScalePunch = Mathf.Max(0f, landScalePunch);

            matchWindupDistance = Mathf.Max(0f, matchWindupDistance);
            matchWindupTime = Mathf.Max(0.01f, matchWindupTime);
            matchHitTime = Mathf.Max(0.01f, matchHitTime);
            matchShakeTime = Mathf.Max(0f, matchShakeTime);
            matchShakeAmount = Mathf.Max(0f, matchShakeAmount);
            matchBreakTime = Mathf.Max(0.01f, matchBreakTime);
            matchBreakOffsetX = Mathf.Max(0f, matchBreakOffsetX);
            matchBreakOffsetY = Mathf.Max(0f, matchBreakOffsetY);
            matchBreakRotation = Mathf.Max(0f, matchBreakRotation);
            matchHitScale = Mathf.Max(1f, matchHitScale);
            matchEndScale = Mathf.Clamp(matchEndScale, 0.1f, 1f);

            tracerWidth = Mathf.Max(1f, tracerWidth);
            tracerFadeTime = Mathf.Max(0.01f, tracerFadeTime);
            tracerFollowLerp = Mathf.Max(1f, tracerFollowLerp);
        }
#endif

        public IEnumerator PlayFly(RectTransform tileRect, Vector2 startPos, Vector2 targetPos, Vector2 targetSize)
        {
            if (tileRect == null)
                yield break;

            Vector3 startScale = tileRect.localScale;
            Vector3 targetScale = Vector3.one;

            float dynamicArc = Mathf.Max(arcHeight, Mathf.Abs(targetPos.x - startPos.x) * 0.10f);
            float time = 0f;

            Image tracerImage = null;
            RectTransform tracerRect = null;
            Vector2 tracerTail = startPos;

            if (useTracerTrail && tracerSprite != null)
                CreateTracer(tileRect.parent, out tracerImage, out tracerRect, startPos);

            while (time < flyDuration)
            {
                if (tileRect == null)
                {
                    CleanupTracer(tracerImage);
                    yield break;
                }

                time += Time.deltaTime;

                float t = Mathf.Clamp01(time / flyDuration);
                float eased = EaseOutCubic(t);

                Vector2 pos = Vector2.Lerp(startPos, targetPos, eased);
                pos.y += Mathf.Sin(t * Mathf.PI) * dynamicArc;

                tileRect.anchoredPosition = pos;
                tileRect.localScale = Vector3.Lerp(startScale, targetScale, eased);

                if (tracerRect != null && tracerImage != null)
                {
                    tracerTail = Vector2.Lerp(tracerTail, pos, Time.deltaTime * tracerFollowLerp);
                    UpdateTracer(tracerRect, tracerImage, tracerTail, pos);
                }

                yield return null;
            }

            if (tileRect != null)
            {
                tileRect.sizeDelta = targetSize;
                tileRect.anchoredPosition = targetPos;
                tileRect.localScale = targetScale;
                tileRect.localRotation = Quaternion.identity;

                if (landScalePunch > 0f)
                    yield return PlayLandingPunch(tileRect, targetScale);

                PlayTileLandSound();
            }

            if (tracerImage != null)
                yield return FadeAndDestroyTracer(tracerImage);
        }

        public IEnumerator PlayMatchBreak(RectTransform ra, RectTransform rb)
        {
            if (ra == null || rb == null)
                yield break;

            Vector2 startA = ra.anchoredPosition;
            Vector2 startB = rb.anchoredPosition;

            Vector3 startScaleA = ra.localScale;
            Vector3 startScaleB = rb.localScale;

            Quaternion startRotA = ra.localRotation;
            Quaternion startRotB = rb.localRotation;

            float dir = Mathf.Sign(startB.x - startA.x);
            if (Mathf.Approximately(dir, 0f))
                dir = 1f;

            Vector2 windupA = startA + Vector2.left * dir * matchWindupDistance;
            Vector2 windupB = startB + Vector2.right * dir * matchWindupDistance;

            float time = 0f;

            while (time < matchWindupTime)
            {
                if (ra == null || rb == null)
                    yield break;

                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / matchWindupTime);
                float eased = EaseOutCubic(t);

                ra.anchoredPosition = Vector2.Lerp(startA, windupA, eased);
                rb.anchoredPosition = Vector2.Lerp(startB, windupB, eased);

                yield return null;
            }

            Vector2 center = (startA + startB) * 0.5f;

            time = 0f;
            while (time < matchHitTime)
            {
                if (ra == null || rb == null)
                    yield break;

                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / matchHitTime);
                float eased = EaseOutCubic(t);

                ra.anchoredPosition = Vector2.Lerp(windupA, center, eased);
                rb.anchoredPosition = Vector2.Lerp(windupB, center, eased);

                Vector3 hitScale = Vector3.one * Mathf.Lerp(1f, matchHitScale, eased);
                ra.localScale = Vector3.Lerp(startScaleA, hitScale, eased);
                rb.localScale = Vector3.Lerp(startScaleB, hitScale, eased);

                yield return null;
            }

            PlayMatchSound();

            time = 0f;
            while (time < matchShakeTime)
            {
                if (ra == null || rb == null)
                    yield break;

                time += Time.deltaTime;

                Vector2 jitterA = Random.insideUnitCircle * matchShakeAmount;
                Vector2 jitterB = Random.insideUnitCircle * matchShakeAmount;

                ra.anchoredPosition = center + jitterA;
                rb.anchoredPosition = center + jitterB;

                yield return null;
            }

            CanvasGroup cga = GetOrAddCanvasGroup(ra.gameObject);
            CanvasGroup cgb = GetOrAddCanvasGroup(rb.gameObject);

            Vector2 breakOffsetA = new Vector2(-matchBreakOffsetX, matchBreakOffsetY);
            Vector2 breakOffsetB = new Vector2(matchBreakOffsetX, -matchBreakOffsetY);

            Quaternion breakRotA = Quaternion.Euler(0f, 0f, -matchBreakRotation);
            Quaternion breakRotB = Quaternion.Euler(0f, 0f, matchBreakRotation);

            time = 0f;
            while (time < matchBreakTime)
            {
                if (ra == null || rb == null)
                    yield break;

                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / matchBreakTime);
                float eased = EaseOutCubic(t);

                ra.anchoredPosition = Vector2.Lerp(center, center + breakOffsetA, eased);
                rb.anchoredPosition = Vector2.Lerp(center, center + breakOffsetB, eased);

                ra.localRotation = Quaternion.Lerp(startRotA, breakRotA, eased);
                rb.localRotation = Quaternion.Lerp(startRotB, breakRotB, eased);

                ra.localScale = Vector3.Lerp(Vector3.one * matchHitScale, Vector3.one * matchEndScale, eased);
                rb.localScale = Vector3.Lerp(Vector3.one * matchHitScale, Vector3.one * matchEndScale, eased);

                float alpha = Mathf.Lerp(1f, 0f, eased);
                cga.alpha = alpha;
                cgb.alpha = alpha;

                yield return null;
            }
        }

        private IEnumerator PlayLandingPunch(RectTransform tileRect, Vector3 baseScale)
        {
            Vector3 overshoot = baseScale * (1f + landScalePunch);

            float upTime = 0.04f;
            float downTime = 0.05f;

            float time = 0f;
            while (time < upTime)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / upTime);
                tileRect.localScale = Vector3.Lerp(baseScale, overshoot, t);
                yield return null;
            }

            time = 0f;
            while (time < downTime)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / downTime);
                tileRect.localScale = Vector3.Lerp(overshoot, baseScale, t);
                yield return null;
            }

            tileRect.localScale = baseScale;
        }

        private void CreateTracer(Transform parent, out Image image, out RectTransform tracerRect, Vector2 startPos)
        {
            GameObject go = new GameObject("TileTracer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();

            image = go.GetComponent<Image>();
            tracerRect = go.GetComponent<RectTransform>();

            image.sprite = tracerSprite;
            image.color = tracerColor;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            tracerRect.pivot = new Vector2(0f, 0.5f);
            tracerRect.anchorMin = new Vector2(0.5f, 0.5f);
            tracerRect.anchorMax = new Vector2(0.5f, 0.5f);
            tracerRect.anchoredPosition = startPos;
            tracerRect.sizeDelta = new Vector2(1f, tracerWidth);
        }

        private void UpdateTracer(RectTransform tracerRect, Image image, Vector2 from, Vector2 to)
        {
            Vector2 dir = to - from;
            float length = dir.magnitude;
            if (length < 0.01f)
                return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            tracerRect.anchoredPosition = from;
            tracerRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            tracerRect.sizeDelta = new Vector2(length, tracerWidth);

            Color c = tracerColor;
            c.a = tracerColor.a;
            image.color = c;
        }

        private IEnumerator FadeAndDestroyTracer(Image tracerImage)
        {
            if (tracerImage == null)
                yield break;

            Color start = tracerImage.color;
            float time = 0f;

            while (time < tracerFadeTime)
            {
                if (tracerImage == null)
                    yield break;

                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / tracerFadeTime);

                Color c = start;
                c.a = Mathf.Lerp(start.a, 0f, t);
                tracerImage.color = c;

                yield return null;
            }

            if (tracerImage != null)
                Destroy(tracerImage.gameObject);
        }

        private void CleanupTracer(Image tracerImage)
        {
            if (tracerImage != null)
                Destroy(tracerImage.gameObject);
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        private void PlayTileLandSound()
        {
            if (audioSource == null || tileLandClip == null)
                return;

            audioSource.PlayOneShot(tileLandClip, tileLandVolume);
        }

        private void PlayMatchSound()
        {
            if (audioSource == null || tileMatchClip == null)
                return;

            audioSource.PlayOneShot(tileMatchClip, tileMatchVolume);
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}