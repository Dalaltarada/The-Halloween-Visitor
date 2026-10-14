
using UnityEngine;

public class PickupController : MonoBehaviour
{
    [Header("Pickup Settings")]

    [Tooltip("The point where picked-up items are held.")]
    public Transform holdPoint;

    [Tooltip("How far in front of the camera the item is held.")]
    public float holdDistance = 1.0f;

    [Tooltip("How far in front of the camera an item is dropped.")]
    public float dropDistance = 1.5f;

    [Header("Hold Position")]

    [Tooltip("Adjust where the item appears on screen.")]
    public Vector3 holdPositionOffset =
        new Vector3(0.5f, -0.3f, 0f);

    [Header("Held Item")]

    [SerializeField]
    private PickupItem heldItem;

    private Camera playerCamera;

    // Allows other scripts to check which item is held.
    // Used by CandyBasketTask.
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

        // Automatically create HoldPoint if not assigned.
        if (holdPoint == null)
        {
            GameObject point = new GameObject("HoldPoint");

            holdPoint = point.transform;

            holdPoint.SetParent(
                playerCamera.transform,
                false
            );

            holdPoint.localRotation = Quaternion.identity;
        }

        UpdateHoldPosition();
    }

    private void UpdateHoldPosition()
    {
        if (holdPoint == null)
            return;

        holdPoint.localPosition = new Vector3(
            holdPositionOffset.x,
            holdPositionOffset.y,
            holdDistance + holdPositionOffset.z
        );
    }

    public void Pickup(PickupItem item)
    {
        // Prevent picking up another item while holding one.
        if (heldItem != null || item == null)
            return;

        if (holdPoint == null)
        {
            Debug.LogWarning(
                "PickupController: HoldPoint is missing!"
            );
            return;
        }

        if (!item.canPickup || item.IsHeld)
            return;

        heldItem = item;

        heldItem.OnPickup(holdPoint);
    }

    public void Drop()
    {
        if (heldItem == null)
            return;

        Vector3 dropPosition = heldItem.transform.position;

        if (playerCamera != null)
        {
            dropPosition =
                playerCamera.transform.position +
                playerCamera.transform.forward * dropDistance;
        }

        heldItem.OnDrop(dropPosition);

        heldItem = null;
    }

    // Used by FirstPersonController.
    public bool IsHoldingItem()
    {
        return heldItem != null;
    }

    // Used by tasks to check for a specific item.
    public bool IsHoldingItem(PickupItem item)
    {
        return heldItem != null && heldItem == item;
    }

    // Used when a task consumes an item,
    // such as pouring candy into the basket.
    public void ConsumeHeldItem()
    {
        if (heldItem == null)
            return;

        PickupItem itemToConsume = heldItem;

        heldItem = null;

        // Remove the item without dropping it.
        itemToConsume.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        holdDistance = Mathf.Max(0.1f, holdDistance);
        dropDistance = Mathf.Max(0.1f, dropDistance);

        // Allow position adjustments during Play Mode.
        if (Application.isPlaying && holdPoint != null)
        {
            UpdateHoldPosition();
        }
    }
}
