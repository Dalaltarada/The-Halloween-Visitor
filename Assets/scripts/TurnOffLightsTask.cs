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
    [Min(0)] public int requiredTVs = 1;

    private int switchesOff;
    private int pumpkinsOff;
    private int tvsOff;
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

    // Existing switches and pumpkins keep using this method.
    public bool RegisterLight(bool isPumpkin)
    {
        return RegisterLightKind(isPumpkin ? LightCategory.Pumpkin : LightCategory.WallSwitch);
    }

    public enum LightCategory { WallSwitch, Pumpkin, TV }

    public bool RegisterTV()
    {
        return RegisterLightKind(LightCategory.TV);
    }

    private bool RegisterLightKind(LightCategory category)
    {
        if (!IsActive || finished) return false;
        switch (category)
        {
            case LightCategory.WallSwitch:
                if (switchesOff >= requiredSwitches) return false;
                switchesOff++;
                break;
            case LightCategory.Pumpkin:
                if (pumpkinsOff >= requiredPumpkins) return false;
                pumpkinsOff++;
                break;
            case LightCategory.TV:
                if (tvsOff >= requiredTVs) return false;
                tvsOff++;
                break;
        }

        RefreshCounter();
        if (switchesOff >= requiredSwitches &&
            pumpkinsOff >= requiredPumpkins && tvsOff >= requiredTVs)
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
            counterText.text = "Lights off: " + (switchesOff + pumpkinsOff + tvsOff) + "/" + (requiredSwitches + requiredPumpkins + requiredTVs)
                + "\nSwitches: " + switchesOff + "/" + requiredSwitches
                + "  Pumpkins: " + pumpkinsOff + "/" + requiredPumpkins
                + "  TV: " + tvsOff + "/" + requiredTVs;
    }
}
