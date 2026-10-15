
using UnityEngine;

public class CandyBasketTask : MonoBehaviour, IInteractable
{
    [Header("Required References")]
    public PickupController pickupController;
    public PickupItem candyBag;
    public CandyTimingMinigame minigame;

    [Header("Candy Objects")]
    public GameObject[] candies;

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
    }

    public void Interact()
    {
        if (taskCompleted || minigame == null)
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

        foreach (GameObject candy in candies)
        {
            if (candy != null)
                candy.SetActive(true);
        }

        if (pickupController != null &&
            pickupController.IsHoldingItem(candyBag))
        {
            pickupController.ConsumeHeldItem();
        }

        Debug.Log("Candy basket task completed!");
    }
}
