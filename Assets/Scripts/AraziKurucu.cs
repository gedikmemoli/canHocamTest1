using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

/// <summary>
/// Araziyi, ağaçları, tek oyuncu karakterini (Oyuncu_RB), NavMesh yüzeyini, duvarları ve
/// rota çizicili düşmanı (Ajan_Gozcu) hem Editörde hem Play modunda kuran bileşen.
/// </summary>
[ExecuteAlways]
public class AraziKurucu : MonoBehaviour
{
    [Header("Otomatik Kurulum")]
    public bool otomatikKur = true;
    [SerializeField] private bool kuruldu = false;

    private void Awake()
    {
        if (otomatikKur && (!kuruldu || Terrain.activeTerrain == null || GameObject.Find("Oyuncu_RB") == null))
        {
            KurulumuGerceklestir();
        }
    }

    private void Start()
    {
        if (otomatikKur && (!kuruldu || Terrain.activeTerrain == null || GameObject.Find("Oyuncu_RB") == null))
        {
            KurulumuGerceklestir();
        }
    }

    [ContextMenu("Araziyi ve Sahneyi Şimdi Kur")]
    public void KurulumuGerceklestir()
    {
        Debug.Log("[AraziKurucu] Sahne kurulumu ve kontrolleri gerçekleştiriliyor...");

        // 1. Terrain Bul veya Oluştur
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) terrain = Object.FindFirstObjectByType<Terrain>();

