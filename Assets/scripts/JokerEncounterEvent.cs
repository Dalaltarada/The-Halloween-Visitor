using System.Collections;
using UnityEngine;
using Fungus;

/// <summary>
/// Handles the Joker's two visits using the existing TrickOrTreatManager.
/// First visit: silent arrival, aura music, custom walking exit.
/// Second visit: normal doorbell, faces player, normal candy and departure.
/// </summary>
public class JokerEncounterEvent : MonoBehaviour
{
    [Header("Existing Systems")]
    public TrickOrTreatManager manager;
    public DoorInteractable frontDoor;
    public GameObject jokerNPC;

    [Header("Encounter IDs")]
    public string firstEncounterID = "JokerFirst";
    public string secondEncounterID = "JokerSecond";

    [Header("Music / Timing")]
    public AudioSource auraMusic;
    [Min(0f)] public float secondVisitDelay = 2f;

    [Header("Joker Rotation")]
    public Transform player;
    [Min(0.05f)] public float turnDuration = 2f;

    [Header("First Visit Walking Exit")]
    [Tooltip("Uses the manager's exitWaypoints if this is empty.")]
    public Transform[] firstExitWaypoints;
    [Min(0.01f)] public float firstExitSpeed = 1.5f;
    [Min(1f)] public float firstExitRotationSpeed = 360f;
    public Animator jokerAnimator;
    public string walkAnimation = "walk";

    [Header("Fungus")]
    public Flowchart musicIntroFlowchart;
    public Flowchart doorClosedFlowchart;
    public Flowchart emptyBowlFlowchart;
    public string fungusBlockName = "Start";

    [Header("Candy Visual (decorative only)")]
    public GameObject candyBeforeEmpty;

    private Quaternion originalJokerRotation;
    private Coroutine turnRoutine;
    private bool sequenceStarted;
    private bool firstVisitStarted;
    private bool firstExitStarted;
    private bool secondVisitStarted;
    private bool endingStarted;
    private AudioClip savedDoorbellSound;
    private bool firstDoorbellMuted;
    private Coroutine finalMusicRoutine;

    private void Awake()
    {
        if (jokerNPC != null)
            originalJokerRotation = jokerNPC.transform.rotation;
    }

    /// <summary>Invoke at the end of the mummy's ending Flowchart.</summary>
    public void BeginJokerSequence()
    {
        if (sequenceStarted) return;
        if (manager == null || frontDoor == null)
        {
            Debug.LogWarning("[Joker] Assign Manager and Front Door.");
            return;
        }
        sequenceStarted = true;
        StartCoroutine(BeginAfterDoorClosed());
    }

    private IEnumerator BeginAfterDoorClosed()
    {
        yield return new WaitUntil(() => frontDoor != null && frontDoor.IsFullyClosed);

        if (auraMusic != null && !auraMusic.isPlaying)
            auraMusic.Play();

        if (musicIntroFlowchart != null)
            ExecuteFlowchart(musicIntroFlowchart);
        else
            StartFirstVisit();
    }

    /// <summary>Invoke after 'What's that music?' No doorbell on first visit.</summary>
    public void StartFirstVisit()
    {
        if (firstVisitStarted || manager == null) return;
        firstVisitStarted = true;
        StartCoroutine(StartFirstVisitSilently());
    }

    private IEnumerator StartFirstVisitSilently()
    {
        // The manager rings a bell during BeginEncounter; temporarily remove its clip.
        // Restore it as soon as the first arrival is complete.
        savedDoorbellSound = manager.doorbellSound;
        manager.doorbellSound = null;
        firstDoorbellMuted = true;
        manager.StartEncounter(firstEncounterID);

        // Allow manager to enter WaitingForClosedDoor, then wait for arrival.
        yield return null;
        float timeout = 30f;
        while (manager != null && manager.CurrentState != TrickOrTreatManager.VisitState.AtDoor && timeout > 0f)
        {
            // If the encounter did not start, avoid muting the doorbell indefinitely.
            if (manager.CurrentState == TrickOrTreatManager.VisitState.Waiting ||
                manager.CurrentState == TrickOrTreatManager.VisitState.Completed)
                break;
            timeout -= Time.deltaTime;
            yield return null;
        }
        RestoreDoorbell();
    }

    private void RestoreDoorbell()
    {
        if (!firstDoorbellMuted || manager == null) return;
        manager.doorbellSound = savedDoorbellSound;
        firstDoorbellMuted = false;
    }

    private void OnDisable()
    {
        RestoreDoorbell();
    }

    public void TurnTowardPlayer()
    {
        if (jokerNPC == null || player == null)
        {
            Debug.LogWarning("[Joker] Assign Joker NPC and Player.");
            return;
        }
        Vector3 direction = player.position - jokerNPC.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        StartTurn(Quaternion.LookRotation(direction, Vector3.up));
    }

    public void TurnAway()
    {
        if (jokerNPC != null) StartTurn(originalJokerRotation);
    }

