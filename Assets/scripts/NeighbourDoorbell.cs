
using System.Collections;
using UnityEngine;
using Fungus;

/// <summary>
/// Attach to each neighbour's doorbell.
/// Supports ordered visits between neighbours.
/// </summary>
public class NeighbourDoorbell : MonoBehaviour, IInteractable
{
    [Header("Neighbour Door")]
    public DoorInteractable neighbourDoor;
    public GameObject neighbourNPC;

    [Tooltip("If checked, the NPC is hidden until the bell is rung.")]
    public bool hideNPCBeforeBell = true;

    [Header("Doorbell")]
    public AudioSource bellAudio;
    public AudioClip bellClip;

    [Min(0f)]
    public float delayBeforeOpening = 0.8f;

    [Header("Dialogue")]
    public Flowchart dialogueFlowchart;
    public string dialogueBlock = "Start";

    [Min(0f)]
    public float dialogueDelayAfterOpening = 0f;

    [Header("Visit Settings")]
    public bool allowRepeatVisits = false;
    public bool lockAfterVisit = true;

    [Header("Visit Order")]
    [Tooltip("Assign the previous neighbour's doorbell. Leave empty for the first neighbour.")]
    public NeighbourDoorbell requiredPreviousDoorbell;

    private bool busy;
    private bool visited;
    private bool visitCompleted;

    public bool VisitCompleted => visitCompleted;

    private void Awake()
    {
        if (hideNPCBeforeBell && neighbourNPC != null)
            neighbourNPC.SetActive(false);
    }

    public void Interact()
    {
        // Prevent interaction while busy or already visited.
        if (busy || (visited && !allowRepeatVisits))
            return;

        // Require the previous neighbour's visit to be completed.
        if (requiredPreviousDoorbell != null &&
            !requiredPreviousDoorbell.VisitCompleted)
        {
            Debug.Log(
                "[NeighbourDoorbell] Visit the previous neighbour first."
            );

            return;
        }

        if (neighbourDoor == null ||
            dialogueFlowchart == null ||
            dialogueFlowchart.FindBlock(dialogueBlock) == null)
        {
            Debug.LogWarning(
                "[NeighbourDoorbell] Assign door and valid Fungus block on " + name
            );

            return;
        }

        StartCoroutine(RingSequence());
    }

    private IEnumerator RingSequence()
    {
        busy = true;
        visited = true;

        if (bellAudio != null)
        {
            if (bellClip != null)
                bellAudio.PlayOneShot(bellClip);
            else
                bellAudio.Play();
        }

        yield return new WaitForSeconds(delayBeforeOpening);

        if (neighbourNPC != null)
            neighbourNPC.SetActive(true);

        neighbourDoor.OpenFromScript();

        yield return new WaitUntil(
            () => neighbourDoor != null &&
                  neighbourDoor.IsFullyOpen
        );

        if (dialogueDelayAfterOpening > 0f)
            yield return new WaitForSeconds(
                dialogueDelayAfterOpening
            );

        dialogueFlowchart.ExecuteBlock(dialogueBlock);

        busy = false;
    }

    // Called by Fungus when the neighbour closes their door.
    public void CloseNeighbourDoor()
    {
        if (neighbourDoor != null)
            neighbourDoor.CloseFromScript();
    }

    // Called by Fungus to reopen the door.
    public void OpenNeighbourDoor()
    {
        if (neighbourDoor != null)
            neighbourDoor.OpenFromScript();
    }

    // Called by Fungus at the end of the visit.
    public void FinishVisit()
    {
        StartCoroutine(FinishVisitRoutine());
    }

    private IEnumerator FinishVisitRoutine()
    {
        busy = true;

        if (neighbourDoor != null)
        {
            neighbourDoor.CloseFromScript();

            yield return new WaitUntil(
                () => neighbourDoor == null ||
                      neighbourDoor.IsFullyClosed
            );
        }

        if (neighbourNPC != null)
            neighbourNPC.SetActive(false);

        if (!lockAfterVisit)
            visited = false;

        // Mark visit completed only after the door closes.
        visitCompleted = true;

        busy = false;

        Debug.Log(
            "[NeighbourDoorbell] Visit completed: " + name
        );
    }
}
