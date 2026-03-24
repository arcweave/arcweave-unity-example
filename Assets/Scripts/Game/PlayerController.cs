using UnityEngine;

/// <summary>
/// Controls player movement with camera-relative controls
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Combat")]
    [Tooltip("Set to true by SwordSwingHandler when the player unlocks the attack ability")]
    public bool canSwing = false;

    // References
    private Animator animator;
    private Transform cameraTransform;
    
    void Start()
    {
        // Get component references
        animator = GetComponent<Animator>();
        
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
    }
    
    void Update()
    {
        HandleMovement();
        if (canSwing) HandleAttack();
    }

    /// <summary>
    /// Handles player movement based on input
    /// </summary>
    private void HandleMovement()
    {
        // Exit if we don't have a camera reference
        if (cameraTransform == null) return;
        
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Create camera-relative direction vectors
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        // Project vectors onto the horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Calculate movement direction
        Vector3 movement = (forward * vertical + right * horizontal).normalized;
        
        // Apply movement if there's input
        if (movement.magnitude > 0)
        {
            // Rotate towards movement direction
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
            
            // Move forward
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
            
            // Update animation
            UpdateAnimationState(1f);
        }
        else
        {
            // Idle state
            UpdateAnimationState(0f);
        }
    }
    
    /// <summary>
    /// Updates the animation state with movement speed
    /// </summary>
    private void UpdateAnimationState(float speed)
    {
        if (animator != null)
            animator.SetFloat("Speed", speed);
    }

    /// <summary>
    /// Fires the Attack trigger on left mouse click.
    /// Only called when canSwing is true.
    /// </summary>
    private void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0) && animator != null
            && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            animator.SetTrigger("Attack");
    }

    /// <summary>
    /// Called by SwordSwingHandler when the player reaches a dialogue element
    /// with the Attack component. Permanently enables sword swing.
    /// The speed parameter sets the Animator speed for the attack animation.
    /// </summary>
    public void EnableSwordSwing(float speed = 1f)
    {
        canSwing = true;
        if (animator != null)
            animator.speed = speed;
    }
}