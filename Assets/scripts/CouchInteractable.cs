
using UnityEngine;

public class CouchInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    public TVRelaxTask relaxTask;

    public void Interact()
    {
        if (relaxTask == null)
        {
            Debug.LogWarning(
                "[CouchInteractable] TVRelaxTask is not assigned!"
            );
            return;
        }

        if (!relaxTask.TaskStarted)
        {
            Debug.Log(
                "[CouchInteractable] Task 3 has not started yet."
            );
            return;
        }

        if (!relaxTask.TVOn)
        {
            Debug.Log(
                "[CouchInteractable] Turn on the TV first!"
            );
            return;
        }

        if (relaxTask.TaskCompleted)
            return;

        relaxTask.RelaxOnCouch();
    }
}
