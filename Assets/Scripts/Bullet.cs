using UnityEngine;

public class Bullet : MonoBehaviour
{
    private CameraController cameraController;
    private bool boundsFound = false;
    private Collider2D bulletCollider;

    private void Start() {
        // Find the camera controller to get the actual bounds
        cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null) {
            boundsFound = true;
        } else {
            Debug.LogWarning("CameraController not found! Bullet bounds checking disabled.");
        }
        
        // Disable collider briefly to avoid immediate collision with player
        bulletCollider = GetComponent<Collider2D>();
        bulletCollider.enabled = false;
        
        // Re-enable collider after a short delay
        Invoke("EnableCollider", 0.1f);
    }
    
    private void EnableCollider() {
        if (bulletCollider != null) {
            bulletCollider.enabled = true;
        }
    }

    private void Update() {
        // Only check bounds if we found the camera controller
        if (boundsFound) {
            // Access the bounds from CameraController (you'll need to make these public)
            // For now, let's use a more generous boundary check
            Vector3 pos = transform.position;
            
            // Check if bullet is way outside reasonable bounds
            if (pos.x < -50f || pos.x > 50f || pos.y < -50f || pos.y > 50f) {
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        Destroy(gameObject);
    }
}
