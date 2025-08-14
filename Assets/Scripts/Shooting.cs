using UnityEngine;

public class Shooting : MonoBehaviour
{
    
    public Transform firePoint;
    public GameObject bulletPrefab;

    public float bulletForce = 20f;

    void Update()
    {
        if (Input.GetButtonDown("Fire1")) {
            Shoot();
        } 
    }

    void Shoot() {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D body = bullet.GetComponent<Rigidbody2D>();
        body.AddForce(firePoint.up * bulletForce, ForceMode2D.Impulse);
    }

}
