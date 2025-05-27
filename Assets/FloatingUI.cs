using UnityEngine;

public class FloatingUI : MonoBehaviour
{
    public Transform targetCamera; // Assign XR Origin's Main Camera
    public float followSpeed = 5f;
    public Vector3 positionOffset = new Vector3(0, 0, 2f);
    public float horizontalOffset = 0.3f; // Added horizontal offset control

    void LateUpdate()
    {
        if (targetCamera == null) return;
        
        // Calculate target position with horizontal offset
        Vector3 targetPosition = targetCamera.position + 
            targetCamera.forward * positionOffset.z + 
            targetCamera.up * positionOffset.y +
            targetCamera.right * horizontalOffset; // Added horizontal offset

        // Face towards camera while maintaining upright rotation
        Quaternion targetRotation = Quaternion.LookRotation(
            targetCamera.forward, 
            Vector3.up
        );

        // Smooth movement
        transform.position = Vector3.Lerp(
            transform.position, 
            targetPosition, 
            followSpeed * Time.deltaTime
        );
        
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            followSpeed * Time.deltaTime
        );
    }
}