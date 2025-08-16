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
    public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);
    private GameObject healthBarCanvas;
    private Slider healthBarSlider;
    private Text healthText;

    [Header("Camera Settings")]
    public string virtualCameraName = "VirtualCameraOne";

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera mainCam;
    private Vector2 velocity;
    private GameObject virtualCameraObject;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        
        // Position player within border markers FIRST
        PositionPlayerInBorderCenter();
        
        // THEN setup camera references so it positions correctly
        SetupCameraReferences();

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

    void SetupCameraReferences()
    {
        // Get the main camera for UI and bounds calculations
        mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = FindFirstObjectByType<Camera>();
        }

        // Find the virtual camera object
        virtualCameraObject = GameObject.Find(virtualCameraName);
        if (virtualCameraObject != null)
        {
            // Try the newer CinemachineCamera component first
            var virtualCameraComponent = virtualCameraObject.GetComponent("CinemachineCamera");
            
            // If not found, try the older CinemachineVirtualCamera component
            if (virtualCameraComponent == null)
            {
                virtualCameraComponent = virtualCameraObject.GetComponent("CinemachineVirtualCamera");
            }
            
            if (virtualCameraComponent != null)
            {
                // Set Follow property using reflection
                var followProperty = virtualCameraComponent.GetType().GetProperty("Follow");
                if (followProperty != null)
                {
                    followProperty.SetValue(virtualCameraComponent, transform);
                    Debug.Log($"Set Follow target to player for {virtualCameraComponent.GetType().Name}");
                }

                // Set LookAt property using reflection (if it exists)
                var lookAtProperty = virtualCameraComponent.GetType().GetProperty("LookAt");
                if (lookAtProperty != null)
                {
                    lookAtProperty.SetValue(virtualCameraComponent, transform);
                    Debug.Log($"Set LookAt target to player for {virtualCameraComponent.GetType().Name}");
                }

                // Position the virtual camera over the player initially with proper Z distance
                Vector3 cameraPosition = new Vector3(
                    transform.position.x,
                    transform.position.y,
                    -10f // Proper camera distance for 2D games
                );
                virtualCameraObject.transform.position = cameraPosition;
                Debug.Log($"Positioned {virtualCameraName} at: {cameraPosition}");

                // Try to set the priority to make sure this camera is active
                var priorityProperty = virtualCameraComponent.GetType().GetProperty("Priority");
                if (priorityProperty != null)
                {
                    priorityProperty.SetValue(virtualCameraComponent, 10); // High priority
                    Debug.Log($"Set {virtualCameraName} priority to 10");
                }

                Debug.Log($"Successfully attached {virtualCameraName} to player using {virtualCameraComponent.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"Found {virtualCameraName} but it doesn't have a CinemachineCamera or CinemachineVirtualCamera component");
            }
        }
        else
        {
            Debug.LogWarning($"Could not find virtual camera named '{virtualCameraName}' in the hierarchy");
        }
    }

    void PositionVirtualCameraInBounds()
    {
        if (virtualCameraObject == null) return;

        // Use BorderMarkerUtils to get the bounds (same as the original camera system)
        Bounds bounds = BorderMarkerUtils.GetBorderBounds();
        
        if (bounds.size == Vector3.one * 10f) // Default bounds means no markers found
        {
            Debug.LogWarning("No BorderMarkers found for virtual camera positioning!");
            return;
        }

        // Position the virtual camera at the center of the border marker area
        Vector3 centerPosition = new Vector3(
            bounds.center.x,
            bounds.center.y,
            virtualCameraObject.transform.position.z // Keep the original Z position
        );
        
        virtualCameraObject.transform.position = centerPosition;
        
        Debug.Log($"Positioned {virtualCameraName} at center of border bounds: {centerPosition}");
        
        // Also try to set the camera's orthographic size to fit the bounds (if it's orthographic)
        var virtualCameraComponent = virtualCameraObject.GetComponent("CinemachineVirtualCamera");
        if (virtualCameraComponent != null)
        {
            // Try to get the lens settings and adjust orthographic size
            var lensProperty = virtualCameraComponent.GetType().GetProperty("m_Lens");
            if (lensProperty != null)
            {
                var lensValue = lensProperty.GetValue(virtualCameraComponent);
                var orthographicSizeField = lensValue.GetType().GetField("OrthographicSize");
                
                if (orthographicSizeField != null)
                {
                    // Calculate the orthographic size to fit the vertical bounds
                    float totalHeight = bounds.size.y;
                    float newOrthographicSize = totalHeight / 2f;
                    
                    // Clamp to reasonable values
                    newOrthographicSize = Mathf.Clamp(newOrthographicSize, 3f, 20f);
                    
                    orthographicSizeField.SetValue(lensValue, newOrthographicSize);
                    lensProperty.SetValue(virtualCameraComponent, lensValue);
                    
                    Debug.Log($"Set {virtualCameraName} orthographic size to {newOrthographicSize} to fit bounds");
                }
            }
        }
    }

    void CreateHealthBar()
    {
        // Create a Canvas for the health bar
        GameObject canvasGO = new GameObject("PlayerHealthBarCanvas");
        canvasGO.transform.SetParent(transform); // Parent to player so it follows

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCam; // Use main camera for UI

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 0.001f;

        // Position relative to player
        canvasGO.transform.localPosition = healthBarOffset;
        canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        healthBarCanvas = canvasGO;

        // Create background
        GameObject backgroundGO = new GameObject("HealthBarBackground");
        backgroundGO.transform.SetParent(canvasGO.transform, false);

        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.color = Color.black;

        RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(200, 40);

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
        fillImage.color = Color.blue;

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
        gameObject.SetActive(false);
    }

    void Update() {
        // Input handling
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        // Make health bar face the camera
        if (healthBarCanvas != null && mainCam != null)
        {
            healthBarCanvas.transform.LookAt(mainCam.transform);
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

    void PositionPlayerInBorderCenter()
    {
        // Use BorderMarkerUtils to get the bounds
        Bounds bounds = BorderMarkerUtils.GetBorderBounds();
        
        if (bounds.size != Vector3.one * 10f) // Check if we found real markers
        {
            // Position the player at the center of the border marker area
            Vector3 centerPosition = new Vector3(
                bounds.center.x,
                bounds.center.y,
                transform.position.z // Keep the original Z position
            );
            
            transform.position = centerPosition;
            Debug.Log($"Positioned player at center of border bounds: {centerPosition}");
        }
        else
        {
            Debug.LogWarning("No border markers found - player will remain at current position");
        }
    }

    // Public method to change virtual camera at runtime if needed
    public void SetVirtualCamera(string cameraName)
    {
        virtualCameraName = cameraName;
        SetupCameraReferences();
    }
}