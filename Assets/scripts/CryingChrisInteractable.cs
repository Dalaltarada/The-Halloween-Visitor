using UnityEngine;
using Fungus;

/// <summary>
/// Starts Chris's crying-room dialogue and controls his scripted look/follow behavior.
/// Add to Chris_Crying, which must have a Collider for the player's IInteractable raycast.
/// </summary>
public class CryingChrisInteractable : MonoBehaviour, IInteractable
{
    [Header("Fungus Dialogue")]
    public Flowchart dialogueFlowchart;
    public string blockName = "Start";

    [Header("Crying")]
    public AudioSource cryingAudio;
    public bool stopCryingOnInteract = true;

    [Header("Movement")]
    public Transform player;
    public Transform livingRoomWaitPoint;
    public float followSpeed = 2.5f;
    public float stoppingDistance = 1.8f;
    public float turnSpeed = 360f;
    public float waitPointArrivalDistance = 0.15f;

    [Header("Optional Fixed Waypoint Route")]
    [Tooltip("If assigned, Chris follows these points in order instead of chasing the player. Place the final point in the living room.")]
    public Transform[] waypoints;
    [Tooltip("How close Chris must get before moving to the next waypoint.")]
    public float waypointArrivalDistance = 0.2f;

    [Header("Legacy Animation")]
    public Animation legacyAnimation;
    public string idleClip = "PumpkinKid_idle01_CH_Pumpkin_Kid";
    public string walkClip = "PumpkinKid_walk_cycle02_CH_Pumpkin_Kid";

    [Header("Follow Behavior")]
    [Tooltip("When enabled, Chris walks to the living-room point after following the player near it.")]
    public bool goToWaitPointWhenPlayerNearby = true;
    public float playerNearWaitPointDistance = 3f;

    private bool interacted;
    private bool following;
    private bool goingToWaitPoint;
    private bool arrived;
    private string currentClip;
    private int waypointIndex;

    private void Awake()
    {
        if (legacyAnimation == null)
            legacyAnimation = GetComponent<Animation>();
    }

    public void Interact()
    {
        if (interacted) return;

        if (dialogueFlowchart == null || dialogueFlowchart.FindBlock(blockName) == null)
        {
            Debug.LogWarning("[CryingChris] Missing Fungus dialogue.");
            return;
        }

        interacted = true;
        if (stopCryingOnInteract && cryingAudio != null)
            cryingAudio.Stop();

        dialogueFlowchart.ExecuteBlock(blockName);
    }

    // Fungus Invoke Method after "Yes! I promise you!"
    public void LookAtPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("[CryingChris] Assign Player in the Inspector.");
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    // Fungus Invoke Method after "Wait for me in the living room."
    public void StartFollowing()
    {
        bool hasRoute = waypoints != null && waypoints.Length > 0;
        if (!hasRoute && (player == null || livingRoomWaitPoint == null))
        {
            Debug.LogWarning("[CryingChris] Assign Waypoints, or assign Player and Living Room Wait Point for the original follow behavior.");
            return;
        }

        following = true;
        goingToWaitPoint = false;
        arrived = false;
        waypointIndex = 0;
    }

    private void Update()
    {
        if (!following || arrived)
            return;

        // A fixed route takes priority, preventing Chris from cutting through furniture.
        if (waypoints != null && waypoints.Length > 0)
        {
            FollowWaypoints();
            return;
        }

        if (player == null || livingRoomWaitPoint == null)
            return;

        if (!goingToWaitPoint && goToWaitPointWhenPlayerNearby &&
            Vector3.Distance(player.position, livingRoomWaitPoint.position) <= playerNearWaitPointDistance)
        {
            goingToWaitPoint = true;
        }

        Vector3 target = goingToWaitPoint ? livingRoomWaitPoint.position : player.position;
        target.y = transform.position.y;
        Vector3 delta = target - transform.position;
        float distance = delta.magnitude;
        float stopAt = goingToWaitPoint ? waitPointArrivalDistance : stoppingDistance;

        if (distance <= stopAt)
        {
            PlayClip(idleClip);
            if (goingToWaitPoint)
            {
                arrived = true;
                following = false;
                Debug.Log("[CryingChris] Arrived at living-room wait point.");
            }
            return;
        }

        if (delta.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(delta);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(
            transform.position, target, followSpeed * Time.deltaTime);
        PlayClip(walkClip);
    }

    private void FollowWaypoints()
    {
        // Skip missing or already-reached waypoints without getting stuck.
        while (waypointIndex < waypoints.Length)
        {
            Transform point = waypoints[waypointIndex];
            if (point == null)
            {
                waypointIndex++;
                continue;
            }

            Vector3 destination = point.position;
            destination.y = transform.position.y;
            Vector3 direction = destination - transform.position;

            if (direction.magnitude <= Mathf.Max(0.01f, waypointArrivalDistance))
            {
                waypointIndex++;
                continue;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            transform.position = Vector3.MoveTowards(
                transform.position, destination, followSpeed * Time.deltaTime);
            PlayClip(walkClip);
            return;
        }

        // All waypoints reached: wait here and return to idle.
        following = false;
        arrived = true;
        PlayClip(idleClip);
        Debug.Log("[CryingChris] Reached final waypoint and is waiting.");
    }

    private void PlayClip(string clip)
    {
        if (legacyAnimation == null || string.IsNullOrEmpty(clip) || currentClip == clip)
            return;

        if (legacyAnimation.GetClip(clip) == null)
        {
            Debug.LogWarning("[CryingChris] Missing Legacy Animation clip: " + clip);
            return;
        }

        legacyAnimation.Play(clip);
        currentClip = clip;
    }
}
