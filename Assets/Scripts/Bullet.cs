using UnityEngine;

public class Bullet : MonoBehaviour {
    private static Camera mainCam; 
    private bool boundsFound = false;
    private Collider2D bulletCollider;

    [Header("Damage Settings")]
    public float damage = 25f;

    private void Start() {
        
        if (mainCam == null) {
            mainCam = Camera.main;
            if (mainCam == null) {
                mainCam = FindFirstObjectByType<Camera>();
            }
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

        // Use border markers for bullet destruction instead of camera bounds
        Bounds playAreaBounds = BorderMarkerUtils.GetBorderBounds();
        
        // If we have valid border markers, use them
        if (playAreaBounds.size != Vector3.one * 10f) 
        {
            // Give bullets a generous margin beyond the play area before destroying them
            float margin = 5f; // Much larger margin
            
            Vector3 pos = transform.position;
            if (pos.x < playAreaBounds.min.x - margin || pos.x > playAreaBounds.max.x + margin ||
                pos.y < playAreaBounds.min.y - margin || pos.y > playAreaBounds.max.y + margin) {
                Debug.Log($"Bullet destroyed - outside play area bounds at {pos}");
                Destroy(gameObject);
            }
        }
        else
        {
            // Fallback to camera bounds with much larger margin if no border markers
            Vector3 minCam = mainCam.ViewportToWorldPoint(new Vector3(0, 0, 0));
            Vector3 maxCam = mainCam.ViewportToWorldPoint(new Vector3(1, 1, 0));

            float margin = 10f; // Much larger margin than before

            Vector3 pos = transform.position;
            if (pos.x < minCam.x - margin || pos.x > maxCam.x + margin ||
                pos.y < minCam.y - margin || pos.y > maxCam.y + margin) {
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        Debug.Log($"Bullet collided with: {collision.gameObject.name}");
        
        // Ignore the camera boundary - it's just for camera constraint, not bullet collision
        if (collision.gameObject.name == "CameraBoundary") {
            Debug.Log("Bullet ignored CameraBoundary collision");
            return; // Don't destroy, just ignore
        }
        
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

        // Destroy the bullet regardless of what it hit (except enemy bullets and camera boundary, handled above)
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        Debug.Log($"Bullet triggered with: {other.gameObject.name}");
        
        // Ignore the camera boundary - it's just for camera constraint, not bullet collision
        if (other.gameObject.name == "CameraBoundary") {
            Debug.Log("Bullet ignored CameraBoundary trigger");
            return; // Don't destroy, just ignore
        }
        
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
