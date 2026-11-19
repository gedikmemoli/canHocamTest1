// Dosya: Assets/Scripts/Guard/Guard.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Guard : MonoBehaviour
{
    [Header("Sahne")]
    public Transform player;
    public Transform[] patrolPoints;
    public SlidingDoor door;

    [Header("Hız (m/sn)")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Görüş (3. hafta)")]
    public float viewDistance = 10f;
    public float viewAngle = 120f;

    [Header("Süreler ve mesafeler")]
    public float loseTime = 1f;            // bu kadar süre görmezse Ara'ya geçer
    public float searchTime = 5f;          // son görülen noktada bakınma süresi
    public float doorStopDistance = 2.5f;  // kapalı kapıya bu kadar yaklaşınca bekler

    [Header("Hafta 7 Ödevi: Sesi Duyan Bekçi (Araştır)")]
    public float hearingDistance = 8f;     // 8 m'den yakınsa duyar, duvarlar sesi kesmez
    public float investigateTime = 3f;     // sesin geldiği noktada 3 sn bakınır

    // Durum nesneleri: her bekçinin kendi kopyası (6.5) + 5. durum Araştır (10. Ödev)
    public readonly GuardPatrol Patrol = new();
    public readonly GuardChase Chase = new();
    public readonly GuardSearch Search = new();
    public readonly GuardWaitAtDoor WaitAtDoor = new();
    public readonly GuardInvestigate Investigate = new();

    public StateMachine<Guard> FSM { get; private set; }
    public NavMeshAgent Agent { get; private set; }

    [HideInInspector] public int patrolIndex;
    [HideInInspector] public Vector3 lastSeenPosition;
    [HideInInspector] public float lastSeenTime;
    [HideInInspector] public Vector3 lastHeardPosition;

    private Renderer rend;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Agent.speed = patrolSpeed;
        rend = GetComponent<Renderer>();

        FSM = new StateMachine<Guard>(this);
        FSM.Global = new GuardGlobal();
    }

    private void Start()
    {
        if (player == null || patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("Guard: Player ve en az bir devriye noktası atanmalı.");
            enabled = false;
            return;
        }
        FSM.ChangeState(Patrol);
    }

    private void Update()
    {
        if (!Agent.isOnNavMesh) return;
        FSM.Update();
    }

    // Görüş: mesafe, açı, engel (3. hafta)
    public bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 eye = transform.position;
        Vector3 toPlayer = player.position + Vector3.up * 0.5f - eye;

        if (toPlayer.magnitude > viewDistance) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle / 2f) return false;

        return Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, viewDistance)
               && hit.transform.IsChildOf(player);
    }

    // Kapı kapalı ve bekçi kapıya yakın mı? Yükseklik farkı sayılmaz.
    public bool DoorBlocksMe()
    {
        if (door == null || door.IsOpen) return false;
        Vector3 d = door.transform.position - transform.position;
        d.y = 0f;
        return d.magnitude < doorStopDistance;
    }

    // NavMeshAgent hedefe vardı mı?
    public bool Arrived()
    {
        return !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + 0.1f;
    }

    // Hafta 7 Ödevi (Bölüm 10): Sesi Duyan Bekçi
    public void HearNoise(Vector3 position)
    {
        // Kovala durumundayken sesi görmezden gelir; oyuncuyu zaten görüyordur.
        if (FSM == null || FSM.IsIn(Chase)) return;

        // Bekçi sese 8 m'den yakınsa duyar. Duvarlar sesi kesmez.
        float dist = Vector3.Distance(transform.position, position);
        if (dist > hearingDistance)
        {
            Debug.Log($"[Guard] Ses duyulamadı (Mesafe: {dist:F1} m > {hearingDistance} m).");
            return;
        }

        // Bekçi Devriye ya da Ara durumundayken (veya zaten Araştır'dayken yeni ses gelirse) Araştır'a geçer.
        if (FSM.IsIn(Patrol) || FSM.IsIn(Search) || FSM.IsIn(Investigate))
        {
            lastHeardPosition = position;
            if (FSM.IsIn(Investigate))
            {
                // Zaten Araştır'daysa hedefi ve 3 sn sayacını yenile
                Investigate.Enter(this);
            }
            else
            {
                FSM.ChangeState(Investigate);
            }
        }
    }

    public void SetColor(Color c)
    {
        if (rend != null)
            rend.material.color = c;
    }

    // Son görülen ve duyulan noktayı Scene penceresinde göster
    private void OnDrawGizmos()
    {
        if (FSM == null) return;
        if (FSM.IsIn(Chase) || FSM.IsIn(Search))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSeenPosition, 0.4f);
        }
        else if (FSM.IsIn(Investigate))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastHeardPosition, 0.5f);
        }
    }
}
