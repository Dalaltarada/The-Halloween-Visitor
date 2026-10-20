
using UnityEngine;

public class TrickOrTreatNPC : MonoBehaviour, IInteractable
{
    [Header("Trick Or Treat Manager")]
    public TrickOrTreatManager manager;

    [Tooltip("Root NPC GameObject assigned in the manager.")]
    public GameObject npcRoot;

    public void Interact()
    {
        if (manager == null || npcRoot == null)
            return;

        manager.InteractWithNPC(npcRoot);
    }
}
