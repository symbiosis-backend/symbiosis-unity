using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class AllianceRoundedGraphic : MaskableGraphic
    {
        [SerializeField] private float cornerRadius = 18f;
        [SerializeField] private int cornerSegments = 8;

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                cornerRadius = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set
            {
                cornerSegments = Mathf.Clamp(value, 2, 16);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
            int segments = Mathf.Max(2, cornerSegments);
            Vector2 center = rect.center;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center;
            vh.AddVert(vertex);

            AddCorner(vh, vertex, rect.xMax - radius, rect.yMax - radius, radius, 0f, 90f, segments);
            AddCorner(vh, vertex, rect.xMin + radius, rect.yMax - radius, radius, 90f, 180f, segments);
            AddCorner(vh, vertex, rect.xMin + radius, rect.yMin + radius, radius, 180f, 270f, segments);
            AddCorner(vh, vertex, rect.xMax - radius, rect.yMin + radius, radius, 270f, 360f, segments);

            int count = vh.currentVertCount;
            for (int i = 1; i < count; i++)
            {
                int next = i == count - 1 ? 1 : i + 1;
                vh.AddTriangle(0, i, next);
            }
        }

        private static void AddCorner(VertexHelper vh, UIVertex source, float centerX, float centerY, float radius, float from, float to, int segments)
        {
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(from, to, i / (float)segments) * Mathf.Deg2Rad;
                source.position = new Vector3(centerX + Mathf.Cos(angle) * radius, centerY + Mathf.Sin(angle) * radius, 0f);
                vh.AddVert(source);
            }
        }
    }
}
