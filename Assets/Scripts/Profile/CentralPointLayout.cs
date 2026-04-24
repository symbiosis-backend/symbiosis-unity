using System;
using UnityEngine;

namespace MahjongGame
{
    public static class CentralPointLayout
    {
        public const float LeftX = 20f;
        public const float TopY = -18f;
        public const float MenuWidth = 285f;
        public const float ProfileHeight = 340f;
        public const float MenuButtonHeight = 84f;
        public const float MenuGap = 12f;

        private const string LeftMenuRootName = "CentralPointLeftMenu";

        public static Canvas ResolveMainCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas fallback = null;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                    continue;

                if (string.Equals(canvas.name, "Canvas", StringComparison.Ordinal))
                    return canvas;

                if (fallback == null && !string.Equals(canvas.name, "DoorCanvas", StringComparison.Ordinal))
                    fallback = canvas;
            }

            return fallback != null ? fallback : (canvases.Length > 0 ? canvases[0] : null);
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

            rect.SetAsLastSibling();
            SetTopLeft(rect, Vector2.zero, Vector2.zero);
            return rect;
        }

        public static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
