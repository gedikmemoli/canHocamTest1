using UnityEngine;
using UnityEngine.InputSystem;

public class CameraModes : MonoBehaviour
{
    public Transform target;                            // bizim karakter
    public MazeGenerator maze;
    public Vector3 followOffset = new(0f, 9f, -7f);     // dünyaya göre sabit
    public float smoothing = 6f;                        // t = 1 − e^(−k·Δt)
    public float topHeight = 70f;                       // 62 m'lik labirent ekrana sığsın

    public bool top = true;                             // üstten başla: iki ajan birden görünsün

    private void LateUpdate()   // karakter Update'te hareket etti; kamera ondan sonra
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) top = !top;

        Vector3 wantedPos;
        Quaternion wantedRot;

        if (top && maze != null)
        {
            var center = new Vector2Int(maze.width / 2, maze.depth / 2);
            wantedPos = maze.CellToWorld(center) + Vector3.up * topHeight;
            wantedRot = Quaternion.Euler(90f, 0f, 0f);
        }
        else if (target != null)
        {
            wantedPos = target.position + followOffset;
            wantedRot = Quaternion.LookRotation(target.position - wantedPos);
        }
        else
        {
            return;
        }

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, wantedPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, wantedRot, t);
    }
}
