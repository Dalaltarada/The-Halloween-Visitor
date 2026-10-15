
using UnityEngine;

public class ObjectPlacementTask : MonoBehaviour
{
    [Header("Task Settings")]
    public string taskName = "Place Object";

    [Tooltip("Temporary trigger using the 1 key.")]
    public bool useKeyboardTrigger = true;

    public KeyCode triggerKey = KeyCode.Alpha1;

    [Header("Required References")]
    public PickupController pickupController;

    [Tooltip("The item that must be picked up and placed.")]
    public PickupItem requiredItem;

    [Tooltip("The placement zone for this task.")]
    public ObjectPlacementZone placementZone;

    [Header("Arrow")]
    [Tooltip("3D arrow showing where to place the item.")]
    public GameObject placementArrow;

    [Header("Task State")]
    [SerializeField] private bool taskStarted = false;
    [SerializeField] private bool taskCompleted = false;

    public bool IsStarted => taskStarted;
    public bool IsCompleted => taskCompleted;

    private void Start()
    {
        if (placementArrow != null)
            placementArrow.SetActive(false);

        if (placementZone != null)
            placementZone.SetTask(this);
    }

    private void Update()
    {
        // Temporary keyboard trigger.
        if (useKeyboardTrigger &&
            Input.GetKeyDown(triggerKey))
        {
            StartTask();
        }

        if (!taskStarted || taskCompleted)
            return;

        if (pickupController == null ||
            requiredItem == null)
            return;

        // Show arrow only when holding the correct item.
        bool holdingRequiredItem =
            pickupController.IsHoldingItem(requiredItem);

        if (placementArrow != null &&
            placementArrow.activeSelf != holdingRequiredItem)
        {
            placementArrow.SetActive(holdingRequiredItem);
        }
    }

    // Can be called by another script,
    // dialogue, cutscene, or event later.
    public void StartTask()
    {
        if (taskStarted || taskCompleted)
            return;

        if (requiredItem == null ||
            placementZone == null)
        {
            Debug.LogWarning(
                "[ObjectPlacementTask] Missing references!"
            );
            return;
        }

        taskStarted = true;

        Debug.Log(
            "[ObjectPlacementTask] Started: " + taskName
        );
    }

    public void CompleteTask()
    {
        if (!taskStarted || taskCompleted)
            return;

        taskCompleted = true;

        if (placementArrow != null)
            placementArrow.SetActive(false);

        Debug.Log(
            "[ObjectPlacementTask] Completed: " + taskName
        );
    }
}
