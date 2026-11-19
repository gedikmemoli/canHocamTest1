// Dosya: Assets/Scripts/WaypointGraph.cs
using System.Collections.Generic;
using UnityEngine;

public class WaypointGraph : MonoBehaviour
{
    public LayerMask obstacleMask;          // Obstacle katmanı
    public float maxEdgeLength = 8f;        // bundan uzun kenar kurma
    [HideInInspector] public Waypoint[] nodes;

    [Header("9.1 Deney ve Ölçüm Aracı")]
    public Transform testFrom, testTo;      // TestBaşlangıç ve TestHedef nesneleri
    public bool testHeuristic = true;       // false: Dijkstra

    private void Awake() => BuildEdges();

    [ContextMenu("Kenarları Kur")]
    public void BuildEdges()
    {
        nodes = GetComponentsInChildren<Waypoint>();     // hiyerarşi sırasıyla: W0, W1, ...
        int undirectedEdges = 0;

        foreach (Waypoint a in nodes)
        {
            a.neighbors.Clear();
            foreach (Waypoint b in nodes)
            {
                if (a == b) continue;
                float dist = Vector3.Distance(a.transform.position, b.transform.position);
                if (dist <= maxEdgeLength && CanSee(a.transform.position, b.transform.position))
                {
                    a.neighbors.Add(b);
                    undirectedEdges++;
                }
            }
        }

        Debug.Log($"[WaypointGraph] {nodes.Length} düğüm için {undirectedEdges / 2} yönsüz kenar ({undirectedEdges} yönlü komşuluk) kuruldu.");
    }

    // İki nokta arasında engel yok mu? (3. haftadaki görüş hattı)
    public bool CanSee(Vector3 from, Vector3 to)
    {
        return !Physics.Linecast(from, to, obstacleMask);
    }

    // Bir konuma görünen en yakın düğüm
    public Waypoint Nearest(Vector3 pos)
    {
        if (nodes == null || nodes.Length == 0) nodes = GetComponentsInChildren<Waypoint>();
        Waypoint best = null;
        float bestDist = float.MaxValue;
        foreach (Waypoint w in nodes)
        {
            if (w == null || !w.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(pos, w.transform.position);
            if (d < bestDist && CanSee(pos, w.transform.position))
            {
                best = w;
                bestDist = d;
            }
        }
        return best;
    }

    [ContextMenu("A* Testi")]
    public void TestPath()
    {
        BuildEdges();
        if (testFrom == null || testTo == null)
        {
            Debug.LogWarning("[WaypointGraph] TestBaşlangıç (testFrom) ve TestHedef (testTo) atanmalıdır.");
            return;
        }

        Waypoint s = Nearest(testFrom.position);
        Waypoint g = Nearest(testTo.position);
        if (s == null || g == null) { Debug.Log("Başlangıç veya hedef için görünen düğüm yok"); return; }

        var astar = new GraphAStar { useHeuristic = testHeuristic };
        List<Waypoint> p = astar.FindPath(s, g);
        if (p == null) { Debug.Log("Yol yok"); return; }

        // Uzunluk: başlangıç → ilk düğüm + düğümler arası + son düğüm → hedef
        float len = Vector3.Distance(testFrom.position, s.transform.position)
                  + Vector3.Distance(g.transform.position, testTo.position);
        for (int i = 1; i < p.Count; i++)
            len += Vector3.Distance(p[i - 1].transform.position, p[i].transform.position);

        Debug.Log($"{string.Join(" → ", p.ConvertAll(w => w.name))} | " +
                  $"genişletilen: {astar.Expanded} | uzunluk: {len:F1} m");
    }
}
