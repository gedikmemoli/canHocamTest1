using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gelişmiş Gözcü ve Düşman Yapay Zekası:
/// 1. NavMesh sistemi ile oyuncuyu akıllıca takip eder.
/// 2. Rotasını (NavMeshPath) Game ve Scene ekranlarında mor bir çizgi (LineRenderer & Gizmos) ile çizer.
/// 3. Başlangıç yönünden sağa-sola 60'ar derece sınırlar içinde 30°/sn hızla tarama yapar.
/// 4. Dikey ve yatay görüş konisi ile oyuncuyu tespit eder (Vector3.Angle kullanmadan, dot product ile).
/// 5. Görüş hattını Physics.Linecast ile denetler (Player katmanı hariç).
/// 6. Oyuncu görüldüğünde kırmızı, görülmediğinde yeşil materyal rengi alır.
/// 7. Çapraz çarpım (Cross Product) Y bileşeni ile oyuncunun sağda mı solda mı olduğunu Console'a yazar.
/// 8. Görüş hattı kesilirse son görülen konuma gider; 2 saniye bulamazsa devriyeye döner.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Gozcu : MonoBehaviour
{
    [Header("Gözcü Algı Parametreleri")]
    [Tooltip("Gözcünün azami görüş menzili (metre).")]
    public float menzil = 15f;

    [Tooltip("Gözcünün toplam yatay görüş açısı (derece).")]
    [Range(10f, 180f)]
    public float gorusAcisi = 90f;

    [Tooltip("Gözcünün toplam dikey görüş açısı (derece). Tepedeki oyuncuların tespiti için limit.")]
    [Range(10f, 120f)]
    public float dikeyGorusAcisi = 50f;

    [Tooltip("Gözcünün göz hizası yüksekliği (metre).")]
    public float gozYuksekligi = 0.6f;

    [Header("NavMesh Takip ve Hız Ayarları")]
    [Tooltip("Düşmanın oyuncuyu kovalama hızı (m/s).")]
    public float kovalamaHizi = 4.5f;

    [Tooltip("Çevre taraması esnasındaki açısal dönüş hızı (derece/saniye).")]
    public float taramaHizi = 30f;

    [Tooltip("Görüş hattı kesildikten sonra çevre taramasına dönmek için beklenecek süre (saniye).")]
    public float kaybetmeZamanAsimi = 2.0f;

    [Header("Rota Görselleştirme (LineRenderer)")]
    [Tooltip("Düşmanın NavMesh rotasını çizecek mor renk.")]
    public Color rotaCizgiRengi = new Color(0.75f, 0.1f, 0.95f, 1f); // Mor
    public float cizgiKalinligi = 0.15f;

    [Header("Engel ve Katman Ayarları")]
    [Tooltip("Görüş hattını engelleyen katmanlar. Player katmanı hariç tutulmalıdır.")]
    public LayerMask engelMaskesi;

    [Header("Hedef Oyuncu")]
    public Transform hedefOyuncu;

    [Header("Canlı Durum ve Algı Sonuçları (Salt Okunur)")]
    [SerializeField] private bool gorusKonisinde;
    [SerializeField] private bool gorusHattiAcik;
    [SerializeField] private bool oyuncuGoruluyor;
    [SerializeField] private bool takipModuAktif;
    [SerializeField] private float hedefeMesafe;
    [SerializeField] private float hedefeYatayAci;
    [SerializeField] private float hedefeDikeyAci;
    [SerializeField] private float noktaCarpimDegeri;
    [SerializeField] private float caprazCarpimY;
    [SerializeField] private string oyuncuYonKonumu = "Bilinmiyor";
    [SerializeField] private Vector3 sonGorulenKonum;

    public bool GorusKonisindeMi => gorusKonisinde;
    public bool GorusHattiAcikMi => gorusHattiAcik;
    public bool OyuncuGoruluyorMu => oyuncuGoruluyor;
    public bool TakipModuAktifMi => takipModuAktif;
    public Vector3 GozPozisyonu => transform.position + (Vector3.up * gozYuksekligi);

    // Dahili bileşenler
    private NavMeshAgent agent;
    private LineRenderer lineRenderer;
    private Renderer govdeRenderer;
    private Material dinamikMateryal;

    private float baslangicY;
    private Vector3 baslangicPozisyonu;
    private bool sagaDonuyor = true;
    private float sonGorulmeZamani = -10f;
    private float sonLogZamani = -10f;
    private string sonBildirilenYon = "";

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        govdeRenderer = GetComponent<Renderer>();

        if (govdeRenderer != null)
        {
            dinamikMateryal = govdeRenderer.material;
        }

        baslangicY = transform.eulerAngles.y;
        baslangicPozisyonu = transform.position;

        // NavMeshAgent ayarları
        agent.speed = kovalamaHizi;
        agent.stoppingDistance = 1.6f;
        agent.angularSpeed = 240f;
        agent.acceleration = 14f;

        // Mor Rota Çizici (LineRenderer) Hazırla
        LineRendererHazirla();

        // Engel maskesi varsayılan olarak Player ve Ignore Raycast katmanları hariç tüm katmanları kapsasın
        if (engelMaskesi.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

            int haricMaskesi = 0;
            if (playerLayer != -1) haricMaskesi |= (1 << playerLayer);
            if (ignoreRaycastLayer != -1) haricMaskesi |= (1 << ignoreRaycastLayer);

            engelMaskesi = ~haricMaskesi;
        }

        HedefiBul();
    }

    private void Start()
    {
        HedefiBul();
    }

    private void Update()
    {
        if (hedefOyuncu == null)
        {
            HedefiBul();
            if (hedefOyuncu == null) return;
        }

        Vector3 hedefNoktasi = HedefNoktasiGetir(hedefOyuncu);

        // 1. Görüş Kontrolleri: Görüş Konisi (Yatay + Dikey Açı ve Menzil)
        gorusKonisinde = GorusKonisinde(hedefNoktasi, out hedefeMesafe, out hedefeYatayAci, out hedefeDikeyAci, out noktaCarpimDegeri);

        // 2. Görüş Hattı Kontrolü (Physics.Linecast)
        gorusHattiAcik = gorusKonisinde && GorusHattiAcik(hedefNoktasi);

        // 3. Nihai Görüş Durumu
        oyuncuGoruluyor = gorusKonisinde && gorusHattiAcik;

        // 4. Gövde Materyal Rengini Güncelle: Görülüyorsa Kırmızı, Görülmüyorsa Yeşil
        if (dinamikMateryal != null)
        {
            dinamikMateryal.color = oyuncuGoruluyor ? Color.red : Color.green;
        }

        // 5. Oyuncu Görüldüğünde Çapraz Çarpım (Cross Product) ile Sağ/Sol Hesabı
        if (oyuncuGoruluyor)
        {
            sonGorulmeZamani = Time.time;
            sonGorulenKonum = hedefOyuncu.position;
            SagSolKonumunuHesaplaVeYazdir(hedefNoktasi);
        }

        // 6. NavMesh Takip ve Devriye Davranışı
        NavMeshTakipVeTarama();

        // 7. Mor Çizgi ile Rotayı Ekrana Çiz
        RotayiCiz();
    }

    /// <summary>
    /// NavMeshAgent kullanarak oyuncuyu takip eder veya çevre taraması yapar.
    /// </summary>
    private void NavMeshTakipVeTarama()
    {
        bool oyuncuTaze = oyuncuGoruluyor || (Time.time - sonGorulmeZamani < kaybetmeZamanAsimi);

        if (oyuncuTaze)
        {
            takipModuAktif = true;
            agent.isStopped = false;
            agent.speed = kovalamaHizi;

            // Oyuncu görülüyorsa canlı pozisyonuna, görüş yeni kesildiyse son görülen noktaya doğru git
            Vector3 hedefHedef = oyuncuGoruluyor ? hedefOyuncu.position : sonGorulenKonum;
            agent.SetDestination(hedefHedef);
        }
        else
        {
            takipModuAktif = false;

            // Eğer başlangıç noktasına uzaksa başlangıç noktasına geri dön
            if (Vector3.Distance(transform.position, baslangicPozisyonu) > 1.0f)
            {
                agent.isStopped = false;
                agent.speed = kovalamaHizi * 0.6f;
                agent.SetDestination(baslangicPozisyonu);
            }
            else
            {
                // Başlangıç noktasındayken dur ve sağa sola 60'ar derece tarama yap
                agent.isStopped = true;
                agent.ResetPath();
                CevreTaramasiYap();
            }
        }
    }

    /// <summary>
    /// Başlangıç yönünden sağa ve sola 60'ar derece sınırlar içinde saniyede 30 derece hızla tarama yapar.
    /// </summary>
    private void CevreTaramasiYap()
    {
        float hedefAciY = sagaDonuyor ? (baslangicY + 60f) : (baslangicY - 60f);
        Quaternion hedefRotasyon = Quaternion.Euler(0f, hedefAciY, 0f);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, hedefRotasyon, taramaHizi * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, hedefRotasyon) < 0.5f)
        {
            sagaDonuyor = !sagaDonuyor;
        }
    }

    /// <summary>
    /// Düşmanın NavMeshAgent rotasını mor bir çizgi olarak ekranda (Game ekranı) gösterir.
    /// </summary>
    private void RotayiCiz()
    {
        if (lineRenderer == null) return;

        if (agent.hasPath && agent.path.corners.Length > 1)
        {
            Vector3[] koseler = agent.path.corners;
            lineRenderer.positionCount = koseler.Length;

            // Çizginin zemine batmaması için hafif yukarı (+0.12m) kaldırarak çiz
            for (int i = 0; i < koseler.Length; i++)
            {
                Vector3 nokta = koseler[i] + Vector3.up * 0.12f;
                lineRenderer.SetPosition(i, nokta);
            }
            lineRenderer.enabled = true;
        }
        else
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }

    private void LineRendererHazirla()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.startWidth = cizgiKalinligi;
        lineRenderer.endWidth = cizgiKalinligi;
        lineRenderer.startColor = rotaCizgiRengi;
        lineRenderer.endColor = rotaCizgiRengi;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;

        // Mor unlit materyal oluştur
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
        Material lineMat = new Material(shader);
        lineMat.color = rotaCizgiRengi;
        lineRenderer.sharedMaterial = lineMat;
    }

    /// <summary>
    /// Ajanın ileri vektörü ile hedef vektörünün çapraz çarpımının (Cross Product)
    /// Y bileşenini kullanarak oyuncunun sağda mı solda mı olduğunu hesaplar.
    /// </summary>
    private void SagSolKonumunuHesaplaVeYazdir(Vector3 hedefNoktasi)
    {
        Vector3 goz = GozPozisyonu;
        Vector3 hedefeVektor = (hedefNoktasi - goz).normalized;
        Vector3 ileriVektor = transform.forward;

        // C_y = (u.z * v.x) - (u.x * v.z)
        Vector3 caprazCarpim = Vector3.Cross(ileriVektor, hedefeVektor);
        caprazCarpimY = caprazCarpim.y;

        if (caprazCarpimY > 0.03f)
        {
            oyuncuYonKonumu = "SAĞINDA";
        }
        else if (caprazCarpimY < -0.03f)
        {
            oyuncuYonKonumu = "SOLUNDA";
        }
        else
        {
            oyuncuYonKonumu = "TAM ÖNÜNDE";
        }

        if (sonBildirilenYon != oyuncuYonKonumu || Time.time - sonLogZamani >= 1.0f)
        {
            sonBildirilenYon = oyuncuYonKonumu;
            sonLogZamani = Time.time;
            Debug.Log($"<color=red><b>[Gözcü]</b></color> Oyuncu takip ediliyor! Konum: <b>{oyuncuYonKonumu}</b> (Çapraz Çarpım Y: {caprazCarpimY:F3})");
        }
    }

    /// <summary>
    /// Oyuncunun ajana uzaklığını menzil ile, yatay ve dikey açıyı ise görüş açıları ile kıyaslar.
    /// NOT: Bu açı Vector3.Angle kullanılmadan, doğrudan normalizasyon ve dot product ile hesaplanır.
    /// </summary>
    public bool GorusKonisinde(Vector3 hedefPozisyon, out float mesafe, out float yatayAci, out float dikeyAci, out float dotProduct)
    {
        Vector3 goz = GozPozisyonu;
        Vector3 hedefeVektor = hedefPozisyon - goz;

        // 1. Menzil Kontrolü
        mesafe = hedefeVektor.magnitude;
        if (mesafe > menzil || mesafe < 0.001f)
        {
            yatayAci = 180f;
            dikeyAci = 180f;
            dotProduct = -1f;
            return false;
        }

        // 2. Dikey Açı Kısıtlaması (Tepedeki oyuncuları engelleme)
        float yatayMesafe = Mathf.Sqrt(hedefeVektor.x * hedefeVektor.x + hedefeVektor.z * hedefeVektor.z);
        dikeyAci = Mathf.Atan2(hedefeVektor.y, yatayMesafe) * Mathf.Rad2Deg;

        float yarimDikeyAci = dikeyGorusAcisi * 0.5f;
        if (Mathf.Abs(dikeyAci) > yarimDikeyAci)
        {
            yatayAci = 180f;
            dotProduct = -1f;
            return false;
        }

        // 3. Yatay Açı Hesabı (Vector3.Angle KULLANILMADAN, Doğrudan Normalizasyon ve Dot Product ile)
        Vector3 hedefeYatayBirim = new Vector3(hedefeVektor.x, 0f, hedefeVektor.z).normalized;
        Vector3 ajaninYatayBirim = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        dotProduct = (ajaninYatayBirim.x * hedefeYatayBirim.x) + (ajaninYatayBirim.z * hedefeYatayBirim.z);
        dotProduct = Mathf.Clamp(dotProduct, -1f, 1f);

        yatayAci = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
        float yarimYatayAci = gorusAcisi * 0.5f;

        return yatayAci <= yarimYatayAci;
    }

    /// <summary>
    /// Physics.Linecast ile engelleri denetler. Player katmanı hariç tutulur.
    /// </summary>
    public bool GorusHattiAcik(Vector3 hedefPozisyon)
    {
        Vector3 baslangicNoktasi = GozPozisyonu;
        RaycastHit hit;
        bool engelVuruldu = Physics.Linecast(baslangicNoktasi, hedefPozisyon, out hit, engelMaskesi, QueryTriggerInteraction.Ignore);
        return !engelVuruldu;
    }

    public Vector3 HedefNoktasiGetir(Transform hedef)
    {
        if (hedef == null) return Vector3.zero;

        CharacterController cc = hedef.GetComponent<CharacterController>();
        if (cc != null) return hedef.position + cc.center;

        CapsuleCollider col = hedef.GetComponent<CapsuleCollider>();
        if (col != null) return hedef.position + col.center;

        return hedef.position + (Vector3.up * 1f);
    }

    private void HedefiBul()
    {
        if (hedefOyuncu != null) return;

        // Önce Oyuncu_RB, yoksa Oyuncu_CC, yoksa "Oyuncu"
        GameObject rbPlayer = GameObject.Find("Oyuncu_RB");
        if (rbPlayer != null)
        {
            hedefOyuncu = rbPlayer.transform;
            return;
        }

        GameObject oyuncu = GameObject.Find("Oyuncu");
        if (oyuncu != null)
        {
            hedefOyuncu = oyuncu.transform;
            return;
        }

        GameObject ccPlayer = GameObject.Find("Oyuncu_CC");
        if (ccPlayer != null)
        {
            hedefOyuncu = ccPlayer.transform;
        }
    }

    private void OnDrawGizmos()
    {
        GozcuGizmoCiz();
    }

    private void OnDrawGizmosSelected()
    {
        GozcuGizmoCiz();
    }

    private void GozcuGizmoCiz()
    {
        Vector3 goz = GozPozisyonu;

        // 1. Tel Küre (Wire Sphere)
        Gizmos.color = oyuncuGoruluyor ? new Color(1f, 0f, 0f, 0.4f) : new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(goz, menzil);

        // 2. Quaternion.AngleAxis ile Sağ ve Sol Sınır Çizgileri
        float yarimAci = gorusAcisi * 0.5f;
        Quaternion solRotasyon = Quaternion.AngleAxis(-yarimAci, Vector3.up);
        Quaternion sagRotasyon = Quaternion.AngleAxis(yarimAci, Vector3.up);

        Vector3 solSinir = solRotasyon * transform.forward;
        Vector3 sagSinir = sagRotasyon * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(goz, solSinir * menzil);
        Gizmos.DrawRay(goz, sagSinir * menzil);

        // 3. Mor NavMesh Rotasını Scene Ekranında Çiz
        if (agent != null && agent.hasPath && agent.path.corners.Length > 1)
        {
            Gizmos.color = rotaCizgiRengi;
            Vector3[] koseler = agent.path.corners;
            for (int i = 0; i < koseler.Length - 1; i++)
            {
                Gizmos.DrawLine(koseler[i] + Vector3.up * 0.15f, koseler[i + 1] + Vector3.up * 0.15f);
                Gizmos.DrawSphere(koseler[i] + Vector3.up * 0.15f, 0.12f);
            }
        }

        // 4. Hedefe Çizilen Görüş Hattı Işını
        if (hedefOyuncu != null)
        {
            Vector3 hedefNoktasi = HedefNoktasiGetir(hedefOyuncu);
            if (oyuncuGoruluyor)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(goz, hedefNoktasi);
            }
            else if (gorusKonisinde)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(goz, hedefNoktasi);
            }
        }
    }
}
