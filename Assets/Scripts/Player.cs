using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float speed = 7f;
    public Rigidbody2D body;
    public Camera cam;

    Vector2 movement;
    Vector2 mousePos;
    


    void Update() {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
    }

    private void FixedUpdate() {
        body.MovePosition(body.position + movement * speed * Time.fixedDeltaTime);

        Vector2 lookDir = mousePos - body.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        body.rotation = angle;
    }

}
