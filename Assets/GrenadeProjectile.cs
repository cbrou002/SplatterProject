using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class GrenadeProjectile : MonoBehaviour
{
    public float delay = 3f;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public Material[] splatterMaterials;
    public Material shrapnelEntranceMaterial;
    public Material shrapnelExitMaterial;
    public GameObject woundDripPrefab;
    public GameObject bloodSpewPrefab;
    public Material spewSplatterMaterial;
    public float spewSplatterDistance = 10f;
    public AudioClip explosionSound;

    private bool hasExploded = false;

    void Start()
    {
        StartCoroutine(ExplosionTimer());
    }

    IEnumerator ExplosionTimer()
    {
        yield return new WaitForSeconds(delay);
        if (!hasExploded) Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        CreateDefaultExplosionEffect();

        // Play sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            if (hit.CompareTag("Dummy"))
            {
                SpawnSplattersOnDummy(hit);
                SpawnShrapnelOnDummy(hit);
            }
        }

        // Hide the grenade mesh while the effect plays
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;
        
        Destroy(gameObject, 1.5f); // Destroy after effect and sound
    }

    void SpawnShrapnelOnDummy(Collider dummyCollider)
    {
        int shrapnelCount = Random.Range(20, 35); // Increased amount
        Vector3 explosionPos = transform.position;

        for (int i = 0; i < shrapnelCount; i++)
        {
            // Pick a random point in dummy bounds to aim shrapnel
            Vector3 targetPoint = dummyCollider.bounds.center + Random.insideUnitSphere * 0.5f;
            Vector3 shrapnelDir = (targetPoint - explosionPos).normalized;

            // 1. Entrance Wound
            if (Physics.Raycast(explosionPos, shrapnelDir, out RaycastHit entranceHit, explosionRadius))
            {
                if (entranceHit.collider == dummyCollider || entranceHit.collider.transform.IsChildOf(dummyCollider.transform))
                {
                    CreateWoundDecal(entranceHit.point, entranceHit.normal, entranceHit.collider.transform, shrapnelEntranceMaterial, Random.Range(0.08f, 0.18f), "ShrapnelEntrance");

                    // Spawn Blood Drip for entrance
                    SpawnBloodDrip(entranceHit.point, entranceHit.normal, entranceHit.collider.transform);

                    // Spawn Blood Spew for entrance
                    SpawnBloodSpew(entranceHit.point, entranceHit.normal);

                    // 2. Exit Wound Logic
// Cast a ray from further ahead back towards the entrance to find the exit point on the dummy
                    float bodyThickness = 0.6f; 
                    Vector3 backRayStart = entranceHit.point + shrapnelDir * bodyThickness;
                    Ray backRay = new Ray(backRayStart, -shrapnelDir);

                    // We use RaycastAll to find the hit on the dummy that is furthest from backRayStart (which would be the exit point)
                    RaycastHit[] hits = Physics.RaycastAll(backRay, bodyThickness);
                    foreach (var h in hits)
                    {
                        if (h.collider == dummyCollider || h.collider.transform.IsChildOf(dummyCollider.transform))
                        {
                            // Create exit wound on the side facing away from the explosion
                            // The normal of the exit surface should be pointing outwards
                            CreateWoundDecal(h.point, h.normal, h.collider.transform, shrapnelExitMaterial, Random.Range(0.15f, 0.3f), "ShrapnelExit");
                            
                            // Spawn Blood Drip for exit
                            SpawnBloodDrip(h.point, h.normal, h.collider.transform);

                            // Spawn Blood Spew for exit
                            SpawnBloodSpew(h.point, h.normal);

                            break; // Just one exit per shrapnel piece
}
                    }
                }
                }
                }
                }

                void SpawnBloodDrip(Vector3 position, Vector3 normal, Transform parent)
                {
                    if (woundDripPrefab != null)
                    {
                        Vector3 spawnPos = position + normal * 0.05f;
                        GameObject dripInstance = Instantiate(woundDripPrefab, spawnPos, Quaternion.identity);
                        dripInstance.transform.SetParent(parent, true);
                        Destroy(dripInstance, 12f);
                    }
                }

                void SpawnBloodSpew(Vector3 position, Vector3 normal)
                {
                    if (bloodSpewPrefab != null)
                    {
                        Instantiate(bloodSpewPrefab, position, Quaternion.LookRotation(normal));
                    }

                    if (spewSplatterMaterial != null)
                    {
                        StartCoroutine(DelayedSpewDecal(position, normal));
                    }
                }

                IEnumerator DelayedSpewDecal(Vector3 position, Vector3 normal)
                {
                    yield return new WaitForSeconds(0.5f);
                    
                    Vector3 spewDir = normal;
                    // Add slight gravity bias like the shotgun/sword
                    spewDir = Vector3.Slerp(spewDir, Vector3.down, 0.15f).normalized;

                    int envMask = ~((1 << 3) | (1 << 2)); // Ignore Player and Ignore Raycast
                    if (Physics.Raycast(position + normal * 0.1f, spewDir, out RaycastHit spewHit, spewSplatterDistance, envMask))
                    {
                        if (!spewHit.collider.CompareTag("Dummy"))
                        {
                            CreateSpewDecal(spewHit);
                        }
                    }
                }

                void CreateSpewDecal(RaycastHit hit)
                {
                    GameObject decalGo = new GameObject("GrenadeSpewSplatter");
                    decalGo.transform.position = hit.point + hit.normal * 0.02f;
                    decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal);

                    DecalProjector projector = decalGo.AddComponent<DecalProjector>();
                    projector.scaleMode = DecalScaleMode.ScaleInvariant;
                    
                    float flipX = Random.value > 0.5f ? 1f : -1f;
                    float flipY = Random.value > 0.5f ? 1f : -1f;
                    float size = Random.Range(0.4f, 0.8f);
                    
                    projector.size = new Vector3(size * flipX, size * flipY, 1.0f);
                    
                    Material instance = new Material(spewSplatterMaterial);
                    if (instance.HasProperty("_BaseColor"))
                    {
                        Color c = instance.GetColor("_BaseColor");
                        float brightness = Random.Range(0.7f, 1.1f);
                        c.r *= brightness; c.g *= brightness; c.b *= brightness;
                        instance.SetColor("_BaseColor", c);
                    }
                    projector.material = instance;
                    
                    decalGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
                    decalGo.transform.SetParent(hit.collider.transform, true);
                    
                    Destroy(decalGo, 60f);
                }

                void CreateWoundDecal(Vector3 position, Vector3 normal, Transform parent, Material mat, float size, string name)
                {
                if (mat == null) return;

                GameObject decalGo = new GameObject(name);
                decalGo.transform.position = position + normal * 0.01f;
                decalGo.transform.rotation = Quaternion.LookRotation(-normal);
                decalGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);

                DecalProjector projector = decalGo.AddComponent<DecalProjector>();
        
                float flipX = Random.value > 0.5f ? 1f : -1f;
                float flipY = Random.value > 0.5f ? 1f : -1f;
                projector.size = new Vector3(size * flipX, size * flipY, 0.5f);
        
                Material instance = new Material(mat);
                if (instance.HasProperty("_BaseColor"))
                {
                Color c = instance.GetColor("_BaseColor");
                float brightness = Random.Range(0.8f, 1.2f);
                c.r *= brightness; c.g *= brightness; c.b *= brightness;
                instance.SetColor("_BaseColor", c);
                }
                projector.material = instance;
                projector.scaleMode = DecalScaleMode.ScaleInvariant;
        
                decalGo.transform.SetParent(parent, true);
                Destroy(decalGo, 60f);
                }

    void SpawnSplattersOnDummy(Collider dummyCollider)
    {
        // Vector pointing from explosion to dummy center
        Vector3 origin = transform.position;
        Vector3 targetPoint = dummyCollider.bounds.center;
        Vector3 explosionToDummyDir = (targetPoint - origin).normalized;
        
        // Find a point on the dummy surface
        if (Physics.Raycast(origin, explosionToDummyDir, out RaycastHit hit, explosionRadius))
        {
            if (hit.collider.CompareTag("Dummy") || hit.collider.transform.IsChildOf(dummyCollider.transform))
            {
                int splatterCount = Random.Range(6, 12);
                for (int i = 0; i < splatterCount; i++)
                {
                    // Direction with spread: blow blood from explosion past/through dummy
                    Vector3 sprayDir = Vector3.Slerp(explosionToDummyDir, Random.onUnitSphere, 0.25f).normalized;
                    
                    // Start ray from just outside the dummy on the far side or just keep going from the initial hit
                    // We cast from the hit point further into the environment
                    float envRayRange = explosionRadius * 2f; 
                    int envMask = ~(1 << 3); // Ignore Player layer

                    // Raycast past the dummy to find environment (floor/walls/ceiling)
                    if (Physics.Raycast(hit.point + sprayDir * 0.1f, sprayDir, out RaycastHit envHit, envRayRange, envMask))
                    {
                        // Ensure we didn't just hit another part of the dummy
                        if (!envHit.collider.CompareTag("Dummy") && !envHit.collider.transform.IsChildOf(dummyCollider.transform))
                        {
                            CreateSplatter(envHit.point, envHit.normal, envHit.collider.transform);
                        }
                    }
                }
            }
        }
    }

    void CreateSplatter(Vector3 position, Vector3 normal, Transform parent)
    {
        GameObject splatterGo = new GameObject("GrenadeSplatterEnvironment");
        splatterGo.transform.position = position + normal * 0.02f;
        splatterGo.transform.rotation = Quaternion.LookRotation(-normal);
        
        splatterGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        
        // Add random flip for more variety
        float flipX = Random.value > 0.5f ? 1f : -1f;
        float flipY = Random.value > 0.5f ? 1f : -1f;
        
        float size = Random.Range(0.8f, 2.0f);
        
        DecalProjector projector = splatterGo.AddComponent<DecalProjector>();
        projector.size = new Vector3(size * flipX, size * flipY, 1.0f);
        projector.scaleMode = DecalScaleMode.ScaleInvariant;
        
        if (splatterMaterials != null && splatterMaterials.Length > 0)
        {
            Material mat = splatterMaterials[Random.Range(0, splatterMaterials.Length)];
            // Create a material instance to allow per-decal color variation
            Material instance = new Material(mat);
            
            // Randomly darken or slightly shift the color
            if (instance.HasProperty("_BaseColor"))
            {
                Color c = instance.GetColor("_BaseColor");
                float brightness = Random.Range(0.7f, 1.1f);
                c.r *= brightness;
                c.g *= brightness;
                c.b *= brightness;
                instance.SetColor("_BaseColor", c);
            }
            else if (instance.HasProperty("Base_Color")) // Check for alternative naming
            {
                Color c = instance.GetColor("Base_Color");
                float brightness = Random.Range(0.7f, 1.1f);
                c.r *= brightness;
                c.g *= brightness;
                c.b *= brightness;
                instance.SetColor("Base_Color", c);
            }
            
            projector.material = instance;
        }
        
        // Parent to hit object (walls/floor)
        splatterGo.transform.SetParent(parent, true);
        Destroy(splatterGo, 30f);
    }

    void CreateDefaultExplosionEffect()
    {
        GameObject exp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        exp.transform.position = transform.position;
        exp.transform.localScale = Vector3.one * 0.1f;
        
        Collider col = exp.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        Renderer ren = exp.GetComponent<Renderer>();
        // Using a standard URP shader that supports transparency
        ren.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        ren.material.color = new Color(1f, 0.5f, 0f, 1f); // Orange
        
        // Set transparent rendering if possible
        ren.material.SetFloat("_Surface", 1); // 1 is Transparent in many URP shaders
        ren.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ren.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ren.material.SetInt("_ZWrite", 0);
        ren.material.DisableKeyword("_ALPHATEST_ON");
        ren.material.EnableKeyword("_ALPHABLEND_ON");
        ren.material.renderQueue = 3000;

        Light light = exp.AddComponent<Light>();
        light.color = new Color(1f, 0.5f, 0f);
        light.range = explosionRadius;
        light.intensity = 50f;
        
        StartCoroutine(AnimateExplosion(exp, light));
    }

    IEnumerator AnimateExplosion(GameObject obj, Light light)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.1f;
        Vector3 endScale = Vector3.one * (explosionRadius * 0.8f);

        Material mat = obj.GetComponent<Renderer>().material;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            light.intensity = Mathf.Lerp(50f, 0f, t);
            
            Color c = mat.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            mat.color = c;

            yield return null;
        }
        Destroy(obj);
    }
}
