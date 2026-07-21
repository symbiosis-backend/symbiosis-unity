using System.Collections.Generic;
using System.Linq;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.UI
{
    public sealed class ScorePanelUI : MonoBehaviour
    {
        [SerializeField] private Transform playerList;

        private readonly Dictionary<int, PlayerItemView> itemViews = new Dictionary<int, PlayerItemView>();

        public void Initialize(Transform listRoot)
        {
            playerList = listRoot;
        }

        public void UpdateScore(IReadOnlyList<PlayerState> players, BoardGraph graph)
        {
            if (players == null || playerList == null)
            {
                return;
            }

            var sorted = players
                .OrderByDescending(player => graph?.GetProgress(player.currentNode) ?? 0)
                .ThenByDescending(player => player.score)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                PlayerState player = sorted[i];
                PlayerItemView view = GetItem(player.playerId);
                view.transform.SetSiblingIndex(i);
                view.Set(player);
            }
        }

        private PlayerItemView GetItem(int playerId)
        {
            if (itemViews.TryGetValue(playerId, out PlayerItemView view))
            {
                return view;
            }

            GameObject item = new GameObject($"PlayerItem_{playerId}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            item.transform.SetParent(playerList, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 42f);
            Image bg = item.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.075f, 0.11f, 0.78f);
            HorizontalLayoutGroup layout = item.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            view = item.AddComponent<PlayerItemView>();
            view.Initialize(playerId);
            itemViews[playerId] = view;
            return view;
        }

        private sealed class PlayerItemView : MonoBehaviour
        {
            private Image colorIcon;
            private TextMeshProUGUI nameText;
            private TextMeshProUGUI scoreText;
            private TextMeshProUGUI statusText;

            public void Initialize(int playerId)
            {
                colorIcon = CreateIcon("Icon", transform, HUDController.PlayerColor(playerId));
                nameText = CreateText("Name", transform, 17, TextAlignmentOptions.Left, new Vector2(142f, 28f));
                scoreText = CreateText("Score", transform, 16, TextAlignmentOptions.Right, new Vector2(72f, 28f));
                statusText = CreateText("Status", transform, 13, TextAlignmentOptions.Center, new Vector2(76f, 28f));
            }

            public void Set(PlayerState player)
            {
                nameText.text = player.nickname;
                scoreText.text = player.score.ToString();
                statusText.text = player.finished ? "FINISHED" : player.eliminated ? "ELIMINATED" : player.hasShortcutPass ? "PASS" : player.extraRollAvailable ? "EXTRA" : player.skipNextTurn ? "SKIP" : "ALIVE";
                statusText.color = player.finished
                    ? new Color(0.95f, 0.76f, 0.22f, 1f)
                    : player.eliminated
                        ? new Color(0.9f, 0.16f, 0.18f, 1f)
                        : new Color(0.32f, 0.9f, 0.48f, 1f);
            }

            private static Image CreateIcon(string name, Transform parent, Color color)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(parent, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(18f, 18f);
                Image image = obj.GetComponent<Image>();
                image.color = color;
                return image;
            }

            private static TextMeshProUGUI CreateText(string name, Transform parent, int size, TextAlignmentOptions alignment, Vector2 sizeDelta)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                obj.transform.SetParent(parent, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = sizeDelta;
                TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
                text.fontSize = size;
                text.alignment = alignment;
                text.color = new Color(0.92f, 0.9f, 0.82f, 1f);
                text.textWrappingMode = TextWrappingModes.NoWrap;
                return text;
            }
        }
    }
}
