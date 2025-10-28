using UnityEngine;

public class Hotplate : MonoBehaviour
{
    public float surfaceOffset = 0.01f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Steak"))
        {
            SteakCookable steak = other.GetComponent<SteakCookable>();
            if (steak != null)
            {
                steak.EnterHotplate(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Steak"))
        {
            SteakCookable steak = other.GetComponent<SteakCookable>();
            if (steak != null)
            {
                steak.ExitHotplate(this);
            }
        }
    }

    public Vector3 GetSurfacePosition(Vector3 worldSteakPos)
    {
        Vector3 pos = worldSteakPos;
        pos.y = transform.position.y + surfaceOffset;
        return pos;
    }
}
