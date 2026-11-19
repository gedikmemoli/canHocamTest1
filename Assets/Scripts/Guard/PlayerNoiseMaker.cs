// Dosya: Assets/Scripts/Guard/PlayerNoiseMaker.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hafta 7 Ödevi (Bölüm 10): Sesi Duyan Bekçi - Oyuncu Ses Çıkarma Betiği
/// Oyuncu N tuşuna (InputSystem_Actions -> MakeNoise eylemi) basınca bulunduğu yerde ses çıkarır
/// ve sahnedeki Guard bileşenlerinin HearNoise(transform.position) metodunu çağırır.
/// </summary>
public class PlayerNoiseMaker : MonoBehaviour
{
    public Guard targetGuard;
    public float visualPulseDuration = 0.8f;

    private InputAction makeNoiseAction;
    private float lastNoiseTime = -10f;
    private Vector3 lastNoisePos;

    private void Awake()
    {
        makeNoiseAction = InputSystem.actions?.FindAction("MakeNoise");
        makeNoiseAction?.Enable();
    }

    private void Update()
    {
        bool pressed = (makeNoiseAction != null && makeNoiseAction.WasPressedThisFrame()) ||
                       (makeNoiseAction == null && Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame);

        if (pressed)
        {
            EmitNoise();
        }
    }

    public void EmitNoise()
    {
        lastNoiseTime = Time.time;
        lastNoisePos = transform.position;

        Debug.Log($"<color=magenta><b>[MakeNoise]</b></color> Oyuncu {lastNoisePos} konumunda ses çıkardı! (N tuşu)");

        if (targetGuard != null)
        {
            targetGuard.HearNoise(lastNoisePos);
        }
        else
        {
            Guard[] guards = Object.FindObjectsByType<Guard>(FindObjectsSortMode.None);
            foreach (Guard g in guards)
            {
                g.HearNoise(lastNoisePos);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (Time.time - lastNoiseTime <= visualPulseDuration)
        {
            float t = (Time.time - lastNoiseTime) / visualPulseDuration;
            Gizmos.color = new Color(0.85f, 0.2f, 1f, 1f - t);
            Gizmos.DrawWireSphere(lastNoisePos, Mathf.Lerp(0.5f, 8f, t));
        }
    }
}
