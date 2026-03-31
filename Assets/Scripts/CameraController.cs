using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Camera Positioning")]
    public float distance = 4.0f;
    // REDUCED: Allows camera to get extremely close to the pivot if backed into a corner
    public float minDistance = 0.05f;

    [Header("Rotation")]
    public float rotationSmoothSpeed = 15.0f;

    [Header("Collision Protection")]
    public LayerMask collisionLayer;
    public float cameraRadius = 0.3f;
    public float wallPushback = 0.15f;
    public float collisionSmoothSpeed = 20f;

    private float currentDistance;

    private void Start()
    {
        currentDistance = distance;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Focal point 
        Vector3 worldOffset = target.TransformDirection(targetOffset);
        Vector3 lookAtPoint = target.position + worldOffset;
        Vector3 desiredPosition = lookAtPoint - (target.forward * distance);
        Vector3 castDirection = (desiredPosition - lookAtPoint).normalized;

        float targetDistance = distance;

        // 2. Double-Cast Collision Check
        // First, check a pure straight line for absolute center-point accuracy
        if (Physics.Linecast(lookAtPoint, desiredPosition, out RaycastHit lineHit, collisionLayer))
        {
            targetDistance = lineHit.distance - wallPushback;
        }

        // Second, cast a thick sphere to catch sharp corners the line might miss
        if (Physics.SphereCast(lookAtPoint, cameraRadius, castDirection, out RaycastHit sphereHit, distance, collisionLayer))
        {
            float sphereDistance = sphereHit.distance - wallPushback;
            // Use whichever distance is shorter (forces the camera closer to the player to be safe)
            if (sphereDistance < targetDistance)
            {
                targetDistance = sphereDistance;
            }
        }

        // 3. Prevent the "Push Through" bug
        targetDistance = Mathf.Clamp(targetDistance, minDistance, distance);

        // 4. Smoothly apply the distance
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, collisionSmoothSpeed * Time.deltaTime);
        transform.position = lookAtPoint + castDirection * currentDistance;

        // 5. Apply Rotation
        Quaternion desiredRotation = Quaternion.LookRotation(lookAtPoint - transform.position, target.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}