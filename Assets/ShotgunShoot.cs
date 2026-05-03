using UnityEngine;

public class ShotgunShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 50f;
    public GameObject bloodEffectPrefab;

    public AudioSource audioSource;
    public AudioClip shotgunSound;

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

        if (Physics.Raycast(rayOrigin, fpsCamera.transform.forward, out hit, range))
        {
            if (hit.collider.CompareTag("Dummy"))
            {
                Instantiate(
                    bloodEffectPrefab,
                    hit.point + hit.normal * 0.02f,
                    Quaternion.LookRotation(hit.normal)
                );
            }
            
        }
    }
}