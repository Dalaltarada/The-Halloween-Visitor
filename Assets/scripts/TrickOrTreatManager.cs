
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fungus;

public class TrickOrTreatManager : MonoBehaviour
{
    public enum VisitState
    {
        Waiting,
        WaitingForClosedDoor,
        AtDoor,
        Greeting,
        WaitingForCandy,
        HoldingCandy,
        Farewell,
        Leaving,
        Completed
    }

    public enum EncounterType
    {
        StandardCandy,
        NoCandy,
        Special
    }

    public enum DepartureType
    {
        Walk,
        Run,
        Stay
    }

    [System.Serializable]
    public class VisitorEncounter
    {
        [Header("Identification")]
        public string encounterID;

        [Header("NPC")]
        public GameObject npc;
        public Animator animator;

        [Header("Separate Fungus Flowcharts")]
        public Flowchart greetingFlowchart;
        public Flowchart farewellFlowchart;

        [Header("Encounter")]
        public EncounterType encounterType =
            EncounterType.StandardCandy;

        public bool autoStartNext = false;

        [Header("Departure")]
        public DepartureType departure =
            DepartureType.Walk;

        public float movementSpeed = 1.5f;
        public float rotationSpeed = 8f;

        [Header("Animation State Names")]
        public string idleAnimation = "idle";
        public string walkAnimation = "walk";
        public string runAnimation = "run";
    }

    [Header("Door")]
    public DoorInteractable frontDoor;

    [Header("Doorbell")]
    public AudioSource doorbellSource;
    public AudioClip doorbellSound;
    public float doorbellDelay = 1f;

    [Header("Candy Requirement")]
    public DialogueTaskManager taskManager;
    public string requiredTaskID = "CandyTask";

    [Header("Pickup System")]
    public PickupController pickupController;

    [Tooltip("Candy object with a PickupItem component.")]
    public PickupItem candyItem;

    [Header("Shared NPC Exit Path")]
    [Tooltip("All NPCs follow these points in order after farewell.")]
    public Transform[] exitWaypoints;

    [Header("Visitor Encounters")]
    public VisitorEncounter[] encounters;

    [Header("Task 3 - Adult Encounter")]
    [Tooltip("Encounter ID that starts the TV and couch task after leaving.")]
    public string adultEncounterID = "Adult";

    [Tooltip("Assign the GameObject containing TVRelaxTask.")]
    public TVRelaxTask tvRelaxTask;

    [Header("Starting Settings")]
    public bool startFirstEncounterAutomatically = false;

    [Header("Runtime State")]
    [SerializeField]
    private VisitState currentState = VisitState.Waiting;

    [SerializeField]
    private int currentEncounterIndex = -1;

    private Coroutine arrivalRoutine;
    private Coroutine departureRoutine;

    private readonly HashSet<string> completedEncounters =
        new HashSet<string>();

    private readonly Dictionary<GameObject, Vector3>
        originalPositions =
            new Dictionary<GameObject, Vector3>();

    private readonly Dictionary<GameObject, Quaternion>
        originalRotations =
            new Dictionary<GameObject, Quaternion>();

    public VisitState CurrentState => currentState;

    private VisitorEncounter CurrentEncounter
    {
        get
        {
            if (encounters == null ||
                currentEncounterIndex < 0 ||
                currentEncounterIndex >= encounters.Length)
                return null;

            return encounters[currentEncounterIndex];
        }
    }

    private void Start()
    {
        // Hide candy until it is needed.
        if (candyItem != null)
            candyItem.gameObject.SetActive(false);

        if (encounters != null)
        {
            foreach (VisitorEncounter visitor in encounters)
            {
                if (visitor.npc == null)
                    continue;

                if (!originalPositions.ContainsKey(visitor.npc))
                {
                    originalPositions.Add(
                        visitor.npc,
                        visitor.npc.transform.position
                    );

                    originalRotations.Add(
                        visitor.npc,
                        visitor.npc.transform.rotation
                    );
                }

                visitor.npc.SetActive(false);
            }
        }

        if (startFirstEncounterAutomatically)
            StartEncounterByIndex(0);
    }

    public void StartFirstEncounter()
    {
        StartEncounterByIndex(0);
    }

    public void StartEncounter(string encounterID)
    {
        if (encounters == null)
            return;

        for (int i = 0; i < encounters.Length; i++)
        {
            if (encounters[i].encounterID == encounterID)
            {
                StartEncounterByIndex(i);
                return;
            }
        }

        Debug.LogWarning(
            "[TrickOrTreat] Unknown encounter: " +
            encounterID
        );
    }

