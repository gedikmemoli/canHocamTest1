// Dosya: Assets/Scripts/Guard/GuardStates.cs
using UnityEngine;

// Global durum: her karede, mevcut durumdan ÖNCE çalışır (6.2)
public class GuardGlobal : State<Guard>
{
    public override void Execute(Guard g)
    {
        if (!g.CanSeePlayer()) return;

        g.lastSeenPosition = g.player.position;
        g.lastSeenTime = Time.time;
        if (!g.FSM.IsIn(g.Chase)) g.FSM.ChangeState(g.Chase);
    }
}

public class GuardPatrol : State<Guard>
{
    public override string Name => "Devriye";

    public override void Enter(Guard g)
    {
        g.SetColor(Color.green);
        g.Agent.SetDestination(g.patrolPoints[g.patrolIndex].position);
    }

    public override void Execute(Guard g)
    {
        if (g.DoorBlocksMe()) { g.FSM.ChangeState(g.WaitAtDoor); return; }

        if (g.Arrived())   // noktaya vardı: durum değişmez, sıradaki nokta (3.2)
        {
            g.patrolIndex = (g.patrolIndex + 1) % g.patrolPoints.Length;
            g.Agent.SetDestination(g.patrolPoints[g.patrolIndex].position);
        }
    }
}

public class GuardChase : State<Guard>
{
    public override string Name => "Kovala";

    public override void Enter(Guard g)
    {
        g.SetColor(Color.red);
        g.Agent.speed = g.chaseSpeed;
    }

    public override void Execute(Guard g)
    {
        if (Time.time - g.lastSeenTime > g.loseTime) { g.FSM.ChangeState(g.Search); return; }

        g.Agent.SetDestination(g.lastSeenPosition);
    }

    public override void Exit(Guard g)
    {
        g.Agent.speed = g.patrolSpeed;   // Enter'da değişen ayarı geri al
    }
}

public class GuardSearch : State<Guard>
{
    private float lookTimer;   // bu bekçiye ait durum nesnesi olduğu için veri tutabilir (6.5)

    public override string Name => "Ara";

    public override void Enter(Guard g)
    {
        g.SetColor(Color.yellow);
        g.Agent.SetDestination(g.lastSeenPosition);
        lookTimer = g.searchTime;   // her girişte süre sıfırdan başlar (3.3)
    }

    public override void Execute(Guard g)
    {
        if (g.DoorBlocksMe()) { g.FSM.ChangeState(g.WaitAtDoor); return; }
        if (!g.Arrived()) return;                              // henüz varmadı

        g.transform.Rotate(0f, 90f * Time.deltaTime, 0f);      // dönerek bakın
        lookTimer -= Time.deltaTime;
        if (lookTimer <= 0f) g.FSM.ChangeState(g.Patrol);
    }
}

public class GuardWaitAtDoor : State<Guard>
{
    public override string Name => "KapıdaBekle";

    public override void Enter(Guard g)
    {
        g.SetColor(Color.gray);
        g.Agent.isStopped = true;
    }

    public override void Execute(Guard g)
    {
        if (g.door != null && g.door.IsOpen) g.FSM.RevertToPreviousState();   // Devriye, Ara ya da Araştır (6.3)
    }

    public override void Exit(Guard g)
    {
        g.Agent.isStopped = false;   // bu satır olmazsa bekçi donar (5.2)
    }
}

// Hafta 7 Ödevi (Bölüm 10): 5. Durum -> Araştır (Sesi Duyan Bekçi)
// Renk: Mor/Eflatun (diğer dört rengin hiçbiriyle aynı değil)
public class GuardInvestigate : State<Guard>
{
    private float investigateTimer;

    public override string Name => "Araştır";

    public override void Enter(Guard g)
    {
        g.SetColor(new Color(0.75f, 0.15f, 0.95f)); // Mor / Magenta
        g.Agent.SetDestination(g.lastHeardPosition);
        investigateTimer = g.investigateTime;       // 3 sn bakınma sayacı
    }

    public override void Execute(Guard g)
    {
        if (g.DoorBlocksMe()) { g.FSM.ChangeState(g.WaitAtDoor); return; }
        if (!g.Arrived()) return;

        g.transform.Rotate(0f, 90f * Time.deltaTime, 0f);      // ses noktasında dönerek bakın
        investigateTimer -= Time.deltaTime;
        if (investigateTimer <= 0f) g.FSM.ChangeState(g.Patrol);
    }

    public override void Exit(Guard g)
    {
        // Özel bir hız veya durdurma değişikliği yapılmadığı için ek sıfırlama gerekmez;
        // ancak varsa duraklama bayrağını güvenceye alırız.
        g.Agent.isStopped = false;
    }
}
