using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class Grabber : MonoBehaviour
{
    [Header("Grab Settings")]
    public float grabForce = 500f;
    public float angularFriction = 0.95f;
    public float maxAngularSpeedGrabbed = 5f;
    public float raycastMaxDistance = 6f;
    public float grabDrag = 5f;
    public float grabAngularDrag = 5f;
    public Transform grabberTarget;
    public LayerMask grabbableMask;

    [Header("Distance Settings")]
    public float[] grabDistances = { 1f, 1.5f, 2f };

    private float currentGrabDistance;
    private Rigidbody heldBody = null;
    private float originalDrag;
    private float originalAngularDrag;
    private InputSystem_Actions input;

    void Awake()
    {
        input = new InputSystem_Actions();

        input.Player.Attack.performed += _ =>
        {
            if (heldBody == null)
                TryGrab();
        };

        input.Player.Attack.canceled += _ =>
        {
            if (heldBody != null)
                Release();
        };

        input.Player.Scroll.performed += ctx =>
        {
            float scroll = -ctx.ReadValue<float>();
            if (heldBody == null) return;

            if (scroll < 0f)
            {
                float next = grabDistances.FirstOrDefault(d => d > currentGrabDistance);
                if (next > 0f) currentGrabDistance = next;
            }
            else if (scroll > 0f)
            {
                float prev = grabDistances.LastOrDefault(d => d < currentGrabDistance);
                if (prev > 0f) currentGrabDistance = prev;
            }

            grabberTarget.localPosition = new Vector3(0f, 0f, currentGrabDistance);
        };
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void FixedUpdate()
    {
        if (heldBody != null)
        {
            if (heldBody.isKinematic) return;

            Vector3 toTarget = grabberTarget.position - heldBody.worldCenterOfMass;
            heldBody.AddForce(toTarget * grabForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            heldBody.angularVelocity *= angularFriction;

            if (heldBody.angularVelocity.magnitude > maxAngularSpeedGrabbed)
                heldBody.angularVelocity = heldBody.angularVelocity.normalized * maxAngularSpeedGrabbed;
        }
    }

    void TryGrab()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, grabbableMask))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb != null)
            {
                heldBody = rb;
                Ingredient ing = rb.GetComponent<Ingredient>();
                if (ing != null)
                    Debug.Log($"[Grabber] Grabbed {rb.gameObject.name} ({ing.connectedIngredients.Count} connections)");
                else
                    Debug.Log($"[Grabber] Grabbed {rb.gameObject.name} (no ingredient data)");


                originalDrag = rb.linearDamping;
                originalAngularDrag = rb.angularDamping;

                rb.linearDamping = grabDrag;
                rb.angularDamping = grabAngularDrag;
                rb.isKinematic = false;
                rb.useGravity = false;

                float dist = Vector3.Distance(Camera.main.transform.position, rb.worldCenterOfMass);
                currentGrabDistance = dist;
                grabberTarget.localPosition = new Vector3(0f, 0f, currentGrabDistance);

                Debug.Log($"[Grabber] Grabbed {rb.gameObject.name} at {currentGrabDistance:F2}m");
            }
        }
        else
        {
            Debug.Log("[Grabber] No object to grab");
        }
    }

    void Release()
    {
        if (heldBody == null) return;

        heldBody.linearDamping = originalDrag;
        heldBody.angularDamping = originalAngularDrag;
        heldBody.isKinematic = false;

        Debug.Log($"[Grabber] Released {heldBody.gameObject.name}");
        heldBody = null;
    }

    void Update()
    {
        if (heldBody == null) return;

        // Interaction F → empiler
        if (input.Player.Interact.WasPressedThisFrame())
        {
            Ingredient heldIng = heldBody.GetComponent<Ingredient>();
            if (heldIng != null)
            {
                Ingredient closest = heldIng.FindClosestIngredient();
                if (closest != null)
                    heldIng.StackWith(closest);
            }
        }
    }
}
