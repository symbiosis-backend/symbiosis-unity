using System.Collections;
using UnityEngine;

namespace VoidSurvivor
{
    [DisallowMultipleComponent]
    public sealed class ScreenShake2D : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private Coroutine routine;
        private Vector3 basePosition;

        private void Awake()
        {
            if (target == null)
                target = transform;

            basePosition = target.localPosition;
        }

        public void Shake(float duration, float strength)
        {
            if (target == null)
                return;

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(ShakeRoutine(duration, strength));
        }

        private IEnumerator ShakeRoutine(float duration, float strength)
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                target.localPosition = basePosition + (Vector3)(Random.insideUnitCircle * strength);
                yield return null;
            }

            target.localPosition = basePosition;
            routine = null;
        }
    }
}
