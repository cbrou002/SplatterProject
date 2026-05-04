using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

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
    public float baseDecalSize = 0.12f;
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

        List<RaycastHit> entranceHits = new List<RaycastHit>();

        for (int i = 0; i < pelletCount; i++)
        {
            // Calculate spread
            Quaternion spread = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0
            );
            
            Vector3 direction = spread * fpsCamera.transform.forward;
            Ray ray = new Ray(fpsCamera.transform.position, direction);

            // Find all hits along the path
            RaycastHit[] hits = Physics.RaycastAll(ray, range);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Dummy"))
                {
                    entranceHits.Add(hit);
                    break; // Only first dummy hit per pellet
                }
            }
        }

        if (entranceHits.Count > 0)
        {
            // 1. Spawn exactly ONE blood effect at the closest hit point
            RaycastHit closestHit = entranceHits[0];
            float minDist = closestHit.distance;
            foreach (var h in entranceHits)
            {
                if (h.distance < minDist)
                {
                    minDist = h.distance;
                    closestHit = h;
                }
            }

            if (bloodEffectPrefab != null)
            {
                // Instantiate slightly away from surface to prevent clipping
                Instantiate(bloodEffectPrefab, closestHit.point + closestHit.normal * 0.05f, Quaternion.LookRotation(closestHit.normal));
            }

            // 2. Process all decals
            foreach (var hit in entranceHits)
            {
                CreateWoundDecal(hit, "EntranceWound", entranceDecalMaterial, 1.0f, hit.distance);

                // Exit Search: Cast back from well behind the entrance
                Vector3 rayDir = (hit.point - fpsCamera.transform.position).normalized;
                Vector3 backRayStart = hit.point + rayDir * 1.5f;
                Ray backRay = new Ray(backRayStart, -rayDir);
                
                RaycastHit[] backwardHits = Physics.RaycastAll(backRay, 1.6f);
                System.Array.Sort(backwardHits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var bHit in backwardHits)
                {
                    // The first Dummy we hit from the back that is NOT the same surface as the entrance
                    if (bHit.collider.CompareTag("Dummy"))
                    {
                        // Ensure it's significantly far from the entrance to be an "exit"
                        if (Vector3.Distance(hit.point, bHit.point) > 0.02f)
                        {
                            CreateWoundDecal(bHit, "ExitWound", exitDecalMaterial, exitWoundMultiplier, hit.distance);
                            break; // Only one exit per pellet
                        }
                    }
                }
            }
        }
    }

    void CreateWoundDecal(RaycastHit hit, string name, Material mat, float sizeMult, float shotDistance)
    {
        if (mat == null) return;

        GameObject decalGo = new GameObject(name);
        
        // Parent to the hit collider so it moves with the dummy
        decalGo.transform.SetParent(hit.collider.transform, true);

        // Position slightly outside the surface (1cm) facing it
        decalGo.transform.position = hit.point + hit.normal * 0.01f;
        decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal);

        // FIX: Counteract parent scale so size is in world units.
        // DecalProjector size is relative to transform scale. 
        // We set localScale to inverse of parent's lossyScale to make lossyScale (1,1,1).
        Vector3 parentScale = hit.collider.transform.lossyScale;
        decalGo.transform.localScale = new Vector3(
            1.0f / Mathf.Max(parentScale.x, 0.0001f),
            1.0f / Mathf.Max(parentScale.y, 0.0001f),
            1.0f / Mathf.Max(parentScale.z, 0.0001f)
        );

        DecalProjector projector = decalGo.AddComponent<DecalProjector>();
        
        // Create instance to allow draw order modification
        projector.material = new Material(mat);
        if (projector.material.HasProperty("_DrawOrder"))
            projector.material.SetFloat("_DrawOrder", 50);

        float distanceFactor = 1.0f + (shotDistance / range) * spreadIntensity;
        float effectiveSize = baseDecalSize * sizeMult * distanceFactor;

        // Depth for reliable capture
        float worldDepth = 0.5f;
        projector.size = new Vector3(effectiveSize, effectiveSize, worldDepth);

        // Random rotation for variation
        decalGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        
        Destroy(decalGo, 60f);
    }
    }
