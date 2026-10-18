
using UnityEngine;
using Fungus;

public class TVRelaxTask : MonoBehaviour
{
    [Header("Task Settings")]
    public string taskID = "TVRelaxTask";
    public DialogueTaskManager taskManager;

    [Header("Dialogue")]
    public Flowchart taskStartFlowchart;
    public Flowchart taskCompleteFlowchart;

    [Header("Runtime State")]
    [SerializeField] private bool taskStarted;
    [SerializeField] private bool tvOn;
    [SerializeField] private bool taskCompleted;

    public bool TaskStarted => taskStarted;
    public bool TVOn => tvOn;
    public bool TaskCompleted => taskCompleted;

    // Call this after the Adult leaves.
    public void StartTask()
    {
        if (taskStarted)
            return;

        taskStarted = true;

        Debug.Log("[TVRelaxTask] Task 3 started.");

        if (taskStartFlowchart != null)
            taskStartFlowchart.ExecuteBlock("Start");
    }

    public void TurnOnTV()
    {
        if (!taskStarted || taskCompleted)
            return;

        tvOn = true;

        Debug.Log("[TVRelaxTask] TV turned on.");
    }

    public void RelaxOnCouch()
    {
        if (!taskStarted || taskCompleted)
            return;

        if (!tvOn)
        {
            Debug.Log("[TVRelaxTask] Turn on the TV first!");
            return;
        }

        CompleteTask();
    }

    private void CompleteTask()
    {
        taskCompleted = true;

        if (taskManager != null)
            taskManager.CompleteTask(taskID);

        Debug.Log("[TVRelaxTask] Task 3 completed!");

        if (taskCompleteFlowchart != null)
            taskCompleteFlowchart.ExecuteBlock("Start");
    }
}
