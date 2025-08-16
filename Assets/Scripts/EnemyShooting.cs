using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public Transform firePoint;
    public GameObject enemyBulletPrefab;
    public float bulletForce = 5f;
    public float bulletForceVariation = 0.5f; // How much to vary the speed (0.5 = 50% variation)
    
    [Header("Auto Shooting")]
    public float fireRate = 1f;
    private float nextFireTime = 0f;
    
    private Transform player;
    private GameObject cachedBulletPrefab; // Cache the prefab to prevent loss

    void Start()
    {
        PlayerFlightController playerController = FindFirstObjectByType<PlayerFlightController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        
        if (firePoint == null)
        {
            firePoint = transform;
        }

        // Cache the prefab reference to prevent it from being lost
        if (enemyBulletPrefab != null)
        {
            cachedBulletPrefab = enemyBulletPrefab;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Restore prefab reference if it gets lost
        if (enemyBulletPrefab == null && cachedBulletPrefab != null)
        {
            enemyBulletPrefab = cachedBulletPrefab;
        }

        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        // Use cached prefab if main one is null
        GameObject prefabToUse = enemyBulletPrefab != null ? enemyBulletPrefab : cachedBulletPrefab;
        
        if (prefabToUse == null)
        {
            Debug.LogWarning("Enemy bullet prefab not assigned!");
            return;
        }

        Vector3 directionToPlayer = (player.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;
        Quaternion bulletRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Use the cached prefab to avoid reference issues
        GameObject bullet = Instantiate(prefabToUse, firePoint.position, bulletRotation);
        
        // Force visibility settings
        bullet.transform.position = new Vector3(bullet.transform.position.x, bullet.transform.position.y, -1f);
        
        // Variable bullet speed - random between half speed and full speed
        float randomSpeedMultiplier = Random.Range(bulletForceVariation, 1f);
        float actualBulletForce = bulletForce * randomSpeedMultiplier;
        
        Rigidbody2D body = bullet.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.AddForce(directionToPlayer * actualBulletForce, ForceMode2D.Impulse);
        }
        
    }
}
