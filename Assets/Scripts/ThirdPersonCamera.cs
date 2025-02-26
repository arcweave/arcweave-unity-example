using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;          // Il target che la camera segue (CameraTarget)
    public float distance = 5.0f;     // Distanza della camera dal target
    public float height = 2.0f;       // Altezza della camera rispetto al target
    public float smoothSpeed = 10.0f; // Velocità di lerp per il movimento fluido

    [Header("Camera Controls")]
    public float mouseSensitivity = 3.0f;
    public float minVerticalAngle = -30.0f;
    public float maxVerticalAngle = 60.0f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera target not assigned. Please assign a target in the inspector.");
        }
        
        // Inizializza la rotazione Y in base alla rotazione iniziale
        rotationY = transform.eulerAngles.y;
        
        // Opzionale: nascondi e blocca il cursore
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Controlla l'input del mouse per la rotazione della camera
        rotationX += -Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
        
        // Limita l'angolo verticale
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        
        // Calcola la rotazione della camera
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        
        // Calcola la posizione della camera
        Vector3 negDistance = new Vector3(0.0f, height, -distance);
        Vector3 position = rotation * negDistance + target.position;
        
        // Applica posizione e rotazione in modo fluido
        transform.position = Vector3.Lerp(transform.position, position, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, smoothSpeed * Time.deltaTime);
    }
}