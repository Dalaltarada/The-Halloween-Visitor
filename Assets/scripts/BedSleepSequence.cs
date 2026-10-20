using UnityEngine;
using Fungus;

/// <summary>
/// Attach to the bed's collider GameObject. Uses the existing IInteractable
/// player interaction and DialogueTaskManager UI system.
/// </summary>
public class BedSleepSequence : MonoBehaviour, IInteractable
{
    [Header("Existing Tasks")]
    public DialogueTaskManager taskManager;
    public string lightsTaskID = "TurnOffLights";
    public string sleepTaskID = "GoToSleep";
    public string checkHouseTaskID = "CheckHouse";

    [Header("Fungus Sleep Sequence")]
    public Flowchart sleepFlowchart;
    public string sleepBlockName = "Start";

    [Header("Wall Writing")]
    [Tooltip("The wall writing GameObject. It will be hidden on Start.")]
    public GameObject wallWriting;

    private bool sleepTaskStarted;
    private bool sleepStarted;

    private void Start()
    {
        if (wallWriting != null)
            wallWriting.SetActive(false);
    }

    private void Update()
    {
        if (sleepTaskStarted || taskManager == null)
            return;

        if (taskManager.IsTaskCompleted(lightsTaskID))
        {
            sleepTaskStarted = true;
            taskManager.StartTask(sleepTaskID);
        }
    }

    // Called by the player's existing left-click interaction system.
    public void Interact()
    {
        if (sleepStarted || taskManager == null ||
            !taskManager.IsTaskStarted(sleepTaskID))
            return;

        if (sleepFlowchart == null ||
            sleepFlowchart.FindBlock(sleepBlockName) == null)
        {
            Debug.LogWarning("[BedSleepSequence] Assign a valid Fungus sleep Flowchart and block.");
            return;
        }

        sleepStarted = true;
        taskManager.CompleteTask(sleepTaskID);
        sleepFlowchart.ExecuteBlock(sleepBlockName);
    }

    // Fungus Invoke Method: place immediately AFTER Play Sound.
    public void RevealWallWriting()
    {
        if (!sleepStarted) return;
        if (wallWriting != null)
            wallWriting.SetActive(true);
        else
            Debug.LogWarning("[BedSleepSequence] Wall Writing is not assigned.");
    }

    // Fungus Invoke Method: place AFTER the final Say command.
    public void StartCheckHouseTask()
    {
        if (!sleepStarted || taskManager == null) return;
        taskManager.StartTask(checkHouseTaskID);
    }
}
