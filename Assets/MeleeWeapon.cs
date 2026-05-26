using UnityEngine;
using System.Collections;

public class MeleeWeapon : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 1.5f;
    public float damage = 20f;
    public GameObject bloodEffectPrefab;
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip hitSound;

    [Header("Decal Settings")]
    public Material slashDecalMaterial;
    public Material punctureDecalMaterial;
    public Material environmentSplatterMaterial; // Reverted to singular to match assembly
    public float baseDecalSize = 0.2f;

    [Header("Wound Orientation")]
    [Range(0f, 360f)] public float horizontalDecalRotation = 0f;
    [Range(0f, 360f)] public float diagonalDecalRotation = 0f;
    [Range(0f, 360f)] public float punctureDecalRotation = 0f;

    public enum AttackType { Stab, Diagonal, Horizontal }

    private bool isSwinging = false;
    private Vector3 originalPos;
    private Quaternion originalRot;

    void Start()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
    }

    void Update()
    {
        if (isSwinging) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(SwingRoutine());
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(SlashRoutine());
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(HorizontalSlashRoutine(true)); // Left to Right
        }
        else if (Input.GetKeyDown(KeyCode.G))
        {
            StartCoroutine(HorizontalSlashRoutine(false)); // Right to Left
        }
    }

    IEnumerator HorizontalSlashRoutine(bool leftToRight)
    {
        isSwinging = true;
        float elapsed = 0f;
        float duration = 0.25f;

        float xDist = 1.0f;
        Vector3 slashStartPos = originalPos + new Vector3(leftToRight ? -xDist : xDist, 0f, -0.3f);
        Vector3 slashEndPos = originalPos + new Vector3(leftToRight ? xDist : -xDist, 0f, 0.2f);

        transform.localPosition = slashStartPos;
        transform.localRotation = originalRot;
        yield return new WaitForSeconds(0.05f); 

        bool hitPerformed = false;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(slashStartPos, slashEndPos, t);
        
            if (!hitPerformed && t >= 0.4f)
            {
                Vector3 swingDir = (slashEndPos - slashStartPos).normalized;
                PerformHit(transform.parent.TransformDirection(swingDir), 1.0f, AttackType.Horizontal);
                hitPerformed = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = slashEndPos;

        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(slashEndPos, originalPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        isSwinging = false;
    }

    IEnumerator SwingRoutine()
    {
        isSwinging = true;
        float elapsed = 0f;
        float duration = 0.15f;
        Vector3 targetPos = originalPos + new Vector3(-0.45f, 0.45f, 0.8f);
        
        while (elapsed < duration)
        {
            transform.localPosition = Vector3.Lerp(originalPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = targetPos;
        PerformHit(Vector3.zero, 1.2f, AttackType.Stab);

        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localPosition = Vector3.Lerp(targetPos, originalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        isSwinging = false;
    }

    IEnumerator SlashRoutine()
    {
        isSwinging = true;
        float elapsed = 0f;
        float duration = 0.25f;

        Vector3 slashStartPos = originalPos + new Vector3(0.6f, 0.6f, -0.3f);
        Vector3 slashEndPos = originalPos + new Vector3(-1.0f, -1.0f, 0.2f);
        
        Quaternion slashStartRot = originalRot * Quaternion.Euler(0, 0, -60);
        Quaternion slashEndRot = originalRot * Quaternion.Euler(0, 0, 60);

        transform.localPosition = slashStartPos;
        transform.localRotation = slashStartRot;
        yield return new WaitForSeconds(0.05f);

        bool hitPerformed = false;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(slashStartPos, slashEndPos, t);
            transform.localRotation = Quaternion.Slerp(slashStartRot, slashEndRot, t);
            
            if (!hitPerformed && t >= 0.4f)
            {
                Vector3 swingDir = (slashEndPos - slashStartPos).normalized;
                PerformHit(transform.parent.TransformDirection(swingDir), 1.0f, AttackType.Diagonal);
                hitPerformed = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = slashEndPos;
        transform.localRotation = slashEndRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(slashEndPos, originalPos, t);
            transform.localRotation = Quaternion.Slerp(slashEndRot, originalRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        transform.localRotation = originalRot;
        isSwinging = false;
    }

    void PerformHit(Vector3 direction, float force, AttackType attackType)
    {
        RaycastHit hit;
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, range))
        {
            if (hit.collider.CompareTag("Dummy"))
            {
                if (audioSource != null && hitSound != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }

                if (attackType == AttackType.Stab)
                {
                    CreateWoundDecal(hit, "PunctureWound", punctureDecalMaterial, 0.7f * force, Vector3.up, punctureDecalRotation);
                }
                else
                {
                    float rotationOffset = (attackType == AttackType.Horizontal) ? horizontalDecalRotation : diagonalDecalRotation;
                    CreateWoundDecal(hit, "SlashWound", slashDecalMaterial, 1.0f * force, direction, rotationOffset);
                }

                if (bloodEffectPrefab != null)
                {
                    Vector3 sprayDir = (direction == Vector3.zero) ? hit.normal : Vector3.Lerp(hit.normal, direction, 0.5f).normalized;
                    
                    GameObject bloodInstance = Instantiate(bloodEffectPrefab, hit.point, Quaternion.LookRotation(sprayDir));
                    
                    ParticleSystem ps = bloodInstance.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        var emission = ps.emission;
                        var shape = ps.shape;

                        var burst = emission.GetBurst(0);
                        float baseCount = 350f; 
                        burst.count = new ParticleSystem.MinMaxCurve(baseCount * force, (baseCount + 200f) * force);
                        emission.SetBurst(0, burst);

                        shape.shapeType = ParticleSystemShapeType.Cone;
                        shape.angle = (direction == Vector3.zero) ? 10f : 35f;

                        main.startSpeed = new ParticleSystem.MinMaxCurve(5f * force, 9f * force);
                        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);

                        if (environmentSplatterMaterial != null)
                        {
                            Vector3 currentPos = hit.point + sprayDir * 0.1f;
                            Vector3 velocity = sprayDir * (8.0f * force); 
                            float timeStep = 0.03f;
                            float maxLifeTime = 1.5f;
                            
                            for (float t = 0; t < maxLifeTime; t += timeStep)
                            {
                                Vector3 nextPos = currentPos + velocity * timeStep;
                                Vector3 moveDir = nextPos - currentPos;
                                float moveDist = moveDir.magnitude;

                                // Ignore Layer 3 (Player) and Layer 2 (Ignore Raycast)
                                int rayMask = ~( (1 << 3) | (1 << 2) );

                                if (Physics.Raycast(currentPos, moveDir, out RaycastHit envHit, moveDist, rayMask))
                                {
                                    // If we hit a character (Dummy), "pass through" by moving start point
                                    if (envHit.collider.CompareTag("Dummy"))
                                    {
                                        currentPos = envHit.point + moveDir.normalized * 0.1f;
                                        continue; 
                                    }

                                    // Hit environment!
                                    float travelDist = Vector3.Distance(hit.point, envHit.point);
                                    // Scale size up based on distance: base size + 25% increase per meter traveled
                                    float distanceScale = 1.0f + (travelDist * 0.25f);
                                    
                                    CreateWoundDecal(envHit, "EnvSplatter", environmentSplatterMaterial, 4.0f * force * distanceScale, Vector3.up, Random.Range(0f, 360f));
                                    break;
                                }

                                currentPos = nextPos;
                                velocity += Physics.gravity * timeStep;
                                if (currentPos.y < hit.point.y - 10f) break;
                            }
                        }
                        
                        Destroy(bloodInstance, 2f);
                    }
                }
            }
        }
    }

    void CreateWoundDecal(RaycastHit hit, string name, Material mat, float sizeMult, Vector3 upDirection, float rotationOffset)
    {
        if (mat == null) return;
        GameObject decalGo = new GameObject(name);
        float projectionDepth = 0.5f;
        float effectiveSize = 0.3f * sizeMult;
        decalGo.transform.position = hit.point + hit.normal * 0.05f;
        decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal, upDirection);
        decalGo.transform.Rotate(Vector3.forward, rotationOffset, Space.Self);
        decalGo.transform.localScale = new Vector3(effectiveSize, effectiveSize, projectionDepth);
        decalGo.transform.SetParent(hit.collider.transform, true);
        var projector = decalGo.AddComponent<UnityEngine.Rendering.Universal.DecalProjector>();
        projector.scaleMode = UnityEngine.Rendering.Universal.DecalScaleMode.InheritFromHierarchy;
        
        // Ignore Layer 3 (Player) for projection
        projector.renderingLayerMask = ~(1u << 3); 

        projector.material = new Material(mat);
        decalGo.layer = hit.collider.gameObject.layer;
        projector.size = new Vector3(1, 1, 1); 
        projector.fadeFactor = 1.0f;
        if (projector.material.HasProperty("_DrawOrder"))
            projector.material.SetFloat("_DrawOrder", 100);
        Destroy(decalGo, 60f);
    }
}
