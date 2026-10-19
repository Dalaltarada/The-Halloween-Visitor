using UnityEngine;
using Fungus;

/// <summary>
/// Special visitor scare: the mummy vanishes when the player looks away.
/// Call StartScare() at the end of the greeting Fungus block.
/// Call FinishScareEncounter() at the end of the ending Fungus block.
/// </summary>
public class MummyKidScareEvent : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The player's first-person camera (not the player root).")]
    public Camera playerCamera;

    [Tooltip("The mummy NPC GameObject assigned in TrickOrTreatManager.")]
    public GameObject mummyNPC;

    [Tooltip("The existing visitor manager.")]
    public TrickOrTreatManager trickOrTreatManager;

    [Header("Ending Dialogue")]
    public Flowchart endingFlowchart;
    public string endingBlockName = "Start";

    [Header("Scary Music")]
    [Tooltip("AudioSource with the scary clip assigned. Disable Play On Awake.")]
    public AudioSource scaryMusic;
    public bool stopMusicOnEnding = true;

    [Header("Look Detection")]
    [Tooltip("Degrees the player must turn away from the mummy before it vanishes.")]
    [Range(60f, 180f)] public float lookAwayAngle = 100f;

    [Tooltip("Degrees from the original mummy direction to count as looking back.")]
    [Range(5f, 80f)] public float lookBackAngle = 40f;

    [Tooltip("Minimum delay after starting the scare before the mummy can vanish.")]
    [Min(0f)] public float minimumScareSeconds = 0.5f;

    private enum ScareState { Inactive, WaitingForLookAway, WaitingForLookBack, Ending, Finished }
    [SerializeField] private ScareState state = ScareState.Inactive;

    private Vector3 originalLookDirection;
    private float scareStartTime;

    public void StartScare()
    {
        if (state != ScareState.Inactive)
            return;

        if (playerCamera == null || mummyNPC == null || trickOrTreatManager == null)
        {
            Debug.LogWarning("[MummyKidScareEvent] Assign Player Camera, Mummy NPC and TrickOrTreatManager.");
            return;
        }

        // Remember the direction of the doorway/mummy before the player turns.
        originalLookDirection = mummyNPC.transform.position - playerCamera.transform.position;
        originalLookDirection.y = 0f;
        if (originalLookDirection.sqrMagnitude < 0.001f)
        {
            originalLookDirection = playerCamera.transform.forward;
            originalLookDirection.y = 0f;
        }
        originalLookDirection.Normalize();

        scareStartTime = Time.time;
        state = ScareState.WaitingForLookAway;

        if (scaryMusic != null)
            scaryMusic.Play();

        Debug.Log("[MummyKidScareEvent] Scare started. Turn around.");
    }

    private void Update()
    {
        if (state != ScareState.WaitingForLookAway &&
            state != ScareState.WaitingForLookBack)
            return;

        if (playerCamera == null)
            return;

        Vector3 facing = playerCamera.transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.001f)
            return;

        float angle = Vector3.Angle(originalLookDirection, facing.normalized);

        if (state == ScareState.WaitingForLookAway)
        {
            if (Time.time - scareStartTime >= minimumScareSeconds && angle >= lookAwayAngle)
            {
                mummyNPC.SetActive(false);
                state = ScareState.WaitingForLookBack;
                Debug.Log("[MummyKidScareEvent] Mummy vanished while out of view.");
            }
        }
        else if (angle <= lookBackAngle)
        {
            state = ScareState.Ending;
            if (stopMusicOnEnding && scaryMusic != null)
                scaryMusic.Stop();

            if (endingFlowchart != null && endingFlowchart.FindBlock(endingBlockName) != null)
                endingFlowchart.ExecuteBlock(endingBlockName);
            else
                Debug.LogWarning("[MummyKidScareEvent] Ending Flowchart or block is missing. Call FinishScareEncounter() manually.");
        }
    }

    // Call this as the LAST command of the ending Fungus block.
    public void FinishScareEncounter()
    {
        if (state != ScareState.Ending)
            return;

        if (trickOrTreatManager == null)
            return;

        state = ScareState.Finished;
        if (scaryMusic != null)
            scaryMusic.Stop();

        // A Special encounter's greeting ends in Farewell state.
        // DepartureType.Stay completes it without walking away.
        trickOrTreatManager.FinishGreeting();
        trickOrTreatManager.FinishFarewell();

        Debug.Log("[MummyKidScareEvent] Special mummy encounter finished.");
    }
}
