
using System.Collections.Generic;
using UnityEngine;
using Fungus;
using UnityStandardAssets.Characters.FirstPerson;

public class FungusDialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class LockedDialogue
    {
        public Flowchart flowchart;
        public string blockName;
    }

    [Header("Player")]
    public FirstPersonController playerController;

    [Header("Dialogues That Disable Movement")]
    public LockedDialogue[] lockedDialogues;

    private bool dialogueLocked;
    private bool previousMovementState;

    private void Update()
    {
        bool shouldLock = false;

        foreach (LockedDialogue dialogue in lockedDialogues)
        {
            if (dialogue.flowchart == null ||
                string.IsNullOrEmpty(dialogue.blockName))
                continue;

            Block block = dialogue.flowchart.FindBlock(
                dialogue.blockName
            );

            if (block != null && block.IsExecuting())
            {
                shouldLock = true;
                break;
            }
        }

        if (shouldLock && !dialogueLocked)
        {
            LockMovement();
        }
        else if (!shouldLock && dialogueLocked)
        {
            UnlockMovement();
        }
    }

    private void LockMovement()
    {
        if (playerController == null)
            return;

        previousMovementState =
            playerController.disableMovement;

        playerController.disableMovement = true;
        dialogueLocked = true;
    }

    private void UnlockMovement()
    {
        if (playerController == null)
            return;

        playerController.disableMovement =
            previousMovementState;

        dialogueLocked = false;
    }

    private void OnDisable()
    {
        if (dialogueLocked)
            UnlockMovement();
    }
}
