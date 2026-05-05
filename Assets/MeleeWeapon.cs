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

        // Horizontal sweep positions relative to originalPos
        float xDist = 1.0f;
        Vector3 slashStartPos = originalPos + new Vector3(leftToRight ? -xDist : xDist, 0f, -0.3f);
        Vector3 slashEndPos = originalPos + new Vector3(leftToRight ? xDist : -xDist, 0f, 0.2f);

        // Reset to original rotation (no rotation necessary as requested)
        transform.localPosition = slashStartPos;
        transform.localRotation = originalRot;
        yield return new WaitForSeconds(0.05f); // Wind up

        bool hitPerformed = false;

        // Slash phase
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(slashStartPos, slashEndPos, t);
        
            // Perform hit when sword is roughly at the center (t ~ 0.4)
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

        // Return phase
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
        
        // Stab towards the center of the screen
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
        // For a stab, we pass Vector3.zero to signal PerformHit to use the puncture decal.
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

        // Diagonal from top-right to bottom-left, passing through screen center
        Vector3 slashStartPos = originalPos + new Vector3(0.6f, 0.6f, -0.3f);
        Vector3 slashEndPos = originalPos + new Vector3(-1.0f, -1.0f, 0.2f);
        
        Quaternion slashStartRot = originalRot * Quaternion.Euler(0, 0, -60);
        Quaternion slashEndRot = originalRot * Quaternion.Euler(0, 0, 60);

        // Wind up
        transform.localPosition = slashStartPos;
        transform.localRotation = slashStartRot;
        yield return new WaitForSeconds(0.05f);

        bool hitPerformed = false;

        // Slash
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(slashStartPos, slashEndPos, t);
            transform.localRotation = Quaternion.Slerp(slashStartRot, slashEndRot, t);
            
            // Perform hit when sword is roughly at the center (t ~ 0.4)
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

        // Return
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

                // Create Wound Decal
                if (attackType == AttackType.Stab)
                {
                    // Puncture for stabs
                    CreateWoundDecal(hit, "PunctureWound", punctureDecalMaterial, 0.5f * force, Vector3.up, punctureDecalRotation);
                }
                else
                {
                    // Slash for slashes, oriented in swing direction with optional manual offset
                    float rotationOffset = (attackType == AttackType.Horizontal) ? horizontalDecalRotation : diagonalDecalRotation;
                    CreateWoundDecal(hit, "SlashWound", slashDecalMaterial, 1.0f * force, direction, rotationOffset);
                }


                if (bloodEffectPrefab != null)
                {
                    // For stabs (direction is zero), use the normal (points toward player).
                    // For slashes, blend the normal with the swing direction.
                    Vector3 sprayDir = (direction == Vector3.zero) ? hit.normal : Vector3.Lerp(hit.normal, direction, 0.5f).normalized;
                    
                    GameObject bloodInstance = Instantiate(bloodEffectPrefab, hit.point, Quaternion.LookRotation(sprayDir));
                    
                    ParticleSystem ps = bloodInstance.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        var emission = ps.emission;
                        var shape = ps.shape;

                        // Count based on force - increased for more impact
                        var burst = emission.GetBurst(0);
                        float baseCount = 35f;
                        burst.count = new ParticleSystem.MinMaxCurve(baseCount * force, (baseCount + 20f) * force);
                        emission.SetBurst(0, burst);

                        // Shape and Spread
                        shape.shapeType = ParticleSystemShapeType.Cone;
                        // Stabs are more concentrated, slashes more spread
                        shape.angle = (direction == Vector3.zero) ? 10f : 35f;

                        // Speed and Travel - droplets fly further with more force
                        main.startSpeed = new ParticleSystem.MinMaxCurve(5f * force, 9f * force);
                        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); // Smaller droplets for better look

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

    float projectionDepth = 1f;

    decalGo.transform.position = hit.point + hit.normal * 0.05f;
    decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal, upDirection);
    decalGo.transform.Rotate(Vector3.forward, rotationOffset, Space.Self);

    decalGo.transform.SetParent(hit.collider.transform, true);
    decalGo.transform.localScale = Vector3.one;

    var projector = decalGo.AddComponent<UnityEngine.Rendering.Universal.DecalProjector>();
    projector.scaleMode = UnityEngine.Rendering.Universal.DecalScaleMode.ScaleInvariant;
    projector.material = new Material(mat);

    decalGo.layer = hit.collider.gameObject.layer;

    projector.size = new Vector3(0.3f * sizeMult, 0.3f * sizeMult, projectionDepth);
    projector.fadeFactor = 1.0f;

    if (projector.material.HasProperty("_DrawOrder"))
        projector.material.SetFloat("_DrawOrder", 100);

    Destroy(decalGo, 60f);
}
    }


