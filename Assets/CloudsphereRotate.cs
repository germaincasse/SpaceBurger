using UnityEngine;

[DisallowMultipleComponent]
public class CloudsphereRotator : MonoBehaviour
{
 

    [Header("Rotation speed (degrees/second)")]
    [Tooltip("Rotation around the X axis (pitch) in degrees per second.")]
    public float speedX = 0f;

    [Tooltip("Rotation around the Y axis (yaw) in degrees per second.")]
    public float speedY = 10f;

    [Tooltip("Rotation around the Z axis (roll) in degrees per second.")]
    public float speedZ = 0f;


    private void Update()
    {

        // Rotate around each local axis independently.
        Vector3 delta = new Vector3(speedX, speedY, speedZ) * Time.deltaTime;
        transform.Rotate(delta, Space.Self);
    }
}
