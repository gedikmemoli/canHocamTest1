using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum SearchAlgorithm { DFS, BFS, Dijkstra, AStar }
public enum HeuristicType { Manhattan, Euclidean, Chebyshev }

public class GridPathfinder : MonoBehaviour
{
    [Header("Harita ve Görsel")]
    public MonoBehaviour gridSource; // ISearchGrid (MazeGenerator veya TerrainGrid)
    public PathMarker markerPrefab;
    public bool drawMarkers = true;
    public float markerLift = 0.15f;
    public float stepDelay = 0.005f;

    [Header("Algoritma Ayarları")]
    public SearchAlgorithm algorithm = SearchAlgorithm.AStar;
    public HeuristicType heuristic = HeuristicType.Manhattan;

    [Header("Sonuçlar (Salt Okunur)")]
    public int expandedCount;
    public float totalCost;
    public List<Vector2Int> Path { get; private set; } = new();

    public event Action<List<Vector2Int>> OnPathFound;

    private ISearchGrid grid;
    private Coroutine running;
    private readonly Dictionary<Vector2Int, PathMarker> markers = new();

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    private void Awake()
    {
        grid = gridSource as ISearchGrid;
    }

    private void Update()
    {
        if (grid == null && gridSource != null) grid = gridSource as ISearchGrid;
        if (Keyboard.current == null || grid == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) { algorithm = SearchAlgorithm.DFS; Debug.Log("[GridPathfinder] Algoritma: DFS"); }
        if (Keyboard.current.digit2Key.wasPressedThisFrame) { algorithm = SearchAlgorithm.BFS; Debug.Log("[GridPathfinder] Algoritma: BFS"); }
        if (Keyboard.current.digit3Key.wasPressedThisFrame) { algorithm = SearchAlgorithm.Dijkstra; Debug.Log("[GridPathfinder] Algoritma: Dijkstra"); }
        if (Keyboard.current.digit4Key.wasPressedThisFrame) { algorithm = SearchAlgorithm.AStar; Debug.Log("[GridPathfinder] Algoritma: A* (AStar)"); }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            FindPath(grid.StartCell, grid.GoalCell);
        }
    }

    public void FindPath(Vector2Int start, Vector2Int goal)
    {
        if (grid == null && gridSource != null) grid = gridSource as ISearchGrid;
        if (grid == null) return;

        Clear();
        running = StartCoroutine(Search(start, goal));
    }

    public void Clear()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        foreach (var kv in markers)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
        markers.Clear();
        Path.Clear();
        expandedCount = 0;
        totalCost = 0f;
    }

    private IFrontier<Vector2Int> CreateFrontier()
    {
        return algorithm switch
        {
            SearchAlgorithm.DFS => new StackFrontier<Vector2Int>(),
            SearchAlgorithm.BFS => new QueueFrontier<Vector2Int>(),
            _ => new PriorityFrontier<Vector2Int>()
        };
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        if (algorithm != SearchAlgorithm.AStar) return 0f;

        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        float d = heuristic switch
        {
            HeuristicType.Euclidean => Mathf.Sqrt(dx * dx + dy * dy),
            HeuristicType.Chebyshev => Mathf.Max(dx, dy),
            _ => dx + dy
        };
        return d * grid.MinCellCost;
    }

    private IEnumerator Search(Vector2Int start, Vector2Int goal)
    {
        if (!grid.IsWalkable(start) || !grid.IsWalkable(goal))
        {
            Debug.LogWarning($"[GridPathfinder] Başlangıç ({start}) veya Hedef ({goal}) yürünemez hücre!");
            yield break;
        }

        IFrontier<Vector2Int> open = CreateFrontier();
        HashSet<Vector2Int> closed = new();
        Dictionary<Vector2Int, Vector2Int> parent = new();
        Dictionary<Vector2Int, float> gScore = new();

        gScore[start] = 0f;
        open.Push(start, Heuristic(start, goal));
        Paint(start, new Color(0.2f, 0.6f, 1f));

        bool found = false;

        while (open.Count > 0)
        {
            Vector2Int current = open.Pop();
            if (!closed.Add(current)) continue;

            expandedCount++;
            Paint(current, new Color(0.55f, 0.55f, 0.55f));

            if (current == goal)
            {
                found = true;
                break;
            }

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (!grid.IsWalkable(next) || closed.Contains(next)) continue;

                float stepCost = grid.CellCost(next);
                float newG = gScore[current] + stepCost;

                if (!gScore.TryGetValue(next, out float oldG) || newG < oldG)
                {
                    gScore[next] = newG;
                    parent[next] = current;
                    float f = newG + Heuristic(next, goal);
                    open.Push(next, f);
                    Paint(next, new Color(0.2f, 0.75f, 1f));
                }
            }

            if (stepDelay > 0f)
                yield return new WaitForSeconds(stepDelay);
        }

        BuildPath(found, start, goal, parent, gScore);
        if (found)
        {
            OnPathFound?.Invoke(Path);
        }
    }

    private void BuildPath(bool found, Vector2Int start, Vector2Int goal,
                           Dictionary<Vector2Int, Vector2Int> parent,
                           Dictionary<Vector2Int, float> gScore)
    {
        Path.Clear();
        if (!found)
        {
            Debug.LogWarning($"[GridPathfinder] {algorithm}: yol yok, {expandedCount} düğüm genişletildi.");
            return;
        }

        Vector2Int cur = goal;
        while (true)
        {
            Path.Add(cur);
            Paint(cur, Color.yellow);
            if (cur == start) break;
            cur = parent[cur];
        }
        Path.Reverse();
        totalCost = gScore[goal];
        Debug.Log($"[GridPathfinder] {algorithm} ({heuristic}): Yol uzunluğu = {Path.Count} adım | Toplam Maliyet = {totalCost:F2} | Genişletilen = {expandedCount}");
    }

    private void Paint(Vector2Int c, Color col)
    {
        if (!drawMarkers || markerPrefab == null) return;

        if (!markers.TryGetValue(c, out PathMarker m) || m == null)
        {
            Vector3 pos = grid.CellToWorld(c) + Vector3.up * markerLift;
            m = Instantiate(markerPrefab, pos, Quaternion.identity, transform);
            markers[c] = m;
        }
        m.SetColor(col);
    }
}
