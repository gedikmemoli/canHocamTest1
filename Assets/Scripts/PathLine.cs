using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathLine : MonoBehaviour
{
    public GridPathfinder pathfinder;
    public MonoBehaviour gridSource;     // MazeGenerator ya da ödevde TerrainGrid
    public float lift = 0.3f;            // çizgi zeminin biraz üstünde

    private LineRenderer line;
    private ISearchGrid grid;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        grid = gridSource as ISearchGrid;
        line.positionCount = 0;

        if (pathfinder != null)
            pathfinder.OnPathFound += Draw;

        if (gridSource is MazeGenerator maze)
            maze.OnRegenerated += Hide;
    }

    public void Draw(List<Vector2Int> path)
    {
        if (grid == null && gridSource != null) grid = gridSource as ISearchGrid;
        if (grid == null || path == null) return;

        line.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            line.SetPosition(i, grid.CellToWorld(path[i]) + Vector3.up * lift);
    }

    public void Hide() => line.positionCount = 0;
}
