using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class BloodDropletPool : MonoBehaviour
{
    public static BloodDropletPool Instance { get; private set; }

    [Header("Settings")]
    public int poolSize = 200;
    public Material dropletMaterial;
    
    private List<DecalProjector> pool;
    private int currentIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePool();
    }

    void InitializePool()
    {
        pool = new List<DecalProjector>();
        GameObject root = new GameObject("BloodDropletPool_Container");
        root.transform.SetParent(transform);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject("BloodDroplet_Pooled");
            go.transform.SetParent(root.transform);
            
            DecalProjector projector = go.AddComponent<DecalProjector>();
            projector.material = dropletMaterial;
            projector.scaleMode = DecalScaleMode.ScaleInvariant;
            projector.size = new Vector3(0.2f, 0.2f, 0.5f); // Increased depth
            
            // Set a default high draw order to appear above splatters
            if (dropletMaterial != null && dropletMaterial.HasProperty("_DrawOrder"))
                projector.material.SetFloat("_DrawOrder", 200);

            go.SetActive(false);
            pool.Add(projector);
        }
    }

    public void SpawnDroplet(Vector3 position, Vector3 normal, float size)
    {
        if (pool == null || pool.Count == 0) return;

        DecalProjector projector = pool[currentIndex];
        GameObject go = projector.gameObject;

        go.SetActive(false); // Reset
        go.transform.position = position + normal * 0.01f;
        go.transform.rotation = Quaternion.LookRotation(-normal);
        go.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);

        projector.size = new Vector3(size, size, 0.5f); // Maintain the fixed depth
        
        go.SetActive(true);

        currentIndex = (currentIndex + 1) % poolSize;
    }
}
