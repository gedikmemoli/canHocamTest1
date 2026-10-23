using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Tüm sahne ve yapay zeka gereksinimlerini otomatik kuran ve yapılandıran Editor aracı:
/// 1. Tek yönetilebilir oyuncu bırakır (Oyuncu_CC silinir, sadece Oyuncu_RB kalır).
/// 2. NavMeshSurface ekleyip arazi ve engelleri tarayarak NavMesh yüzeyi inşa eder.
/// 3. Düşman (Ajan_Gozcu) NavMeshAgent ile oyuncuyu takip eder ve rotasını mor çizgiyle çizer.
/// 4. Düşmanın yol bulmasını test etmek için koridor ve L köşesi oluşturan fiziksel duvarlar ekler.
/// </summary>
[InitializeOnLoad]
public class SetupTerrainAndScene : EditorWindow
{
    private const string KURULUM_TAMAMLANDI_KEY = "SetupTerrainAndScene_V3_NavMesh_Completed";

    static SetupTerrainAndScene()
    {
        EditorApplication.delayCall += OtomatikCalistir;
    }

    private static void OtomatikCalistir()
    {
        if (!SessionState.GetBool(KURULUM_TAMAMLANDI_KEY, false))
        {
            SessionState.SetBool(KURULUM_TAMAMLANDI_KEY, true);
            Debug.Log("[SetupTerrainAndScene] Otomatik kurulum tetiklendi...");
            TumunuKur();
        }
    }

    [MenuItem("Tools/Sahneyi ve Nesneleri Kur (Tam Kurulum)")]
    public static void TumunuKur()
    {
        Debug.Log(">>> [KURULUM] Başlatılıyor...");

        // 1. Dokuları ve Materyalleri Hazırla
        Texture2D grassTex = DokulariOlustur("Assets/Textures/CimenDoku.png", new Color(0.25f, 0.55f, 0.20f), new Color(0.35f, 0.65f, 0.25f));
        Texture2D rockTex = DokulariOlustur("Assets/Textures/KayaDoku.png", new Color(0.45f, 0.42f, 0.40f), new Color(0.60f, 0.58f, 0.55f));
        TerrainLayer grassLayer = KatmanOlustur("Assets/TerrainData/CimenKatmani.terrainlayer", grassTex, new Vector2(10, 10));
        TerrainLayer rockLayer = KatmanOlustur("Assets/TerrainData/KayaKatmani.terrainlayer", rockTex, new Vector2(10, 10));
        PhysicsMaterial surtunmesizFizikMat = SurtunmesizFizikMateryaliHazirla();

        // 2. Terrain ve TerrainData Kurulumu
        Terrain terrain = ArazisiOlustur(grassLayer, rockLayer);

        // 3. Ağaç Prefab'ı ve Ağaçlar
        GameObject agacPrefab = AgacPrefabiOlustur();
        AgaclariYerlestir(terrain, agacPrefab, 16);

        // 4. AraziSondasi ve GirdiOkuyucu
        AraziSondasiOlustur(terrain);
        GirdiOkuyucu girdiOkuyucu = GirdiOkuyucuOlustur();

        // 5. Karakteri Bire Düşür: Oyuncu_CC nesnesini sil
        GameObject eskiCC = GameObject.Find("Oyuncu_CC");
        if (eskiCC != null)
        {
            Object.DestroyImmediate(eskiCC);
            Debug.Log("[Kurulum] Oyuncu_CC silindi. Tek yönetilebilir karakter olarak Oyuncu_RB bırakıldı.");
        }

        // 6. Tek Yönetilebilir Oyuncu: Oyuncu_RB
        Vector3 baslangicNoktasiRB = new Vector3(-22f, 0f, -20f);
        baslangicNoktasiRB.y = terrain.SampleHeight(baslangicNoktasiRB) + terrain.transform.position.y + 0.1f;
        GameObject oyuncuRB = OyuncuRBOlustur(baslangicNoktasiRB, girdiOkuyucu, surtunmesizFizikMat);

        // 7. Ana Kameraya TakipKamerasi Ekle ve Hedefi Oyuncu_RB Yap
        TakipKamerasiniAyarla(oyuncuRB.transform, girdiOkuyucu);

        // 8. Karşılaştırma Nesneleri: İtilebilir Küp ve Basamaklar
        KarsilastirmaNesneleriniOlustur(terrain, baslangicNoktasiRB);

        // 9. Düşmanın Yol Bulmasını Test Edecek Duvarlar
        DuvarlariOlustur(terrain, baslangicNoktasiRB);

        // 10. NavMeshSurface Ekle ve Yüzeyi İnşa Et (Bake)
        NavMeshYuzeyiniInsaEt(terrain);

        // 11. Düşman (Ajan_Gozcu): NavMeshAgent + Mor LineRenderer Rota Çizici
        AjanGozcuOlustur(terrain, baslangicNoktasiRB, oyuncuRB.transform);

        // Sahneyi Kaydet
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log(">>> [KURULUM] NavMesh ve tüm yeni özellikler başarıyla kuruldu ve sahne kaydedildi!");
    }

