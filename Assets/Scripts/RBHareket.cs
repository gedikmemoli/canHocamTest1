using UnityEngine;

/// <summary>
/// Rigidbody kullanarak fiziksel ivmelenme limitini hesaplayan ve FixedUpdate döngüsünde
/// ForceMode.VelocityChange uygulayarak karakteri hareket ettiren Rigidbody tabanlı hareket bileşeni.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class RBHareket : MonoBehaviour
{
    [Header("Referanslar")]
    public GirdiOkuyucu girdiOkuyucu;
    public Transform kameraTransform;

    [Header("Hareket Hızları")]
    public float maksimumHiz = 7f;
    public float sprintHizi = 11f;
    public float donusHizi = 14f;

    [Header("Fiziksel İvmelenme")]
    [Tooltip("Uygulanabilecek maksimum ivmelenme limiti (m/s²).")]
    public float maksimumIvme = 35f;

    [Tooltip("Havadayken yönlendirme kontrolü çarpanı (0 ile 1 arası).")]
    [Range(0.1f, 1f)]
    public float havaKontrolC捨rpani = 0.4f;

    [Header("Zıplama & Zemin")]
    public float ziplamaKuvveti = 7.5f;
    public float zeminKontrolMesafesi = 1.15f;
    public LayerMask zeminKatmanlari = ~0; // Varsayılan tüm katmanlar

    [Header("Canlı Durum (Salt Okunur)")]
    [SerializeField] private Vector3 anlikHiz;
    [SerializeField] private float anlikSurat;
    [SerializeField] private bool zeminde;

    private Rigidbody rb;
    private CapsuleCollider col;
    private bool ziplamaTetiklendi;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        if (girdiOkuyucu == null)
        {
            girdiOkuyucu = Object.FindFirstObjectByType<GirdiOkuyucu>();
        }

        if (kameraTransform == null && Camera.main != null)
        {
            kameraTransform = Camera.main.transform;
        }

        // Rigidbody ayarlarını güvenceye al
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        // Tek karelik zıplama girdisini Update içinde yakala
        if (girdiOkuyucu != null && girdiOkuyucu.ZiplamaBuKareBasildi)
        {
            ziplamaTetiklendi = true;
        }
    }

    private void FixedUpdate()
    {
        // 1. Zemin Kontrolü
        ZeminKontroluYap();

        // 2. Girdileri Oku
        Vector2 hareketGirdisi = girdiOkuyucu != null ? girdiOkuyucu.HareketGirdisi : Vector2.zero;
        bool sprintGirdisi = girdiOkuyucu != null && girdiOkuyucu.SprintBasili;

        // 3. Kamera açısına göre yer düzlemine izdüşürülmüş hareket yönünü hesapla
        Vector3 hareketYonu = Hareket.HareketYonuHesapla(hareketGirdisi, kameraTransform);

        // 4. Hedef Yatay Hız Hesaplaması
        float hedefSurat = sprintGirdisi ? sprintHizi : maksimumHiz;
        Vector3 hedefYatayHiz = hareketYonu * hedefSurat;

        // 5. Mevcut Yatay Hızı Al (Y bileşeni hariç)
        Vector3 mevcutLinearHiz = rb.linearVelocity;
        Vector3 mevcutYatayHiz = new Vector3(mevcutLinearHiz.x, 0f, mevcutLinearHiz.z);

        // 6. Gerekli Hız Değişimi: DeltaV = V_hedef - V_mevcut
        Vector3 gerekenHizDegisimi = hedefYatayHiz - mevcutYatayHiz;

        // 7. Fiziksel İvmelenme Limitinin Hesaplanması:
        // DeltaV_maks = a_maks * FixedDeltaTime
        float gecerliIvme = zeminde ? maksimumIvme : (maksimumIvme * havaKontrolC捨rpani);
        float maksimumUygulanabilirDeltaV = gecerliIvme * Time.fixedDeltaTime;

        // İvme limitini aşmayacak şekilde hız değişimini sınırla
        Vector3 uygulanacakDeltaV = Vector3.ClampMagnitude(gerekenHizDegisimi, maksimumUygulanabilirDeltaV);

        // 8. Kuvveti ForceMode.VelocityChange ile Uygula (Doğrudan ivmelenme ekler)
        rb.AddForce(uygulanacakDeltaV, ForceMode.VelocityChange);

        // 9. Zıplama Mantığı
        if (ziplamaTetiklendi)
        {
            if (zeminde)
            {
                // Mevcut dikey hızı sıfırlayıp temiz bir zıplama ivmesi uygula
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * ziplamaKuvveti, ForceMode.VelocityChange);
            }
            ziplamaTetiklendi = false;
        }

        // 10. Karakter Dönüşü
        if (hareketYonu.sqrMagnitude > 0.001f)
        {
            Quaternion hedefRot = Quaternion.LookRotation(hareketYonu, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, hedefRot, donusHizi * Time.fixedDeltaTime));
        }

        // Bilgi amaçlı değişkenleri güncelle
        anlikHiz = rb.linearVelocity;
        anlikSurat = new Vector3(anlikHiz.x, 0f, anlikHiz.z).magnitude;
    }

    private void ZeminKontroluYap()
    {
        // Karakterin alt tabanından aşağıya doğru küre taraması (SphereCast)
        Vector3 kureMerkezi = transform.position + Vector3.up * (col.radius + 0.05f);
        float taramaMesafesi = col.radius * 0.35f;

        RaycastHit hit;
        // Kendi collider'ını taramadan hariç tut
        int eskiLayer = gameObject.layer;
        zeminde = Physics.SphereCast(kureMerkezi, col.radius * 0.9f, Vector3.down, out hit, taramaMesafesi, zeminKatmanlari, QueryTriggerInteraction.Ignore);
    }
}