    private void StartTurn(Quaternion target)
    {
        if (turnRoutine != null) StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnTo(target));
    }

    private IEnumerator TurnTo(Quaternion target)
    {
        Quaternion start = jokerNPC.transform.rotation;
        float elapsed = 0f;
        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / turnDuration));
            jokerNPC.transform.rotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }
        jokerNPC.transform.rotation = target;
        turnRoutine = null;
    }

    /// <summary>Invoke after 'Keep your candy. I have a message.'</summary>
    public void WaitForDoorToClose()
    {
        if (firstExitStarted) return;
        firstExitStarted = true;
        StartCoroutine(FirstVisitExit());
    }

    private IEnumerator FirstVisitExit()
    {
        if (manager == null || frontDoor == null || jokerNPC == null)
        {
            Debug.LogWarning("[Joker] Missing first-exit references.");
            yield break;
        }

        // First encounter must be Special; manager changes Greeting -> Farewell.
        manager.FinishGreeting();

        // Wait for the 'turn away' animation to finish before walking.
        while (turnRoutine != null) yield return null;

        Animator animator = jokerAnimator;
        if (animator == null) animator = jokerNPC.GetComponentInChildren<Animator>();
        if (animator != null && !string.IsNullOrEmpty(walkAnimation))
            animator.Play(walkAnimation);

        Transform[] waypoints = (firstExitWaypoints != null && firstExitWaypoints.Length > 0)
            ? firstExitWaypoints : manager.exitWaypoints;

        if (waypoints == null || waypoints.Length == 0)
            Debug.LogWarning("[Joker] No exit waypoints assigned for first visit.");
        else
        {
            foreach (Transform waypoint in waypoints)
            {
                if (waypoint == null) continue;
                Vector3 destination = waypoint.position;
                destination.y = jokerNPC.transform.position.y;
                while (Vector3.Distance(jokerNPC.transform.position, destination) > 0.005f)
                {
                    Vector3 direction = destination - jokerNPC.transform.position;
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        Quaternion facing = Quaternion.LookRotation(direction);
                        jokerNPC.transform.rotation = Quaternion.RotateTowards(
                            jokerNPC.transform.rotation, facing, firstExitRotationSpeed * Time.deltaTime);
                    }
                    jokerNPC.transform.position = Vector3.MoveTowards(
                        jokerNPC.transform.position, destination, firstExitSpeed * Time.deltaTime);
                    yield return null;
                }
                jokerNPC.transform.position = destination;
            }
        }

        // Music stops once Joker has left, not when the player closes the door.
        if (auraMusic != null) auraMusic.Stop();

        // Let the player close the door after seeing him leave.
        yield return new WaitUntil(() => frontDoor != null && frontDoor.IsFullyClosed);

        // First encounter MUST have Departure = Stay, since we walked manually.
        manager.FinishFarewell();
        yield return new WaitUntil(() => manager.CurrentState == TrickOrTreatManager.VisitState.Completed);
        yield return null;

        if (doorClosedFlowchart != null) ExecuteFlowchart(doorClosedFlowchart);
        else StartSecondVisit();
    }

    /// <summary>Invoke after the player's Dark Knight joke.</summary>
    public void StartSecondVisit()
    {
        if (secondVisitStarted || manager == null) return;
        secondVisitStarted = true;
        StartCoroutine(SecondVisitArrival());
    }

    private IEnumerator SecondVisitArrival()
    {
        if (secondVisitDelay > 0f)
            yield return new WaitForSeconds(secondVisitDelay);

        // Manager restores the NPC's initial position and facing direction.
        // Turn toward the player after spawning, before the doorbell finishes.
        manager.StartEncounter(secondEncounterID);
        yield return new WaitUntil(() => manager.CurrentState == TrickOrTreatManager.VisitState.AtDoor);
        TurnTowardPlayer();
    }

    /// <summary>
    /// Invoke in Joker_Farewell immediately after "The Joker never sleeps."
    /// Plays the aura music until the player closes the front door.
    /// </summary>
    public void PlayFinalAuraMusic()
    {
        if (auraMusic == null || frontDoor == null)
        {
            Debug.LogWarning("[Joker] Assign Aura Music and Front Door for the final music.");
            return;
        }

        if (finalMusicRoutine != null)
            StopCoroutine(finalMusicRoutine);

        auraMusic.Stop();
        auraMusic.Play();
        finalMusicRoutine = StartCoroutine(StopFinalMusicWhenDoorCloses());
    }

    private IEnumerator StopFinalMusicWhenDoorCloses()
    {
        // Require an actual open -> closed transition so music doesn't
        // instantly stop if the method is invoked while the door is closed.
        yield return new WaitUntil(() => frontDoor != null && frontDoor.IsOpen);
        yield return new WaitUntil(() => frontDoor != null && frontDoor.IsFullyClosed);

        if (auraMusic != null)
            auraMusic.Stop();

        finalMusicRoutine = null;
    }

    /// <summary>Invoke at the end of Joker_Farewell.</summary>
    public void FinishSecondVisit()
    {
        if (endingStarted || manager == null) return;
        endingStarted = true;
        manager.FinishFarewell();
        StartCoroutine(AfterSecondDeparture());
    }

    private IEnumerator AfterSecondDeparture()
    {
        yield return new WaitUntil(() => manager.CurrentState == TrickOrTreatManager.VisitState.Completed);
        if (candyBeforeEmpty != null) candyBeforeEmpty.SetActive(false);
        ExecuteFlowchart(emptyBowlFlowchart);
    }

    private void ExecuteFlowchart(Flowchart flowchart)
    {
        if (flowchart == null) return;
        if (flowchart.FindBlock(fungusBlockName) == null)
        {
            Debug.LogWarning("[Joker] Missing Fungus block: " + fungusBlockName);
            return;
        }
        flowchart.ExecuteBlock(fungusBlockName);
    }
}
