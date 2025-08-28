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

    [Header("Dash Settings")]
    [Tooltip("Distance the player dashes")]
    [Range(3f, 15f)]
    public float dashDistance = 8f;

    [Tooltip("Duration of the dash in seconds")]
    [Range(0.1f, 0.5f)]
    public float dashDuration = 0.2f;

    [Tooltip("Time to recharge one dash charge (in seconds)")]
    [Range(1f, 10f)]
    public float dashRechargeTime = 5f;

    [Tooltip("Maximum number of dash charges")]
    [Range(1, 5)]
    public int maxDashCharges = 2;

    [Tooltip("Time window for double-tap detection (in seconds)")]
    [Range(0.1f, 0.5f)]
    public float doubleTapWindow = 0.3f;

    // Dash system variables
    private int currentDashCharges;
    private float lastRechargeTime;
    private bool isDashing = false;
    private Vector2 dashDirection;
    private float dashTimer;
    private Vector2 dashStartPosition;
    private Vector2 dashTargetPosition;

    // Double-tap detection
    private float lastHorizontalTapTime = -1f;
    private float lastVerticalTapTime = -1f;
    private KeyCode lastHorizontalKey = KeyCode.None;
    private KeyCode lastVerticalKey = KeyCode.None;

    // REMOVE: All camera-related fields
    // private Camera mainCam;
    // private GameObject virtualCameraObject;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 velocity;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        
        // Initialize dash system
        currentDashCharges = maxDashCharges;
        lastRechargeTime = Time.time;
        
        // REMOVE ALL CAMERA SETUP - CinemachineManager handles this now
        
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
        // REMOVE: canvas.worldCamera = mainCam; // Don't reference mainCam

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
        if (!isDashing)
        {
            // Input handling (only when not dashing)
            HandleMovementInput();
            HandleDashInput();
        }
        
        // Update dash charges
        UpdateDashCharges();
        
        // Handle dash movement
        if (isDashing)
        {
            UpdateDash();
        }

        // REMOVE: Make health bar face the camera - let it be world-space
        // The health bar will work fine without camera reference
    }

    void HandleMovementInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;
    }

    void HandleDashInput()
    {
        // Check for double-tap on horizontal keys
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CheckDoubleTap(KeyCode.A, -1f, true);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            CheckDoubleTap(KeyCode.D, 1f, true);
        }
        
        // Check for double-tap on vertical keys
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            CheckDoubleTap(KeyCode.W, 1f, false);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            CheckDoubleTap(KeyCode.S, -1f, false);
        }
    }

    void CheckDoubleTap(KeyCode key, float direction, bool isHorizontal)
    {
        float currentTime = Time.time;
        
        if (isHorizontal)
        {
            // Check if this is a double-tap
            if (lastHorizontalKey == key && currentTime - lastHorizontalTapTime <= doubleTapWindow)
            {
                // Double-tap detected!
                Vector2 dashDir = new Vector2(direction, 0f);
                TryDash(dashDir);
            }
            
            lastHorizontalTapTime = currentTime;
            lastHorizontalKey = key;
        }
        else
        {
            // Check if this is a double-tap
            if (lastVerticalKey == key && currentTime - lastVerticalTapTime <= doubleTapWindow)
            {
                // Double-tap detected!
                Vector2 dashDir = new Vector2(0f, direction);
                TryDash(dashDir);
            }
            
            lastVerticalTapTime = currentTime;
            lastVerticalKey = key;
        }
    }

    void TryDash(Vector2 direction)
    {
        // Check if we have charges and aren't already dashing
        if (currentDashCharges > 0 && !isDashing)
        {
            StartDash(direction);
        }
        else if (currentDashCharges <= 0)
        {
            Debug.Log("No dash charges remaining!");
        }
    }

    void StartDash(Vector2 direction)
    {
        // Use a charge
        currentDashCharges--;
        
        // Set dash state
        isDashing = true;
        dashDirection = direction.normalized;
        dashTimer = 0f;
        
        // Calculate dash positions
        dashStartPosition = rb.position;
        dashTargetPosition = dashStartPosition + (dashDirection * dashDistance);
        
        // Clamp target position to bounds
        if (boundsInitialized)
        {
            Bounds bounds = BorderMarkerUtils.GetBorderBounds();
            if (bounds.size != Vector3.one * 10f)
            {
                dashTargetPosition.x = Mathf.Clamp(dashTargetPosition.x, bounds.min.x, bounds.max.x);
                dashTargetPosition.y = Mathf.Clamp(dashTargetPosition.y, bounds.min.y, bounds.max.y);
            }
        }
        
        Debug.Log($"Dash started! Direction: {dashDirection}, Charges remaining: {currentDashCharges}");
    }

    void UpdateDash()
    {
        dashTimer += Time.deltaTime;
        float progress = dashTimer / dashDuration;
        
        if (progress >= 1f)
        {
            // Dash complete
            progress = 1f;
            isDashing = false;
            rb.MovePosition(dashTargetPosition);
            Debug.Log("Dash completed!");
        }
        else
        {
            // Interpolate position during dash
            Vector2 currentPos = Vector2.Lerp(dashStartPosition, dashTargetPosition, progress);
            rb.MovePosition(currentPos);
        }
    }

    void UpdateDashCharges()
    {
        // Recharge dash if we're not at max charges
        if (currentDashCharges < maxDashCharges)
        {
            if (Time.time >= lastRechargeTime + dashRechargeTime)
            {
                currentDashCharges++;
                lastRechargeTime = Time.time;
                Debug.Log($"Dash recharged! Charges: {currentDashCharges}/{maxDashCharges}");
            }
        }
    }

    void FixedUpdate() {
        if (!isDashing) // Only apply normal movement when not dashing
        {
            if (moveInput.magnitude > 0) {
                velocity = Vector2.MoveTowards(velocity, moveInput * moveSpeed, acceleration * Time.fixedDeltaTime);
            } else {
                velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            }

            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }

        if (boundsInitialized) {
            ClampToBounds();
        }
    }

    void ClampToBounds() {
        if (!boundsInitialized) return;
        
        Vector3 pos = transform.position;

        // Get border bounds for movement constraints
        Bounds bounds = BorderMarkerUtils.GetBorderBounds();
        
        if (bounds.size != Vector3.one * 10f) // Check if we found real markers
        {
            // Player can move right up to the boundary (no padding)
            pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
            pos.y = Mathf.Clamp(pos.y, bounds.min.y, bounds.max.y);
            
            transform.position = pos;
        }
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

    public bool IsDead()
    {
        return healthSystem != null && healthSystem.IsDead;
    }

    public float GetCurrentHealth()
    {
        return healthSystem != null ? healthSystem.CurrentHealth : 0f;
    }

    public float GetMaxHealth()
    {
        return healthSystem != null ? healthSystem.MaxHealth : 0f;
    }

    public float GetHealthPercentage()
    {
        return healthSystem != null ? healthSystem.HealthPercentage : 0f;
    }

    // Add these public methods to check dash status (useful for UI)
    public int GetCurrentDashCharges()
    {
        return currentDashCharges;
    }

    public int GetMaxDashCharges()
    {
        return maxDashCharges;
    }

    public float GetDashRechargeProgress()
    {
        if (currentDashCharges >= maxDashCharges) return 1f;
        
        float timeSinceLastRecharge = Time.time - lastRechargeTime;
        return Mathf.Clamp01(timeSinceLastRecharge / dashRechargeTime);
    }

    public bool IsDashing()
    {
        return isDashing;
    }
}

public class CameraFollower : MonoBehaviour
{
    public Transform target;
    public float followSpeed = 5f;
    
    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }
}