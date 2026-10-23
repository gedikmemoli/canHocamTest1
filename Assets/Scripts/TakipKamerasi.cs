using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// LateUpdate döngüsünde kare hızından bağımsız üssel sönümleme fonksiyonu (Exponential Decay)
/// kullanarak hedefi yumuşakça takip eden üçüncü şahıs takip kamerası.
/// </summary>
public class TakipKamerasi : MonoBehaviour
{
    [Header("Takip Hedefi")]
    [Tooltip("Kameranın takip edeceği karakterin Transform bileşeni.")]
    public Transform hedef;

    [Tooltip("Hedef karakterin pivotuna eklenecek ofset (genellikle baş/göz hizası).")]
    public Vector3 hedefOfset = new Vector3(0f, 1.6f, 0f);

    [Header("Mesafe ve Açı Ayarları")]
    [Tooltip("Kameranın hedefe olan mesafesi (metre).")]
    public float mesafe = 5.5f;

    [Tooltip("Yatay bakış hassasiyeti (Yaw).")]
    public float hassasiyetX = 0.15f;

    [Tooltip("Dikey bakış hassasiyeti (Pitch).")]
    public float hassasiyetY = 0.15f;

    [Tooltip("Minimum dikey açı (aşağı bakış limiti).")]
    public float minPitch = -30f;

    [Tooltip("Maksimum dikey açı (yukarı bakış limiti).")]
    public float maxPitch = 70f;

    [Header("Üssel Yumuşatma (Exponential Smoothing)")]
    [Tooltip("Kare hızından bağımsız konum takip hızı katsayısı (lambda).")]
    public float pozisyonTakipFaktoru = 12f;

    [Tooltip("Kare hızından bağımsız rotasyon takip hızı katsayısı (lambda).")]
    public float rotasyonTakipFaktoru = 15f;

    [Header("Girdi Referansı")]
    public GirdiOkuyucu girdiOkuyucu;

    [Header("Canlı Açı Değerleri")]
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float pitch = 15f;

    private void Start()
    {
        if (hedef == null)
        {
            GameObject ccPlayer = GameObject.Find("Oyuncu_CC");
            if (ccPlayer != null) hedef = ccPlayer.transform;
        }

        if (girdiOkuyucu == null)
        {
            girdiOkuyucu = Object.FindFirstObjectByType<GirdiOkuyucu>();
        }

        // Başlangıç açılarını mevcut kamera rotasyonuna eşitle
        Vector3 acilar = transform.eulerAngles;
        yaw = acilar.y;
        pitch = acilar.x;

        // İmleci kilitle (isteğe bağlı, kolay kamera kontrolü için)
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        // Fare bakış girdilerini al
        Vector2 bakisGirdisi = Vector2.zero;

        if (girdiOkuyucu != null && girdiOkuyucu.BakisGirdisi.sqrMagnitude > 0.001f)
        {
            bakisGirdisi = girdiOkuyucu.BakisGirdisi;
        }
        else if (Mouse.current != null)
        {
            bakisGirdisi = Mouse.current.delta.ReadValue();
        }

        // Yaw ve Pitch açılarını güncelle
        yaw += bakisGirdisi.x * hassasiyetX;
        pitch -= bakisGirdisi.y * hassasiyetY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Klavye 'Tab' tuşu ile Oyuncu_CC ve Oyuncu_RB arasında hedef geçişi yapabilme
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            HedefKarakteriDegistir();
        }
    }

    /// <summary>
    /// LateUpdate içerisinde hedef karakterin hareketi bittikten sonra
    /// kare hızından bağımsız (Time.deltaTime ve üssel fonksiyon ile) yumuşak takip uygulanır.
    /// </summary>
    private void LateUpdate()
    {
        if (hedef == null) return;

        // 1. Hedefin odaklanılacak dünya pozisyonu (Baş/Göğüs hizası)
        Vector3 odakNoktasi = hedef.position + hedefOfset;

        // 2. İstenen kamera rotasyonu (Yaw & Pitch)
        Quaternion hedefRotasyon = Quaternion.Euler(pitch, yaw, 0f);

        // 3. İstenen kamera konumu (Odak noktasından geriye doğru mesafe kadar uzaklık)
        Vector3 hedefKonum = odakNoktasi - (hedefRotasyon * Vector3.forward * mesafe);

        // 4. Kare Hızından Bağımsız Üssel Yumuşatma Fonksiyonu (Exponential Smoothing):
        // Formül: x(t + dt) = x_hedef + (x(t) - x_hedef) * exp(-lambda * dt)
        // Lerp formunda: Lerp(mevcut, hedef, 1 - exp(-lambda * dt))
        float tPozisyon = 1f - Mathf.Exp(-pozisyonTakipFaktoru * Time.deltaTime);
        float tRotasyon = 1f - Mathf.Exp(-rotasyonTakipFaktoru * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, hedefKonum, tPozisyon);
        transform.rotation = Quaternion.Slerp(transform.rotation, hedefRotasyon, tRotasyon);
    }

    /// <summary>
    /// Takip edilecek hedefi dinamik olarak değiştirir.
    /// </summary>
    public void HedefAyarla(Transform yeniHedef)
    {
        hedef = yeniHedef;
    }

    public void HedefKarakteriDegistir()
    {
        GameObject ccPlayer = GameObject.Find("Oyuncu_CC");
        GameObject rbPlayer = GameObject.Find("Oyuncu_RB");

        if (hedef != null && ccPlayer != null && hedef == ccPlayer.transform && rbPlayer != null)
        {
            HedefAyarla(rbPlayer.transform);
            Debug.Log("[TakipKamerasi] Hedef karakter 'Oyuncu_RB' olarak değiştirildi.");
        }
        else if (ccPlayer != null)
        {
            HedefAyarla(ccPlayer.transform);
            Debug.Log("[TakipKamerasi] Hedef karakter 'Oyuncu_CC' olarak değiştirildi.");
        }
    }
}
