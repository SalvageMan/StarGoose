using UnityEngine;

public class Bullet : MonoBehaviour {
    private static Camera mainCam; 
    private static CameraController cachedController;
    private bool boundsFound = false;
    private Collider2D bulletCollider;

    [Header("Damage Settings")]
    public float damage = 25f;

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
        if (bulletCollider != null) {
            bulletCollider.enabled = false;
            Invoke(nameof(EnableCollider), 0.1f);
        }
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
        Debug.Log($"Bullet collided with: {collision.gameObject.name}");
        
        // Check if we hit an enemy bullet - destroy ourselves but not the enemy bullet
        EnemyBullet enemyBullet = collision.gameObject.GetComponent<EnemyBullet>();
        if (enemyBullet != null) {
            Debug.Log("Player bullet destroyed by enemy bullet!");
            Destroy(gameObject); // Only destroy the player bullet
            return; // Exit early, don't continue to other collision checks
        }
        
        // Try multiple ways to find the PlanetEnemy component
        PlanetEnemy enemy = collision.gameObject.GetComponent<PlanetEnemy>();
        
        if (enemy != null) {
            Debug.Log($"Found PlanetEnemy component, dealing {damage} damage");
            enemy.TakeDamage(damage);
        } else {
            // Remove this debug spam - it's not needed in production
            // Debug.Log("No PlanetEnemy component found on collision object");
            
            // Try to find it in parent or children (keep this logic, remove excessive logging)
            PlanetEnemy parentEnemy = collision.gameObject.GetComponentInParent<PlanetEnemy>();
            if (parentEnemy != null) {
                Debug.Log("Found PlanetEnemy component in parent");
                parentEnemy.TakeDamage(damage);
            } else {
                PlanetEnemy childEnemy = collision.gameObject.GetComponentInChildren<PlanetEnemy>();
                if (childEnemy != null) {
                    Debug.Log("Found PlanetEnemy component in children");
                    childEnemy.TakeDamage(damage);
                }
            }
        }

        // Destroy the bullet regardless of what it hit (except enemy bullets, handled above)
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        Debug.Log($"Bullet triggered with: {other.gameObject.name}");
        
        // Check if we hit an enemy bullet - destroy ourselves but not the enemy bullet
        EnemyBullet enemyBullet = other.GetComponent<EnemyBullet>();
        if (enemyBullet != null) {
            Debug.Log("Player bullet destroyed by enemy bullet (trigger)!");
            Destroy(gameObject); // Only destroy the player bullet
            return; // Exit early
        }
        
        // Check if we hit a PlanetEnemy (in case it's a trigger)
        PlanetEnemy enemy = other.GetComponent<PlanetEnemy>();
        if (enemy != null) {
            Debug.Log($"Found PlanetEnemy component via trigger, dealing {damage} damage");
            enemy.TakeDamage(damage);
        }

        // Destroy the bullet
        Destroy(gameObject);
    }
}
