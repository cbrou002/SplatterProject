using UnityEngine;
using UnityEngine.AI;

public class StalkerAI : MonoBehaviour
{
    public Transform target;
    public float speed = 4.0f;
    
    private NavMeshAgent agent;
    private Camera playerCamera;
    private Renderer[] renderers;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        playerCamera = Camera.main;
        renderers = GetComponentsInChildren<Renderer>();
        
        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                // Fallback to camera if tag isn't set
                player = playerCamera.gameObject;
            }
            if (player != null) target = player.transform;
        }

        if (agent != null)
        {
            agent.speed = speed;
            agent.updateRotation = false; // We will handle rotation manually to face the player
        }
    }

    public float rotationSpeed = 10.0f;

    void Update()
    {
        if (target == null || playerCamera == null || agent == null) return;

        if (IsBeingWatched())
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
            RotateTowardsTarget();
        }
    }

    void RotateTowardsTarget()
    {
        // Calculate direction from dummy to player
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // Keep the dummy upright

        if (direction != Vector3.zero)
        {
            // If the dummy is facing away, we flip the direction vector
            Quaternion targetRotation = Quaternion.LookRotation(-direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    bool IsBeingWatched()
    {
        // 1. Frustum Check: Is any part of the dummy in the camera view?
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        bool inFrustum = false;
        foreach (var r in renderers)
        {
            if (r.isVisible && GeometryUtility.TestPlanesAABB(planes, r.bounds))
            {
                inFrustum = true;
                break;
            }
        }

        if (!inFrustum) return false;

        // 2. Occlusion Check: Can the player actually see it, or is it behind a wall?
        // Check center and a few points to be more robust
        Vector3[] checkPoints = new Vector3[] 
        {
            transform.position + Vector3.up * 1.5f, // Head area
            transform.position + Vector3.up * 0.5f, // Torso area
            transform.position + Vector3.up * 0.1f  // Feet area
        };

        foreach (var point in checkPoints)
        {
            Vector3 direction = point - playerCamera.transform.position;
            float distance = direction.magnitude;
            
            // Raycast to check for obstacles
            if (Physics.Raycast(playerCamera.transform.position, direction, out RaycastHit hit, distance + 0.1f))
            {
                // If we hit the dummy or one of its children, it's visible
                if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
