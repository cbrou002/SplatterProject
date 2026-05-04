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

        List<RaycastHit> allHits = new List<RaycastHit>();

        for (int i = 0; i < pelletCount; i++)
        {
            Quaternion spread = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0
            );
            
            Vector3 direction = spread * fpsCamera.transform.forward;
            Ray ray = new Ray(fpsCamera.transform.position, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                if (hit.collider.CompareTag("Dummy"))
                {
                    allHits.Add(hit);
                }
            }
        }

        if (allHits.Count > 0)
        {
            // 1. Blood Effect: Exactly ONE per shot at the closest hit
            RaycastHit closestOverall = allHits.OrderBy(h => h.distance).First();
            if (bloodEffectPrefab != null)
            {
                Instantiate(bloodEffectPrefab, closestOverall.point + closestOverall.normal * 0.05f, Quaternion.LookRotation(closestOverall.normal));
            }

            // 2. Wounds: One entrance and one exit per hit body part (collider)
            var hitGroups = allHits.GroupBy(h => h.collider);

            foreach (var group in hitGroups)
            {
                Collider hitCollider = group.Key;
                
                // Find hit in group closest to the average point
                Vector3 avgPoint = Vector3.zero;
                foreach (var h in group) avgPoint += h.point;
                avgPoint /= group.Count();

                RaycastHit mainHit = group.OrderBy(h => Vector3.Distance(h.point, avgPoint)).First();

                // Entrance Wound
                CreateWoundDecal(mainHit, "EntranceWound", entranceDecalMaterial, 1.0f, mainHit.distance);

                // Exit Wound: Search through the collider
                Vector3 rayDir = (mainHit.point - fpsCamera.transform.position).normalized;
                Vector3 backRayStart = mainHit.point + rayDir * 1.5f;
                Ray backRay = new Ray(backRayStart, -rayDir);
                
                RaycastHit[] backwardHits = Physics.RaycastAll(backRay, 1.6f);
                System.Array.Sort(backwardHits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var bHit in backwardHits)
                {
                    if (bHit.collider == hitCollider && Vector3.Distance(mainHit.point, bHit.point) > 0.02f)
                    {
                        CreateWoundDecal(bHit, "ExitWound", exitDecalMaterial, exitWoundMultiplier, mainHit.distance);
                        break; 
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
