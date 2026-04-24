using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [SerializeField] private Vector3 profileModelEulerOffset = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector3 bearMaleLobbyPositionOffset = new Vector3(0f, -0.18f, 0f);
        [SerializeField] private Vector3 bearMaleLobbyEulerOffset = new Vector3(0f, 90f, -10f);
        [SerializeField] private Vector3 bearFemaleLobbyPositionOffset = new Vector3(0f, -0.30f, 0f);
        [SerializeField] private Vector3 bearFemaleLobbyEulerOffset = new Vector3(0f, 90f, -10f);
        [SerializeField] private Vector3 foxMaleLobbyEulerOffset = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector3 wolfMaleLobbyPositionOffset = new Vector3(0.42f, 0.12f, 0f);
        [SerializeField] private Vector3 wolfMaleLobbyEulerOffset = new Vector3(0f, 90f, 0f);
        [SerializeField] private Vector3 standingLobbyPositionOffset = new Vector3(0f, 0.12f, 0f);
        [SerializeField] private Vector3 standingBackFacingLobbyEulerOffset = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector3 tigerLobbyPositionOffset = new Vector3(0f, 0.27f, 0f);
        [SerializeField] private Vector3 tigerMaleLobbyEulerOffset = new Vector3(-16f, 180f, 0f);
        [SerializeField] private Vector3 foxFemaleBattleEulerOffset = new Vector3(0f, 90f, 0f);
        [SerializeField] private Vector3 tigerFemaleBattleEulerOffset = new Vector3(0f, 90f, 0f);
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [SerializeField] private bool mirrorX;

        [Header("UI Anchor")]
        [SerializeField] private bool followRectTransform = true;
        [SerializeField] private float cameraDistance = 6f;
        [SerializeField] private Vector3 worldOffset = Vector3.zero;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Image fallbackImage;
        [SerializeField] private bool hideFallbackImageWhenModelIsShown = true;

        [Header("UI Render")]
        [SerializeField] private bool renderModelToUiTexture = true;
        [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(512, 512);
        [SerializeField] private Color renderBackgroundColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Vector3 renderCameraPosition = new Vector3(0f, 1.2f, -5.5f);
        [SerializeField] private Vector3 renderLookAtOffset = new Vector3(0f, 1.05f, 0f);
        [SerializeField] private float renderFieldOfView = 28f;
        [SerializeField, Range(-1f, 2f)] private float lobbyRenderVerticalFrameOffset = 0f;
        [SerializeField, Range(-1f, 2f)] private float bearFemaleLobbyRenderVerticalFrameOffset = 0f;
        [SerializeField, Range(-1f, 2f)] private float foxMaleLobbyRenderVerticalFrameOffset = 0f;
        [SerializeField, Range(-1f, 2f)] private float tigerLobbyRenderVerticalFrameOffset = 0f;
        [SerializeField, Range(0.85f, 2.2f)] private float renderFitPadding = 1.06f;
        [SerializeField, Range(0.85f, 2.4f)] private float lobbyRenderFitPadding = 1.08f;
        [SerializeField, Range(0.85f, 2.2f)] private float battleRenderFitPadding = 1.06f;
        [SerializeField, Range(0.85f, 2.2f)] private float profileRenderFitPadding = 1.06f;
        [SerializeField, Range(0.1f, 2f)] private float previewLightIntensity = 0.95f;
        [SerializeField] private Color previewLightColor = new Color(0.98f, 0.96f, 0.9f, 1f);
        [SerializeField] private bool animatePreviewFallback = false;
        [SerializeField] private float fallbackSwayDegrees = 6f;
        [SerializeField] private float fallbackBobAmount = 0.035f;
        [SerializeField] private float fallbackAnimationSpeed = 1.2f;

        [Header("Battle Actions")]
        [SerializeField] private bool pauseBattleAnimationsUntilAction = true;
        [SerializeField] private string attackTriggerName = "Attack";
        [SerializeField] private string hitTriggerName = "Hit";
        [SerializeField, Min(0.05f)] private float actionReturnDelay = 0.55f;
        [SerializeField, Min(0.05f)] private float fallbackActionPulseDuration = 0.22f;
        [SerializeField, Min(0f)] private float fallbackAttackLunge = 0.08f;
        [SerializeField, Min(0f)] private float fallbackHitRecoil = 0.06f;

        private GameObject currentInstance;
        private GameObject currentPrefab;
        private string currentAddressKey;
        private AsyncOperationHandle<GameObject>? currentAddressHandle;
        private Coroutine loadRoutine;
        private int loadVersion;
        private BattleCharacterDatabase.BattleCharacterData currentData;
        private RectTransform rectTransform;
        private RawImage renderImage;
        private RenderTexture renderTexture;
        private Camera renderCamera;
        private Light renderLight;
        private int previewLayer = -1;
        private Vector3 previewWorldOrigin;
        private bool previewWorldOriginReady;
        private PlayableGraph animationGraph;
        private bool hasRuntimeAnimation;
        private Coroutine actionAnimationRoutine;
        private static int nextPreviewOriginId;

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
            if (currentInstance == null)
                return;

            if (!ShouldPauseBattleAnimation())
                UpdateFallbackPreviewAnimation();

            if (renderModelToUiTexture)
                UpdateRenderCameraFrame();
            else if (followRectTransform)
                FollowUiAnchor();
        }

        private void OnDestroy()
        {
            ClearInstance();

            if (modelRoot != null && modelRoot.gameObject != gameObject)
                Destroy(modelRoot.gameObject);

            ReleaseRenderTexture();
        }

        private void OnDisable()
        {
            StopActionAnimationRoutine();
            StopPlayableAnimation();
        }

        public bool Show(
            BattleCharacterDatabase.BattleCharacterData data,
            ModelContext modelContext,
            bool flipX = false)
        {
            context = modelContext;
            mirrorX = flipX;
            currentData = data;

            GameObject prefab = ResolveLocalModelPrefab(data);
            if (prefab != null && ShowLocalModel(data, prefab))
                return true;

            string addressKey = ResolveModelAddressKey(data);
            if (!string.IsNullOrWhiteSpace(addressKey))
            {
                EnsureModelRoot();
                BeginAddressableLoad(data, addressKey);

                if (hideFallbackImageWhenModelIsShown && fallbackImage != null)
                    fallbackImage.enabled = false;

                return true;
            }

            AssetReferenceGameObject address = ResolveModelAddress(data);
            if (BattleCharacterDatabase.BattleCharacterData.IsValidAddress(address))
            {
                EnsureModelRoot();
                BeginAddressableLoad(data, address.RuntimeKey);

                if (hideFallbackImageWhenModelIsShown && fallbackImage != null)
                    fallbackImage.enabled = false;

                return true;
            }

            if (!ShowLocalModel(data, prefab))
            {
                Hide();
                return false;
            }

            return true;
        }

        private bool ShowLocalModel(BattleCharacterDatabase.BattleCharacterData data, GameObject prefab = null)
        {
            prefab ??= ResolveLocalModelPrefab(data);
            if (prefab == null)
                return false;

            EnsureModelRoot();
            if (currentInstance == null || currentPrefab != prefab)
            {
                ClearInstance();
                currentPrefab = prefab;
                currentInstance = Instantiate(prefab, modelRoot);
                currentInstance.name = prefab.name;
                ApplyModelTexture(data);
                ApplyAnimator(data);
            }

            ApplyTransform();
            ShowRenderTexture();

            if (renderModelToUiTexture)
                SetupRenderPreview();
            else
                FollowUiAnchor();

            EnsurePreviewAnimation(data);

            if (hideFallbackImageWhenModelIsShown && fallbackImage != null)
                fallbackImage.enabled = false;

            return true;
        }

        public void Hide()
        {
            ClearInstance();
            currentPrefab = null;
            currentAddressKey = string.Empty;
            currentData = null;
        }

        public bool PlayAttackAnimation()
        {
            return PlayBattleAction(ResolveAttackClip(), attackTriggerName, fallbackAttackLunge);
        }

        public bool PlayHitAnimation()
        {
            return PlayBattleAction(ResolveHitClip(), hitTriggerName, -fallbackHitRecoil);
        }

        private void EnsureModelRoot()
        {
            if (modelRoot != null)
            {
                EnsurePreviewWorldOrigin();
                return;
            }

            GameObject root = new GameObject($"{name}_3DModelRoot");
            modelRoot = root.transform;
            EnsurePreviewWorldOrigin();
            modelRoot.position = previewWorldOrigin;
        }

        private void ClearInstance()
        {
            StopActionAnimationRoutine();
            StopPlayableAnimation();

            if (loadRoutine != null)
            {
                StopCoroutine(loadRoutine);
                loadRoutine = null;
            }

            loadVersion++;

            if (currentInstance == null)
            {
                ReleaseAddressableHandle();
                HideRenderTexture();
                return;
            }

            Destroy(currentInstance);
            currentInstance = null;
            ReleaseAddressableHandle();
            HideRenderTexture();
        }

        private void BeginAddressableLoad(
            BattleCharacterDatabase.BattleCharacterData data,
            object runtimeKey)
        {
            string key = runtimeKey == null ? string.Empty : runtimeKey.ToString();

            if (loadRoutine != null && string.Equals(currentAddressKey, key, System.StringComparison.Ordinal))
                return;

            if (currentInstance != null && string.Equals(currentAddressKey, key, System.StringComparison.Ordinal))
            {
                ApplyTransform();
                FollowUiAnchor();
                return;
            }

            ClearInstance();
            currentAddressKey = key;
            currentPrefab = null;

            int version = ++loadVersion;
            loadRoutine = StartCoroutine(LoadAddressableModel(data, runtimeKey, version));
        }

        private IEnumerator LoadAddressableModel(
            BattleCharacterDatabase.BattleCharacterData data,
            object runtimeKey,
            int version)
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(runtimeKey, typeof(GameObject));
            yield return locationsHandle;

            if (version != loadVersion)
            {
                if (locationsHandle.IsValid())
                    Addressables.Release(locationsHandle);

                yield break;
            }

            if (locationsHandle.Status != AsyncOperationStatus.Succeeded ||
                locationsHandle.Result == null ||
                locationsHandle.Result.Count == 0)
            {
                if (locationsHandle.IsValid())
                    Addressables.Release(locationsHandle);

                loadRoutine = null;
                currentAddressKey = string.Empty;
                ShowLocalModel(data);
                yield break;
            }

            if (locationsHandle.IsValid())
                Addressables.Release(locationsHandle);

            AsyncOperationHandle<GameObject> handle;
            try
            {
                handle = Addressables.LoadAssetAsync<GameObject>(runtimeKey);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BattleCharacterModelView] Addressable key not found: {runtimeKey}. Falling back to local model. {ex.GetType().Name}: {ex.Message}", this);
                loadRoutine = null;
                currentAddressKey = string.Empty;
                ShowLocalModel(data);
                yield break;
            }

            currentAddressHandle = handle;
            yield return handle;

            loadRoutine = null;

            if (version != loadVersion)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                yield break;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogWarning("[BattleCharacterModelView] Could not load addressable character model: " + runtimeKey + ". Falling back to local model.", this);
                ReleaseAddressableHandle();
                currentAddressKey = string.Empty;

                if (!ShowLocalModel(data))
                    Hide();

                yield break;
            }

            currentInstance = Instantiate(handle.Result, modelRoot);
            currentInstance.name = handle.Result.name;
            ApplyModelTexture(data);
            ApplyAnimator(data);
            ApplyTransform();
            ShowRenderTexture();

            if (renderModelToUiTexture)
                SetupRenderPreview();
            else
                FollowUiAnchor();

            EnsurePreviewAnimation(data);

            if (hideFallbackImageWhenModelIsShown && fallbackImage != null)
                fallbackImage.enabled = false;
        }

        private void ReleaseAddressableHandle()
        {
            if (!currentAddressHandle.HasValue)
                return;

            AsyncOperationHandle<GameObject> handle = currentAddressHandle.Value;
            if (handle.IsValid())
                Addressables.Release(handle);

            currentAddressHandle = null;
        }

        private AssetReferenceGameObject ResolveModelAddress(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return null;

            switch (context)
            {
                case ModelContext.Profile:
                    return BattleCharacterDatabase.BattleCharacterData.IsValidAddress(data.ProfileModelAddress)
                        ? data.ProfileModelAddress
                        : data.DisplayModelAddress;

                case ModelContext.Battle:
                    return data.CombatModelAddress;

                default:
                    return data.DisplayModelAddress;
            }
        }

        private string ResolveModelAddressKey(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (data == null)
                return string.Empty;

            switch (context)
            {
                case ModelContext.Profile:
                    return !string.IsNullOrWhiteSpace(data.ProfileModelAddressKey)
                        ? data.ProfileModelAddressKey
                        : data.DisplayModelKey;

                case ModelContext.Battle:
                    return data.CombatModelKey;

                default:
                    return data.DisplayModelKey;
            }
        }

        private GameObject ResolveLocalModelPrefab(BattleCharacterDatabase.BattleCharacterData data)
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
            hasRuntimeAnimation = false;

            if (animator != null && controller != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.enabled = true;
                if (ShouldPauseBattleAnimation())
                    PauseAnimatorAtRest(animator);
                else
                    hasRuntimeAnimation = true;

                return;
            }

            AnimationClip clip = ResolveIdleClip(data) ?? ResolveEmbeddedClipFromCurrentPrefab();
            if (clip == null)
            {
                Debug.LogWarning($"[BattleCharacterModelView] No animation clip found for {data.Id} {context}. Assign the FBX idle clip in BattleCharacterDatabase.", this);
                return;
            }

            Animator playableAnimator = animator != null ? animator : currentInstance.GetComponent<Animator>();
            if (playableAnimator == null)
            {
                playableAnimator = currentInstance.AddComponent<Animator>();
                Debug.Log($"[BattleCharacterModelView] Added Animator to preview root for {data.Id} {context}.", this);
            }

            if (playableAnimator != null)
            {
                playableAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (ShouldPauseBattleAnimation())
                {
                    SampleClipAtRest(clip);
                    playableAnimator.enabled = false;
                    hasRuntimeAnimation = false;
                    return;
                }

                Debug.Log($"[BattleCharacterModelView] Playing native animation clip '{clip.name}' for {data.Id} {context}. Avatar={(playableAnimator.avatar != null ? playableAnimator.avatar.name : "None")}", this);
                PlayClipWithPlayable(playableAnimator, clip);
                return;
            }

            Animation animation = currentInstance.GetComponentInChildren<Animation>(true);
            if (animation == null)
                animation = currentInstance.AddComponent<Animation>();

            if (clip.legacy)
            {
                if (ShouldPauseBattleAnimation())
                {
                    SampleClipAtRest(clip);
                    hasRuntimeAnimation = false;
                    return;
                }

                Debug.Log($"[BattleCharacterModelView] Playing legacy animation clip '{clip.name}' for {data.Id} {context}.", this);
                animation.clip = clip;
                animation.AddClip(clip, clip.name);
                animation.Play(clip.name);
                hasRuntimeAnimation = true;
                return;
            }

            Debug.LogWarning($"[BattleCharacterModelView] Clip '{clip.name}' could not be played for {data.Id} {context}: no Animator/legacy Animation path was available.", this);
        }

        private void EnsurePreviewAnimation(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (currentInstance == null || data == null || ShouldPauseBattleAnimation())
                return;

            if (hasRuntimeAnimation)
            {
                if (animationGraph.IsValid())
                    return;

                Animation legacyAnimation = currentInstance.GetComponentInChildren<Animation>(true);
                if (legacyAnimation != null && legacyAnimation.isPlaying)
                    return;

                Animator runningAnimator = currentInstance.GetComponentInChildren<Animator>(true);
                if (runningAnimator != null && runningAnimator.enabled && runningAnimator.runtimeAnimatorController != null)
                    return;
            }

            ApplyAnimator(data);
        }

        private bool PlayBattleAction(AnimationClip clip, string triggerName, float fallbackLocalX)
        {
            if (currentInstance == null)
                return false;

            StopActionAnimationRoutine();

            Animator animator = currentInstance.GetComponentInChildren<Animator>(true);
            if (clip != null && animator != null)
            {
                actionAnimationRoutine = StartCoroutine(PlayActionClipRoutine(animator, clip));
                return true;
            }

            if (animator != null && HasAnimatorTrigger(animator, triggerName))
            {
                actionAnimationRoutine = StartCoroutine(PlayTriggerActionRoutine(animator, triggerName));
                return true;
            }

            actionAnimationRoutine = StartCoroutine(PlayFallbackActionPulseRoutine(fallbackLocalX));
            return false;
        }

        private IEnumerator PlayActionClipRoutine(Animator animator, AnimationClip clip)
        {
            PlayClipWithPlayable(animator, clip);

            float delay = Mathf.Max(actionReturnDelay, clip != null ? clip.length : 0f);
            yield return new WaitForSeconds(delay);

            actionAnimationRoutine = null;

            if (currentInstance != null && currentData != null)
                ApplyAnimator(currentData);
        }

        private IEnumerator PlayTriggerActionRoutine(Animator animator, string triggerName)
        {
            if (animator == null)
                yield break;

            animator.enabled = true;
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);

            yield return new WaitForSeconds(Mathf.Max(0.05f, actionReturnDelay));

            actionAnimationRoutine = null;

            if (ShouldPauseBattleAnimation())
                PauseAnimatorAtRest(animator);
        }

        private IEnumerator PlayFallbackActionPulseRoutine(float localX)
        {
            if (currentInstance == null)
                yield break;

            Transform target = currentInstance.transform;
            Vector3 basePosition = ResolveContextPosition();
            Vector3 pulsePosition = basePosition + new Vector3(mirrorX ? -localX : localX, 0f, 0f);
            float duration = Mathf.Max(0.05f, fallbackActionPulseDuration);
            float halfDuration = duration * 0.5f;

            for (float t = 0f; t < halfDuration; t += Time.deltaTime)
            {
                if (target == null)
                    yield break;

                target.localPosition = Vector3.Lerp(basePosition, pulsePosition, t / halfDuration);
                yield return null;
            }

            for (float t = 0f; t < halfDuration; t += Time.deltaTime)
            {
                if (target == null)
                    yield break;

                target.localPosition = Vector3.Lerp(pulsePosition, basePosition, t / halfDuration);
                yield return null;
            }

            if (target != null)
                target.localPosition = basePosition;

            actionAnimationRoutine = null;
        }

        private void StopActionAnimationRoutine()
        {
            if (actionAnimationRoutine == null)
                return;

            StopCoroutine(actionAnimationRoutine);
            actionAnimationRoutine = null;
        }

        private static bool HasAnimatorTrigger(Animator animator, string triggerName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(triggerName))
                return false;

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter != null &&
                    parameter.type == AnimatorControllerParameterType.Trigger &&
                    string.Equals(parameter.name, triggerName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private AnimationClip ResolveAttackClip()
        {
            if (currentData == null)
                return ResolveEmbeddedActionClip("attack", "combat", "battle", "salute", "mixamo");

            return currentData.AttackAnimation != null
                ? currentData.AttackAnimation
                : ResolveDefaultBattleActionClip();
        }

        private AnimationClip ResolveHitClip()
        {
            if (currentData == null)
                return ResolveEmbeddedActionClip("hit", "damage", "hurt", "impact", "battle", "mixamo");

            return currentData.HitAnimation != null
                ? currentData.HitAnimation
                : ResolveDefaultBattleActionClip();
        }

        private AnimationClip ResolveDefaultBattleActionClip()
        {
            if (currentData == null)
                return ResolveEmbeddedActionClip("battle", "combat", "attack", "hit", "mixamo");

            return currentData.BattleIdleAnimation != null
                ? currentData.BattleIdleAnimation
                : ResolveEmbeddedActionClip("battle", "combat", "attack", "hit", "damage", "hurt", "mixamo");
        }

        private AnimationClip ResolveEmbeddedActionClip(params string[] nameHints)
        {
#if UNITY_EDITOR
            if (currentPrefab == null)
                return null;

            string path = AssetDatabase.GetAssetPath(currentPrefab);
            if (string.IsNullOrWhiteSpace(path))
                return null;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            AnimationClip firstClip = null;

            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                    continue;

                firstClip ??= clip;
                if (MatchesAnyNameHint(clip.name, nameHints))
                    return clip;
            }

            return firstClip;
#else
            return null;
#endif
        }

        private static bool MatchesAnyNameHint(string clipName, params string[] nameHints)
        {
            if (string.IsNullOrWhiteSpace(clipName) || nameHints == null)
                return false;

            for (int i = 0; i < nameHints.Length; i++)
            {
                string hint = nameHints[i];
                if (!string.IsNullOrWhiteSpace(hint) &&
                    clipName.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldPauseBattleAnimation()
        {
            return pauseBattleAnimationsUntilAction && context == ModelContext.Battle;
        }

        private void PauseAnimatorAtRest(Animator animator)
        {
            if (animator == null)
                return;

            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
            hasRuntimeAnimation = false;
        }

        private void SampleClipAtRest(AnimationClip clip)
        {
            if (clip == null || currentInstance == null)
                return;

            clip.SampleAnimation(currentInstance, 0f);
            hasRuntimeAnimation = false;
        }

        private void ApplyModelTexture(BattleCharacterDatabase.BattleCharacterData data)
        {
            if (currentInstance == null || data == null)
                return;

            Renderer[] renderers = currentInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Material[] materials = renderer.materials;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null)
                        continue;

                    if (data.UseSolidModelColor)
                    {
                        ApplySolidModelColor(material, data.SolidModelColor);
                        ConfigureLitPreviewMaterial(material);
                        continue;
                    }

                    if (data.ModelTexture != null)
                    {
                        material.mainTexture = data.ModelTexture;
                        ApplyTextureTransform(material, data.ModelTextureScale, data.ModelTextureOffset);

                        if (material.HasProperty("_BaseMap"))
                            material.SetTexture("_BaseMap", data.ModelTexture);

                        if (material.HasProperty("_MainTex"))
                            material.SetTexture("_MainTex", data.ModelTexture);

                        if (material.HasProperty("_BaseColorMap"))
                            material.SetTexture("_BaseColorMap", data.ModelTexture);

                        if (material.HasProperty("_DiffuseMap"))
                            material.SetTexture("_DiffuseMap", data.ModelTexture);

                        if (material.HasProperty("_AlbedoMap"))
                            material.SetTexture("_AlbedoMap", data.ModelTexture);

                        if (material.HasProperty("_BaseColor"))
                            material.SetColor("_BaseColor", Color.white);

                        if (material.HasProperty("_Color"))
                            material.SetColor("_Color", Color.white);
                    }

                    ConfigureLitPreviewMaterial(material);
                }
            }
        }

        private static void ConfigureLitPreviewMaterial(Material material)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.12f);

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.12f);

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);

            material.DisableKeyword("_EMISSION");
        }

        private static void ApplySolidModelColor(Material material, Color color)
        {
            if (material == null)
                return;

            ClearTexture(material, "_BaseMap");
            ClearTexture(material, "_MainTex");
            ClearTexture(material, "_BaseColorMap");
            ClearTexture(material, "_DiffuseMap");
            ClearTexture(material, "_AlbedoMap");
            material.mainTexture = null;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.18f);

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.18f);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
        }

        private static void ClearTexture(Material material, string propertyName)
        {
            if (material.HasProperty(propertyName))
                material.SetTexture(propertyName, null);
        }

        private static void ApplyTextureTransform(Material material, Vector2 scale, Vector2 offset)
        {
            if (material == null)
                return;

            if (scale == Vector2.zero)
                scale = Vector2.one;

            material.mainTextureScale = scale;
            material.mainTextureOffset = offset;

            SetTextureTransform(material, "_BaseMap", scale, offset);
            SetTextureTransform(material, "_MainTex", scale, offset);
            SetTextureTransform(material, "_BaseColorMap", scale, offset);
            SetTextureTransform(material, "_DiffuseMap", scale, offset);
            SetTextureTransform(material, "_AlbedoMap", scale, offset);
        }

        private static void SetTextureTransform(Material material, string propertyName, Vector2 scale, Vector2 offset)
        {
            if (!material.HasProperty(propertyName))
                return;

            material.SetTextureScale(propertyName, scale);
            material.SetTextureOffset(propertyName, offset);
        }

        private void PlayClipWithPlayable(Animator animator, AnimationClip clip)
        {
            if (animator == null || clip == null)
                return;

            StopPlayableAnimation();

            animator.enabled = true;
            animationGraph = PlayableGraph.Create($"{name}_PreviewAnimation");
            animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(animationGraph, clip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "PreviewAnimation", animator);
            output.SetSourcePlayable(clipPlayable);
            animationGraph.Play();
            hasRuntimeAnimation = true;
        }

        private void StopPlayableAnimation()
        {
            if (animationGraph.IsValid())
                animationGraph.Destroy();

            hasRuntimeAnimation = false;
        }

        private void UpdateFallbackPreviewAnimation()
        {
            if (!animatePreviewFallback || hasRuntimeAnimation || currentInstance == null)
                return;

            float t = Time.time * Mathf.Max(0.1f, fallbackAnimationSpeed);
            float sway = Mathf.Sin(t) * fallbackSwayDegrees;
            float bob = Mathf.Sin(t * 1.7f) * fallbackBobAmount;

            currentInstance.transform.localPosition = ResolveContextPosition() + new Vector3(0f, bob, 0f);
            currentInstance.transform.localRotation = Quaternion.Euler(ResolveContextEulerAngles() + new Vector3(0f, sway, 0f));
        }

        private AnimationClip ResolveEmbeddedClipFromCurrentPrefab()
        {
#if UNITY_EDITOR
            if (currentPrefab == null)
                return null;

            string path = AssetDatabase.GetAssetPath(currentPrefab);
            if (string.IsNullOrWhiteSpace(path))
                return null;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                    return clip;
            }
