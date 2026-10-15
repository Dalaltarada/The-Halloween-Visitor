
using UnityEngine;

public class CandyBasketTask : MonoBehaviour, IInteractable
{
    [Header("Required References")]
    public PickupController pickupController;
    public PickupItem candyBag;
    public CandyTimingMinigame minigame;

    [Header("Candy Objects")]
    public GameObject[] candies;

    [Header("Next Task - Basket Placement")]
    public ObjectPlacementTask basketPlacementTask;

    [Header("Basket Pickup")]
    [Tooltip("The basket that the player picks up after the minigame.")]
    public PickupItem candyBasket;

    [Header("Task State")]
    [SerializeField] private bool taskCompleted = false;

    private void Start()
    {
        if (taskCompleted)
            return;

        // Hide candies until the minigame is completed.
        foreach (GameObject candy in candies)
        {
            if (candy != null)
                candy.SetActive(false);
        }

        // Basket cannot be picked up before the minigame.
        if (candyBasket != null)
        {
            candyBasket.canPickup = false;
        }
    }

    public void Interact()
    {
        // AFTER MINIGAME:
        // Pick up the basket instead of starting the minigame.
        if (taskCompleted)
        {
            if (candyBasket == null)
            {
                Debug.LogWarning(
                    "[CandyBasketTask] Candy Basket reference missing!"
                );
                return;
            }

            if (pickupController == null)
            {
                Debug.LogWarning(
                    "[CandyBasketTask] Pickup Controller missing!"
                );
                return;
            }

            if (!candyBasket.canPickup)
                return;

            if (candyBasket.IsHeld)
                return;

            pickupController.Pickup(candyBasket);

            Debug.Log(
                "[CandyBasketTask] Basket pickup requested!"
            );

            return;
        }

        // BEFORE MINIGAME:
        // Require the candy bag to start the minigame.
        if (minigame == null)
            return;

        if (pickupController == null || candyBag == null)
            return;

        if (!pickupController.IsHoldingItem(candyBag))
        {
            Debug.Log("Pick up the candy bag first!");
            return;
        }

        minigame.Begin(this);
    }

    public void CompleteTask()
    {
        if (taskCompleted)
            return;

        taskCompleted = true;

        // Show candies inside the basket.
        foreach (GameObject candy in candies)
        {
            if (candy != null)
                candy.SetActive(true);
        }

        // Remove the candy bag after the minigame.
        if (pickupController != null &&
            pickupController.IsHoldingItem(candyBag))
        {
            pickupController.ConsumeHeldItem();
        }

        // Unlock the basket.
        if (candyBasket != null)
        {
            candyBasket.canPickup = true;

            Debug.Log(
                "[CandyBasketTask] Basket pickup unlocked!"
            );
        }

        // Start basket placement task.
        if (basketPlacementTask != null)
        {
            basketPlacementTask.StartTask();

            Debug.Log(
                "[CandyBasketTask] Basket placement started!"
            );
        }

        Debug.Log(
            "[CandyBasketTask] Candy minigame completed!"
        );
    }
}
