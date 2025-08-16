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
        
        // Ensure proper scale and color with more aggressive settings
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            sr.color = Color.red;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 100; // Very high sorting order
            sr.enabled = true;
            
            // Make it bigger temporarily to see if size is the issue
            transform.localScale = Vector3.one * 20f; // Keep the visual size you like
        }
        
        // Fix the collision size - make it smaller than the visual
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            if (collider is CircleCollider2D circleCol)
            {
                circleCol.radius = 0.05f; // Much smaller collision radius
            }
            else if (collider is BoxCollider2D boxCol)
            {
                boxCol.size = new Vector2(0.1f, 0.1f); // Much smaller collision box
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

        // Same bounds checking as your regular Bullet.cs
        Vector3 minCam = mainCam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 maxCam = mainCam.ViewportToWorldPoint(new Vector3(1, 1, 0));

        float margin = 1f;
        Vector3 pos = transform.position;
        if (pos.x < minCam.x - margin || pos.x > maxCam.x + margin ||
            pos.y < minCam.y - margin || pos.y > maxCam.y + margin) {
            Debug.Log($"EnemyBullet destroyed - out of bounds at {pos}");
            Destroy(gameObject);
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
