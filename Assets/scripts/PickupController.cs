
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

        holdPoint.localPosition = new Vector3(
            holdPositionOffset.x,
            holdPositionOffset.y,
            holdDistance + holdPositionOffset.z
        );
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
            Vector3 flatForward =
                playerCamera.transform.forward;

            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = transform.forward;

            flatForward.Normalize();

            // Drop in front of the camera rather
            // than relative to the player's feet.
            dropPosition =
                playerCamera.transform.position +
                flatForward * dropDistance +
                Vector3.down * dropHeightOffset;
        }

        itemToDrop.OnDrop(dropPosition);

        Debug.Log(
            "[PickupController] Dropped: " +
            itemToDrop.gameObject.name
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

        itemToConsume.gameObject.SetActive(false);
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
