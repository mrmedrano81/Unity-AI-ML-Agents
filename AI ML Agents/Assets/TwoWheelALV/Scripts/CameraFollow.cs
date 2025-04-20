using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform carTransform;
    public Vector3 offset;
    public float smoothSpeed = 0.125f;
    public float rotationSpeed = 5f;

    private CameraManager cameraManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        carTransform = FindFirstObjectByType<CarAgent>().transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, carTransform.position + offset, smoothSpeed);

        float yInput = Input.GetAxis("Vertical");
        float xInput = Input.GetAxis("Horizontal"); 
    }
}
