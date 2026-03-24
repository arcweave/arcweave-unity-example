using UnityEngine;

/// <summary>
/// Third person camera controller that follows a target and rotates with mouse input
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    
    [Header("Position Settings")]
    public float distance = 5.0f;
    public float height = 2.0f;
    public float smoothSpeed = 10.0f;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 3.0f;
    public float minVerticalAngle = -30.0f;
    public float maxVerticalAngle = 60.0f;

    // Camera rotation state
    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera target not assigned!");
        }
        
        // Initialize rotation Y based on initial rotation
        rotationY = transform.eulerAngles.y;

        // Optional: hide and lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraRotation();
        UpdateCameraPosition();
    }

    /// <summary>
    /// Update camera rotation based on mouse input
    /// </summary>
    private void UpdateCameraRotation()
    {
        // Get mouse input for rotation
        rotationX += -Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
        
        // Clamp vertical rotation angle
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
    }

    /// <summary>
    /// Update camera position based on target position and rotation
    /// </summary>
    private void UpdateCameraPosition()
    {
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        
        // Calculate position with offset for distance and height
        Vector3 offsetPosition = new Vector3(0.0f, height, -distance);
        Vector3 position = rotation * offsetPosition + target.position;
        
        // Apply position and rotation with smooth interpolation
        transform.position = Vector3.Lerp(
            transform.position, 
            position, 
            smoothSpeed * Time.deltaTime
        );
        transform.rotation = Quaternion.Lerp(
            transform.rotation, 
            rotation, 
            smoothSpeed * Time.deltaTime
        );
    }
}