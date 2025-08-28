using UnityEngine;

public class VirtualCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Controls how much of the game world is visible. Smaller = closer view, Larger = wider view")]
    [Range(3f, 15f)]
    public float cameraSize = 8f;
    
    [Tooltip("Update camera size in real-time during play (for testing)")]
    public bool updateSizeInRealTime = false;
    
    [Header("Camera Position")]
    [Tooltip("Z position for this camera (should be negative for 2D games)")]
    public float cameraZPosition = -10f;
    
    [Header("Boundary Settings")]
    [Tooltip("Should this camera be constrained by border markers?")]
    public bool enableBoundaryConstraint = true;
    
    [Header("Camera Info")]
    [Tooltip("Descriptive name for this camera state")]
    public string cameraDescription = "Default Camera";
    
    [Tooltip("Priority of this camera (higher = more likely to be active)")]
    public int basePriority = 10;
    
    private object cinemachineComponent;
    private float lastCameraSize;
    
    void Start()
    {
        // Find the Cinemachine component
        cinemachineComponent = GetComponent("CinemachineCamera");
        if (cinemachineComponent == null)
        {
            cinemachineComponent = GetComponent("CinemachineVirtualCamera");
        }
        
        if (cinemachineComponent != null)
        {
            SetCameraSize(cameraSize);
            lastCameraSize = cameraSize;
            
            // Set the correct Z position after a short delay to ensure Cinemachine is ready
            Invoke(nameof(SetCameraZPosition), 0.1f);
            
            // REMOVE: Only log in debug scenarios, or remove entirely
            // Debug.Log($"{gameObject.name} boundary constraint enabled: {enableBoundaryConstraint}");
        }
        else
        {
            Debug.LogWarning($"No Cinemachine component found on {gameObject.name}!");
        }
    }
    
    void LateUpdate()
    {
        if (updateSizeInRealTime && cinemachineComponent != null)
        {
            if (Mathf.Abs(cameraSize - lastCameraSize) > 0.01f)
            {
                SetCameraSize(cameraSize);
                lastCameraSize = cameraSize;
            }
        }
        
        // REMOVE: All these commented-out lines - they're no longer needed
        // REMOVE ALL THE BOUNDARY CONSTRAINT CODE FROM HERE
        // The CinemachineBoundaryConstraint script handles this now
        // 
        // Remove this entire section:
        // if (constrainToBounds)
        // {
        //     Debug.Log($"[{gameObject.name}] About to clamp - Priority: {GetPriority()}");
        //     ClampCameraToBounds();
        // }
    }
    
    void SetCameraSize(float size)
    {
        if (cinemachineComponent == null) return;
        
        var componentType = cinemachineComponent.GetType();
        var lensField = componentType.GetField("Lens");
        
        if (lensField != null)
        {
            var lensValue = lensField.GetValue(cinemachineComponent);
            var lensType = lensValue.GetType();
            var orthographicSizeField = lensType.GetField("OrthographicSize");
            
            if (orthographicSizeField != null)
            {
                orthographicSizeField.SetValue(lensValue, size);
                lensField.SetValue(cinemachineComponent, lensValue);
            }
        }
    }
    
    public void SetPriority(int priority)
    {
        if (cinemachineComponent == null) 
        {
            Debug.LogError($"Cannot set priority for {gameObject.name} - no Cinemachine component found!");
            return;
        }
        
        var componentType = cinemachineComponent.GetType();
        
        // Try different property names for different Cinemachine versions
        var priorityProperty = componentType.GetProperty("Priority");
        if (priorityProperty == null)
        {
            priorityProperty = componentType.GetProperty("priority");
        }
        if (priorityProperty == null)
        {
            priorityProperty = componentType.GetProperty("m_Priority");
        }
        
        if (priorityProperty != null)
        {
            priorityProperty.SetValue(cinemachineComponent, priority);
            Debug.Log($"✓ Set {gameObject.name} priority to {priority} using {priorityProperty.Name}");
            
            // Verify it was set
            var actualPriority = (int)priorityProperty.GetValue(cinemachineComponent);
            Debug.Log($"✓ Verified {gameObject.name} priority is now {actualPriority}");
        }
        else
        {
            // If priority property doesn't exist, use enabled/disabled approach
            Debug.LogWarning($"No Priority property found on {componentType.Name}. Using enabled/disabled approach.");
            
            var enabledProperty = componentType.GetProperty("enabled");
            if (enabledProperty != null)
            {
                bool shouldBeEnabled = priority > 0;
                enabledProperty.SetValue(cinemachineComponent, shouldBeEnabled);
                Debug.Log($"✓ Set {gameObject.name} enabled to {shouldBeEnabled} (priority {priority})");
            }
            else
            {
                // Last resort: enable/disable the entire GameObject
                gameObject.SetActive(priority > 0);
                Debug.Log($"✓ Set {gameObject.name} active to {priority > 0} (priority {priority})");
            }
        }
    }
    
    public int GetPriority()
    {
        if (cinemachineComponent == null) return 0;
        
        var componentType = cinemachineComponent.GetType();
        
        // Try different property names
        var priorityProperty = componentType.GetProperty("Priority");
        if (priorityProperty == null)
        {
            priorityProperty = componentType.GetProperty("priority");
        }
        if (priorityProperty == null)
        {
            priorityProperty = componentType.GetProperty("m_Priority");
        }
        
        if (priorityProperty != null)
        {
            return (int)priorityProperty.GetValue(cinemachineComponent);
        }
        else
        {
            // Fallback: return based on enabled state
            var enabledProperty = componentType.GetProperty("enabled");
            if (enabledProperty != null)
            {
                return (bool)enabledProperty.GetValue(cinemachineComponent) ? 10 : 0;
            }
            return gameObject.activeInHierarchy ? 10 : 0;
        }
    }

    void SetCameraZPosition()
    {
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, currentPos.y, cameraZPosition);
        // REMOVE: Reduce debug spam
        // Debug.Log($"Set {gameObject.name} Z position to {cameraZPosition}");
    }
    
    // Add this public method so CinemachineManager can use it
    public float GetCameraZPosition()
    {
        return cameraZPosition;
    }
}
