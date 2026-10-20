using System.Collections;
using UnityEngine;
using Fungus;

/// <summary>
/// Attach to the shower handle, which must have a Collider.
/// Start the task after the final Joker dialogue using a Fungus Invoke Method.
/// </summary>
public class ShowerTask : MonoBehaviour, IInteractable
{
    [Header("Existing Task Manager")]
    public DialogueTaskManager taskManager;
    public string taskID = "ShowerTask";

    [Header("Shower")]
    public ShowerWaterEffect showerWater;
    public AudioSource showerAudio;
    [Min(0.1f)] public float showerDuration = 5f;

    [Header("Fungus - Dark Screen")]
    public Flowchart darkScreenFlowchart;
    public string darkScreenBlock = "Start";

    [Header("Runtime State")]
    [SerializeField] private bool showerStarted;
    [SerializeField] private bool taskFinished;

    private void Start()
    {
        if (showerWater != null) showerWater.TurnOff();
        if (showerAudio != null)
        {
            showerAudio.playOnAwake = false;
            showerAudio.Stop();
        }
    }

    // Optional: call from Fungus after the final Say command.
    public void BeginTask()
    {
        if (taskManager != null) taskManager.StartTask(taskID);
    }

    public void Interact()
    {
        if (showerStarted || taskFinished) return;
        if (taskManager == null || !taskManager.IsTaskStarted(taskID)) return;
        if (showerWater == null)
        {
            Debug.LogWarning("[ShowerTask] Assign Shower Water in the Inspector.");
            return;
        }

        showerStarted = true;
        StartCoroutine(RunShower());
    }

    private IEnumerator RunShower()
    {
        showerWater.TurnOn();
        if (showerAudio != null)
        {
            showerAudio.Stop();
            showerAudio.Play();
        }

        yield return new WaitForSeconds(showerDuration);

        showerWater.TurnOff();
        if (showerAudio != null) showerAudio.Stop();

        taskFinished = true;
        if (taskManager != null) taskManager.CompleteTask(taskID);

        if (darkScreenFlowchart == null)
        {
            Debug.LogWarning("[ShowerTask] Dark Screen Flowchart is not assigned.");
            yield break;
        }
        if (darkScreenFlowchart.FindBlock(darkScreenBlock) == null)
        {
            Debug.LogWarning("[ShowerTask] Fungus block not found: " + darkScreenBlock);
            yield break;
        }
        darkScreenFlowchart.ExecuteBlock(darkScreenBlock);
    }
}
