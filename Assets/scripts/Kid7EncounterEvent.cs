using System.Collections;
using UnityEngine;
using Fungus;

/// <summary>
/// Connects the shower ending to Kid 7 and the final lights task.
/// Add this component to an active scene GameObject, not the NPC.
/// </summary>
public class Kid7EncounterEvent : MonoBehaviour
{
    [Header("Existing Systems")]
    public TrickOrTreatManager manager;
    public DialogueTaskManager taskManager;

    [Header("Kid 7")]
    public string encounterID = "Kid7";

    [Header("After Kid Leaves")]
    public Flowchart afterDepartureFlowchart;
    public string afterDepartureBlock = "Start";

    [Header("Next Task")]
    public string lightsTaskID = "TurnOffLights";

    private bool arrivalRequested;
    private bool departureRequested;
    private bool afterDepartureStarted;
    private bool lightsTaskStarted;

    // Fungus: invoke this AFTER the shower's final "Who could be?" Say.
    public void StartKid7()
    {
        if (arrivalRequested) return;
        if (manager == null)
        {
            Debug.LogWarning("[Kid7] Assign TrickOrTreatManager.");
            return;
        }
        arrivalRequested = true;
        manager.StartEncounter(encounterID);
    }

    // Fungus: invoke this AFTER Kid 7's final "Thank you so much" Say.
    // NoCandy encounters move directly from Greeting to Farewell.
    public void FinishKid7Conversation()
    {
        if (manager == null) return;
        if (manager.CurrentState != TrickOrTreatManager.VisitState.Greeting)
        {
            Debug.LogWarning("[Kid7] FinishKid7Conversation called outside Greeting state.");
            return;
        }
        manager.FinishGreeting();
        manager.BeginSpecialFarewell();
    }

    // Fungus: invoke at the END of Kid 7's farewell Flowchart.
    // The manager plays the walking animation and follows its shared exit path.
    public void SendKid7Away()
    {
        if (departureRequested || manager == null) return;
        if (manager.CurrentState != TrickOrTreatManager.VisitState.Farewell)
        {
            Debug.LogWarning("[Kid7] SendKid7Away called outside Farewell state.");
            return;
        }
        departureRequested = true;
        manager.FinishFarewell();
        StartCoroutine(WaitForDeparture());
    }

    private IEnumerator WaitForDeparture()
    {
        yield return new WaitUntil(() => manager != null &&
            manager.CurrentState == TrickOrTreatManager.VisitState.Completed);

        if (afterDepartureStarted) yield break;
        afterDepartureStarted = true;

        if (afterDepartureFlowchart == null ||
            afterDepartureFlowchart.FindBlock(afterDepartureBlock) == null)
        {
            Debug.LogWarning("[Kid7] Assign an after-departure Flowchart and valid block.");
            yield break;
        }
        afterDepartureFlowchart.ExecuteBlock(afterDepartureBlock);
    }

    // Fungus: invoke AFTER "I'll go to sleep now." is dismissed.
    public void StartLightsTask()
    {
        if (lightsTaskStarted || taskManager == null) return;
        lightsTaskStarted = true;
        taskManager.StartTask(lightsTaskID);
    }
}
