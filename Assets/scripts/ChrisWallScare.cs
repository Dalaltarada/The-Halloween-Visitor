using System.Collections;
using UnityEngine;
using Fungus;

/// <summary>
/// Entering the trigger reveals Chris by the fridge.
/// Looking at Chris makes him run to Point 2, disappear, then triggers
/// the Fungus shout and reveals crying Chris after a delay.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChrisWallScare : MonoBehaviour
{
    [Header("Task Gate")]
    public DialogueTaskManager taskManager;
    public string requiredTaskID = "CheckHouse";

    [Header("Detection")]
    public Transform player;
    public Camera playerCamera;
    // Kept for compatibility with previously configured scenes; no longer used.
    public Collider wallWritingCollider;
    public Collider fridgeChrisCollider;
    public float lookDistance = 10f;

    [Header("Chris at Fridge")]
    public GameObject fridgeChris;
    public Animation fridgeAnimation;
    public string idleClip = "PumpkinKid_idle01_CH_Pumpkin_Kid";
    public string runClip = "PumpkinKid_walk_cycle02_CH_Pumpkin_Kid";
    public Transform point2;
    public float runSpeed = 6f;
    public float turnSpeed = 900f;

    [Header("Fridge")]
    [Tooltip("Assign the DoorInteractable on the actual refrigerator door. The door remains active.")]
    public DoorInteractable fridgeDoor;
    [Tooltip("Old reference retained for existing Inspector data; this object is never toggled.")]
    public GameObject openFridgeDoor;

    [Header("Running SFX")]
    public AudioSource runningAudio;
    public AudioClip runningSound;

    [Header("Fungus")]
    public Flowchart shoutFlowchart;
    public string shoutBlock = "Start";

    [Header("Crying Room")]
    public float cryingDelay = 5f;
    public GameObject cryingChris;
    public AudioSource cryingSound;

    private Collider triggerZone;
    private bool writingTriggered;
    private bool chaseStarted;

    private void Awake()
    {
        triggerZone = GetComponent<Collider>();
        if (!triggerZone.isTrigger)
            Debug.LogWarning("[ChrisWallScare] Set the zone collider to Is Trigger.", this);

        // Keep the trigger inactive until the CheckHouse task begins.
        triggerZone.enabled = false;

        if (fridgeChris != null) fridgeChris.SetActive(false);
        if (cryingChris != null) cryingChris.SetActive(false);
        // Never disable the fridge door or the wall writing.
        // Ensure Chris starts hidden until the player enters the zone.
    }

    private void Update()
    {
        // Enable the trigger only while CheckHouse is active. Once the
        // scare starts, keep it enabled so leaving the zone does not
        // interrupt the sequence.
        if (!writingTriggered && triggerZone != null)
        {
            bool taskActive = taskManager != null &&
                taskManager.IsTaskStarted(requiredTaskID);
            if (triggerZone.enabled != taskActive)
                triggerZone.enabled = taskActive;
        }

        if (!writingTriggered || chaseStarted) return;

        if (LookingAt(fridgeChrisCollider))
        {
            if (fridgeChris == null || point2 == null)
            {
                Debug.LogWarning("[ChrisWallScare] Assign Fridge Chris and Point 2.", this);
                return;
            }

            chaseStarted = true;
            StartCoroutine(ChrisRunsAway());
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (player == null || other == null) return false;
        Transform t = other.transform;
        return t == player || t.IsChildOf(player) || player.IsChildOf(t);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other) || !TaskIsActive()) return;
        RevealFridgeChris();
    }

    private void OnTriggerStay(Collider other)
    {
        // Handles a player already standing inside when the collider enables.
        if (!IsPlayer(other) || !TaskIsActive()) return;
        RevealFridgeChris();
    }

    private bool TaskIsActive()
    {
        return taskManager != null && taskManager.IsTaskStarted(requiredTaskID);
    }

    private void RevealFridgeChris()
    {
        if (writingTriggered) return;
        writingTriggered = true;

        if (fridgeChris != null) fridgeChris.SetActive(true);
        // Open the actual hinged door using its existing interaction logic.
        // Do not toggle the door GameObject; that bypasses its rotation system.
        if (fridgeDoor != null && fridgeDoor.IsFullyClosed)
            fridgeDoor.Interact();
        else if (fridgeDoor == null)
            Debug.LogWarning("[ChrisWallScare] Assign Fridge Door (DoorInteractable).", this);
        PlayAnimation(idleClip);
        Debug.Log("[ChrisWallScare] Player entered zone: Chris appeared by fridge.");
    }

    private bool LookingAt(Collider target)
    {
        if (playerCamera == null || target == null || !target.gameObject.activeInHierarchy)
            return false;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, lookDistance, ~0, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider == target ||
               hit.collider.transform.IsChildOf(target.transform) ||
               target.transform.IsChildOf(hit.collider.transform);
    }

    private IEnumerator ChrisRunsAway()
    {
        PlayAnimation(runClip);

        if (runningAudio != null)
        {
            if (runningSound != null) runningAudio.PlayOneShot(runningSound);
            else runningAudio.Play();
        }

        Transform kid = fridgeChris.transform;
        Vector3 destination = point2.position;
        destination.y = kid.position.y;

        while (Vector3.Distance(kid.position, destination) > 0.02f)
        {
            Vector3 direction = destination - kid.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                kid.rotation = Quaternion.RotateTowards(
                    kid.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            kid.position = Vector3.MoveTowards(
                kid.position, destination, Mathf.Max(0.01f, runSpeed) * Time.deltaTime);
            yield return null;
        }

        fridgeChris.SetActive(false);
        if (runningAudio != null) runningAudio.Stop();

        if (shoutFlowchart != null && shoutFlowchart.FindBlock(shoutBlock) != null)
            shoutFlowchart.ExecuteBlock(shoutBlock);
        else
            Debug.LogWarning("[ChrisWallScare] Shout Flowchart or block is missing.", this);

        yield return new WaitForSeconds(Mathf.Max(0f, cryingDelay));

        if (cryingChris != null) cryingChris.SetActive(true);
        if (cryingSound != null) cryingSound.Play();
    }

    private void PlayAnimation(string clipName)
    {
        if (fridgeAnimation == null || string.IsNullOrEmpty(clipName)) return;

        if (fridgeAnimation.GetClip(clipName) != null)
            fridgeAnimation.Play(clipName);
        else
            Debug.LogWarning("[ChrisWallScare] Missing animation: " + clipName, this);
    }
}
