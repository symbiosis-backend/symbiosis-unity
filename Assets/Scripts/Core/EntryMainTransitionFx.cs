using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongGame
{
    public sealed class EntryMainTransitionFx : MonoBehaviour
    {
        private const string DefaultDoorSpriteResourcePath = "Mahjong/Sprites/Doors/AirlockDoorLeaf_Cohesive";
        private const int CanvasSortingOrder = 32767;
        private const float CloseSeconds = 0.42f;
        private const float OpenSeconds = 0.78f;
        private const float HoldSeconds = 0.08f;
        private const float HiddenPadding = 120f;
        private const float FadeAlpha = 0.16f;
        private const int PostLoadWaitFrames = 8;

        private Canvas canvas;
        private RectTransform leftDoor;
        private RectTransform rightDoor;
        private Image fadeImage;
        private bool running;
        private static EntryMainTransitionFx activeInstance;

        public bool IsRunning => running;
        public static bool IsTransitionActive => activeInstance != null && activeInstance.running;

        public static bool TryPlay(string targetSceneName, string doorSpriteResourcePath = null)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
                return false;

            GameObject host = new GameObject("EntryMainTransitionFx", typeof(RectTransform));
            EntryMainTransitionFx fx = host.AddComponent<EntryMainTransitionFx>();
            activeInstance = fx;
            fx.Build(doorSpriteResourcePath);
            DontDestroyOnLoad(host);
            fx.StartCoroutine(fx.Run(targetSceneName));
            return true;
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
                activeInstance = null;
        }

        private void Build(string doorSpriteResourcePath)
        {
            GameObject canvasObject = new GameObject("EntryMainTransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            fadeImage = CreateImage(canvasObject.transform, "Fade", null, true);
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = true;
            Stretch(fadeImage.rectTransform);

            Sprite doorSprite = LoadDoorSprite(doorSpriteResourcePath);
            leftDoor = CreateImage(canvasObject.transform, "LeftDoor", doorSprite, false).rectTransform;
            rightDoor = CreateImage(canvasObject.transform, "RightDoor", doorSprite, false).rectTransform;

            ConfigureDoorRects();
            SetDoorsOpened();
        }

        private IEnumerator Run(string targetSceneName)
        {
            if (running)
                yield break;

            running = true;
            yield return Animate(0f, 1f, CloseSeconds);

            if (HoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(HoldSeconds);

            DestroyStaleRuntimeDoorFx();

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
            if (operation != null)
            {
                while (!operation.isDone)
                    yield return null;
            }
            else
            {
                SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
            }

            for (int i = 0; i < PostLoadWaitFrames; i++)
            {
                Canvas.ForceUpdateCanvases();
                yield return null;
            }

            yield return new WaitForEndOfFrame();
            StabilizeMainSceneAfterEntry(targetSceneName);
            MainSceneResponsiveLayout.CancelMainReturnSanitizers();
            yield return Animate(1f, 0f, OpenSeconds);

            SetTransitionOverlayActive(false);
            if (string.Equals(targetSceneName, "Main", StringComparison.Ordinal))
                MainSceneResponsiveLayout.ForceRefreshCurrentScene();

            Destroy(gameObject);
        }

        private IEnumerator Animate(float from, float to, float seconds)
        {
            float time = 0f;
            seconds = Mathf.Max(0.001f, seconds);
            Vector2 openPositions = ResolveOpenPositions();

            SetClosedFraction(from, openPositions.x, openPositions.y);
            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / seconds);
                float eased = SmootherStep(t);
                SetClosedFraction(Mathf.Lerp(from, to, eased), openPositions.x, openPositions.y);
                yield return null;
            }

            SetClosedFraction(to, openPositions.x, openPositions.y);
        }

        private static float SmootherStep(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private void ConfigureDoorRects()
        {
            if (leftDoor == null || rightDoor == null)
                return;

            leftDoor.anchorMin = new Vector2(0f, 0f);
            leftDoor.anchorMax = new Vector2(0.5f, 1f);
            leftDoor.pivot = new Vector2(0.5f, 0.5f);
            leftDoor.offsetMin = Vector2.zero;
            leftDoor.offsetMax = Vector2.zero;

            rightDoor.anchorMin = new Vector2(0.5f, 0f);
            rightDoor.anchorMax = new Vector2(1f, 1f);
            rightDoor.pivot = new Vector2(0.5f, 0.5f);
            rightDoor.offsetMin = Vector2.zero;
            rightDoor.offsetMax = Vector2.zero;

            Vector3 scale = rightDoor.localScale;
            scale.x = -Mathf.Abs(scale.x == 0f ? 1f : scale.x);
            rightDoor.localScale = scale;
        }

        private void SetDoorsOpened()
        {
            SetClosedFraction(0f);
            if (fadeImage != null)
                fadeImage.raycastTarget = false;
        }

        private void SetClosedFraction(float value)
        {
            Vector2 openPositions = ResolveOpenPositions();
            SetClosedFraction(value, openPositions.x, openPositions.y);
        }

        private void SetClosedFraction(float value, float openLeft, float openRight)
        {
            float t = Mathf.Clamp01(value);

            if (leftDoor != null)
                leftDoor.anchoredPosition = new Vector2(Mathf.Lerp(openLeft, 0f, t), 0f);
            if (rightDoor != null)
                rightDoor.anchoredPosition = new Vector2(Mathf.Lerp(openRight, 0f, t), 0f);

            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = Mathf.Lerp(0f, FadeAlpha, t);
                fadeImage.color = color;
                fadeImage.raycastTarget = t > 0.01f;
            }
        }

        private Vector2 ResolveOpenPositions()
        {
            Canvas.ForceUpdateCanvases();

            float leftWidth = leftDoor != null ? Mathf.Abs(leftDoor.rect.width) : 0f;
            float rightWidth = rightDoor != null ? Mathf.Abs(rightDoor.rect.width) : 0f;
            if (leftWidth > 1f && rightWidth > 1f)
                return new Vector2(-(leftWidth + HiddenPadding), rightWidth + HiddenPadding);

            Vector2 screenSize = ResolveScreenSize();
            float fallbackHalfWidth = Mathf.Max(1f, screenSize.x * 0.5f);
            return new Vector2(-(fallbackHalfWidth + HiddenPadding), fallbackHalfWidth + HiddenPadding);
        }

        private static Vector2 ResolveScreenSize()
        {
            float width = Mathf.Max(Screen.width, Screen.height, 1f);
            float height = Mathf.Max(Mathf.Min(Screen.width, Screen.height), 1f);
            return new Vector2(width, height);
        }

        private static Sprite LoadDoorSprite(string resourcePath)
        {
            string path = string.IsNullOrWhiteSpace(resourcePath) ? DefaultDoorSpriteResourcePath : resourcePath.Trim();
            Sprite sprite = LoadDoorSpriteExact(path);
            if (sprite != null)
                return sprite;

            sprite = LoadDoorSpriteExact(path + "_Landscape");
            if (sprite != null)
                return sprite;

            sprite = LoadDoorSpriteExact(path + "_Portrait");
            if (sprite != null)
                return sprite;

            if (!string.Equals(path, DefaultDoorSpriteResourcePath, StringComparison.Ordinal))
            {
                sprite = LoadDoorSpriteExact(DefaultDoorSpriteResourcePath + "_Landscape");
                if (sprite != null)
                    return sprite;

                sprite = LoadDoorSpriteExact(DefaultDoorSpriteResourcePath + "_Portrait");
                if (sprite != null)
                    return sprite;
            }

            return null;
        }

        private static Sprite LoadDoorSpriteExact(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
                return sprites[0];

            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null)
                return null;

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, bool solidImage)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.enabled = solidImage || sprite != null;
            image.raycastTarget = solidImage;
            image.color = solidImage || sprite != null ? Color.white : Color.clear;
            return image;
        }

        private static void DestroyStaleRuntimeDoorFx()
        {
            DoorFx[] doors = FindObjectsByType<DoorFx>(FindObjectsInactive.Include);
            for (int i = 0; i < doors.Length; i++)
            {
                DoorFx door = doors[i];
                if (door != null && door.gameObject != null && door.gameObject.name == "RuntimeDoorTransition")
                    Destroy(door.gameObject);
            }
        }

        private void SetTransitionOverlayActive(bool active)
        {
            if (canvas != null)
                canvas.enabled = active;

            if (canvas != null && canvas.TryGetComponent(out GraphicRaycaster raycaster))
                raycaster.enabled = active;

            if (!active && fadeImage != null)
                fadeImage.raycastTarget = false;

            if (canvas != null && canvas.gameObject.activeSelf != active)
                canvas.gameObject.SetActive(active);
        }

        private static void StabilizeMainSceneAfterEntry(string targetSceneName)
        {
            if (!string.Equals(targetSceneName, "Main", StringComparison.Ordinal))
                return;

            MainShopUI.ForceCloseAll();
            SettingsMenuUI.ForceCloseAllSettingsMenus();
            SettingsMenuUI.SetMainSettingsButtonSuppressed(false);
            SettingsMenuUI.ForceRefreshAllForCurrentScene();

            MailboxBootstrap.EnsureForCurrentScene();
            FriendsBootstrap.EnsureForCurrentScene();
            GlobalChatBootstrap.EnsureForCurrentScene();
            AllianceBootstrap.EnsureForCurrentScene();
            MainSceneResponsiveLayout.ForceRefreshCurrentScene();

            Canvas.ForceUpdateCanvases();
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
