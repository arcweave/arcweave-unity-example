using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    private Animator animator;
    private Transform cameraTransform;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
    }
    
    void Update()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Create movement vector relative to camera orientation
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        // Project vectors onto the horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Combine movement
        Vector3 movement = (forward * vertical + right * horizontal).normalized;
        
        if (movement.magnitude > 0)
        {
            // Rotate towards movement direction
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Move
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
            
            // Update animation
            if (animator != null)
                animator.SetFloat("Speed", 1f);
        }
        else if (animator != null)
        {
            animator.SetFloat("Speed", 0);
        }
    }
}