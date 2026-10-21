using System.Collections;
using UnityEngine;
using Fungus;

/// <summary>
/// Attach to each neighbour's doorbell (with a collider).
/// Fungus can invoke CloseNeighbourDoor(), OpenNeighbourDoor(), or FinishVisit().
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
    [Min(0f)] public float delayBeforeOpening = 0.8f;

    [Header("Dialogue")]
    public Flowchart dialogueFlowchart;
    public string dialogueBlock = "Start";
    [Min(0f)] public float dialogueDelayAfterOpening = 0f;

    [Header("Visit Settings")]
    public bool allowRepeatVisits = false;
    public bool lockAfterVisit = true;

    private bool busy;
    private bool visited;

    private void Awake()
    {
        if (hideNPCBeforeBell && neighbourNPC != null)
            neighbourNPC.SetActive(false);
    }

    public void Interact()
    {
        if (busy || (visited && !allowRepeatVisits)) return;
        if (neighbourDoor == null || dialogueFlowchart == null ||
            dialogueFlowchart.FindBlock(dialogueBlock) == null)
        {
            Debug.LogWarning("[NeighbourDoorbell] Assign door and valid Fungus block on " + name);
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
            if (bellClip != null) bellAudio.PlayOneShot(bellClip);
            else bellAudio.Play();
        }
        yield return new WaitForSeconds(delayBeforeOpening);
        if (neighbourNPC != null) neighbourNPC.SetActive(true);
        neighbourDoor.OpenFromScript();
        yield return new WaitUntil(() => neighbourDoor != null && neighbourDoor.IsFullyOpen);
        if (dialogueDelayAfterOpening > 0f)
            yield return new WaitForSeconds(dialogueDelayAfterOpening);
        dialogueFlowchart.ExecuteBlock(dialogueBlock);
        busy = false;
    }

    // Invoke from Fungus when the neighbour closes their door.
    public void CloseNeighbourDoor()
    {
        if (neighbourDoor != null) neighbourDoor.CloseFromScript();
    }

    // Invoke from Fungus if the door should reopen.
    public void OpenNeighbourDoor()
    {
        if (neighbourDoor != null) neighbourDoor.OpenFromScript();
    }

    // Invoke at the end of the visit. Hides NPC after the door has closed.
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
            yield return new WaitUntil(() => neighbourDoor == null || neighbourDoor.IsFullyClosed);
        }
        if (neighbourNPC != null) neighbourNPC.SetActive(false);
        if (!lockAfterVisit) visited = false;
        busy = false;
    }
}
