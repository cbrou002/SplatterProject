using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class BloodWallOoze : MonoBehaviour
{
    public Material dropletMaterial;
    public int dripCount = 8; // Increased default
    public float minSpeed = 0.05f;
    public float maxSpeed = 0.2f;
    public float dripDuration = 15f; // Longer duration
    public float areaSize = 1.0f;
    public float startDelayMax = 1.5f;

    private Vector3 wallNormal;

    void Start()
    {
        // The decal is oriented so transform.forward = -hit.normal
        wallNormal = -transform.forward;

        Debug.Log($"[BloodWallOoze] Started on {gameObject.name}. Normal: {wallNormal}. Y component: {wallNormal.y}");

        // Only ooze if the surface is vertical enough (normal.y is small)
        if (Mathf.Abs(wallNormal.y) < 0.5f)
        {
            // Spawn more drips: between 5 and 12
            dripCount = Random.Range(5, 12); 
            Debug.Log($"[BloodWallOoze] Spawning {dripCount} drips on vertical surface.");
            for (int i = 0; i < dripCount; i++)
            {
                StartCoroutine(SpawnAndSlideDrip());
            }
        }
else
        {
            Debug.Log("[BloodWallOoze] Surface not vertical enough for ooze.");
        }
    }

    private IEnumerator SpawnAndSlideDrip()
    {
        yield return new WaitForSeconds(Random.Range(0f, startDelayMax));

        GameObject dripGo = new GameObject("OozingDroplet_Debug");
        
        // Ensure rotation is facing the wall correctly
        Vector3 dripDown = Vector3.ProjectOnPlane(Vector3.down, wallNormal).normalized;
        if (dripDown.sqrMagnitude < 0.001f) dripDown = Vector3.forward; 
        
        dripGo.transform.rotation = Quaternion.LookRotation(-wallNormal, dripDown);
        
        Vector3 horizontalAxis = Vector3.Cross(wallNormal, Vector3.up).normalized;
        Vector3 verticalAxis = Vector3.ProjectOnPlane(Vector3.up, wallNormal).normalized;
        
        Vector3 randomOffset = horizontalAxis * Random.Range(-areaSize * 0.4f, areaSize * 0.4f);
        randomOffset += verticalAxis * Random.Range(-areaSize * 0.3f, areaSize * 0.3f);
        
        // Position slightly in front of the wall to ensure it's not clipped
        dripGo.transform.position = transform.position + randomOffset + wallNormal * 0.05f;

        DecalProjector projector = dripGo.AddComponent<DecalProjector>();
        projector.material = dropletMaterial;
        projector.scaleMode = DecalScaleMode.ScaleInvariant;
        
        // Ensure NO angle-based fading and maximum opacity
        projector.startAngleFade = 180f;
        projector.endAngleFade = 180f;
        projector.fadeFactor = 1.0f;
        projector.fadeScale = 1.0f;
        
        float width = Random.Range(0.12f, 0.22f);
        float height = width * 2.5f;
        projector.size = new Vector3(width, height, 2.0f); 
        
        DecalProjector parentProjector = GetComponent<DecalProjector>();
        if (parentProjector != null)
        {
            projector.renderingLayerMask = parentProjector.renderingLayerMask;
        }

        if (projector.material != null)
        {
            projector.material = new Material(projector.material);
            if (projector.material.HasProperty("_DrawOrder"))
                projector.material.SetFloat("_DrawOrder", 200);
        }

        Debug.Log($"[BloodWallOoze] Spawned drip at {dripGo.transform.position} sliding down {dripDown}");

        float speed = Random.Range(minSpeed, maxSpeed);
        float elapsed = 0;
        
        while (elapsed < dripDuration)
        {
            if (dripGo == null) yield break;

            dripGo.transform.position += dripDown * speed * Time.deltaTime;
            
            // FORCED FULL OPACITY - NO FADING
            projector.fadeFactor = 1.0f;
            projector.size = new Vector3(width, height * (1.0f + (elapsed / dripDuration) * 3.5f), 2.0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (dripGo != null)
            Destroy(dripGo);
    }
}
