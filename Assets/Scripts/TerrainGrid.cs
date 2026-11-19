using UnityEngine;

/// <summary>
/// Hafta 4 Ödevi: Dağ Yolu (TerrainGrid)
/// Arazi üzerinde sanal bir ızgara kurar, her hücrenin eğimini arazinin normalinden hesaplar,
/// yürünemez dik yamaçları engeller ve eğime bağlı hücre maliyeti (CellCost) döndürür.
/// </summary>
public class TerrainGrid : MonoBehaviour, ISearchGrid
{
    [Header("Izgara Boyutları")]
    public int width = 40;
    public int depth = 40;
    public float cellSize = 2f;

    [Header("Arazi ve Eğim Maliyeti")]
    public Terrain terrain;
    public float maxWalkableSlope = 42f; // Bu eğimin üstü yürünemez
    public float slopeCostFactor = 8f;   // Eğim arttıkça maliyet çarpanı: 1 + (eğim / maxSlope) * factor

    [Header("Başlangıç ve Hedef")]
    public Vector2Int startCell = new(2, 2);
    public Vector2Int goalCell = new(35, 35);

    public int Width => width;
    public int Depth => depth;
    public Vector2Int StartCell => startCell;
    public Vector2Int GoalCell => goalCell;
    public float MinCellCost => 1f;

    private void Awake()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
    }

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < depth;

    public float GetSlopeDegrees(Vector2Int c)
    {
        if (terrain == null || terrain.terrainData == null) return 0f;
        Vector3 world = CellToWorld(c);
        Vector3 local = world - terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float u = Mathf.Clamp01(local.x / size.x);
        float v = Mathf.Clamp01(local.z / size.z);
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(u, v);
        return Mathf.Acos(Mathf.Clamp(normal.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    public bool IsWalkable(Vector2Int c)
    {
        if (!InBounds(c)) return false;
        return GetSlopeDegrees(c) <= maxWalkableSlope;
    }

    public float CellCost(Vector2Int c)
    {
        float slope = GetSlopeDegrees(c);
        float ratio = Mathf.Clamp01(slope / Mathf.Max(1f, maxWalkableSlope));
        return 1f + ratio * ratio * slopeCostFactor;
    }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Vector3 center = transform.position + new Vector3((width - 1) * cellSize * 0.5f, 1f, (depth - 1) * cellSize * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(width * cellSize, 2f, depth * cellSize));
    }
}
