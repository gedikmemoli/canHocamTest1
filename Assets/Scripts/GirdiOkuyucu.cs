using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Unity New Input System kullanarak 'Player' eylem haritasındaki
/// Move, Look, Jump ve Sprint eylemlerini doğrudan kod üzerinden (Polling yöntemiyle)
/// okuyup test eden bileşen.
/// </summary>
public class GirdiOkuyucu : MonoBehaviour
{
    [Header("Input Actions Varlığı (İsteğe Bağlı)")]
    [Tooltip("InputSystem_Actions.inputactions dosyası veya boş bırakılırsa üretilen C# sınıfı kullanılır.")]
    public InputActionAsset girdiVarligi;

    [Header("Canlı Polling Değerleri (Salt Okunur)")]
    [Tooltip("Move eyleminden okunan Vector2 değeri (WASD / Sol Analog).")]
    [SerializeField] private Vector2 hareketGirdisi;

    [Tooltip("Look eyleminden okunan Vector2 değeri (Fare / Sağ Analog).")]
    [SerializeField] private Vector2 bakisGirdisi;

    [Tooltip("Jump eyleminin o an basılı olup olmadığı (Space / Gamepad Güney Butonu).")]
    [SerializeField] private bool ziplamaBasili;

    [Tooltip("Jump eyleminin tam bu karede basılıp basılmadığı.")]
    [SerializeField] private bool ziplamaBuKareBasildi;

    [Tooltip("Sprint eyleminin o an basılı olup olmadığı (Sol Shift / Gamepad Sol Çubuk Basma).")]
    [SerializeField] private bool sprintBasili;

    [Header("Görselleştirme ve Test")]
    [Tooltip("Oyun ekranında (Game View) girdi değerlerini bir kutuda canlı gösterir.")]
    public bool ekrandaGoster = true;

    [Tooltip("Konsola girdi değişikliklerini log olarak basar.")]
    public bool konsolaYaz = false;

    // C# Wrapper sınıf örneği
    private InputSystem_Actions uretilenGirdiEylemleri;

    // Dahili eylem referansları
    private InputActionMap playerHaritasi;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    // Public erişim özellikleri (başka betikler doğrudan bu polling verilerini tüketebilir)
    public Vector2 HareketGirdisi => hareketGirdisi;
    public Vector2 BakisGirdisi => bakisGirdisi;
    public bool ZiplamaBasili => ziplamaBasili;
    public bool ZiplamaBuKareBasildi => ziplamaBuKareBasildi;
    public bool SprintBasili => sprintBasili;

    private void Awake()
    {
        EylemleriHazirla();
    }

    private void OnEnable()
    {
        if (uretilenGirdiEylemleri != null)
        {
            uretilenGirdiEylemleri.Player.Enable();
        }
        else if (playerHaritasi != null)
        {
            playerHaritasi.Enable();
        }
        else
        {
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
            sprintAction?.Enable();
        }
    }

    private void OnDisable()
    {
        if (uretilenGirdiEylemleri != null)
        {
            uretilenGirdiEylemleri.Player.Disable();
        }
        else if (playerHaritasi != null)
        {
            playerHaritasi.Disable();
        }
        else
        {
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            sprintAction?.Disable();
        }
    }

    private void OnDestroy()
    {
        uretilenGirdiEylemleri?.Dispose();
    }

    /// <summary>
    /// Girdi eylemlerini hazırlar ve bağlar.
    /// </summary>
    public void EylemleriHazirla()
    {
        if (girdiVarligi != null)
        {
            playerHaritasi = girdiVarligi.FindActionMap("Player", false);

            if (playerHaritasi != null)
            {
                moveAction = playerHaritasi.FindAction("Move");
                lookAction = playerHaritasi.FindAction("Look");
                jumpAction = playerHaritasi.FindAction("Jump");
                sprintAction = playerHaritasi.FindAction("Sprint");
            }
            else
            {
                moveAction = girdiVarligi.FindAction("Player/Move");
                lookAction = girdiVarligi.FindAction("Player/Look");
                jumpAction = girdiVarligi.FindAction("Player/Jump");
                sprintAction = girdiVarligi.FindAction("Player/Sprint");
            }
        }
        else
        {
            // Eğer Inspector'da atanmadıysa doğrudan oluşturulan tip güvenli C# sarmalayıcısını kullan
            uretilenGirdiEylemleri = new InputSystem_Actions();
            moveAction = uretilenGirdiEylemleri.Player.Move;
            lookAction = uretilenGirdiEylemleri.Player.Look;
            jumpAction = uretilenGirdiEylemleri.Player.Jump;
            sprintAction = uretilenGirdiEylemleri.Player.Sprint;
        }
    }

    /// <summary>
    /// Her karede (frame) eylemler polling yöntemiyle (doğrudan sorgulama) okunur.
    /// </summary>
    private void Update()
    {
        // 1. Move Eylemi (Vector2 Polling)
        if (moveAction != null)
        {
            hareketGirdisi = moveAction.ReadValue<Vector2>();
        }

        // 2. Look Eylemi (Vector2 Polling)
        if (lookAction != null)
        {
            bakisGirdisi = lookAction.ReadValue<Vector2>();
        }

        // 3. Jump Eylemi (Button Polling)
        if (jumpAction != null)
        {
            ziplamaBasili = jumpAction.IsPressed();
            ziplamaBuKareBasildi = jumpAction.WasPressedThisFrame();
        }

        // 4. Sprint Eylemi (Button Polling)
        if (sprintAction != null)
        {
            sprintBasili = sprintAction.IsPressed();
        }

        // Konsol çıktısı opsiyonu
        if (konsolaYaz && (hareketGirdisi.sqrMagnitude > 0.01f || bakisGirdisi.sqrMagnitude > 0.01f || ziplamaBuKareBasildi || sprintBasili))
        {
            Debug.Log($"[GirdiOkuyucu Polling] Hareket: {hareketGirdisi} | Bakış: {bakisGirdisi} | Zıplama: {ziplamaBasili} | Sprint: {sprintBasili}");
        }
    }

    private void OnGUI()
    {
        if (!ekrandaGoster) return;

        // Game View üzerinde sol üst köşede şık bir canlı test paneli
        GUILayout.BeginArea(new Rect(15, 15, 320, 220), GUI.skin.box);
        GUILayout.Label("<b><size=14>🎮 Girdi Okuyucu (Polling Testi)</size></b>");
        GUILayout.Space(5);

        GUILayout.Label($"<b>Move (Vector2):</b> ({hareketGirdisi.x:F2}, {hareketGirdisi.y:F2})");
        GUILayout.Label($"<b>Look (Vector2):</b> ({bakisGirdisi.x:F2}, {bakisGirdisi.y:F2})");

        // Renkli durum göstergeleri
        Color varsayilanRenk = GUI.color;

        GUI.color = ziplamaBasili ? Color.green : Color.white;
        GUILayout.Label($"<b>Jump (Zıplama):</b> {(ziplamaBasili ? "BASILI [Aktif]" : "Bırakıldı")}");

        GUI.color = sprintBasili ? Color.cyan : Color.white;
        GUILayout.Label($"<b>Sprint (Koşma):</b> {(sprintBasili ? "BASILI [Aktif]" : "Bırakıldı")}");

        GUI.color = varsayilanRenk;
        GUILayout.Space(5);
        GUILayout.Label("<size=10>Klavye: WASD, Fare, Space, Left Shift\nGamepad: Sol/Sağ Çubuk, A/X, Sol Çubuk Basma</size>");
        GUILayout.EndArea();
    }
}
