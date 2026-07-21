using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class ArenaBackdropBuilder : MonoBehaviour
    {
        private static readonly Color Sand = new Color(0.72f, 0.6f, 0.42f, 1f);
        private static readonly Color StoneDark = new Color(0.44f, 0.32f, 0.22f, 1f);
        private static readonly Color StoneLight = new Color(0.66f, 0.53f, 0.38f, 1f);
        private static readonly Color Gold = new Color(1f, 0.73f, 0.16f, 1f);
        private static readonly Color Red = new Color(0.86f, 0.09f, 0.06f, 1f);
        private static readonly Color Grass = new Color(0.42f, 0.62f, 0.2f, 1f);

        public void Build(Transform parent)
        {
            Transform root = parent.Find("ArenaVisuals");
            if (root == null)
            {
                root = new GameObject("ArenaVisuals").transform;
                root.SetParent(parent, false);
            }
            else
            {
                Clear(root);
            }

            CreateBox(root, "ArenaFloor", new Vector3(0f, -0.09f, 0f), new Vector3(35f, 0.08f, 35f), Sand);
            CreateBox(root, "BackStoneWall", new Vector3(0f, 2.2f, 17.6f), new Vector3(36f, 4.6f, 0.65f), StoneDark);
            CreateBox(root, "UpperTerrace", new Vector3(0f, 4.65f, 18.05f), new Vector3(36f, 0.75f, 2.5f), StoneLight);
            CreateBox(root, "FrontRim", new Vector3(0f, 0.12f, -17.2f), new Vector3(36f, 0.35f, 0.45f), StoneDark);

            BuildArches(root);
            BuildBanners(root);
            BuildFences(root);
            BuildRuins(root);
            BuildBushes(root);
        }

        private static void BuildArches(Transform root)
        {
            for (int i = 0; i < 8; i++)
            {
                float x = -15.5f + i * 4.4f;
                CreateBox(root, $"ArchLeft_{i}", new Vector3(x - 0.55f, 1.15f, 17.15f), new Vector3(0.28f, 2.05f, 0.45f), StoneLight);
                CreateBox(root, $"ArchRight_{i}", new Vector3(x + 0.55f, 1.15f, 17.15f), new Vector3(0.28f, 2.05f, 0.45f), StoneLight);
                CreateBox(root, $"ArchTop_{i}", new Vector3(x, 2.18f, 17.15f), new Vector3(1.35f, 0.28f, 0.45f), StoneLight);
                CreateBox(root, $"ArchVoid_{i}", new Vector3(x, 0.75f, 16.9f), new Vector3(0.78f, 1.45f, 0.12f), new Color(0.12f, 0.08f, 0.055f, 1f));
            }
        }

        private static void BuildBanners(Transform root)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = -10f + i * 5f;
                Color color = i % 2 == 0 ? Gold : Red;
                CreateBox(root, $"Banner_{i}", new Vector3(x, 3.5f, 16.75f), new Vector3(0.95f, 1.85f, 0.08f), color);
                CreateBox(root, $"BannerTip_{i}", new Vector3(x, 2.45f, 16.74f), new Vector3(0.45f, 0.28f, 0.08f), color);
            }
        }

        private static void BuildFences(Transform root)
        {
            CreateFence(root, "FenceBackLeft", new Vector3(-15.1f, 0.32f, 11.8f), 6);
            CreateFence(root, "FenceBackRight", new Vector3(9.8f, 0.32f, 12.1f), 6);
            CreateFence(root, "FenceFrontRight", new Vector3(9.6f, 0.32f, -14.7f), 6);
        }

        private static void CreateFence(Transform root, string name, Vector3 start, int posts)
        {
            CreateBox(root, $"{name}_RailA", start + new Vector3(posts * 0.45f, 0.28f, 0f), new Vector3(posts * 0.95f, 0.08f, 0.08f), new Color(0.36f, 0.2f, 0.08f, 1f));
            CreateBox(root, $"{name}_RailB", start + new Vector3(posts * 0.45f, 0.02f, 0f), new Vector3(posts * 0.95f, 0.08f, 0.08f), new Color(0.36f, 0.2f, 0.08f, 1f));
            for (int i = 0; i < posts; i++)
            {
                CreateBox(root, $"{name}_Post_{i}", start + new Vector3(i * 0.9f, 0.16f, 0f), new Vector3(0.09f, 0.62f, 0.09f), new Color(0.3f, 0.16f, 0.06f, 1f));
            }
        }

        private static void BuildRuins(Transform root)
        {
            Vector3[] positions =
            {
                new Vector3(-15.1f, 0.02f, 8.8f),
                new Vector3(-14.6f, 0.02f, 0.8f),
                new Vector3(15.1f, 0.02f, 5.5f),
                new Vector3(14.9f, 0.02f, -4.7f),
                new Vector3(5.8f, 0.02f, -15.1f),
                new Vector3(-10.5f, 0.02f, -15.2f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateBox(root, $"RuinSlab_{i}_A", positions[i], new Vector3(0.85f, 0.1f, 0.24f), StoneLight);
                CreateBox(root, $"RuinSlab_{i}_B", positions[i] + new Vector3(0.28f, 0.08f, 0.32f), new Vector3(0.62f, 0.1f, 0.22f), StoneLight);
                CreateBox(root, $"RuinPebble_{i}", positions[i] + new Vector3(-0.4f, 0.04f, -0.28f), new Vector3(0.2f, 0.08f, 0.16f), StoneDark);
            }
        }

        private static void BuildBushes(Transform root)
        {
            Vector3[] positions =
            {
                new Vector3(-15.3f, 0.03f, -7.5f),
                new Vector3(-15.2f, 0.03f, 4.7f),
                new Vector3(-3.5f, 0.03f, 13.8f),
                new Vector3(7.2f, 0.03f, 14.1f),
                new Vector3(15.2f, 0.03f, 8.2f),
                new Vector3(15.1f, 0.03f, -8.4f),
                new Vector3(-4.5f, 0.03f, -15.3f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateBox(root, $"Bush_{i}_A", positions[i], new Vector3(0.82f, 0.16f, 0.46f), Grass);
                CreateBox(root, $"Bush_{i}_B", positions[i] + new Vector3(0.26f, 0.08f, 0.2f), new Vector3(0.52f, 0.14f, 0.34f), new Color(0.5f, 0.7f, 0.24f, 1f));
            }
        }

        private static void Clear(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            if (obj.TryGetComponent(out Collider collider))
            {
                collider.enabled = false;
            }

            Renderer renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"))
            {
                color = color
            };
            return obj;
        }
    }
}
