using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveForce = 10f;
    public float brakeFactor = 0.95f;
    public float maxLinearSpeed = 10f;

    [Header("Rotation")]
    public float lookSensitivity = 2f;
    public float rollAcceleration = 100f;
    public float rollFriction = 1.5f;
    public float maxAngularSpeed = 5f;
    public Transform cameraTransform;

    private Rigidbody rb;
    private InputSystem_Actions input;

    private Vector2 moveInput;
    private float rollInput;
    private bool isJumping;
    private bool isCrouching;
    private bool isSlowing;

    private float rollSpeed = 0f;
    private Quaternion currentRotation;

    void Awake()
    {
        input = new InputSystem_Actions();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += _ => moveInput = Vector2.zero;

        input.Player.Jump.performed += _ => isJumping = true;
        input.Player.Jump.canceled += _ => isJumping = false;

        input.Player.Crouch.performed += _ => isCrouching = true;
        input.Player.Crouch.canceled += _ => isCrouching = false;

        input.Player.Slow.performed += _ => isSlowing = true;
        input.Player.Slow.canceled += _ => isSlowing = false;

        input.Player.Roll.performed += ctx => rollInput = ctx.ReadValue<float>();
        input.Player.Roll.canceled += _ => rollInput = 0f;
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on Player!");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        Cursor.lockState = CursorLockMode.Locked;

        currentRotation = transform.rotation;
    }

    void Update()
    {
        // Lecture de la souris
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * lookSensitivity;

        // Mouvement relatif à l'orientation actuelle
        Quaternion pitchRot = Quaternion.AngleAxis(-mouseDelta.y, transform.right);
        Quaternion yawRot = Quaternion.AngleAxis(mouseDelta.x, transform.up);

        currentRotation = yawRot * pitchRot * currentRotation;

        // Gestion du roll avec inertie
        rollSpeed -= rollInput * rollAcceleration * Time.deltaTime;
        rollSpeed = Mathf.Lerp(rollSpeed, 0f, rollFriction * Time.deltaTime);

        Quaternion rollRot = Quaternion.AngleAxis(rollSpeed * Time.deltaTime, transform.forward);
        currentRotation = rollRot * currentRotation;

        transform.rotation = currentRotation;
    }

    void FixedUpdate()
    {
        if (isSlowing)
        {
            rb.linearVelocity *= brakeFactor;
            rb.angularVelocity *= brakeFactor;
        }
        else
        {
            Vector3 inputVector = new Vector3(moveInput.x, isJumping ? 1f : (isCrouching ? -1f : 0f), moveInput.y);
            Vector3 moveDir = transform.TransformDirection(inputVector.normalized);
            rb.AddForce(moveDir * moveForce, ForceMode.Acceleration);
        }

        if (rb.linearVelocity.magnitude > maxLinearSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxLinearSpeed;

        if (rb.angularVelocity.magnitude > maxAngularSpeed)
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularSpeed;
    }
}
