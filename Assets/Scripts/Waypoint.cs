// Dosya: Assets/Scripts/Waypoint.cs
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public List<Waypoint> neighbors = new();   // komşuluk listesi

    private void OnDrawGizmos()
    {
        // Düğüm: camgöbeği küre
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Kenarlar: beyaz çizgi
        Gizmos.color = Color.white;
        foreach (Waypoint n in neighbors)
            if (n != null) Gizmos.DrawLine(transform.position, n.transform.position);
    }
}
