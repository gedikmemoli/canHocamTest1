using UnityEngine;

/// <summary>
/// Belirtilen dünya koordinatlarındaki arazinin zemin yüksekliğini ve zemin normalini
/// hesaplayıp döndüren sonda bileşeni.
/// </summary>
[ExecuteAlways]
public class AraziSondasi : MonoBehaviour
{
    [Header("Hedef Arazi")]
    [Tooltip("Hesaplamaların yapılacağı Terrain nesnesi. Boş bırakılırsa sahneden otomatik bulunur.")]
    public Terrain hedefArazi;

    [Header("Canlı Test Ayarları")]
    [Tooltip("İşaretliyse, bu GameObject'in kendi Transform pozisyonu test noktası olarak kullanılır.")]
    public bool nesnePozisyonunuKullan = true;

    [Tooltip("Özel bir test dünya koordinatı.")]
    public Vector3 ozelTestPozisyonu = Vector3.zero;

    [Header("Gizmo Görselleştirme")]
    public bool gizmoGoster = true;
    public Color temasNoktasiRengi = Color.yellow;
    public Color normalRengi = Color.cyan;
    public float normalCizgiUzunlugu = 3f;

    [Header("Canlı Hesaplama Sonuçları (Salt Okunur)")]
    [SerializeField] private Vector3 sonSorguPozisyonu;
    [SerializeField] private float hesaplananZeminYuksekligi;
    [SerializeField] private Vector3 hesaplananZeminNormali = Vector3.up;
    [SerializeField] private float hesaplananEgimAcisi; // Derece cinsinden (0 = düz, 90 = dikey)
    [SerializeField] private bool araziSinirlariIcinde;

    private void Awake()
    {
        AraziyiBul();
    }

    private void OnValidate()
    {
        AraziyiBul();
        GuncelTestiCalistir();
    }

    private void Update()
    {
        GuncelTestiCalistir();
    }

    /// <summary>
    /// Sahnedeki aktif araziyi bulur ve atar.
    /// </summary>
    public void AraziyiBul()
    {
        if (hedefArazi == null)
        {
            hedefArazi = Terrain.activeTerrain;
            if (hedefArazi == null)
            {
                hedefArazi = Object.FindFirstObjectByType<Terrain>();
            }
        }
    }

    /// <summary>
    /// Verilen dünya koordinatındaki zemin yüksekliğini (Dünya Y ekseni) döndürür.
    /// Arazi sınırları dışındaysa 0f döndürür.
    /// </summary>
    /// <param name="dunyaPozisyonu">Sorgulanan dünya koordinatı</param>
    /// <returns>Zeminin dünya koordinatındaki Y yüksekliği</returns>
    public float ZeminYuksekligiGetir(Vector3 dunyaPozisyonu)
    {
        if (hedefArazi == null)
        {
            AraziyiBul();
            if (hedefArazi == null) return 0f;
        }

        // SampleHeight, arazinin transform.position.y ofseti hariç yerel yüksekliği verir.
        float yerelYukseklik = hedefArazi.SampleHeight(dunyaPozisyonu);
        return yerelYukseklik + hedefArazi.transform.position.y;
    }

    /// <summary>
    /// Verilen dünya koordinatındaki arazinin zemin yüzey normal vektörünü döndürür.
    /// </summary>
    /// <param name="dunyaPozisyonu">Sorgulanan dünya koordinatı</param>
    /// <returns>Normalize edilmiş yüzey normal vektörü (Vector3)</returns>
    public Vector3 ZeminNormaliGetir(Vector3 dunyaPozisyonu)
    {
        if (hedefArazi == null)
        {
            AraziyiBul();
            if (hedefArazi == null) return Vector3.up;
        }

        TerrainData tData = hedefArazi.terrainData;
        if (tData == null) return Vector3.up;

        Vector3 terrainPos = hedefArazi.transform.position;
        Vector3 yerelPos = dunyaPozisyonu - terrainPos;

        // 0.0 ile 1.0 arasında normalize edilmiş arazi koordinatları
        float normX = yerelPos.x / tData.size.x;
        float normZ = yerelPos.z / tData.size.z;

        // Koordinatların arazi sınırları içinde olup olmadığını sınırla
        normX = Mathf.Clamp01(normX);
        normZ = Mathf.Clamp01(normZ);

        return tData.GetInterpolatedNormal(normX, normZ);
    }

    /// <summary>
    /// Verilen dünya koordinatındaki zemin yüksekliğini ve normalini aynı anda hesaplayıp döndürür.
    /// </summary>
    /// <param name="dunyaPozisyonu">Sorgulanan dünya koordinatı</param>
    /// <param name="yukseklik">Çıktı zemin yüksekliği</param>
    /// <param name="normal">Çıktı zemin yüzey normali</param>
    /// <returns>Noktanın arazi sınırları içinde olup olmadığı</returns>
    public bool ZeminBilgisiGetir(Vector3 dunyaPozisyonu, out float yukseklik, out Vector3 normal)
    {
        yukseklik = ZeminYuksekligiGetir(dunyaPozisyonu);
        normal = ZeminNormaliGetir(dunyaPozisyonu);

        if (hedefArazi != null && hedefArazi.terrainData != null)
        {
            Vector3 tPos = hedefArazi.transform.position;
            Vector3 tSize = hedefArazi.terrainData.size;
            bool iceride = dunyaPozisyonu.x >= tPos.x && dunyaPozisyonu.x <= (tPos.x + tSize.x) &&
                           dunyaPozisyonu.z >= tPos.z && dunyaPozisyonu.z <= (tPos.z + tSize.z);
            return iceride;
        }

        return false;
    }

    private void GuncelTestiCalistir()
    {
        Vector3 sorguNoktasi = nesnePozisyonunuKullan ? transform.position : ozelTestPozisyonu;
        sonSorguPozisyonu = sorguNoktasi;

        araziSinirlariIcinde = ZeminBilgisiGetir(sorguNoktasi, out hesaplananZeminYuksekligi, out hesaplananZeminNormali);
        hesaplananEgimAcisi = Vector3.Angle(Vector3.up, hesaplananZeminNormali);
    }

    private void OnDrawGizmos()
    {
        if (!gizmoGoster) return;

        Vector3 sorguNoktasi = nesnePozisyonunuKullan ? transform.position : ozelTestPozisyonu;
        Vector3 zeminNoktasi = new Vector3(sorguNoktasi.x, hesaplananZeminYuksekligi, sorguNoktasi.z);

        // Sorgu noktası ile zemin arasındaki bağlantı çizgisi
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(sorguNoktasi, zeminNoktasi);

        // Zemindeki temas noktası (Küre)
        Gizmos.color = temasNoktasiRengi;
        Gizmos.DrawSphere(zeminNoktasi, 0.4f);

        // Zemin yüzey normali (Çizgi/Ok)
        Gizmos.color = normalRengi;
        Gizmos.DrawRay(zeminNoktasi, hesaplananZeminNormali * normalCizgiUzunlugu);

#if UNITY_EDITOR
        // Eğim açısı 45 dereceden dik ise uyarı rengiyle gösterim
        if (hesaplananEgimAcisi >= 45f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(zeminNoktasi, 0.6f);
        }
#endif
    }
}
