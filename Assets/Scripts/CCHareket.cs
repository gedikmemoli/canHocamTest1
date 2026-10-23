using UnityEngine;

/// <summary>
/// CharacterController kullanarak hareket eden, yerçekimi ve zıplamayı
/// Euler sayısal integrasyon adımıyla kendisi hesaplayan karakter kontrolcüsü.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CCHareket : MonoBehaviour
{
    [Header("Referanslar")]
    public GirdiOkuyucu girdiOkuyucu;
    public Transform kameraTransform;

    [Header("Hareket Ayarları")]
    public float yurumeHizi = 6f;
    public float kosmaHizi = 10f;
    public float donusHizi = 14f;

    [Header("Fizik & Euler Entegrasyonu")]
    [Tooltip("Yerçekimi ivmesi (m/s²). Negatif değer girilmelidir.")]
    public float yercekimi = -19.62f;

    [Tooltip("Ulaşılmak istenen maksimum zıplama yüksekliği (metre).")]
    public float ziplamaYuksekligi = 1.5f;

    [Tooltip("Zemindeyken zemine yapışık kalmayı sağlayan küçük dikey hız (m/s).")]
    public float zeminYapısmaHizi = -2f;

    [Header("Nesne İtme Ayarları")]
    [Tooltip("Fiziksel Rigidbody nesnelerine çarpıldığında uygulanacak itme kuvveti çarpanı.")]
    public float itmeKuvveti = 3.5f;

    [Header("Canlı Durum (Salt Okunur)")]
    [SerializeField] private Vector3 mevcutDikeyHizVector;
    [SerializeField] private float dikeyHiz;
    [SerializeField] private bool zeminde;

    private CharacterController cc;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (girdiOkuyucu == null)
        {
            girdiOkuyucu = Object.FindFirstObjectByType<GirdiOkuyucu>();
        }

        if (kameraTransform == null && Camera.main != null)
        {
            kameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        zeminde = cc.isGrounded;

        // 1. Girdileri Al
        Vector2 hareketGirdisi = girdiOkuyucu != null ? girdiOkuyucu.HareketGirdisi : Vector2.zero;
        bool ziplamaGirdisi = girdiOkuyucu != null && girdiOkuyucu.ZiplamaBuKareBasildi;
        bool sprintGirdisi = girdiOkuyucu != null && girdiOkuyucu.SprintBasili;

        // 2. Kamera açısına göre yer düzlemine izdüşürülmüş hareket yönünü hesapla
        Vector3 hareketYonu = Hareket.HareketYonuHesapla(hareketGirdisi, kameraTransform);

        // 3. Karakterin hareket yönüne doğru yumuşak dönüşü
        if (hareketYonu.sqrMagnitude > 0.001f)
        {
            Quaternion hedefRotasyon = Quaternion.LookRotation(hareketYonu, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, hedefRotasyon, donusHizi * Time.deltaTime);
        }

        // 4. Yatay Hız Hesaplaması
        float hedefSurat = sprintGirdisi ? kosmaHizi : yurumeHizi;
        Vector3 yatayHiz = hareketYonu * hedefSurat;

        // 5. Yerçekimi ve Zıplama İçin Euler Sayısal Entegrasyon Adımı:
        // v(t + dt) = v(t) + a * dt
        // y(t + dt) = y(t) + v * dt
        if (zeminde)
        {
            // Karakter zemindeyken eğimlerden düşmemesi ve zemine tutunması için küçük negatif hız
            if (dikeyHiz < 0f)
            {
                dikeyHiz = zeminYapısmaHizi;
            }

            // Zıplama: v_0 = sqrt(2 * |g| * h)
            if (ziplamaGirdisi)
            {
                dikeyHiz = Mathf.Sqrt(2f * Mathf.Abs(yercekimi) * ziplamaYuksekligi);
            }
        }
        else
        {
            // Euler Adımı: İvme (yerçekimi) hızın değişimine eklenir
            dikeyHiz += yercekimi * Time.deltaTime;
        }

        mevcutDikeyHizVector = Vector3.up * dikeyHiz;

        // 6. Toplam Hareketi Uygula: Move() çağrısı Euler konum adımını tamamlar (x = x + v * dt)
        Vector3 toplamHiz = yatayHiz + Vector3.up * dikeyHiz;
        cc.Move(toplamHiz * Time.deltaTime);
    }

    /// <summary>
    /// CharacterController dinamik fizik nesneleriyle (Rigidbody) çarpıştığında nesneyi iter.
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        // Çarpılan nesnede dinamik bir Rigidbody yoksa veya kinematikse işlem yapma
        if (rb == null || rb.isKinematic) return;

        // Karakter nesnenin üzerine basıyorsa aşağı doğru itme
        if (hit.moveDirection.y < -0.3f) return;

        // İtme yönünü sadece yatay düzlemde hesapla
        Vector3 itmeYonu = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;
        if (itmeYonu.sqrMagnitude < 0.001f) return;

        // Kuvveti nesnenin temas noktasına uygula
        float itmeGucu = (sprintGirdisiAktif() ? kosmaHizi : yurumeHizi) * itmeKuvveti;
        rb.AddForceAtPosition(itmeYonu * itmeGucu, hit.point, ForceMode.Force);
    }

    private bool sprintGirdisiAktif()
    {
        return girdiOkuyucu != null && girdiOkuyucu.SprintBasili;
    }
}
