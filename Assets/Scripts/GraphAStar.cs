// Dosya: Assets/Scripts/GraphAStar.cs
using System.Collections.Generic;
using UnityEngine;

public class GraphAStar
{
    // Bir düğümün kaydı: waypoint, g, h, ebeveyn
    private class Node
    {
        public Waypoint wp;                                 // [YENİ] hücre yerine waypoint
        public float g, h;
        public Node parent;
    }

    public bool useHeuristic = true;                        // false: Dijkstra (h = 0)
    public int Expanded { get; private set; }               // kaç düğüm genişletildi

    public List<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        var open = new List<Node>();                         // açık liste
        var openByWp = new Dictionary<Waypoint, Node>();     // "bu düğüm açıkta mı?"
        var closed = new HashSet<Waypoint>();                // kapalı liste
        Expanded = 0;

        var first = new Node { wp = start, g = 0f, h = H(start, goal) };
        open.Add(first);
        openByWp[start] = first;

        while (open.Count > 0)
        {
            // 1. Açık listeden en küçük f'li düğümü seç; eşitlikte h'si küçük olanı
            Node current = open[0];
            foreach (Node n in open)
                if (F(n) < F(current) || (F(n) == F(current) && n.h < current.h))
                    current = n;

            // 2. Onu kapalı listeye taşı
            open.Remove(current);
            openByWp.Remove(current.wp);
            closed.Add(current.wp);
            Expanded++;

            // 3. Hedefse yolu ebeveynlerden geri izle
            if (current.wp == goal) return Retrace(current);

            // 4. [YENİ] Komşular: dört yön değil, düğümün kendi listesi
            foreach (Waypoint next in current.wp.neighbors)
            {
                if (next == null || closed.Contains(next)) continue;

                // [YENİ] Kenar maliyeti: iki nokta arasındaki uzaklık
                float newG = current.g + Vector3.Distance(current.wp.transform.position,
                                                          next.transform.position);

                if (!openByWp.TryGetValue(next, out Node node))
                {
                    // ilk kez görülüyor: açığa ekle
                    node = new Node { wp = next, g = newG, h = H(next, goal), parent = current };
                    open.Add(node);
                    openByWp[next] = node;
                }
                else if (newG < node.g)
                {
                    // daha ucuz yol: güncelle
                    node.g = newG;
                    node.parent = current;
                }
            }
        }
        return null;                                         // yol yok
    }

    private float F(Node n) => n.g + n.h;

    // [YENİ] Sezgisel: Öklid uzaklığı
    private float H(Waypoint a, Waypoint b) =>
        useHeuristic ? Vector3.Distance(a.transform.position, b.transform.position) : 0f;

    private List<Waypoint> Retrace(Node end)
    {
        var path = new List<Waypoint>();
        for (Node n = end; n != null; n = n.parent) path.Add(n.wp);
        path.Reverse();                                      // hedef→başlangıç dizisini çevir
        return path;
    }
}
