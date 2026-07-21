using System.Collections.Generic;
using Dynasty.Legacy.CorrosionCollapse.Pooling;
using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class BoardBuilder : MonoBehaviour
    {
        private const int GridWidth = 50;
        private const int GridHeight = 50;
        private const int GridMin = -25;
        private const int GridMax = 24;
        private const float CellSize = 0.72f;

        [SerializeField] private Transform boardRoot;
        [SerializeField] private ObjectPoolManager poolManager;
        [SerializeField] private bool showBackgroundGrid;

        private readonly Dictionary<int, BoardNodeView> nodeViews = new Dictionary<int, BoardNodeView>();
        private readonly Dictionary<Vector2Int, BoardNode> mainNodesByGrid = new Dictionary<Vector2Int, BoardNode>();

        public BoardGraph Graph { get; private set; }

        private sealed class MainRouteData
        {
            public readonly List<Vector2Int> cells = new List<Vector2Int>(280);
            public readonly Dictionary<string, int> markers = new Dictionary<string, int>();

            public Vector2Int Cell(string marker)
            {
                return cells[markers[marker]];
            }
        }

        public void Initialize(Transform root, ObjectPoolManager pool)
        {
            boardRoot = root;
            poolManager = pool;
            Graph = BuildGraph();
            BuildGridViews();
            BuildRouteViews();
        }

        public void BuildManualRoute(IReadOnlyList<Vector2Int> cells)
        {
            BuildManualRoute(cells, null);
        }

        public void BuildManualRoute(IReadOnlyList<Vector2Int> cells, IReadOnlyList<TileType> effects)
        {
            if (cells == null || cells.Count < 2)
            {
                Debug.LogWarning("[Builder] Manual route requires at least two cells.");
                return;
            }

            ClearRoute();
            mainNodesByGrid.Clear();
            var graph = new BoardGraph();
            List<BoardNode> mainPath = CreateMainPath(graph, cells);
            if (effects != null)
            {
                for (int i = 0; i < mainPath.Count && i < effects.Count; i++)
                {
                    mainPath[i].type = effects[i];
                    mainPath[i].isSafeZone = effects[i] == TileType.Safe;
                }
            }

            SetSafe(mainPath, 0);
            SetSafe(mainPath, mainPath.Count - 1);
            graph.SetStart(mainPath[0]);
            graph.SetFinish(mainPath[^1]);
            Graph = graph;
            BuildRouteViews();
        }

        public void ClearRoute()
        {
            nodeViews.Clear();
            Transform routeRoot = EnsureChild(boardRoot, "Route");
            for (int i = routeRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = routeRoot.GetChild(i).gameObject;
                poolManager.Release(child);
            }
        }

        public Vector3 GridToWorldPosition(Vector2Int cell, float y)
        {
            return GridToWorld(cell, y);
        }

        public bool TryWorldToGrid(Vector3 world, out Vector2Int cell)
        {
            int x = Mathf.RoundToInt(world.x / CellSize);
            int y = Mathf.RoundToInt(world.z / CellSize);
            cell = new Vector2Int(x, y);
            return IsInsideGrid(cell);
        }

        public BoardNodeView GetView(int nodeId)
        {
            return nodeViews.TryGetValue(nodeId, out BoardNodeView view) ? view : null;
        }

        public void RefreshNode(BoardNode node)
        {
            if (node != null && nodeViews.TryGetValue(node.id, out BoardNodeView view))
            {
                view.Refresh();
            }
        }

        private BoardGraph BuildGraph()
        {
            mainNodesByGrid.Clear();
            var graph = new BoardGraph();
            MainRouteData route = BuildMainRouteData();
            List<BoardNode> mainPath = CreateMainPath(graph, route.cells);
            ApplyRouteBalance(mainPath, route);

            AddShortcut(graph, route.Cell("ShortcutAEntry"), route.Cell("ShortcutAExit"), new[]
            {
                new Vector2Int(10, 13),
                new Vector2Int(10, 14)
            });

            AddShortcut(graph, route.Cell("ShortcutBEntry"), route.Cell("ShortcutBExit"), new[]
            {
                new Vector2Int(11, 21),
                new Vector2Int(11, 22),
                new Vector2Int(11, 23)
            });

            AddShortcut(graph, route.Cell("ShortcutCEntry"), route.Cell("ShortcutCExit"), new[]
            {
                new Vector2Int(39, 28),
                new Vector2Int(39, 29),
                new Vector2Int(39, 30),
                new Vector2Int(39, 31)
            });

            AddShortcut(graph, route.Cell("ShortcutDEntry"), route.Cell("ShortcutDExit"), new[]
            {
                new Vector2Int(23, 23),
                new Vector2Int(24, 23),
                new Vector2Int(25, 23),
                new Vector2Int(26, 23),
                new Vector2Int(27, 23),
                new Vector2Int(28, 23)
            });

            graph.SetStart(mainPath[0]);
            graph.SetFinish(mainPath[^1]);
            return graph;
        }

        private static MainRouteData BuildMainRouteData()
        {
            var route = new MainRouteData();
            Vector2Int cursor = new Vector2Int(2, 8);
            AddUnique(route.cells, cursor);

            AddRouteLine(route, ref cursor, new Vector2Int(10, 8));
            AddRouteLine(route, ref cursor, new Vector2Int(10, 12));
            route.markers["ShortcutAEntry"] = route.cells.Count - 1;

            AddRouteLine(route, ref cursor, new Vector2Int(4, 12));
            AddRouteLine(route, ref cursor, new Vector2Int(4, 16));
            AddRouteLine(route, ref cursor, new Vector2Int(10, 15));
            AddRouteLine(route, ref cursor, new Vector2Int(16, 16));

            AddRouteLine(route, ref cursor, new Vector2Int(16, 20));
            AddRouteLine(route, ref cursor, new Vector2Int(8, 20));

            AddRouteLine(route, ref cursor, new Vector2Int(8, 24));
            AddRouteLine(route, ref cursor, new Vector2Int(22, 24));

            AddRouteLine(route, ref cursor, new Vector2Int(22, 19));

            AddRouteLine(route, ref cursor, new Vector2Int(29, 19));

            AddRouteLine(route, ref cursor, new Vector2Int(29, 23));
            AddRouteLine(route, ref cursor, new Vector2Int(39, 23));
            AddRouteLine(route, ref cursor, new Vector2Int(39, 27));

            AddRouteLine(route, ref cursor, new Vector2Int(32, 27));

            AddRouteLine(route, ref cursor, new Vector2Int(32, 31));
            AddRouteLine(route, ref cursor, new Vector2Int(39, 32));
            AddRouteLine(route, ref cursor, new Vector2Int(42, 31));
            AddRouteLine(route, ref cursor, new Vector2Int(42, 35));

            MarkCell(route, "ShortcutAEntry", new Vector2Int(10, 12));
            MarkCell(route, "ShortcutAExit", new Vector2Int(10, 15));
            MarkCell(route, "ShortcutBEntry", new Vector2Int(11, 20));
            MarkCell(route, "ShortcutBExit", new Vector2Int(11, 24));
            MarkCell(route, "ShortcutCEntry", new Vector2Int(39, 27));
            MarkCell(route, "ShortcutCExit", new Vector2Int(39, 32));
            MarkCell(route, "ShortcutDEntry", new Vector2Int(22, 23));
            MarkCell(route, "ShortcutDExit", new Vector2Int(29, 23));
            return route;
        }

        private static void AddRouteLine(MainRouteData route, ref Vector2Int cursor, Vector2Int target)
        {
            AddLine(route.cells, cursor, target);
            cursor = target;
        }

        private static void MoveSegment(MainRouteData route, ref Vector2Int cursor, Vector2Int direction, int count, string entryMarker = null, int entryStep = 0)
        {
            Move(route, ref cursor, direction, count, entryMarker, entryStep);
            Move(route, ref cursor, Vector2Int.up, 1, null, 0);
        }

        private static void MarkCell(MainRouteData route, string marker, Vector2Int cell)
        {
            int index = route.cells.IndexOf(cell);
            if (index < 0)
            {
                Debug.LogWarning($"Route marker skipped: {marker} at {cell}");
                return;
            }

            route.markers[marker] = index;
        }

        private static void Move(MainRouteData route, ref Vector2Int cursor, Vector2Int direction, int count, string marker, int markerStep)
        {
            for (int i = 1; i <= count; i++)
            {
                cursor += direction;
                cursor.x = Mathf.Clamp(cursor.x, GridMin, GridMax);
                cursor.y = Mathf.Clamp(cursor.y, GridMin, GridMax);
                AddUnique(route.cells, cursor);
                if (!string.IsNullOrEmpty(marker) && i == markerStep)
                {
                    route.markers[marker] = route.cells.Count - 1;
                }
            }
        }

        private List<BoardNode> CreateMainPath(BoardGraph graph, IReadOnlyList<Vector2Int> cells)
        {
            var mainPath = new List<BoardNode>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                TileType type = i == 0 || i == cells.Count - 1 ? TileType.Safe : PickType(i);
                BoardNode node = graph.CreateNode(GridToWorld(cells[i], 0.06f), type);
                node.progressIndex = i;
                node.routeLineId = GetRouteLineId(cells[i]);
                node.isSafeZone = type == TileType.Safe;
                mainPath.Add(node);
                mainNodesByGrid[cells[i]] = node;
                if (i > 0)
                {
                    graph.Connect(mainPath[i - 1], node);
                }
            }

            return mainPath;
        }

        private static void ApplyRouteBalance(IReadOnlyList<BoardNode> mainPath, MainRouteData route)
        {
            SetSafe(mainPath, 0);
            SetSafe(mainPath, mainPath.Count - 1);
            SetSafe(mainPath, 30);
            SetSafe(mainPath, 60);
            SetSafe(mainPath, 90);

            SetType(mainPath, route, "ShortcutAEntry", TileType.Yellow);
            SetType(mainPath, route, "ShortcutBEntry", TileType.Yellow);
            SetType(mainPath, route, "ShortcutCEntry", TileType.Yellow);
            SetType(mainPath, route, "ShortcutDEntry", TileType.Yellow);
        }

        private static void SetSafe(IReadOnlyList<BoardNode> path, int index)
        {
            SetType(path, index, TileType.Safe);
            if (index >= 0 && index < path.Count)
            {
                path[index].isSafeZone = true;
            }
        }

        private static void SetType(IReadOnlyList<BoardNode> path, MainRouteData route, string marker, TileType type)
        {
            if (route.markers.TryGetValue(marker, out int index))
            {
                SetType(path, index, type);
            }
        }

        private static void SetType(IReadOnlyList<BoardNode> path, int index, TileType type)
        {
            if (index < 0 || index >= path.Count)
            {
                return;
            }

            path[index].type = type;
            path[index].isSafeZone = type == TileType.Safe;
        }

        private void AddShortcut(BoardGraph graph, Vector2Int entryCell, Vector2Int exitCell, IReadOnlyList<Vector2Int> shortcutCells)
        {
            if (!mainNodesByGrid.TryGetValue(entryCell, out BoardNode entry) || !mainNodesByGrid.TryGetValue(exitCell, out BoardNode exit))
            {
                Debug.LogWarning($"Shortcut skipped: {entryCell} -> {exitCell}");
                return;
            }

            BoardNode previous = entry;
            for (int i = 0; i < shortcutCells.Count; i++)
            {
                BoardNode node = graph.CreateNode(GridToWorld(shortcutCells[i], 0.09f), i == shortcutCells.Count - 1 ? TileType.Red : TileType.Purple);
                node.isShortcut = true;
                node.progressIndex = Mathf.Clamp(entry.progressIndex + i + 1, entry.progressIndex, exit.progressIndex);
                node.routeLineId = GetRouteLineId(shortcutCells[i]);
                graph.Connect(previous, node);
                previous = node;
            }

            graph.Connect(previous, exit);
        }

        private void BuildGridViews()
        {
            Transform gridRoot = EnsureChild(boardRoot, "Grid");
            gridRoot.gameObject.SetActive(showBackgroundGrid);
            if (!showBackgroundGrid)
            {
                for (int i = 0; i < gridRoot.childCount; i++)
                {
                    gridRoot.GetChild(i).gameObject.SetActive(false);
                }

                return;
            }

            for (int y = GridMin; y <= GridMax; y++)
            {
                for (int x = GridMin; x <= GridMax; x++)
                {
                    GameObject viewObject = poolManager.Get("GridTile", gridRoot);
                    if (viewObject == null)
                    {
                        continue;
                    }

                    GridCellView view = viewObject.GetComponent<GridCellView>() ?? viewObject.AddComponent<GridCellView>();
                    view.Bind(GridToWorld(new Vector2Int(x, y), -0.13f), x, y, CellSize);
                }
            }
        }

        private void BuildRouteViews()
        {
            Transform routeRoot = EnsureChild(boardRoot, "Route");
            nodeViews.Clear();
            foreach (BoardNode node in Graph.Nodes)
            {
                GameObject viewObject = poolManager.Get("BoardNode", routeRoot);
                if (viewObject == null)
                {
                    continue;
                }

                BoardNodeView view = viewObject.GetComponent<BoardNodeView>() ?? viewObject.AddComponent<BoardNodeView>();
                view.Bind(node);
                nodeViews[node.id] = view;
            }
        }

        private static void AddLine(ICollection<Vector2Int> cells, Vector2Int from, Vector2Int to)
        {
            Vector2Int cursor = from;
            AddUnique(cells, cursor);

            while (cursor != to)
            {
                if (cursor.x != to.x)
                {
                    cursor.x += cursor.x < to.x ? 1 : -1;
                }

                if (cursor.y != to.y)
                {
                    cursor.y += cursor.y < to.y ? 1 : -1;
                }

                AddUnique(cells, cursor);
            }
        }

        private static void AddUnique(ICollection<Vector2Int> cells, Vector2Int cell)
        {
            if (cells is List<Vector2Int> list && list.Count > 0 && list[^1] == cell)
            {
                return;
            }

            cells.Add(cell);
        }

        private static Vector3 GridToWorld(Vector2Int cell, float y)
        {
            return new Vector3(cell.x * CellSize, y, cell.y * CellSize);
        }

        private static bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= GridMin && cell.x <= GridMax && cell.y >= GridMin && cell.y <= GridMax;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static TileType PickType(int index)
        {
            if (index > 0 && index % 35 == 0)
            {
                return TileType.BlackRed;
            }

            if (index > 0 && index % 21 == 0)
            {
                return TileType.Red;
            }

            if (index > 0 && index % 14 == 0)
            {
                return TileType.Yellow;
            }

            if (index > 0 && index % 10 == 0)
            {
                return TileType.Purple;
            }

            if (index > 0 && index % 6 == 0)
            {
                return TileType.Green;
            }

            return TileType.Normal;
        }

        private static int GetRouteLineId(Vector2Int cell)
        {
            return Mathf.Clamp(cell.y / 6, 0, 7);
        }

        private void OnDrawGizmos()
        {
            if (Graph == null)
            {
                return;
            }

            foreach (BoardNode node in Graph.Nodes)
            {
                Gizmos.color = node.state switch
                {
                    NodeState.Corrupted => new Color(0.45f, 0f, 0.8f, 1f),
                    NodeState.Destroyed => Color.black,
                    _ => node.isShortcut ? new Color(0.55f, 0.12f, 0.72f, 1f) : Color.yellow
                };
                Gizmos.DrawSphere(node.position + Vector3.up * 0.25f, 0.12f);

                Gizmos.color = node.isShortcut ? new Color(0.52f, 0.12f, 0.7f, 1f) : Color.cyan;
                foreach (BoardNode next in node.nextNodes)
                {
                    Gizmos.DrawLine(node.position + Vector3.up * 0.25f, next.position + Vector3.up * 0.25f);
                }
            }
        }
    }
}
