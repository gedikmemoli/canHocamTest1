// Dosya: Assets/Scripts/PathDebugDraw.cs
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

// NPC'nin kalan yolunu ölçer: köşe sayısı, uzunluk ve alan maliyetli toplam
[RequireComponent(typeof(NavMeshAgent))]
public class PathDebugDraw : MonoBehaviour
{
    public NavMeshLink jumpLink;      // bağlantı parçasını tanımak için
    public float sampleStep = 0.25f;  // maliyeti kaç metrede bir örnekleyelim

    private NavMeshAgent agent;
    private float length, cost;
    private int cornerCount;

    private void Awake() => agent = GetComponent<NavMeshAgent>();

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh || agent.pathPending || !agent.hasPath) return;

        Vector3[] c = agent.path.corners;
        cornerCount = c.Length;
        length = 0f;
        cost = 0f;

        for (int i = 0; i < c.Length - 1; i++)
        {
            float segLen = Vector3.Distance(c[i], c[i + 1]);
            length += segLen;
            cost += SegmentCost(c[i], c[i + 1], segLen);
        }
    }

    private float SegmentCost(Vector3 a, Vector3 b, float segLen)
    {
        // 1. Bu parça atlama bağlantısı mı? O zaman uzunluk × bağlantının alan maliyeti
        if (IsLink(a, b))
            return segLen * agent.GetAreaCost(jumpLink.area);

        // 2. Değilse parçayı küçük adımlara böl, her adımın ortasındaki alana bak
        int steps = Mathf.Max(1, Mathf.CeilToInt(segLen / sampleStep));
        float stepLen = segLen / steps;
        float total = 0f;

        for (int s = 0; s < steps; s++)
        {
            Vector3 p = Vector3.Lerp(a, b, (s + 0.5f) / steps);
            int area = 0;   // bulunamazsa Walkable say
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                area = AreaIndex(hit.mask);
            total += stepLen * agent.GetAreaCost(area);
        }
        return total;
    }

    private bool IsLink(Vector3 a, Vector3 b)
    {
        if (jumpLink == null || !jumpLink.activated) return false;
        Vector3 s = jumpLink.transform.TransformPoint(jumpLink.startPoint);
        Vector3 e = jumpLink.transform.TransformPoint(jumpLink.endPoint);
        return (Near(a, s) && Near(b, e)) || (Near(a, e) && Near(b, s));
    }

    private static bool Near(Vector3 p, Vector3 q)
    {
        p.y = 0f;
        q.y = 0f;   // yalnızca yatay düzlemde karşılaştır
        return Vector3.Distance(p, q) < 0.3f;
    }

    public static int AreaIndex(int mask)
    {
        // hit.mask bir bit maskesidir: alan 3 ise yalnızca 3. bit (8) doludur
        for (int i = 0; i < 32; i++)
            if ((mask & (1 << i)) != 0) return i;
        return 0;
    }

    private void OnGUI()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        GUI.Label(new Rect(10, 10, 500, 25), $"Köşe: {cornerCount}   Uzunluk: {length:F2} m   Maliyet: {cost:F2}");
        GUI.Label(new Rect(10, 30, 500, 25), $"Durum: {agent.pathStatus}");
    }

    private void OnDrawGizmos()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.hasPath) return;
        Gizmos.color = Color.yellow;
        foreach (Vector3 p in agent.path.corners)
            Gizmos.DrawSphere(p, 0.2f);
    }
}
