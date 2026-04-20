using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class BattlePick : MonoBehaviour
    {
        [SerializeField] private BattleCharacterCircularCarousel carousel;
        [SerializeField] private GameObject closeWindow;
        [SerializeField] private BattleLobbyChar lobbyChar;

        private Button btn;

        private void Awake()
        {
            btn = GetComponent<Button>();

            if (carousel == null)
                carousel = FindAnyObjectByType<BattleCharacterCircularCarousel>();

            if (lobbyChar == null)
                lobbyChar = FindAnyObjectByType<BattleLobbyChar>();

            btn.onClick.AddListener(Pick);
        }

        private void OnDestroy()
        {
            if (btn != null)
                btn.onClick.RemoveListener(Pick);
        }

        public void Pick()
        {
            if (carousel == null || carousel.CenteredButton == null)
                return;

            bool picked = carousel.CenteredButton.SelectDirectly();
            if (!picked)
                return;

            if (lobbyChar != null)
                lobbyChar.ConfirmAndRefresh();

            if (closeWindow != null)
                closeWindow.SetActive(false);
        }
    }
}
