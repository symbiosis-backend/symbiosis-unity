using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.UI
{
    public static class CCMapRuntimeBackground
    {
        private const string CanvasName = "CCMapCanvas";
        private const string ImageName = "CCMapUIBackground";
        private const string ResourcePath = "CorrosionCollapse/BGCC";
        private const string MapFileName = "BGCC.png";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "CorrosionCollapse" && GameObject.Find("CorrosionCollapseRoot") == null)
            {
                DestroyExisting();
                return;
            }

            Texture2D texture = Resources.Load<Texture2D>(ResourcePath) ?? LoadTextureFromFile();
            if (texture == null)
            {
                Debug.LogWarning("[Map] BGCC texture could not be loaded.");
                return;
            }

            GameObject canvasObject = GameObject.Find(CanvasName) ?? new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(null, false);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.SetActive(true);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            canvasRect.localScale = Vector3.one;

            Transform existing = canvasObject.transform.Find(ImageName);
            GameObject imageObject = existing != null ? existing.gameObject : new GameObject(ImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            imageObject.transform.SetParent(canvasObject.transform, false);
            imageObject.transform.localRotation = Quaternion.identity;
            imageObject.SetActive(true);

            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            imageRect.localRotation = Quaternion.identity;
            imageRect.localScale = Vector3.one;

            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;

            AspectRatioFitter fitter = imageObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = texture.width / (float)texture.height;

            Debug.Log("[Map] BGCC runtime background created.");
        }

        private static void DestroyExisting()
        {
            GameObject existing = GameObject.Find(CanvasName);
            if (existing != null)
            {
                Object.Destroy(existing);
            }
        }

        private static Texture2D LoadTextureFromFile()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "CorrosionCollapse", MapFileName);
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return texture.LoadImage(bytes) ? texture : null;
        }
    }
}