        TerrainData tData = null;
        if (terrain == null)
        {
            tData = new TerrainData();
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
                terrain.terrainData = tData;
            }
        }

        // Boyutlar 200 x 50 x 200 m, Pozisyon (-100, 0, -100)
        tData.size = new Vector3(200f, 50f, 200f);
        terrain.transform.position = new Vector3(-100f, 0f, -100f);

        // Heightmap: Tepe, Düzlük ve 45°'den dik yamaç
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

                // Tepe
                float tepeMesafe = Vector2.Distance(new Vector2(normX, normY), new Vector2(0.72f, 0.68f));
                if (tepeMesafe < 0.28f)
                {
                    float factor = 1f - (tepeMesafe / 0.28f);
                    h += Mathf.Pow(factor, 1.8f) * 0.70f;
                }

                // Düzlük
                bool duzlukIcinde = (normX >= 0.18f && normX <= 0.48f && normY >= 0.20f && normY <= 0.55f);
                if (duzlukIcinde)
                {
                    h = Mathf.Max(h, 0.36f);
                }
                else
                {
                    // 45°'den dik yamaç
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

                float dalga = Mathf.PerlinNoise(normX * 10f, normY * 10f) * 0.015f;
                h += dalga;
                heights[y, x] = Mathf.Clamp01(h);
            }
        }
        tData.SetHeights(0, 0, heights);

        // İki doku katmanı
        Texture2D grassTex = DokuyuYukleVeyaUret(new Color(0.25f, 0.55f, 0.20f), new Color(0.35f, 0.65f, 0.25f));
        Texture2D rockTex = DokuyuYukleVeyaUret(new Color(0.45f, 0.42f, 0.40f), new Color(0.60f, 0.58f, 0.55f));
        tData.terrainLayers = new TerrainLayer[]
        {
            new TerrainLayer { diffuseTexture = grassTex, tileSize = new Vector2(10, 10) },
            new TerrainLayer { diffuseTexture = rockTex, tileSize = new Vector2(10, 10) }
        };

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

        // Ağaçlar
        AgaclariKur(terrain);

        // Girdi Okuyucu
        GirdiOkuyucu okuyucu = Object.FindFirstObjectByType<GirdiOkuyucu>();
        if (okuyucu == null)
        {
            GameObject g = new GameObject("GirdiOkuyucu");
            okuyucu = g.AddComponent<GirdiOkuyucu>();
        }

        // Arazi Sondası
        AraziSondasi sonda = Object.FindFirstObjectByType<AraziSondasi>();
        if (sonda != null) sonda.hedefArazi = terrain;

        // Karakter Başlangıç Noktası
        Vector3 baslangicRB = new Vector3(-22f, 0f, -20f);
        baslangicRB.y = terrain.SampleHeight(baslangicRB) + terrain.transform.position.y + 0.1f;

        // 1. Karakteri Bire Düşür: Eski Oyuncu_CC nesnesi varsa kaldır
        GameObject eskiCC = GameObject.Find("Oyuncu_CC");
        if (eskiCC != null)
        {
            DestroyImmediate(eskiCC);
        }

        // 2. Tek Yönetilebilir Oyuncu: Oyuncu_RB
        GameObject oyuncuRB = GameObject.Find("Oyuncu_RB");
        if (oyuncuRB == null)
        {
            oyuncuRB = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            oyuncuRB.name = "Oyuncu_RB";
            oyuncuRB.transform.position = baslangicRB;

            int pLayer = LayerMask.NameToLayer("Player");
            if (pLayer != -1) oyuncuRB.layer = pLayer;

            CapsuleCollider cCol = oyuncuRB.GetComponent<CapsuleCollider>();
            if (cCol == null) cCol = oyuncuRB.AddComponent<CapsuleCollider>();
            cCol.height = 2f;
            cCol.radius = 0.5f;
            cCol.center = new Vector3(0f, 1f, 0f);

            PhysicsMaterial pMat = new PhysicsMaterial("SurtunmesizFizikMateryali")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            cCol.sharedMaterial = pMat;

            Rigidbody rb = oyuncuRB.GetComponent<Rigidbody>();
            if (rb == null) rb = oyuncuRB.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            RBHareket rbH = oyuncuRB.AddComponent<RBHareket>();
            rbH.girdiOkuyucu = okuyucu;
            if (Camera.main != null) rbH.kameraTransform = Camera.main.transform;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            oyuncuRB.GetComponent<Renderer>().sharedMaterial = new Material(s) { color = new Color(0.2f, 0.55f, 0.95f) };
        }

        // Takip Kamerası: Hedef Oyuncu_RB
        if (Camera.main != null)
        {
            TakipKamerasi tk = Camera.main.GetComponent<TakipKamerasi>();
            if (tk == null) tk = Camera.main.gameObject.AddComponent<TakipKamerasi>();
            tk.hedef = oyuncuRB.transform;
            tk.girdiOkuyucu = okuyucu;
        }

        // Karşılaştırma Nesneleri
        if (GameObject.Find("ItilebilirKup") == null)
        {
            Vector3 kupPos = baslangicRB + new Vector3(0f, 0.6f, 3.5f);
            kupPos.y = terrain.SampleHeight(kupPos) + terrain.transform.position.y + 0.6f;

            GameObject kup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kup.name = "ItilebilirKup";
            kup.transform.position = kupPos;
            kup.transform.localScale = Vector3.one * 1.2f;

            Rigidbody kupRb = kup.AddComponent<Rigidbody>();
            kupRb.mass = 15f;
            kupRb.interpolation = RigidbodyInterpolation.Interpolate;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            kup.GetComponent<Renderer>().sharedMaterial = new Material(s) { color = new Color(0.95f, 0.60f, 0.10f) };
        }

        if (GameObject.Find("Basamak_025m") == null)
        {
            Vector3 b25Pos = baslangicRB + new Vector3(-8f, 0f, 6f);
            b25Pos.y = terrain.SampleHeight(b25Pos) + terrain.transform.position.y + 0.125f;

            GameObject b025 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b025.name = "Basamak_025m";
            b025.transform.position = b25Pos;
            b025.transform.localScale = new Vector3(3.5f, 0.25f, 2.5f);

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            b025.GetComponent<Renderer>().sharedMaterial = new Material(s) { color = new Color(0.70f, 0.72f, 0.75f) };
        }

        if (GameObject.Find("Basamak_050m") == null)
        {
            Vector3 b50Pos = baslangicRB + new Vector3(-8f, 0f, 9.5f);
            b50Pos.y = terrain.SampleHeight(b50Pos) + terrain.transform.position.y + 0.25f;

            GameObject b050 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b050.name = "Basamak_050m";
            b050.transform.position = b50Pos;
            b050.transform.localScale = new Vector3(3.5f, 0.50f, 2.5f);

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            b050.GetComponent<Renderer>().sharedMaterial = new Material(s) { color = new Color(0.70f, 0.72f, 0.75f) };
        }

        // 3. Test Duvarları (Koridor ve Köşe)
        Shader shd = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material duvarMat = new Material(shd) { color = new Color(0.40f, 0.42f, 0.46f) };

        if (GameObject.Find("Duvar_1") == null)
        {
            GameObject d1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            d1.name = "Duvar_1";
            Vector3 p1 = baslangicRB + new Vector3(-3f, 0f, 5.5f);
            p1.y = terrain.SampleHeight(p1) + terrain.transform.position.y + 1.5f;
            d1.transform.position = p1;
            d1.transform.localScale = new Vector3(1f, 3f, 9f);
            d1.GetComponent<Renderer>().sharedMaterial = duvarMat;
        }

        if (GameObject.Find("Duvar_2") == null)
        {
            GameObject d2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            d2.name = "Duvar_2";
            Vector3 p2 = baslangicRB + new Vector3(1.5f, 0f, 9.5f);
            p2.y = terrain.SampleHeight(p2) + terrain.transform.position.y + 1.5f;
            d2.transform.position = p2;
            d2.transform.localScale = new Vector3(8f, 3f, 1f);
            d2.GetComponent<Renderer>().sharedMaterial = duvarMat;
        }

        if (GameObject.Find("Duvar_3") == null)
        {
            GameObject d3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            d3.name = "Duvar_3";
            Vector3 p3 = baslangicRB + new Vector3(4.5f, 0f, 4f);
            p3.y = terrain.SampleHeight(p3) + terrain.transform.position.y + 1.5f;
            d3.transform.position = p3;
            d3.transform.localScale = new Vector3(1f, 3f, 7f);
            d3.GetComponent<Renderer>().sharedMaterial = duvarMat;
        }

        // 4. NavMeshSurface İnşa Et (Bake)
        NavMeshSurface surface = terrain.GetComponent<NavMeshSurface>();
        if (surface == null) surface = terrain.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        // 5. Ajan_Gozcu (NavMeshAgent + Mor Çizgi)
        GameObject gozcu = GameObject.Find("Ajan_Gozcu");
        if (gozcu == null)
        {
            Vector3 gozcuPos = baslangicRB + new Vector3(0f, 0f, 14f);
            gozcuPos.y = terrain.SampleHeight(gozcuPos) + terrain.transform.position.y + 1f;

            gozcu = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            gozcu.name = "Ajan_Gozcu";
            gozcu.transform.position = gozcuPos;
            gozcu.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            gozcu.GetComponent<Renderer>().sharedMaterial = new Material(s) { color = new Color(0.95f, 0.75f, 0.15f) };

            GameObject burun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            burun.name = "Burun";
            burun.transform.SetParent(gozcu.transform, false);
            burun.transform.localScale = new Vector3(0.25f, 0.2f, 0.5f);
            burun.transform.localPosition = new Vector3(0f, 0.6f, 0.55f);
            DestroyImmediate(burun.GetComponent<BoxCollider>());
            burun.GetComponent<Renderer>().sharedMaterial = new Material(s) { color = new Color(0.15f, 0.15f, 0.15f) };

            NavMeshAgent ag = gozcu.AddComponent<NavMeshAgent>();
            ag.speed = 4.5f;
            ag.stoppingDistance = 1.6f;
            ag.angularSpeed = 240f;
            ag.acceleration = 14f;

            Gozcu gozcuComp = gozcu.AddComponent<Gozcu>();
            gozcuComp.menzil = 18f;
            gozcuComp.gorusAcisi = 90f;
            gozcuComp.dikeyGorusAcisi = 50f;
            gozcuComp.gozYuksekligi = 0.6f;
            gozcuComp.kovalamaHizi = 4.5f;
            gozcuComp.hedefOyuncu = oyuncuRB.transform;

            int pLayer = LayerMask.NameToLayer("Player");
            int igRay = LayerMask.NameToLayer("Ignore Raycast");
            int mask = 0;
            if (pLayer != -1) mask |= (1 << pLayer);
            if (igRay != -1) mask |= (1 << igRay);
            gozcuComp.engelMaskesi = ~mask;
        }

        kuruldu = true;
        Debug.Log("[AraziKurucu] Tüm nesneler, NavMesh ve düşman takibi başarıyla kuruldu!");
    }

    private Texture2D DokuyuYukleVeyaUret(Color anaRenk, Color ikincilRenk)
    {
        Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, true);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                tex.SetPixel(x, y, Color.Lerp(anaRenk, ikincilRenk, noise));
            }
        }
        tex.Apply();
        return tex;
    }

    private void AgaclariKur(Terrain terrain)
    {
        if (GameObject.Find("Agaclar") != null) return;

        GameObject agaclarRoot = new GameObject("Agaclar");

        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material govdeMat = new Material(standardShader) { color = new Color(0.38f, 0.22f, 0.10f) };
        Material yaprakMat = new Material(standardShader) { color = new Color(0.12f, 0.45f, 0.15f) };

        Vector2[] konumlar = new Vector2[]
        {
            new Vector2(-50, -40), new Vector2(-40, -35), new Vector2(-30, -45),
            new Vector2(-25, -20), new Vector2(-35, -15), new Vector2(-45, -25),
            new Vector2(-55, -10), new Vector2(-20, -30), new Vector2(-30, 0),
            new Vector2(-40, 5),   new Vector2(-15, -40), new Vector2(-50, -50),
            new Vector2(10, -50),  new Vector2(20, -40),  new Vector2(0, -60),
            new Vector2(-10, -70)
        };

        for (int i = 0; i < konumlar.Length; i++)
        {
            Vector2 k = konumlar[i];
            float worldX = k.x;
            float worldZ = k.y;
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrain.transform.position.y;

            GameObject agac = new GameObject($"Agac_{i + 1}");
            agac.transform.position = new Vector3(worldX, worldY, worldZ);
            agac.transform.SetParent(agaclarRoot.transform);

            GameObject govde = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            govde.name = "Govde_Silindir";
            govde.transform.SetParent(agac.transform, false);
            govde.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
            govde.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            govde.GetComponent<Renderer>().sharedMaterial = govdeMat;

            GameObject yapraklar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            yapraklar.name = "Yapraklar_Kure";
            yapraklar.transform.SetParent(agac.transform, false);
            yapraklar.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            yapraklar.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            yapraklar.GetComponent<Renderer>().sharedMaterial = yaprakMat;
        }
    }
}
