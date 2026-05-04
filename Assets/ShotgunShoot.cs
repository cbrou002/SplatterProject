using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

public class ShotgunShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 15f;
    public GameObject bloodEffectPrefab;
    public Material entranceDecalMaterial;
    public Material exitDecalMaterial;

    public AudioSource audioSource;
    public AudioClip shotgunSound;

    [Header("Decal Settings")]
    public float baseDecalSize = 0.2f;
    public float spreadIntensity = 1.5f;
    public float exitWoundMultiplier = 1.8f;

    [Header("Shotgun Settings")]
    public int pelletCount = 8;
    public float spreadAngle = 3.0f;

    void Update()
    {
        // Support both 'E' and Left Click
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (audioSource != null && shotgunSound != null)
        {
            audioSource.PlayOneShot(shotgunSound);
        }

        // Single slug ray from camera center forward
        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            if (hit.collider.CompareTag("Dummy"))
            {
                // 1. Blood Effect: Exactly ONE at the hit point
                if (bloodEffectPrefab != null)
                {
                    Instantiate(bloodEffectPrefab, hit.point + hit.normal * 0.05f, Quaternion.LookRotation(hit.normal));
                }

                // 2. Entrance Wound: Exactly where the crosshair is aimed
                CreateWoundDecal(hit, "EntranceWound", entranceDecalMaterial, 1.0f, hit.distance);

                // 3. Exit Wound: Find the point where the slug leaves the dummy
                // Cast back from well ahead of the impact point
                Vector3 backRayStart = hit.point + ray.direction * 1.5f; 
                Ray backRay = new Ray(backRayStart, -ray.direction);
                
                RaycastHit[] backwardHits = Physics.RaycastAll(backRay, 1.6f);
                
                // The first "Dummy" hit from the back is the absolute exit point along the bullet's path
                var exitHit = backwardHits
                    .Where(h => h.collider.CompareTag("Dummy"))
                    .OrderBy(h => h.distance)
                    .FirstOrDefault();

                if (exitHit.collider != null)
                {
                    // Ensure the exit is actually behind the entrance
                    if (Vector3.Distance(hit.point, exitHit.point) > 0.05f)
                    {
                        CreateWoundDecal(exitHit, "ExitWound", exitDecalMaterial, exitWoundMultiplier, hit.distance);
                    }
                }
            }
        }
    }

    void CreateWoundDecal(RaycastHit hit, string name, Material mat, float sizeMult, float shotDistance)
    {
        if (mat == null) return;

        GameObject decalGo = new GameObject(name);
        decalGo.transform.SetParent(hit.collider.transform, true);

        // URP Decal Projectors project along the local Z axis.
        // We position the projector 0.1m outside the surface and use a 0.5m depth.
        // This ensures it projects 0.4m into the mesh, handling any thickness or curvature.
        float worldDepth = 0.5f;
        decalGo.transform.position = hit.point + hit.normal * 0.1f; 
        decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal);

        // Counteract parent scale
        Vector3 parentScale = hit.collider.transform.lossyScale;
        decalGo.transform.localScale = new Vector3(
            1.0f / Mathf.Max(parentScale.x, 0.0001f),
            1.0f / Mathf.Max(parentScale.y, 0.0001f),
            1.0f / Mathf.Max(parentScale.z, 0.0001f)
        );

        DecalProjector projector = decalGo.AddComponent<DecalProjector>();
        projector.material = new Material(mat);
        
        // Ensure the decal is on the same layer as the dummy
        decalGo.layer = hit.collider.gameObject.layer;

        // Size logic
        float distFactor = 1.0f + (shotDistance / range) * 0.15f;
        float effectiveSize = baseDecalSize * sizeMult * distFactor;

        projector.size = new Vector3(effectiveSize, effectiveSize, worldDepth);
        projector.fadeFactor = 1.0f;

        // Set high draw order to ensure it's visible over the dummy's base texture
        if (projector.material.HasProperty("_DrawOrder"))
            projector.material.SetFloat("_DrawOrder", 100);

        // Random rotation for variety
        decalGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        
        Debug.Log($"Spawned {name} on {hit.collider.name} at {hit.point}. Size: {effectiveSize}");

        Destroy(decalGo, 60f);
    }
}
