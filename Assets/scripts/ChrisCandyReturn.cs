using UnityEngine;
using Fungus;

// Attach to Chris_InsideCandy (with a collider for the player's interaction raycast).
// Starts the candy dialogue without turning Chris, moving the camera, or locking movement.
public class ChrisCandyReturn : MonoBehaviour, IInteractable
{
    [Header("Requirements")]
    public GrannyJumpscareEvent grannyEvent;
    public bool requireGrannyDialogueFinished = true;
    public Flowchart dialogueFlowchart;
    public string dialogueBlock = "Start";

    private bool started;
    private bool finished;

    public void Interact()
    {
        if (started || finished)
            return;

        if (grannyEvent == null || !grannyEvent.CandyReceived ||
            (requireGrannyDialogueFinished && !grannyEvent.JumpscareFinished))
        {
            Debug.Log("[ChrisCandyReturn] Finish Granny's conversation and get the candy first.");
            return;
        }

        if (dialogueFlowchart == null || dialogueFlowchart.FindBlock(dialogueBlock) == null)
        {
            Debug.LogWarning("[ChrisCandyReturn] Assign a valid Fungus Flowchart and Block.");
            return;
        }

        started = true;
        dialogueFlowchart.ExecuteBlock(dialogueBlock);
    }

    // Fungus Invoke Method immediately after the player says "Here you go.."
    public void GiveCandy()
    {
        if (!started || finished || grannyEvent == null)
            return;

        grannyEvent.GiveCandyToChris();
    }

    // Fungus Invoke Method after Chris says "Ok.."
    public void FinishConversation()
    {
        finished = true;
    }
}
