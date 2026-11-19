using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MazeGenerator : MonoBehaviour, ISearchGrid
{
    public const int FLOOR = 0;
    public const int WALL = 1;

    [Header("Labirent Boyutları")]
    public int width = 31;
    public int depth = 31;
    public float cellSize = 2f;
    public int seed = 2026;
    [Range(0f, 0.3f)] public float loopChance = 0.08f;

    [Header("Görsel")]
    public GameObject wallPrefab;
    public Transform floor;

    [Header("Arazi")]
    public Terrain terrain;
    public float wallHeight = 2f;
    public float wallSink = 1f;

    public int[,] Map { get; private set; }
    public int Width => width;
    public int Depth => depth;
    public Vector2Int StartCell { get; private set; } = new Vector2Int(1, 1);
    public Vector2Int GoalCell { get; private set; }
    public float MinCellCost => 1f;

    public event Action OnRegenerated;
    public event Action<Vector2Int> OnCellChanged;

    private readonly Dictionary<Vector2Int, GameObject> wallObjects = new();

    private void Awake()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        Generate(seed);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            seed++;
            Generate(seed);
        }
    }

    public void Generate(int newSeed)
    {
        if (width % 2 == 0) width++;
        if (depth % 2 == 0) depth++;

        UnityEngine.Random.InitState(newSeed);
        Map = new int[width, depth];

        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                Map[x, z] = WALL;

        StartCell = new Vector2Int(1, 1);
        GoalCell = new Vector2Int(width - 2, depth - 2);

        CarveIterative(StartCell);
        AddLoops();
        Build();
        Validate();
        OnRegenerated?.Invoke();
    }

    private void CarveIterative(Vector2Int start)
    {
        Stack<Vector2Int> stack = new();
        Map[start.x, start.y] = FLOOR;
        stack.Push(start);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> unvisited = new();

            foreach (var d in dirs)
            {
                Vector2Int next = current + d * 2;
                if (next.x > 0 && next.y > 0 && next.x < width - 1 && next.y < depth - 1 && Map[next.x, next.y] == WALL)
                {
                    unvisited.Add(d);
                }
            }

            if (unvisited.Count > 0)
            {
                Vector2Int chosenDir = unvisited[UnityEngine.Random.Range(0, unvisited.Count)];
                Vector2Int between = current + chosenDir;
                Vector2Int next = current + chosenDir * 2;
                Map[between.x, between.y] = FLOOR;
                Map[next.x, next.y] = FLOOR;
                stack.Push(next);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    private void AddLoops()
    {
        for (int x = 2; x < width - 2; x++)
        {
            for (int z = 2; z < depth - 2; z++)
            {
                if (Map[x, z] != WALL) continue;
                bool horiz = Map[x - 1, z] == FLOOR && Map[x + 1, z] == FLOOR && Map[x, z - 1] == WALL && Map[x, z + 1] == WALL;
                bool vert = Map[x, z - 1] == FLOOR && Map[x, z + 1] == FLOOR && Map[x - 1, z] == WALL && Map[x + 1, z] == WALL;
                if ((horiz || vert) && UnityEngine.Random.value < loopChance)
                {
                    Map[x, z] = FLOOR;
                }
            }
        }
    }

    private GameObject SpawnWall(Vector2Int c)
    {
        if (wallPrefab == null) return null;
        float h = wallHeight + wallSink;
        Vector3 pos = CellToWorld(c) + Vector3.up * ((wallHeight - wallSink) * 0.5f);
        GameObject w = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
        w.name = $"Wall_{c.x}_{c.y}";
        w.transform.localScale = new Vector3(cellSize, h, cellSize);
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer != -1) w.layer = wallLayer;
        return w;
    }

    public void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject old = transform.GetChild(i).gameObject;
            old.SetActive(false);
            Destroy(old);
        }
        wallObjects.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (Map[x, z] == WALL)
                {
                    var c = new Vector2Int(x, z);
                    GameObject w = SpawnWall(c);
                    if (w != null) wallObjects[c] = w;
                }
            }
        }

        if (floor != null)
        {
            floor.position = transform.position +
                new Vector3((width - 1) * cellSize * 0.5f, 0f, (depth - 1) * cellSize * 0.5f);
            floor.localScale = new Vector3(width * cellSize / 10f, 1f, depth * cellSize / 10f);
        }
    }

    public bool ToggleWall(Vector2Int c)
    {
        bool border = c.x <= 0 || c.y <= 0 || c.x >= width - 1 || c.y >= depth - 1;
        if (!InBounds(c) || border) return false;

        if (Map[c.x, c.y] == WALL)
        {
            Map[c.x, c.y] = FLOOR;
            if (wallObjects.TryGetValue(c, out GameObject w) && w != null)
            {
                w.SetActive(false);
                Destroy(w);
                wallObjects.Remove(c);
            }
        }
        else
        {
            Map[c.x, c.y] = WALL;
            GameObject w = SpawnWall(c);
            if (w != null) wallObjects[c] = w;
        }

        OnCellChanged?.Invoke(c);
        return true;
    }

    private void Validate()
    {
        int rooms = ((width - 1) / 2) * ((depth - 1) / 2);
        int floorCount = 0;
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                if (Map[x, z] == FLOOR) floorCount++;

        Debug.Log($"[MazeGenerator] Labirent {width}x{depth}, tohum {seed}: {rooms} oda, {floorCount} yürünebilir zemin hücresi.");
    }

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < depth;

    public bool IsWalkable(Vector2Int c) => InBounds(c) && Map != null && Map[c.x, c.y] == FLOOR;

    public float CellCost(Vector2Int c) => 1f;

    public Vector3 CellToWorld(Vector2Int c)
    {
        Vector3 p = transform.position + new Vector3(c.x * cellSize, 0f, c.y * cellSize);
        if (terrain != null)
            p.y = terrain.transform.position.y + terrain.SampleHeight(p);
        return p;
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;
        int x = Mathf.RoundToInt(local.x / cellSize);
        int z = Mathf.RoundToInt(local.z / cellSize);
        return new Vector2Int(x, z);
    }
}
