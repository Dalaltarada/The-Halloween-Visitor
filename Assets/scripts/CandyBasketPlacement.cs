
using UnityEngine;

public class CandyBasketPlacement : MonoBehaviour
{
    [Header("Required References")]
    public PickupController pickupController;
    public PickupItem basket;

    [Tooltip("Exact final position and rotation of the basket.")]
    public Transform finalPosition;

    [Tooltip("The existing floating arrow.")]
    public GameObject placementArrow;

    [Header("Placement Settings")]
    [Tooltip("How close the dropped basket must be to the final position.")]
    [Min(0.1f)]
    public float placementRadius = 1f;

    [Tooltip("Use horizontal distance only, ignoring height.")]
    public bool ignoreHeight = true;

    [Header("Task Manager")]
    public DialogueTaskManager taskManager;
    public string taskID = "CandyTask";

    [Header("Trick Or Treat - After Placement")]
    public TrickOrTreatManager trickOrTreatManager;
    public string encounterToStart = "PumpkinKid";

    [Header("Task State")]
    [SerializeField] private bool taskStarted = false;
    [SerializeField] private bool taskCompleted = false;

    private bool wasHoldingBasket = false;

    public bool IsStarted => taskStarted;
    public bool IsCompleted => taskCompleted;

    private void Start()
    {
        if (placementArrow != null)
            placementArrow.SetActive(false);
    }

    private void Update()
    {
        if (!taskStarted || taskCompleted)
            return;

        if (pickupController == null ||
            basket == null ||
            finalPosition == null)
            return;

        bool holdingBasket =
            pickupController.IsHoldingItem(basket);

        // Show arrow while holding basket.
        if (placementArrow != null &&
            placementArrow.activeSelf != holdingBasket)
        {
            placementArrow.SetActive(holdingBasket);
        }

        // Detect when the basket is dropped.
        if (wasHoldingBasket && !holdingBasket)
        {
            if (basket.gameObject.activeInHierarchy &&
                !basket.IsHeld)
            {
                TryPlaceBasket();
            }
        }

        wasHoldingBasket = holdingBasket;
    }

    public void StartTask()
    {
        if (taskStarted || taskCompleted)
            return;

        if (pickupController == null ||
            basket == null ||
            finalPosition == null)
        {
            Debug.LogWarning(
                "[CandyBasketPlacement] Missing references!"
            );
            return;
        }

        taskStarted = true;

        wasHoldingBasket =
            pickupController.IsHoldingItem(basket);

        if (placementArrow != null)
            placementArrow.SetActive(wasHoldingBasket);

        Debug.Log(
            "[CandyBasketPlacement] Placement task started!"
        );
    }

    private void TryPlaceBasket()
    {
        Vector3 basketPosition = basket.transform.position;
        Vector3 targetPosition = finalPosition.position;

        if (ignoreHeight)
        {
            basketPosition.y = 0f;
            targetPosition.y = 0f;
        }

        float distance = Vector3.Distance(
            basketPosition,
            targetPosition
        );

        Debug.Log(
            "[CandyBasketPlacement] Drop distance: " +
            distance.ToString("F2")
        );

        if (distance > placementRadius)
        {
            Debug.Log(
                "[CandyBasketPlacement] Basket is too far " +
                "from the placement position."
            );
            return;
        }

        CompletePlacement();
    }

    private void CompletePlacement()
    {
        if (taskCompleted)
            return;

        taskCompleted = true;

        // Move basket to final position.
        basket.transform.position = finalPosition.position;
        basket.transform.rotation = finalPosition.rotation;

        // Keep basket in place.
        Rigidbody rb = basket.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Prevent picking up the basket again.
        basket.canPickup = false;

        // Hide placement arrow.
        if (placementArrow != null)
            placementArrow.SetActive(false);

        Physics.SyncTransforms();

        Debug.Log(
            "[CandyBasketPlacement] Basket placed successfully!"
        );

        // Complete the overall candy basket task.
        if (taskManager != null)
        {
            taskManager.CompleteTask(taskID);

            Debug.Log(
                "[CandyBasketPlacement] CandyTask completed!"
            );
        }
        else
        {
            Debug.LogWarning(
                "[CandyBasketPlacement] Task Manager missing!"
            );
        }

        // Start Pumpkin Head's encounter.
        if (trickOrTreatManager != null)
        {
            trickOrTreatManager.StartEncounter(
                encounterToStart
            );

            Debug.Log(
                "[CandyBasketPlacement] Triggering visitor: " +
                encounterToStart
            );
        }
        else
        {
            Debug.LogWarning(
                "[CandyBasketPlacement] TrickOrTreatManager missing!"
            );
        }
    }
}
