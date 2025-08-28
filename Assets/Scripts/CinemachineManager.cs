using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public enum CameraState
{
    PlayerFollow = 0,
    BossEncounter = 1,
    Cutscene = 2,
    GameOver = 3,
    Victory = 4
}

public class CinemachineManager : MonoBehaviour
{
    [Header("Camera Management")]
    [Tooltip("All virtual cameras in order of states")]
    public VirtualCameraController[] cameras;
    
    [Tooltip("Starting camera state")]
    public CameraState startingState = CameraState.PlayerFollow;
    
    [Tooltip("Transition duration between cameras")]
    [Range(0.1f, 5f)]
    public float transitionDuration = 1f;
    
    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool debugMode = true;
    
    [Tooltip("Force camera state change (for testing)")]
    public CameraState debugForceState = CameraState.PlayerFollow;
    
    [Header("Player Reference")]
    public Transform playerTransform;
    
    [Header("Victory Transition")]
    [Tooltip("Countdown duration after enemy death before switching cameras")]
    [Range(1f, 10f)]
    public float victoryCountdownDuration = 5f;

    [Tooltip("UI Text prefab for countdown display (optional - will create if null)")]
    public GameObject countdownTextPrefab;
    
    private CameraState currentState = (CameraState)(-1);
    private Dictionary<CameraState, VirtualCameraController> cameraDict;
    private Camera mainCamera;
    private object cinemachineBrain;

    private bool isCountingDown = false;
    private GameObject countdownUI;
    private UnityEngine.UI.Text countdownText;
    private float countdownTimer;

    void Start()
    {
        Debug.Log("=== CinemachineManager Starting ===");
        
        // Find main camera and brain
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cinemachineBrain = mainCamera.GetComponent("CinemachineBrain");
            Debug.Log($"Found main camera: {mainCamera.name}");
            Debug.Log($"Cinemachine Brain found: {cinemachineBrain != null}");
            
            if (cinemachineBrain == null)
            {
                Debug.LogError("NO CINEMACHINE BRAIN FOUND ON MAIN CAMERA! This is required for virtual cameras to work.");
            }
        }
        else
        {
            Debug.LogError("NO MAIN CAMERA FOUND!");
        }
        
        InitializeCameras();
        FindPlayer();
        
        // Position player at starting camera location, then set camera state
        PositionPlayerAtCamera(startingState);
        currentState = (CameraState)(-1); // Force state change
        SetCameraState(startingState);
        
