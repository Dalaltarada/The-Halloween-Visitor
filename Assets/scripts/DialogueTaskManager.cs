
using System.Collections.Generic;
using UnityEngine;
using Fungus;

public class DialogueTaskManager : MonoBehaviour
{
    [System.Serializable]
    public class DialogueTask
    {
        [Header("Identification")]
        public string taskID;

        [Header("Fungus Dialogue")]
        public Flowchart flowchart;
        public string blockName;

        [Header("Task UI")]
        public GameObject taskUI;

        [HideInInspector]
        public bool completed;

        [HideInInspector]
        public bool started;

        [HideInInspector]
        public bool dialogueWasPlaying;
    }

    [Header("Tasks")]
    public List<DialogueTask> tasks =
        new List<DialogueTask>();

    private void Start()
    {
        foreach (DialogueTask task in tasks)
        {
            if (task.taskUI != null)
                task.taskUI.SetActive(false);
        }
    }

    private void Update()
    {
        foreach (DialogueTask task in tasks)
        {
            if (task.completed || task.started)
                continue;

            if (task.flowchart == null ||
                string.IsNullOrEmpty(task.blockName))
                continue;

            Block block = task.flowchart.FindBlock(
                task.blockName
            );

            if (block == null)
                continue;

            bool isPlaying = block.IsExecuting();

            if (isPlaying)
            {
                task.dialogueWasPlaying = true;
            }
            else if (task.dialogueWasPlaying)
            {
                StartTask(task);
            }
        }
    }

    private void StartTask(DialogueTask task)
    {
        task.started = true;

        if (task.taskUI != null)
            task.taskUI.SetActive(true);

        Debug.Log("Task Started: " + task.taskID);
    }

    public void CompleteTask(string taskID)
    {
        foreach (DialogueTask task in tasks)
        {
            if (task.taskID != taskID)
                continue;

            if (!task.started || task.completed)
                return;

            task.completed = true;

            if (task.taskUI != null)
                task.taskUI.SetActive(false);

            Debug.Log("Task Completed: " + taskID);
            return;
        }

        Debug.LogWarning(
            "Task not found: " + taskID
        );
    }

    public bool IsTaskCompleted(string taskID)
    {
        foreach (DialogueTask task in tasks)
        {
            if (task.taskID == taskID)
                return task.completed;
        }

        return false;
    }
}
