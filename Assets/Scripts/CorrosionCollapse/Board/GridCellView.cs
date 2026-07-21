using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class GridCellView : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        public void Bind(Vector3 position, int x, int y, float cellSize)
        {
            DiamondTileMesh.Ensure(gameObject);
            transform.position = position;
            transform.rotation = Quaternion.identity;
            transform.localScale = new Vector3(cellSize * 0.985f, 1f, cellSize * 0.985f);
            name = $"GridCell_{x:00}_{y:00}";

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer == null)
            {
                return;
            }

            float noise = ((x * 19 + y * 31) % 7) / 100f;
            float edgeShade = (x + y) % 2 == 0 ? 0.01f : 0f;
            targetRenderer.material.color = new Color(0.66f + noise - edgeShade, 0.56f + noise - edgeShade, 0.39f + noise - edgeShade, 1f);
        }
    }
}
