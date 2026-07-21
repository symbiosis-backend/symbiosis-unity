using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Core
{
    public static class TransformRotationSanitizer
    {
        private const float MinSqrMagnitude = 0.999f * 0.999f;
        private const float MaxSqrMagnitude = 1.001f * 1.001f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SanitizeLoadedScene()
        {
            SanitizeAllLoadedTransforms();
        }

        public static void SanitizeAllLoadedTransforms()
        {
            int fixedCount = 0;
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (Sanitize(transforms[i]))
                {
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                Debug.Log($"[Sanitizer] Normalized {fixedCount} invalid transform rotations.");
            }
        }

        public static bool Sanitize(Transform transform)
        {
            if (transform == null)
            {
                return false;
            }

            Quaternion rotation = transform.localRotation;
            float sqrMagnitude = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
            if (float.IsNaN(sqrMagnitude) || sqrMagnitude < MinSqrMagnitude)
            {
                transform.localRotation = Quaternion.identity;
                return true;
            }

            if (sqrMagnitude > MaxSqrMagnitude)
            {
                float magnitude = Mathf.Sqrt(sqrMagnitude);
                transform.localRotation = new Quaternion(rotation.x / magnitude, rotation.y / magnitude, rotation.z / magnitude, rotation.w / magnitude);
                return true;
            }

            return false;
        }
    }
}
