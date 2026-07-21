using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Players;
using UnityEngine;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.UI
{
    public sealed class CorrosionCollapseHud : MonoBehaviour
    {
        [SerializeField] private HUDController controller;

        public Button DiceButton => controller.DiceButton;

        public void Initialize(HUDController hudController)
        {
            controller = hudController;
        }

        public void Refresh(IReadOnlyList<PlayerState> players, PlayerState current, BoardGraph graph, bool canRoll)
        {
            controller.SetDiceInteractable(canRoll);
            controller.UpdateProgress(players, graph);
            controller.UpdateScore(players, graph);
            controller.SetTurnTimer(canRoll ? 1f : 0.35f);
            controller.ShowStatus(current == null ? "Collapse Sequence" : $"Ход: {current.nickname}");
        }

        public void SetDiceText(int dice1, int dice2)
        {
            controller.ShowDiceResult(dice1, dice2);
        }

        public void ShowEvent(string message)
        {
            controller.ShowEvent(message);
        }

        public void ShowStatus(string message)
        {
            controller.ShowStatus(message);
        }

        public void ShowResult(string text, IReadOnlyList<PlayerState> players = null)
        {
            controller.ShowResult(text, players);
        }

        public static CorrosionCollapseHud Create(Canvas canvas)
        {
            HUDController controller = HUDController.Create(canvas);
            CorrosionCollapseHud facade = controller.GetComponent<CorrosionCollapseHud>();
            if (facade == null)
            {
                facade = controller.gameObject.AddComponent<CorrosionCollapseHud>();
            }

            facade.Initialize(controller);
            return facade;
        }
    }
}
