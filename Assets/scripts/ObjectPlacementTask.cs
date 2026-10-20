using UnityEngine;

public class ObjectPlacementTask : MonoBehaviour
{
    [Header("Task Settings")]
    public string taskName = "Place Pizza";
    [Tooltip("For manual testing only. Keep OFF for Task 3.")]
    public bool useKeyboardTrigger = false;
    public KeyCode triggerKey = KeyCode.Alpha1;

    [Header("Required References")]
    public PickupController pickupController;
    public PickupItem requiredItem;
    public ObjectPlacementZone placementZone;

    [Header("Arrow")]
    public GameObject placementArrow;

    [Header("Pickup Rules")]
    [Tooltip("Prevent picking up the required item before the task begins.")]
    public bool lockPickupUntilTaskStarts = true;

    [Header("Task State")]
    [SerializeField] private bool taskStarted;
    [SerializeField] private bool taskCompleted;

    public bool IsStarted => taskStarted;
    public bool IsCompleted => taskCompleted;

    private void Awake()
    {
        if (lockPickupUntilTaskStarts && requiredItem != null && !taskStarted)
            requiredItem.canPickup = false;
    }

    private void Start()
    {
        if (placementArrow != null) placementArrow.SetActive(false);
        if (placementZone != null) placementZone.SetTask(this);
    }

    private void Update()
    {
        if (useKeyboardTrigger && Input.GetKeyDown(triggerKey)) StartTask();
        if (!taskStarted || taskCompleted) return;
        if (pickupController == null || requiredItem == null) return;

        bool holding = pickupController.IsHoldingItem(requiredItem);
        if (placementArrow != null && placementArrow.activeSelf != holding)
            placementArrow.SetActive(holding);
    }

    public void StartTask()
    {
        if (taskStarted || taskCompleted) return;
        if (requiredItem == null || placementZone == null)
        {
            Debug.LogWarning("[ObjectPlacementTask] Missing pizza or placement zone!");
            return;
        }

        taskStarted = true;
        requiredItem.canPickup = true;
        Debug.Log("[ObjectPlacementTask] Started: " + taskName);
    }

    public void CompleteTask()
    {
        if (!taskStarted || taskCompleted) return;
        taskCompleted = true;
        if (placementArrow != null) placementArrow.SetActive(false);
        if (requiredItem != null && placementZone != null && placementZone.lockItemAfterPlacement)
            requiredItem.canPickup = false;
        Debug.Log("[ObjectPlacementTask] Completed: " + taskName);
    }
}
