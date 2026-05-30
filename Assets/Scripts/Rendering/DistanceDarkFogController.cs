using UnityEngine;

public sealed class DistanceDarkFogController : MonoBehaviour
{
    [SerializeField] private Color fogColor = new Color(0.03f, 0.04f, 0.08f);
    [SerializeField] private float startDistance = 20f;
    [SerializeField] private float endDistance = 80f;

    private void Awake()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = startDistance;
        RenderSettings.fogEndDistance = endDistance;
    }
}