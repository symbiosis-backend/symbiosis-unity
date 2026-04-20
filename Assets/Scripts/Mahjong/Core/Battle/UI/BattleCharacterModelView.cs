using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class BattleCharacterModelView : MonoBehaviour
    {
        public enum ModelContext
        {
            Profile = 0,
            Lobby = 1,
            Battle = 2
        }

        [Header("Model")]
        [SerializeField] private ModelContext context = ModelContext.Lobby;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Vector3 modelLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 modelLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [SerializeField] private bool mirrorX;

        [Header("UI Anchor")]
        [SerializeField] private bool followRectTransform = true;
        [SerializeField] private float cameraDistance = 6f;
        [SerializeField] private Vector3 worldOffset = Vector3.zero;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Image fallbackImage;
        [SerializeField] private bool hideFallbackImageWhenModelIsShown = true;

        private GameObject currentInstance;
        private GameObject currentPrefab;
        private BattleCharacterDatabase.BattleCharacterData currentData;
        private RectTransform rectTransform;

        public bool HasModel => currentInstance != null;

        private void Awake()
        {
            rectTransform = transform as RectTransform;

            if (fallbackImage == null)
                fallbackImage = GetComponent<Image>();

            EnsureModelRoot();
        }

        private void LateUpdate()
        {
            if (currentInstance == null || !followRectTransform)
                return;

            FollowUiAnchor();
        }

        private void OnDestroy()
        {
            if (modelRoot != null && modelRoot.gameObject != gameObject)
                Destroy(modelRoot.gameObject);
        }

        public bool Show(
            BattleCharacterDatabase.BattleCharacterData data,
            ModelContext modelContext,
            bool flipX = false)
        {
            context = modelContext;
            mirrorX = flipX;
            currentData = data;

            GameObject prefab = ResolveModelPrefab(data);
            if (prefab == null)
            {
                Hide();
                return false;
            }

            EnsureModelRoot();

            if (currentInstance == null || currentPrefab != prefab)
            {
                ClearInstance();
                currentPrefab = prefab;
                currentInstance = Instantiate(prefab, modelRoot);
                currentInstance.name = prefab.name;
                ApplyAnimator(data);
            }

            ApplyTransform();
            FollowUiAnchor();

            if (hideFallbackImageWhenModelIsShown && fallbackImage != null)
                fallbackImage.enabled = false;

            return true;
        }

        public void Hide()
        {
            ClearInstance();
            currentPrefab = null;
            currentData = null;
        }

        private void EnsureModelRoot()
        {
            if (modelRoot != null)
                return;

            GameObject root = new GameObject($"{name}_3DModelRoot");
            modelRoot = root.transform;
        }

        private void ClearInstance()
        {
            if (currentInstance == null)
                return;

            Destroy(currentInstance);
            currentInstance = null;
        }

        private GameObject ResolveModelPrefab(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return null;

            switch (context)
            {
                case ModelContext.Profile:
                    return data.ProfileModelPrefab != null
                        ? data.ProfileModelPrefab
                        : data.DisplayModelPrefab;

                case ModelContext.Battle:
                    return data.CombatModelPrefab;

                default:
                    return data.DisplayModelPrefab;
            }
        }

        private RuntimeAnimatorController ResolveAnimatorController(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return null;

            switch (context)
            {
                case ModelContext.Profile:
                    return data.ProfileAnimatorController != null
                        ? data.ProfileAnimatorController
                        : data.LobbyAnimatorController;

                case ModelContext.Battle:
                    return data.BattleAnimatorController != null
                        ? data.BattleAnimatorController
                        : data.LobbyAnimatorController;

                default:
                    return data.LobbyAnimatorController;
            }
        }

        private AnimationClip ResolveIdleClip(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return null;

            switch (context)
            {
                case ModelContext.Profile:
                    return data.ProfileIdleAnimation != null
                        ? data.ProfileIdleAnimation
                        : data.LobbyIdleAnimation;

                case ModelContext.Battle:
                    return data.BattleIdleAnimation != null
                        ? data.BattleIdleAnimation
                        : data.LobbyIdleAnimation;

                default:
                    return data.LobbyIdleAnimation;
            }
        }

        private void ApplyAnimator(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (currentInstance == null || data == null)
                return;

            RuntimeAnimatorController controller = ResolveAnimatorController(data);
            Animator animator = currentInstance.GetComponentInChildren<Animator>(true);
            if (animator != null && controller != null)
            {
                animator.runtimeAnimatorController = controller;
                return;
            }

            AnimationClip clip = ResolveIdleClip(data);
            if (clip == null)
                return;

            Animation animation = currentInstance.GetComponentInChildren<Animation>(true);
            if (animation == null)
                animation = currentInstance.AddComponent<Animation>();

            animation.clip = clip;
            animation.AddClip(clip, clip.name);
            animation.Play(clip.name);
        }

        private void ApplyTransform()
        {
            if (currentInstance == null)
                return;

            currentInstance.transform.localPosition = modelLocalPosition;
            currentInstance.transform.localRotation = Quaternion.Euler(modelLocalEulerAngles);

            Vector3 scale = modelLocalScale;
            if (mirrorX)
                scale.x = -Mathf.Abs(scale.x);

            currentInstance.transform.localScale = scale;
        }

        private void FollowUiAnchor()
        {
            if (modelRoot == null)
                return;

            Camera camera = ResolveCamera();
            if (camera == null || rectTransform == null)
                return;

            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
            screenPoint.z = Mathf.Max(0.1f, cameraDistance);
            modelRoot.position = camera.ScreenToWorldPoint(screenPoint) + worldOffset;
            modelRoot.rotation = Quaternion.identity;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null)
                return targetCamera;

            targetCamera = Camera.main;
            if (targetCamera != null)
                return targetCamera;

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return cameras.Length > 0 ? cameras[0] : null;
        }
    }
}
