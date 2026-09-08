using UnityEngine;

// Obsolete pack script - Replaced by modular CarController & DrivableVehicle
public class CarControl : MonoBehaviour
{
    private void Awake()
    {
        // Automatically self-disable if modern DrivableVehicle exists
        if (GetComponent<DrivableVehicle>() != null || GetComponent<CarController>() != null)
        {
            enabled = false;
        }
    }
}
