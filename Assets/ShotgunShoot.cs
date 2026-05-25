using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

public class ShotgunShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 15f;
    public GameObject bloodEffectPrefab;
    public Material[] entranceWoundMaterials;
    public Material[] exitWoundMaterials;
    public Material[] entranceSplatterMaterials;
    public Material[] exitSplatterMaterials;

    public AudioSource audioSource;
public AudioClip shotgunSound;

    [Header("Decal Settings")]
    public float baseDecalSize = 0.2f;
    public float spreadIntensity = 1.5f;
    public float exitWoundMultiplier = 1.8f;

    [Header("Splatter Settings")]
    public float splatterDistance = 5f;
    public float splatterBaseSize = 1.2f;

    [Header("Shotgun Settings")]
    public int pelletCount = 8;
    public float spreadAngle = 0.3f;

    [Header("Recoil Settings")]
    public Transform recoilTransform;
    public Vector3 recoilRotation = new Vector3(15f, 0f, 0f);
    public Vector3 recoilPosition = new Vector3(0f, 0f, 0.1f);
    public float snappiness = 10f;
    public float returnSpeed = 5f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private Vector3 currentPosition;
    private Vector3 targetPosition;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        if (recoilTransform == null) recoilTransform = transform;
        originalPosition = recoilTransform.localPosition;
        originalRotation = recoilTransform.localRotation;
    }

    void Update()
    {
        // Handle Recoil Return
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);
        recoilTransform.localRotation = originalRotation * Quaternion.Euler(currentRotation);

        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, returnSpeed * Time.deltaTime);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, snappiness * Time.deltaTime);
        recoilTransform.localPosition = originalPosition + currentPosition;

        // Support both 'E' and Left Click
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Debug.Log("Shotgun Shoot triggered");
        // Apply Recoil
        targetRotation += new Vector3(recoilRotation.x, Random.Range(-recoilRotation.y, recoilRotation.y), Random.Range(-recoilRotation.z, recoilRotation.z));
        targetPosition += recoilPosition;

        if (audioSource != null && shotgunSound != null)
        {
            audioSource.PlayOneShot(shotgunSound);
        }

        // Single slug ray from camera center forward
        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.Log($"Raycast hit: {hit.collider.gameObject.name} with tag {hit.collider.tag}");
            if (hit.collider.CompareTag("Dummy"))
            {
                // 1. Blood Effect: Exactly ONE at the hit point
                if (bloodEffectPrefab != null)
                {
                    Instantiate(bloodEffectPrefab, hit.point + hit.normal * 0.05f, Quaternion.LookRotation(hit.normal));
                }

                // 2. Entrance Wound: Exactly where the crosshair is aimed
                CreateWoundDecal(hit, "EntranceWound", entranceWoundMaterials, 1.0f, hit.distance);

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
                        CreateWoundDecal(exitHit, "ExitWound", exitWoundMaterials, exitWoundMultiplier, hit.distance);
                        
                        // Exit Splatter: Extremely tight (0.01 spread)
                        CreateSplatter(exitHit.point, ray.direction, 0.01f, 20f, exitSplatterMaterials, false);
                    }
                }

                // Entrance Splatter: Reworked for realism (Backspatter)
                // 1. Wider cone (0.6) with gravity bias, more droplets (8), shorter range (3m)
                CreateSplatter(hit.point, -ray.direction, 0.6f, 3f, entranceSplatterMaterials, true);
                
                // 2. Add local misting on the target around the wound
                CreateLocalMist(hit);
                }
                }
                }

                void CreateLocalMist(RaycastHit hit)
                {
                    if (entranceSplatterMaterials == null || entranceSplatterMaterials.Length == 0) return;

                    GameObject mistGo = new GameObject("EntranceMist");
                    mistGo.transform.position = hit.point + hit.normal * 0.05f;
                    mistGo.transform.rotation = Quaternion.LookRotation(-hit.normal);
                    mistGo.transform.SetParent(hit.collider.transform, true);

                    DecalProjector projector = mistGo.AddComponent<DecalProjector>();
                    projector.scaleMode = DecalScaleMode.ScaleInvariant;
                    
                    float size = baseDecalSize * 4f; // Larger but faint
                    projector.size = new Vector3(size, size, 0.2f);
                    
                    Material mat = new Material(entranceSplatterMaterials[Random.Range(0, entranceSplatterMaterials.Length)]);
                    projector.material = mat;
                    projector.fadeFactor = 1.0f; // Full opacity mist

                    if (mat.HasProperty("_DrawOrder")) mat.SetFloat("_DrawOrder", 90);
                    
                    mistGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
                    Destroy(mistGo, 60f);
                }

                void CreateSplatter(Vector3 origin, Vector3 direction, float spread, float distance, Material[] materials, bool isEntrance)
                {
                    if (materials == null || materials.Length == 0) return;

                    // Create multiple splatters in a cone for better coverage
                    int splatterCount = isEntrance ? 5 : 2;
                    
                    for (int i = 0; i < splatterCount; i++)
                    {
                        Vector3 sprayDir = Vector3.Slerp(direction, Random.onUnitSphere, spread).normalized;
                        
                        // Add gravity bias for entrance spatter to simulate arc
                        if (isEntrance)
                        {
                            sprayDir = Vector3.Slerp(sprayDir, Vector3.down, 0.3f).normalized;
                        }

                        float rayOffset = 0.1f;

                        if (Physics.Raycast(origin + sprayDir * rayOffset, sprayDir, out RaycastHit hit, distance))
                        {
                            // Ignore the dummy and the player
                            if (hit.collider.CompareTag("Dummy") || hit.collider.CompareTag("Player"))
                            {
                                if (Physics.Raycast(hit.point + sprayDir * 0.1f, sprayDir, out hit, distance - hit.distance))
                                {
                                    if (hit.collider.CompareTag("Dummy") || hit.collider.CompareTag("Player")) continue;
                                }
                                else continue;
                            }

                            GameObject splatterGo = new GameObject("SplatterDecal");
                            splatterGo.transform.position = hit.point + hit.normal * 0.02f;
                            
                            // Base orientation: look into the surface
                            splatterGo.transform.rotation = Quaternion.LookRotation(-hit.normal);
                
                            float sizeMult = isEntrance ? Random.Range(0.4f, 1.2f) : Random.Range(1.0f, 2.5f);
                            float size = splatterBaseSize * sizeMult;
                
                            DecalProjector projector = splatterGo.AddComponent<DecalProjector>();
                            projector.scaleMode = DecalScaleMode.ScaleInvariant;
                            projector.size = new Vector3(size, size, 1.0f);
                
                            // Randomly choose from the provided materials
                            Material selectedMat = materials[Random.Range(0, materials.Length)];
                            projector.material = new Material(selectedMat);
                            projector.fadeFactor = 1.0f;

                            if (projector.material.HasProperty("_DrawOrder"))
                                projector.material.SetFloat("_DrawOrder", 50);

                            // Directional Alignment Logic
                            Vector3 radialDir = (sprayDir - direction).normalized;
                            Vector3 outwardDir = Vector3.ProjectOnPlane(radialDir, hit.normal).normalized;

                            if (outwardDir.sqrMagnitude > 0.001f)
                            {
                                splatterGo.transform.rotation = Quaternion.LookRotation(-hit.normal, outwardDir);
                                splatterGo.transform.Rotate(Vector3.forward, Random.Range(-15f, 15f), Space.Self);
                            }
                            else
                            {
                                splatterGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
                            }
                            
                            Destroy(splatterGo, 30f);
                        }
                    }
                }

                void CreateWoundDecal(RaycastHit hit, string name, Material[] materials, float sizeMult, float shotDistance)
                {
                if (materials == null || materials.Length == 0) return;

                GameObject decalGo = new GameObject(name);
        
                // Size logic
                float distFactor = 1.0f + (shotDistance / range) * 0.90f;
                float effectiveSize = baseDecalSize * sizeMult * distFactor;
        
                // Position slightly outside surface, rotate to look INTO the surface
                float projectionDepth = 0.2f;
                decalGo.transform.position = hit.point + hit.normal * 0.05f; 
                decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal);
        
                decalGo.transform.localScale = new Vector3(effectiveSize, effectiveSize, projectionDepth);

                // Now parent it - Unity handles the scale compensation.
                decalGo.transform.SetParent(hit.collider.transform, true);

                DecalProjector projector = decalGo.AddComponent<DecalProjector>();
                projector.scaleMode = DecalScaleMode.InheritFromHierarchy;
        
                // Pick random material
                Material mat = materials[Random.Range(0, materials.Length)];
                projector.material = new Material(mat);
        
                // Ensure the decal is on the same layer as the dummy
                decalGo.layer = hit.collider.gameObject.layer;

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
