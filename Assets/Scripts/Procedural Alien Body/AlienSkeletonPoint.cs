using UnityEngine;

[ExecuteAlways]
public class AlienSkeletonPoint : MonoBehaviour
{
    public float radiusX = 0.5f;
    public float radiusY = 0.5f;

    [Header("Chaînage")]
    public AlienSkeletonPoint previous;
    public AlienSkeletonPoint next;
    [HideInInspector]
    public Quaternion localOrientation = Quaternion.identity;


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.05f);

        Quaternion orientation = GetOrientation();
        int segments = 32;
        Vector3 prev = transform.position + orientation * new Vector3(radiusX, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 local = new Vector3(
                Mathf.Cos(angle) * radiusX,
                Mathf.Sin(angle) * radiusY,
                0
            );
            Vector3 world = transform.position + orientation * local;
            Gizmos.DrawLine(prev, world);
            prev = world;
        }

        if (next != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, next.transform.position);
        }
    }

    public Quaternion GetOrientation() {
    Vector3 forward = Vector3.forward;
    if (next != null && previous != null) {
        forward = (next.transform.position - previous.transform.position).normalized;
    } else if (next != null) {
        forward = (next.transform.position - transform.position).normalized;
    } else if (previous != null) {
        forward = (transform.position - previous.transform.position).normalized;
    }

    Vector3 up = Vector3.up;
    if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f) up = Vector3.right;

    return Quaternion.LookRotation(forward, up);
}

}
