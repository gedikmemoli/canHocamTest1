using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshRefresher : MonoBehaviour
{
    public NavMeshSurface surface;
    public MazeGenerator maze;
    public NavMeshAgent navAgent;

    private void Awake()
    {
        if (maze != null)
        {
            maze.OnRegenerated += Rebuild;            // R: yeni labirent, baştan pişir
            maze.OnCellChanged += _ => Refresh();     // sağ tık: yalnızca güncelle
        }
    }

    private void Start() => Rebuild();                // labirent Awake'te üretildi, şimdi pişir

    public void Rebuild()
    {
        if (surface == null || maze == null) return;
        FitVolumeToMaze();
        surface.BuildNavMesh();                       // eşzamanlı: kısa bir takılma olabilir
        if (navAgent != null && navAgent.gameObject.activeInHierarchy)
            navAgent.Warp(maze.CellToWorld(maze.StartCell));
    }

    public void Refresh()
    {
        if (surface == null || surface.navMeshData == null) return;
        AsyncOperation op = surface.UpdateNavMesh(surface.navMeshData);   // arka planda
        op.completed += _ =>
        {
            if (navAgent != null && navAgent.isOnNavMesh && navAgent.hasPath)
                navAgent.SetDestination(navAgent.destination);            // yeni ağda yeniden yol
        };
    }

    private void FitVolumeToMaze()
    {
        // Bütün araziyi pişirmek yerine yalnızca labirentin çevresini pişir: çok daha hızlı
        surface.collectObjects = CollectObjects.Volume;
        surface.transform.SetPositionAndRotation(
            maze.CellToWorld(new Vector2Int(maze.width / 2, maze.depth / 2)), Quaternion.identity);
        surface.center = Vector3.zero;
        surface.size = new Vector3(maze.width * maze.cellSize + 4f, 80f, maze.depth * maze.cellSize + 4f);
    }
}
