using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using MahjongGame;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dynasty.Legacy.Symbioz
{
    [DisallowMultipleComponent]
    public sealed class SymbiozNetworkPawn : NetworkBehaviour
    {
        private const float BaseSpeedCellsPerSecond = 4.2f;
        private const float CellSize = 1f;
        private const float StateSendInterval = 1f / 24f;
        private const float OwnerVisualSmoothTime = 0.035f;
        private const float RemoteVisualSmoothTime = 0.095f;
        private const float RemoteInterpolationBackTime = 0.18f;
        private const float RemoteMaxExtrapolationTime = 0.08f;
        private const float OwnerPredictionBlend = 0.55f;
        private const float RemoteSnapDistance = 4.5f;
        private const float InputSendInterval = 1f / 30f;
        private const float InputIdleResendInterval = 0.18f;
        private const float InputChangeEpsilon = 0.0001f;
        private const float MeetInteractionDistance = 2.25f;
        private const float MeetClickRayDistance = 200f;
        private const int MaxBufferedStates = 12;
        private const string UnknownStatusLabel = "Citizen";
        private const string MeetPrefsPrefix = "DynastyLegacySymbiozMeetKnown_";

        private Vector2 serverInput;
        private Vector2 serverFacing = Vector2.down;
        private Vector3 serverVelocity;
        private Vector3 targetPosition;
        private Vector2 targetFacing = Vector2.down;
        private bool targetMoving;
        private float nextStateSendTime;
        private float nextInputSendTime;
        private float nextInputLogTime;
        private bool hasSentInput;
        private Vector2 lastSentInput;
        private Vector2 predictedOwnerInput;
        private Vector3 visualSmoothVelocity;
        private MeshRenderer bodyRenderer;
        private MeshRenderer headRenderer;
        private TextMeshPro identityLabel;
        private BoxCollider interactionCollider;
        private string publicProfileId;
        private string displayName;
        private string dynastyName;
        private int profileAge;
        private bool profileSubmitted;
        private RectTransform socialPanel;
        private TextMeshProUGUI socialTitleText;
        private TextMeshProUGUI socialBodyText;
        private Button meetButton;
        private Button acceptMeetButton;
        private Button declineMeetButton;
        private Button closeSocialButton;
        private Transform visualRoot;
        private int activeTargetOwnerId = -1;
        private int pendingRequesterOwnerId = -1;
        private readonly List<NetworkState> stateBuffer = new List<NetworkState>(MaxBufferedStates);

        public static Transform LocalOwnedTransform { get; private set; }
        public static Vector2 LocalNavigationInput { get; private set; }

        public static void SetLocalNavigationInput(Vector2 input)
        {
            LocalNavigationInput = input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private readonly struct NetworkState
        {
            public readonly Vector3 Position;
            public readonly Vector3 Velocity;
            public readonly Vector2 Facing;
            public readonly bool Moving;
            public readonly float ServerTime;
            public readonly float ReceivedTime;

            public NetworkState(Vector3 position, Vector3 velocity, Vector2 facing, bool moving, float serverTime, float receivedTime)
            {
                Position = position;
                Velocity = velocity;
                Facing = facing;
                Moving = moving;
                ServerTime = serverTime;
                ReceivedTime = receivedTime;
            }
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            DisableRuntimePrefabRenderer();
            targetPosition = transform.position;
            stateBuffer.Clear();
            AddBufferedState(new NetworkState(
                targetPosition,
                Vector3.zero,
                targetFacing,
                false,
                Time.time,
                Time.unscaledTime));
            if (ShouldCreateVisuals())
                EnsureVisuals();
            int ownerId = Owner != null ? Owner.ClientId : -1;
            int localId = LocalConnection != null ? LocalConnection.ClientId : -1;
            Debug.Log($"[SymbiozNetworkPawn] Started object={name} ownerLocal={Owner.IsLocalClient} ownerId={ownerId} localId={localId} server={IsServer} client={IsClient} pos={transform.position}");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            UpdateLocalOwnedReference();
            LogClientOwnership("OnStartClient");
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            UpdateLocalOwnedReference();
            LogClientOwnership("OnOwnershipClient");
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (LocalOwnedTransform == transform)
                LocalOwnedTransform = null;
            LocalNavigationInput = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (LocalOwnedTransform == transform)
                LocalOwnedTransform = null;
            LocalNavigationInput = Vector2.zero;
        }

        private void UpdateLocalOwnedReference()
        {
            if (IsOwner && IsClient)
                LocalOwnedTransform = transform;
        }

        private void LogClientOwnership(string source)
        {
            int ownerId = Owner != null ? Owner.ClientId : -1;
            int localId = LocalConnection != null ? LocalConnection.ClientId : -1;
            Debug.Log($"[SymbiozNetworkPawn] {source} object={name} isOwner={IsOwner} ownerLocal={Owner.IsLocalClient} ownerId={ownerId} localId={localId} server={IsServer} client={IsClient} pos={transform.position}");
        }

        private void Update()
        {
            if (ShouldCreateVisuals())
                EnsureVisuals();

            if (IsOwner && IsClient)
            {
                SubmitProfileIfNeeded();
                HandleSocialPointerInput();
            }

            if (IsOwner && IsClient)
                SendOwnerInput();

            if (IsServer)
                ServerMove();

            if (IsClient)
                SmoothClientTransform();
        }

        private void SubmitProfileIfNeeded()
        {
            if (profileSubmitted)
                return;

            ResolveLocalProfile(out string resolvedPublicId, out string resolvedName, out string resolvedDynasty, out int resolvedAge);
            profileSubmitted = true;
            ApplyProfile(Owner != null ? Owner.ClientId : -1, resolvedPublicId, resolvedName, resolvedDynasty, resolvedAge);
            SubmitProfileServerRpc(resolvedPublicId, resolvedName, resolvedDynasty, resolvedAge);
        }

        private void SendOwnerInput()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (keyboard == null)
            {
                input = LocalNavigationInput;
            }
            else
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    input.x += 1f;

                if (input.sqrMagnitude <= 0.001f)
                    input = LocalNavigationInput;
            }

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            float now = Time.unscaledTime;
            bool changed = !hasSentInput || (input - lastSentInput).sqrMagnitude > InputChangeEpsilon;
            if (!changed && now < nextInputSendTime)
                return;

            hasSentInput = true;
            lastSentInput = input;
            predictedOwnerInput = input;
            nextInputSendTime = now + (input.sqrMagnitude > 0.001f ? InputSendInterval : InputIdleResendInterval);
            SubmitInputServerRpc(input.x, input.y, now, Channel.Unreliable);
        }

        private void ServerMove()
        {
            Vector2 input = serverInput;
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            bool moving = input.sqrMagnitude > 0.001f;
            if (moving)
                serverFacing = input.normalized;

            serverVelocity = new Vector3(input.x, 0f, input.y) * (BaseSpeedCellsPerSecond * CellSize);
            transform.position += serverVelocity * Time.deltaTime;

            if (Time.time >= nextStateSendTime)
            {
                nextStateSendTime = Time.time + StateSendInterval;
                BroadcastStateObserversRpc(
                    transform.position,
                    serverVelocity,
                    serverFacing.x,
                    serverFacing.y,
                    moving,
                    Time.time,
                    Channel.Unreliable);
            }
        }

        private void SmoothClientTransform()
        {
            if (IsServer)
                return;

            Vector3 desired = IsOwner ? ResolveOwnerPredictedPosition() : ResolveBufferedRemotePosition();
            float distance = Vector3.Distance(transform.position, desired);
            if (distance > RemoteSnapDistance)
            {
                transform.position = desired;
                visualSmoothVelocity = Vector3.zero;
                return;
            }

            float smoothTime = IsOwner ? OwnerVisualSmoothTime : RemoteVisualSmoothTime;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref visualSmoothVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime);
        }

        private Vector3 ResolveOwnerPredictedPosition()
        {
            Vector3 desired = targetPosition;
            if (predictedOwnerInput.sqrMagnitude <= 0.001f)
                return desired;

            Vector2 input = predictedOwnerInput;
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Vector3 predicted = transform.position + new Vector3(input.x, 0f, input.y) * (BaseSpeedCellsPerSecond * CellSize * Time.deltaTime);
            return Vector3.Lerp(desired, predicted, OwnerPredictionBlend);
        }

        private Vector3 ResolveBufferedRemotePosition()
        {
            if (stateBuffer.Count == 0)
                return targetPosition;

            if (stateBuffer.Count == 1)
            {
                NetworkState only = stateBuffer[0];
                float extrapolate = Mathf.Min(Time.unscaledTime - only.ReceivedTime, RemoteMaxExtrapolationTime);
                return only.Position + only.Velocity * extrapolate;
            }

            float renderTime = Time.time - RemoteInterpolationBackTime;
            for (int i = 0; i < stateBuffer.Count - 1; i++)
            {
                NetworkState older = stateBuffer[i];
                NetworkState newer = stateBuffer[i + 1];
                if (older.ServerTime > renderTime || newer.ServerTime < renderTime)
                    continue;

                float span = Mathf.Max(0.0001f, newer.ServerTime - older.ServerTime);
                float t = Mathf.Clamp01((renderTime - older.ServerTime) / span);
                return Vector3.LerpUnclamped(older.Position, newer.Position, t);
            }

            NetworkState latest = stateBuffer[stateBuffer.Count - 1];
            float extraSeconds = Mathf.Min(Mathf.Max(0f, renderTime - latest.ServerTime), RemoteMaxExtrapolationTime);
            return latest.Position + latest.Velocity * extraSeconds;
        }

        [ServerRpc]
        private void SubmitInputServerRpc(float x, float y, float clientRealtime, Channel channel = Channel.Unreliable)
        {
            serverInput = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
            if (serverInput.sqrMagnitude > 1f)
                serverInput.Normalize();

            if (serverInput.sqrMagnitude > 0.001f && Time.time >= nextInputLogTime)
            {
                nextInputLogTime = Time.time + 1f;
                Debug.Log($"[SymbiozNetworkPawn] Server input object={name} input={serverInput} pos={transform.position}");
            }
        }

        [ServerRpc]
        private void SubmitProfileServerRpc(string profileId, string nick, string dynasty, int age)
        {
            int ownerId = Owner != null ? Owner.ClientId : -1;
            ApplyProfile(ownerId, profileId, nick, dynasty, age);
            BroadcastProfileObserversRpc(ownerId, publicProfileId, displayName, dynastyName, profileAge);
        }

        [ObserversRpc(BufferLast = true)]
        private void BroadcastProfileObserversRpc(int ownerId, string profileId, string nick, string dynasty, int age)
        {
            ApplyProfile(ownerId, profileId, nick, dynasty, age);
        }

        [ServerRpc]
        private void RequestMeetServerRpc(int targetOwnerId)
        {
            SymbiozNetworkPawn target = FindPawnByOwnerId(targetOwnerId);
            if (target == null || target == this || target.Owner == null)
                return;

            if (Vector3.Distance(transform.position, target.transform.position) > MeetInteractionDistance)
                return;

            EnsureServerIdentityFallback();
            target.TargetReceiveMeetRequest(target.Owner, Owner != null ? Owner.ClientId : -1, publicProfileId, displayName, dynastyName, profileAge);
        }

        [ServerRpc]
        private void RespondMeetServerRpc(int requesterOwnerId, bool accepted)
        {
            SymbiozNetworkPawn requester = FindPawnByOwnerId(requesterOwnerId);
            if (requester == null || requester == this || requester.Owner == null || Owner == null)
                return;

            if (Vector3.Distance(transform.position, requester.transform.position) > MeetInteractionDistance + 1f)
                return;

            EnsureServerIdentityFallback();
            requester.EnsureServerIdentityFallback();

            if (!accepted)
            {
                requester.TargetMeetDeclined(requester.Owner, Owner.ClientId);
                return;
            }

            TargetMarkMet(Owner, requester.Owner.ClientId, requester.publicProfileId, requester.displayName, requester.dynastyName, requester.profileAge);
            requester.TargetMarkMet(requester.Owner, Owner.ClientId, publicProfileId, displayName, dynastyName, profileAge);
        }

        [TargetRpc]
        private void TargetReceiveMeetRequest(NetworkConnection connection, int requesterOwnerId, string requesterProfileId, string requesterName, string requesterDynasty, int requesterAge)
        {
            pendingRequesterOwnerId = requesterOwnerId;
            ShowMeetRequestPanel(requesterOwnerId, requesterProfileId, requesterName, requesterDynasty, requesterAge);
        }

        [TargetRpc]
        private void TargetMarkMet(NetworkConnection connection, int otherOwnerId, string otherProfileId, string otherName, string otherDynasty, int otherAge)
        {
            MarkKnownProfile(otherProfileId);
            SymbiozNetworkPawn other = FindPawnByOwnerId(otherOwnerId);
            if (other != null)
                other.ApplyProfile(otherOwnerId, otherProfileId, otherName, otherDynasty, otherAge);

            ShowMetConfirmedPanel(otherProfileId, otherName, otherDynasty, otherAge);
            RefreshEveryIdentityLabel();
        }

        [TargetRpc]
        private void TargetMeetDeclined(NetworkConnection connection, int responderOwnerId)
        {
            SymbiozNetworkPawn responder = FindPawnByOwnerId(responderOwnerId);
            string label = responder != null ? responder.ResolveVisibleName() : UnknownStatusLabel;
            ShowInfoPanel("Meet declined", $"{label} declined the introduction.");
        }

        [ObserversRpc(BufferLast = true)]
        private void BroadcastStateObserversRpc(
            Vector3 position,
            Vector3 velocity,
            float facingX,
            float facingY,
            bool moving,
            float serverTime,
            Channel channel = Channel.Unreliable)
        {
            targetPosition = position;
            serverVelocity = velocity;
            targetFacing = new Vector2(facingX, facingY);
            targetMoving = moving;

            if (!IsOwner)
            {
                AddBufferedState(new NetworkState(
                    position,
                    velocity,
                    targetFacing,
                    moving,
                    serverTime,
                    Time.unscaledTime));
                return;
            }

            float maxCorrection = moving ? 0.75f : 0.35f;
            if (Vector3.Distance(transform.position, position) > maxCorrection)
                transform.position = position;
        }

        private void AddBufferedState(NetworkState state)
        {
            if (stateBuffer.Count > 0 && state.ServerTime < stateBuffer[stateBuffer.Count - 1].ServerTime)
                return;

            stateBuffer.Add(state);
            while (stateBuffer.Count > MaxBufferedStates)
                stateBuffer.RemoveAt(0);
        }

        private void EnsureVisuals()
        {
            if (!ShouldCreateVisuals())
                return;

            DisableRuntimePrefabRenderer();
            EnsureInteractionCollider();

            if (bodyRenderer != null && headRenderer != null && identityLabel != null)
            {
                ApplyOwnedVisualVisibility();
                RefreshIdentityLabel();
                return;
            }

            visualRoot = transform.Find("NetworkArchitectVisual");
            if (visualRoot == null)
            {
                GameObject root = new GameObject("NetworkArchitectVisual");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                visualRoot = root.transform;
            }

            bodyRenderer = EnsurePart(visualRoot, "Body", new Vector3(0f, 0.35f, 0f), new Vector3(0.42f, 0.72f, 0.28f), new Color(0.18f, 0.38f, 0.68f, 1f));
            headRenderer = EnsurePart(visualRoot, "Head", new Vector3(0f, 0.86f, 0f), new Vector3(0.34f, 0.26f, 0.34f), new Color(0.95f, 0.68f, 0.44f, 1f));
            EnsurePart(visualRoot, "Shadow", new Vector3(0f, -0.04f, 0f), new Vector3(0.55f, 0.025f, 0.36f), new Color(0.03f, 0.03f, 0.03f, 0.45f));
            EnsureIdentityLabel(visualRoot);
            ApplyOwnedVisualVisibility();
            RefreshIdentityLabel();
        }

        private void ApplyOwnedVisualVisibility()
        {
            if (visualRoot != null)
                visualRoot.gameObject.SetActive(!IsLocallyOwned());
        }

        private void DisableRuntimePrefabRenderer()
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;
        }

        private bool IsLocallyOwned()
        {
            if (IsOwner)
                return true;
            if (Owner != null && Owner.IsLocalClient)
                return true;
            return Owner != null && LocalConnection != null && Owner.ClientId == LocalConnection.ClientId;
        }

        private void EnsureInteractionCollider()
        {
            if (interactionCollider == null)
                interactionCollider = GetComponent<BoxCollider>();
            if (interactionCollider == null)
                interactionCollider = gameObject.AddComponent<BoxCollider>();

            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 0.62f, 0f);
            interactionCollider.size = new Vector3(1.65f, 1.55f, 1.65f);
        }

        private void EnsureIdentityLabel(Transform visual)
        {
            if (identityLabel != null)
                return;

            Transform labelTransform = visual.Find("IdentityLabel");
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject("IdentityLabel");
                labelObject.transform.SetParent(visual, false);
                labelObject.transform.localPosition = new Vector3(0f, 1.08f, -0.72f);
                labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                labelTransform = labelObject.transform;
            }

            identityLabel = labelTransform.GetComponent<TextMeshPro>();
            if (identityLabel == null)
                identityLabel = labelTransform.gameObject.AddComponent<TextMeshPro>();

            identityLabel.alignment = TextAlignmentOptions.Center;
            identityLabel.fontSize = 1.8f;
            identityLabel.rectTransform.sizeDelta = new Vector2(3.4f, 0.9f);
            identityLabel.color = new Color(0.82f, 0.95f, 1f, 1f);
        }

        private void HandleSocialPointerInput()
        {
            if (Application.isBatchMode)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                TryOpenPawnUnderScreenPoint(mouse.position.ReadValue());

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasReleasedThisFrame)
                TryOpenPawnUnderScreenPoint(touchscreen.primaryTouch.position.ReadValue());
        }

        private void TryOpenPawnUnderScreenPoint(Vector2 screenPoint)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            Ray ray = camera.ScreenPointToRay(screenPoint);
            RaycastHit[] hits = Physics.RaycastAll(ray, MeetClickRayDistance);
            if (hits == null || hits.Length == 0)
                return;

            SymbiozNetworkPawn best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                SymbiozNetworkPawn candidate = hits[i].collider != null
                    ? hits[i].collider.GetComponentInParent<SymbiozNetworkPawn>()
                    : null;
                if (candidate == null || candidate == this || candidate.IsOwner)
                    continue;

                float worldDistance = Vector3.Distance(transform.position, candidate.transform.position);
                if (worldDistance > MeetInteractionDistance || worldDistance >= bestDistance)
                    continue;

                best = candidate;
                bestDistance = worldDistance;
            }

            if (best != null)
                ShowRemotePlayerPanel(best, bestDistance);
        }

        private void ShowRemotePlayerPanel(SymbiozNetworkPawn target, float distance)
        {
            if (target == null)
                return;

            EnsureSocialUi();
            activeTargetOwnerId = target.Owner != null ? target.Owner.ClientId : -1;
            pendingRequesterOwnerId = -1;
            bool known = target.IsKnownToLocalClient();

            socialTitleText.text = known ? "Player status" : "Unknown player";
            socialBodyText.text = known
                ? $"{target.ResolveVisibleName()}\nDynasty: {SafeText(target.dynastyName, "Unknown")}\nAge: {(target.profileAge > 0 ? target.profileAge.ToString() : "Unknown")}\nDistance: {distance:0.0} cells"
                : $"{UnknownStatusLabel}\nYou have not met this player yet.\nDistance: {distance:0.0} cells";

            SetButtonVisible(meetButton, !known && activeTargetOwnerId >= 0);
            SetButtonVisible(acceptMeetButton, false);
            SetButtonVisible(declineMeetButton, false);
            socialPanel.gameObject.SetActive(true);
        }

        private void ShowMeetRequestPanel(int requesterOwnerId, string requesterProfileId, string requesterName, string requesterDynasty, int requesterAge)
        {
            EnsureSocialUi();
            bool known = IsKnownProfile(requesterProfileId);
            string requesterLabel = known && !string.IsNullOrWhiteSpace(requesterName) ? requesterName.Trim() : UnknownStatusLabel;
            socialTitleText.text = "Meet request";
            socialBodyText.text = $"{requesterLabel} wants to meet you.\nDo you want to meet?";
            SetButtonVisible(meetButton, false);
            SetButtonVisible(acceptMeetButton, true);
            SetButtonVisible(declineMeetButton, true);
            socialPanel.gameObject.SetActive(true);
        }

        private void ShowMetConfirmedPanel(string otherProfileId, string otherName, string otherDynasty, int otherAge)
        {
            EnsureSocialUi();
            socialTitleText.text = "Introduced";
            socialBodyText.text =
                $"{SafeText(otherName, "Player")}\n" +
                $"Dynasty: {SafeText(otherDynasty, "Unknown")}\n" +
                $"Age: {(otherAge > 0 ? otherAge.ToString() : "Unknown")}";
            SetButtonVisible(meetButton, false);
            SetButtonVisible(acceptMeetButton, false);
            SetButtonVisible(declineMeetButton, false);
            socialPanel.gameObject.SetActive(true);
        }

        private void ShowInfoPanel(string title, string body)
        {
            EnsureSocialUi();
            socialTitleText.text = title;
            socialBodyText.text = body;
            SetButtonVisible(meetButton, false);
            SetButtonVisible(acceptMeetButton, false);
            SetButtonVisible(declineMeetButton, false);
            socialPanel.gameObject.SetActive(true);
        }

        private void EnsureSocialUi()
        {
            if (socialPanel != null)
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("SymbiozSocialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            RectTransform root = canvas.transform as RectTransform;
            socialPanel = CreateSocialPanel(root);
            socialPanel.gameObject.SetActive(false);
        }

        private RectTransform CreateSocialPanel(RectTransform parent)
        {
            GameObject panelObject = new GameObject("HUD_PlayerMeetStatus", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 430f);
            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0.025f, 0.034f, 0.031f, 0.985f);

            socialTitleText = CreateSocialText(rect, "Title", "Player status", 34f, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(34f, -30f), new Vector2(560f, 54f));
            socialBodyText = CreateSocialText(rect, "Body", string.Empty, 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Vector2(36f, -98f), new Vector2(688f, 190f));

            meetButton = CreateSocialButton(rect, "MeetButton", "Meet", new Vector2(-168f, 48f), new Vector2(170f, 58f), SendMeetRequestForActiveTarget);
            acceptMeetButton = CreateSocialButton(rect, "AcceptMeet", "Yes", new Vector2(-168f, 48f), new Vector2(170f, 58f), AcceptPendingMeet);
            declineMeetButton = CreateSocialButton(rect, "DeclineMeet", "No", new Vector2(32f, 48f), new Vector2(170f, 58f), DeclinePendingMeet);
            closeSocialButton = CreateSocialButton(rect, "Close", "X", new Vector2(316f, -38f), new Vector2(76f, 54f), HideSocialPanel);
            SetButtonVisible(acceptMeetButton, false);
            SetButtonVisible(declineMeetButton, false);
            return rect;
        }

        private static TextMeshProUGUI CreateSocialText(RectTransform parent, string name, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static Button CreateSocialButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.13f, 0.24f, 0.22f, 0.96f);
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);
            CreateSocialText(rect, "Label", label, 24f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 0f), size).rectTransform.anchorMin = Vector2.zero;
            TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                text.rectTransform.anchoredPosition = Vector2.zero;
                text.rectTransform.sizeDelta = Vector2.zero;
            }

            return button;
        }

        private void SendMeetRequestForActiveTarget()
        {
            if (activeTargetOwnerId < 0)
                return;

            RequestMeetServerRpc(activeTargetOwnerId);
            ShowInfoPanel("Meet request", "Request sent. Waiting for answer.");
        }

        private void AcceptPendingMeet()
        {
            if (pendingRequesterOwnerId < 0)
                return;

            RespondMeetServerRpc(pendingRequesterOwnerId, true);
            ShowInfoPanel("Meet request", "Answer sent.");
        }

        private void DeclinePendingMeet()
        {
            if (pendingRequesterOwnerId >= 0)
                RespondMeetServerRpc(pendingRequesterOwnerId, false);
            HideSocialPanel();
        }

        private void HideSocialPanel()
        {
            if (socialPanel != null)
                socialPanel.gameObject.SetActive(false);
        }

        private void ApplyProfile(int ownerId, string profileId, string nick, string dynasty, int age)
        {
            publicProfileId = SafeText(profileId, ownerId >= 0 ? "owner-" + ownerId : string.Empty);
            displayName = SafeText(nick, ownerId >= 0 ? "Architect-" + ownerId.ToString("000") : "Architect");
            dynastyName = SafeText(dynasty, "Unknown Dynasty");
            profileAge = Mathf.Clamp(age, 0, 120);
            RefreshIdentityLabel();
        }

        private void EnsureServerIdentityFallback()
        {
            if (Owner != null && string.IsNullOrWhiteSpace(publicProfileId))
                ApplyProfile(Owner.ClientId, "owner-" + Owner.ClientId, "Architect-" + Owner.ClientId.ToString("000"), "Unknown Dynasty", 0);
        }

        private void RefreshIdentityLabel()
        {
            if (identityLabel == null)
                return;

            identityLabel.text = ResolveVisibleName();
            identityLabel.color = IsOwner || IsKnownToLocalClient()
                ? new Color(0.72f, 1f, 0.82f, 1f)
                : new Color(0.86f, 0.91f, 0.96f, 1f);
        }

        private string ResolveVisibleName()
        {
            if (IsOwner || IsKnownToLocalClient())
            {
                string nick = SafeText(displayName, "Architect");
                return string.IsNullOrWhiteSpace(dynastyName) ? nick : $"{nick}\n{dynastyName}";
            }

            return UnknownStatusLabel;
        }

        private bool IsKnownToLocalClient()
        {
            return IsOwner || IsKnownProfile(publicProfileId);
        }

        private static bool IsKnownProfile(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return false;

            string localId = ResolveLocalPublicProfileId();
            if (string.IsNullOrWhiteSpace(localId))
                return false;

            return string.Equals(localId, profileId, System.StringComparison.OrdinalIgnoreCase)
                || PlayerPrefs.GetInt(MakeMeetPrefsKey(localId, profileId), 0) == 1;
        }

        private static void MarkKnownProfile(string profileId)
        {
            string localId = ResolveLocalPublicProfileId();
            if (string.IsNullOrWhiteSpace(localId) || string.IsNullOrWhiteSpace(profileId))
                return;

            PlayerPrefs.SetInt(MakeMeetPrefsKey(localId, profileId), 1);
            PlayerPrefs.Save();
        }

        private static string MakeMeetPrefsKey(string localId, string profileId)
        {
            return MeetPrefsPrefix + SanitizeKey(localId) + "_" + SanitizeKey(profileId);
        }

        private static string ResolveLocalPublicProfileId()
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null && !string.IsNullOrWhiteSpace(profile.PublicPlayerId))
                return profile.PublicPlayerId.Trim();

            string suffix = ClientProfileScope.Suffix;
            return string.IsNullOrWhiteSpace(suffix) ? "local-default" : "local-" + suffix;
        }

        private static void ResolveLocalProfile(out string profileId, out string nick, out string dynasty, out int age)
        {
            PlayerProfile profile = ProfileService.I != null ? ProfileService.I.Current : null;
            if (profile != null)
            {
                profileId = SafeText(profile.PublicPlayerId, ResolveLocalPublicProfileId());
                nick = SafeText(profile.DisplayName, "Architect");
                dynasty = SafeText(profile.DynastyName, "Unknown Dynasty");
                age = Mathf.Clamp(profile.Age, 0, 120);
                return;
            }

            string suffix = ClientProfileScope.Suffix;
            profileId = ResolveLocalPublicProfileId();
            nick = string.IsNullOrWhiteSpace(suffix) ? "Architect" : "Architect-" + suffix;
            dynasty = string.IsNullOrWhiteSpace(suffix) ? "Unknown Dynasty" : "House " + suffix;
            age = 0;
        }

        private static SymbiozNetworkPawn FindPawnByOwnerId(int ownerId)
        {
            if (ownerId < 0)
                return null;

            SymbiozNetworkPawn[] pawns = FindObjectsByType<SymbiozNetworkPawn>(FindObjectsInactive.Exclude);
            for (int i = 0; i < pawns.Length; i++)
            {
                SymbiozNetworkPawn pawn = pawns[i];
                if (pawn != null && pawn.Owner != null && pawn.Owner.ClientId == ownerId)
                    return pawn;
            }

            return null;
        }

        private static void RefreshEveryIdentityLabel()
        {
            SymbiozNetworkPawn[] pawns = FindObjectsByType<SymbiozNetworkPawn>(FindObjectsInactive.Exclude);
            for (int i = 0; i < pawns.Length; i++)
                pawns[i]?.RefreshIdentityLabel();
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }

        private static string SafeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string SanitizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                    builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private static MeshRenderer EnsurePart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            Transform part = parent.Find(name);
            if (part == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = name;
                cube.transform.SetParent(parent, false);
                Collider collider = cube.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
                part = cube.transform;
            }

            part.localPosition = localPosition;
            part.localScale = localScale;
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                if (shader == null)
                    return renderer;
                Material material = new Material(shader);
                material.color = color;
                renderer.sharedMaterial = material;
            }

            return renderer;
        }

        private static bool ShouldCreateVisuals()
        {
            return !Application.isBatchMode;
        }
    }
}
