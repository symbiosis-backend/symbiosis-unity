using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public enum MainLobbySideButtonSlot
    {
        Chat = 0,
        Friends = 1,
        Mail = 2,
        Alliance = 3
    }

    public enum MainLobbyLeftMenuSlot
    {
        Profile = 0,
        Vault = 1,
        Bank = 2,
        Exchange = 3,
        Weekly = 4
    }

    public enum MainLobbyBottomButtonSlot
    {
        Shop = 0,
        RewardBonus = 1
    }

    public enum BattleLobbyBottomButtonSlot
    {
        Return = 0,
        Character = 1,
        Inventory = 2,
        Shop = 3,
        Exchange = 4
    }

    public enum BattleLobbyMatchButtonSlot
    {
        Ranked,
        Random,
        Duel,
        LocalWifi,
        Tournament
    }

    public static class MainLobbyUiCoordinator
    {
        public static readonly Vector2 OverlayReferenceResolution = new Vector2(2400f, 1080f);
        public static readonly Vector2 PortraitReferenceResolution = new Vector2(1080f, 2400f);
        public static readonly Vector2 TabletLandscapeContentResolution = new Vector2(1920f, 1080f);
        public static readonly Vector2 BattleLobbyRightStackButtonSize = new Vector2(360f, 92f);
        public const float OverlayMatchWidthOrHeight = 0.65f;
        public const float PortraitMatchWidthOrHeight = 0.5f;
        private const float TabletLandscapeAspectLimit = 16f / 9f;

        public const float LeftMenuX = 20f;
        public const float LeftMenuTopY = -16f;
        public const float LeftMenuWidth = 350f;
        public const float LeftProfileHeight = 510f;
        public const float LeftMenuButtonHeight = 100f;
        public const float LeftMenuGap = 14f;
        private const float LandscapeProfileMenuWidth = LeftMenuWidth;
        private const float LandscapeProfileFrameSize = 380f;
        private const float LandscapeProfileButtonWidth = LeftMenuWidth;
        private const float LandscapeProfileButtonHeight = LeftMenuButtonHeight;
        private const float LandscapeProfileButtonGap = 18f;
        private const float LandscapeSideStackTopY = LeftMenuTopY - LandscapeProfileFrameSize - LandscapeProfileButtonGap;

        public static readonly Vector2 ShopButtonPosition = new Vector2(36f, 42f);
        public static readonly Vector2 ShopButtonSize = new Vector2(LeftMenuWidth, LeftMenuButtonHeight);
        public static readonly Vector2 BattleLobbyDownBarPanelSize = new Vector2(2850f, 330f);
        public static readonly Vector2 BattleLobbyDownBarPanelPosition = new Vector2(0f, -165f);
        public static readonly Vector2 BattleLobbyTopMirrorBarPanelSize = new Vector2(2850f, 330f);
        public static readonly Vector2 BattleLobbyTopMirrorBarPanelPosition = new Vector2(0f, 0f);
        private const float BattleLobbyBottomButtonAdditionalLift = 12f;

        private static readonly Vector2 RightStackButtonSize = new Vector2(LeftMenuWidth, LeftMenuButtonHeight);
        private static readonly Vector2 RightStackPortraitButtonSize = new Vector2(310f, 88f);
        private const float LeftSocialStepY = LeftMenuButtonHeight + LeftMenuGap;
        private const float LeftSocialTopY = LandscapeSideStackTopY - LeftSocialStepY;
        private const float RightStackPortraitX = -36f;
        private const float RightStackPortraitFirstY = 52f;
        private const float RightStackPortraitStepY = 114f;
        private const float RightMenuX = -20f;
        private const float RightMenuTopY = LandscapeSideStackTopY;
        private const float RightMenuButtonWidth = LeftMenuWidth;
        private const float RightMenuButtonHeight = LeftMenuButtonHeight;
        private const float RightMenuGap = LeftMenuGap;
        private const int RightMenuShopSlotIndex = 4;
        private static readonly Button[] rightStackButtons = new Button[4];
        private static bool rightStackSuppressed;

        public static void ConfigureOverlayScaler(CanvasScaler scaler)
        {
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = OverlayReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = OverlayMatchWidthOrHeight;
        }

        public static void ConfigureResponsiveLobbyScaler(CanvasScaler scaler)
        {
            if (scaler == null)
                return;

            Vector2 screenSize = ResolveScreenSize();
            bool portrait = IsPortraitLayout(screenSize);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = portrait ? PortraitReferenceResolution : OverlayReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = portrait ? PortraitMatchWidthOrHeight : OverlayMatchWidthOrHeight;
        }

        public static Vector2 ResolveScreenSize()
        {
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            return new Vector2(width, height);
        }

        public static bool IsPortraitLayout(Vector2 screenSize)
        {
            return screenSize.y > screenSize.x * 1.05f;
        }

        public static bool UseTabletLandscapeComposition()
        {
            Vector2 screenSize = ResolveScreenSize();
            if (IsPortraitLayout(screenSize))
                return false;

            return screenSize.x / Mathf.Max(1f, screenSize.y) < TabletLandscapeAspectLimit - 0.01f;
        }

        public static void LayoutMainCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);

            if (TryResolveTabletLandscapeFrame(rect, out float scale, out Vector4 insets))
            {
                rect.anchoredPosition = ResolveContentCenterOffset(insets) + position * scale;
                rect.sizeDelta = size * scale;
            }
            else
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
        }

        public static void LayoutRightStackButton(Button button, MainLobbySideButtonSlot slot)
        {
            RectTransform rect = button != null ? button.transform as RectTransform : null;
            if (rect == null)
                return;

            int index = Mathf.Clamp((int)slot, 0, rightStackButtons.Length - 1);
            rightStackButtons[index] = button;
            button.gameObject.SetActive(!rightStackSuppressed);
            button.transform.SetAsLastSibling();
            bool portrait = IsPortraitLayout(ResolveScreenSize());
            if (portrait)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(RightStackPortraitX, RightStackPortraitFirstY + (int)slot * RightStackPortraitStepY);
                rect.sizeDelta = RightStackPortraitButtonSize;
            }
            else
            {
                int visualIndex = rightStackButtons.Length - 1 - index;
                LayoutTopLeft(rect, new Vector2(LeftMenuX, LeftSocialTopY - visualIndex * LeftSocialStepY), RightStackButtonSize);
            }

            if (button.image != null)
                button.image.preserveAspect = false;

            ConfigureMainMenuButtonLabel(button.transform as RectTransform);
        }

        public static void SetRightStackSuppressed(bool suppressed)
        {
            rightStackSuppressed = suppressed;
            for (int i = 0; i < rightStackButtons.Length; i++)
            {
                Button button = rightStackButtons[i];
                if (button != null)
                    button.gameObject.SetActive(!suppressed);
            }
        }

        public static Vector2 GetLeftMenuButtonPosition(MainLobbyLeftMenuSlot slot)
        {
            if (IsPortraitLayout(ResolveScreenSize()))
                return GetPortraitLeftMenuButtonPosition(slot);

            if (slot == MainLobbyLeftMenuSlot.Profile)
                return new Vector2(LeftMenuX, LeftMenuTopY);

            return GetRightMenuButtonPosition(slot);
        }

        private static Vector2 GetPortraitLeftMenuButtonPosition(MainLobbyLeftMenuSlot slot)
        {
            const float x = 26f;
            const float topY = -76f;
            const float buttonHeight = 78f;
            const float gap = 24f;

            if (slot == MainLobbyLeftMenuSlot.Profile)
                return new Vector2(x, topY);

            int menuIndex = (int)slot - 1;
            float y = 122f + (buttonHeight + gap) * menuIndex;
            return new Vector2(x, y);
        }

        public static void LayoutLeftMenuButton(RectTransform rect, MainLobbyLeftMenuSlot slot)
        {
            if (slot == MainLobbyLeftMenuSlot.Profile)
            {
                LayoutProfileOpenButton(rect);
                return;
            }

            bool portrait = IsPortraitLayout(ResolveScreenSize());
            if (portrait)
                LayoutBottomLeft(rect, GetPortraitLeftMenuButtonPosition(slot), new Vector2(250f, 78f));
            else
                LayoutTopRight(rect, GetRightMenuButtonPosition(slot), new Vector2(RightMenuButtonWidth, RightMenuButtonHeight));

            ConfigureMainMenuButtonLabel(rect);
        }

        public static void LayoutProfileAvatarFrame(RectTransform rect)
        {
            bool portrait = IsPortraitLayout(ResolveScreenSize());
            float frameSize = portrait ? 112f : LandscapeProfileFrameSize;
            float menuWidth = portrait ? 160f : LandscapeProfileMenuWidth;
            Vector2 menuPosition = portrait ? GetPortraitLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Profile) : new Vector2(LeftMenuX, LeftMenuTopY);
            float frameX = menuPosition.x + (menuWidth - frameSize) * 0.5f;
            LayoutTopLeft(rect, new Vector2(frameX, menuPosition.y), new Vector2(frameSize, frameSize));
        }

        public static void LayoutProfileAvatar(RectTransform rect)
        {
            bool portrait = IsPortraitLayout(ResolveScreenSize());
            float frameSize = portrait ? 112f : LandscapeProfileFrameSize;
            const float avatarFillRatio = 0.78f;
            float avatarSize = frameSize * avatarFillRatio;
            float avatarInset = (frameSize - avatarSize) * 0.5f;
            float menuWidth = portrait ? 160f : LandscapeProfileMenuWidth;
            Vector2 menuPosition = portrait ? GetPortraitLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Profile) : new Vector2(LeftMenuX, LeftMenuTopY);
            float frameX = menuPosition.x + (menuWidth - frameSize) * 0.5f;
            LayoutTopLeft(rect, new Vector2(frameX + avatarInset, menuPosition.y - avatarInset), new Vector2(avatarSize, avatarSize));
        }

        public static void LayoutProfileOpenButton(RectTransform rect)
        {
            bool portrait = IsPortraitLayout(ResolveScreenSize());
            float frameSize = portrait ? 112f : LandscapeProfileFrameSize;
            float buttonWidth = portrait ? 160f : LandscapeProfileButtonWidth;
            float buttonHeight = portrait ? 52f : LandscapeProfileButtonHeight;
            float buttonGap = portrait ? 8f : LandscapeProfileButtonGap;
            float menuWidth = portrait ? 160f : LandscapeProfileMenuWidth;
            Vector2 menuPosition = portrait ? GetPortraitLeftMenuButtonPosition(MainLobbyLeftMenuSlot.Profile) : new Vector2(LeftMenuX, LeftMenuTopY);
            float x = menuPosition.x + (menuWidth - buttonWidth) * 0.5f;
            float y = menuPosition.y - frameSize - buttonGap;
            LayoutTopLeft(rect, new Vector2(x, y), new Vector2(buttonWidth, buttonHeight));
        }

        public static void LayoutBottomButton(RectTransform rect, MainLobbyBottomButtonSlot slot)
        {
            if (slot == MainLobbyBottomButtonSlot.RewardBonus)
            {
                bool portrait = IsPortraitLayout(ResolveScreenSize());
                if (!portrait && TryResolveTabletLandscapeFrame(rect, out float scale, out Vector4 insets))
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = rect.anchorMin;
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(ResolveContentCenterOffset(insets).x, insets.y + 24f * scale);
                    rect.sizeDelta = new Vector2(430f, 86f) * scale;
                    ConfigureMainMenuButtonLabel(rect);
                    return;
                }

                float screenHeight = Mathf.Max(1f, Screen.height);
                float referenceHeight = portrait ? PortraitReferenceResolution.y : OverlayReferenceResolution.y;
                float safeBottom = Mathf.Max(0f, Screen.safeArea.yMin / screenHeight * referenceHeight);
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, safeBottom + 24f);
                rect.sizeDelta = portrait ? new Vector2(380f, 80f) : new Vector2(430f, 86f);
                ConfigureMainMenuButtonLabel(rect);
                return;
            }

            if (IsPortraitLayout(ResolveScreenSize()))
                LayoutBottomLeft(rect, new Vector2(26f, 26f), new Vector2(250f, 78f));
            else
                LayoutTopRight(rect, GetRightMenuButtonPosition(RightMenuShopSlotIndex), ShopButtonSize);

            ConfigureMainMenuButtonLabel(rect);
        }

        private static void ConfigureMainMenuButtonLabel(RectTransform buttonRect)
        {
            if (buttonRect == null)
                return;

            TMPro.TMP_Text label = buttonRect.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label == null)
                return;

            MainLobbyButtonStyle.ApplyFont(label);
            MainLobbyButtonStyle.ApplySilverTextEffect(label);
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSize = 38f;
            label.fontSizeMax = 38f;
            label.fontSizeMin = 22f;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            MainLobbyButtonStyle.ApplyButtonLabelLayout(label);
        }

        public static Vector2 GetRightMenuButtonPosition(MainLobbyLeftMenuSlot slot)
        {
            int menuIndex = Mathf.Max(0, (int)slot - 1);
            return GetRightMenuButtonPosition(menuIndex);
        }

        private static Vector2 GetRightMenuButtonPosition(int menuIndex)
        {
            float y = RightMenuTopY - (RightMenuButtonHeight + RightMenuGap) * menuIndex;
            return new Vector2(RightMenuX, y);
        }

        public static void LayoutTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            if (TryResolveTabletLandscapeFrame(rect, out float scale, out Vector4 insets))
            {
                rect.anchoredPosition = new Vector2(insets.x + position.x * scale, -insets.w + position.y * scale);
                rect.sizeDelta = size * scale;
            }
            else
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
        }

        public static void LayoutBottomLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            if (TryResolveTabletLandscapeFrame(rect, out float scale, out Vector4 insets))
            {
                rect.anchoredPosition = new Vector2(insets.x + position.x * scale, insets.y + position.y * scale);
                rect.sizeDelta = size * scale;
            }
            else
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
        }

        public static void LayoutTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            if (TryResolveTabletLandscapeFrame(rect, out float scale, out Vector4 insets))
            {
                rect.anchoredPosition = new Vector2(-insets.z + position.x * scale, -insets.w + position.y * scale);
                rect.sizeDelta = size * scale;
            }
            else
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
        }

        public static void LayoutBottomRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            if (TryResolveTabletLandscapeFrame(rect, out float scale, out Vector4 insets))
            {
                rect.anchoredPosition = new Vector2(-insets.z + position.x * scale, insets.y + position.y * scale);
                rect.sizeDelta = size * scale;
            }
            else
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
        }

        private static bool TryResolveTabletLandscapeFrame(RectTransform target, out float scale, out Vector4 insets)
        {
            scale = 1f;
            insets = Vector4.zero;
            if (target == null || !UseTabletLandscapeComposition())
                return false;

            Canvas canvas = target.GetComponentInParent<Canvas>();
            Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
            RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            if (canvasRect == null)
                return false;

            Vector2 canvasSize = canvasRect.rect.size;
            if (canvasSize.x <= 1f || canvasSize.y <= 1f || Screen.width <= 0 || Screen.height <= 0)
                return false;

            Rect safeArea = Screen.safeArea;
            float safeLeft = Mathf.Max(0f, safeArea.xMin) * canvasSize.x / Screen.width;
            float safeRight = Mathf.Max(0f, Screen.width - safeArea.xMax) * canvasSize.x / Screen.width;
            float safeBottom = Mathf.Max(0f, safeArea.yMin) * canvasSize.y / Screen.height;
            float safeTop = Mathf.Max(0f, Screen.height - safeArea.yMax) * canvasSize.y / Screen.height;
            float safeWidth = Mathf.Max(1f, canvasSize.x - safeLeft - safeRight);
            float safeHeight = Mathf.Max(1f, canvasSize.y - safeBottom - safeTop);

            scale = Mathf.Min(
                safeWidth / TabletLandscapeContentResolution.x,
                safeHeight / TabletLandscapeContentResolution.y);
            float contentWidth = TabletLandscapeContentResolution.x * scale;
            float contentHeight = TabletLandscapeContentResolution.y * scale;
            float horizontalGap = Mathf.Max(0f, safeWidth - contentWidth) * 0.5f;
            float verticalGap = Mathf.Max(0f, safeHeight - contentHeight) * 0.5f;
            insets = new Vector4(
                safeLeft + horizontalGap,
                safeBottom + verticalGap,
                safeRight + horizontalGap,
                safeTop + verticalGap);
            return true;
        }

        private static Vector2 ResolveContentCenterOffset(Vector4 insets)
        {
            return new Vector2((insets.x - insets.z) * 0.5f, (insets.y - insets.w) * 0.5f);
        }

        public static void LayoutBattleLobbyPanel(RectTransform rect, bool topMirror)
        {
            if (rect == null)
                return;

            rect.anchorMin = topMirror ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = topMirror ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
            rect.anchoredPosition = topMirror ? BattleLobbyTopMirrorBarPanelPosition : BattleLobbyDownBarPanelPosition;
            rect.sizeDelta = topMirror ? BattleLobbyTopMirrorBarPanelSize : BattleLobbyDownBarPanelSize;
            rect.localScale = topMirror ? new Vector3(1f, -1f, 1f) : Vector3.one;
            rect.SetAsFirstSibling();
        }

        public static void LayoutBattleLobbyBottomButton(Button button, BattleLobbyBottomButtonSlot slot, Vector2 canvasSize, Vector2 maxSize)
        {
            Vector2 size = ResolveBattleLobbyBottomButtonSize(canvasSize, maxSize);
            float[] slots = ResolveBattleLobbyBottomButtonSlots(canvasSize, size.x);
            int index = Mathf.Clamp((int)slot, 0, slots.Length - 1);
            float y = ResolveBattleLobbyBottomButtonY(canvasSize) + BattleLobbyBottomButtonAdditionalLift;
            LayoutCenteredButton(button, ClampBattleLobbyCenteredPosition(new Vector2(slots[index] - 36f, y), size, canvasSize), size);
        }

        public static void LayoutBattleLobbyTopTabButton(Button button, int slotIndex, int slotCount, Vector2 canvasSize)
        {
            RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
            if (rect == null)
                return;

            slotCount = Mathf.Max(1, slotCount);
            int index = Mathf.Clamp(slotIndex, 0, slotCount - 1);
            float width = Mathf.Max(1f, canvasSize.x);
            Vector2 size = ResolveBattleLobbyTopTabButtonSize(canvasSize);
            Rect safeRect = ResolveBattleLobbySafeRect(canvasSize);
            float edgePadding = Mathf.Clamp(width * 0.045f, 86f, 132f);
            float left = safeRect.xMin + size.x * 0.5f + edgePadding;
            float right = safeRect.xMax - size.x * 0.5f - edgePadding;
            float x = slotCount == 1 || right <= left
                ? safeRect.center.x
                : Mathf.Lerp(left, right, (float)index / (slotCount - 1));
            x -= 36f;
            if (slotCount == 4)
            {
                if (index == 1)
                    x += 6f;
                else if (index == 2)
                    x -= 6f;
            }
            float topOffset = ResolveBattleLobbyBottomButtonY(canvasSize) - safeRect.yMin + 8f;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, -topOffset);
            rect.sizeDelta = size;

            RectTransform labelRect = rect.Find("Label") as RectTransform;
            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
        }

        public static Vector2 ResolveBattleLobbyTopTabButtonSize(Vector2 canvasSize)
        {
            return new Vector2(390f, 100f);
        }

        public static void LayoutBattleLobbyMatchButton(Button button, BattleLobbyMatchButtonSlot slot, Vector2 canvasSize, Vector2 maxSize)
        {
            float availableWidth = Mathf.Max(1f, canvasSize.x);
            float availableHeight = Mathf.Max(1f, canvasSize.y);
            float aspect = availableWidth / availableHeight;
            bool tabletLike = aspect < 1.55f;
            float sidePadding = tabletLike ? 180f : 240f;
            float widthFactor = tabletLike ? 0.36f : 0.42f;
            float maxByHeight = availableHeight * (tabletLike ? 0.39f : 0.46f);
            float matchWidth = Mathf.Clamp((availableWidth - sidePadding) * widthFactor, tabletLike ? 360f : 430f, Mathf.Min(maxSize.x, maxByHeight));
            float matchHeight = Mathf.Clamp(matchWidth * (maxSize.y / Mathf.Max(1f, maxSize.x)), tabletLike ? 88f : 104f, maxSize.y);
            Vector2 matchSize = new Vector2(matchWidth, matchHeight);
            float sideX = Mathf.Min(tabletLike ? 310f : 360f, matchWidth * (tabletLike ? 0.68f : 0.64f));
            float rightColumnX = sideX * 0.86f;
            float rowY = Mathf.Clamp(canvasSize.y * (tabletLike ? 0.118f : 0.132f), tabletLike ? 118f : 142f, tabletLike ? 156f : 178f);
            float tournamentGap = Mathf.Clamp(matchHeight * 1.10f, 150f, 190f);

            Vector2 position;
            Vector2 size = matchSize;
            switch (slot)
            {
                case BattleLobbyMatchButtonSlot.Ranked:
                    position = new Vector2(-sideX, rowY);
                    break;
                case BattleLobbyMatchButtonSlot.Random:
                    position = new Vector2(rightColumnX, rowY);
                    break;
                case BattleLobbyMatchButtonSlot.Duel:
                    position = new Vector2(-sideX, -rowY);
                    break;
                case BattleLobbyMatchButtonSlot.LocalWifi:
                    position = new Vector2(rightColumnX, -rowY);
                    break;
                default:
                    position = new Vector2(0f, -rowY - tournamentGap);
                    size = new Vector2(matchSize.x * 0.84f, Mathf.Max(76f, matchSize.y * 0.72f));
                    break;
            }

            LayoutCenteredButton(button, ClampBattleLobbyCenteredPosition(position, size, canvasSize), size);
        }

        public static Vector2 ResolveBattleLobbyBottomButtonSize(Vector2 canvasSize, Vector2 maxSize)
        {
            float availableWidth = Mathf.Max(1f, canvasSize.x);
            float availableHeight = Mathf.Max(1f, canvasSize.y);
            float aspect = availableWidth / availableHeight;
            bool tabletLike = aspect < 1.55f;
            Rect safeRect = ResolveBattleLobbySafeRect(canvasSize);
            float safeWidth = Mathf.Max(1f, safeRect.width);
            float targetWidth = Mathf.Min(maxSize.x, (safeWidth - (tabletLike ? 300f : 520f)) / 5f);
            targetWidth = Mathf.Clamp(targetWidth, tabletLike ? 176f : 180f, maxSize.x);
            float targetHeight = Mathf.Clamp(targetWidth * (maxSize.y / Mathf.Max(1f, maxSize.x)), tabletLike ? 72f : 78f, maxSize.y);
            return new Vector2(targetWidth, targetHeight);
        }

        private static float[] ResolveBattleLobbyBottomButtonSlots(Vector2 canvasSize, float buttonWidth)
        {
            Rect safeRect = ResolveBattleLobbySafeRect(canvasSize);
            float edgePadding = Mathf.Clamp(canvasSize.x * 0.018f, 22f, 54f);
            float left = safeRect.xMin + buttonWidth * 0.5f + edgePadding;
            float right = safeRect.xMax - buttonWidth * 0.5f - edgePadding;

            if (right < left)
            {
                float center = safeRect.center.x;
                return new[] { center, center, center, center, center };
            }

            return new[]
            {
                left,
                Mathf.Lerp(left, right, 0.25f),
                Mathf.Lerp(left, right, 0.50f),
                Mathf.Lerp(left, right, 0.75f),
                right
            };
        }

        private static float ResolveBattleLobbyBottomButtonY(Vector2 canvasSize)
        {
            float aspect = Mathf.Max(1f, canvasSize.x) / Mathf.Max(1f, canvasSize.y);
            bool tabletLike = aspect < 1.55f;
            Rect safeRect = ResolveBattleLobbySafeRect(canvasSize);
            float lift = Mathf.Clamp(canvasSize.y * (tabletLike ? 0.066f : 0.070f), tabletLike ? 58f : 66f, tabletLike ? 90f : 100f);
            return safeRect.yMin + lift;
        }

        public static void LayoutCenteredButton(Button button, Vector2 position, Vector2 size)
        {
            RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void LayoutCenteredButtonSafe(Button button, Vector2 position, Vector2 size, Vector2 canvasSize)
        {
            LayoutCenteredButton(button, ClampBattleLobbyCenteredPosition(position, size, canvasSize), size);
        }

        public static void LayoutTopRightButtonSafe(RectTransform rect, Vector2 desiredTopRightOffset, Vector2 size, Vector2 canvasSize)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = size;

            Rect safeRect = ResolveBattleLobbySafeRect(canvasSize);
            float margin = Mathf.Clamp(Mathf.Min(canvasSize.x, canvasSize.y) * 0.018f, 12f, 36f);
            Vector2 parentTopRight = new Vector2(canvasSize.x * 0.5f, canvasSize.y * 0.5f);
            Vector2 desiredPivot = parentTopRight + desiredTopRightOffset;

            float minPivotX = safeRect.xMin + margin + size.x;
            float maxPivotX = safeRect.xMax - margin;
            float minPivotY = safeRect.yMin + margin + size.y;
            float maxPivotY = safeRect.yMax - margin;

            float pivotX = minPivotX <= maxPivotX ? Mathf.Clamp(desiredPivot.x, minPivotX, maxPivotX) : safeRect.center.x;
            float pivotY = minPivotY <= maxPivotY ? Mathf.Clamp(desiredPivot.y, minPivotY, maxPivotY) : safeRect.center.y;
            rect.anchoredPosition = new Vector2(pivotX, pivotY) - parentTopRight;
        }

        public static void LayoutBattleLobbyRightStackButton(RectTransform rect, int stackIndex, Vector2 canvasSize)
        {
            if (rect == null)
                return;

            Vector2 size = BattleLobbyRightStackButtonSize;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = size;

            Rect safeRect = ResolveBattleLobbySafeRect(canvasSize);
            float margin = Mathf.Clamp(Mathf.Min(canvasSize.x, canvasSize.y) * 0.018f, 12f, 36f);
            float topOffset = Mathf.Clamp(canvasSize.y * 0.135f, 112f, 152f);
            float gap = Mathf.Clamp(canvasSize.y * 0.012f, 10f, 18f);
            float step = size.y + gap;

            float pivotX = safeRect.xMax - margin;
            float pivotY = safeRect.yMax - topOffset - Mathf.Max(0, stackIndex) * step;
            float minPivotX = safeRect.xMin + margin + size.x;
            float maxPivotX = safeRect.xMax - margin;
            float minPivotY = safeRect.yMin + margin + size.y;
            float maxPivotY = safeRect.yMax - margin;

            pivotX = minPivotX <= maxPivotX ? Mathf.Clamp(pivotX, minPivotX, maxPivotX) : safeRect.center.x;
            pivotY = minPivotY <= maxPivotY ? Mathf.Clamp(pivotY, minPivotY, maxPivotY) : safeRect.center.y;

            Vector2 parentTopRight = new Vector2(canvasSize.x * 0.5f, canvasSize.y * 0.5f);
            rect.anchoredPosition = new Vector2(pivotX, pivotY) - parentTopRight;
        }

        private static Vector2 ClampBattleLobbyCenteredPosition(Vector2 position, Vector2 size, Vector2 canvasSize)
        {
            Rect safeRect = ResolveBattleLobbySafeRect(canvasSize);
            float margin = Mathf.Clamp(Mathf.Min(canvasSize.x, canvasSize.y) * 0.018f, 12f, 36f);
            float halfWidth = Mathf.Max(1f, size.x) * 0.5f;
            float halfHeight = Mathf.Max(1f, size.y) * 0.5f;

            float minX = safeRect.xMin + halfWidth + margin;
            float maxX = safeRect.xMax - halfWidth - margin;
            float minY = safeRect.yMin + halfHeight + margin;
            float maxY = safeRect.yMax - halfHeight - margin;

            float x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : safeRect.center.x;
            float y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : safeRect.center.y;
            return new Vector2(x, y);
        }

        private static Rect ResolveBattleLobbySafeRect(Vector2 canvasSize)
        {
            float width = Mathf.Max(1f, canvasSize.x);
            float height = Mathf.Max(1f, canvasSize.y);
            Rect rect = new Rect(-width * 0.5f, -height * 0.5f, width, height);

            Rect safe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0 || safe.width <= 0f || safe.height <= 0f)
                return rect;

            float left = Mathf.Max(0f, safe.xMin) * (width / Screen.width);
            float right = Mathf.Max(0f, Screen.width - safe.xMax) * (width / Screen.width);
            float bottom = Mathf.Max(0f, safe.yMin) * (height / Screen.height);
            float top = Mathf.Max(0f, Screen.height - safe.yMax) * (height / Screen.height);

            float xMin = -width * 0.5f + left;
            float xMax = width * 0.5f - right;
            float yMin = -height * 0.5f + bottom;
            float yMax = height * 0.5f - top;
            if (xMax <= xMin || yMax <= yMin)
                return rect;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
