using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class AfterPizzaTransition : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Your existing FirstPersonController.")]
    public FirstPersonController player;
    [Tooltip("Empty Transform marking where the player stands facing the front door.")]
    public Transform frontDoorStandingPoint;

    [Header("Candy Basket Visuals Only")]
    public GameObject fullCandyVisual;
    public GameObject lessCandyVisual;

    [Header("Object to Hide During Transition")]
    [Tooltip("Optional: Assign any GameObject to hide when BeginNextEncounter runs.")]
    public GameObject objectToHide;

    [Header("Next Visitor")]
    public TrickOrTreatManager trickOrTreatManager;
    [Tooltip("Must exactly match the encounterID in TrickOrTreatManager.")]
    public string nextEncounterID = "NextVisitor";

    private bool transitionStarted;

    private void Start()
    {
        // Leave the existing full-candy visual alone until the transition.
        if (lessCandyVisual != null)
            lessCandyVisual.SetActive(false);
    }

    // Call this once as the LAST command of TaskComplete's Start block.
    public void BeginNextEncounter()
    {
        if (transitionStarted) return;

        if (player == null || frontDoorStandingPoint == null || trickOrTreatManager == null)
        {
            Debug.LogWarning("[AfterPizzaTransition] Assign Player, Front Door Standing Point and Trick Or Treat Manager.");
            return;
        }

        transitionStarted = true;

        // Stand up and teleport using the FPC's own seated-state reset.
        player.StandUpAt(frontDoorStandingPoint);

        // These are decorative visuals, not the interactable candy PickupItem.
        if (fullCandyVisual != null) fullCandyVisual.SetActive(false);
        if (lessCandyVisual != null) lessCandyVisual.SetActive(true);

        // Hide the optional object during this transition.
        if (objectToHide != null) objectToHide.SetActive(false);

        // Existing manager handles waiting for a closed door, doorbell and NPC arrival.
        trickOrTreatManager.StartEncounter(nextEncounterID);
        Debug.Log("[AfterPizzaTransition] Transition complete. Requested encounter: " + nextEncounterID);
    }
}