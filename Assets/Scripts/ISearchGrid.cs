using UnityEngine;

public interface ISearchGrid
{
    int Width { get; }
    int Depth { get; }
    Vector2Int StartCell { get; }
    Vector2Int GoalCell { get; }

    bool InBounds(Vector2Int c);
    bool IsWalkable(Vector2Int c);
    float CellCost(Vector2Int c);
    float MinCellCost { get; }

    Vector3 CellToWorld(Vector2Int c);
    Vector2Int WorldToCell(Vector3 worldPos);
}
