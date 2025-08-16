using UnityEngine;

public static class BorderMarkerUtils
{
    public static Bounds GetBorderBounds()
    {
        GameObject[] borderMarkers = GameObject.FindGameObjectsWithTag("BorderMarker");
        
        if (borderMarkers.Length == 0)
        {
            Debug.LogWarning("No BorderMarkers found!");
            return new Bounds(Vector3.zero, Vector3.one * 10f); // Default bounds
        }
        
        Vector3 min = borderMarkers[0].transform.position;
        Vector3 max = borderMarkers[0].transform.position;
        
        foreach (GameObject marker in borderMarkers)
        {
            Vector3 pos = marker.transform.position;
            if (pos.x < min.x) min.x = pos.x;
            if (pos.x > max.x) max.x = pos.x;
            if (pos.y < min.y) min.y = pos.y;
            if (pos.y > max.y) max.y = pos.y;
        }
        
        Vector3 center = (min + max) / 2f;
        Vector3 size = max - min;
        
        return new Bounds(center, size);
    }
    
    public static Vector3 GetTopBorderPosition()
    {
        Bounds bounds = GetBorderBounds();
        return new Vector3(bounds.center.x, bounds.max.y, 0);
    }
}
