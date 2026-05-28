using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    private Light _light;
    public float minIntensity = 0.05f;
    public float maxIntensity = 0.25f;
    public float flickerSpeed = 0.1f;

    void Start()
    {
        _light = GetComponent<Light>();
    }

    void Update()
    {
        if (_light == null) return;
        
        // Simple random flicker
        if (Random.value < flickerSpeed)
        {
            _light.intensity = Random.Range(minIntensity, maxIntensity);
        }
    }
}
