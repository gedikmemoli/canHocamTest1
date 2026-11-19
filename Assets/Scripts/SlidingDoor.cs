// Dosya: Assets/Scripts/SlidingDoor.cs
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// K tuşu kapıyı açar/kapatır. Açıkken kapı zemine gömülür ve ağı oymaz.
[RequireComponent(typeof(NavMeshObstacle))]
public class SlidingDoor : MonoBehaviour
{
    public float closedY = 1f;     // kapalıyken merkez yüksekliği (2 m'lik kapı zeminde durur)
    public float openY = -1.1f;    // açıkken kapı tamamen zeminin altında
    public float speed = 2f;       // metre/saniye
    public bool startOpen = true;

    private NavMeshObstacle obstacle;
    private InputAction toggle;
    private bool isOpen;
    private float baseGroundY;

    // 7. hafta (8.6): salt okunur: bekçi okur, değiştiremez
    public bool IsOpen => isOpen;

    private void Awake()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        toggle = InputSystem.actions?.FindAction("ToggleDoor");
        toggle?.Enable();

        // Sahnedeki başlangıç taban yüksekliğini koru (eğer kapı y != 1 yüksekliğindeyse ofsetle çalışsın)
        baseGroundY = transform.position.y - closedY;

        isOpen = startOpen;
        SetY(baseGroundY + (isOpen ? openY : closedY));
        obstacle.enabled = !isOpen;
    }

    private void Update()
    {
        bool pressed = (toggle != null && toggle.WasPressedThisFrame()) ||
                       (toggle == null && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame);

        // 1. Tuşa basıldı: durumu değiştir, engeli hemen aç/kapat
        if (pressed)
        {
            isOpen = !isOpen;
            obstacle.enabled = !isOpen;
        }

        // 2. Kapıyı hedef yükseğe doğru kaydır
        float targetY = baseGroundY + (isOpen ? openY : closedY);
        SetY(Mathf.MoveTowards(transform.position.y, targetY, speed * Time.deltaTime));
    }

    private void SetY(float y)
    {
        Vector3 p = transform.position;
        p.y = y;
        transform.position = p;
    }
}
