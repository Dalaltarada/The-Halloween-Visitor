using UnityEngine;
using UnityEngine.UI;
using Fungus;
using UnityStandardAssets.Characters.FirstPerson;

public class TVRelaxTask : MonoBehaviour
{
    [Header("Task")]
    public string taskID = "TVRelaxTask";
    public DialogueTaskManager taskManager;
    public ObjectPlacementTask pizzaPlacement;

    [Header("Flowcharts")]
    public Flowchart taskStartFlowchart;
    public Flowchart taskCompleteFlowchart;

    [Header("Task UI")]
    public GameObject taskUI;
    public GameObject eatingUI;
    [Tooltip("The foreground fill Image (Image Type: Filled, Fill Method: Horizontal).")]
    public Image eatingFillImage;

    [Header("Seating")]
    public FirstPersonController player;
    public Transform couchSeatPoint;

    [Header("Pizza Slice")]
    public GameObject pizzaSlice;
    public Transform sliceHoldPoint;
    [Tooltip("Adjust where the pizza slice sits relative to Slice Hold Point.")]
    public Vector3 sliceLocalPosition = Vector3.zero;
    [Tooltip("Adjust the slice rotation in degrees.")]
    public Vector3 sliceLocalRotation = Vector3.zero;

    [Header("Eating")]
    [Min(0.1f)] public float eatDuration = 3f;
    public float biteBobAmount = 0.025f;
    public float biteBobSpeed = 12f;
    public AudioSource eatingAudio;

    [Header("Runtime State")]
    [SerializeField] private bool taskStarted;
    [SerializeField] private bool tvOn;
    [SerializeField] private bool seated;
    [SerializeField] private bool holdingSlice;
    [SerializeField] private bool sliceAlreadyTaken;
    [SerializeField] private bool taskCompleted;
    [SerializeField, Range(0f, 1f)] private float eatProgress;

    public bool TaskStarted => taskStarted;
    public bool TVOn => tvOn;
    public bool IsSeated => seated;
    public bool TaskCompleted => taskCompleted;
    public bool PizzaPlaced => pizzaPlacement != null && pizzaPlacement.IsCompleted;
    public bool CanTakePizzaSlice => taskStarted && PizzaPlaced && tvOn && seated && !sliceAlreadyTaken && !holdingSlice && !taskCompleted;

    private void Start()
    {
        if (taskUI != null) taskUI.SetActive(false);
        if (eatingUI != null) eatingUI.SetActive(false);
        if (pizzaSlice != null) pizzaSlice.SetActive(false);
        UpdateEatingBar();
    }

    public void StartTask()
    {
        if (taskStarted || taskCompleted) return;
        taskStarted = true;
        if (taskStartFlowchart != null && taskStartFlowchart.FindBlock("Start") != null)
            taskStartFlowchart.ExecuteBlock("Start");
        else
            ShowTaskUI();
    }

    // Call at the END of the task-start Fungus dialogue.
    public void ShowTaskUI()
    {
        if (!taskStarted || taskCompleted || seated) return;
        if (taskUI != null) taskUI.SetActive(true);
        if (pizzaPlacement != null) pizzaPlacement.StartTask();
    }

    public void TurnOnTV()
    {
        if (!taskStarted || !PizzaPlaced || taskCompleted) return;
        tvOn = true;
    }

    public void RelaxOnCouch()
    {
        if (!taskStarted || !PizzaPlaced || !tvOn || seated || taskCompleted) return;
        if (player == null || couchSeatPoint == null)
        {
            Debug.LogWarning("[TVRelaxTask] Assign player and couchSeatPoint!");
            return;
        }
        player.SitAt(couchSeatPoint);
        seated = true;
        if (taskUI != null) taskUI.SetActive(false);
    }

    // Returns true only when a slice was actually taken.
    public bool TakePizzaSlice()
    {
        if (!CanTakePizzaSlice) return false;
        if (pizzaSlice == null || sliceHoldPoint == null)
        {
            Debug.LogWarning("[TVRelaxTask] Assign pizzaSlice and sliceHoldPoint!");
            return false;
        }

        pizzaSlice.transform.SetParent(sliceHoldPoint, false);
        ApplySlicePose();
        pizzaSlice.SetActive(true);
        sliceAlreadyTaken = true;
        holdingSlice = true;
        eatProgress = 0f;
        UpdateEatingBar();
        if (eatingUI != null) eatingUI.SetActive(true);
        return true;
    }

    private void Update()
    {
        if (!holdingSlice || taskCompleted) return;

        if (Input.GetKey(KeyCode.E))
        {
            eatProgress = Mathf.Clamp01(eatProgress + Time.deltaTime / Mathf.Max(0.1f, eatDuration));
            if (pizzaSlice != null)
                pizzaSlice.transform.localPosition = sliceLocalPosition +
                    Vector3.up * (Mathf.Sin(Time.time * biteBobSpeed) * biteBobAmount);
            if (eatingAudio != null && !eatingAudio.isPlaying) eatingAudio.Play();
        }
        else
        {
            ApplySlicePose();
            if (eatingAudio != null && eatingAudio.isPlaying) eatingAudio.Stop();
        }

        UpdateEatingBar();
        if (eatProgress >= 1f) CompleteTask();
    }

    private void ApplySlicePose()
    {
        if (pizzaSlice == null) return;
        pizzaSlice.transform.localPosition = sliceLocalPosition;
        pizzaSlice.transform.localRotation = Quaternion.Euler(sliceLocalRotation);
    }

    private void UpdateEatingBar()
    {
        if (eatingFillImage == null) return;
        if (eatingFillImage.type != Image.Type.Filled)
            eatingFillImage.type = Image.Type.Filled;
        eatingFillImage.fillMethod = Image.FillMethod.Horizontal;
        eatingFillImage.fillOrigin = 0;
        eatingFillImage.fillAmount = Mathf.Clamp01(eatProgress);
    }

    private void CompleteTask()
    {
        if (taskCompleted) return;
        taskCompleted = true;
        holdingSlice = false;
        if (eatingAudio != null) eatingAudio.Stop();
        if (pizzaSlice != null) pizzaSlice.SetActive(false);
        if (eatingUI != null) eatingUI.SetActive(false);
        if (taskUI != null) taskUI.SetActive(false);
        if (taskManager != null) taskManager.CompleteTask(taskID);
        // Hide again in case task manager changed this same UI synchronously.
        if (taskUI != null) taskUI.SetActive(false);
        if (taskCompleteFlowchart != null && taskCompleteFlowchart.FindBlock("Start") != null)
            taskCompleteFlowchart.ExecuteBlock("Start");
    }
}
