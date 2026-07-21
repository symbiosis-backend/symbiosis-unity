using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    public sealed class MainInfoLayerElement : MonoBehaviour
    {
        private CanvasGroup group;

        public void SetVisible(bool visible)
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();

            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }

            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }
    }
}
