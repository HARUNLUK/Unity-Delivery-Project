using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Water_Settings : MonoBehaviour
{
    private Material waterVolume;
    private Material waterMaterial;

    void Update()
    {
        if (waterVolume == null)
        {
            waterVolume = Resources.Load<Material>("Water_Volume");
        }

        if (waterMaterial == null)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null) waterMaterial = mr.sharedMaterial;
        }

        if (waterVolume != null && waterMaterial != null)
        {
            float displacement = waterMaterial.HasProperty("_Displacement_Amount") ? waterMaterial.GetFloat("_Displacement_Amount") : 0f;
            Vector4 bounds = waterVolume.HasProperty("bounds") ? waterVolume.GetVector("bounds") : Vector4.zero;
            waterVolume.SetVector("pos", new Vector4(0, (bounds.y / -2f) + transform.position.y + (displacement / 3f), 0, 0));
        }
    }
}
