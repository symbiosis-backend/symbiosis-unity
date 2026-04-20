using System.Collections;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class PeekFx : MonoBehaviour
    {
        [Header("Цель")]
        [SerializeField] private Transform target;

        [Header("Позиции по Y")]
        [SerializeField] private float hiddenY = -8f;
        [SerializeField] private float firstPeekY = -3f;
        [SerializeField] private float fullPeekY = 0f;

        [Header("Скорость")]
        [SerializeField] private float firstMoveTime = 0.35f;
        [SerializeField] private float secondMoveTime = 0.35f;
        [SerializeField] private float hideMoveTime = 0.4f;

        [Header("Паузы")]
        [SerializeField] private float pauseAtFirst = 0.45f;
        [SerializeField] private float pauseAtFull = 1.1f;
        [SerializeField] private float delayBetweenCycles = 2f;

        [Header("Поведение")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool loop = true;

        [Header("Плавность")]
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine routine;
        private Vector3 localPos;

        private void Reset()
        {
            target = transform;
        }

        private void Awake()
        {
            if (target == null)
                target = transform;

            localPos = target.localPosition;
            localPos.y = hiddenY;
            target.localPosition = localPos;
        }

        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            Stop();
        }

        [ContextMenu("Play")]
        public void Play()
        {
            Stop();

            if (!gameObject.activeInHierarchy)
                return;

            routine = StartCoroutine(Run());
        }

        [ContextMenu("Stop")]
        public void Stop()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private IEnumerator Run()
        {
            do
            {
                yield return MoveY(hiddenY, firstPeekY, firstMoveTime);

                if (pauseAtFirst > 0f)
                    yield return new WaitForSeconds(pauseAtFirst);

                yield return MoveY(firstPeekY, fullPeekY, secondMoveTime);

                if (pauseAtFull > 0f)
                    yield return new WaitForSeconds(pauseAtFull);

                yield return MoveY(fullPeekY, hiddenY, hideMoveTime);

                if (delayBetweenCycles > 0f)
                    yield return new WaitForSeconds(delayBetweenCycles);

            } while (loop);

            routine = null;
        }

        private IEnumerator MoveY(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetY(to);
                yield break;
            }

            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float eased = curve.Evaluate(t);
                SetY(Mathf.LerpUnclamped(from, to, eased));
                yield return null;
            }

            SetY(to);
        }

        private void SetY(float y)
        {
            if (target == null)
                return;

            localPos = target.localPosition;
            localPos.y = y;
            target.localPosition = localPos;
        }
    }
}