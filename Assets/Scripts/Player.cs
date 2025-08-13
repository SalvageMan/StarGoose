using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float speed;
    public Rigidbody2D body;

    private void Start() {
        
    }

    void Update() {

        float xInput = Input.GetAxis("Horizontal");
        float yInput = Input.GetAxis("Vertical");

        if (Mathf.Abs(xInput) > 0) {
            body.linearVelocity = new Vector2(xInput * speed, body.linearVelocity.y);
        }

        if (Mathf.Abs(yInput) > 0)
        {
            body.linearVelocity = new Vector2(yInput * speed, body.linearVelocity.x);
        }

        Vector2 direction = new Vector2(xInput, yInput).normalized;
        
        body.linearVelocity = direction * speed;

    }

}