        Debug.Log("=== CinemachineManager Setup Complete ===");
    }
    
    void InitializeCameras()
    {
        cameraDict = new Dictionary<CameraState, VirtualCameraController>();
        
        for (int i = 0; i < cameras.Length && i < System.Enum.GetValues(typeof(CameraState)).Length; i++)
        {
            if (cameras[i] != null)
            {
                CameraState state = (CameraState)i;
                cameraDict[state] = cameras[i];
                
                // Disable all cameras initially
                cameras[i].gameObject.SetActive(false);
                
                if (debugMode)
                {
                    Debug.Log($"Registered camera {i}: {cameras[i].cameraDescription} for state {state}");
                }
            }
        }
    }
    
    void FindPlayer()
    {
        if (playerTransform == null)
        {
            var player = FindFirstObjectByType<PlayerFlightController>();
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log($"Found player: {playerTransform.name}");
            }
            else
            {
                Debug.LogError("NO PLAYER FOUND!");
            }
        }
    }
    
    void Update()
    {
        // Debug camera switching
        if (debugMode && Input.GetKeyDown(KeyCode.C))
        {
            SetCameraState(debugForceState);
        }
        
        // Debug victory countdown (for testing)
        if (debugMode && Input.GetKeyDown(KeyCode.V))
        {
            TriggerVictoryCountdown();
        }
        
        // Auto state transitions based on game conditions
        CheckForStateTransitions();
        
        // Handle victory countdown
        if (isCountingDown)
        {
            UpdateVictoryCountdown();
        }
    }

    void LateUpdate()
    {
        // REMOVE: No more manual constraint application
        // The CinemachineConfiner2D handles this automatically
    }
    
    void CheckForStateTransitions()
    {
        // Check for player death
        if (playerTransform != null)
        {
            var player = playerTransform.GetComponent<PlayerFlightController>();
            if (player != null && player.IsDead())
            {
                SetCameraState(CameraState.GameOver);
                return;
            }
        }
        
        // Check for enemy death (victory condition)
        if (!isCountingDown && currentState == CameraState.PlayerFollow)
        {
            CheckForEnemyDeath();
        }
        
        // Add more transition conditions here as needed
    }
    
    void CheckForEnemyDeath()
    {
        // Find all PlanetEnemy objects
        PlanetEnemy[] enemies = FindObjectsByType<PlanetEnemy>(FindObjectsSortMode.None);
        
        // If no enemies are left, start victory countdown
        if (enemies.Length == 0)
        {
            StartVictoryCountdown();
        }
    }

    void StartVictoryCountdown()
    {
        if (isCountingDown) return; // Already counting down
        
        Debug.Log("All enemies defeated! Starting victory countdown...");
        
        isCountingDown = true;
        countdownTimer = victoryCountdownDuration;
        
        CreateCountdownUI();
    }

    void CreateCountdownUI()
    {
        // Create a Canvas for the countdown
        GameObject canvasGO = new GameObject("VictoryCountdownCanvas");
        
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Make sure it's on top
        
        UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        UnityEngine.UI.GraphicRaycaster raycaster = canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        countdownUI = canvasGO;
        
        // Create the countdown text
        GameObject textGO = new GameObject("CountdownText");
        textGO.transform.SetParent(canvasGO.transform, false);
        
        countdownText = textGO.AddComponent<UnityEngine.UI.Text>();
        countdownText.text = Mathf.Ceil(countdownTimer).ToString();
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 100;
        countdownText.color = Color.white;
        countdownText.alignment = TextAnchor.MiddleCenter;
        
        // Add outline for better visibility
        UnityEngine.UI.Outline outline = textGO.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, 3);
        
        // Center the text
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Create victory message
        GameObject victoryTextGO = new GameObject("VictoryText");
        victoryTextGO.transform.SetParent(canvasGO.transform, false);
        
        UnityEngine.UI.Text victoryText = victoryTextGO.AddComponent<UnityEngine.UI.Text>();
        victoryText.text = "VICTORY!";
        victoryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        victoryText.fontSize = 60;
        victoryText.color = Color.yellow;
        victoryText.alignment = TextAnchor.MiddleCenter;
        
        // Add outline
        UnityEngine.UI.Outline victoryOutline = victoryTextGO.AddComponent<UnityEngine.UI.Outline>();
        victoryOutline.effectColor = Color.black;
        victoryOutline.effectDistance = new Vector2(2, 2);
        
        // Position victory text above countdown
        RectTransform victoryRect = victoryTextGO.GetComponent<RectTransform>();
        victoryRect.anchorMin = new Vector2(0, 0.6f);
        victoryRect.anchorMax = new Vector2(1, 0.8f);
        victoryRect.sizeDelta = Vector2.zero;
        victoryRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("Victory countdown UI created");
    }

    void UpdateVictoryCountdown()
    {
        countdownTimer -= Time.deltaTime;
        
        // Update countdown text
        if (countdownText != null)
        {
            int displayNumber = Mathf.CeilToInt(countdownTimer);
            countdownText.text = displayNumber > 0 ? displayNumber.ToString() : "0";
            
            // Make the text pulse/scale for dramatic effect
            float pulseScale = 1f + Mathf.Sin(Time.time * 10f) * 0.1f;
            countdownText.transform.localScale = Vector3.one * pulseScale;
        }
        
        // When countdown reaches 0, switch to next camera
        if (countdownTimer <= 0f)
        {
            CompleteVictoryTransition();
        }
    }

    void CompleteVictoryTransition()
    {
        Debug.Log("Victory countdown complete! Switching to next camera...");
        
        // Clean up countdown UI
        if (countdownUI != null)
        {
            Destroy(countdownUI);
            countdownUI = null;
            countdownText = null;
        }
        
        isCountingDown = false;
        
        // Switch to the second camera (index 1) - BossEncounter camera
        // Move the player to the new camera location
        SetCameraState(CameraState.BossEncounter, true);
        
        // Optional: You could also switch to a specific victory state if you have one
        // SetCameraState(CameraState.Victory, true);
    }
    
    public void SetCameraState(CameraState newState, bool movePlayer = false)
    {
        if (currentState == newState) return;
        
        if (!cameraDict.ContainsKey(newState))
        {
            Debug.LogWarning($"No camera registered for state {newState}!");
            return;
        }
        
        // Optionally move player to new camera position
        if (movePlayer)
        {
            PositionPlayerAtCamera(newState);
        }
        
        // Deactivate current camera
        if (cameraDict.ContainsKey(currentState))
        {
            cameraDict[currentState].gameObject.SetActive(false);
            Debug.Log($"Deactivated camera for state {currentState}");
        }
        
        // Activate new camera
        var newCamera = cameraDict[newState];
        newCamera.gameObject.SetActive(true);
        
        if (debugMode)
        {
            Debug.Log($"Camera transition: {currentState} -> {newState} ({newCamera.cameraDescription})");
        }
        
        currentState = newState;
        
        // Setup camera for player following
        SetupCameraForPlayer(newCamera);
    }
    
    void SetupCameraForPlayer(VirtualCameraController camera)
    {
        if (playerTransform == null || camera == null) 
        {
            Debug.LogError($"Cannot setup camera - playerTransform: {playerTransform != null}, camera: {camera != null}");
            return;
        }
        
        Debug.Log($"=== Setting up camera {camera.cameraDescription} ===");
        
        var cinemachineComponent = camera.GetComponent("CinemachineCamera");
        if (cinemachineComponent == null)
        {
            cinemachineComponent = camera.GetComponent("CinemachineVirtualCamera");
        }
        
        if (cinemachineComponent != null)
        {
            Debug.Log($"Found Cinemachine component: {cinemachineComponent.GetType().Name}");
            
            // Set follow target
            var followProperty = cinemachineComponent.GetType().GetProperty("Follow");
            if (followProperty != null)
            {
                followProperty.SetValue(cinemachineComponent, playerTransform);
                Debug.Log($"✓ Set Follow property for {camera.gameObject.name}");
            }
            
            // Clear LookAt for 2D
            var lookAtProperty = cinemachineComponent.GetType().GetProperty("LookAt");
            if (lookAtProperty != null)
            {
                lookAtProperty.SetValue(cinemachineComponent, null);
                Debug.Log($"✓ Cleared LookAt property for {camera.gameObject.name}");
            }
            
            SetupCinemachineTarget(cinemachineComponent);
            SetupTrackingComponents(camera.gameObject);
            
            // NEW: Setup boundary constraint using Cinemachine's built-in system
            if (camera.enableBoundaryConstraint)
            {
                SetupCinemachineConfiner(camera.gameObject);
            }
            
            Debug.Log($"✓ Camera setup complete for {camera.gameObject.name}");
        }
        else
        {
            Debug.LogError($"No Cinemachine component found on {camera.gameObject.name}!");
        }
    }
    
    void SetupCinemachineTarget(object cinemachineComponent)
    {
        var componentType = cinemachineComponent.GetType();
        var targetField = componentType.GetField("Target");
        
        if (targetField != null)
        {
            Debug.Log("Setting up Cinemachine Target for 2D following");
            var targetValue = targetField.GetValue(cinemachineComponent);
            
            if (targetValue != null)
            {
                var targetType = targetValue.GetType();
                
                // Set TrackingTarget
                var trackingTargetField = targetType.GetField("TrackingTarget");
                if (trackingTargetField != null)
                {
                    trackingTargetField.SetValue(targetValue, playerTransform);
                    Debug.Log("✓ Set TrackingTarget to player");
                }
                
                // Clear LookAtTarget for 2D
                var lookAtTargetField = targetType.GetField("LookAtTarget");
                if (lookAtTargetField != null)
                {
                    lookAtTargetField.SetValue(targetValue, null);
                    Debug.Log("✓ Cleared LookAtTarget for 2D");
                }
                
                targetField.SetValue(cinemachineComponent, targetValue);
            }
        }
    }
    
    void SetupTrackingComponents(GameObject vcamGameObject)
    {
        Debug.Log($"Setting up tracking components for {vcamGameObject.name}");
        
        // Try to add CinemachinePositionComposer for 2D tracking
        var positionComposerType = System.Type.GetType("Unity.Cinemachine.CinemachinePositionComposer, Unity.Cinemachine");
        if (positionComposerType == null)
        {
            positionComposerType = System.Type.GetType("Cinemachine.CinemachinePositionComposer, Cinemachine");
        }
        
        if (positionComposerType != null)
        {
            var existingComponent = vcamGameObject.GetComponent(positionComposerType);
            if (existingComponent == null)
            {
                try
                {
                    var newComponent = vcamGameObject.AddComponent(positionComposerType);
                    Debug.Log($"✓ Added CinemachinePositionComposer to {vcamGameObject.name}");
                    ConfigurePositionComposer(newComponent);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not add CinemachinePositionComposer: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"CinemachinePositionComposer already exists on {vcamGameObject.name}");
                ConfigurePositionComposer(existingComponent);
            }
        }
    }

    void ConfigurePositionComposer(object positionComposer)
    {
        if (positionComposer == null) return;
        
        var composerType = positionComposer.GetType();
        
        try
        {
            // Set dead zone (area where camera doesn't move)
            SetField(composerType, positionComposer, "DeadZoneWidth", 0.2f);
            SetField(composerType, positionComposer, "DeadZoneHeight", 0.2f);
            
            // Set soft zone (area where camera starts to move smoothly)
            SetField(composerType, positionComposer, "SoftZoneWidth", 0.6f);
            SetField(composerType, positionComposer, "SoftZoneHeight", 0.6f);
            
            // Set damping for smooth movement
            SetField(composerType, positionComposer, "HorizontalDamping", 1.5f);
            SetField(composerType, positionComposer, "VerticalDamping", 1.5f);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not configure PositionComposer: {e.Message}");
        }
    }
    
    void SetField(System.Type type, object instance, string fieldName, float value)
    {
        var field = type.GetField(fieldName);
        if (field != null)
        {
            field.SetValue(instance, value);
            Debug.Log($"✓ Set {fieldName} to {value}");
        }
    }
    
    VirtualCameraController GetActiveCameraController()
    {
        if (cameraDict.ContainsKey(currentState))
        {
            return cameraDict[currentState];
        }
        return null;
    }
    
    void PositionPlayerAtCamera(CameraState cameraState)
    {
        if (!cameraDict.ContainsKey(cameraState) || playerTransform == null) return;
        
        var camera = cameraDict[cameraState];
        if (camera == null) return;
        
        // Position player at camera's location (keep player's Z)
        Vector3 cameraPos = camera.transform.position;
        Vector3 playerPos = new Vector3(cameraPos.x, cameraPos.y, playerTransform.position.z);
        playerTransform.position = playerPos;
        
        if (debugMode)
        {
            Debug.Log($"Positioned player at camera {cameraState} location: {playerPos}");
        }
    }

    void SetupCinemachineConfiner(GameObject vcamGameObject)
    {
        Debug.Log($"Setting up Cinemachine Confiner for {vcamGameObject.name}");
        
        // Try to find CinemachineConfiner2D component type
        var confinerType = System.Type.GetType("Unity.Cinemachine.CinemachineConfiner2D, Unity.Cinemachine");
        if (confinerType == null)
        {
            confinerType = System.Type.GetType("Cinemachine.CinemachineConfiner2D, Cinemachine");
        }
        
        if (confinerType != null)
        {
            var existingConfiner = vcamGameObject.GetComponent(confinerType);
            if (existingConfiner == null)
            {
                try
                {
                    var confiner = vcamGameObject.AddComponent(confinerType);
                    Debug.Log($"✓ Added CinemachineConfiner2D to {vcamGameObject.name}");
                    
                    // Create and assign boundary collider
                    SetupBoundaryCollider(confiner);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not add CinemachineConfiner2D: {e.Message}");
                    Debug.LogWarning("Falling back to manual constraint system");
                }
            }
            else
            {
                Debug.Log($"CinemachineConfiner2D already exists on {vcamGameObject.name}");
                SetupBoundaryCollider(existingConfiner);
            }
        }
        else
        {
            Debug.LogWarning("CinemachineConfiner2D not found. Using manual constraint system.");
        }
    }

    void SetupBoundaryCollider(object confiner)
    {
        if (confiner == null) return;
        
        var bounds = BorderMarkerUtils.GetBorderBounds();
        if (bounds.size == Vector3.one * 10f) 
        {
            Debug.LogWarning("No valid border bounds found for confiner");
            return;
        }
        
        // Create a GameObject with PolygonCollider2D for the boundary
        GameObject boundaryObject = GameObject.Find("CameraBoundary");
        if (boundaryObject == null)
        {
            boundaryObject = new GameObject("CameraBoundary");
            boundaryObject.transform.position = Vector3.zero;
        }
        
        PolygonCollider2D boundaryCollider = boundaryObject.GetComponent<PolygonCollider2D>();
        if (boundaryCollider == null)
        {
            boundaryCollider = boundaryObject.AddComponent<PolygonCollider2D>();
        }
        
        // Create boundary polygon from border markers
        Vector2[] boundaryPoints = new Vector2[]
        {
            new Vector2(bounds.min.x, bounds.min.y), // Bottom-left
            new Vector2(bounds.max.x, bounds.min.y), // Bottom-right
            new Vector2(bounds.max.x, bounds.max.y), // Top-right
            new Vector2(bounds.min.x, bounds.max.y)  // Top-left
        };
        
        boundaryCollider.points = boundaryPoints;
        boundaryCollider.isTrigger = true; // Important: make it a trigger
        
        // Assign the collider to the confiner
        var confinerType = confiner.GetType();
        var boundingShapeField = confinerType.GetField("BoundingShape2D");
        if (boundingShapeField == null)
        {
            boundingShapeField = confinerType.GetField("m_BoundingShape2D");
        }
        
        if (boundingShapeField != null)
        {
            boundingShapeField.SetValue(confiner, boundaryCollider);
            Debug.Log($"✓ Assigned boundary collider to confiner");
            Debug.Log($"Boundary: {bounds.min} to {bounds.max}");
        }
        else
        {
            Debug.LogWarning("Could not find BoundingShape2D field on confiner");
        }
    }
    
    // Public methods for external scripts
    public void SwitchToBossCamera(bool movePlayer = true) => SetCameraState(CameraState.BossEncounter, movePlayer);
    public void SwitchToPlayerCamera(bool movePlayer = false) => SetCameraState(CameraState.PlayerFollow, movePlayer);
    public void SwitchToCutsceneCamera(bool movePlayer = true) => SetCameraState(CameraState.Cutscene, movePlayer);
    public void SwitchToGameOverCamera(bool movePlayer = false) => SetCameraState(CameraState.GameOver, movePlayer);
    public void SwitchToVictoryCamera(bool movePlayer = false) => SetCameraState(CameraState.Victory, movePlayer);
    
    public CameraState GetCurrentState() => currentState;

    public void TriggerVictoryCountdown()
    {
        if (debugMode)
        {
            StartVictoryCountdown();
        }
    }
}
