using UnityEngine;

/// <summary>
/// Sahne yüklendiğinde eğer HaftaYonetici sahnede yoksa uyarı veren yardımcı bileşen.
/// Asıl tam kurulum Assets/Editor/SetupTerrainAndScene.cs tarafından yapılır.
/// </summary>
[ExecuteAlways]
public class AraziKurucu : MonoBehaviour
{
    public bool otomatikKur = true;
    [SerializeField] private bool kuruldu = true;

    [ContextMenu("Bilgi")]
    public void KurulumuGerceklestir()
    {
        Debug.Log("[AraziKurucu] Lütfen üst menüden 'Tools > Hafta 3-7 Tüm Aşamaları ve Sahneleri Kur (Tam Kurulum)' seçeneğini çalıştırın.");
    }
}
