using UnityEngine;
using UnityEngine.AI;

public class NavPathDebug : MonoBehaviour
{
    public NavMeshAgent navAgent;
    public int cornerCount;               // Inspector'da canlı izlenir

    private void Update()
    {
        if (navAgent == null || !navAgent.hasPath) { cornerCount = 0; return; }

        Vector3[] c = navAgent.path.corners;
        cornerCount = c.Length;
        for (int i = 0; i < c.Length - 1; i++)
            Debug.DrawLine(c[i] + Vector3.up * 0.3f, c[i + 1] + Vector3.up * 0.3f, Color.cyan);
    }
}
