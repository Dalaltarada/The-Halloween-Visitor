
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    public bool canPickup = true;

    [Header("Sound Effects")]
    public AudioClip pickupSound;
    public AudioClip dropSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Hold Settings")]

    [Tooltip("Individual position offset while held.")]
    public Vector3 holdPositionOffset = Vector3.zero;

    [Tooltip("Individual rotation offset while held.")]
    public Vector3 holdRotationOffset = Vector3.zero;

    [Min(0.01f)]
    public float holdScaleMultiplier = 1f;

    [Header("Drop Settings")]

    [Tooltip(
        "Individual drop offset relative to the player. " +
        "X = right/left, Y = up/down, Z = forward/back."
    )]
    public Vector3 dropPositionOffset = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Rigidbody rb;
    private Collider[] itemColliders;
    private bool[] originalColliderStates;

    private Transform originalParent;
    private Vector3 originalLocalScale;
    private Quaternion originalWorldRotation;

    private bool originalUseGravity;
    private bool originalIsKinematic;
    private RigidbodyConstraints originalConstraints;

    private bool isHeld;

    public bool IsHeld => isHeld;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        itemColliders =
            GetComponentsInChildren<Collider>(true);

        originalColliderStates =
            new bool[itemColliders.Length];

        for (int i = 0; i < itemColliders.Length; i++)
        {
            originalColliderStates[i] =
                itemColliders[i].enabled;
        }

        originalParent = transform.parent;
        originalLocalScale = transform.localScale;
        originalWorldRotation = transform.rotation;

        originalUseGravity = rb.useGravity;
        originalIsKinematic = rb.isKinematic;
        originalConstraints = rb.constraints;
    }

    public void Interact()
    {
        if (showDebugLogs)
        {
            Debug.Log(
                "[PickupItem] Interact: " + gameObject.name +
                " | canPickup=" + canPickup +
                " | isHeld=" + isHeld
            );
        }

        if (!canPickup || isHeld)
            return;

        PickupController controller =
            FindObjectOfType<PickupController>();

        if (controller == null)
        {
            Debug.LogWarning(
                "[PickupItem] PickupController not found!"
            );
            return;
        }

        controller.Pickup(this);

        if (showDebugLogs)
        {
            if (controller.HeldItem == this && isHeld)
            {
                Debug.Log(
                    "[PickupItem] Pickup successful: " +
                    gameObject.name
                );
            }
            else
            {
                Debug.LogWarning(
                    "[PickupItem] Pickup request was not completed."
                );
            }
        }
    }

    public void OnPickup(Transform holdPoint)
    {
        if (isHeld || holdPoint == null)
            return;

        isHeld = true;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider col in itemColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        // Preserve current world size.
        transform.SetParent(holdPoint, true);

        // The PickupController applies the
        // individual hold position offset.
        transform.localPosition = Vector3.zero;

        transform.localRotation =
            Quaternion.Euler(holdRotationOffset);

        transform.localScale *= holdScaleMultiplier;

        Physics.SyncTransforms();

        if (showDebugLogs)
        {
            Debug.Log(
                "[PickupItem] Picked up: " +
                gameObject.name +
                " | World Scale: " +
                transform.lossyScale +
                " | Hold Position Offset: " +
                holdPositionOffset
            );
        }

        PlaySound(pickupSound);
    }

    public void OnDrop(Vector3 dropPosition)
    {
        if (!isHeld)
            return;

        // Detach from the camera.
        transform.SetParent(originalParent, true);

        // Restore original transform.
        transform.localScale = originalLocalScale;
        transform.position = dropPosition;
        transform.rotation = originalWorldRotation;

        // Restore original physics settings.
        rb.isKinematic = originalIsKinematic;
        rb.useGravity = originalUseGravity;
        rb.constraints = originalConstraints;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Restore original collider states.
        for (int i = 0; i < itemColliders.Length; i++)
        {
            if (itemColliders[i] != null)
            {
                itemColliders[i].enabled =
                    originalColliderStates[i];
            }
        }

        // Ensure the required root collider is enabled.
        Collider mainCollider = GetComponent<Collider>();

        if (mainCollider != null)
        {
            mainCollider.enabled = true;
        }

        isHeld = false;

        // Synchronize physics after dropping.
        Physics.SyncTransforms();

        if (showDebugLogs)
        {
            Debug.Log(
                "[PickupItem] Dropped: " +
                gameObject.name +
                " | Position: " +
                transform.position +
                " | Main Collider Enabled: " +
                (mainCollider != null &&
                 mainCollider.enabled) +
                " | Layer: " +
                LayerMask.LayerToName(gameObject.layer)
            );
        }

        PlaySound(dropSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(
                clip,
                transform.position,
                soundVolume
            );
        }
    }
}
