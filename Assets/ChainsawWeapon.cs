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
    public Material slashDecalMaterial;
    public float decalFrequency = 0.1f;
    public float decalRotationOffset = 0f;
    
    [Header("Animation")]
public float activeOffset = 0.5f;
    public float smoothSpeed = 10f;
    
    private bool isActive = false;
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

    void ProcessCutting()
    {
        RaycastHit hit;
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, range))
        {
            if (hit.collider.CompareTag("Dummy"))
            {
                // Directional spew
                if (bloodEffectPrefab != null && Time.frameCount % 5 == 0) // Limit frequency of particle spawn
                {
                    GameObject blood = Instantiate(bloodEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(blood, 2f);
                }

                // Decals
                if (Time.time >= nextDecalTime)
                {
                    CreateDecal(hit);
                    nextDecalTime = Time.time + decalFrequency;
                }
            }
        }
    }

    void CreateDecal(RaycastHit hit)
    {
        if (slashDecalMaterial == null) return;

        GameObject decalGo = new GameObject("ChainsawSlash");
        decalGo.transform.position = hit.point + hit.normal * 0.05f;
        
        // Use the camera's up direction for base orientation
        decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal, fpsCamera.transform.up);
        
        // Apply the rotation offset and a tiny bit of jitter for variety
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
        
        // Match the layer of the hit object and set draw order to be visible
        decalGo.layer = hit.collider.gameObject.layer;
        if (projector.material.HasProperty("_DrawOrder"))
            projector.material.SetFloat("_DrawOrder", 100);
            
        projector.renderingLayerMask = ~(1u << 3); // Ignore Player layer

        Destroy(decalGo, 30f);
    }
}
