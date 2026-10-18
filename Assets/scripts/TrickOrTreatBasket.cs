
using UnityEngine;

public class TrickOrTreatBasket : MonoBehaviour, IInteractable
{
    [Header("Visitor Manager")]
    public TrickOrTreatManager trickOrTreatManager;

    public void Interact()
    {
        if (trickOrTreatManager == null)
            return;

        trickOrTreatManager.InteractWithBasket();
    }
}
