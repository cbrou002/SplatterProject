using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class ChainsawWeapon : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 2.0f;
    public float damagePerSecond = 50f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip idleClip;
    public AudioClip activeClip;
    
    [Header("Visual Effects")]
    public GameObject bloodEffectPrefab;
    public GameObject bloodBurstPrefab;
    public Material slashDecalMaterial;
    public Material environmentSplatterMaterial;

    [Header("Fine Spew Settings")]
    public Material[] fineSpewMaterials;
    public int fineSpewPerHit = 3;
    public float fineSpewSpread = 0.06f;
    public float fineSpewMinSize = 1.0f;
    public float fineSpewMaxSize = 2.4f;
    public float fineSpewMinSpeed = 5f;
    public float fineSpewMaxSpeed = 10f;
    public float gravityMultiplier = 1.0f;

    public GameObject woundDripPrefab;
    public float decalFrequency = 0.1f;
    public float decalRotationOffset = 0f;
    
    [Header("Animation")]
    public float activeOffset = 0.5f;
    public float smoothSpeed = 10f;
    
    private bool isActive = false;
    private bool wasCutting = false;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private float nextDecalTime = 0f;

    void Start()
    {
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        if (audioSource != null && idleClip != null)
        {
            audioSource.clip = idleClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void OnEnable()
    {
        isActive = false;
        if (audioSource != null && idleClip != null)
        {
            audioSource.clip = idleClip;
            audioSource.Play();
        }
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        if (isActive)
        {
            ProcessCutting();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SetChainsawActive(true);
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            SetChainsawActive(false);
        }
    }

    void SetChainsawActive(bool active)
    {
        isActive = active;
        if (!active) wasCutting = false;
        if (audioSource != null)
        {
            audioSource.clip = active ? activeClip : idleClip;
            audioSource.Play();
        }
    }

    void HandleMovement()
    {
        Vector3 targetPos = isActive ? originalLocalPos + Vector3.forward * activeOffset : originalLocalPos;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smoothSpeed);
    }

    Vector3 GetSpewDirection(Vector3 hitNormal)
    {
        // Direction based on chainsaw contact, slightly biased upwards for spray effect
        return Vector3.Lerp(hitNormal, fpsCamera.transform.up, 0.3f).normalized;
    }

    void ProcessCutting()
    {
        RaycastHit hit;
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, range))
        {
            if (hit.collider.CompareTag("Dummy"))
            {
                if (!wasCutting)
                {
                    if (bloodBurstPrefab != null)
                    {
                        Instantiate(bloodBurstPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                    wasCutting = true;
                }

                Vector3 spewDir = GetSpewDirection(hit.normal);

                // Directional spew (Visual Particles)
                if (bloodEffectPrefab != null && Time.frameCount % 5 == 0) // Limit frequency of particle spawn
                {
                    GameObject blood = Instantiate(bloodEffectPrefab, hit.point, Quaternion.LookRotation(spewDir));
                    
                    ParticleSystem ps = blood.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        var shape = ps.shape;
                        shape.shapeType = ParticleSystemShapeType.Cone;
                        shape.angle = 15f; // Tightened cone
                        main.startSpeed = new ParticleSystem.MinMaxCurve(fineSpewMinSpeed, fineSpewMaxSpeed);
                        main.gravityModifier = gravityMultiplier;
                    }
                    Destroy(blood, 2f);
                }

                // Decals & Splatter
                if (Time.time >= nextDecalTime)
                {
                    CreateDecal(hit);
                    
                    if (environmentSplatterMaterial != null)
                    {
                        SpawnEnvironmentSplatter(hit);
                    }

                    if (fineSpewMaterials != null && fineSpewMaterials.Length > 0)
                    {
                        SpawnFineSpew(hit, spewDir);
                    }

                    if (woundDripPrefab != null && Random.value > 0.5f)
                    {
                        Vector3 dripPos = hit.point + hit.normal * 0.02f;
                        GameObject drip = Instantiate(woundDripPrefab, dripPos, Quaternion.identity);
                        drip.transform.SetParent(hit.collider.transform, true);
                        Destroy(drip, 15f);
                    }

                    nextDecalTime = Time.time + decalFrequency;
                    }
                    }
                    else
                    {
                    wasCutting = false;
                    }
                    }
                    else
                    {
                    wasCutting = false;
                    }
                    }

    void SpawnFineSpew(RaycastHit hit, Vector3 spewDir)
    {
        StartCoroutine(SpewBurst(hit, spewDir));
    }

    IEnumerator SpewBurst(RaycastHit contactHit, Vector3 baseDir)
    {
        for (int i = 0; i < fineSpewPerHit; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-fineSpewSpread, fineSpewSpread),
                Random.Range(-fineSpewSpread, fineSpewSpread),
                Random.Range(-fineSpewSpread, fineSpewSpread)
            );
            Vector3 velocityDir = (baseDir + randomOffset).normalized;
            float speed = Random.Range(fineSpewMinSpeed, fineSpewMaxSpeed);
            
            StartCoroutine(SimulateSpewTrajectory(contactHit.point + contactHit.normal * 0.05f, velocityDir * speed));
            yield return new WaitForSeconds(Random.Range(0.01f, 0.03f));
        }
    }

    IEnumerator SimulateSpewTrajectory(Vector3 startPos, Vector3 velocity)
    {
        Vector3 currentPos = startPos;
        float timeStep = 0.03f;
        float maxTime = 1.2f;
        int rayMask = ~((1 << 3) | (1 << 2)); // Ignore Player and Ignore Raycast

        for (float t = 0; t < maxTime; t += timeStep)
        {
            Vector3 nextPos = currentPos + velocity * timeStep;
            Vector3 moveDir = nextPos - currentPos;
            float moveDist = moveDir.magnitude;

            if (Physics.Raycast(currentPos, moveDir, out RaycastHit envHit, moveDist, rayMask))
            {
                if (!envHit.collider.CompareTag("Dummy"))
                {
                    Material mat = fineSpewMaterials[Random.Range(0, fineSpewMaterials.Length)];
                    float size = Random.Range(fineSpewMinSize, fineSpewMaxSize);
                    CreateSplatterDecal(envHit, "FineSpewDecal", mat, size);
                    yield break;
                }
                else
                {
                    currentPos = envHit.point + moveDir.normalized * 0.1f;
                }
            }
            else
            {
                currentPos = nextPos;
            }

            velocity += Physics.gravity * gravityMultiplier * timeStep;
            velocity *= 0.98f; // Drag
            yield return new WaitForSeconds(timeStep);
        }
    }

    void SpawnEnvironmentSplatter(RaycastHit hit)
    {
        // Bias spread downwards for a visceral slung effect, with wider dispersion
        Vector3 randomDir = (Vector3.down * Random.Range(0.5f, 1.5f) + fpsCamera.transform.right * Random.Range(-1.2f, 1.2f)).normalized;
        Vector3 sprayDir = Vector3.Lerp(hit.normal, randomDir, 0.7f).normalized;
        
        Vector3 currentPos = hit.point + hit.normal * 0.05f;
        Vector3 velocity = sprayDir * Random.Range(fineSpewMinSpeed, fineSpewMaxSpeed);
        float timeStep = 0.04f;
        float maxLifeTime = 1.2f;

        for (float t = 0; t < maxLifeTime; t += timeStep)
        {
            Vector3 nextPos = currentPos + velocity * timeStep;
            Vector3 moveDir = nextPos - currentPos;
            float moveDist = moveDir.magnitude;

            int rayMask = ~((1 << 3) | (1 << 2));

            if (Physics.Raycast(currentPos, moveDir, out RaycastHit envHit, moveDist, rayMask))
            {
                if (envHit.collider.CompareTag("Dummy"))
                {
                    currentPos = envHit.point + moveDir.normalized * 0.1f;
                    continue;
                }

                CreateSplatterDecal(envHit, "ChainsawSplatter", environmentSplatterMaterial, Random.Range(fineSpewMinSize, fineSpewMaxSize));
                break;
            }

            currentPos = nextPos;
            velocity += Physics.gravity * gravityMultiplier * timeStep;
            velocity *= 0.98f; // Air resistance
        }
    }

    void CreateSplatterDecal(RaycastHit hit, string name, Material mat, float size)
    {
        if (mat == null) return;
        GameObject decalGo = new GameObject(name);
        
        decalGo.transform.position = hit.point + hit.normal * 0.1f;
        
        Vector3 tangent = Vector3.Cross(hit.normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.Cross(hit.normal, Vector3.right);
        
        decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal, tangent);
        decalGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        
        decalGo.transform.localScale = new Vector3(size, size, 2.0f); 
        decalGo.transform.SetParent(hit.collider.transform, true);

        var projector = decalGo.AddComponent<DecalProjector>();
        projector.scaleMode = DecalScaleMode.InheritFromHierarchy;
        projector.material = new Material(mat);
        projector.size = new Vector3(1, 1, 1);
        projector.fadeFactor = 1.0f;
        projector.renderingLayerMask = ~(1u << 3);

        decalGo.layer = hit.collider.gameObject.layer;
        Destroy(decalGo, 20f);
    }

    void CreateDecal(RaycastHit hit)
    {
        if (slashDecalMaterial == null) return;

        GameObject decalGo = new GameObject("ChainsawSlash");
        decalGo.transform.position = hit.point + hit.normal * 0.05f;
        decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal, fpsCamera.transform.up);
        
        float jitter = Random.Range(-5f, 5f);
        decalGo.transform.Rotate(Vector3.forward, decalRotationOffset + jitter, Space.Self);
        
        float size = Random.Range(0.168f, 0.336f);
        decalGo.transform.localScale = new Vector3(size, size, 0.5f);
        decalGo.transform.SetParent(hit.collider.transform, true);

        var projector = decalGo.AddComponent<DecalProjector>();
        projector.scaleMode = DecalScaleMode.InheritFromHierarchy;
        projector.material = new Material(slashDecalMaterial);
        projector.size = new Vector3(1, 1, 1);
        projector.fadeFactor = 1.0f;
        
        decalGo.layer = hit.collider.gameObject.layer;
        if (projector.material.HasProperty("_DrawOrder"))
            projector.material.SetFloat("_DrawOrder", 100);
            
        projector.renderingLayerMask = ~(1u << 3);

        Destroy(decalGo, 30f);
    }
}
