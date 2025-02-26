using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    
    private Rigidbody rb;
    private Transform cameraTransform;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Assicurati che il Rigidbody non ruoti a causa delle collisioni
        rb.freezeRotation = true;
        
        // Trova la camera principale
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
    }
    
    void Update()
    {
        // Gestisci l'input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Crea il vettore di movimento basato sull'input
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        
        if (direction.magnitude >= 0.1f)
        {
            // Calcola l'angolo di rotazione basato sulla direzione dell'input e sulla camera
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            
            // Ruota gradualmente il personaggio nella direzione del movimento
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.1f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            
            // Muovi il personaggio nella direzione indicata dalla camera
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            rb.MovePosition(rb.position + moveDir * moveSpeed * Time.deltaTime);
        }
    }
    
    private float turnSmoothVelocity;
}