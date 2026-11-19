// Dosya: Assets/Scripts/NpcNavChase.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(LineRenderer))]
public class NpcNavChase : MonoBehaviour
{
    public Transform target;              // kovalanacak karakter
    public float repathInterval = 0.25f;  // hedefi kaç saniyede bir güncelle
    public float baseSpeed = 3.5f;        // normal hız
    public bool slowDownInMud = true;     // Hafta 6 Ek Görev: Çamurda gerçekten yavaşla

    private NavMeshAgent agent;
    private LineRenderer line;
    private float timer;
    private int mudArea = -1;
    private int shallowWaterArea = -1;
    private NavMeshPathStatus lastStatus = NavMeshPathStatus.PathComplete;   // [YENİ]

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        line = GetComponent<LineRenderer>();
        mudArea = NavMesh.GetAreaFromName("Camur");
        shallowWaterArea = NavMesh.GetAreaFromName("SigSu");
        if (agent != null && baseSpeed <= 0f) baseSpeed = agent.speed;
    }

    private void Update()
    {
        if (target == null || agent == null || !agent.isOnNavMesh) return;

        // 1. Hedef hareket ediyor: varış noktasını düzenli aralıklarla yenile
        timer -= Time.deltaTime;
        if (timer <= 0f && !agent.isOnOffMeshLink)   // [YENİ] atlarken yeni yol isteme
        {
            agent.SetDestination(target.position);
            timer = repathInterval;
        }

        // Ek görev (Hafta 6): Ajanın altındaki alanı sorgula, Camur veya SigSu ise hızı düşür
        if (slowDownInMud && !agent.isOnOffMeshLink)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.6f, NavMesh.AllAreas))
            {
                int areaIdx = PathDebugDraw.AreaIndex(hit.mask);
                if (areaIdx == mudArea && mudArea >= 0)
                    agent.speed = baseSpeed * 0.5f;
                else if (areaIdx == shallowWaterArea && shallowWaterArea >= 0)
                    agent.speed = baseSpeed * 0.65f;
                else
                    agent.speed = baseSpeed;
            }
        }

        // 2. [YENİ] Yolun durumu değişince konsola yaz
        if (!agent.pathPending && agent.pathStatus != lastStatus)
        {
            lastStatus = agent.pathStatus;
            Debug.Log($"Yol durumu: {lastStatus}");
        }

        // 3. Yolu göster: NavMesh'in bulduğu köşe noktaları
        Vector3[] corners = agent.path.corners;
        line.positionCount = corners.Length;
        for (int i = 0; i < corners.Length; i++)
            line.SetPosition(i, corners[i] + Vector3.up * 0.2f);
    }

    private void OnDisable()
    {
        if (line != null) line.positionCount = 0;
    }
}