    public void StartEncounterByIndex(int index)
    {
        if (encounters == null ||
            index < 0 ||
            index >= encounters.Length)
        {
            Debug.LogWarning(
                "[TrickOrTreat] Invalid encounter index."
            );
            return;
        }

        VisitorEncounter visitor = encounters[index];

        if (string.IsNullOrEmpty(visitor.encounterID))
        {
            Debug.LogWarning(
                "[TrickOrTreat] Encounter ID is empty."
            );
            return;
        }

        if (completedEncounters.Contains(visitor.encounterID))
        {
            Debug.Log(
                "[TrickOrTreat] Already completed: " +
                visitor.encounterID
            );
            return;
        }

        if (currentState != VisitState.Waiting &&
            currentState != VisitState.Completed)
        {
            Debug.LogWarning(
                "[TrickOrTreat] Another visitor is active."
            );
            return;
        }

        if (arrivalRoutine != null ||
            departureRoutine != null)
            return;

        currentEncounterIndex = index;
        currentState = VisitState.WaitingForClosedDoor;

        arrivalRoutine =
            StartCoroutine(BeginEncounter());
    }

    private IEnumerator BeginEncounter()
    {
        VisitorEncounter visitor = CurrentEncounter;

        if (visitor == null ||
            visitor.npc == null ||
            frontDoor == null)
        {
            Debug.LogWarning(
                "[TrickOrTreat] Missing NPC or front door."
            );

            currentState = VisitState.Waiting;
            arrivalRoutine = null;
            yield break;
        }

        yield return new WaitUntil(
            () => frontDoor != null &&
                  frontDoor.IsFullyClosed
        );

        if (originalPositions.ContainsKey(visitor.npc))
        {
            visitor.npc.transform.position =
                originalPositions[visitor.npc];

            visitor.npc.transform.rotation =
                originalRotations[visitor.npc];
        }

        visitor.npc.SetActive(true);

        PlayAnimation(
            visitor,
            visitor.idleAnimation
        );

        if (doorbellDelay > 0f)
            yield return new WaitForSeconds(doorbellDelay);

        if (doorbellSource != null &&
            doorbellSound != null)
        {
            doorbellSource.PlayOneShot(
                doorbellSound
            );
        }

        currentState = VisitState.AtDoor;
        arrivalRoutine = null;

        Debug.Log(
            "[TrickOrTreat] Visitor arrived: " +
            visitor.encounterID
        );
    }

    public void InteractWithNPC(GameObject clickedNPC)
    {
        VisitorEncounter visitor = CurrentEncounter;

        if (visitor == null ||
            clickedNPC != visitor.npc)
            return;

        if (frontDoor == null ||
            !frontDoor.IsFullyOpen)
            return;

        // First click: greeting dialogue.
        if (currentState == VisitState.AtDoor)
        {
            if (visitor.greetingFlowchart == null)
            {
                Debug.LogWarning(
                    "[TrickOrTreat] Greeting Flowchart missing."
                );
                return;
            }

            if (!HasStartBlock(visitor.greetingFlowchart))
                return;

            currentState = VisitState.Greeting;

            ExecuteFlowchart(
                visitor.greetingFlowchart
            );
        }

        // Second click: give candy.
        else if (currentState == VisitState.HoldingCandy)
        {
            if (pickupController == null ||
                candyItem == null)
                return;

            if (!pickupController.IsHoldingItem(candyItem))
            {
                Debug.LogWarning(
                    "[TrickOrTreat] Player is not holding candy!"
                );
                return;
            }

            if (visitor.farewellFlowchart == null ||
                !HasStartBlock(visitor.farewellFlowchart))
                return;

            // Remove candy from player's hand.
            pickupController.ConsumeHeldItem();

            Debug.Log(
                "[TrickOrTreat] Candy given to " +
                visitor.encounterID
            );

            // Start farewell dialogue immediately.
            StartFarewell(visitor);
        }
    }

    public void FinishGreeting()
    {
        if (currentState != VisitState.Greeting)
            return;

        VisitorEncounter visitor = CurrentEncounter;

        if (visitor == null)
            return;

        if (visitor.encounterType ==
            EncounterType.StandardCandy)
        {
            currentState = VisitState.WaitingForCandy;

            Debug.Log(
                "[TrickOrTreat] Greeting finished. " +
                "Player can collect candy."
            );
        }
        else
        {
            currentState = VisitState.Farewell;
        }
    }

    public void InteractWithBasket()
    {
        Debug.Log(
            "[TrickOrTreat] Basket clicked. State: " +
            currentState
        );

        if (currentState != VisitState.WaitingForCandy)
            return;

        // Basket placement must be completed.
        if (taskManager == null ||
            !taskManager.IsTaskCompleted(requiredTaskID))
        {
            Debug.LogWarning(
                "[TrickOrTreat] CandyTask is not completed!"
            );
            return;
        }

        if (pickupController == null ||
            candyItem == null)
        {
            Debug.LogWarning(
                "[TrickOrTreat] Pickup references missing!"
            );
            return;
        }

        // Prevent collecting candy while holding
        // another object.
        if (pickupController.IsHoldingItem())
        {
            Debug.LogWarning(
                "[TrickOrTreat] Player is already holding an item!"
            );
            return;
        }

        // Make candy available for pickup.
        candyItem.gameObject.SetActive(true);
        candyItem.canPickup = true;

        // Use the existing PickupController.
        pickupController.Pickup(candyItem);

        // Confirm pickup actually succeeded.
        if (!pickupController.IsHoldingItem(candyItem))
        {
            Debug.LogWarning(
                "[TrickOrTreat] Candy pickup failed!"
            );

            candyItem.gameObject.SetActive(false);
            return;
        }

        currentState = VisitState.HoldingCandy;

        Debug.Log(
            "[TrickOrTreat] Candy collected successfully!"
        );
    }

