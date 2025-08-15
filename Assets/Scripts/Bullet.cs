using UnityEngine;

public class Bullet : MonoBehaviour {
    private static Camera mainCam; 
    private static CameraController cachedController;
    private bool boundsFound = false;
    private Collider2D bulletCollider;

    private void Start() {
        
        if (mainCam == null) {
            mainCam = Camera.main;
        }
        if (cachedController == null) {
            cachedController = FindFirstObjectByType<CameraController>();
        }

        boundsFound = mainCam != null;

        if (!boundsFound) {
            Debug.LogWarning("Main camera not found! Bullet bounds checking disabled.");
        }

        
        bulletCollider = GetComponent<Collider2D>();
        bulletCollider.enabled = false;
        Invoke(nameof(EnableCollider), 0.1f);
    }

    private void EnableCollider() {
        if (bulletCollider != null) {
            bulletCollider.enabled = true;
        }
    }

    private void Update() {
        if (!boundsFound) return;

        
        Vector3 minCam = mainCam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 maxCam = mainCam.ViewportToWorldPoint(new Vector3(1, 1, 0));

        
        float margin = 1f;

        Vector3 pos = transform.position;
        if (pos.x < minCam.x - margin || pos.x > maxCam.x + margin ||
            pos.y < minCam.y - margin || pos.y > maxCam.y + margin) {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        Destroy(gameObject);
    }
}
