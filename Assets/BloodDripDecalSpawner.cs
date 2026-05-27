using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BloodDripDecalSpawner : MonoBehaviour
{
    [Header("Drip Settings")]
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 1.5f;
    public float dropletSize = 0.2f;
    public float maxRayDistance = 10f;

    private float nextSpawnTime;

    void Start()
    {
        // Spawn first droplet immediately
        SpawnDroplet();
        SetNextSpawnTime();
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnDroplet();
            SetNextSpawnTime();
        }
    }

    void SetNextSpawnTime()
    {
        // Faster dripping for more feedback
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval * 0.5f, maxSpawnInterval * 0.5f);
    }

    void SpawnDroplet()
    {
        // Use the pool if available
        if (BloodDropletPool.Instance == null) return;

        // Raycast from slightly higher to avoid being stuck inside a fallen dummy
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        
        // Ignore Player (3) and Ignore Raycast (2)
        int mask = ~((1 << 3) | (1 << 2));

        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, mask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // If we hit the dummy itself, continue the ray downwards
            if (hit.collider.CompareTag("Dummy") || hit.collider.name.ToLower().Contains("dummy"))
                continue;

            float size = dropletSize * 2.0f * Random.Range(0.8f, 1.2f);
            
            // Spawn via pool
            BloodDropletPool.Instance.SpawnDroplet(hit.point, hit.normal, size);

            break; 
        }
    }
}
