using System.Collections;
using UnityEngine;
using Fungus;
using UnityStandardAssets.Characters.FirstPerson;

[RequireComponent(typeof(BoxCollider))]
public class GrannyJumpscareEvent : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("FPSController root with CharacterController, not the camera child.")]
    public Transform player;
    public Camera playerCamera;
    [Tooltip("Optional mouse-look behaviour. Do not assign the Camera component.")]
    public Behaviour playerLookController;
    public bool restoreLookAfterDialogue = true;
    public FirstPersonController firstPersonController;
    public bool lockMovementDuringScare = true;
    public bool lockCameraOnGranny = true;
    public float grannyLookHeight = 1.4f;

    [Header("Granny")]
    public GameObject grannyJumpscare;
    [Tooltip("Leave empty for dynamic, player-relative placement.")]
    public Transform appearancePoint;
    [Min(0.1f)] public float leftDistance = 0.85f;
    public float forwardDistance = 0.45f;
    public bool facePlayerOnAppearance = true;

    [Header("Dynamic Placement")]
    [Tooltip("Layers for floors AND solid walls. Exclude Player and NPC layers if possible.")]
    public LayerMask environmentLayers = ~0;
    [Min(0.1f)] public float groundRayHeight = 3f;
    [Min(0.1f)] public float groundRayLength = 8f;
    [Min(0f)] public float feetGroundOffset = 0.02f;
    [Tooltip("Avoid placing Granny inside a wall. Uses a small horizontal sphere check.")]
    public bool avoidObstacles = true;
    [Min(0.05f)] public float obstacleRadius = 0.3f;

    [Header("Camera Turn")]
    [Min(0.01f)] public float turnDuration = 0.35f;
    [Min(0f)] public float dialogueDelayAfterTurn = 0.15f;

    [Header("Sound")]
    public AudioSource jumpscareAudio;
    public AudioClip jumpscareClip;

    [Header("Fungus")]
    public Flowchart dialogueFlowchart;
    public string dialogueBlock = "Start";

    [Header("Chris Interrupts Granny")]
    [Tooltip("A separate Chris copy for this scene. Can start inactive.")]
    public GameObject kid;
    [Tooltip("Where Chris first appears. Place this where the player should see him.")]
    public Transform kidAppearancePoint;
    [Tooltip("Where Chris runs after the player's warning (inside the house).")]
    public Transform kidInsidePoint;
    public float kidLookHeight = 1.25f;
    [Min(0.1f)] public float kidRunSpeed = 4f;
    public bool hideKidOnArrival = true;
    [Header("Chris Legacy Animation")]
    [Tooltip("Assign Chris_Outside's Legacy Animation component.")]
    public Animation kidLegacyAnimation;
    public string kidIdleClip = "PumpkinKid_idle01_CH_Pumpkin_Kid";
    public string kidWalkClip = "PumpkinKid_walk_cycle02_CH_Pumpkin_Kid";
    [Min(0.1f)] public float kidWalkAnimationSpeed = 2f;

    [Header("Candy - Existing Pickup System")]
    [Tooltip("Your existing PickupController on the player.")]
    public PickupController pickupController;
    [Tooltip("The existing candy GameObject from the Hierarchy (must have PickupItem).")]
    public PickupItem candyItem;
    private bool candyReceived;
    public bool CandyReceived => candyReceived && candyItem != null &&
        pickupController != null && pickupController.IsHoldingItem(candyItem);
    public bool JumpscareFinished => finished;

    // Fungus Invoke Method immediately after Granny says "Yes..."
    public void ShowCandyInHand()
    {
        if (CandyReceived) return;
        if (pickupController == null || candyItem == null)
        {
            Debug.LogWarning("[GrannyJumpscare] Assign Pickup Controller and Candy Item in the Inspector.");
            return;
        }
        if (pickupController.HeldItem != null)
        {
            Debug.LogWarning("[GrannyJumpscare] Player is already holding another item. Cannot give candy yet.");
            return;
        }

        // Use the SAME scene candy, not an instantiated copy.
        if (!candyItem.gameObject.activeSelf)
            candyItem.gameObject.SetActive(true);
        candyItem.canPickup = true;
        pickupController.Pickup(candyItem);
        candyReceived = pickupController.IsHoldingItem(candyItem);
        if (!candyReceived)
            Debug.LogWarning("[GrannyJumpscare] Candy pickup failed. Check HoldPoint and PickupItem setup.");
    }

    // Called by ChrisCandyReturn.GiveCandy() after "Here you go.."
    public void GiveCandyToChris()
    {
        if (!CandyReceived)
        {
            Debug.LogWarning("[GrannyJumpscare] Candy is not currently held by the player.");
            return;
        }
        pickupController.ConsumeHeldItem();
        candyReceived = false;
    }

    [Header("Chris Inside - After Granny")]
    [Tooltip("Assign the Chris_InsideCandy GameObject. It stays hidden until Granny's dialogue finishes.")]
    public GameObject chrisInsideCandy;

    [Header("After Dialogue")]
    public bool hideGrannyWhenFinished = true;

    [Header("Testing (Play Mode Only)")]
    public bool showTestButtons = false;

    private bool armed;
    private bool triggered;
    private bool finished;
    private bool reportedEntry;
    private bool cameraLocked;
    private bool lookWasEnabled;
    private bool pausedLook;
    private bool movementWasDisabled;
    private bool movementStateSaved;
    private Coroutine scareRoutine;
    private enum LookTarget { None, Granny, Kid }
    private LookTarget currentLookTarget = LookTarget.None;
    private Coroutine cameraTurnRoutine;
    private Coroutine kidRunRoutine;
    [Header("Chris Faces Player")]
    [Tooltip("Make Chris face the player when he appears and while he says Candy!")]
    public bool kidFacePlayer = true;
    [Tooltip("Use 180 if the imported Chris model faces backwards.")]
    public float kidFacingOffset = 0f;
    private bool kidWaitingForPlayer;

    private void Awake()
    {
        if (chrisInsideCandy != null)
            chrisInsideCandy.SetActive(false);
        GetComponent<BoxCollider>().isTrigger = true;
        if (grannyJumpscare != null) grannyJumpscare.SetActive(false);
        if (kid != null) kid.SetActive(false);
    }

    public void ArmJumpscare()
    {
        if (triggered || finished) return;
        armed = true;
        Debug.Log("[GrannyJumpscare] ARMED. Walk into the trigger zone.");
    }

    private void OnTriggerEnter(Collider other) { CheckPlayer(other); }
    private void OnTriggerStay(Collider other) { CheckPlayer(other); }

    private void CheckPlayer(Collider other)
    {
        if (!armed || triggered || player == null) return;
        Transform hit = other.transform;
        if (hit != player && !hit.IsChildOf(player) && !player.IsChildOf(hit)) return;
        if (!reportedEntry)
        {
            reportedEntry = true;
            Debug.Log("[GrannyJumpscare] Player detected in trigger: " + other.name);
        }
        TriggerJumpscare();
    }

    public void TriggerJumpscare()
    {
        if (!armed || triggered) return;
        if (player == null || playerCamera == null || grannyJumpscare == null)
        {
            Debug.LogError("[GrannyJumpscare] Assign Player, Player Camera, and Granny Jumpscare.");
            return;
        }
        if (dialogueFlowchart == null || dialogueFlowchart.FindBlock(dialogueBlock) == null)
        {
            Debug.LogError("[GrannyJumpscare] Missing Fungus dialogue block: " + dialogueBlock);
            return;
        }

        triggered = true;
        armed = false;
        if (firstPersonController != null && lockMovementDuringScare)
        {
            movementWasDisabled = firstPersonController.disableMovement;
            movementStateSaved = true;
            firstPersonController.disableMovement = true;
        }
        Debug.Log("[GrannyJumpscare] TRIGGERED. Spawning Granny and turning camera.");
        scareRoutine = StartCoroutine(PlayJumpscare());
    }

    private IEnumerator PlayJumpscare()
    {
        Transform granny = grannyJumpscare.transform;
        if (appearancePoint != null)
        {
            granny.position = appearancePoint.position;
            granny.rotation = appearancePoint.rotation;
        }
        else
        {
            Vector3 left = -playerCamera.transform.right;
            Vector3 forward = playerCamera.transform.forward;
            left.y = 0f;
            forward.y = 0f;
            if (left.sqrMagnitude < 0.001f) left = -player.right;
            if (forward.sqrMagnitude < 0.001f) forward = player.forward;
            left.Normalize();
            forward.Normalize();

            Vector3 preferred = player.position + left * leftDistance + forward * forwardDistance;
            Vector3 candidate = preferred;
            // Try a few nearby positions if a wall occupies the left-side location.
            if (avoidObstacles && IsBlocked(candidate))
            {
                Vector3[] alternatives = {
                    player.position + left * (leftDistance + 0.4f) + forward * forwardDistance,
                    player.position + left * leftDistance + forward * (forwardDistance + 0.5f),
                    player.position + left * (leftDistance + 0.4f) - forward * 0.25f
                };
                foreach (Vector3 option in alternatives)
                {
                    if (!IsBlocked(option)) { candidate = option; break; }
                }
            }

            float groundY;
            if (!TryFindGround(candidate, out groundY))
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                groundY = controller != null ? controller.bounds.min.y : player.position.y;
                Debug.LogWarning("[GrannyJumpscare] Ground raycast missed. Using player ground height. Check Environment Layers.");
            }

            // Set Granny's actual visible feet at the detected ground height,
            // even when the imported model's pivot or child mesh has a Y offset.
            granny.position = new Vector3(candidate.x, groundY, candidate.z);
            grannyJumpscare.SetActive(true);
            Renderer[] renderers = grannyJumpscare.GetComponentsInChildren<Renderer>();
            bool foundFeet = false;
            float feetY = float.PositiveInfinity;
            foreach (Renderer r in renderers)
            {
                if (!r.enabled) continue;
                feetY = Mathf.Min(feetY, r.bounds.min.y);
                foundFeet = true;
            }
            if (foundFeet)
                granny.position += Vector3.up * (groundY + feetGroundOffset - feetY);
            else
                granny.position += Vector3.up * feetGroundOffset;
        }

        if (!grannyJumpscare.activeSelf) grannyJumpscare.SetActive(true);
        if (facePlayerOnAppearance)
        {
            Vector3 direction = player.position - granny.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                granny.rotation = Quaternion.LookRotation(direction);
        }

        if (jumpscareAudio != null)
        {
            if (jumpscareClip != null) jumpscareAudio.PlayOneShot(jumpscareClip);
            else jumpscareAudio.Play();
        }

        if (playerLookController != null && !(playerLookController is Camera))
        {
            lookWasEnabled = playerLookController.enabled;
            pausedLook = true;
            playerLookController.enabled = false;
        }

        Transform cam = playerCamera.transform;
        Quaternion from = cam.rotation;
        Quaternion to = GetGrannyLookRotation(cam.position, from);
        float elapsed = 0f;
        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            cam.rotation = Quaternion.Slerp(from, to,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / turnDuration)));
            yield return null;
        }
        cam.rotation = to;
        cameraLocked = lockCameraOnGranny;
        currentLookTarget = LookTarget.Granny;
        if (dialogueDelayAfterTurn > 0f)
            yield return new WaitForSeconds(dialogueDelayAfterTurn);
        Debug.Log("[GrannyJumpscare] Starting Granny dialogue.");
        dialogueFlowchart.ExecuteBlock(dialogueBlock);
        scareRoutine = null;
    }

    private Quaternion GetGrannyLookRotation(Vector3 cameraPosition, Quaternion fallback)
    {
        if (grannyJumpscare == null) return fallback;
        Vector3 direction = grannyJumpscare.transform.position + Vector3.up * grannyLookHeight - cameraPosition;
        return direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : fallback;
    }

    private void LateUpdate()
    {
        if (kidWaitingForPlayer && kidFacePlayer && kid != null && kid.activeInHierarchy)
            FaceKidTowardPlayer();
        if (!cameraLocked || playerCamera == null || cameraTurnRoutine != null) return;
        playerCamera.transform.rotation = GetTargetLookRotation(currentLookTarget, playerCamera.transform.rotation);
    }

    private Quaternion GetTargetLookRotation(LookTarget target, Quaternion fallback)
    {
        if (target == LookTarget.Granny)
            return GetGrannyLookRotation(playerCamera.transform.position, fallback);
        if (target == LookTarget.Kid && kid != null && kid.activeInHierarchy)
        {
            Vector3 direction = kid.transform.position + Vector3.up * kidLookHeight - playerCamera.transform.position;
            if (direction.sqrMagnitude > 0.001f) return Quaternion.LookRotation(direction);
        }
        return fallback;
    }

    // Fungus Invoke Method after Chris says "Candy!"
    public void ShowKidAndLook()
    {
        if (!triggered || finished || kid == null || kidAppearancePoint == null)
        {
            Debug.LogWarning("[GrannyJumpscare] Assign Kid and Kid Appearance Point; jumpscare must be active.");
            return;
        }
        kid.transform.SetPositionAndRotation(kidAppearancePoint.position, kidAppearancePoint.rotation);
        kid.SetActive(true);
        kidWaitingForPlayer = true;
        FaceKidTowardPlayer();
        PlayKidAnimation(kidIdleClip, 1f);
        LookAtKid();
    }

    private void FaceKidTowardPlayer()
    {
        if (kid == null || player == null) return;
        Vector3 direction = player.position - kid.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            kid.transform.rotation = Quaternion.LookRotation(direction) *
                Quaternion.Euler(0f, kidFacingOffset, 0f);
    }

    // Optional separate Fungus Invoke Method, if Chris is already visible.
    public void LookAtKid()
    {
        if (kid == null || !kid.activeInHierarchy) return;
        TurnCameraTo(LookTarget.Kid);
    }

    // Fungus Invoke Method after "Hey! Didn't I tell you to wait inside?"
    public void SendKidInside()
    {
        if (kid == null || !kid.activeInHierarchy || kidInsidePoint == null)
        {
            Debug.LogWarning("[GrannyJumpscare] Assign Kid and Kid Inside Point.");
            return;
        }
        kidWaitingForPlayer = false;
        if (kidRunRoutine != null) StopCoroutine(kidRunRoutine);
        kidRunRoutine = StartCoroutine(RunKidInside());
    }

    private IEnumerator RunKidInside()
    {
        PlayKidAnimation(kidWalkClip, kidWalkAnimationSpeed);
        while (kid != null && Vector3.Distance(kid.transform.position, kidInsidePoint.position) > 0.08f)
        {
            Vector3 direction = kidInsidePoint.position - kid.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                kid.transform.rotation = Quaternion.RotateTowards(kid.transform.rotation,
                    Quaternion.LookRotation(direction), 720f * Time.deltaTime);
            kid.transform.position = Vector3.MoveTowards(kid.transform.position,
                kidInsidePoint.position, kidRunSpeed * Time.deltaTime);
            yield return null;
        }
        if (kid != null)
        {
            if (hideKidOnArrival) kid.SetActive(false);
            else PlayKidAnimation(kidIdleClip, 1f);
        }
        kidRunRoutine = null;
    }

    private void PlayKidAnimation(string clipName, float playbackSpeed)
    {
        if (kidLegacyAnimation == null && kid != null)
            kidLegacyAnimation = kid.GetComponentInChildren<Animation>(true);

        if (kidLegacyAnimation == null)
        {
            Debug.LogWarning("[GrannyJumpscare] Chris has no Legacy Animation component assigned.");
            return;
        }

        if (string.IsNullOrEmpty(clipName) || kidLegacyAnimation.GetClip(clipName) == null)
        {
            Debug.LogWarning("[GrannyJumpscare] Missing Chris Legacy clip: " + clipName);
            return;
        }

        AnimationState state = kidLegacyAnimation[clipName];
        state.wrapMode = WrapMode.Loop;
        state.speed = Mathf.Max(0.1f, playbackSpeed);
        kidLegacyAnimation.Play(clipName);
    }

    // Fungus Invoke Method before Granny asks "Who are you talking to, dear?"
    public void LookBackAtGranny()
    {
        if (!triggered || finished) return;
        TurnCameraTo(LookTarget.Granny);
    }

    private void TurnCameraTo(LookTarget target)
    {
        if (playerCamera == null) return;
        if (cameraTurnRoutine != null) StopCoroutine(cameraTurnRoutine);
        cameraTurnRoutine = StartCoroutine(SmoothLookAt(target));
    }

    private IEnumerator SmoothLookAt(LookTarget target)
    {
        cameraLocked = true;
        currentLookTarget = target;
        Quaternion start = playerCamera.transform.rotation;
        float duration = Mathf.Max(0.05f, turnDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Quaternion end = GetTargetLookRotation(target, start);
            playerCamera.transform.rotation = Quaternion.Slerp(start, end,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        playerCamera.transform.rotation = GetTargetLookRotation(target, start);
        cameraTurnRoutine = null;
    }

    private bool TryFindGround(Vector3 position, out float groundY)
    {
        groundY = position.y;
        Ray ray = new Ray(position + Vector3.up * groundRayHeight, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, groundRayHeight + groundRayLength, environmentLayers, QueryTriggerInteraction.Ignore);
        float closest = float.PositiveInfinity;
        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (IsOwnCollider(hit.collider)) continue;
            if (hit.distance < closest)
            {
                closest = hit.distance;
                groundY = hit.point.y;
                found = true;
            }
        }
        return found;
    }

    private bool IsBlocked(Vector3 position)
    {
        float floorY;
        if (!TryFindGround(position, out floorY))
            floorY = player.position.y;
        Collider[] hits = Physics.OverlapSphere(new Vector3(position.x, floorY + 0.9f, position.z),
            obstacleRadius, environmentLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
            if (!IsOwnCollider(hit)) return true;
        return false;
    }

    private bool IsOwnCollider(Collider collider)
    {
        Transform t = collider.transform;
        if (t == transform || t.IsChildOf(transform)) return true;
        if (player != null && (t == player || t.IsChildOf(player))) return true;
        if (grannyJumpscare != null && (t == grannyJumpscare.transform || t.IsChildOf(grannyJumpscare.transform))) return true;
        return false;
    }

    public void FinishJumpscare()
    {
        if (!triggered || finished) return;
        finished = true;
        if (chrisInsideCandy != null)
            chrisInsideCandy.SetActive(true);
        kidWaitingForPlayer = false;
        if (cameraTurnRoutine != null) { StopCoroutine(cameraTurnRoutine); cameraTurnRoutine = null; }
        RestoreControls();
        if (hideGrannyWhenFinished && grannyJumpscare != null)
            grannyJumpscare.SetActive(false);
    }

    private void RestoreControls()
    {
        cameraLocked = false;
        currentLookTarget = LookTarget.None;
        if (movementStateSaved && firstPersonController != null)
        {
            firstPersonController.disableMovement = movementWasDisabled;
            movementStateSaved = false;
        }
        if (restoreLookAfterDialogue && pausedLook && playerLookController != null)
        {
            playerLookController.enabled = lookWasEnabled;
            pausedLook = false;
        }
    }

    private void OnDisable()
    {
        kidWaitingForPlayer = false;
        if (scareRoutine != null) StopCoroutine(scareRoutine);
        if (cameraTurnRoutine != null) StopCoroutine(cameraTurnRoutine);
        if (kidRunRoutine != null) StopCoroutine(kidRunRoutine);
        RestoreControls();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !showTestButtons || triggered || finished) return;
        if (GUI.Button(new Rect(16, 16, 210, 38), "TEST: Arm Granny Trigger")) ArmJumpscare();
        if (GUI.Button(new Rect(16, 60, 210, 38), "TEST: Jumpscare NOW"))
        {
            ArmJumpscare();
            TriggerJumpscare();
        }
    }
}
