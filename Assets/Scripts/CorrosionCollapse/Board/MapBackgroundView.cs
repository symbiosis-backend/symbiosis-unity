using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class MapBackgroundView : MonoBehaviour
    {
        private const string ResourcePath = "CorrosionCollapse/BGCC";
        private const string MapFileName = "BGCC.png";

        public void Build(Transform parent)
        {
            Transform oldArena = parent.Find("ArenaVisuals");
            if (oldArena != null)
            {
                oldArena.gameObject.SetActive(false);
            }

            Transform existing = parent.Find("CCMapBackground");
            GameObject mapObject = existing != null ? existing.gameObject : new GameObject("CCMapBackground");
            mapObject.transform.SetParent(parent, false);
            mapObject.transform.localPosition = new Vector3(0f, -0.16f, 4.2f);
            mapObject.transform.localRotation = Quaternion.identity;
            mapObject.transform.localScale = new Vector3(34f, 1f, 22.65f);

            MeshFilter filter = mapObject.GetComponent<MeshFilter>() ?? mapObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = mapObject.GetComponent<MeshRenderer>() ?? mapObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = CreateQuadMesh();

            Texture2D texture = Resources.Load<Texture2D>(ResourcePath) ?? LoadTextureFromFile();
            if (texture == null)
            {
                Debug.LogWarning($"[Map] BGCC texture not found at Resources/{ResourcePath}");
                return;
            }

            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"))
            {
                mainTexture = texture,
                color = Color.white
            };
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            renderer.sharedMaterial = material;
        }

        public void BuildUI(Canvas canvas)
        {
            Texture2D texture = Resources.Load<Texture2D>(ResourcePath) ?? LoadTextureFromFile();
            if (texture == null)
            {
                Debug.LogWarning($"[Map] BGCC texture not found at Resources/{ResourcePath}");
                return;
            }

            Canvas backgroundCanvas = EnsureBackgroundCanvas(canvas);
            RectTransform existing = backgroundCanvas.transform.Find("CCMapUIBackground") as RectTransform;
            GameObject background = existing != null ? existing.gameObject : new GameObject("CCMapUIBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.SetParent(backgroundCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage image = background.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;

            AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = texture.width / (float)texture.height;
        }

        private static Canvas EnsureBackgroundCanvas(Canvas referenceCanvas)
        {
            GameObject existing = GameObject.Find("CCMapCanvas");
            GameObject obj = existing != null ? existing.gameObject : new GameObject("CCMapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.transform.SetParent(null, false);
            obj.transform.localRotation = Quaternion.identity;
            obj.SetActive(true);

            Canvas canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            return canvas;
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

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh
            {
                name = "CCMapBackgroundQuad"
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
