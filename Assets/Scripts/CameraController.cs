using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform playerTrg;
    public float smoothSpeed = 0.1f;
    public Vector3 offSet;

    [SerializeField] float minPosX, maxPosX;
    [SerializeField] float minPosY, maxPosY;

    private void FixedUpdate()
    {
       Vector3 desiredPos = playerTrg.position + offSet;
        Vector3 smoothPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
        transform.position = smoothPos;

        //Clamp Camera to stage boundary
        transform.position = new Vector3(Mathf.Clamp(smoothPos.x, minPosX, maxPosX), Mathf.Clamp(smoothPos.y, minPosY, maxPosY), transform.position.z);
    }
}
