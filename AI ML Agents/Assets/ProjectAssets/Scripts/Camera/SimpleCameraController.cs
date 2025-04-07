using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float verticalSpeed = 5f;

    [Header("Mouse Look Settings")]
    public float lookSensitivity = 2f;
    public bool invertY = false;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        // Initialize rotation
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;

        // Optional: Lock cursor initially
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            yaw += mouseX;
            pitch -= invertY ? -mouseY : mouseY;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        Vector3 direction = Vector3.zero;

        // Horizontal movement
        if (Input.GetKey(KeyCode.W)) direction += transform.forward;
        if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
        if (Input.GetKey(KeyCode.A)) direction -= transform.right;
        if (Input.GetKey(KeyCode.D)) direction += transform.right;

        // Vertical movement
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) direction += Vector3.up;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) direction -= Vector3.up;

        // Normalize to prevent faster diagonal movement
        direction = direction.normalized;

        // Sprinting
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        // Move
        transform.position += direction * speed * Time.deltaTime;
    }
}
