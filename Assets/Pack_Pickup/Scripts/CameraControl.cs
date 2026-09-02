using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform target; // The car to be followed
    public float smoothness = 5f; // Smoothness during camera tracking
    public float rotationSpeed = 2f; // Response speed to mouse movement for rotation
    public float distance = 5f; // Distance between the camera and the car
    public float height = 2f; // Height of the camera relative to the car

    private Vector3 offset; // Initial offset

    void Start()
    {
        offset = new Vector3(0f, height, -distance); // Set the initial offset
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            // Determine the target position
            Vector3 targetPosition = target.position;

            // Smoothly move the camera towards the target position
            transform.position = Vector3.Lerp(transform.position, targetPosition + offset, smoothness * Time.deltaTime);

            // Ensure the camera always looks at the target
            transform.LookAt(target.position);

            // Rotate the camera with mouse left-right input
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            offset = Quaternion.Euler(0, mouseX, 0) * offset;
        }
    }
}
