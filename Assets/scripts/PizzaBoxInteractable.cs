using UnityEngine;

public class PizzaBoxInteractable : MonoBehaviour, IInteractable
{
    public TVRelaxTask relaxTask;

    [Header("Optional Interaction Collider")]
    [Tooltip("Only assign a dedicated interaction collider, not the box's physical collider.")]
    public Collider interactionCollider;

    public void Interact()
    {
        if (relaxTask == null || !relaxTask.CanTakePizzaSlice) return;

        if (relaxTask.TakePizzaSlice())
        {
            // Prevent repeat interactions after taking the only slice.
            if (interactionCollider != null)
                interactionCollider.enabled = false;
            enabled = false;
        }
    }
}