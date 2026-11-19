using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class ClickToGoal : MonoBehaviour
{
    public Camera cam;
    public MazeGenerator maze;
    public MonoBehaviour customGridSource; // Ödevde TerrainGrid veya MazeGenerator
    public Transform goalFlag;
    public LayerMask groundMask;
    public NavMeshAgent navAgent;

    public GridPathfinder pathfinder;  // YENİ: bizim A*
    public Transform agent;            // YENİ: bizim karakter

    private ISearchGrid Grid => (customGridSource as ISearchGrid) ?? maze;

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Camera activeCam = cam != null ? cam : Camera.main;
        if (activeCam == null || Grid == null) return;

        Ray ray = activeCam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask)) return;

        Vector2Int goal = Grid.WorldToCell(hit.point);
        if (!Grid.IsWalkable(goal))
        {
            Debug.Log($"{goal}: duvar ya da yürünebilir alan dışı");
            return;
        }

        Vector3 goalWorld = Grid.CellToWorld(goal);
        if (goalFlag != null) goalFlag.position = goalWorld;
        if (navAgent != null && navAgent.isOnNavMesh) navAgent.SetDestination(goalWorld);

        // YENİ: aynı hedef için kendi A*'ımız
        Vector2Int start = agent != null ? Grid.WorldToCell(agent.position) : Grid.StartCell;
        if (pathfinder != null) pathfinder.FindPath(start, goal);
    }
}
