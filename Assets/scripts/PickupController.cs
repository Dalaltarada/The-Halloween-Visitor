using UnityEngine;

public class PickupController : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform holdPoint;

    [Tooltip("How far in front of the camera the item is held.")]
    public float holdDistance = 1.5f;

    [Tooltip("How far in front of the camera an item is dropped.")]
    public float dropDistance = 1.5f;

    [Header("Held Item")]
    [SerializeField]
    private PickupItem heldItem;

    private Camera playerCamera;

    private void Start()
    {
        playerCamera = Camera.main;

        if (holdPoint == null && playerCamera != null)
        {
            GameObject point = new GameObject("HoldPoint");

            holdPoint = point.transform;
            holdPoint.SetParent(
                playerCamera.transform,
                false
            );

            holdPoint.localPosition =
                new Vector3(0f, -0.2f, holdDistance);

            holdPoint.localRotation = Quaternion.identity;
        }
    }

    public void Pickup(PickupItem item)
    {
        if (heldItem != null || item == null)
            return;

        if (holdPoint == null)
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

    public bool IsHoldingItem()
    {
        return heldItem != null;
    }
}
