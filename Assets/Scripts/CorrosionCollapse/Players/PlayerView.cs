using System.Collections;
using Dynasty.Legacy.CorrosionCollapse.Board;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Players
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private Renderer targetRenderer;

        public PlayerState State { get; private set; }

        public void Bind(PlayerState state, Color color)
        {
            State = state;
            name = $"Player_{state.playerId}_{state.nickname}";
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer != null)
            {
                targetRenderer.material.color = color;
            }

            SnapToNode(state.currentNode);
        }

        public void SnapToNode(BoardNode node)
        {
            if (node != null)
            {
                transform.position = node.position + GetOffset(State?.playerId ?? 0);
            }
        }

        public IEnumerator MoveToNode(BoardNode node)
        {
            if (node == null)
            {
                yield break;
            }

            Vector3 target = node.position + GetOffset(State?.playerId ?? 0);
            while ((transform.position - target).sqrMagnitude > 0.0025f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
        }

        private static Vector3 GetOffset(int playerId)
        {
            return playerId switch
            {
                1 => new Vector3(0.18f, 0.45f, 0.18f),
                2 => new Vector3(-0.18f, 0.45f, 0.18f),
                3 => new Vector3(0.18f, 0.45f, -0.18f),
                _ => new Vector3(-0.18f, 0.45f, -0.18f)
            };
        }
    }
}