    private static void NavMeshYuzeyiniInsaEt(Terrain terrain)
    {
        NavMeshSurface surface = terrain.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = terrain.gameObject.AddComponent<NavMeshSurface>();
        }

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.defaultArea = 0; // Walkable

        // NavMesh yüzeyini anında bake et
        surface.BuildNavMesh();
        Debug.Log("[Kurulum] NavMeshSurface başarıyla bake edildi.");
    }

    private static void DuvarlariOlustur(Terrain terrain, Vector3 oyuncuKonum)
    {
        Material duvarMat = MateryalAlVeyaOlustur("Assets/Materials/DuvarMat.mat", new Color(0.40f, 0.42f, 0.46f));

        // 1. Ana Engel Duvarı (Düşman ile oyuncu arasına koridor)
        GameObject d1 = GameObject.Find("Duvar_1");
        if (d1 != null) Object.DestroyImmediate(d1);
        d1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        d1.name = "Duvar_1";
        Vector3 pos1 = oyuncuKonum + new Vector3(-3f, 0f, 5.5f);
        pos1.y = terrain.SampleHeight(pos1) + terrain.transform.position.y + 1.5f;
        d1.transform.position = pos1;
        d1.transform.localScale = new Vector3(1f, 3f, 9f);
        d1.GetComponent<Renderer>().sharedMaterial = duvarMat;

        // 2. L Köşesi Duvarı (Arkasına saklanılabilecek köşe)
        GameObject d2 = GameObject.Find("Duvar_2");
        if (d2 != null) Object.DestroyImmediate(d2);
        d2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        d2.name = "Duvar_2";
        Vector3 pos2 = oyuncuKonum + new Vector3(1.5f, 0f, 9.5f);
        pos2.y = terrain.SampleHeight(pos2) + terrain.transform.position.y + 1.5f;
        d2.transform.position = pos2;
        d2.transform.localScale = new Vector3(8f, 3f, 1f);
        d2.GetComponent<Renderer>().sharedMaterial = duvarMat;

        // 3. İkinci Paralel Duvar (Dar geçit / labirent hissi)
        GameObject d3 = GameObject.Find("Duvar_3");
        if (d3 != null) Object.DestroyImmediate(d3);
        d3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        d3.name = "Duvar_3";
        Vector3 pos3 = oyuncuKonum + new Vector3(4.5f, 0f, 4f);
        pos3.y = terrain.SampleHeight(pos3) + terrain.transform.position.y + 1.5f;
        d3.transform.position = pos3;
        d3.transform.localScale = new Vector3(1f, 3f, 7f);
        d3.GetComponent<Renderer>().sharedMaterial = duvarMat;
    }

    private static void AjanGozcuOlustur(Terrain terrain, Vector3 oyuncuKonum, Transform hedefOyuncu)
    {
        GameObject eskiGozcu = GameObject.Find("Ajan_Gozcu");
        if (eskiGozcu != null) Object.DestroyImmediate(eskiGozcu);

        // Ajanın konumu: Duvarların ötesinde, 14 metre mesafede
        Vector3 gozcuKonum = oyuncuKonum + new Vector3(0f, 0f, 14f);
        gozcuKonum.y = terrain.SampleHeight(gozcuKonum) + terrain.transform.position.y + 1f;

        GameObject gozcu = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        gozcu.name = "Ajan_Gozcu";
        gozcu.transform.position = gozcuKonum;
        gozcu.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Material gozcuMat = MateryalAlVeyaOlustur("Assets/Materials/GozcuMat.mat", new Color(0.95f, 0.75f, 0.15f));
        gozcu.GetComponent<Renderer>().sharedMaterial = gozcuMat;

        // Burun
        GameObject burun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        burun.name = "Burun";
        burun.transform.SetParent(gozcu.transform, false);
        burun.transform.localScale = new Vector3(0.25f, 0.2f, 0.5f);
        burun.transform.localPosition = new Vector3(0f, 0.6f, 0.55f);
        BoxCollider burunCol = burun.GetComponent<BoxCollider>();
        if (burunCol != null) Object.DestroyImmediate(burunCol);
        Material burunMat = MateryalAlVeyaOlustur("Assets/Materials/BurunMat.mat", new Color(0.15f, 0.15f, 0.15f));
        burun.GetComponent<Renderer>().sharedMaterial = burunMat;

        // NavMeshAgent ekle
        NavMeshAgent agent = gozcu.GetComponent<NavMeshAgent>();
        if (agent == null) agent = gozcu.AddComponent<NavMeshAgent>();
        agent.speed = 4.5f;
        agent.stoppingDistance = 1.6f;
        agent.angularSpeed = 240f;
        agent.acceleration = 14f;

        // Gozcu bileşenini ekle
        Gozcu gozcuBileseni = gozcu.AddComponent<Gozcu>();
        gozcuBileseni.menzil = 18f; // Duvarların etrafını da görebilmesi için menzil hafif artırıldı
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

        EditorUtility.SetDirty(gozcu);
    }

    private static PhysicsMaterial SurtunmesizFizikMateryaliHazirla()
    {
        string yol = "Assets/Materials/SurtunmesizFizikMateryali.physicMaterial";
        PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(yol);
        if (mat == null)
        {
            mat = new PhysicsMaterial("SurtunmesizFizikMateryali");
            mat.dynamicFriction = 0f;
            mat.staticFriction = 0f;
            mat.bounciness = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            mat.bounceCombine = PhysicsMaterialCombine.Average;
            AssetDatabase.CreateAsset(mat, yol);
        }
        else
        {
            mat.dynamicFriction = 0f;
            mat.staticFriction = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static Texture2D DokulariOlustur(string yol, Color anaRenk, Color ikincilRenk)
    {
        int genislik = 256;
        int yukseklik = 256;
        Texture2D tex = new Texture2D(genislik, yukseklik, TextureFormat.RGBA32, true);

        for (int y = 0; y < yukseklik; y++)
        {
            for (int x = 0; x < genislik; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                Color c = Color.Lerp(anaRenk, ikincilRenk, noise);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(yol, bytes);
        AssetDatabase.ImportAsset(yol, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(yol) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

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
        AssetDatabase.SaveAssets();
        return layer;
    }

    private static Terrain ArazisiOlustur(TerrainLayer grassLayer, TerrainLayer rockLayer)
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        TerrainData tData = null;
        string terrainDataPath = "Assets/TerrainData/AnaAraziVerisi.asset";

        if (terrain == null)
        {
            tData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);
            if (tData == null)
            {
                tData = new TerrainData();
                AssetDatabase.CreateAsset(tData, terrainDataPath);
            }

            GameObject terrainGO = Terrain.CreateTerrainGameObject(tData);
            terrainGO.name = "Terrain_AnaArazi";
            terrain = terrainGO.GetComponent<Terrain>();
        }
        else
        {
            tData = terrain.terrainData;
            if (tData == null)
            {
                tData = new TerrainData();
                AssetDatabase.CreateAsset(tData, terrainDataPath);
                terrain.terrainData = tData;
            }
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
                else
                {
                    if (normX > 0.48f && normX <= 0.53f && normY >= 0.18f && normY <= 0.57f)
                    {
                        float yamaçT = (normX - 0.48f) / 0.05f;
                        float yamaçYukseklik = Mathf.Lerp(0.36f, 0.02f, yamaçT);
                        h = Mathf.Max(h, yamaçYukseklik);
                    }
                    else
                    {
                        float dx = 0f;
                        if (normX < 0.18f) dx = 0.18f - normX;
                        float dy = 0f;
                        if (normY < 0.20f) dy = 0.20f - normY;
                        else if (normY > 0.55f) dy = normY - 0.55f;

                        float kenarMesafe = Mathf.Sqrt(dx * dx + dy * dy);
                        if (kenarMesafe > 0f && kenarMesafe < 0.12f)
                        {
                            float factor = 1f - (kenarMesafe / 0.12f);
                            h = Mathf.Max(h, factor * 0.36f);
                        }
                    }
                }

                float dalga = Mathf.PerlinNoise(normX * 12f, normY * 12f) * 0.015f;
                h += dalga;
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
                if (egimAcisi >= 42f)
                {
                    kayaOrani = Mathf.Clamp01((egimAcisi - 35f) / 15f);
                }
                else if (tData.GetInterpolatedHeight(normX, normY) > 28f)
                {
                    kayaOrani = Mathf.Clamp01((tData.GetInterpolatedHeight(normX, normY) - 28f) / 7f);
                }

                splatmap[y, x, 0] = 1f - kayaOrani;
                splatmap[y, x, 1] = kayaOrani;
            }
        }
        tData.SetAlphamaps(0, 0, splatmap);

        TerrainCollider tCollider = terrain.GetComponent<TerrainCollider>();
        if (tCollider == null) tCollider = terrain.gameObject.AddComponent<TerrainCollider>();
        tCollider.terrainData = tData;

        EditorUtility.SetDirty(tData);
        EditorUtility.SetDirty(terrain);
        return terrain;
    }

    private static GameObject AgacPrefabiOlustur()
    {
        string prefabYolu = "Assets/Prefabs/AgacPrefab.prefab";
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
        AssetDatabase.SaveAssets();
        return savedPrefab;
    }

    private static void AgaclariYerlestir(Terrain terrain, GameObject agacPrefab, int adet)
    {
        GameObject eskiAgaclar = GameObject.Find("Agaclar");
        if (eskiAgaclar != null) Object.DestroyImmediate(eskiAgaclar);

        GameObject agaclarRoot = new GameObject("Agaclar");

        Vector2[] uygunKonumlar = new Vector2[]
        {
            new Vector2(-50, -40), new Vector2(-40, -35), new Vector2(-30, -45),
            new Vector2(-25, -20), new Vector2(-35, -15), new Vector2(-45, -25),
            new Vector2(-55, -10), new Vector2(-20, -30), new Vector2(-30, 0),
            new Vector2(-40, 5),   new Vector2(-15, -40), new Vector2(-50, -50),
            new Vector2(10, -50),  new Vector2(20, -40),  new Vector2(0, -60),
            new Vector2(-10, -70)
        };

        for (int i = 0; i < adet && i < uygunKonumlar.Length; i++)
        {
            Vector2 k = uygunKonumlar[i];
            float worldX = k.x;
            float worldZ = k.y;
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrain.transform.position.y;

            GameObject agac = PrefabUtility.InstantiatePrefab(agacPrefab) as GameObject;
            agac.name = $"Agac_{i + 1}";
            agac.transform.position = new Vector3(worldX, worldY, worldZ);
            agac.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            agac.transform.localScale = Vector3.one * Random.Range(0.85f, 1.25f);
            agac.transform.SetParent(agaclarRoot.transform);
        }
    }

    private static void AraziSondasiOlustur(Terrain terrain)
    {
        GameObject sondaGO = GameObject.Find("AraziSondasi");
        if (sondaGO == null) sondaGO = new GameObject("AraziSondasi");

        sondaGO.transform.position = new Vector3(4f, 25f, -20f);
        AraziSondasi sonda = sondaGO.GetComponent<AraziSondasi>();
        if (sonda == null) sonda = sondaGO.AddComponent<AraziSondasi>();

        sonda.hedefArazi = terrain;
        sonda.nesnePozisyonunuKullan = true;
        sonda.normalCizgiUzunlugu = 4f;
        EditorUtility.SetDirty(sondaGO);
    }

    private static GirdiOkuyucu GirdiOkuyucuOlustur()
    {
        GameObject girdiGO = GameObject.Find("GirdiOkuyucu");
        if (girdiGO == null) girdiGO = new GameObject("GirdiOkuyucu");

        GirdiOkuyucu okuyucu = girdiGO.GetComponent<GirdiOkuyucu>();
        if (okuyucu == null) okuyucu = girdiGO.AddComponent<GirdiOkuyucu>();

        string[] guids = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            okuyucu.girdiVarligi = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        okuyucu.ekrandaGoster = true;
        EditorUtility.SetDirty(girdiGO);
        return okuyucu;
    }

    private static GameObject OyuncuRBOlustur(Vector3 konum, GirdiOkuyucu girdiOkuyucu, PhysicsMaterial surtunmesizMat)
    {
        GameObject oyuncu = GameObject.Find("Oyuncu_RB");
        if (oyuncu != null) Object.DestroyImmediate(oyuncu);

        oyuncu = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        oyuncu.name = "Oyuncu_RB";
        oyuncu.transform.position = konum;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1) oyuncu.layer = playerLayer;

        CapsuleCollider col = oyuncu.GetComponent<CapsuleCollider>();
        if (col == null) col = oyuncu.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.5f;
        col.center = new Vector3(0f, 1f, 0f);
        col.sharedMaterial = surtunmesizMat;

        Rigidbody rb = oyuncu.GetComponent<Rigidbody>();
        if (rb == null) rb = oyuncu.AddComponent<Rigidbody>();
        rb.mass = 70f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        RBHareket rbHareket = oyuncu.AddComponent<RBHareket>();
        rbHareket.girdiOkuyucu = girdiOkuyucu;
        if (Camera.main != null) rbHareket.kameraTransform = Camera.main.transform;

        Material rbMat = MateryalAlVeyaOlustur("Assets/Materials/OyuncuRBMat.mat", new Color(0.2f, 0.55f, 0.95f)); // Şık mavi
        oyuncu.GetComponent<Renderer>().sharedMaterial = rbMat;

        EditorUtility.SetDirty(oyuncu);
        return oyuncu;
    }

    private static void TakipKamerasiniAyarla(Transform hedef, GirdiOkuyucu girdiOkuyucu)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        TakipKamerasi tk = cam.GetComponent<TakipKamerasi>();
        if (tk == null) tk = cam.gameObject.AddComponent<TakipKamerasi>();

        tk.hedef = hedef;
        tk.girdiOkuyucu = girdiOkuyucu;
        tk.mesafe = 5.5f;
        tk.hedefOfset = new Vector3(0f, 1.6f, 0f);

        EditorUtility.SetDirty(cam.gameObject);
    }

    private static void KarsilastirmaNesneleriniOlustur(Terrain terrain, Vector3 oyuncuKonum)
    {
        GameObject eskiKup = GameObject.Find("ItilebilirKup");
        if (eskiKup != null) Object.DestroyImmediate(eskiKup);

        Vector3 kupKonum = oyuncuKonum + new Vector3(0f, 0f, 3.5f);
        kupKonum.y = terrain.SampleHeight(kupKonum) + terrain.transform.position.y + 0.6f;

        GameObject kup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kup.name = "ItilebilirKup";
        kup.transform.position = kupKonum;
        kup.transform.localScale = Vector3.one * 1.2f;

        Rigidbody kupRb = kup.AddComponent<Rigidbody>();
        kupRb.mass = 15f;
        kupRb.interpolation = RigidbodyInterpolation.Interpolate;

        Material kupMat = MateryalAlVeyaOlustur("Assets/Materials/ItilebilirKupMat.mat", new Color(0.95f, 0.60f, 0.10f));
        kup.GetComponent<Renderer>().sharedMaterial = kupMat;

        // 0.25m Basamak
        GameObject eskiB25 = GameObject.Find("Basamak_025m");
        if (eskiB25 != null) Object.DestroyImmediate(eskiB25);

        Vector3 b25Konum = oyuncuKonum + new Vector3(-8f, 0f, 6f);
        float zeminY25 = terrain.SampleHeight(b25Konum) + terrain.transform.position.y;
        b25Konum.y = zeminY25 + (0.25f / 2f);

        GameObject basamak025 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basamak025.name = "Basamak_025m";
        basamak025.transform.position = b25Konum;
        basamak025.transform.localScale = new Vector3(3.5f, 0.25f, 2.5f);

        Material basamakMat = MateryalAlVeyaOlustur("Assets/Materials/BasamakMat.mat", new Color(0.70f, 0.72f, 0.75f));
        basamak025.GetComponent<Renderer>().sharedMaterial = basamakMat;

        // 0.50m Basamak
        GameObject eskiB50 = GameObject.Find("Basamak_050m");
        if (eskiB50 != null) Object.DestroyImmediate(eskiB50);

        Vector3 b50Konum = oyuncuKonum + new Vector3(-8f, 0f, 9.5f);
        float zeminY50 = terrain.SampleHeight(b50Konum) + terrain.transform.position.y;
        b50Konum.y = zeminY50 + (0.50f / 2f);

        GameObject basamak050 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basamak050.name = "Basamak_050m";
        basamak050.transform.position = b50Konum;
        basamak050.transform.localScale = new Vector3(3.5f, 0.50f, 2.5f);
        basamak050.GetComponent<Renderer>().sharedMaterial = basamakMat;
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
}
