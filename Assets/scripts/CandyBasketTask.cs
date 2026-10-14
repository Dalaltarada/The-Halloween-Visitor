
using UnityEngine;

public class CandyBasketTask : MonoBehaviour, IInteractable
{
    [Header("Required References")]
    public PickupController pickupController;
    public PickupItem candyBag;
    public CandyTimingMinigame minigame;

    [Header("Candy Objects")]
    [Tooltip("Candies hidden inside the basket.")]
    public GameObject[] candies;

    [Header("Task State")]
    [SerializeField] private bool taskCompleted = false;

    private void Start()
    {
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

        if (pickupController.HeldItem != candyBag)
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

        // Release the bag from the pickup system first.
        if (pickupController != null &&
            pickupController.HeldItem == candyBag)
        {
            pickupController.Drop();
        }

        if (candyBag != null)
            candyBag.gameObject.SetActive(false);

        Debug.Log("Candy basket task completed!");
    }
}
