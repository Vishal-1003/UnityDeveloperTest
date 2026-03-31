using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerControl : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8.0f;
    public float gravityMultiplier = 20.0f;
    public float groundStickForce = 5.0f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 3.0f;

    [Header("Settings")]
    public LayerMask groundLayer;
    public float fallOffThreshold = -50.0f;

    [Header("Animator Params")]
    public string isRunningParam = "isRunning";
    public string isFallingParam = "isFalling";

    // Gravity state
    private Vector3 currentDownVector = -Vector3.up;
    private float currentVerticalVelocity = 0f;
    private bool isGrounded;
    private bool isGravityShifting = false;

    // NEW: Death state
    private bool isDead = false;

    // Cached components
    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider capCollider;

    // Input cache
    private float cachedH;
    private float cachedV;
    private bool cachedIsMoving;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capCollider = GetComponent<CapsuleCollider>();

        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        CheckFallOffMap();

        if (isDead) return;

        HandleMouseLook();

        cachedH = 0f;
        cachedV = 0f;
        if (Input.GetKey(KeyCode.W)) cachedV += 1f;
        if (Input.GetKey(KeyCode.S)) cachedV -= 1f;
        if (Input.GetKey(KeyCode.A)) cachedH -= 1f;
        if (Input.GetKey(KeyCode.D)) cachedH += 1f;

        cachedIsMoving = (cachedH != 0f || cachedV != 0f) && !isGravityShifting;

        if (animator != null)
        {
            animator.SetBool(isRunningParam, cachedIsMoving);
            animator.SetBool(isFallingParam, !isGrounded || isGravityShifting);
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        CheckGrounded();
        if (!isGravityShifting)
            MovePlayer();
    }

    private void HandleMouseLook()
    {
        if (isGravityShifting) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0, mouseX, 0, Space.Self);
    }

    private void MovePlayer()
    {
        Vector3 moveDirection = transform.right * cachedH + transform.forward * cachedV;

        Vector3 moveVelocity = moveDirection.sqrMagnitude > 0.01f
            ? moveDirection.normalized * moveSpeed
            : Vector3.zero;

        if (isGrounded)
        {
            currentVerticalVelocity = 0f;
            moveVelocity += currentDownVector * groundStickForce;
        }
        else
        {
            currentVerticalVelocity += gravityMultiplier * Time.fixedDeltaTime;
            moveVelocity += currentDownVector * currentVerticalVelocity;
        }

        rb.velocity = moveVelocity;
    }

    private void CheckGrounded()
    {
        Vector3 origin = transform.position + (transform.up * capCollider.radius);
        isGrounded = Physics.SphereCast(
            origin, capCollider.radius * 0.9f, currentDownVector, out RaycastHit _, 0.2f, groundLayer
        );
    }

    private void CheckFallOffMap()
    {
        if (transform.position.y < fallOffThreshold && GameManager.instance != null)
            GameManager.instance.PlayerFellOffMap();
    }

    public void InitiateGravityShift(Vector3 newDownVector, Vector3 newPosition)
    {
        if (isGravityShifting || isDead) return;
        isGravityShifting = true;
        currentDownVector = newDownVector;
        currentVerticalVelocity = 0f;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        StartCoroutine(GravityShiftRoutine(newPosition));
    }

    private IEnumerator GravityShiftRoutine(Vector3 targetPosition)
    {
        float t = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetUp = -currentDownVector;
        Vector3 targetForward = Vector3.ProjectOnPlane(transform.forward, targetUp).normalized;
        if (targetForward == Vector3.zero)
            targetForward = transform.up;

        Quaternion targetRotation = Quaternion.LookRotation(targetForward, targetUp);

        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        rb.isKinematic = false;
        isGravityShifting = false;

        if (animator != null)
            animator.SetBool(isRunningParam, cachedH != 0f || cachedV != 0f);
    }

    public void InitiateFailedShift(Vector3 leapDirection)
    {
        if (isGravityShifting || isDead) return;
        isDead = true;

        // Turn on standard Unity gravity so they naturally fall downwards
        rb.useGravity = true;

        // Calculate a "jump" vector relative to the wall they are standing on
        Vector3 jumpUp = -currentDownVector;

        // Launch them forward into the abyss, with a slight hop upwards
        rb.velocity = leapDirection * 12f + jumpUp * 5f;

        // Force the falling animation to play
        if (animator != null)
        {
            animator.SetBool(isRunningParam, false);
            animator.SetBool(isFallingParam, true);
        }

        currentDownVector = -Vector3.up;

        // Start the rotation and countdown timer
        StartCoroutine(FailedShiftRoutine());
    }

    private IEnumerator FailedShiftRoutine()
    {
        float t = 0f;
        Quaternion startRotation = transform.rotation;

        // Calculate what "Upright" looks like in the real world, keeping the player facing forward
        Vector3 worldForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (worldForward == Vector3.zero) worldForward = Vector3.forward;

        Quaternion targetRotation = Quaternion.LookRotation(worldForward, Vector3.up);

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        // Wait the remaining 1.5 seconds while plummeting straight down
        yield return new WaitForSeconds(1.5f);

        // Tell the UI it is Game Over
        if (GameManager.instance != null)
        {
            GameManager.instance.PlayerFellOffMap();
        }
    }
}