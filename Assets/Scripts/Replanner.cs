using UnityEngine;

public class Replanner : MonoBehaviour
{
    public MazeGenerator maze;
    public GridPathfinder pathfinder;
    public PathFollower follower;
    public bool replanOnAnyChange = false;   // açıkken açılan duvarlar da kısa yol için denenir

    private void Awake()
    {
        if (maze != null)
            maze.OnCellChanged += OnCellChanged;
    }

    private void OnCellChanged(Vector2Int cell)
    {
        if (follower == null || pathfinder == null || maze == null) return;
        if (!follower.IsMoving) return;

        bool blocked = false;                          // değişen hücre kalan yolun üstünde mi?
        for (int i = follower.Index; i < follower.Route.Count; i++)
            if (follower.Route[i] == cell) { blocked = true; break; }

        if (!blocked && !replanOnAnyChange) return;

        Vector2Int goal = follower.Route[follower.Route.Count - 1];
        Vector2Int here = maze.WorldToCell(follower.transform.position);
        follower.Stop();                               // kapalı yolda yürümeye devam etmesin
        pathfinder.FindPath(here, goal);               // bulununca OnPathFound → Follow

        Debug.Log(blocked ? $"Yol {cell} hücresinde kapandı, yeniden planlanıyor"
                          : $"{cell} değişti, daha kısa yol aranıyor");
    }
}
