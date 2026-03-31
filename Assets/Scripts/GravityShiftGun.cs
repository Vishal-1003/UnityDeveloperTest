using UnityEngine;

[RequireComponent(typeof(PlayerControl))]
public class GravityShiftGun : MonoBehaviour
{
    [Header("Settings")]
    public float raycastDistance = 20.0f;
    public LayerMask walkableLayer;
    public float raycastHeightOffset = 1.0f;

    private PlayerControl playerControl;

    // We keep track of the key, but now we also LOCK the vectors when pressed
    private KeyCode? queuedKey = null;
    private Vector3 lockedAimDirection;
    private Vector3 lockedPlayerForward;

    [Header("Hologram")]
    public GameObject holoCharacter;

    private void Start()
    {
        if (holoCharacter != null) holoCharacter.SetActive(false);
        playerControl = GetComponent<PlayerControl>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            queuedKey = null;
            if (holoCharacter != null) holoCharacter.SetActive(false);
        }

        if (Input.GetMouseButton(0))
        {
            // Lock the aim direction and the player's forward direction the moment a key is tapped
            if (Input.GetKeyDown(KeyCode.UpArrow)) LockAim(KeyCode.UpArrow, transform.forward);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) LockAim(KeyCode.DownArrow, -transform.up);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) LockAim(KeyCode.LeftArrow, -transform.right);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) LockAim(KeyCode.RightArrow, transform.right);

            if (queuedKey.HasValue)
            {
                UpdateHologram();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (queuedKey.HasValue)
            {
                TryShift();
                queuedKey = null;
            }

            if (holoCharacter != null) holoCharacter.SetActive(false);
        }
    }

    private void LockAim(KeyCode key, Vector3 aimDir)
    {
        queuedKey = key;
        lockedAimDirection = aimDir;
        lockedPlayerForward = transform.forward;
    }

    private void UpdateHologram()
    {
        if (holoCharacter == null) return;

        Vector3 origin = transform.position + (transform.up * raycastHeightOffset);

        if (Physics.Raycast(origin, lockedAimDirection, out RaycastHit hit, raycastDistance, walkableLayer))
        {
            holoCharacter.SetActive(true);
            holoCharacter.transform.position = hit.point + hit.normal * 0.5f;

            Vector3 targetUp = hit.normal;
            Vector3 targetForward = Vector3.ProjectOnPlane(lockedPlayerForward, targetUp).normalized;

            if (targetForward == Vector3.zero)
            {
                targetForward = transform.up;
            }

            holoCharacter.transform.rotation = Quaternion.LookRotation(targetForward, targetUp);
        }
        else
        {
            holoCharacter.SetActive(false);
        }
    }

    private void TryShift()
    {
        Vector3 origin = transform.position + (transform.up * raycastHeightOffset);

        if (Physics.Raycast(origin, lockedAimDirection, out RaycastHit hit, raycastDistance, walkableLayer))
        {
            // SUCCESS: The player hit a wall, jump to it!
            Vector3 safeTargetPosition = hit.point + hit.normal * 0.5f;
            playerControl.InitiateGravityShift(-hit.normal, safeTargetPosition);
        }
        else
        {
            // FAILURE: The player aimed at empty space!
            playerControl.InitiateFailedShift(lockedAimDirection);
        }
    }
}