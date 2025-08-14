using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float speed = 7f;
    public Rigidbody2D body;
    public Camera cam;
    
    // Border constraint variables
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private bool boundsInitialized = false;

    Vector2 movement;
    Vector2 mousePos;

    void Start() {
        CalculateBounds();
    }

    void Update() {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
    }

    private void FixedUpdate() {
        Vector2 newPosition = body.position + movement * speed * Time.fixedDeltaTime;
        
        // Clamp the new position within bounds
        if (boundsInitialized) {
            newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
            newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);
        }
        
        body.MovePosition(newPosition);

        Vector2 lookDir = mousePos - body.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        body.rotation = angle;
    }
    
    private void CalculateBounds() {
        GameObject[] borderMarkers = GameObject.FindGameObjectsWithTag("BorderMarker");
        
        if (borderMarkers.Length == 0) {
            Debug.LogWarning("No BorderMarkers found! Make sure to tag your BorderMarker prefabs with 'BorderMarker'");
            return;
        }
        
        // Initialize with first marker position
        minBounds = borderMarkers[0].transform.position;
        maxBounds = borderMarkers[0].transform.position;
        
        // Find the min and max bounds from all border markers
        foreach (GameObject marker in borderMarkers) {
            Vector3 pos = marker.transform.position;
            
            if (pos.x < minBounds.x) minBounds.x = pos.x;
            if (pos.x > maxBounds.x) maxBounds.x = pos.x;
            if (pos.y < minBounds.y) minBounds.y = pos.y;
            if (pos.y > maxBounds.y) maxBounds.y = pos.y;
        }
        
        boundsInitialized = true;
        Debug.Log($"Player bounds calculated: Min({minBounds.x}, {minBounds.y}) Max({maxBounds.x}, {maxBounds.y})");
    }
}
