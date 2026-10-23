using UnityEngine;

/// <summary>
/// Kamera açısına göre yer düzlemine izdüşürülmüş hareket yönünü hesaplayan statik yardımcı sınıf.
/// </summary>
public static class Hareket
{
    /// <summary>
    /// Verilen 2B girdi (x: sağ/sol, y: ileri/geri) ve kamera yönelimini kullanarak
    /// yer düzlemine (XZ) izdüşürülmüş, normalize edilmiş 3B hareket yön vektörünü hesaplar.
    /// </summary>
    /// <param name="girdi">Girdi aygıtından gelen yön (Vector2)</param>
    /// <param name="kameraTransform">Referans alınacak kameranın Transform bileşeni</param>
    /// <returns>Yer düzleminde normalize edilmiş 3B yön vektörü (Vector3)</returns>
    public static Vector3 HareketYonuHesapla(Vector2 girdi, Transform kameraTransform)
    {
        if (kameraTransform == null)
        {
            return HareketYonuHesapla(girdi, Vector3.forward, Vector3.right);
        }

        return HareketYonuHesapla(girdi, kameraTransform.forward, kameraTransform.right);
    }

    /// <summary>
    /// Kamera ileri ve sağ vektörlerini yer düzlemine izdüşürerek hareket yönü hesaplar.
    /// </summary>
    /// <param name="girdi">Girdi aygıtından gelen yön (Vector2)</param>
    /// <param name="kameraIleri">Kameranın forward vektörü</param>
    /// <param name="kameraSag">Kameranın right vektörü</param>
    /// <returns>Yer düzleminde normalize edilmiş 3B yön vektörü (Vector3)</returns>
    public static Vector3 HareketYonuHesapla(Vector2 girdi, Vector3 kameraIleri, Vector3 kameraSag)
    {
        // Girdi yoksa sıfır vektör döndür
        if (girdi.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        // Kameranın ileri ve sağ vektörlerini yer düzlemine (Y = 0) izdüşür
        Vector3 ileriIzdusum = Vector3.ProjectOnPlane(kameraIleri, Vector3.up).normalized;
        Vector3 sagIzdusum = Vector3.ProjectOnPlane(kameraSag, Vector3.up).normalized;

        // İzdüşürülmüş bileşenleri girdi ile birleştir
        Vector3 hareketYonu = (sagIzdusum * girdi.x) + (ileriIzdusum * girdi.y);

        // Büyüklük 1'i aşıyorsa normalize et (çapraz hareketlerde hızlanmayı önler)
        if (hareketYonu.sqrMagnitude > 1f)
        {
            hareketYonu.Normalize();
        }

        return hareketYonu;
    }
}
