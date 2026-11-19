using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Hafta 3, Hafta 4, Hafta 5, Hafta 6 ve Hafta 7'nin tüm ders içi uygulamalarını ve ödevlerini
/// tek bir projede aşama aşama gösteren, hem arayüzden (Game View HUD) hem de F1-F5 tuşlarıyla
/// anında aşamalar arası geçiş sağlayan merkezi yönetim bileşeni.
/// </summary>
public class HaftaYonetici : MonoBehaviour
{
    public enum AktifHafta
    {
        Hafta3_AraziHareketGozcu = 3,
        Hafta4_LabirentVeAStar = 4,
        Hafta5_WaypointVeGraflar = 5,
        Hafta6_NavMeshMaliyetVeLink = 6,
        Hafta7_SonluDurumMakineleri = 7
    }

    [Header("Aktif Aşama / Hafta Seçimi")]
    public AktifHafta seciliHafta = AktifHafta.Hafta3_AraziHareketGozcu;

    [Header("Aşama Kök Nesneleri (Stages)")]
    public GameObject hafta3Root;
    public GameObject hafta4Root;
    public GameObject hafta567ArenaRoot;

    [Header("Hafta 3 Referansları")]
    public GameObject oyuncuCC_H3;
    public GameObject oyuncuRB_H3;
    public Gozcu gozcu_H3;
    public AraziSondasi araziSondasi_H3;
    public bool h3ActiveCharacterIsRB = false;

    [Header("Hafta 4 Referansları")]
    public MazeGenerator maze_H4;
    public TerrainGrid terrainGrid_H4;
    public GridPathfinder pathfinder_H4;
    public PathFollower follower_H4;
    public PathLine pathLine_H4;
    public ClickToGoal clickToGoal_H4;
    public NavMeshAgent navAgent_H4;
    public NavMeshRefresher navRefresher_H4;
    public bool h4DagYoluOdeviModu = false; // Maze yerine TerrainGrid (Dağ Yolu ödevi)

    [Header("Hafta 5-6-7 (20x20 Arena) Referansları")]
    public Transform arenaPlayer;
    public GameObject waypointsRoot_H5;
    public WaypointGraph waypointGraph_H5;
    public GameObject[] cornerWaypoints_H5; // K1, K2, K3, K4
    public GameObject camurVeLinkRoot_H6;
    public SlidingDoor slidingDoor_H67;
    public GameObject patrolPointsRoot_H7;

    [Header("Arena NPC (Kırmızı Küre / Bekçi) Bileşenleri")]
    public GameObject arenaNpcGO;
    public NpcWaypointGuard npcWaypointGuard_H5;
    public NpcNavChase npcNavChase_H6;
    public JumpLinkTraverser jumpTraverser_H6;
    public AreaCostToggle areaCostToggle_H6;
    public PathDebugDraw pathDebugDraw_H6;
    public Guard guardFsm_H7;
    public PlayerNoiseMaker playerNoiseMaker_H7;

    [Header("Kamera Referansları")]
    public Camera mainCamera;
    public TakipKamerasi takipKamerasi_H3;
    public CameraModes cameraModes_H4;

