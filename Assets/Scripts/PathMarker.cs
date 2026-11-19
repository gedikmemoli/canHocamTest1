using UnityEngine;

public class PathMarker : MonoBehaviour
{
    private Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void SetColor(Color c)
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = c;
        }
    }
}
