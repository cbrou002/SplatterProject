using UnityEngine;

public class GrenadeWeapon : MonoBehaviour
{
    public GameObject grenadeProjectilePrefab;
    public float throwForce = 6f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ThrowGrenade();
        }
    }

    void ThrowGrenade()
    {
        if (grenadeProjectilePrefab == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Spawn slightly in front of the camera to avoid sticking
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * 0.3f;

        GameObject grenade = Instantiate(grenadeProjectilePrefab, spawnPos, cam.transform.rotation);
        
        // Ensure the grenade doesn't collide with the player launcher
        Collider playerCol = cam.GetComponentInParent<Collider>();
        Collider grenadeCol = grenade.GetComponent<Collider>();
        if (playerCol != null && grenadeCol != null)
        {
            Physics.IgnoreCollision(playerCol, grenadeCol);
        }

        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Throw exactly in the direction the camera is facing
            Vector3 throwDir = cam.transform.forward;
            
            // Apply force
            rb.AddForce(throwDir * throwForce, ForceMode.Impulse);
            
            // Small random torque for variety
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
        }
    }
}
