using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 20x20 arenasındaki oyuncu küresini / kapsülünü WASD (New Input System Move eylemi)
/// ile hareket ettiren basit ve doğrudan oyuncu hareket betiği.
/// </summary>
public class PlayerMove : MonoBehaviour
{
    public float speed = 5f;
    public Vector2 minBounds = new(0.5f, 0.5f);
    public Vector2 maxBounds = new(19.5f, 19.5f);
    public bool clampToArena = false;
    public Vector3 arenaOrigin = Vector3.zero;

    private InputAction moveAction;
    private CharacterController cc;

    private void Awake()
    {
        moveAction = InputSystem.actions?.FindAction("Player/Move");
        cc = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void Update()
    {
        Vector2 input = Vector2.zero;
        if (moveAction != null)
        {
            input = moveAction.ReadValue<Vector2>();
        }
        else if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        Vector3 dir = new Vector3(input.x, 0f, input.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        if (cc != null && cc.enabled)
        {
            cc.Move((dir * speed + Vector3.down * 9.81f) * Time.deltaTime);
        }
        else
        {
            transform.position += dir * (speed * Time.deltaTime);
        }

        if (clampToArena)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, arenaOrigin.x + minBounds.x, arenaOrigin.x + maxBounds.x);
            p.z = Mathf.Clamp(p.z, arenaOrigin.z + minBounds.y, arenaOrigin.z + maxBounds.y);
            transform.position = p;
        }
    }
}
