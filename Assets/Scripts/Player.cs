using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public Rigidbody2D body;

    private void Start() {
        
    }

    void Update() {

        float xInput = Input.GetAxis("Horizontal");
        float yInput = Input.GetAxis("Vertical");

        body.linearVelocity = new Vector2(xInput, yInput);
    }

}
