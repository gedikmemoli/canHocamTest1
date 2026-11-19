using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Hafta 3, Hafta 4, Hafta 5, Hafta 6 ve Hafta 7'nin tüm ders içi uygulamalarını ve ödevlerini
/// hem SampleScene içinde aşamalı (F1-F5 ile geçişli) olarak hem de Assets/Scenes altında
/// ayrı sahneler (Hafta3_Gozcu, Hafta4_Uygulama, Hafta5_Waypoint, Hafta6_NavMesh, Hafta7_FSM)
/// olarak otomatik kuran kapsamlı Editor aracı.
/// </summary>
[InitializeOnLoad]
public class SetupTerrainAndScene : EditorWindow
{
    private const string KURULUM_TAMAMLANDI_KEY = "SetupTerrainAndScene_V5_AllWeeks_Completed";

    static SetupTerrainAndScene()
    {
        EditorApplication.delayCall += OtomatikCalistir;
    }

    private static void OtomatikCalistir()
    {
        if (!SessionState.GetBool(KURULUM_TAMAMLANDI_KEY, false))
        {
            SessionState.SetBool(KURULUM_TAMAMLANDI_KEY, true);
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.Log("[SetupTerrainAndScene] Hafta 3-7 tam proje kurulumu başlatılıyor...");
                TumunuKur();
            }
        }
    }

    [MenuItem("Tools/Hafta 3-7 Tüm Aşamaları ve Sahneleri Kur (Tam Kurulum)")]
    public static void TumunuKur()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Lütfen Play modundan çıkıp tekrar deneyin.");
            return;
        }

        Debug.Log(">>> [KURULUM] Hafta 3, 4, 5, 6, 7 Tüm Aşamalar ve Ödevler Kuruluyor...");

        KlasorleriHazirla();

        // Ana sahneyi aç (SampleScene)
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        if (File.Exists(sampleScenePath))
        {
            EditorSceneManager.OpenScene(sampleScenePath, OpenSceneMode.Single);
        }

        HaftaYonetici yonetici = SahneyiTamKur();

        // SampleScene'i kaydet
        Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, sampleScenePath);

        // Her hafta için ayrı sahne kopyalarını da kaydet (başlangıç haftası o haftaya ayarlı olarak)
        KaydetHaftaSahnesi(yonetici, HaftaYonetici.AktifHafta.Hafta3_AraziHareketGozcu, "Assets/Scenes/Hafta3_Gozcu.unity");
        KaydetHaftaSahnesi(yonetici, HaftaYonetici.AktifHafta.Hafta4_LabirentVeAStar, "Assets/Scenes/Hafta4_Uygulama.unity");
        KaydetHaftaSahnesi(yonetici, HaftaYonetici.AktifHafta.Hafta5_WaypointVeGraflar, "Assets/Scenes/Hafta5_Waypoint.unity");
        KaydetHaftaSahnesi(yonetici, HaftaYonetici.AktifHafta.Hafta6_NavMeshMaliyetVeLink, "Assets/Scenes/Hafta6_NavMesh.unity");
        KaydetHaftaSahnesi(yonetici, HaftaYonetici.AktifHafta.Hafta7_SonluDurumMakineleri, "Assets/Scenes/Hafta7_FSM.unity");

        // Tekrar SampleScene'e dön ve Hafta 3 ile başlat (F1-F5 ile hepsi gezilebilir)
        EditorSceneManager.OpenScene(sampleScenePath, OpenSceneMode.Single);
        BuildSettingsSahneleriniGuncelle();
        AssetDatabase.SaveAssets();

        Debug.Log(">>> [KURULUM TAMAMLANDI] Hafta 3, 4, 5, 6 ve 7 hem SampleScene içinde (F1-F5 / HUD ile) hem de Assets/Scenes altında ayrı sahneler olarak hazırlandı!");
    }

    [MenuItem("Tools/Aşamaya Geç/Hafta 3 - Arazi, Girdi, Hareket ve Gözcü [F1]")]
    public static void SecHafta3() => EditorHaftaSec(HaftaYonetici.AktifHafta.Hafta3_AraziHareketGozcu);

    [MenuItem("Tools/Aşamaya Geç/Hafta 4 - Labirent, A* ve Dağ Yolu [F2]")]
    public static void SecHafta4() => EditorHaftaSec(HaftaYonetici.AktifHafta.Hafta4_LabirentVeAStar);

    [MenuItem("Tools/Aşamaya Geç/Hafta 5 - Waypoint ve Graflar [F3]")]
    public static void SecHafta5() => EditorHaftaSec(HaftaYonetici.AktifHafta.Hafta5_WaypointVeGraflar);

    [MenuItem("Tools/Aşamaya Geç/Hafta 6 - NavMesh Maliyet, Kapı ve Atlama [F4]")]
    public static void SecHafta6() => EditorHaftaSec(HaftaYonetici.AktifHafta.Hafta6_NavMeshMaliyetVeLink);

    [MenuItem("Tools/Aşamaya Geç/Hafta 7 - Sonlu Durum Makineleri (FSM) ve Sesi Duyan Bekçi [F5]")]
    public static void SecHafta7() => EditorHaftaSec(HaftaYonetici.AktifHafta.Hafta7_SonluDurumMakineleri);

    private static void EditorHaftaSec(HaftaYonetici.AktifHafta hafta)
    {
        HaftaYonetici hy = Object.FindFirstObjectByType<HaftaYonetici>();
        if (hy == null) hy = SahneyiTamKur();
        if (hy != null)
        {
            hy.HaftayiUygula(hafta);
            EditorUtility.SetDirty(hy);
            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }

    private static void KaydetHaftaSahnesi(HaftaYonetici yonetici, HaftaYonetici.AktifHafta hafta, string scenePath)
    {
        if (yonetici != null)
        {
            yonetici.HaftayiUygula(hafta);
            EditorUtility.SetDirty(yonetici);
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath, true);
    }

    private static void BuildSettingsSahneleriniGuncelle()
    {
        string[] paths =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Hafta3_Gozcu.unity",
            "Assets/Scenes/Hafta4_Uygulama.unity",
            "Assets/Scenes/Hafta5_Waypoint.unity",
            "Assets/Scenes/Hafta6_NavMesh.unity",
            "Assets/Scenes/Hafta7_FSM.unity"
        };

        List<EditorBuildSettingsScene> list = new();
        foreach (string p in paths)
        {
            if (File.Exists(p))
                list.Add(new EditorBuildSettingsScene(p, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }

    private static void KlasorleriHazirla()
    {
        string[] dirs = { "Assets/Textures", "Assets/TerrainData", "Assets/Materials", "Assets/Prefabs", "Assets/Scenes" };
        foreach (string d in dirs)
        {
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
        }
    }

    public static HaftaYonetici SahneyiTamKur()
    {
        // Eski dağınık kök nesneleri temizle
        string[] temizlenecekler =
        {
            "Oyuncu_CC", "Oyuncu_RB", "Ajan_Gozcu", "ItilebilirKup", "Basamak_025m", "Basamak_050m",
            "Duvar_1", "Duvar_2", "Duvar_3", "AraziSondasi", "GirdiOkuyucu", "AraziKurucu",
            "Hafta3_AraziHareketGozcu", "Hafta4_LabirentVeAStar", "Hafta5_6_7_Arena20x20", "HaftaYonetici"
        };
        foreach (string ad in temizlenecekler)
        {
            GameObject go = GameObject.Find(ad);
            while (go != null)
            {
                Object.DestroyImmediate(go);
                go = GameObject.Find(ad);
            }
        }

        // 1. Dokular, Katmanlar ve Ortak Arazi (Terrain 200x50x200)
        Texture2D grassTex = DokulariOlustur("Assets/Textures/CimenDoku.png", new Color(0.25f, 0.55f, 0.20f), new Color(0.35f, 0.65f, 0.25f));
        Texture2D rockTex = DokulariOlustur("Assets/Textures/KayaDoku.png", new Color(0.45f, 0.42f, 0.40f), new Color(0.60f, 0.58f, 0.55f));
        TerrainLayer grassLayer = KatmanOlustur("Assets/TerrainData/CimenKatmani.terrainlayer", grassTex, new Vector2(10, 10));
        TerrainLayer rockLayer = KatmanOlustur("Assets/TerrainData/KayaKatmani.terrainlayer", rockTex, new Vector2(10, 10));
        PhysicsMaterial surtunmesizMat = SurtunmesizFizikMateryaliHazirla();

        Terrain terrain = ArazisiOlustur(grassLayer, rockLayer);
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer != -1) terrain.gameObject.layer = groundLayer;

        GameObject agacPrefab = AgacPrefabiOlustur();
        AgaclariYerlestir(terrain, agacPrefab, 16);

        // Ortak GirdiOkuyucu
        GirdiOkuyucu girdiOkuyucu = GirdiOkuyucuOlustur();

        // Ana Kamera
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();

        // ====================================================================
        // AŞAMA 1: HAFTA 3 — Arazi, Girdi, Hareket (CC + RB) ve Gözcü Ödevi
        // ====================================================================
        GameObject hafta3Root = new GameObject("Hafta3_AraziHareketGozcu");

        AraziSondasi sonda = AraziSondasiOlustur(terrain, hafta3Root.transform);

        Vector3 h3StartPos = new Vector3(-25f, 0f, -20f);
        h3StartPos.y = terrain.SampleHeight(h3StartPos) + terrain.transform.position.y + 0.1f;

        GameObject oyuncuCC_H3 = OyuncuCCOlustur("Oyuncu_CC", h3StartPos, girdiOkuyucu, hafta3Root.transform);
        GameObject oyuncuRB_H3 = OyuncuRBOlustur("Oyuncu_RB", h3StartPos + new Vector3(2.5f, 0f, 0f), girdiOkuyucu, surtunmesizMat, hafta3Root.transform);

        KarsilastirmaNesneleriniOlustur(terrain, h3StartPos, hafta3Root.transform);
        DuvarlariOlustur(terrain, h3StartPos, hafta3Root.transform);
        Gozcu gozcu_H3 = AjanGozcuOlustur(terrain, h3StartPos, oyuncuCC_H3.transform, hafta3Root.transform);

        TakipKamerasi takipKamerasi = null;
        if (mainCam != null)
        {
            takipKamerasi = mainCam.GetComponent<TakipKamerasi>();
            if (takipKamerasi == null) takipKamerasi = mainCam.gameObject.AddComponent<TakipKamerasi>();
            takipKamerasi.hedef = oyuncuCC_H3.transform;
            takipKamerasi.girdiOkuyucu = girdiOkuyucu;
            takipKamerasi.mesafe = 5.5f;
            takipKamerasi.hedefOfset = new Vector3(0f, 1.6f, 0f);
        }

        // ====================================================================
        // AŞAMA 2: HAFTA 4 — Arazide Labirent, NavMesh vs A* ve Dağ Yolu Ödevi
        // ====================================================================
        GameObject hafta4Root = new GameObject("Hafta4_LabirentVeAStar");

        GameObject wallPrefab = DuvarPrefabiOlustur();
        PathMarker markerPrefab = MarkerPrefabiOlustur();

        // Labirent konumu: arazinin (-65, 0, -55) bölgesindeki plato üzerinde 31x31 (62x62m)
        GameObject mazeGO = new GameObject("Maze");
        mazeGO.transform.SetParent(hafta4Root.transform, false);
        mazeGO.transform.position = new Vector3(-65f, 0f, -55f);
        MazeGenerator maze = mazeGO.AddComponent<MazeGenerator>();
        maze.width = 31;
        maze.depth = 31;
        maze.cellSize = 2f;
        maze.seed = 2026;
        maze.loopChance = 0.08f;
        maze.wallPrefab = wallPrefab;
        maze.terrain = terrain;
        maze.wallHeight = 2f;
        maze.wallSink = 1f;

        // Dağ Yolu Ödevi için TerrainGrid bileşeni
        GameObject terrainGridGO = new GameObject("TerrainGrid_DagYolu");
        terrainGridGO.transform.SetParent(hafta4Root.transform, false);
        terrainGridGO.transform.position = new Vector3(-65f, 0f, -55f);
        TerrainGrid terrainGrid = terrainGridGO.AddComponent<TerrainGrid>();
        terrainGrid.width = 31;
        terrainGrid.depth = 31;
        terrainGrid.cellSize = 2f;
        terrainGrid.terrain = terrain;
        terrainGrid.maxWalkableSlope = 42f;
        terrainGrid.slopeCostFactor = 8f;

        // Pathfinder
        GameObject pathfinderGO = new GameObject("Pathfinder");
        pathfinderGO.transform.SetParent(hafta4Root.transform, false);
        GridPathfinder pathfinder = pathfinderGO.AddComponent<GridPathfinder>();
        pathfinder.gridSource = maze;
        pathfinder.markerPrefab = markerPrefab;
        pathfinder.drawMarkers = true;
        pathfinder.markerLift = 0.15f;
        pathfinder.stepDelay = 0.005f;
        pathfinder.algorithm = SearchAlgorithm.AStar;
        pathfinder.heuristic = HeuristicType.Manhattan;

        // PathLine
        GameObject pathLineGO = new GameObject("PathLine");
        pathLineGO.transform.SetParent(hafta4Root.transform, false);
        LineRenderer lr4 = pathLineGO.AddComponent<LineRenderer>();
        lr4.startWidth = 0.25f;
        lr4.endWidth = 0.25f;
        lr4.useWorldSpace = true;
        lr4.numCornerVertices = 4;
        lr4.numCapVertices = 4;
        lr4.sharedMaterial = UnlitMateryalAlVeyaOlustur("Assets/Materials/PathLineMat.mat", Color.yellow);
        PathLine pathLine = pathLineGO.AddComponent<PathLine>();
        pathLine.pathfinder = pathfinder;
        pathLine.gridSource = maze;
        pathLine.lift = 0.3f;

        // H4 A* Karakteri (PathFollower)
        GameObject h4FollowerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        h4FollowerGO.name = "AStar_Agent_CC";
        h4FollowerGO.transform.SetParent(hafta4Root.transform, false);
        Object.DestroyImmediate(h4FollowerGO.GetComponent<CapsuleCollider>());
        CharacterController cc4 = h4FollowerGO.AddComponent<CharacterController>();
        cc4.radius = 0.4f;
        cc4.height = 1.8f;
        cc4.center = Vector3.zero;
        cc4.slopeLimit = 45f;
        cc4.stepOffset = 0.3f;
        h4FollowerGO.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/OyuncuCCMat.mat", new Color(0.95f, 0.75f, 0.15f));
        PathFollower follower = h4FollowerGO.AddComponent<PathFollower>();
        follower.pathfinder = pathfinder;
        follower.gridSource = maze;
        follower.speed = 4f;
        follower.turnSpeed = 10f;
        follower.arriveRadius = 0.4f;

        // H4 NavMesh Ajanı (NavAgent)
        GameObject navAgentGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        navAgentGO.name = "NavAgent";
        navAgentGO.transform.SetParent(hafta4Root.transform, false);
        Object.DestroyImmediate(navAgentGO.GetComponent<CapsuleCollider>());
        navAgentGO.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/OyuncuRBMat.mat", new Color(0.2f, 0.55f, 0.95f));
        NavMeshAgent navAgent4 = navAgentGO.AddComponent<NavMeshAgent>();
        navAgent4.speed = 4f;
        navAgent4.angularSpeed = 720f;
        navAgent4.acceleration = 20f;
        navAgent4.stoppingDistance = 0.2f;
        navAgent4.radius = 0.4f;
        navAgent4.height = 1.8f;
        NavPathDebug navDebug = navAgentGO.AddComponent<NavPathDebug>();
        navDebug.navAgent = navAgent4;

        // NavSurface & NavMeshRefresher
        GameObject navSurfaceGO = new GameObject("NavSurface");
        navSurfaceGO.transform.SetParent(hafta4Root.transform, false);
        NavMeshSurface surface4 = navSurfaceGO.AddComponent<NavMeshSurface>();
        surface4.collectObjects = CollectObjects.Volume;
        surface4.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        int wallLayer = LayerMask.NameToLayer("Wall");
        int gMask = (groundLayer != -1 ? (1 << groundLayer) : ~0);
        int wMask = (wallLayer != -1 ? (1 << wallLayer) : 0);
        surface4.layerMask = gMask | wMask | 1; // Default + Ground + Wall
        NavMeshRefresher refresher4 = navSurfaceGO.AddComponent<NavMeshRefresher>();
        refresher4.surface = surface4;
        refresher4.maze = maze;
        refresher4.navAgent = navAgent4;

        // GoalFlag
        GameObject goalFlagGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goalFlagGO.name = "GoalFlag";
        goalFlagGO.transform.SetParent(hafta4Root.transform, false);
        goalFlagGO.transform.localScale = new Vector3(0.4f, 1.5f, 0.4f);
        Object.DestroyImmediate(goalFlagGO.GetComponent<CapsuleCollider>());
        goalFlagGO.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/GoalFlagMat.mat", Color.red);

        // Input (ClickToGoal & WallEditor)
        GameObject inputGO = new GameObject("Input");
        inputGO.transform.SetParent(hafta4Root.transform, false);
        ClickToGoal clickToGoal = inputGO.AddComponent<ClickToGoal>();
        clickToGoal.cam = mainCam;
        clickToGoal.maze = maze;
        clickToGoal.goalFlag = goalFlagGO.transform;
        clickToGoal.groundMask = (groundLayer != -1) ? (1 << groundLayer) : ~0;
        clickToGoal.navAgent = navAgent4;
        clickToGoal.pathfinder = pathfinder;
        clickToGoal.agent = h4FollowerGO.transform;

        WallEditor wallEditor = inputGO.AddComponent<WallEditor>();
        wallEditor.cam = mainCam;
        wallEditor.maze = maze;
        wallEditor.agent = h4FollowerGO.transform;
        wallEditor.navAgent = navAgentGO.transform;
        wallEditor.editMask = gMask | wMask | 1;

        // Replanner
        GameObject replannerGO = new GameObject("Replanner");
        replannerGO.transform.SetParent(hafta4Root.transform, false);
        Replanner replanner = replannerGO.AddComponent<Replanner>();
        replanner.maze = maze;
        replanner.pathfinder = pathfinder;
        replanner.follower = follower;
        replanner.replanOnAnyChange = false;

        // CameraModes (Main Camera üzerinde)
        CameraModes camModes = null;
        if (mainCam != null)
        {
            camModes = mainCam.GetComponent<CameraModes>();
            if (camModes == null) camModes = mainCam.gameObject.AddComponent<CameraModes>();
            camModes.target = h4FollowerGO.transform;
            camModes.maze = maze;
            camModes.enabled = false;
        }

        // ====================================================================
        // AŞAMA 3, 4, 5: HAFTA 5, 6, 7 — 20x20 U Engelli Arena
        // (Ortak 20x20 sahne, yüksekliği Y = 60 m'de düz ve bağımsız arena)
        // ====================================================================
        Vector3 arenaOrigin = new Vector3(200f, 60f, 200f);
        GameObject arenaRoot = new GameObject("Hafta5_6_7_Arena20x20");
        arenaRoot.transform.position = arenaOrigin;

        GameObject alanGO = new GameObject("Alan");
        alanGO.transform.SetParent(arenaRoot.transform, false);
        alanGO.transform.localPosition = Vector3.zero;

        // 20x20 Zemin (merkezi (10, -0.1, 10), boyutu (20, 0.2, 20), üst yüzeyi tam y=0)
        GameObject zeminGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zeminGO.name = "Zemin_20x20";
        zeminGO.transform.SetParent(alanGO.transform, false);
        zeminGO.transform.localPosition = new Vector3(10f, -0.1f, 10f);
        zeminGO.transform.localScale = new Vector3(20f, 0.2f, 20f);
        zeminGO.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/ArenaZeminMat.mat", new Color(0.22f, 0.25f, 0.28f));

        // U Engeli:
        // Üst kol: x = [7..13], z = [14..15] -> merkez (10, 0.5, 14.5), boyut (6, 1, 1)
        // Alt kol: x = [7..13], z = [5..6]   -> merkez (10, 0.5, 5.5),  boyut (6, 1, 1)
        // Sağ dikey duvar: x = [12..13], z = [6..14] -> merkez (12.5, 0.5, 10), boyut (1, 1, 8)
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        Material uWallMat = MateryalAlVeyaOlustur("Assets/Materials/DuvarMat.mat", new Color(0.45f, 0.47f, 0.52f));

        GameObject uRoot = new GameObject("U_Engeli");
        uRoot.transform.SetParent(alanGO.transform, false);

        OlusturEngelBlok("U_UstKol", uRoot.transform, new Vector3(10f, 0.5f, 14.5f), new Vector3(6f, 1f, 1f), obstacleLayer, uWallMat);
        OlusturEngelBlok("U_AltKol", uRoot.transform, new Vector3(10f, 0.5f, 5.5f), new Vector3(6f, 1f, 1f), obstacleLayer, uWallMat);
        OlusturEngelBlok("U_SagDuvar", uRoot.transform, new Vector3(12.5f, 0.5f, 10f), new Vector3(1f, 1f, 8f), obstacleLayer, uWallMat);

        // Arena Oyuncusu (Mavi Küre/Kapsül, konum (17.5, 0.5, 9.5))
        GameObject arenaPlayerGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        arenaPlayerGO.name = "Player_Arena";
        arenaPlayerGO.transform.SetParent(arenaRoot.transform, false);
        arenaPlayerGO.transform.localPosition = new Vector3(17.5f, 0.5f, 9.5f);
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1) arenaPlayerGO.layer = playerLayer;
        arenaPlayerGO.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/OyuncuRBMat.mat", new Color(0.2f, 0.6f, 1f));
        PlayerMove pMove = arenaPlayerGO.AddComponent<PlayerMove>();
        pMove.speed = 5f;
        pMove.clampToArena = true;
        pMove.arenaOrigin = arenaOrigin;
        PlayerNoiseMaker noiseMaker = arenaPlayerGO.AddComponent<PlayerNoiseMaker>();

        // --------------------------------------------------------------------
        // HAFTA 5 ÖĞELERİ: Waypoints (W0..W8 + Ödev Köşeler K1..K4 + TestBaşlangıç/Hedef)
        // --------------------------------------------------------------------
        GameObject waypointsGO = new GameObject("Waypoints");
        waypointsGO.transform.SetParent(arenaRoot.transform, false);
        waypointsGO.transform.localPosition = Vector3.zero;
        WaypointGraph wpGraph = waypointsGO.AddComponent<WaypointGraph>();
        wpGraph.obstacleMask = (obstacleLayer != -1) ? (1 << obstacleLayer) : ~0;
        wpGraph.maxEdgeLength = 8f;

        Vector3[] wPositions =
        {
            new(3f, 0.5f, 3f),   // W0
            new(10f, 0.5f, 3f),  // W1
            new(16f, 0.5f, 3f),  // W2
            new(16f, 0.5f, 10f), // W3
            new(16f, 0.5f, 17f), // W4
            new(10f, 0.5f, 17f), // W5
            new(3f, 0.5f, 17f),  // W6
            new(3f, 0.5f, 10f),  // W7
            new(9f, 0.5f, 10f)   // W8
        };

        Waypoint[] patrolRouteW = new Waypoint[8];
        for (int i = 0; i < wPositions.Length; i++)
        {
            GameObject wGO = new GameObject($"W{i}");
            wGO.transform.SetParent(waypointsGO.transform, false);
            wGO.transform.localPosition = wPositions[i];
            Waypoint wp = wGO.AddComponent<Waypoint>();
            if (i < 8) patrolRouteW[i] = wp;
        }

        // Hafta 5 Ödevi Görev 1: U'nun köşelerine 4 nokta (K1, K2, K3, K4)
        Vector3[] kPositions =
        {
            new(6f, 0.5f, 4f),   // K1
            new(14f, 0.5f, 4f),  // K2
            new(14f, 0.5f, 16f), // K3
            new(6f, 0.5f, 16f)   // K4
        };
        GameObject[] cornerWps = new GameObject[4];
        for (int i = 0; i < kPositions.Length; i++)
        {
            GameObject kGO = new GameObject($"K{i + 1}");
            kGO.transform.SetParent(waypointsGO.transform, false);
            kGO.transform.localPosition = kPositions[i];
            kGO.AddComponent<Waypoint>();
            cornerWps[i] = kGO;
        }

        // 9.1 TestBaşlangıç ve TestHedef
        GameObject testFromGO = new GameObject("TestBaslangic");
        testFromGO.transform.SetParent(arenaRoot.transform, false);
        testFromGO.transform.localPosition = new Vector3(6.5f, 0.5f, 13.5f);
        GameObject testToGO = new GameObject("TestHedef");
        testToGO.transform.SetParent(arenaRoot.transform, false);
        testToGO.transform.localPosition = new Vector3(17.5f, 0.5f, 9.5f);
        wpGraph.testFrom = testFromGO.transform;
        wpGraph.testTo = testToGO.transform;
        wpGraph.BuildEdges();

        // --------------------------------------------------------------------
        // HAFTA 6 ÖĞELERİ: Çamur, Sığ Su (Ödev 2. Alan), Kapı (SlidingDoor) ve Atlama Bağlantısı (NavMeshLink)
        // --------------------------------------------------------------------
        GameObject h6ExtrasRoot = new GameObject("Hafta6_MaliyetVeBaglantilar");
        h6ExtrasRoot.transform.SetParent(alanGO.transform, false);

        int camurArea = NavMesh.GetAreaFromName("Camur");
        if (camurArea < 0) camurArea = 3;
        int sigSuArea = NavMesh.GetAreaFromName("SigSu");
        if (sigSuArea < 0) sigSuArea = 4;
        int jumpArea = NavMesh.GetAreaFromName("Jump");
        if (jumpArea < 0) jumpArea = 2;

        // Çamur (10, 0, 17.5), Size (6, 2, 5)
        GameObject camurGO = new GameObject("Camur");
        camurGO.transform.SetParent(h6ExtrasRoot.transform, false);
        camurGO.transform.localPosition = new Vector3(10f, 0f, 17.5f);
        NavMeshModifierVolume camurVol = camurGO.AddComponent<NavMeshModifierVolume>();
        camurVol.size = new Vector3(6f, 2f, 5f);
        camurVol.center = Vector3.zero;
        camurVol.area = camurArea;

        GameObject camurGorsel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        camurGorsel.name = "CamurGorsel";
        camurGorsel.transform.SetParent(h6ExtrasRoot.transform, false);
        camurGorsel.transform.localPosition = new Vector3(10f, 0.01f, 17.5f);
        camurGorsel.transform.localScale = new Vector3(6f, 0.02f, 5f);
        Object.DestroyImmediate(camurGorsel.GetComponent<BoxCollider>());
        camurGorsel.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/CamurMat.mat", new Color(0.42f, 0.26f, 0.12f));

        // Hafta 6 Ödevi (Bölüm 13): İkinci Özel Alan Tipi -> Sığ Su (SigSu)
        GameObject sigSuGO = new GameObject("SigSu");
        sigSuGO.transform.SetParent(h6ExtrasRoot.transform, false);
        sigSuGO.transform.localPosition = new Vector3(15.5f, 0f, 2.5f);
        NavMeshModifierVolume sigSuVol = sigSuGO.AddComponent<NavMeshModifierVolume>();
        sigSuVol.size = new Vector3(4f, 2f, 4f);
        sigSuVol.center = Vector3.zero;
        sigSuVol.area = sigSuArea;

        GameObject sigSuGorsel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sigSuGorsel.name = "SigSuGorsel";
        sigSuGorsel.transform.SetParent(h6ExtrasRoot.transform, false);
        sigSuGorsel.transform.localPosition = new Vector3(15.5f, 0.01f, 2.5f);
        sigSuGorsel.transform.localScale = new Vector3(4f, 0.02f, 4f);
        Object.DestroyImmediate(sigSuGorsel.GetComponent<BoxCollider>());
        sigSuGorsel.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/SigSuMat.mat", new Color(0.15f, 0.55f, 0.85f));

        // Kapi (9.5, 1, 2.5), ölçek (1, 2, 5)
        GameObject kapiGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kapiGO.name = "Kapi";
        kapiGO.transform.SetParent(alanGO.transform, false);
        kapiGO.transform.localPosition = new Vector3(9.5f, 1f, 2.5f);
        kapiGO.transform.localScale = new Vector3(1f, 2f, 5f);
        kapiGO.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/KapiMat.mat", new Color(0.6f, 0.6f, 0.65f));

        NavMeshModifier kapiMod = kapiGO.AddComponent<NavMeshModifier>();
        kapiMod.ignoreFromBuild = true; // Remove object: Bake dışında bırak

        NavMeshObstacle kapiObs = kapiGO.AddComponent<NavMeshObstacle>();
        kapiObs.shape = NavMeshObstacleShape.Box;
        kapiObs.center = Vector3.zero;
        kapiObs.size = Vector3.one;
        kapiObs.carving = true;
        kapiObs.carvingMoveThreshold = 0.1f;
        kapiObs.carvingTimeToStationary = 0.5f;
        kapiObs.carveOnlyStationary = true;

        SlidingDoor slidingDoor = kapiGO.AddComponent<SlidingDoor>();
        slidingDoor.closedY = 1f;
        slidingDoor.openY = -1.1f;
        slidingDoor.speed = 2f;
        slidingDoor.startOpen = true;

        // AtlamaBaglantisi (12.5, 0, 9.5)
        GameObject linkGO = new GameObject("AtlamaBaglantisi");
        linkGO.transform.SetParent(h6ExtrasRoot.transform, false);
        linkGO.transform.localPosition = new Vector3(12.5f, 0f, 9.5f);
        NavMeshLink navLink = linkGO.AddComponent<NavMeshLink>();
        navLink.startPoint = new Vector3(-1.5f, 0f, 0f);
        navLink.endPoint = new Vector3(1.5f, 0f, 0f);
        navLink.width = 0f;
        navLink.bidirectional = true;
        navLink.area = jumpArea;
        navLink.activated = true;

        // Alan üzerine NavMeshSurface ekle ve Bake et
        NavMeshSurface arenaSurface = alanGO.AddComponent<NavMeshSurface>();
        arenaSurface.collectObjects = CollectObjects.Children;
        arenaSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        arenaSurface.BuildNavMesh();

        // --------------------------------------------------------------------
        // HAFTA 7 ÖĞELERİ: P0, P1, P2, P3 Devriye Noktaları (P1-P2 kapıdan geçer)
        // --------------------------------------------------------------------
        GameObject patrolRoot = new GameObject("PatrolPoints_H7");
        patrolRoot.transform.SetParent(arenaRoot.transform, false);
        Vector3[] pCoords =
        {
            new(4f, 0.5f, 16f),  // P0 (Sol üst)
            new(4f, 0.5f, 2.5f), // P1 (Kapının sol yanı)
            new(15f, 0.5f, 2.5f),// P2 (Kapının sağ yanı - kapıdan geçer)
            new(15f, 0.5f, 16f)  // P3 (Sağ üst)
        };
        Transform[] patrolTransforms = new Transform[4];
        for (int i = 0; i < pCoords.Length; i++)
        {
            GameObject pGO = new GameObject($"P{i}");
            pGO.transform.SetParent(patrolRoot.transform, false);
            pGO.transform.localPosition = pCoords[i];
            patrolTransforms[i] = pGO.transform;
        }

        // --------------------------------------------------------------------
        // ARENA NPC / BEKÇİ (Kırmızı Küre: Hafta 5, 6 ve 7 bileşenlerini taşır)
        // --------------------------------------------------------------------
        GameObject bekciGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bekciGO.name = "Bekci";
        bekciGO.transform.SetParent(arenaRoot.transform, false);
        bekciGO.transform.localPosition = new Vector3(6.5f, 0.5f, 13.5f);
        bekciGO.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/BekciMat.mat", Color.red);

        // NavMeshAgent
        NavMeshAgent bekciAgent = bekciGO.AddComponent<NavMeshAgent>();
        bekciAgent.speed = 3.5f;
        bekciAgent.angularSpeed = 360f;
        bekciAgent.acceleration = 16f;
        bekciAgent.stoppingDistance = 0.2f;
        bekciAgent.radius = 0.5f;
        bekciAgent.height = 1f;
        bekciAgent.baseOffset = 0.5f;

        // LineRenderer (Hafta 6 rota çizimi için)
        LineRenderer bekciLine = bekciGO.AddComponent<LineRenderer>();
        bekciLine.startWidth = 0.15f;
        bekciLine.endWidth = 0.15f;
        bekciLine.useWorldSpace = true;
        bekciLine.sharedMaterial = UnlitMateryalAlVeyaOlustur("Assets/Materials/PathLineMat.mat", Color.yellow);

        // Hafta 5: NpcWaypointGuard
        NpcWaypointGuard wpGuard = bekciGO.AddComponent<NpcWaypointGuard>();
        wpGuard.graph = wpGraph;
        wpGuard.player = arenaPlayerGO.transform;
        wpGuard.patrolRoute = patrolRouteW;
        wpGuard.pingPong = false;
        wpGuard.usePathSmoothing = true;
        wpGuard.enabled = false;

        // Hafta 6: NpcNavChase, JumpLinkTraverser, AreaCostToggle, PathDebugDraw
        NpcNavChase navChase = bekciGO.AddComponent<NpcNavChase>();
        navChase.target = arenaPlayerGO.transform;
        navChase.baseSpeed = 3.5f;
        navChase.slowDownInMud = true;
        navChase.enabled = false;

        JumpLinkTraverser jumpTrav = bekciGO.AddComponent<JumpLinkTraverser>();
        jumpTrav.arcHeight = 1.5f;
        jumpTrav.duration = 0.6f;
        jumpTrav.enabled = false;

        AreaCostToggle costToggle = bekciGO.AddComponent<AreaCostToggle>();
        costToggle.jumpLink = navLink;
        costToggle.mudAreaName = "Camur";
        costToggle.enabled = false;

        PathDebugDraw pathDebug = bekciGO.AddComponent<PathDebugDraw>();
        pathDebug.jumpLink = navLink;
        pathDebug.enabled = false;

        // Hafta 7: Guard (FSM + 5. Durum Araştır)
        Guard guardFsm = bekciGO.AddComponent<Guard>();
        guardFsm.player = arenaPlayerGO.transform;
        guardFsm.patrolPoints = patrolTransforms;
        guardFsm.door = slidingDoor;
        guardFsm.patrolSpeed = 2f;
        guardFsm.chaseSpeed = 4f;
        guardFsm.viewDistance = 10f;
        guardFsm.viewAngle = 120f;
        guardFsm.loseTime = 1f;
        guardFsm.searchTime = 5f;
        guardFsm.doorStopDistance = 2.5f;
        guardFsm.hearingDistance = 8f;
        guardFsm.investigateTime = 3f;
        guardFsm.enabled = false;

        noiseMaker.targetGuard = guardFsm;

        // ====================================================================
        // MERKEZİ HAFTA YÖNETİCİSİ (F1-F5 ve HUD)
        // ====================================================================
        GameObject yoneticiGO = new GameObject("HaftaYonetici");
        HaftaYonetici yonetici = yoneticiGO.AddComponent<HaftaYonetici>();
        yonetici.hafta3Root = hafta3Root;
        yonetici.hafta4Root = hafta4Root;
        yonetici.hafta567ArenaRoot = arenaRoot;

        yonetici.oyuncuCC_H3 = oyuncuCC_H3;
        yonetici.oyuncuRB_H3 = oyuncuRB_H3;
        yonetici.gozcu_H3 = gozcu_H3;
        yonetici.araziSondasi_H3 = sonda;

        yonetici.maze_H4 = maze;
        yonetici.terrainGrid_H4 = terrainGrid;
        yonetici.pathfinder_H4 = pathfinder;
        yonetici.follower_H4 = follower;
        yonetici.pathLine_H4 = pathLine;
        yonetici.clickToGoal_H4 = clickToGoal;
        yonetici.navAgent_H4 = navAgent4;
        yonetici.navRefresher_H4 = refresher4;

        yonetici.arenaPlayer = arenaPlayerGO.transform;
        yonetici.waypointsRoot_H5 = waypointsGO;
        yonetici.waypointGraph_H5 = wpGraph;
        yonetici.cornerWaypoints_H5 = cornerWps;
        yonetici.camurVeLinkRoot_H6 = h6ExtrasRoot;
        yonetici.slidingDoor_H67 = slidingDoor;
        yonetici.patrolPointsRoot_H7 = patrolRoot;

        yonetici.arenaNpcGO = bekciGO;
        yonetici.npcWaypointGuard_H5 = wpGuard;
        yonetici.npcNavChase_H6 = navChase;
        yonetici.jumpTraverser_H6 = jumpTrav;
        yonetici.areaCostToggle_H6 = costToggle;
        yonetici.pathDebugDraw_H6 = pathDebug;
        yonetici.guardFsm_H7 = guardFsm;
        yonetici.playerNoiseMaker_H7 = noiseMaker;

        yonetici.mainCamera = mainCam;
        yonetici.takipKamerasi_H3 = takipKamerasi;
        yonetici.cameraModes_H4 = camModes;

        yonetici.HaftayiUygula(HaftaYonetici.AktifHafta.Hafta3_AraziHareketGozcu);
        return yonetici;
    }

    private static void OlusturEngelBlok(string ad, Transform parent, Vector3 localPos, Vector3 scale, int layer, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = ad;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = scale;
        if (layer != -1) cube.layer = layer;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static GameObject DuvarPrefabiOlustur()
    {
        string path = "Assets/Prefabs/MazeWallPrefab.prefab";
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "MazeWallPrefab";
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer != -1) cube.layer = wallLayer;
        cube.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/DuvarMat.mat", new Color(0.42f, 0.44f, 0.48f));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cube, path);
        Object.DestroyImmediate(cube);
        return prefab;
    }

    private static PathMarker MarkerPrefabiOlustur()
    {
        string path = "Assets/Prefabs/PathMarkerPrefab.prefab";
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        quad.name = "PathMarkerPrefab";
        quad.transform.localScale = new Vector3(1.8f, 0.1f, 1.8f);
        Object.DestroyImmediate(quad.GetComponent<BoxCollider>());
        quad.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/MarkerMat.mat", new Color(0.3f, 0.7f, 1f));
        quad.AddComponent<PathMarker>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(quad, path);
        Object.DestroyImmediate(quad);
        return prefab.GetComponent<PathMarker>();
    }

    private static GameObject OyuncuCCOlustur(string ad, Vector3 konum, GirdiOkuyucu girdiOkuyucu, Transform parent)
    {
        GameObject oyuncu = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        oyuncu.name = ad;
        oyuncu.transform.SetParent(parent, false);
        oyuncu.transform.position = konum;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1) oyuncu.layer = playerLayer;

        Object.DestroyImmediate(oyuncu.GetComponent<CapsuleCollider>());
        CharacterController cc = oyuncu.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 1f, 0f);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;

        CCHareket ccHareket = oyuncu.AddComponent<CCHareket>();
        ccHareket.girdiOkuyucu = girdiOkuyucu;
        if (Camera.main != null) ccHareket.kameraTransform = Camera.main.transform;

        Material ccMat = MateryalAlVeyaOlustur("Assets/Materials/OyuncuCCMat.mat", new Color(0.95f, 0.55f, 0.15f));
        oyuncu.GetComponent<Renderer>().sharedMaterial = ccMat;
        return oyuncu;
    }

    private static GameObject OyuncuRBOlustur(string ad, Vector3 konum, GirdiOkuyucu girdiOkuyucu, PhysicsMaterial surtunmesizMat, Transform parent)
    {
        GameObject oyuncu = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        oyuncu.name = ad;
        oyuncu.transform.SetParent(parent, false);
        oyuncu.transform.position = konum;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1) oyuncu.layer = playerLayer;

        CapsuleCollider col = oyuncu.GetComponent<CapsuleCollider>();
        if (col == null) col = oyuncu.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.5f;
        col.center = new Vector3(0f, 1f, 0f);
        col.sharedMaterial = surtunmesizMat;

        Rigidbody rb = oyuncu.AddComponent<Rigidbody>();
        rb.mass = 70f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        RBHareket rbHareket = oyuncu.AddComponent<RBHareket>();
        rbHareket.girdiOkuyucu = girdiOkuyucu;
        if (Camera.main != null) rbHareket.kameraTransform = Camera.main.transform;

        Material rbMat = MateryalAlVeyaOlustur("Assets/Materials/OyuncuRBMat.mat", new Color(0.2f, 0.55f, 0.95f));
        oyuncu.GetComponent<Renderer>().sharedMaterial = rbMat;
        return oyuncu;
    }

    private static Gozcu AjanGozcuOlustur(Terrain terrain, Vector3 oyuncuKonum, Transform hedefOyuncu, Transform parent)
    {
        Vector3 gozcuKonum = oyuncuKonum + new Vector3(0f, 0f, 14f);
        gozcuKonum.y = terrain.SampleHeight(gozcuKonum) + terrain.transform.position.y + 1f;

        GameObject gozcu = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        gozcu.name = "Ajan_Gozcu";
        gozcu.transform.SetParent(parent, false);
        gozcu.transform.position = gozcuKonum;
        gozcu.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Material gozcuMat = MateryalAlVeyaOlustur("Assets/Materials/GozcuMat.mat", new Color(0.95f, 0.75f, 0.15f));
        gozcu.GetComponent<Renderer>().sharedMaterial = gozcuMat;

        GameObject burun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        burun.name = "Burun";
        burun.transform.SetParent(gozcu.transform, false);
        burun.transform.localScale = new Vector3(0.25f, 0.2f, 0.5f);
        burun.transform.localPosition = new Vector3(0f, 0.6f, 0.55f);
        Object.DestroyImmediate(burun.GetComponent<BoxCollider>());
        burun.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/BurunMat.mat", new Color(0.15f, 0.15f, 0.15f));

        NavMeshAgent agent = gozcu.AddComponent<NavMeshAgent>();
        agent.speed = 4.5f;
        agent.stoppingDistance = 1.6f;
        agent.angularSpeed = 240f;
        agent.acceleration = 14f;

        Gozcu gozcuBileseni = gozcu.AddComponent<Gozcu>();
        gozcuBileseni.menzil = 18f;
        gozcuBileseni.gorusAcisi = 90f;
        gozcuBileseni.dikeyGorusAcisi = 50f;
        gozcuBileseni.gozYuksekligi = 0.6f;
        gozcuBileseni.kovalamaHizi = 4.5f;
        gozcuBileseni.hedefOyuncu = hedefOyuncu;

        int playerLayer = LayerMask.NameToLayer("Player");
        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        int haric = 0;
        if (playerLayer != -1) haric |= (1 << playerLayer);
        if (ignoreRaycast != -1) haric |= (1 << ignoreRaycast);
        gozcuBileseni.engelMaskesi = ~haric;

        return gozcuBileseni;
    }

    private static void DuvarlariOlustur(Terrain terrain, Vector3 oyuncuKonum, Transform parent)
    {
        Material duvarMat = MateryalAlVeyaOlustur("Assets/Materials/DuvarMat.mat", new Color(0.40f, 0.42f, 0.46f));

        GameObject d1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        d1.name = "Duvar_1";
        d1.transform.SetParent(parent, false);
        Vector3 pos1 = oyuncuKonum + new Vector3(-3f, 0f, 5.5f);
        pos1.y = terrain.SampleHeight(pos1) + terrain.transform.position.y + 1.5f;
        d1.transform.position = pos1;
        d1.transform.localScale = new Vector3(1f, 3f, 9f);
        d1.GetComponent<Renderer>().sharedMaterial = duvarMat;

        GameObject d2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        d2.name = "Duvar_2";
        d2.transform.SetParent(parent, false);
        Vector3 pos2 = oyuncuKonum + new Vector3(1.5f, 0f, 9.5f);
        pos2.y = terrain.SampleHeight(pos2) + terrain.transform.position.y + 1.5f;
        d2.transform.position = pos2;
        d2.transform.localScale = new Vector3(8f, 3f, 1f);
        d2.GetComponent<Renderer>().sharedMaterial = duvarMat;

        GameObject d3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        d3.name = "Duvar_3";
        d3.transform.SetParent(parent, false);
        Vector3 pos3 = oyuncuKonum + new Vector3(4.5f, 0f, 4f);
        pos3.y = terrain.SampleHeight(pos3) + terrain.transform.position.y + 1.5f;
        d3.transform.position = pos3;
        d3.transform.localScale = new Vector3(1f, 3f, 7f);
        d3.GetComponent<Renderer>().sharedMaterial = duvarMat;
    }

    private static void KarsilastirmaNesneleriniOlustur(Terrain terrain, Vector3 oyuncuKonum, Transform parent)
    {
        Vector3 kupKonum = oyuncuKonum + new Vector3(0f, 0f, 3.5f);
        kupKonum.y = terrain.SampleHeight(kupKonum) + terrain.transform.position.y + 0.6f;

        GameObject kup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kup.name = "ItilebilirKup";
        kup.transform.SetParent(parent, false);
        kup.transform.position = kupKonum;
        kup.transform.localScale = Vector3.one * 1.2f;
        Rigidbody kupRb = kup.AddComponent<Rigidbody>();
        kupRb.mass = 15f;
        kupRb.interpolation = RigidbodyInterpolation.Interpolate;
        kup.GetComponent<Renderer>().sharedMaterial = MateryalAlVeyaOlustur("Assets/Materials/ItilebilirKupMat.mat", new Color(0.95f, 0.60f, 0.10f));

        Material basamakMat = MateryalAlVeyaOlustur("Assets/Materials/BasamakMat.mat", new Color(0.70f, 0.72f, 0.75f));

        Vector3 b25Konum = oyuncuKonum + new Vector3(-8f, 0f, 6f);
        b25Konum.y = terrain.SampleHeight(b25Konum) + terrain.transform.position.y + 0.125f;
        GameObject basamak025 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basamak025.name = "Basamak_025m";
        basamak025.transform.SetParent(parent, false);
        basamak025.transform.position = b25Konum;
        basamak025.transform.localScale = new Vector3(3.5f, 0.25f, 2.5f);
        basamak025.GetComponent<Renderer>().sharedMaterial = basamakMat;

        Vector3 b50Konum = oyuncuKonum + new Vector3(-8f, 0f, 9.5f);
        b50Konum.y = terrain.SampleHeight(b50Konum) + terrain.transform.position.y + 0.25f;
        GameObject basamak050 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basamak050.name = "Basamak_050m";
        basamak050.transform.SetParent(parent, false);
        basamak050.transform.position = b50Konum;
        basamak050.transform.localScale = new Vector3(3.5f, 0.50f, 2.5f);
        basamak050.GetComponent<Renderer>().sharedMaterial = basamakMat;
    }

    private static AraziSondasi AraziSondasiOlustur(Terrain terrain, Transform parent)
    {
        GameObject sondaGO = new GameObject("AraziSondasi");
        sondaGO.transform.SetParent(parent, false);
        sondaGO.transform.position = new Vector3(4f, 25f, -20f);
        AraziSondasi sonda = sondaGO.AddComponent<AraziSondasi>();
        sonda.hedefArazi = terrain;
        sonda.nesnePozisyonunuKullan = true;
        sonda.normalCizgiUzunlugu = 4f;
        return sonda;
    }

    private static GirdiOkuyucu GirdiOkuyucuOlustur()
    {
        GameObject girdiGO = new GameObject("GirdiOkuyucu");
        GirdiOkuyucu okuyucu = girdiGO.AddComponent<GirdiOkuyucu>();
        string[] guids = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            okuyucu.girdiVarligi = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
        okuyucu.ekrandaGoster = true;
        return okuyucu;
    }

    private static PhysicsMaterial SurtunmesizFizikMateryaliHazirla()
    {
        string yol = "Assets/Materials/SurtunmesizFizikMateryali.physicMaterial";
        PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(yol);
        if (mat == null)
        {
            mat = new PhysicsMaterial("SurtunmesizFizikMateryali")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Average
            };
            AssetDatabase.CreateAsset(mat, yol);
        }
        return mat;
    }

    private static Texture2D DokulariOlustur(string yol, Color anaRenk, Color ikincilRenk)
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(yol);
        if (existing != null) return existing;

        int genislik = 256, yukseklik = 256;
        Texture2D tex = new Texture2D(genislik, yukseklik, TextureFormat.RGBA32, true);
        for (int y = 0; y < yukseklik; y++)
            for (int x = 0; x < genislik; x++)
                tex.SetPixel(x, y, Color.Lerp(anaRenk, ikincilRenk, Mathf.PerlinNoise(x * 0.08f, y * 0.08f)));
        tex.Apply();
        File.WriteAllBytes(yol, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(yol, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(yol);
    }

    private static TerrainLayer KatmanOlustur(string yol, Texture2D diffuseTex, Vector2 tileSize)
    {
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(yol);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, yol);
        }
        layer.diffuseTexture = diffuseTex;
        layer.tileSize = tileSize;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static Terrain ArazisiOlustur(TerrainLayer grassLayer, TerrainLayer rockLayer)
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        string terrainDataPath = "Assets/TerrainData/AnaAraziVerisi.asset";
        TerrainData tData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);

        if (tData == null)
        {
            tData = new TerrainData();
            AssetDatabase.CreateAsset(tData, terrainDataPath);
        }

        if (terrain == null)
        {
            GameObject terrainGO = Terrain.CreateTerrainGameObject(tData);
            terrainGO.name = "Terrain_AnaArazi";
            terrain = terrainGO.GetComponent<Terrain>();
        }
        else
        {
            terrain.terrainData = tData;
        }

        tData.size = new Vector3(200f, 50f, 200f);
        terrain.transform.position = new Vector3(-100f, 0f, -100f);

        int hRes = 257;
        tData.heightmapResolution = hRes;
        float[,] heights = new float[hRes, hRes];

        for (int y = 0; y < hRes; y++)
        {
            float normY = (float)y / (hRes - 1);
            for (int x = 0; x < hRes; x++)
            {
                float normX = (float)x / (hRes - 1);
                float h = 0f;

                float tepeMesafe = Vector2.Distance(new Vector2(normX, normY), new Vector2(0.72f, 0.68f));
                if (tepeMesafe < 0.28f)
                {
                    float factor = 1f - (tepeMesafe / 0.28f);
                    h += Mathf.Pow(factor, 1.8f) * 0.70f;
                }

                bool duzlukIcinde = (normX >= 0.18f && normX <= 0.48f && normY >= 0.20f && normY <= 0.55f);
                if (duzlukIcinde)
                {
                    h = Mathf.Max(h, 0.36f);
                }
                else if (normX > 0.48f && normX <= 0.53f && normY >= 0.18f && normY <= 0.57f)
                {
                    float yamacT = (normX - 0.48f) / 0.05f;
                    h = Mathf.Max(h, Mathf.Lerp(0.36f, 0.02f, yamacT));
                }
                else
                {
                    float dx = normX < 0.18f ? 0.18f - normX : 0f;
                    float dy = normY < 0.20f ? 0.20f - normY : (normY > 0.55f ? normY - 0.55f : 0f);
                    float kenarMesafe = Mathf.Sqrt(dx * dx + dy * dy);
                    if (kenarMesafe > 0f && kenarMesafe < 0.12f)
                        h = Mathf.Max(h, (1f - (kenarMesafe / 0.12f)) * 0.36f);
                }

                h += Mathf.PerlinNoise(normX * 12f, normY * 12f) * 0.015f;
                heights[y, x] = Mathf.Clamp01(h);
            }
        }
        tData.SetHeights(0, 0, heights);

        tData.terrainLayers = new TerrainLayer[] { grassLayer, rockLayer };
        int aRes = 256;
        tData.alphamapResolution = aRes;
        float[,,] splatmap = new float[aRes, aRes, 2];

        for (int y = 0; y < aRes; y++)
        {
            float normY = (float)y / (aRes - 1);
            for (int x = 0; x < aRes; x++)
            {
                float normX = (float)x / (aRes - 1);
                Vector3 normal = tData.GetInterpolatedNormal(normX, normY);
                float egimAcisi = Vector3.Angle(Vector3.up, normal);
                float kayaOrani = 0f;
                if (egimAcisi >= 42f) kayaOrani = Mathf.Clamp01((egimAcisi - 35f) / 15f);
                else if (tData.GetInterpolatedHeight(normX, normY) > 28f)
                    kayaOrani = Mathf.Clamp01((tData.GetInterpolatedHeight(normX, normY) - 28f) / 7f);

                splatmap[y, x, 0] = 1f - kayaOrani;
                splatmap[y, x, 1] = kayaOrani;
            }
        }
        tData.SetAlphamaps(0, 0, splatmap);

        TerrainCollider tCollider = terrain.GetComponent<TerrainCollider>();
        if (tCollider == null) tCollider = terrain.gameObject.AddComponent<TerrainCollider>();
        tCollider.terrainData = tData;

        NavMeshSurface surface = terrain.GetComponent<NavMeshSurface>();
        if (surface == null) surface = terrain.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        return terrain;
    }

    private static GameObject AgacPrefabiOlustur()
    {
        string prefabYolu = "Assets/Prefabs/AgacPrefab.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabYolu);
        if (existing != null) return existing;

        Material govdeMat = MateryalAlVeyaOlustur("Assets/Materials/AgacGovdeMat.mat", new Color(0.38f, 0.22f, 0.10f));
        Material yaprakMat = MateryalAlVeyaOlustur("Assets/Materials/AgacYaprakMat.mat", new Color(0.12f, 0.45f, 0.15f));

        GameObject root = new GameObject("AgacPrefab");
        GameObject govde = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        govde.name = "Govde_Silindir";
        govde.transform.SetParent(root.transform, false);
        govde.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
        govde.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        govde.GetComponent<Renderer>().sharedMaterial = govdeMat;

        GameObject yapraklar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        yapraklar.name = "Yapraklar_Kure";
        yapraklar.transform.SetParent(root.transform, false);
        yapraklar.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        yapraklar.transform.localPosition = new Vector3(0f, 3.2f, 0f);
        yapraklar.GetComponent<Renderer>().sharedMaterial = yaprakMat;

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabYolu);
        Object.DestroyImmediate(root);
        return savedPrefab;
    }

    private static void AgaclariYerlestir(Terrain terrain, GameObject agacPrefab, int adet)
    {
        GameObject eskiAgaclar = GameObject.Find("Agaclar");
        if (eskiAgaclar != null) Object.DestroyImmediate(eskiAgaclar);

        GameObject agaclarRoot = new GameObject("Agaclar");
        Vector2[] uygunKonumlar =
        {
            new(-50, -40), new(-40, -35), new(-30, -45), new(-25, -20),
            new(-35, -15), new(-45, -25), new(-55, -10), new(-20, -30),
            new(-30, 0),   new(-40, 5),   new(-15, -40), new(-50, -50),
            new(10, -50),  new(20, -40),  new(0, -60),   new(-10, -70)
        };

        for (int i = 0; i < adet && i < uygunKonumlar.Length; i++)
        {
            Vector2 k = uygunKonumlar[i];
            float worldY = terrain.SampleHeight(new Vector3(k.x, 0, k.y)) + terrain.transform.position.y;
            GameObject agac = PrefabUtility.InstantiatePrefab(agacPrefab) as GameObject;
            agac.name = $"Agac_{i + 1}";
            agac.transform.position = new Vector3(k.x, worldY, k.y);
            agac.transform.SetParent(agaclarRoot.transform);
        }
    }

    private static Material MateryalAlVeyaOlustur(string yol, Color renk)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(yol);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { color = renk };
            AssetDatabase.CreateAsset(mat, yol);
        }
        else
        {
            mat.color = renk;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static Material UnlitMateryalAlVeyaOlustur(string yol, Color renk)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(yol);
        if (mat == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            mat = new Material(shader) { color = renk };
            AssetDatabase.CreateAsset(mat, yol);
        }
        return mat;
    }
}
