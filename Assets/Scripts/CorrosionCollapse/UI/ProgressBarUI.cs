using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Board;
using Dynasty.Legacy.CorrosionCollapse.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dynasty.Legacy.CorrosionCollapse.UI
{
    public sealed class ProgressBarUI : MonoBehaviour
    {
        [SerializeField] private RectTransform markerRoot;
        [SerializeField] private RectTransform track;
        [SerializeField] private TextMeshProUGUI finishLabel;

        private readonly Dictionary<int, RectTransform> markers = new Dictionary<int, RectTransform>();

        public void Initialize(RectTransform trackRect, RectTransform markersRect, TextMeshProUGUI label)
        {
            track = trackRect;
            markerRoot = markersRect;
            finishLabel = label;
        }

        public void UpdateProgress(IReadOnlyList<PlayerState> players, BoardGraph graph)
        {
            if (players == null || graph?.finishNode == null || markerRoot == null)
            {
                return;
            }

            if (finishLabel != null)
            {
                finishLabel.text = "FINISH";
            }

            int finishProgress = Mathf.Max(1, graph.GetProgress(graph.finishNode));
            for (int i = 0; i < players.Count; i++)
            {
                PlayerState player = players[i];
                RectTransform marker = GetMarker(player.playerId, HUDController.PlayerColor(player.playerId));
                float progress = Mathf.Clamp01(graph.GetProgress(player.currentNode) / (float)finishProgress);
                marker.anchorMin = new Vector2(progress, 0.5f);
                marker.anchorMax = new Vector2(progress, 0.5f);
                marker.anchoredPosition = new Vector2(0f, i % 2 == 0 ? 8f : -8f);
            }
        }

        private RectTransform GetMarker(int playerId, Color color)
        {
            if (markers.TryGetValue(playerId, out RectTransform marker))
            {
                return marker;
            }

            GameObject obj = new GameObject($"PlayerMarker_{playerId}", typeof(RectTransform), typeof(Image), typeof(Outline));
            obj.transform.SetParent(markerRoot, false);
            marker = obj.GetComponent<RectTransform>();
            marker.sizeDelta = new Vector2(18f, 18f);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            Outline outline = obj.GetComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.015f, 0.035f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            markers[playerId] = marker;
            return marker;
        }
    }
}
