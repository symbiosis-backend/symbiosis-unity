using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class BoardNodeView : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private BoardNode node;

        public BoardNode Node => node;

        public void Bind(BoardNode boardNode)
        {
            DiamondTileMesh.Ensure(gameObject);
            node = boardNode;
            transform.position = boardNode.position;
            transform.rotation = Quaternion.identity;
            name = $"Node_{boardNode.id:00}_{boardNode.type}";
            Refresh();
        }

        public void Refresh()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer == null || node == null)
            {
                return;
            }

            targetRenderer.material.color = GetColor(node);
            Vector3 activeScale = node.isShortcut
                ? new Vector3(0.48f, 1f, 0.48f)
                : new Vector3(0.6f, 1f, 0.6f);
            transform.localScale = node.state == NodeState.Destroyed
                ? new Vector3(activeScale.x * 0.86f, 1f, activeScale.z * 0.86f)
                : activeScale;
        }

        private static Color GetColor(BoardNode boardNode)
        {
            if (boardNode.state == NodeState.Destroyed)
            {
                return new Color(0.03f, 0.02f, 0.05f, 1f);
            }

            if (boardNode.state == NodeState.Corrupted)
            {
                return new Color(0.28f, 0.02f, 0.42f, 1f);
            }

            Color color = boardNode.type switch
            {
                TileType.Purple => new Color(0.62f, 0.17f, 0.95f, 1f),
                TileType.Yellow => new Color(1f, 0.78f, 0.08f, 1f),
                TileType.Green => new Color(0.18f, 0.84f, 0.28f, 1f),
                TileType.Red => new Color(0.95f, 0.12f, 0.08f, 1f),
                TileType.BlackRed => new Color(0.06f, 0.025f, 0.03f, 1f),
                TileType.Safe => new Color(0.96f, 0.9f, 0.58f, 1f),
                _ => new Color(0.66f, 0.52f, 0.32f, 1f)
            };

            return boardNode.isShortcut ? Color.Lerp(color, new Color(0.12f, 0.08f, 0.1f, 1f), 0.22f) : color;
        }
    }
}
