using UnityEngine;

public class Billboard : MonoBehaviour
{
    [Tooltip("Speed at which the UI rotates to match the camera's yaw.")]
    [SerializeField] private float rotationSmoothSpeed = 10f;
    
    private Camera mainCamera;

    void Awake()
    {
        // Cache the main camera.
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("No main camera found. Make sure a camera is tagged as 'MainCamera'.");
        }
    }

    void LateUpdate()
    {
        // Reacquire the camera if needed.
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        // Get the camera's Y rotation (yaw) and form a new rotation that only uses that angle.
        float cameraYaw = mainCamera.transform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, cameraYaw, 0);

        // Smoothly rotate the UI towards the target rotation.
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
