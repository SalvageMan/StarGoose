using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFlightController : MonoBehaviour {
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float acceleration = 15f;
    public float deceleration = 20f;
    public Vector2 screenPadding = new Vector2(0.5f, 0.5f);

    [Header("Boundary Settings")]
    public bool useCameraBounds = true;
    private Vector3 minBounds;
    private Vector3 maxBounds;
    private bool boundsInitialized = false;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera mainCam;
    private Vector2 velocity;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        if (!useCameraBounds) {
            CalculateBounds();
        } else {
            boundsInitialized = true;
        }
    }

    void Update() {
     
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;
    }

    void FixedUpdate() {
        
        if (moveInput.magnitude > 0) {
            velocity = Vector2.MoveTowards(velocity, moveInput * moveSpeed, acceleration * Time.fixedDeltaTime);
        } else {
            
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
        }

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        if (boundsInitialized) {
            ClampToBounds();
        }
    }

    void ClampToBounds() {
        Vector3 pos = transform.position;

        if (useCameraBounds) {
            Vector3 minCam = mainCam.ViewportToWorldPoint(Vector3.zero);
            Vector3 maxCam = mainCam.ViewportToWorldPoint(Vector3.one);

            pos.x = Mathf.Clamp(pos.x, minCam.x + screenPadding.x, maxCam.x - screenPadding.x);
            pos.y = Mathf.Clamp(pos.y, minCam.y + screenPadding.y, maxCam.y - screenPadding.y);
        } else {
            pos.x = Mathf.Clamp(pos.x, minBounds.x + screenPadding.x, maxBounds.x - screenPadding.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y + screenPadding.y, maxBounds.y - screenPadding.y);
        }

        transform.position = pos;
    }

    private void CalculateBounds() {
        GameObject[] borderMarkers = GameObject.FindGameObjectsWithTag("BorderMarker");

        if (borderMarkers.Length == 0) {
            Debug.LogWarning("No BorderMarkers found! Make sure to tag your BorderMarker prefabs with 'BorderMarker'");
            return;
        }

        minBounds = borderMarkers[0].transform.position;
        maxBounds = borderMarkers[0].transform.position;

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