using UnityEngine;

public class SteakCookable : MonoBehaviour
{
    public enum SteakState { Idle, Cooking, Burnt }
    public SteakState currentState = SteakState.Idle;

    public float cookTime = 5f;
    public float burnTime = 8f;
    public float alignSpeed = 5f;
    public float attractionForce = 15f;
    public float surfaceOffset = 0.01f;

    private Rigidbody rb;
    private Material steakMaterial;
    private bool isHeld = false;
    private Hotplate currentHotplate = null;
    private float cookTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        steakMaterial = GetComponent<Renderer>().material = new Material(GetComponent<Renderer>().material);
        Debug.Log($"[STEAK] {name} initialized.");
    }

    void Update()
    {
        if (currentHotplate != null && currentState != SteakState.Burnt)
        {
            if (currentState != SteakState.Cooking)
            {
                currentState = SteakState.Cooking;
                cookTimer = 0f;
                Debug.Log($"[STEAK] {name} started cooking.");
            }

            cookTimer += Time.deltaTime;
            UpdateColor();

            if (cookTimer > burnTime)
            {
                currentState = SteakState.Burnt;
                UpdateColor();
                Debug.Log($"[STEAK] {name} is burnt!");
            }
        }
    }



    void FixedUpdate()
    {
        if (currentHotplate != null && !isHeld)
        {
            Vector3 target = currentHotplate.GetSurfacePosition(transform.position);
            Vector3 force = (target - transform.position) * attractionForce;
            rb.AddForce(force, ForceMode.Acceleration);

            Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * alignSpeed));
        }
    }



    public void GrabSteak()
    {
        isHeld = true;
        Debug.Log($"[STEAK] {name} grabbed.");
    }

    public void ReleaseSteak()
    {
        isHeld = false;
        Debug.Log($"[STEAK] {name} released.");
    }

    public void EnterHotplate(Hotplate plate)
    {
        if (currentHotplate == null)
        {
            currentHotplate = plate;
            Debug.Log($"[STEAK] {name} entered hotplate.");
        }
    }

    public void ExitHotplate(Hotplate plate)
    {
        if (currentHotplate == plate)
        {
            currentHotplate = null;
            Debug.Log($"[STEAK] {name} exited hotplate.");
        }
    }

    void UpdateColor()
    {
        if (currentState == SteakState.Burnt)
        {
            steakMaterial.color = Color.black;
            return;
        }

        if (cookTimer < cookTime)
        {
            float t = cookTimer / cookTime;
            steakMaterial.color = Color.Lerp(Color.red, new Color(0.5f, 0.25f, 0.1f), t);
        }
        else if (cookTimer < burnTime)
        {
            float t = (cookTimer - cookTime) / (burnTime - cookTime);
            steakMaterial.color = Color.Lerp(new Color(0.5f, 0.25f, 0.1f), Color.black, t);
        }
    }
}
