
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

    private Rigidbody rb;
    private Collider[] itemColliders;

    private bool isHeld = false;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private RigidbodyConstraints originalConstraints;

    private int originalLayer;
    private Transform originalParent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        itemColliders = GetComponentsInChildren<Collider>();

        originalUseGravity = rb.useGravity;
        originalIsKinematic = rb.isKinematic;
        originalConstraints = rb.constraints;

        originalLayer = gameObject.layer;
        originalParent = transform.parent;
    }

    public void Interact()
    {
        if (!canPickup || isHeld)
            return;

        PickupController controller =
            FindObjectOfType<PickupController>();

        if (controller != null)
            controller.Pickup(this);
    }

    public void OnPickup(Transform holdPoint)
    {
        isHeld = true;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.useGravity = false;
        rb.isKinematic = true;

        // Disable collisions while carried
        foreach (Collider col in itemColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        transform.SetParent(holdPoint, true);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        PlaySound(pickupSound);
    }

    public void OnDrop(Vector3 dropPosition)
    {
        transform.SetParent(originalParent, true);
        transform.position = dropPosition;

        rb.isKinematic = originalIsKinematic;
        rb.useGravity = originalUseGravity;
        rb.constraints = originalConstraints;

        foreach (Collider col in itemColliders)
        {
            if (col != null)
                col.enabled = true;
        }

        isHeld = false;

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

    public bool IsHeld => isHeld;
}
