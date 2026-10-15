
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectPlacementZone : MonoBehaviour
{
    [Header("Placement Settings")]
    [Tooltip("Optional point to snap the item into place.")]
    public Transform snapPoint;

    [Tooltip("Snap the item into its final position.")]
    public bool snapItemIntoPlace = true;

    [Tooltip("Prevent picking up the item after placement.")]
    public bool lockItemAfterPlacement = true;

    private ObjectPlacementTask task;
    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        zoneCollider.isTrigger = true;
    }

    public void SetTask(ObjectPlacementTask newTask)
    {
        task = newTask;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPlaceItem(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryPlaceItem(other);
    }

    private void TryPlaceItem(Collider other)
    {
        if (task == null ||
            !task.IsStarted ||
            task.IsCompleted)
            return;

        PickupItem item =
            other.GetComponentInParent<PickupItem>();

        if (item == null ||
            item != task.requiredItem)
            return;

        // The item must have been dropped,
        // not still held by the player.
        if (item.IsHeld)
            return;

        // Position the item exactly where it belongs.
        if (snapItemIntoPlace && snapPoint != null)
        {
            item.transform.position =
                snapPoint.position;

            item.transform.rotation =
                snapPoint.rotation;

            Rigidbody rb =
                item.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        if (lockItemAfterPlacement)
        {
            item.canPickup = false;
        }

        task.CompleteTask();
    }
}