#endif
            return null;
        }

        private void ApplyTransform()
        {
            if (currentInstance == null)
                return;

            currentInstance.transform.localPosition = ResolveContextPosition();
            currentInstance.transform.localRotation = Quaternion.Euler(ResolveContextEulerAngles());

            Vector3 scale = modelLocalScale;
            if (mirrorX)
                scale.x = -Mathf.Abs(scale.x);

            currentInstance.transform.localScale = scale;
        }

        private Vector3 ResolveContextEulerAngles()
        {
            Vector3 eulerAngles = modelLocalEulerAngles;

            if (context == ModelContext.Profile)
                eulerAngles += profileModelEulerOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("BearMaleLobby"))
                eulerAngles += bearMaleLobbyEulerOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("BearFemaleLobby"))
                eulerAngles += bearFemaleLobbyEulerOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("FoxMaleLobby"))
                eulerAngles += foxMaleLobbyEulerOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("WolfMaleLobby"))
                eulerAngles += wolfMaleLobbyEulerOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("WolfFemaleLobby", "TigerFemaleLobby"))
                eulerAngles += standingBackFacingLobbyEulerOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("TigerMaleLobby"))
                eulerAngles += tigerMaleLobbyEulerOffset;

            if (context == ModelContext.Battle && IsCurrentPrefabNamed("FoxFemaleBattle"))
                eulerAngles += foxFemaleBattleEulerOffset;

            if (context == ModelContext.Battle && IsCurrentPrefabNamed("TigerFemaleBattle", "TigerFemaleHit"))
                eulerAngles += tigerFemaleBattleEulerOffset;

            if (context == ModelContext.Battle && IsCurrentPrefabNamed(
                    "BearMaleBattle",
                    "BearFemaleBattle",
                    "FoxMaleBattle",
                    "WolfMaleBattle",
                    "WolfFemaleBattle",
                    "TigerMaleBattle"))
                eulerAngles += new Vector3(0f, 90f, 0f);

            return eulerAngles;
        }

        private Vector3 ResolveContextPosition()
        {
            Vector3 position = modelLocalPosition;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("BearMaleLobby"))
                position += bearMaleLobbyPositionOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("BearFemaleLobby"))
                position += bearFemaleLobbyPositionOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("WolfMaleLobby"))
                position += wolfMaleLobbyPositionOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("WolfFemaleLobby"))
                position += standingLobbyPositionOffset;

            if (context == ModelContext.Lobby && IsCurrentPrefabNamed("TigerMaleLobby", "TigerFemaleLobby"))
                position += tigerLobbyPositionOffset;

            return position;
        }

        private bool IsCurrentPrefabNamed(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName))
                return false;

            if (currentPrefab != null && string.Equals(currentPrefab.name, prefabName, System.StringComparison.Ordinal))
                return true;

            return currentInstance != null &&
                   string.Equals(currentInstance.name, prefabName, System.StringComparison.Ordinal);
        }

        private bool IsCurrentPrefabNamed(params string[] prefabNames)
        {
            if (prefabNames == null)
                return false;

            for (int i = 0; i < prefabNames.Length; i++)
            {
                if (IsCurrentPrefabNamed(prefabNames[i]))
                    return true;
            }

            return false;
        }

        private void ShowRenderTexture()
        {
            if (!renderModelToUiTexture)
                return;

            EnsureRenderImage();

            if (renderImage != null)
                renderImage.enabled = renderImage.texture != null;
        }

        private void HideRenderTexture()
        {
            if (renderImage != null)
                renderImage.enabled = false;
        }

        private void EnsureRenderImage()
        {
            if (renderImage == null || renderImage.transform == transform)
                renderImage = FindChildRenderImage();

            if (renderImage == null)
            {
                GameObject imageObject = new GameObject("CharacterModelRawImage");
                imageObject.transform.SetParent(transform, false);
                imageObject.layer = gameObject.layer;
                renderImage = imageObject.AddComponent<RawImage>();

                RectTransform imageRect = renderImage.rectTransform;
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = Vector2.zero;
                imageRect.offsetMax = Vector2.zero;
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.localScale = Vector3.one;
            }

            renderImage.raycastTarget = false;
            renderImage.color = Color.white;
            renderImage.transform.SetAsLastSibling();
        }

        private RawImage FindChildRenderImage()
        {
            RawImage[] images = GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < images.Length; i++)
            {
                RawImage image = images[i];
                if (image != null && image.transform != transform)
                    return image;
            }

            return null;
        }

        private void SetupRenderPreview()
        {
            if (currentInstance == null)
                return;

            EnsureRenderImage();
            EnsureRenderTexture();
            EnsureRenderCamera();

            previewLayer = ResolvePreviewLayer();
            EnsurePreviewWorldOrigin();
            modelRoot.position = previewWorldOrigin;
            SetLayerRecursively(currentInstance, previewLayer);

            if (renderCamera != null)
            {
                renderCamera.targetTexture = renderTexture;
                renderCamera.cullingMask = 1 << previewLayer;
                renderCamera.clearFlags = CameraClearFlags.SolidColor;
                renderCamera.backgroundColor = renderBackgroundColor;
                renderCamera.fieldOfView = renderFieldOfView;
                renderCamera.enabled = true;
            }

            if (renderLight != null)
            {
                renderLight.cullingMask = 1 << previewLayer;
                renderLight.intensity = previewLightIntensity;
                renderLight.color = previewLightColor;
                renderLight.enabled = true;
            }

            if (renderImage != null)
            {
                renderImage.texture = renderTexture;
                renderImage.enabled = renderTexture != null;
            }

            UpdateRenderCameraFrame();
        }

        private void EnsureRenderTexture()
        {
            int width = Mathf.Max(128, renderTextureSize.x);
            int height = Mathf.Max(128, renderTextureSize.y);

            if (renderTexture != null && renderTexture.width == width && renderTexture.height == height)
                return;

            ReleaseRenderTexture();

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = $"{name}_CharacterPreviewRT",
                antiAliasing = 4,
                useMipMap = false,
                autoGenerateMips = false
            };

            renderTexture.Create();
        }

        private void EnsureRenderCamera()
        {
            if (renderCamera == null)
            {
                GameObject cameraObject = new GameObject($"{name}_PreviewCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                renderCamera = cameraObject.AddComponent<Camera>();
                renderCamera.nearClipPlane = 0.01f;
                renderCamera.farClipPlane = 100f;
                renderCamera.allowHDR = false;
                renderCamera.allowMSAA = true;
            }

            if (renderLight == null)
            {
                GameObject lightObject = new GameObject($"{name}_PreviewLight");
                lightObject.hideFlags = HideFlags.HideAndDontSave;
                renderLight = lightObject.AddComponent<Light>();
                renderLight.type = LightType.Directional;
                renderLight.intensity = previewLightIntensity;
                renderLight.color = previewLightColor;
                renderLight.transform.rotation = Quaternion.Euler(38f, -30f, 0f);
            }
        }

        private void UpdateRenderCameraFrame()
        {
            if (currentInstance == null || renderCamera == null)
                return;

            Bounds bounds = CalculateBounds(currentInstance);
            Vector3 target = bounds.size.sqrMagnitude > 0.0001f
                ? bounds.center
                : currentInstance.transform.position + renderLookAtOffset;

            float radius = Mathf.Max(0.65f, bounds.extents.magnitude);
            target += ResolveRenderFrameOffset(bounds, radius);

            float distance = ResolveRenderCameraDistance(bounds, radius);
            Vector3 cameraPosition = target + renderCameraPosition.normalized * distance;

            renderCamera.transform.position = cameraPosition;
            renderCamera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, Vector3.up);
        }

        private float ResolveRenderCameraDistance(Bounds bounds, float radius)
        {
            if (renderCamera == null)
                return Mathf.Max(2.2f, radius * ResolveRenderFitPadding());

            float aspect = renderTexture != null && renderTexture.height > 0
                ? renderTexture.width / (float)renderTexture.height
                : Mathf.Max(0.1f, renderCamera.aspect);

            float verticalFov = Mathf.Max(1f, renderCamera.fieldOfView) * Mathf.Deg2Rad;
            float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * Mathf.Max(0.1f, aspect));
            float distanceByHeight = Mathf.Max(0.1f, bounds.extents.y) / Mathf.Tan(verticalFov * 0.5f);
            float distanceByWidth = Mathf.Max(0.1f, bounds.extents.x) / Mathf.Tan(horizontalFov * 0.5f);
            float fitDistance = Mathf.Max(distanceByHeight, distanceByWidth) * ResolveRenderFitPadding();

            return Mathf.Max(2.2f, fitDistance);
        }

        private float ResolveRenderFitPadding()
        {
            switch (context)
            {
                case ModelContext.Lobby:
                    return lobbyRenderFitPadding;
                case ModelContext.Battle:
                    return battleRenderFitPadding;
                case ModelContext.Profile:
                    return profileRenderFitPadding;
                default:
                    return renderFitPadding;
            }
        }

        private Vector3 ResolveRenderFrameOffset(Bounds bounds, float radius)
        {
            if (context != ModelContext.Lobby)
                return Vector3.zero;

            float verticalReference = Mathf.Max(bounds.extents.y, radius * 0.35f);
            float offset = lobbyRenderVerticalFrameOffset;

            if (IsCurrentPrefabNamed("BearFemaleLobby"))
                offset = bearFemaleLobbyRenderVerticalFrameOffset;
            else if (IsCurrentPrefabNamed("FoxMaleLobby"))
                offset = foxMaleLobbyRenderVerticalFrameOffset;
            else if (IsCurrentPrefabNamed("TigerMaleLobby", "TigerFemaleLobby"))
                offset = tigerLobbyRenderVerticalFrameOffset;

            return Vector3.up * verticalReference * offset;
        }

        private Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return new Bounds(root.transform.position + renderLookAtOffset, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private int ResolvePreviewLayer()
        {
            int layer = LayerMask.NameToLayer("UI");
            return layer >= 0 ? layer : gameObject.layer;
        }

        private void EnsurePreviewWorldOrigin()
        {
            if (previewWorldOriginReady)
                return;

            int previewId = nextPreviewOriginId++;
            previewWorldOrigin = new Vector3((previewId % 1000) * 25f, -10000f, 0f);
            previewWorldOriginReady = true;
        }

        private void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null || layer < 0)
                return;

            target.layer = layer;

            for (int i = 0; i < target.transform.childCount; i++)
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }

        private void ReleaseRenderTexture()
        {
            DetachRenderTextureFromCameras();

            if (renderImage != null && renderImage.texture == renderTexture)
            {
                renderImage.texture = null;
                renderImage.enabled = false;
            }

            if (RenderTexture.active == renderTexture)
                RenderTexture.active = null;

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            if (renderCamera != null)
            {
                Destroy(renderCamera.gameObject);
                renderCamera = null;
            }

            if (renderLight != null)
            {
                Destroy(renderLight.gameObject);
                renderLight = null;
            }
        }

        private void DetachRenderTextureFromCameras()
        {
            if (renderTexture == null)
                return;

            if (renderCamera != null && renderCamera.targetTexture == renderTexture)
                renderCamera.targetTexture = null;

            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.targetTexture == renderTexture)
                    camera.targetTexture = null;
            }
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

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
            return cameras.Length > 0 ? cameras[0] : null;
        }
    }
}