    private void StartFarewell(
        VisitorEncounter visitor)
    {
        if (visitor.farewellFlowchart == null ||
            !HasStartBlock(visitor.farewellFlowchart))
        {
            Debug.LogWarning(
                "[TrickOrTreat] Farewell Flowchart unavailable."
            );
            return;
        }

        currentState = VisitState.Farewell;

        ExecuteFlowchart(
            visitor.farewellFlowchart
        );
    }

    public void FinishFarewell()
    {
        if (currentState != VisitState.Farewell)
            return;

        if (departureRoutine != null)
            return;

        departureRoutine =
            StartCoroutine(LeaveEncounter());
    }

    public void BeginSpecialFarewell()
    {
        if (currentState != VisitState.Farewell)
            return;

        VisitorEncounter visitor = CurrentEncounter;

        if (visitor != null)
            StartFarewell(visitor);
    }

    private bool HasStartBlock(Flowchart flowchart)
    {
        if (flowchart == null ||
            flowchart.FindBlock("Start") == null)
        {
            Debug.LogWarning(
                "[TrickOrTreat] Flowchart missing Start Block!"
            );
            return false;
        }

        return true;
    }

    private void ExecuteFlowchart(Flowchart flowchart)
    {
        if (!HasStartBlock(flowchart))
            return;

        flowchart.ExecuteBlock("Start");
    }

    private IEnumerator LeaveEncounter()
    {
        currentState = VisitState.Leaving;

        VisitorEncounter visitor = CurrentEncounter;

        if (visitor == null)
        {
            departureRoutine = null;
            yield break;
        }

        if (visitor.departure != DepartureType.Stay)
        {
            string animationName =
                visitor.departure == DepartureType.Run
                ? visitor.runAnimation
                : visitor.walkAnimation;

            PlayAnimation(visitor, animationName);

            if (visitor.npc != null)
            {
                Transform npcTransform = visitor.npc.transform;

                if (exitWaypoints == null || exitWaypoints.Length == 0)
                {
                    Debug.LogWarning("[TrickOrTreat] Shared exit waypoints are not assigned.");
                }
                else
                {
                    foreach (Transform waypoint in exitWaypoints)
                    {
                        if (waypoint == null)
                            continue;

                        // Preserve NPC height on a level walkway.
                        Vector3 destination = waypoint.position;
                        destination.y = npcTransform.position.y;

                        while (Vector3.Distance(npcTransform.position, destination) > 0.005f)
                        {
                            Vector3 direction = destination - npcTransform.position;

                            if (direction.sqrMagnitude > 0.0001f)
                            {
                                Quaternion targetRotation = Quaternion.LookRotation(direction);
                                npcTransform.rotation = Quaternion.RotateTowards(
                                    npcTransform.rotation,
                                    targetRotation,
                                    Mathf.Max(0f, visitor.rotationSpeed) * 90f * Time.deltaTime
                                );
                            }

                            npcTransform.position = Vector3.MoveTowards(
                                npcTransform.position,
                                destination,
                                Mathf.Max(0.01f, visitor.movementSpeed) * Time.deltaTime
                            );

                            yield return null;
                        }

                        // Reach each waypoint exactly before continuing.
                        npcTransform.position = destination;
                    }
                }
            }
        }

        CompleteEncounter();
        departureRoutine = null;
    }

    private void CompleteEncounter()
    {
        VisitorEncounter visitor = CurrentEncounter;

        if (visitor == null)
            return;

        completedEncounters.Add(
            visitor.encounterID
        );

        bool startNext = visitor.autoStartNext;
        int nextIndex = currentEncounterIndex + 1;

        if (visitor.npc != null)
            visitor.npc.SetActive(false);

        currentState = VisitState.Completed;

        Debug.Log(
            "[TrickOrTreat] Encounter completed: " +
            visitor.encounterID
        );

        // Start Task 3 only after the Adult has finished leaving.
        if (visitor.encounterID == adultEncounterID)
        {
            if (tvRelaxTask != null)
            {
                tvRelaxTask.StartTask();
                Debug.Log("[TrickOrTreat] Adult left. Task 3 started.");
            }
            else
            {
                Debug.LogWarning("[TrickOrTreat] TVRelaxTask is not assigned.");
            }
        }

        if (startNext &&
            encounters != null &&
            nextIndex < encounters.Length)
        {
            StartCoroutine(
                StartNextEncounter(nextIndex)
            );
        }
    }

    private IEnumerator StartNextEncounter(int index)
    {
        yield return null;

        StartEncounterByIndex(index);
    }

    private void PlayAnimation(
        VisitorEncounter visitor,
        string animationName)
    {
        if (visitor.animator == null ||
            string.IsNullOrEmpty(animationName))
            return;

        visitor.animator.Play(animationName);
    }
}
