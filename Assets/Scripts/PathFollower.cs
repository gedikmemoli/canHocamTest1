using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PathFollower : MonoBehaviour
{
    public GridPathfinder pathfinder;
    public MonoBehaviour gridSource;      // MazeGenerator ya da ödevde TerrainGrid
    public float speed = 4f;              // m/sn (NavMesh ajanıyla aynı)
    public float turnSpeed = 10f;         // 3. hafta: t = 1 − e^(−k·Δt)
    public float arriveRadius = 0.4f;     // bu kadar yaklaşınca sıradaki hücreye geç
    public float gravity = -20f;
    public bool adjustSpeedBySlope = false; // Dağ Yolu ödevinde dik yerlerde yavaşlama

    public bool IsMoving => route != null && index < route.Count;
    public IReadOnlyList<Vector2Int> Route => route;   // Adım 9: Replanner okuyacak
    public int Index => index;

    private CharacterController cc;
    private ISearchGrid grid;
    private List<Vector2Int> route;
    private int index;
    private float verticalSpeed;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        grid = gridSource as ISearchGrid;
        if (pathfinder != null)
            pathfinder.OnPathFound += Follow;
        if (gridSource is MazeGenerator maze)
            maze.OnRegenerated += () => Teleport(grid.StartCell);
    }

    private void Start()
    {
        if (grid == null && gridSource != null) grid = gridSource as ISearchGrid;
        if (grid != null) Teleport(grid.StartCell);
    }

    public void Teleport(Vector2Int cell)
    {
        if (grid == null && gridSource != null) grid = gridSource as ISearchGrid;
        if (grid == null) return;

        cc.enabled = false;               // açıkken CharacterController konumu geri yazar
        float up = cc.height * 0.5f - cc.center.y + 0.1f;   // ayaklar zeminin 10 cm üstünde
        transform.position = grid.CellToWorld(cell) + Vector3.up * up;
        cc.enabled = true;
        route = null;
        verticalSpeed = 0f;
    }

    public void Follow(List<Vector2Int> path)
    {
        if (path == null) return;
        route = new List<Vector2Int>(path);   // kopya: yol bulucu kendi listesini yeniden kullanır
        index = 0;                            // önce bulunduğumuz hücrenin merkezine
    }

    public void Stop() => route = null;

    private void Update()
    {
        if (grid == null && gridSource != null) grid = gridSource as ISearchGrid;
        if (grid == null) return;

        Vector3 horizontal = Vector3.zero;

        if (IsMoving)
        {
            Vector3 target = grid.CellToWorld(route[index]);
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;                                 // yükseklik farkını yok say

            if (toTarget.magnitude <= arriveRadius)
            {
                index++;                                     // vardık, sıradakine
            }
            else
            {
                Vector3 dir = toTarget.normalized;
                float t = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), t);

                float currentSpeed = speed;
                if (adjustSpeedBySlope)
                {
                    float cellCost = Mathf.Max(1f, grid.CellCost(route[index]));
                    currentSpeed = speed / Mathf.Clamp(cellCost * 0.5f, 1f, 3f);
                }

                horizontal = dir * currentSpeed;
            }
        }

        if (cc.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;   // zemine bastır
        verticalSpeed += gravity * Time.deltaTime;                      // 2. hafta: v ← v + a·Δt

        cc.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
    }
}
