using UnityEngine;

public class CouchInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    public TVRelaxTask relaxTask;

    [Header("Interaction Collider")]
    [Tooltip("Assign the collider on the couch's interaction spot, NOT the couch's physical collision collider.")]
    public Collider interactionCollider;

    public void Interact()
    {
        if (relaxTask == null)
        {
            Debug.LogWarning(
                "[CouchInteractable] TVRelaxTask is not assigned!"
            );
            return;
        }

        if (relaxTask.IsSeated || relaxTask.TaskCompleted)
            return;

        relaxTask.RelaxOnCouch();

        // Disable interaction only after sitting succeeds.
        if (relaxTask.IsSeated)
        {
            if (interactionCollider != null)
                interactionCollider.enabled = false;

            enabled = false;
        }
    }
}