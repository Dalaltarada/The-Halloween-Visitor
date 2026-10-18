
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
    public CandyBasketPlacement basketPlacement;

    [Header("Basket Pickup")]
    public PickupItem candyBasket;

    [Header("Trick Or Treat")]
    [Tooltip("Handles collecting candy for visitors.")]
    public TrickOrTreatBasket trickOrTreatBasket;

    [Header("Task State")]
    [SerializeField] private bool taskCompleted = false;

    private void Start()
    {
        if (taskCompleted)
            return;

        if (candies != null)
        {
            foreach (GameObject candy in candies)
            {
                if (candy != null)
                    candy.SetActive(false);
            }
        }

        if (candyBasket != null)
            candyBasket.canPickup = false;
    }

    public void Interact()
    {
        // STAGE 1:
        // Candy minigame has not been completed.
        if (!taskCompleted)
        {
            StartCandyMinigame();
            return;
        }

        // STAGE 2:
        // Minigame completed, but basket
        // has not been placed on the table.
        if (basketPlacement == null ||
            !basketPlacement.IsCompleted)
        {
            PickupBasket();
            return;
        }

        // STAGE 3:
        // Basket has been placed.
        // Use it to collect candy for NPCs.
        if (trickOrTreatBasket != null)
        {
            trickOrTreatBasket.Interact();
        }
        else
        {
            Debug.LogWarning(
                "[CandyBasketTask] TrickOrTreatBasket missing!"
            );
        }
    }

    private void StartCandyMinigame()
    {
        if (minigame == null ||
            pickupController == null ||
            candyBag == null)
            return;

        if (!pickupController.IsHoldingItem(candyBag))
        {
            Debug.Log(
                "[CandyBasketTask] Pick up the candy bag first!"
            );
            return;
        }

        minigame.Begin(this);
    }

    private void PickupBasket()
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
    }

    public void CompleteTask()
    {
        if (taskCompleted)
            return;

        taskCompleted = true;

        // Reveal candies inside the basket.
        if (candies != null)
        {
            foreach (GameObject candy in candies)
            {
                if (candy != null)
                    candy.SetActive(true);
            }
        }

        // Consume the candy bag.
        if (pickupController != null &&
            pickupController.IsHoldingItem(candyBag))
        {
            pickupController.ConsumeHeldItem();
        }

        // Allow basket pickup.
        if (candyBasket != null)
            candyBasket.canPickup = true;

        // Start basket placement.
        if (basketPlacement != null)
        {
            basketPlacement.StartTask();
        }
        else
        {
            Debug.LogWarning(
                "[CandyBasketTask] Basket placement missing!"
            );
        }

        Debug.Log(
            "[CandyBasketTask] Candy minigame completed!"
        );
    }
}
