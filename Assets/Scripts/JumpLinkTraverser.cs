// Dosya: Assets/Scripts/JumpLinkTraverser.cs
using UnityEngine;
using UnityEngine.AI;

// Bağlantıya gelince NPC'yi parabol çizerek karşıya atlatır
[RequireComponent(typeof(NavMeshAgent))]
public class JumpLinkTraverser : MonoBehaviour
{
    public float arcHeight = 1.5f;   // h: tepe yüksekliği (m)
    public float duration = 0.6f;    // atlayış süresi (s)

    private NavMeshAgent agent;
    private bool jumping;
    private float t;
    private Vector3 a, b;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;   // geçişi biz yapacağız
    }

    private void Update()
    {
        // 1. Bağlantının başına geldik: atlayışı hazırla
        if (agent.isOnOffMeshLink && !jumping)
        {
            OffMeshLinkData data = agent.currentOffMeshLinkData;
            a = transform.position;
            b = data.endPos + Vector3.up * agent.baseOffset;   // kürenin merkezi zeminin 0,5 m üstünde
            t = 0f;
            jumping = true;
        }

        if (!jumping) return;

        // 2. Parabol: düz çizgi + 4h·t(1−t) kadar yukarı
        t += Time.deltaTime / duration;
        float k = Mathf.Clamp01(t);
        transform.position = Vector3.Lerp(a, b, k) + Vector3.up * (4f * arcHeight * k * (1f - k));

        // 3. Vardık: ajana bağlantının bittiğini söyle
        if (t >= 1f)
        {
            agent.CompleteOffMeshLink();
            jumping = false;
        }
    }
}
