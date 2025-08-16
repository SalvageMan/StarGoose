using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Removed playerTrg and smoothSpeed since we don't need them for a fixed camera
    // public Transform playerTrg;
    // public float smoothSpeed = 0.1f;
    // public Vector3 offSet;

    [SerializeField] float minPosX, maxPosX;
    [SerializeField] float minPosY, maxPosY;

    // Optional: If you want to set a specific fixed position
    [Header("Fixed Camera Settings")]
    public Vector3 fixedPosition;
    public bool useFixedPosition = false;
    public Vector3 playerOffset = new Vector3(0, 0, -10); // Offset from player (typically negative Z for camera)

    [Header("Camera View Settings")]
    public float cameraSize = 5f; // For orthographic cameras
    public float fieldOfView = 60f; // For perspective cameras
    public bool expandToFitBorderMarkers = true;

    private void Start()
    {
        // Set initial camera size/FOV
        Camera cam = GetComponent<Camera>();
        
        if (expandToFitBorderMarkers)
        {
            ExpandCameraToFitBorderMarkers(cam);
        }
        else
        {
            if (cam.orthographic)
            {
                cam.orthographicSize = cameraSize;
            }
            else
            {
                cam.fieldOfView = fieldOfView;
            }
        }

        // Position camera
        if (useFixedPosition)
        {
            transform.position = new Vector3(fixedPosition.x, fixedPosition.y, transform.position.z);
        }
        else
        {
            PositionCameraOverPlayer();
        }
        
        // Clamp to boundaries if needed
        ClampToBounds();
    }

    // Remove FixedUpdate since we don't need continuous following
    // private void FixedUpdate() { ... }

    private void ClampToBounds()
    {
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(
            Mathf.Clamp(currentPos.x, minPosX, maxPosX), 
            Mathf.Clamp(currentPos.y, minPosY, maxPosY), 
            currentPos.z
        );
    }

    private void ExpandCameraToFitBorderMarkers(Camera cam)
    {
        // Use BorderMarkerUtils instead of duplicating code
        Bounds bounds = BorderMarkerUtils.GetBorderBounds();
        
        if (bounds.size == Vector3.one * 10f) // Default bounds means no markers found
        {
            Debug.LogWarning("No BorderMarkers found! Using default camera size.");
            if (cam.orthographic)
            {
                cam.orthographicSize = cameraSize;
            }
            else
            {
                cam.fieldOfView = fieldOfView;
            }
            return;
        }

        float totalHeight = bounds.size.y;
        
        if (cam.orthographic)
        {
            cam.orthographicSize = totalHeight / 2f;
            Debug.Log($"Camera orthographic size set to {cam.orthographicSize} to fit vertical bounds from {bounds.min.y} to {bounds.max.y}");
        }
        else
        {
            float distance = Mathf.Abs(transform.position.z);
            float fov = 2.0f * Mathf.Atan(totalHeight / (2.0f * distance)) * Mathf.Rad2Deg;
            cam.fieldOfView = fov;
            Debug.Log($"Camera field of view set to {fov} to fit vertical bounds from {bounds.min.y} to {bounds.max.y}");
        }
    }

    private void PositionCameraOverPlayer()
    {
        Vector3 cameraPosition = transform.position;
        
        if (expandToFitBorderMarkers)
        {
            // Use BorderMarkerUtils for positioning
            Bounds bounds = BorderMarkerUtils.GetBorderBounds();
            
            if (bounds.size != Vector3.one * 10f) // Check if we found real markers
            {
                cameraPosition.x = bounds.center.x + playerOffset.x;
                cameraPosition.y = bounds.center.y + playerOffset.y;
                
                Debug.Log($"Camera centered at: X: {cameraPosition.x}, Y: {cameraPosition.y} using border bounds");
            }
            else
            {
                Debug.LogWarning("No BorderMarkers found! Camera will remain at current position.");
            }
        }
        else
        {
            // Fallback: position over player
            PlayerFlightController player = FindFirstObjectByType<PlayerFlightController>();
            if (player != null)
            {
                cameraPosition.x = player.transform.position.x + playerOffset.x;
                cameraPosition.y = player.transform.position.y + playerOffset.y;
                Debug.Log("Camera positioned over player (border markers disabled)");
            }
            else
            {
                Debug.LogWarning("Player not found and border markers disabled! Camera will remain at current position.");
            }
        }
        
        cameraPosition.z = transform.position.z + playerOffset.z;
        transform.position = cameraPosition;
        Debug.Log($"Final camera position: {transform.position}");
    }
}
