using UnityEngine;
using UnityEngine.UI;

public class PlanetEnemy : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 1000f;
    
    private HealthSystem healthSystem;
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private Text healthText;
    private Canvas healthBarCanvas;
    
    [Header("Combat")]
    public bool canShoot = true;

    void Start()
    {
        // Position the PlanetEnemy within the border markers
        PositionPlanetEnemyInBounds();
        
        // Initialize health system
        healthSystem = new HealthSystem(maxHealth);
        healthSystem.OnHealthChanged += OnHealthChanged;
        healthSystem.OnDeath += Die;
        
        CreateHealthBar();
        
        // Add shooting capability
        if (canShoot && GetComponent<EnemyShooting>() == null)
        {
            EnemyShooting shooting = gameObject.AddComponent<EnemyShooting>();
            
            // Create a firepoint for shooting
            CreateFirePoint(shooting);
            
            // ASSIGN THE PREFAB IMMEDIATELY
            shooting.enemyBulletPrefab = Resources.Load<GameObject>("EnemyBullet");
            if (shooting.enemyBulletPrefab == null)
            {
                Debug.LogError("Could not find EnemyBullet prefab in Resources folder!");
            }
            else
            {
                Debug.Log("Successfully assigned EnemyBullet prefab to EnemyShooting component");
            }
        }
        
        Debug.Log($"PlanetEnemy spawned with {healthSystem.CurrentHealth} health at position {transform.position}");
    }
    
    void CreateHealthBar()
    {
        // Position health bar relative to the player/camera position instead of border markers
        Vector3 playerPosition = Vector3.zero;
        PlayerFlightController player = FindFirstObjectByType<PlayerFlightController>();
        if (player != null)
        {
            playerPosition = player.transform.position;
        }
        
        // Position the health bar above the player's view area - find the sweet spot
        Vector3 healthBarPosition = new Vector3(
            playerPosition.x, // Same X as player
            playerPosition.y + 9f, // Reduced from 11f to 8f (between the original 6f and too-high 11f)
            -5f // In front of everything
        );

        Debug.Log($"Creating PlanetEnemy health bar at position: {healthBarPosition}");
        Debug.Log($"Player position: {playerPosition}");

        // Create a Canvas for the health bar
        GameObject canvasGO = new GameObject("PlanetEnemyHealthBarCanvas");
        
        healthBarCanvas = canvasGO.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 0.01f;
        
        canvasGO.transform.position = healthBarPosition;
        canvasGO.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        Debug.Log($"Health bar canvas created at: {canvasGO.transform.position} with scale: {canvasGO.transform.localScale}");

        // Create background - make it a bit bigger
        GameObject backgroundGO = new GameObject("HealthBarBackground");
        backgroundGO.transform.SetParent(canvasGO.transform, false);
        
        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.color = Color.black;
        
        RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(75, 12); // Increased from 50x8 to 75x12 (50% bigger)

        // Create slider - match the bigger size
        GameObject sliderGO = new GameObject("HealthBarSlider");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        
        healthBarSlider = sliderGO.AddComponent<Slider>();
        healthBarSlider.maxValue = maxHealth;
        healthBarSlider.value = healthSystem.CurrentHealth;
        
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(75, 12); // Match background size

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
        fillImage.color = Color.green;
        
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        healthBarSlider.fillRect = fillRect;

        // Create text - slightly bigger font
        GameObject textGO = new GameObject("HealthText");
        textGO.transform.SetParent(canvasGO.transform, false);
        
        healthText = textGO.AddComponent<Text>();
        healthText.text = $"{healthSystem.CurrentHealth}/{healthSystem.MaxHealth} ({healthSystem.HealthPercentage:F0}%)";
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.fontSize = 4; // Increased from 3 to 4
        healthText.color = Color.white;
        healthText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(75, 12); // Match the bigger size
        textRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("PlanetEnemy health bar created successfully");
    }

    public void TakeDamage(float damage)
    {
        healthSystem.TakeDamage(damage);
        Debug.Log($"PlanetEnemy took {damage} damage. Health: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
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
                fillImage.color = Color.green;
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

    private void Die()
    {
        Debug.Log("PlanetEnemy destroyed!");
        
        if (healthBarCanvas != null)
        {
            Destroy(healthBarCanvas.gameObject);
        }
        
        Destroy(gameObject);
    }

    void Update()
    {
        // Make health bar face the camera
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.LookAt(Camera.main.transform);
            healthBarCanvas.transform.Rotate(0, 180, 0);
        }
    }

    void PositionPlanetEnemyInBounds()
    {
        // Use BorderMarkerUtils to get the bounds
        Bounds bounds = BorderMarkerUtils.GetBorderBounds();
        
        if (bounds.size != Vector3.one * 10f) // Check if we found real markers
        {
            // Position the planet enemy somewhere within the bounds (not at center like player)
            // Let's put it at the right side of the play area
            Vector3 planetPosition = new Vector3(
                bounds.center.x + (bounds.size.x * 0.3f), // 30% to the right of center
                bounds.center.y, // Same Y as center
                transform.position.z // Keep original Z
            );
            
            transform.position = planetPosition;
            Debug.Log($"Positioned PlanetEnemy within border bounds at: {planetPosition}");
        }
        else
        {
            Debug.LogWarning("No border markers found - PlanetEnemy will remain at current position");
        }
    }

    void CreateFirePoint(EnemyShooting shooting)
    {
        // Create a child GameObject as the fire point
        GameObject firePointGO = new GameObject("FirePoint");
        firePointGO.transform.SetParent(transform);
        
        // Position it slightly in front of the planet (towards the player)
        firePointGO.transform.localPosition = new Vector3(-1f, 0f, 0f); // 1 unit to the left (towards center)
        
        // Assign it to the shooting component
        shooting.firePoint = firePointGO.transform;
        
        Debug.Log($"Created FirePoint for PlanetEnemy at local position: {firePointGO.transform.localPosition}");
    }
}
