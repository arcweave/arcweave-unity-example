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
        // Input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Movimento
        Vector3 movement = new Vector3(horizontal, 0f, vertical);
        
        if (movement.magnitude > 0)
        {
            // Rotazione basata sulla camera
            float targetAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            Quaternion rotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

            // Movimento nella direzione dello sguardo
            Vector3 moveDirection = rotation * Vector3.forward;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
            
            // Animazione
            if (animator != null)
                animator.SetFloat("Speed", movement.magnitude);
        }
        else if (animator != null)
        {
            animator.SetFloat("Speed", 0);
        }
    }
}