    private bool showHud = true;
    private string lastExperimentOutput = "";

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        HaftayiUygula(seciliHafta);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame) HaftayiUygula(AktifHafta.Hafta3_AraziHareketGozcu);
        if (Keyboard.current.f2Key.wasPressedThisFrame) HaftayiUygula(AktifHafta.Hafta4_LabirentVeAStar);
        if (Keyboard.current.f3Key.wasPressedThisFrame) HaftayiUygula(AktifHafta.Hafta5_WaypointVeGraflar);
        if (Keyboard.current.f4Key.wasPressedThisFrame) HaftayiUygula(AktifHafta.Hafta6_NavMeshMaliyetVeLink);
        if (Keyboard.current.f5Key.wasPressedThisFrame) HaftayiUygula(AktifHafta.Hafta7_SonluDurumMakineleri);

        if (Keyboard.current.hKey.wasPressedThisFrame) showHud = !showHud;

        // Hafta 3'te Tab tuşuyla Oyuncu_CC ve Oyuncu_RB arasında geçiş
        if (seciliHafta == AktifHafta.Hafta3_AraziHareketGozcu && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            Hafta3KarakterDegistir();
        }
    }

    public void HaftayiUygula(AktifHafta yeniHafta)
    {
        seciliHafta = yeniHafta;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Kök nesneleri aç/kapat
        if (hafta3Root != null) hafta3Root.SetActive(yeniHafta == AktifHafta.Hafta3_AraziHareketGozcu);
        if (hafta4Root != null) hafta4Root.SetActive(yeniHafta == AktifHafta.Hafta4_LabirentVeAStar);

        bool isArenaWeek = (yeniHafta == AktifHafta.Hafta5_WaypointVeGraflar ||
                            yeniHafta == AktifHafta.Hafta6_NavMeshMaliyetVeLink ||
                            yeniHafta == AktifHafta.Hafta7_SonluDurumMakineleri);
        if (hafta567ArenaRoot != null) hafta567ArenaRoot.SetActive(isArenaWeek);

        // Kamera modlarını sıfırla
        if (takipKamerasi_H3 != null) takipKamerasi_H3.enabled = false;
        if (cameraModes_H4 != null) cameraModes_H4.enabled = false;

        switch (yeniHafta)
        {
            case AktifHafta.Hafta3_AraziHareketGozcu:
                KurHafta3();
                break;
            case AktifHafta.Hafta4_LabirentVeAStar:
                KurHafta4();
                break;
            case AktifHafta.Hafta5_WaypointVeGraflar:
                KurHafta5();
                break;
            case AktifHafta.Hafta6_NavMeshMaliyetVeLink:
                KurHafta6();
                break;
            case AktifHafta.Hafta7_SonluDurumMakineleri:
                KurHafta7();
                break;
        }

        Debug.Log($"<color=cyan><b>[HaftaYonetici]</b></color> Aktif Aşama: <b>{yeniHafta}</b>");
    }

    private void KurHafta3()
    {
        if (oyuncuCC_H3 != null) oyuncuCC_H3.SetActive(!h3ActiveCharacterIsRB);
        if (oyuncuRB_H3 != null) oyuncuRB_H3.SetActive(h3ActiveCharacterIsRB);

        Transform activePlayer = h3ActiveCharacterIsRB
            ? (oyuncuRB_H3 != null ? oyuncuRB_H3.transform : null)
            : (oyuncuCC_H3 != null ? oyuncuCC_H3.transform : null);

        if (gozcu_H3 != null && activePlayer != null)
            gozcu_H3.hedefOyuncu = activePlayer;

        if (takipKamerasi_H3 != null && activePlayer != null)
        {
            takipKamerasi_H3.enabled = true;
            takipKamerasi_H3.HedefAyarla(activePlayer);
        }
    }

    public void Hafta3KarakterDegistir()
    {
        h3ActiveCharacterIsRB = !h3ActiveCharacterIsRB;
        KurHafta3();
    }

    private void KurHafta4()
    {
        if (cameraModes_H4 != null)
        {
            cameraModes_H4.enabled = true;
            cameraModes_H4.top = true;
        }

        if (navRefresher_H4 != null && !h4DagYoluOdeviModu)
        {
            navRefresher_H4.Rebuild();
        }
    }

    public void Hafta4GridModuDegistir(bool dagYoluModu)
    {
        h4DagYoluOdeviModu = dagYoluModu;
        MonoBehaviour activeGrid = dagYoluModu ? (MonoBehaviour)terrainGrid_H4 : maze_H4;

        if (maze_H4 != null) maze_H4.gameObject.SetActive(!dagYoluModu);
        if (navAgent_H4 != null) navAgent_H4.gameObject.SetActive(!dagYoluModu);

        if (pathfinder_H4 != null)
        {
            pathfinder_H4.Clear();
            pathfinder_H4.gridSource = activeGrid;
        }
        if (pathLine_H4 != null)
        {
            pathLine_H4.Hide();
            pathLine_H4.gridSource = activeGrid;
        }
        if (follower_H4 != null)
        {
            follower_H4.gridSource = activeGrid;
            follower_H4.adjustSpeedBySlope = dagYoluModu;
            if (activeGrid is ISearchGrid sg) follower_H4.Teleport(sg.StartCell);
        }
        if (clickToGoal_H4 != null)
        {
            clickToGoal_H4.customGridSource = activeGrid;
        }
    }

    private void KurHafta5()
    {
        KameraArenayaBak();
        ArenaPozisyonlariniSifirla();

        if (waypointsRoot_H5 != null) waypointsRoot_H5.SetActive(true);
        if (camurVeLinkRoot_H6 != null) camurVeLinkRoot_H6.SetActive(false);
        if (patrolPointsRoot_H7 != null) patrolPointsRoot_H7.SetActive(false);

        // Hafta 5: NavMeshAgent, NpcNavChase, Guard kapalı; NpcWaypointGuard açık
        NavMeshAgent ag = arenaNpcGO != null ? arenaNpcGO.GetComponent<NavMeshAgent>() : null;
        if (ag != null) ag.enabled = false;
        if (npcNavChase_H6 != null) npcNavChase_H6.enabled = false;
        if (jumpTraverser_H6 != null) jumpTraverser_H6.enabled = false;
        if (areaCostToggle_H6 != null) areaCostToggle_H6.enabled = false;
        if (pathDebugDraw_H6 != null) pathDebugDraw_H6.enabled = false;
        if (guardFsm_H7 != null) guardFsm_H7.enabled = false;
        if (playerNoiseMaker_H7 != null) playerNoiseMaker_H7.enabled = false;

        if (arenaNpcGO != null)
        {
            Renderer r = arenaNpcGO.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.red;
        }

        if (waypointGraph_H5 != null) waypointGraph_H5.BuildEdges();
        if (npcWaypointGuard_H5 != null) npcWaypointGuard_H5.enabled = true;
    }

    private void KurHafta6()
    {
        KameraArenayaBak();
        ArenaPozisyonlariniSifirla();

        if (waypointsRoot_H5 != null) waypointsRoot_H5.SetActive(false);
        if (camurVeLinkRoot_H6 != null) camurVeLinkRoot_H6.SetActive(true);
        if (patrolPointsRoot_H7 != null) patrolPointsRoot_H7.SetActive(false);

        if (npcWaypointGuard_H5 != null) npcWaypointGuard_H5.enabled = false;
        if (guardFsm_H7 != null) guardFsm_H7.enabled = false;
        if (playerNoiseMaker_H7 != null) playerNoiseMaker_H7.enabled = false;

        NavMeshAgent ag = arenaNpcGO != null ? arenaNpcGO.GetComponent<NavMeshAgent>() : null;
        if (ag != null)
        {
            ag.enabled = true;
            ag.isStopped = false;
            ag.speed = 3.5f;
        }

        if (arenaNpcGO != null)
        {
            Renderer r = arenaNpcGO.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.red;
        }

        if (npcNavChase_H6 != null) npcNavChase_H6.enabled = true;
        if (jumpTraverser_H6 != null) jumpTraverser_H6.enabled = true;
        if (areaCostToggle_H6 != null) areaCostToggle_H6.enabled = true;
        if (pathDebugDraw_H6 != null) pathDebugDraw_H6.enabled = true;
    }

    private void KurHafta7()
    {
        KameraArenayaBak();
        ArenaPozisyonlariniSifirla();

        if (waypointsRoot_H5 != null) waypointsRoot_H5.SetActive(false);
        if (camurVeLinkRoot_H6 != null) camurVeLinkRoot_H6.SetActive(true);
        if (patrolPointsRoot_H7 != null) patrolPointsRoot_H7.SetActive(true);

        if (npcWaypointGuard_H5 != null) npcWaypointGuard_H5.enabled = false;
        if (npcNavChase_H6 != null) npcNavChase_H6.enabled = false;
        if (areaCostToggle_H6 != null) areaCostToggle_H6.enabled = false;
        if (pathDebugDraw_H6 != null) pathDebugDraw_H6.enabled = false;
        if (jumpTraverser_H6 != null) jumpTraverser_H6.enabled = true;

        NavMeshAgent ag = arenaNpcGO != null ? arenaNpcGO.GetComponent<NavMeshAgent>() : null;
        if (ag != null)
        {
            ag.enabled = true;
            ag.isStopped = false;
            ag.speed = 2f;
        }

        if (playerNoiseMaker_H7 != null) playerNoiseMaker_H7.enabled = true;
        if (guardFsm_H7 != null)
        {
            guardFsm_H7.enabled = true;
            if (guardFsm_H7.FSM != null)
                guardFsm_H7.FSM.ChangeState(guardFsm_H7.Patrol);
        }
    }

    private void ArenaPozisyonlariniSifirla()
    {
        if (hafta567ArenaRoot == null) return;
        Vector3 origin = hafta567ArenaRoot.transform.position;

        if (arenaNpcGO != null)
        {
            NavMeshAgent ag = arenaNpcGO.GetComponent<NavMeshAgent>();
            bool wasEnabled = ag != null && ag.enabled;
            if (wasEnabled) ag.enabled = false;
            arenaNpcGO.transform.position = origin + new Vector3(6.5f, 0.5f, 13.5f);
            arenaNpcGO.transform.rotation = Quaternion.identity;
            if (wasEnabled)
            {
                ag.enabled = true;
                ag.Warp(origin + new Vector3(6.5f, 0.5f, 13.5f));
            }
        }

        if (arenaPlayer != null)
        {
            arenaPlayer.position = origin + new Vector3(17.5f, 0.5f, 9.5f);
        }
    }

    private void KameraArenayaBak()
    {
        if (mainCamera == null || hafta567ArenaRoot == null) return;
        Vector3 arenaCenter = hafta567ArenaRoot.transform.position + new Vector3(10f, 0f, 10f);
        mainCamera.transform.position = arenaCenter + new Vector3(0f, 23f, -6f);
        mainCamera.transform.rotation = Quaternion.LookRotation(arenaCenter - mainCamera.transform.position);
    }

    private void OnGUI()
    {
        // Sağ üstte her zaman görünür Hafta Seçici Çubuğu
        GUILayout.BeginArea(new Rect(Screen.width - 430, 10, 420, showHud ? 460 : 45), GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("<b><size=13>📚 Ders & Ödev Aşamaları (F1-F5)</size></b>");
        if (GUILayout.Button(showHud ? "Gizle [H]" : "Göster [H]", GUILayout.Width(85)))
            showHud = !showHud;
        GUILayout.EndHorizontal();

        if (!showHud)
        {
            GUILayout.EndArea();
            return;
        }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        SecimButonu("Hafta 3 [F1]", AktifHafta.Hafta3_AraziHareketGozcu);
        SecimButonu("Hafta 4 [F2]", AktifHafta.Hafta4_LabirentVeAStar);
        SecimButonu("Hafta 5 [F3]", AktifHafta.Hafta5_WaypointVeGraflar);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        SecimButonu("Hafta 6 [F4]", AktifHafta.Hafta6_NavMeshMaliyetVeLink);
        SecimButonu("Hafta 7 [F5]", AktifHafta.Hafta7_SonluDurumMakineleri);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        CizAktifHaftaDetaylari();

        GUILayout.EndArea();
    }

    private void SecimButonu(string etiket, AktifHafta hafta)
    {
        Color old = GUI.backgroundColor;
        if (seciliHafta == hafta) GUI.backgroundColor = Color.green;
        if (GUILayout.Button(etiket, GUILayout.Height(28)))
        {
            HaftayiUygula(hafta);
        }
        GUI.backgroundColor = old;
    }

    private void CizAktifHaftaDetaylari()
    {
        switch (seciliHafta)
        {
            case AktifHafta.Hafta3_AraziHareketGozcu:
                GUILayout.Label("<b><color=#77ddff>HAFTA 3 — Arazi, Girdi, Hareket & Gözcü Ödevi</color></b>");
                GUILayout.Label("• <b>WASD + Fare + Space + Shift:</b> Kameraya göre hareket & zıplama");
                GUILayout.Label($"• <b>Aktif Karakter [Tab]:</b> {(h3ActiveCharacterIsRB ? "Oyuncu_RB (Rigidbody)" : "Oyuncu_CC (CharacterController)")}");
                if (GUILayout.Button("Karakter Değiştir: Oyuncu_CC ↔ Oyuncu_RB [Tab]"))
                    Hafta3KarakterDegistir();

                if (gozcu_H3 != null)
                {
                    GUILayout.Space(4);
                    GUILayout.Label($"• <b>Gözcü Durumu:</b> {(gozcu_H3.OyuncuGoruluyorMu ? "<color=red>GÖRÜYOR (Kırmızı)</color>" : "<color=lime>GÖRMÜYOR / TARAMA (Yeşil)</color>")}");
                    GUILayout.Label("• <b>Ek Görevler (+10):</b> ±60° Tarama, Dikey Açı Sınırı, 2 sn Takip");
                }
                break;

            case AktifHafta.Hafta4_LabirentVeAStar:
                GUILayout.Label("<b><color=#77ddff>HAFTA 4 — Arazide Labirent, NavMesh vs A* & Dağ Yolu</color></b>");
                GUILayout.Label("• <b>Sol Tık:</b> Hedef seç (Mavi NavAgent + Sarı A* Karakteri yarışır)");
                GUILayout.Label("• <b>Sağ Tık:</b> Duvar Ekle / Kaldır (Replanner & UpdateNavMesh)");
                GUILayout.Label("• <b>C Tuşu:</b> Tepe Kamera ↔ Takip Kamerası | <b>R Tuşu:</b> Yeni Labirent");
                GUILayout.Label("• <b>1/2/3/4 Tuşları:</b> DFS / BFS / Dijkstra / A* | <b>Space:</b> Başlangıç→Hedef");

                if (pathfinder_H4 != null)
                {
                    GUILayout.Label($"• <b>Algoritma:</b> {pathfinder_H4.algorithm} | <b>Genişletilen:</b> {pathfinder_H4.expandedCount} | <b>Maliyet:</b> {pathfinder_H4.totalCost:F1}");
                }

                if (GUILayout.Button(h4DagYoluOdeviModu ? "Mod: Dağ Yolu Ödevi (TerrainGrid) → Labirente Dön" : "Mod: Labirent (MazeGenerator) → Dağ Yolu Ödevine Geç"))
                {
                    Hafta4GridModuDegistir(!h4DagYoluOdeviModu);
                }
                break;

            case AktifHafta.Hafta5_WaypointVeGraflar:
                GUILayout.Label("<b><color=#77ddff>HAFTA 5 — Waypoint'ler, Graflar & Köşe Noktaları Ödevi</color></b>");
                GUILayout.Label("• <b>WASD:</b> Mavi oyuncuyu 20x20 arenada yürüt");
                GUILayout.Label("• <b>Kırmızı NPC:</b> W0..W7 devriye gezer, görünce GraphAStar ile kovalar");

                if (npcWaypointGuard_H5 != null)
                {
                    bool newPingPong = GUILayout.Toggle(npcWaypointGuard_H5.pingPong, " Ödev Görev 3: Gidip Gelme (Ping-Pong) Modu");
                    if (newPingPong != npcWaypointGuard_H5.pingPong) npcWaypointGuard_H5.pingPong = newPingPong;

                    bool newSmooth = GUILayout.Toggle(npcWaypointGuard_H5.usePathSmoothing, " Ek Görev (+10): Yol Yumuşatma (Path Smoothing)");
                    if (newSmooth != npcWaypointGuard_H5.usePathSmoothing) npcWaypointGuard_H5.usePathSmoothing = newSmooth;
                }

                if (cornerWaypoints_H5 != null && cornerWaypoints_H5.Length > 0 && cornerWaypoints_H5[0] != null)
                {
                    bool cornersActive = cornerWaypoints_H5[0].activeSelf;
                    if (GUILayout.Button(cornersActive ? "Ödev Görev 1-2: Köşeler (K1-K4) AÇIK (13 Düğüm)" : "Ödev Görev 1-2: Köşeler (K1-K4) KAPALI (9 Düğüm)"))
                    {
                        foreach (var k in cornerWaypoints_H5) if (k != null) k.SetActive(!cornersActive);
                        if (waypointGraph_H5 != null) waypointGraph_H5.BuildEdges();
                    }
                }

                if (GUILayout.Button("9.1 & Görev 2: A* ve Dijkstra Ölçüm Testini Çalıştır"))
                {
                    CalistirHafta5Deneyi();
                }
                if (!string.IsNullOrEmpty(lastExperimentOutput))
                {
                    GUILayout.Label($"<size=11><color=yellow>{lastExperimentOutput}</color></size>");
                }
                break;

            case AktifHafta.Hafta6_NavMeshMaliyetVeLink:
                GUILayout.Label("<b><color=#77ddff>HAFTA 6 — NavMesh Alan Maliyetleri, Kapı & Atlama</color></b>");
                GUILayout.Label("• <b>M Tuşu:</b> Çamur Maliyetini Döndür (1 → 2 → 5 → 10)");
                GUILayout.Label("• <b>J Tuşu:</b> Duvar Üstü Atlama Bağlantısını (NavMeshLink) Aç/Kapat");
                GUILayout.Label("• <b>K Tuşu:</b> Alt Koridordaki Kayan Kapıyı (Carve) Aç/Kapat");
                GUILayout.Label("• <b>Sol Üst Panel:</b> Köşe sayısı, uzunluk, maliyet ve PathStatus");
                if (npcNavChase_H6 != null)
                {
                    npcNavChase_H6.slowDownInMud = GUILayout.Toggle(npcNavChase_H6.slowDownInMud, " Ek Görev (+10): Çamurda ve Sığ Suda Gerçekten Yavaşla");
                }
                break;

            case AktifHafta.Hafta7_SonluDurumMakineleri:
                GUILayout.Label("<b><color=#77ddff>HAFTA 7 — Sonlu Durum Makineleri (FSM) & Sesi Duyan Bekçi</color></b>");
                GUILayout.Label("• <b>Durumlar:</b> <color=lime>Devriye (Yeşil)</color>, <color=red>Kovala (Kırmızı)</color>, <color=yellow>Ara (Sarı)</color>, <color=grey>KapıdaBekle (Gri)</color>");
                GUILayout.Label("• <b>Ödev 5. Durum:</b> <color=#d946ef>Araştır (Mor)</color> — 8m mesafeden sesi duyar, 3 sn bakınır");
                GUILayout.Label("• <b>N Tuşu (MakeNoise):</b> Oyuncu ses çıkarır | <b>K Tuşu:</b> Kapıyı Aç/Kapat");

                if (guardFsm_H7 != null && guardFsm_H7.FSM != null)
                {
                    string curName = guardFsm_H7.FSM.Current != null ? guardFsm_H7.FSM.Current.Name : "-";
                    string prevName = guardFsm_H7.FSM.Previous != null ? guardFsm_H7.FSM.Previous.Name : "-";
                    GUILayout.Label($"• <b>Aktif Durum:</b> <b>{curName}</b> (Önceki: {prevName})");
                }

                if (playerNoiseMaker_H7 != null && GUILayout.Button("🔊 Şimdi Ses Çıkar (MakeNoise - N Tuşu)"))
                {
                    playerNoiseMaker_H7.EmitNoise();
                }
                break;
        }
    }

    private void CalistirHafta5Deneyi()
    {
        if (waypointGraph_H5 == null) return;
        waypointGraph_H5.testHeuristic = true;
        waypointGraph_H5.TestPath();

        Waypoint s = waypointGraph_H5.Nearest(waypointGraph_H5.testFrom.position);
        Waypoint g = waypointGraph_H5.Nearest(waypointGraph_H5.testTo.position);
        var astar = new GraphAStar { useHeuristic = true };
        var pA = astar.FindPath(s, g);

        var dijkstra = new GraphAStar { useHeuristic = false };
        dijkstra.FindPath(s, g);

        if (pA != null)
        {
            float len = Vector3.Distance(waypointGraph_H5.testFrom.position, s.transform.position)
                      + Vector3.Distance(g.transform.position, waypointGraph_H5.testTo.position);
            for (int i = 1; i < pA.Count; i++)
                len += Vector3.Distance(pA[i - 1].transform.position, pA[i].transform.position);

            lastExperimentOutput = $"Yol: {string.Join("→", pA.ConvertAll(w => w.name))} | A* Gen: {astar.Expanded} | Dijk Gen: {dijkstra.Expanded} | Uz: {len:F1}m";
        }
    }
}
