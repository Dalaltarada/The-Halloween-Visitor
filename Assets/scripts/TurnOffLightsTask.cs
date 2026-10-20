using UnityEngine;
using UnityEngine.UI;

public class TurnOffLightsTask : MonoBehaviour
{
    [Header("Existing Task System")]
    public DialogueTaskManager taskManager;
    public string taskID = "TurnOffLights";

    [Header("Counter UI (Legacy Unity Text)")]
    public Text counterText;
    public GameObject counterRoot;

    [Header("Required Interactions")]
    [Min(0)] public int requiredSwitches = 4;
    [Min(0)] public int requiredPumpkins = 6;

    private int switchesOff;
    private int pumpkinsOff;
    private bool finished;

    public bool IsActive
    {
        get
        {
            if (taskManager == null || taskManager.tasks == null) return false;
            foreach (DialogueTaskManager.DialogueTask task in taskManager.tasks)
                if (task.taskID == taskID) return task.started && !task.completed;
            return false;
        }
    }

    private void Start()
    {
        RefreshCounter();
        if (counterRoot != null) counterRoot.SetActive(false);
    }

    private void Update()
    {
        if (counterRoot != null && counterRoot.activeSelf != IsActive)
            counterRoot.SetActive(IsActive);
    }

    public bool RegisterLight(bool isPumpkin)
    {
        if (!IsActive || finished) return false;
        if (isPumpkin)
        {
            if (pumpkinsOff >= requiredPumpkins) return false;
            pumpkinsOff++;
        }
        else
        {
            if (switchesOff >= requiredSwitches) return false;
            switchesOff++;
        }

        RefreshCounter();
        if (switchesOff >= requiredSwitches && pumpkinsOff >= requiredPumpkins)
        {
            finished = true;
            taskManager.CompleteTask(taskID);
            if (counterRoot != null) counterRoot.SetActive(false);
        }
        return true;
    }

    private void RefreshCounter()
    {
        if (counterText != null)
            counterText.text = "Lights off: " + (switchesOff + pumpkinsOff) + "/" + (requiredSwitches + requiredPumpkins)
                + "\nSwitches: " + switchesOff + "/" + requiredSwitches
                + "  Pumpkins: " + pumpkinsOff + "/" + requiredPumpkins;
    }
}
