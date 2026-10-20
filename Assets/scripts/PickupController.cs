
using UnityEngine;

public class PickupController : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform holdPoint;

    public float holdDistance = 1f;
    public float dropDistance = 1.5f;

    [Header("Hold Position")]
    public Vector3 holdPositionOffset =
        new Vector3(0.5f, -0.3f, 0f);

    [Header("Drop Settings")]
    public float dropHeightOffset = 0.5f;

    [Header("Held Item")]
    [SerializeField] private PickupItem heldItem;

    private Camera playerCamera;

    public PickupItem HeldItem => heldItem;

    private void Start()
    {
        playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError(
                "PickupController: Main Camera not found!"
            );
            return;
        }

        if (holdPoint == null)
        {
            GameObject point =
                new GameObject("HoldPoint");

            holdPoint = point.transform;

            holdPoint.SetParent(
                playerCamera.transform,
                false
            );

            holdPoint.localRotation =
                Quaternion.identity;
        }

        UpdateHoldPosition();
    }

    private void UpdateHoldPosition()
    {
        if (holdPoint == null)
            return;

        // Shared default hold position.
        Vector3 position = new Vector3(
            holdPositionOffset.x,
            holdPositionOffset.y,
            holdDistance + holdPositionOffset.z
        );

        // Add the held item's individual offset.
        if (heldItem != null)
        {
            position += heldItem.holdPositionOffset;
        }

        holdPoint.localPosition = position;
    }

    public void Pickup(PickupItem item)
    {
        if (heldItem != null || item == null)
            return;

        if (holdPoint == null)
        {
            Debug.LogWarning(
                "PickupController: HoldPoint missing!"
            );
            return;
        }

        if (!item.canPickup || item.IsHeld)
            return;

        heldItem = item;

        // Apply the item's own holding position.
        UpdateHoldPosition();

        // Existing pickup behavior.
        heldItem.OnPickup(holdPoint);

        Debug.Log(
            "[PickupController] Picked up: " +
            item.gameObject.name
        );
    }

    public void Drop()
    {
        if (heldItem == null)
            return;

        PickupItem itemToDrop = heldItem;
        heldItem = null;

        Vector3 dropPosition =
            itemToDrop.transform.position;

        if (playerCamera != null)
        {
            // Horizontal forward direction.
            Vector3 flatForward =
                playerCamera.transform.forward;

            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = transform.forward;

            flatForward.Normalize();

            // Default drop position.
            dropPosition =
                playerCamera.transform.position +
                flatForward * dropDistance +
                Vector3.down * dropHeightOffset;

            // Individual drop offset.
            Vector3 itemOffset =
                itemToDrop.dropPositionOffset;

            // X: Move left or right.
            dropPosition +=
                playerCamera.transform.right *
                itemOffset.x;

            // Y: Move up or down.
            dropPosition +=
                Vector3.up * itemOffset.y;

            // Z: Move forward or backward.
            dropPosition +=
                flatForward * itemOffset.z;
        }

        // Restore the item's normal world state.
        itemToDrop.OnDrop(dropPosition);

        // Reset hold point for the next item.
        UpdateHoldPosition();

        Debug.Log(
            "[PickupController] Dropped: " +
            itemToDrop.gameObject.name +
            " | Drop Position: " +
            dropPosition
        );
    }

    public bool IsHoldingItem()
    {
        return heldItem != null;
    }

    public bool IsHoldingItem(PickupItem item)
    {
        return heldItem != null && heldItem == item;
    }


    public void ConsumeHeldItem()
    {
        if (heldItem == null)
            return;

        PickupItem itemToConsume = heldItem;
        heldItem = null;

        // Reset the item's held state, parent and physics.
        // OnDrop sets isHeld back to false.
        itemToConsume.OnDrop(itemToConsume.transform.position);

        // Hide the consumed item until it is needed again.
        itemToConsume.gameObject.SetActive(false);

        // Reset the hold point for the next item.
        UpdateHoldPosition();

        Debug.Log(
            "[PickupController] Item consumed and reset: " +
            itemToConsume.gameObject.name
        );
    }


    private void OnValidate()
    {
        holdDistance = Mathf.Max(0.1f, holdDistance);
        dropDistance = Mathf.Max(0.1f, dropDistance);
        dropHeightOffset = Mathf.Max(0f, dropHeightOffset);

        if (Application.isPlaying && holdPoint != null)
            UpdateHoldPosition();
    }
}
