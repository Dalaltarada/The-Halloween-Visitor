
using UnityEngine;

public class CandyBasketTask : MonoBehaviour, IInteractable
{
    [Header("Required References")]
    public PickupController pickupController;
    public PickupItem candyBag;
    public CandyTimingMinigame minigame;

    [Header("Candy Objects")]
    public GameObject[] candies;

    [Header("Next Task - New Basket Placement")]
    public CandyBasketPlacement basketPlacement;

    [Header("Basket Pickup")]
    public PickupItem candyBasket;

    [Header("Task State")]
    [SerializeField] private bool taskCompleted = false;

    private void Start()
    {
        if (taskCompleted)
            return;

        foreach (GameObject candy in candies)
        {
            if (candy != null)
                candy.SetActive(false);
        }

        if (candyBasket != null)
            candyBasket.canPickup = false;
    }

    public void Interact()
    {
        // AFTER MINIGAME:
        // Pick up the candy basket.
        if (taskCompleted)
        {
            if (candyBasket == null ||
                pickupController == null)
                return;

            if (!candyBasket.canPickup ||
                candyBasket.IsHeld)
                return;

            pickupController.Pickup(candyBasket);

            Debug.Log(
                "[CandyBasketTask] Basket pickup requested!"
            );

            return;
        }

        // BEFORE MINIGAME:
        // Require the candy bag.
        if (minigame == null ||
            pickupController == null ||
            candyBag == null)
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

        // Reveal candies.
        foreach (GameObject candy in candies)
        {
            if (candy != null)
                candy.SetActive(true);
        }

        // Consume the candy bag.
        if (pickupController != null &&
            pickupController.IsHoldingItem(candyBag))
        {
            pickupController.ConsumeHeldItem();
        }

        // Unlock basket pickup.
        if (candyBasket != null)
        {
            candyBasket.canPickup = true;
        }

        // Start the NEW placement system.
        if (basketPlacement != null)
        {
            basketPlacement.StartTask();
        }
        else
        {
            Debug.LogWarning(
                "[CandyBasketTask] Basket placement reference missing!"
            );
        }

        Debug.Log(
            "[CandyBasketTask] Candy minigame completed!"
        );
    }
}
