using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongGame
{
    public static class CentralPointLayout
    {
        public const float LeftX = MainLobbyUiCoordinator.LeftMenuX;
        public const float TopY = MainLobbyUiCoordinator.LeftMenuTopY;
        public const float MenuWidth = MainLobbyUiCoordinator.LeftMenuWidth;
        public const float ProfileHeight = MainLobbyUiCoordinator.LeftProfileHeight;
        public const float MenuButtonHeight = MainLobbyUiCoordinator.LeftMenuButtonHeight;
        public const float MenuGap = MainLobbyUiCoordinator.LeftMenuGap;

        private const string LeftMenuRootName = "CentralPointLeftMenu";

        public static Canvas ResolveMainCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas fallback = null;
            Scene activeScene = SceneManager.GetActiveScene();

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                    continue;

                if (!canvas.gameObject.scene.IsValid() || canvas.gameObject.scene != activeScene)
                    continue;

                if (string.Equals(canvas.name, "Canvas", StringComparison.Ordinal))
                    return canvas;

                if (fallback == null && IsUsableMainCanvas(canvas))
                    fallback = canvas;
            }

            if (fallback != null)
                return fallback;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (IsUsableMainCanvas(canvas))
                    return canvas;
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static bool IsUsableMainCanvas(Canvas canvas)
        {
            if (canvas == null)
                return false;

            return !IsRuntimeOverlayCanvasName(canvas.name);
        }

        public static bool IsRuntimeOverlayCanvasName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.Contains("Door", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Transition", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Entry", StringComparison.OrdinalIgnoreCase)
                || name.Contains("BrainGames", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Launch", StringComparison.OrdinalIgnoreCase)
                || name.Contains("MahjongModeChoice", StringComparison.OrdinalIgnoreCase)
                || name.Contains("MoonEffect", StringComparison.OrdinalIgnoreCase)
                || name.Contains("InfoHint", StringComparison.OrdinalIgnoreCase)
                || name.Contains("SymbiozLogin", StringComparison.OrdinalIgnoreCase)
                || name.Contains("OrbiosisHangar", StringComparison.OrdinalIgnoreCase)
                || name.Contains("SymbiGridOrientationBlackout", StringComparison.OrdinalIgnoreCase)
                || name.Contains("SymbiGridSceneTransitionFx", StringComparison.OrdinalIgnoreCase);
        }

        public static RectTransform ResolveLeftMenuRoot(Canvas canvas = null)
        {
            if (canvas == null)
                canvas = ResolveMainCanvas();
            if (canvas == null)
                return null;

            Transform existing = canvas.transform.Find(LeftMenuRootName);
            RectTransform rect = existing != null ? existing as RectTransform : null;
            if (rect == null)
            {
                GameObject root = new GameObject(LeftMenuRootName, typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                rect = root.GetComponent<RectTransform>();
            }

            Stretch(rect);
            return rect;
        }

        public static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            MainLobbyUiCoordinator.LayoutTopLeft(rect, position, size);
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
