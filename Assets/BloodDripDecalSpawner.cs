using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BloodDripDecalSpawner : MonoBehaviour
{
    public Material dropletMaterial;
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
        // Raycast from slightly above the wound to be safe
        Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
        
        int mask = ~((1 << 3) | (1 << 2));

        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, mask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Dummy") || hit.collider.name.ToLower().Contains("dummy"))
                continue;

            GameObject dropletGo = new GameObject("BloodDroplet_Debug");
            
            // Position it significantly above the floor to ensure projection depth hits
            dropletGo.transform.position = hit.point + hit.normal * 0.5f; 
            dropletGo.transform.rotation = Quaternion.LookRotation(-hit.normal);
            
            dropletGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);

            DecalProjector projector = dropletGo.AddComponent<DecalProjector>();
            projector.scaleMode = DecalScaleMode.ScaleInvariant;
            
            // INCREASED SIZE FOR VISIBILITY
            float size = dropletSize * 2.0f * Random.Range(0.8f, 1.2f);
            projector.size = new Vector3(size, size, 2.0f); // 2 meter projection depth
            
            if (dropletMaterial != null)
            {
                projector.material = new Material(dropletMaterial);
                if (projector.material.HasProperty("_DrawOrder"))
                    projector.material.SetFloat("_DrawOrder", 200);
            }

            // Ensure it's on the default layer
            dropletGo.layer = 0;

            Debug.Log($"[BloodDrip] Spawned DEBUG droplet at {hit.point} on {hit.collider.name} Size: {size}");

            Destroy(dropletGo, 45f);
            break; 
        }
    }
}
