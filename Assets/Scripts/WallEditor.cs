using UnityEngine;
using UnityEngine.InputSystem;

public class WallEditor : MonoBehaviour
{
    public Camera cam;
    public MazeGenerator maze;
    public Transform agent;            // bizim karakter
    public Transform navAgent;         // NavMesh ajanı
    public LayerMask editMask;         // Ground + Wall

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame) return;

        Camera activeCam = cam != null ? cam : Camera.main;
        if (activeCam == null || maze == null) return;

        Ray ray = activeCam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, editMask)) return;

        // Duvara çarptıysa o duvarın hücresi (kaldır), zemine çarptıysa tıklanan hücre (ekle)
        bool hitWall = hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall");
        Vector3 p = hitWall ? hit.collider.transform.position : hit.point;
        Vector2Int cell = maze.WorldToCell(p);

        // Ajanların üstüne duvar örme
        if (agent != null && cell == maze.WorldToCell(agent.position)) return;
        if (navAgent != null && cell == maze.WorldToCell(navAgent.position)) return;

        maze.ToggleWall(cell);
    }
}
