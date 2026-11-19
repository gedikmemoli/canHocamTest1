// Dosya: Assets/Scripts/AreaCostToggle.cs
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.AI.Navigation;

// Deney paneli: M çamur maliyetini değiştirir, J atlama bağlantısını açıp kapatır
[RequireComponent(typeof(NavMeshAgent))]
public class AreaCostToggle : MonoBehaviour
{
    public NavMeshLink jumpLink;
    public string mudAreaName = "Camur";
    public float[] mudCosts = { 1f, 2f, 5f, 10f };

    private NavMeshAgent agent;
    private InputAction cycleMud, toggleLink;
    private int mudArea, costIndex;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        cycleMud = InputSystem.actions?.FindAction("CycleMudCost");
        toggleLink = InputSystem.actions?.FindAction("ToggleLink");
        cycleMud?.Enable();
        toggleLink?.Enable();

        mudArea = NavMesh.GetAreaFromName(mudAreaName);   // alanın sıra numarası (User 3 → 3)
        if (mudArea < 0)
            Debug.LogError($"'{mudAreaName}' adlı alan yok: Navigation > Areas sekmesini kontrol edin.");
    }

    private void Start() => ApplyMudCost();

    private void Update()
    {
        bool mudPressed = (cycleMud != null && cycleMud.WasPressedThisFrame()) ||
                          (cycleMud == null && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame);

        bool linkPressed = (toggleLink != null && toggleLink.WasPressedThisFrame()) ||
                           (toggleLink == null && Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame);

        // 1. M: sıradaki çamur maliyetine geç
        if (mudPressed)
        {
            costIndex = (costIndex + 1) % mudCosts.Length;
            ApplyMudCost();
        }

        // 2. J: bağlantıyı aç/kapat
        if (linkPressed && jumpLink != null)
            jumpLink.activated = !jumpLink.activated;
    }

    private void ApplyMudCost()
    {
        if (mudArea >= 0 && agent != null)
            agent.SetAreaCost(mudArea, mudCosts[costIndex]);   // yalnızca bu ajan için
    }

    private void OnGUI()
    {
        string link = (jumpLink != null && jumpLink.activated) ? "açık" : "kapalı";
        GUI.Label(new Rect(10, 60, 500, 25), $"[M] Çamur maliyeti: {mudCosts[costIndex]}   [J] Bağlantı: {link}   [K] Kapı");
    }
}
