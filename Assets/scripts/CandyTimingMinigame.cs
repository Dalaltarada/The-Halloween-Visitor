
using UnityEngine;
using UnityEngine.UI;

public class CandyTimingMinigame : MonoBehaviour
{
    [Header("UI")]
    public GameObject minigamePanel;
    public RectTransform timingBar;
    public RectTransform movingMarker;
    public RectTransform targetZone;
    public Text progressText;
    public Text feedbackText;

    [Header("Game Settings")]
    [Min(1)]
    public int requiredSuccesses = 3;

    [Header("Difficulty Settings")]
    [Min(0.01f)]
    public float markerSpeed = 1.2f;

    [Min(0f)]
    public float speedIncreasePerSuccess = 0.4f;

    [Range(0.01f, 0.8f)]
    public float targetWidth = 0.2f;

    [Range(0f, 0.3f)]
    public float targetShrinkPerSuccess = 0.04f;

    [Range(0.01f, 0.8f)]
    public float minimumTargetWidth = 0.08f;

    [Header("Target Randomization")]
    [Tooltip("How far from the bar edges the target can appear.")]
    [Range(0f, 0.4f)]
    public float edgePadding = 0.05f;

    [Tooltip("Minimum distance between consecutive target centers.")]
    [Range(0f, 0.8f)]
    public float minimumTargetMovement = 0.15f;

    [Header("Player")]
    public UnityStandardAssets.Characters.FirstPerson
        .FirstPersonController playerController;

    [Header("Testing / Debug")]
    [Tooltip("Enable the number 2 shortcut to instantly complete the minigame. Disable before building.")]
    public bool enableTestingShortcut = true;

    [Tooltip("Basket task to complete when skipping the minigame without starting it.")]
    public CandyBasketTask testingBasketTask;

    private CandyBasketTask activeBasket;

    private bool isPlaying = false;
    private int successes = 0;

    private float markerPosition = 0f;
    private float targetPosition = 0.5f;
    private float direction = 1f;

    private float currentMarkerSpeed;
    private float currentTargetWidth;

    private bool waitingForSpaceRelease = false;

    public bool IsPlaying => isPlaying;

    private void Start()
    {
        if (minigamePanel != null)
            minigamePanel.SetActive(false);
    }

    public void Begin(CandyBasketTask basket)
    {
        if (isPlaying || basket == null)
            return;

        activeBasket = basket;
        successes = 0;

        currentMarkerSpeed = markerSpeed;
        currentTargetWidth = targetWidth;

        markerPosition = 0f;
        direction = 1f;

        waitingForSpaceRelease = true;

        isPlaying = true;

        RandomizeTarget(true);

        if (playerController != null)
        {
            playerController.SuppressJumpUntilReleased();
            playerController.disableMovement = true;
        }

        if (minigamePanel != null)
            minigamePanel.SetActive(true);

        SetFeedback("Press SPACE in the target zone!");

        UpdateProgress();
        UpdateVisuals();
    }

    private void Update()
    {
        // TESTING SHORTCUT:
        // Press 2 to instantly complete the minigame.
        if (enableTestingShortcut &&
            Input.GetKeyDown(KeyCode.Alpha2))
        {
            SkipMinigame();
            return;
        }

        if (!isPlaying)
            return;

        markerPosition +=
            direction * currentMarkerSpeed * Time.deltaTime;

        while (markerPosition > 1f || markerPosition < 0f)
        {
            if (markerPosition > 1f)
            {
                markerPosition = 2f - markerPosition;
                direction = -1f;
            }
            else if (markerPosition < 0f)
            {
                markerPosition = -markerPosition;
                direction = 1f;
            }
        }

        UpdateVisuals();

        if (waitingForSpaceRelease)
        {
            if (!Input.GetKey(KeyCode.Space))
                waitingForSpaceRelease = false;

            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
            CheckTiming();
    }

    private void SkipMinigame()
    {
        CandyBasketTask basketToComplete =
            activeBasket != null
                ? activeBasket
                : testingBasketTask;

        if (basketToComplete == null)
        {
            Debug.LogWarning(
                "[CandyTimingMinigame] Testing Basket Task not assigned!"
            );
            return;
        }

        Debug.Log(
            "[CandyTimingMinigame] DEBUG: Minigame skipped using key 2!"
        );

        activeBasket = basketToComplete;

        Finish();
    }

    private void CheckTiming()
    {
        float distance =
            Mathf.Abs(markerPosition - targetPosition);

        float halfWidth = currentTargetWidth / 2f;

        bool successful = distance <= halfWidth;

        if (successful)
        {
            successes++;

            SetFeedback("PERFECT!");

            if (successes >= requiredSuccesses)
            {
                Finish();
                return;
            }

            currentMarkerSpeed += speedIncreasePerSuccess;

            currentTargetWidth = Mathf.Max(
                minimumTargetWidth,
                currentTargetWidth - targetShrinkPerSuccess
            );
        }
        else
        {
            SetFeedback("MISSED! TRY AGAIN!");
        }

        RandomizeTarget(false);

        markerPosition = 0f;
        direction = 1f;

        UpdateProgress();
        UpdateVisuals();
    }

    private void RandomizeTarget(bool firstTarget)
    {
        float halfWidth = currentTargetWidth / 2f;

        float minPosition = Mathf.Clamp01(
            edgePadding + halfWidth
        );

        float maxPosition = Mathf.Clamp01(
            1f - edgePadding - halfWidth
        );

        if (minPosition > maxPosition)
        {
            targetPosition = 0.5f;
            return;
        }

        float previousPosition = targetPosition;
        float newPosition = previousPosition;

        for (int i = 0; i < 20; i++)
        {
            newPosition = Random.Range(
                minPosition,
                maxPosition
            );

            if (firstTarget ||
                Mathf.Abs(newPosition - previousPosition)
                    >= minimumTargetMovement)
            {
                break;
            }
        }

        if (!firstTarget &&
            Mathf.Abs(newPosition - previousPosition)
                < minimumTargetMovement)
        {
            float distanceToMin =
                Mathf.Abs(minPosition - previousPosition);

            float distanceToMax =
                Mathf.Abs(maxPosition - previousPosition);

            newPosition = distanceToMax > distanceToMin
                ? maxPosition
                : minPosition;
        }

        targetPosition = newPosition;
    }

    private void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text =
                successes + " / " + requiredSuccesses;
        }
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }

    private void UpdateVisuals()
    {
        if (timingBar == null)
            return;

        float width = timingBar.rect.width;

        if (movingMarker != null)
        {
            Vector2 position =
                movingMarker.anchoredPosition;

            position.x =
                (markerPosition - 0.5f) * width;

            movingMarker.anchoredPosition = position;
        }

        if (targetZone != null)
        {
            Vector2 size = targetZone.sizeDelta;

            size.x = width * currentTargetWidth;

            targetZone.sizeDelta = size;

            Vector2 position =
                targetZone.anchoredPosition;

            position.x =
                (targetPosition - 0.5f) * width;

            targetZone.anchoredPosition = position;
        }
    }

    private void Finish()
    {
        isPlaying = false;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (activeBasket != null)
            activeBasket.CompleteTask();

        if (playerController != null)
        {
            playerController.SuppressJumpUntilReleased();
            playerController.disableMovement = false;
        }

        activeBasket = null;
    }
}
