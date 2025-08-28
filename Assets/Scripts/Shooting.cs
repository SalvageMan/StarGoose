using UnityEngine;

public class Shooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    
    [Tooltip("Force applied to bullets when fired")]
    [Range(5f, 50f)]
    public float bulletForce = 20f;
    
    [Header("Auto Shooting")]
    [Tooltip("Rate of fire when holding spacebar (bullets per second)")]
    [Range(1f, 20f)] // This creates a slider from 1 to 20 bullets per second
    public float fireRate = 5f;
    
    [Tooltip("Delay before auto-fire starts when holding spacebar (in seconds)")]
    [Range(0f, 1f)] // Slider from 0 to 1 second
    public float autoFireDelay = 0.1f;
    
    private float nextFireTime = 0f;
    private bool isHoldingFire = false;
    private float holdStartTime = 0f;

    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        // Check if spacebar is pressed down (first frame)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Shoot immediately on first press
            Shoot();
            
            // Start tracking hold time
            isHoldingFire = true;
            holdStartTime = Time.time;
            nextFireTime = Time.time + (1f / fireRate); // Set next fire time
        }
        
        // Check if spacebar is being held down
        else if (Input.GetKey(KeyCode.Space) && isHoldingFire)
        {
            // Only start auto-fire after the delay
            if (Time.time >= holdStartTime + autoFireDelay)
            {
                // Check if enough time has passed for next shot
                if (Time.time >= nextFireTime)
                {
                    Shoot();
                    nextFireTime = Time.time + (1f / fireRate);
                }
            }
        }
        
        // Check if spacebar is released
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            isHoldingFire = false;
        }
        
        // Fallback: if spacebar is not being held, stop auto-fire
        if (!Input.GetKey(KeyCode.Space))
        {
            isHoldingFire = false;
        }
    }

    void Shoot() 
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Bullet prefab not assigned to Shooting component!");
            return;
        }
        
        if (firePoint == null)
        {
            Debug.LogWarning("Fire point not assigned to Shooting component!");
            return;
        }
        
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D body = bullet.GetComponent<Rigidbody2D>();
        
        if (body != null)
        {
            body.AddForce(firePoint.up * bulletForce, ForceMode2D.Impulse);
        }
        else
        {
            Debug.LogWarning("Bullet prefab doesn't have a Rigidbody2D component!");
        }
    }
}
