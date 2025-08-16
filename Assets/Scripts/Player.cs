using UnityEngine;
using UnityEngine.UI;

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

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private HealthSystem healthSystem;

    [Header("Health Bar Settings")]
    public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0); // Offset above player
    private GameObject healthBarCanvas;
    private Slider healthBarSlider;
    private Text healthText;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera mainCam;
    private Vector2 velocity;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        // Initialize health system
        healthSystem = new HealthSystem(maxHealth);
        healthSystem.OnHealthChanged += OnHealthChanged;
        healthSystem.OnDeath += OnPlayerDeath;

        CreateHealthBar();

        if (!useCameraBounds) {
            CalculateBounds();
        } else {
            boundsInitialized = true;
        }

        Debug.Log($"Player spawned with {healthSystem.CurrentHealth} health");
    }

    void CreateHealthBar()
    {
        // Create a Canvas for the health bar
        GameObject canvasGO = new GameObject("PlayerHealthBarCanvas");
        canvasGO.transform.SetParent(transform); // Parent to player so it follows

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 0.001f; // Same scale as enemy health bar

        // Position relative to player
        canvasGO.transform.localPosition = healthBarOffset;
        canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Same scale as enemy

        healthBarCanvas = canvasGO;

        // Create background
        GameObject backgroundGO = new GameObject("HealthBarBackground");
        backgroundGO.transform.SetParent(canvasGO.transform, false);

        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.color = Color.black;

        RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(200, 40); // Same size as enemy health bar

        // Create slider
        GameObject sliderGO = new GameObject("HealthBarSlider");
        sliderGO.transform.SetParent(canvasGO.transform, false);

        healthBarSlider = sliderGO.AddComponent<Slider>();
        healthBarSlider.maxValue = maxHealth;
        healthBarSlider.value = healthSystem.CurrentHealth;

        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(200, 40);

        // Create fill area and fill
        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);

        RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);

        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = Color.blue; // Different color from enemy (blue instead of green)

        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        healthBarSlider.fillRect = fillRect;

        // Create text
        GameObject textGO = new GameObject("HealthText");
        textGO.transform.SetParent(canvasGO.transform, false);

        healthText = textGO.AddComponent<Text>();
        healthText.text = $"{healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0} ({healthSystem.HealthPercentage:F0}%)";
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.fontSize = 14;
        healthText.color = Color.white;
        healthText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200, 40);
        textRect.anchoredPosition = Vector2.zero;
    }

    public void TakeDamage(float damage)
    {
        healthSystem.TakeDamage(damage);
        Debug.Log($"Player took {damage} damage. Health: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
    }

    private void OnHealthChanged(float newHealth)
    {
        // Update health bar
        if (healthBarSlider != null)
        {
            healthBarSlider.value = newHealth;

            // Change color based on health percentage
            Image fillImage = healthBarSlider.fillRect.GetComponent<Image>();
            float healthPercent = newHealth / healthSystem.MaxHealth;

            if (healthPercent > 0.6f)
                fillImage.color = Color.blue;
            else if (healthPercent > 0.3f)
                fillImage.color = Color.yellow;
            else
                fillImage.color = Color.red;
        }

        // Update text
        if (healthText != null)
        {
            healthText.text = $"{healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0} ({healthSystem.HealthPercentage:F0}%)";
        }
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player died!");
        // Add death logic here (restart level, game over screen, etc.)
        // For now, just disable the player
        gameObject.SetActive(false);
    }

    void Update() {
        // Input handling
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        // Make health bar face the camera (same as enemy health bar)
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.LookAt(Camera.main.transform);
            healthBarCanvas.transform.Rotate(0, 180, 0);
        }
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