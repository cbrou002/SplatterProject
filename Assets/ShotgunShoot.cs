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

                // 3. Exit Wound Logic
                // To find the exit point, we cast a ray from far ahead back towards the impact.
                // We use a shorter buffer (0.5m) to stay closer to the target and avoid hitting distant objects.
                Vector3 backRayStart = hit.point + ray.direction * 0.5f; 
                Ray backRay = new Ray(backRayStart, -ray.direction);
                
                RaycastHit[] backwardHits = Physics.RaycastAll(backRay, 0.6f);
                
                // CRITICAL FIX: Prioritize the SAME collider for the exit wound to ensure limb wounds appear correctly.
                // If the bullet passed through the arm, we want the exit hole ON the arm.
                var exitHit = backwardHits
                    .Where(h => h.collider == hit.collider)
                    .OrderBy(h => h.distance)
                    .FirstOrDefault();

                // Fallback: If we didn't hit the same collider (e.g. bullet entered one and left another), 
                // pick the first dummy collider we see from the back.
                if (exitHit.collider == null)
                {
                    exitHit = backwardHits
                        .Where(h => h.collider.CompareTag("Dummy"))
                        .OrderBy(h => h.distance)
                        .FirstOrDefault();
                }

                if (exitHit.collider != null)
                {
                    // Ensure the exit is actually behind the entrance and has some thickness
                    if (Vector3.Distance(hit.point, exitHit.point) > 0.02f)
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
        
        // Size logic
        float distFactor = 1.0f + (shotDistance / range) * 0.15f;
        float effectiveSize = baseDecalSize * sizeMult * distFactor;
        
        // CRITICAL FIX for Left/Right Swap and Skewing:
        // 1. Set position and rotation in world space first.
        // 2. Set the scale in world space (before parenting).
        // 3. Parent with worldPositionStays = true. 
        // Unity will automatically calculate the correct localScale to maintain world proportions.
        
        // Position slightly outside surface, rotate to look INTO the surface
        // We want the projection box to be centered such that most of it is inside the mesh.
        // With a 0.2m depth, positioning it 0.05m outside results in 0.15m of projection depth.
        float projectionDepth = 0.2f;
        decalGo.transform.position = hit.point + hit.normal * 0.05f; 
        decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal);
        
        decalGo.transform.localScale = new Vector3(effectiveSize, effectiveSize, projectionDepth);

        // Now parent it - Unity handles the scale compensation.
        decalGo.transform.SetParent(hit.collider.transform, true);

        DecalProjector projector = decalGo.AddComponent<DecalProjector>();
        projector.material = new Material(mat);
        
        // Ensure the decal is on the same layer as the dummy
        decalGo.layer = hit.collider.gameObject.layer;

        // The projector size should match the transform scale we set.
        // Note: DecalProjector.size X and Y are width/height, Z is depth.
        projector.size = new Vector3(1, 1, 1); 
        projector.fadeFactor = 1.0f;

        // Set high draw order
        if (projector.material.HasProperty("_DrawOrder"))
            projector.material.SetFloat("_DrawOrder", 100);

        // Random rotation for variety (rotate around local Z)
        decalGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        
        Destroy(decalGo, 60f);
    }
}
