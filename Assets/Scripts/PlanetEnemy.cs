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
        // Initialize health system
        healthSystem = new HealthSystem(maxHealth);
        healthSystem.OnHealthChanged += OnHealthChanged;
        healthSystem.OnDeath += Die;
        
        CreateHealthBar();
        
        // Add shooting capability
        if (canShoot && GetComponent<EnemyShooting>() == null)
        {
            EnemyShooting shooting = gameObject.AddComponent<EnemyShooting>();
            
            // ASSIGN THE PREFAB IMMEDIATELY
            shooting.enemyBulletPrefab = Resources.Load<GameObject>("EnemyBullet");
            if (shooting.enemyBulletPrefab == null)
            {
                Debug.LogError("Could not find EnemyBullet prefab in Resources folder! Make sure EnemyBullet prefab is in a Resources folder, or assign it manually in the inspector.");
            }
            else
            {
                Debug.Log("Successfully assigned EnemyBullet prefab to EnemyShooting component");
            }
        }
        
        Debug.Log($"PlanetEnemy spawned with {healthSystem.CurrentHealth} health");
    }
    
    void CreateHealthBar()
    {
        // Use BorderMarkerUtils to get position
        Vector3 healthBarPosition = BorderMarkerUtils.GetTopBorderPosition() + Vector3.down;

        // Create a Canvas for the health bar
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        
        healthBarCanvas = canvasGO.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 0.001f;
        
        canvasGO.transform.position = healthBarPosition;
        canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

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
        fillImage.color = Color.green;
        
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
        healthText.text = $"{healthSystem.CurrentHealth}/{healthSystem.MaxHealth} ({healthSystem.HealthPercentage:F0}%)";
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
}
