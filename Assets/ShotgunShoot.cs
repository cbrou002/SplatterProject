using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShotgunShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 10f;
    public GameObject bloodEffectPrefab;
    public Material entranceDecalMaterial;
    public Material exitDecalMaterial;

    public AudioSource audioSource;
    public AudioClip shotgunSound;

    [Header("Decal Settings")]
    public float baseDecalSize = 0.12f;
    public float spreadIntensity = 3.0f;
    public float exitWoundMultiplier = 1.8f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
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

        RaycastHit hit;
        Vector3 rayOrigin = fpsCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 direction = fpsCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, direction, out hit, range))
        {
            if (hit.collider.CompareTag("Dummy"))
            {
                // Instantiate particle effect
                if (bloodEffectPrefab != null)
                {
                    Instantiate(
                        bloodEffectPrefab,
                        hit.point + hit.normal * 0.02f,
                        Quaternion.LookRotation(hit.normal)
                    );
                }

                // Create Entrance Wound
                if (entranceDecalMaterial != null)
                {
                    CreateWoundDecal(hit, "EntranceWound", entranceDecalMaterial, 1.0f, hit.distance);
                }

                // Create Exit Wound
                // Raycast from 1 meter ahead back towards the entry to find the exit point
                Ray backRay = new Ray(hit.point + direction * 1.0f, -direction);
                if (hit.collider.Raycast(backRay, out RaycastHit exitHit, 1.0f))
                {
                    if (exitDecalMaterial != null)
                    {
                        CreateWoundDecal(exitHit, "ExitWound", exitDecalMaterial, exitWoundMultiplier, hit.distance);
                    }
                }
            }
        }
    }

    void CreateWoundDecal(RaycastHit hit, string name, Material mat, float sizeMult, float shotDistance)
    {
        GameObject decalGo = new GameObject(name);
        decalGo.transform.SetParent(hit.collider.transform, true);

        // Extremely shallow depth (2cm) to prevent projecting through thin limbs
        float worldDepth = 0.02f;

        // Position slightly outside the surface so the volume captures the skin
        decalGo.transform.position = hit.point + hit.normal * (worldDepth * 0.5f);
        decalGo.transform.rotation = Quaternion.LookRotation(-hit.normal);

        DecalProjector projector = decalGo.AddComponent<DecalProjector>();
        projector.material = mat;

        float angle = Vector3.Angle(-fpsCamera.transform.forward, hit.normal);
        float widthFactor = 1.0f + (angle / 90f) * 0.4f;
        float distanceFactor = 1.0f + (shotDistance / range) * spreadIntensity;

        projector.size = new Vector3(
            baseDecalSize * widthFactor * distanceFactor * sizeMult,
            baseDecalSize * distanceFactor * sizeMult,
            worldDepth
        );

        decalGo.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        Destroy(decalGo, 60f);
    }
}
