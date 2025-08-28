using UnityEngine;

public class EnemyBullet : MonoBehaviour {
    private static Camera mainCam; 
    private bool boundsFound = false;
    private Collider2D bulletCollider;

    [Header("Damage Settings")]
    public float damage = 10f;

    private void Start() {
        if (mainCam == null) {
            mainCam = Camera.main;
        }

        boundsFound = mainCam != null;

        if (!boundsFound) {
            Debug.LogWarning("Main camera not found! Enemy bullet bounds checking disabled.");
        }

        // ONLY set up this bullet - don't create any new ones
        // Make sure it's visible and at the right depth
        transform.position = new Vector3(transform.position.x, transform.position.y, -2f); // Even further forward
        
        // Ensure proper scale and color with better visibility
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            sr.color = Color.white; // Keep natural asteroid color, or use Color.red if you want it red
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 100;
            sr.enabled = true;
            
            // Scale down the asteroid - try different values to get the right size
            transform.localScale = Vector3.one * 0.3f; // Much smaller - try 0.1f, 0.2f, 0.3f, 0.5f etc.
        }
        
        // Fix the collision size - make it match the visual size better
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            if (collider is CircleCollider2D circleCol)
            {
                circleCol.radius = 5.15f; // Much bigger than 0.05f - should match the visual asteroid size
            }
            else if (collider is BoxCollider2D boxCol)
            {
                boxCol.size = new Vector2(0.3f, 0.3f); // Much bigger collision box
            }
        }
        
        // Standard collider setup like your regular bullets
        bulletCollider = GetComponent<Collider2D>();
        if (bulletCollider != null) {
            bulletCollider.enabled = false;
            Invoke(nameof(EnableCollider), 0.1f);
        }
        
        // Make the bullet kinematic so it doesn't get pushed by physics
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) {
            rb.bodyType = RigidbodyType2D.Kinematic; // Won't be affected by forces after initial velocity
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
                Debug.Log($"EnemyBullet destroyed - outside play area bounds at {pos}");
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
                Debug.Log($"EnemyBullet destroyed - out of bounds at {pos}");
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        // Check if we hit a player bullet - ignore collision, don't destroy
        Bullet playerBullet = collision.gameObject.GetComponent<Bullet>();
        if (playerBullet != null) {
            Debug.Log("Enemy bullet hit by player bullet - continuing flight");
            return; // Don't destroy, just ignore and continue
        }

        // Ignore collisions with PlanetEnemy (the thing that shot us)
        if (collision.gameObject.GetComponent<PlanetEnemy>() != null) {
            return; // Don't destroy, just ignore
        }

        // Check if we hit the player
        PlayerFlightController player = collision.gameObject.GetComponent<PlayerFlightController>();
        if (player != null) {
            Debug.Log($"Enemy bullet hit player for {damage} damage!");
            player.TakeDamage(damage);
            Destroy(gameObject); // Only destroy when hitting the player
            return;
        }

        // For anything else (walls, borders, etc.), destroy the bullet
        Debug.Log($"Enemy bullet hit {collision.gameObject.name} - destroying");
        Destroy(gameObject);
    }
}
