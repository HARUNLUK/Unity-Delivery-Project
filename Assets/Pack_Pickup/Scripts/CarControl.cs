using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    public float enginePower = 2000.0f; // Engine power
    public float turnSpeed = 25.0f; // Maximum turn speed
    public float turnSmoothness = 5.0f; // Turn smoothness
    public Transform[] wheels; // Array of Wheel Collider components
    public Transform[] wheelMeshes; // Array of wheel meshes
    public Transform centerOfMass; // Center of Mass point
    public GameObject steeringWheel;

    private Rigidbody rb; // Car's Rigidbody component
    private float currentTurnAngle = 0.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition; // Set center of mass
    }

    void FixedUpdate()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // Turn input (right/left arrow keys or A/D keys)
        float verticalInput = Input.GetAxis("Vertical"); // Acceleration input (up/down arrow keys or W/S keys)

        // Calculate turn angle
        float targetTurnAngle = horizontalInput * turnSpeed;
        currentTurnAngle = Mathf.Lerp(currentTurnAngle, targetTurnAngle, Time.deltaTime * turnSmoothness);

        steeringWheel.transform.localEulerAngles = new Vector3(-64, 0, currentTurnAngle * 3);



        // Wheels rotation and steering
        for (int i = 0; i < wheels.Length; i++)
        {
            WheelCollider wheelCollider = wheels[i].GetComponent<WheelCollider>();
            if (i < 2) // First two wheels steer
            {
                wheelCollider.steerAngle = currentTurnAngle;
            }
            else
            {
                wheelCollider.steerAngle = 0f; // Other two wheels remain straight
            }

            // Wheels driving (forward/backward movement)
            wheelCollider.motorTorque = verticalInput * enginePower;

            // Rotate wheel meshes
            Quaternion wheelRotation;
            Vector3 wheelPosition;
            wheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);
            wheelMeshes[i].position = wheelPosition;
            wheelMeshes[i].rotation = wheelRotation;
        }
    }
}
