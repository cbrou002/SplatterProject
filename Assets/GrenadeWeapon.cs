using UnityEngine;

public class GrenadeWeapon : MonoBehaviour
{
    public GameObject grenadeProjectilePrefab;
    public Transform throwPoint;
    public float throwForce = 8f;

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

        GameObject grenade = Instantiate(grenadeProjectilePrefab, throwPoint.position, throwPoint.rotation);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(throwPoint.forward * throwForce, ForceMode.Impulse);
            // Add some random torque for rolling effect
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }
    }
}
