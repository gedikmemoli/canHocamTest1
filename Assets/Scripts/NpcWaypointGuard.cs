// Dosya: Assets/Scripts/NpcWaypointGuard.cs
using System.Collections.Generic;
using UnityEngine;

public class NpcWaypointGuard : MonoBehaviour
{
    public enum State { Patrol, Chase, Return }

    public WaypointGraph graph;
    public Transform player;
    public Waypoint[] patrolRoute;                       // devriye sırası: W0 … W7
    public float patrolSpeed = 2f, chaseSpeed = 4f, arriveRadius = 0.2f;
    public float viewDistance = 10f, viewAngle = 120f, loseTime = 3f;

    [Header("Hafta 5 Ödevi: Devriye Modu ve Yol Yumuşatma")]
    public bool pingPong = false;                        // true: 0 1 2 … 7 6 5 … 0 1 …
    public bool usePathSmoothing = true;                 // Ek görev (+10): 7.2'deki yol yumuşatma

    private State state = State.Patrol;
    private int patrolIndex;
    private int patrolDir = 1;                           // +1 ileri, −1 geri
    private Waypoint heading;                            // şu an yürüdüğü düğüm (yoksa null)

    private readonly GraphAStar astar = new();
    private readonly List<Vector3> path = new();         // izlenecek noktalar
    private readonly List<Waypoint> pathNodes = new();   // aynı sırada düğümler; son nokta için null
    private int pathIndex;
    private float lastSeenTime = -999f, replanTimer;

    private void Update()
    {
        if (graph == null || player == null || patrolRoute == null || patrolRoute.Length == 0) return;

        bool sees = CanSeePlayer();
        if (sees) lastSeenTime = Time.time;

        if (state == State.Patrol)
        {
            if (sees) { state = State.Chase; replanTimer = 0f; return; }

            heading = patrolRoute[patrolIndex];
            if (MoveTo(heading.transform.position, patrolSpeed))
            {
                if (pingPong && patrolRoute.Length > 1)
                {
                    int next = patrolIndex + patrolDir;
                    if (next < 0 || next >= patrolRoute.Length)
                        patrolDir = -patrolDir;
                    patrolIndex += patrolDir;
                }
                else
                {
                    patrolIndex = (patrolIndex + 1) % patrolRoute.Length;   // döngü modu
                }
            }
        }
        else if (state == State.Chase)
        {
            if (Time.time - lastSeenTime > loseTime) { StartReturn(); return; }

            replanTimer -= Time.deltaTime;
            if (replanTimer <= 0f) { PlanTo(player.position); replanTimer = 0.5f; }

            FollowPath(chaseSpeed);
        }
        else // State.Return
        {
            if (sees) { state = State.Chase; replanTimer = 0f; return; }
            if (FollowPath(patrolSpeed)) state = State.Patrol;
        }
    }

    // Görüş: mesafe, açı, engel (3. hafta)
    private bool CanSeePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > viewDistance) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle / 2f) return false;
        return graph.CanSee(transform.position, player.position);
    }

    // Hedefe yol kur: görüyorsan düz git, görmüyorsan graf üzerinden A*
    private void PlanTo(Vector3 target)
    {
        path.Clear();
        pathNodes.Clear();
        pathIndex = 0;

        if (!graph.CanSee(transform.position, target))
        {
            Waypoint start = heading != null ? heading : graph.Nearest(transform.position);
            Waypoint goal = graph.Nearest(target);
            List<Waypoint> nodes = (start != null && goal != null) ? astar.FindPath(start, goal) : null;
            if (nodes != null)
            {
                foreach (Waypoint w in nodes) { path.Add(w.transform.position); pathNodes.Add(w); }
                Debug.Log($"A*: {start.name} → {goal.name}, yol {nodes.Count} düğüm, {astar.Expanded} genişletildi");
            }
        }
        path.Add(target);                                // son durak: hedefin kendisi
        pathNodes.Add(null);

        if (usePathSmoothing && path.Count > 1)
        {
            SmoothPath();
        }

        heading = pathNodes.Count > 0 ? pathNodes[0] : null;
    }

    // Ek görev (+10): 7.2'deki yol yumuşatma (NPC'nin konumundan görülebilen en uzak noktaya kadar ara noktaları sil)
    private void SmoothPath()
    {
        var smoothedPoints = new List<Vector3>();
        var smoothedNodes = new List<Waypoint>();

        Vector3 currentPos = transform.position;
        int i = 0;

        while (i < path.Count)
        {
            int farthest = i;
            for (int j = path.Count - 1; j > i; j--)
            {
                if (graph.CanSee(currentPos, path[j]))
                {
                    farthest = j;
                    break;
                }
            }
            smoothedPoints.Add(path[farthest]);
            smoothedNodes.Add(pathNodes[farthest]);
            currentPos = path[farthest];
            i = farthest + 1;
        }

        path.Clear();
        path.AddRange(smoothedPoints);
        pathNodes.Clear();
        pathNodes.AddRange(smoothedNodes);
    }

    // Gözden kaybedince: en yakın devriye noktasına A* ile dön
    private void StartReturn()
    {
        int best = 0;
        for (int i = 1; i < patrolRoute.Length; i++)
            if (Dist(patrolRoute[i]) < Dist(patrolRoute[best])) best = i;

        patrolIndex = best;
        PlanTo(patrolRoute[best].transform.position);
        state = State.Return;
    }

    private float Dist(Waypoint w) => Vector3.Distance(transform.position, w.transform.position);

    // Yolu sırayla izle; bittiyse true
    private bool FollowPath(float speed)
    {
        if (pathIndex >= path.Count) return true;
        if (MoveTo(path[pathIndex], speed))
        {
            pathIndex++;
            heading = pathIndex < pathNodes.Count ? pathNodes[pathIndex] : null;
        }
        return pathIndex >= path.Count;
    }

    // Hedefe bir adım yürü ve ona dön; vardıysa true
    private bool MoveTo(Vector3 target, float speed)
    {
        target.y = transform.position.y;                 // yalnızca zemin düzleminde
        Vector3 dir = target - transform.position;
        if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        return Vector3.Distance(transform.position, target) < arriveRadius;
    }

    private void OnDrawGizmos()
    {
        // Bakış yönü: yeşil devriye, kırmızı kovalama, sarı dönüş
        Gizmos.color = state == State.Chase ? Color.red : state == State.Return ? Color.yellow : Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);

        // Devriyede: şu anki hedef noktaya çizgi
        if (state == State.Patrol && heading != null)
            Gizmos.DrawLine(transform.position, heading.transform.position);

        // Kalan yol
        for (int i = pathIndex; i < path.Count; i++)
            Gizmos.DrawLine(i == pathIndex ? transform.position : path[i - 1], path[i]);
    }
}
