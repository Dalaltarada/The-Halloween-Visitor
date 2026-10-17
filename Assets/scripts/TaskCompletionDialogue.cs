
using UnityEngine;
using Fungus;

public class TaskCompletionDialogue : MonoBehaviour
{
    [Header("Task Manager")]
    [Tooltip("Drag the GameObject containing DialogueTaskManager.")]
    public DialogueTaskManager taskManager;

    [Header("Task Completion Dialogues")]
    public TaskDialogue[] taskDialogues;

    [System.Serializable]
    public class TaskDialogue
    {
        [Tooltip("Must match the Task ID in DialogueTaskManager.")]
        public string taskID;

        [Tooltip("The Fungus Flowchart containing the dialogue.")]
        public Flowchart flowchart;

        [Tooltip("The Block to play when the task is completed.")]
        public string blockName;

        [HideInInspector]
        public bool dialoguePlayed;
    }

    private void Update()
    {
        if (taskManager == null)
            return;

        foreach (TaskDialogue task in taskDialogues)
        {
            // Prevent playing the dialogue multiple times.
            if (task.dialoguePlayed)
                continue;

            if (string.IsNullOrEmpty(task.taskID) ||
                task.flowchart == null ||
                string.IsNullOrEmpty(task.blockName))
                continue;

            // Check if the task has been completed.
            if (taskManager.IsTaskCompleted(task.taskID))
            {
                PlayDialogue(task);
            }
        }
    }

    private void PlayDialogue(TaskDialogue task)
    {
        // Mark as played before starting the dialogue.
        task.dialoguePlayed = true;

        task.flowchart.ExecuteBlock(task.blockName);

        Debug.Log(
            "[TaskCompletionDialogue] Playing dialogue for task: "
            + task.taskID
        );
    }
}
