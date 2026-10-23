
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Characters.FirstPerson;

public class FinalSceneSequence : MonoBehaviour
{
    [Header("Player Lock")]
    public FirstPersonController playerController;

    [Header("Doorbell")]
    public AudioSource doorbellAudio;
    [Min(0f)] public float initialDelay = 1.5f;
    [Min(0f)] public float delayAfterDoorbell = 0.5f;

    [Header("Ending Credits Music")]
    public AudioSource creditsMusic;
    [Range(0f, 1f)] public float musicVolume = 1f;
    public bool loopMusic = true;

    [Header("Existing Door System")]
    public DoorInteractable door;

    [Header("Credits Timing")]
    [Min(0f)] public float delayBeforeCredits = 2f;
    [Min(0f)] public float creditsDuration = 35f;
    [Min(0f)] public float holdAtEnd = 2f;

    [Header("Credits Appearance")]
    public Font creditsFont;
    [Range(10, 100)] public int fontSize = 30;
    public Color textColor = Color.white;
    public Color backgroundColor = Color.black;

    [Header("Credits Scrolling")]
    [Min(0f)] public float scrollSpeed = 90f;
    public bool useDurationInsteadOfSpeed = false;

    [Header("Credits Starting Position")]
    [Tooltip("0 = bottom of screen. Negative = below screen. Positive = higher up.")]
    public float creditsStartOffset = 0f;

    [Header("Credits Text")]
    [TextArea(15, 40)]
    public string creditsText =
        "THANK YOU FOR PLAYING!\n" +
        "\n" +
        "THE HALLOWEEN VISITOR\n" +
        "\n" +
        "Created for HORRORJAM 2026\n" +
        "Hosted by Extar\n" +
        "\n" +
        "────────────────────\n" +
        "\n" +
        "A GAME BY\n" +
        "Ghost Mushroom Studio\n" +
        "\n" +
        "DEVELOPMENT & GAME DESIGN\n" +
        "Dalal\n" +
        "\n" +
        "ENVIRONMENT DESIGN & MUSIC\n" +
        "Fuiccix\n" +
        "\n" +
        "PLAYTESTERS\n" +
        "Ahmed · Farah · Raven\n" +
        "\n" +
        "SPECIAL THANKS\n" +
        "Talal · Khalifa · Anoodi\n" +
        "Maxwell - Senior Cat Operations Manager\n" +
        "\n" +
        "────────────────────\n" +
        "\n" +
        "ASSET CREDITS\n" +
        "\n" +
        "Sound Effects: pixabay.com\n" +
        "3D Models: sketchfab.com\n" +
        "\n" +
        "────────────────────\n" +
        "\n" +
        "Thank you for being part of our\n" +
        "spooky little adventure!\n" +
        "\n" +
        "More games are coming soon!\n" +
        "\n" +
        "© 2026 Ghost Mushroom Studio";

    [Header("Next Scene")]
    public bool loadNextScene = true;

    [Tooltip("If enabled, loads the next scene in Build Settings.")]
    public bool useNextBuildIndex = true;

    public string nextSceneName = "";

    private Canvas creditsCanvas;
    private RectTransform creditsRect;
    private RectTransform textRect;
    private Text creditsUIText;
    private bool sequenceStarted;

    private void Start()
    {
        // Lock player movement and mouse look
        if (playerController != null)
        {
            playerController.scriptedMovement = true;
            playerController.enabled = false;
        }
        else
        {
            Debug.LogWarning(
                "[FinalSceneSequence] Player Controller is not assigned."
            );
        }

        // Prevent music from playing before the doorbell
        if (creditsMusic != null)
        {
            creditsMusic.Stop();
            creditsMusic.playOnAwake = false;
        }

        if (!sequenceStarted)
        {
            sequenceStarted = true;
            StartCoroutine(PlayFinalSequence());
        }
    }

    private IEnumerator PlayFinalSequence()
    {
        // Initial delay
        if (initialDelay > 0f)
            yield return new WaitForSecondsRealtime(initialDelay);

        // Play doorbell SFX
        if (doorbellAudio != null && doorbellAudio.clip != null)
        {
            doorbellAudio.Stop();
            doorbellAudio.Play();

            yield return new WaitForSecondsRealtime(
                doorbellAudio.clip.length
            );
        }
        else
        {
            Debug.LogWarning(
                "[FinalSceneSequence] Assign a doorbell AudioSource with an AudioClip."
            );
        }

        // Start ending music immediately after doorbell
        if (creditsMusic != null)
        {
            creditsMusic.volume = musicVolume;
            creditsMusic.loop = loopMusic;
            creditsMusic.Play();
        }
        else
        {
            Debug.LogWarning(
                "[FinalSceneSequence] Credits Music is not assigned."
            );
        }

        // Delay after doorbell
        if (delayAfterDoorbell > 0f)
            yield return new WaitForSecondsRealtime(delayAfterDoorbell);

        // Open door
        if (door != null)
        {
            door.OpenFromScript();
        }
        else
        {
            Debug.LogWarning(
                "[FinalSceneSequence] DoorInteractable is not assigned."
            );
        }

        // Wait before credits
        if (delayBeforeCredits > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeCredits);

        // Create credits UI
        CreateCreditsUI();

        // Start scrolling credits
        yield return StartCoroutine(ScrollCredits());

        // Hold after credits
        if (holdAtEnd > 0f)
            yield return new WaitForSecondsRealtime(holdAtEnd);

        // Load next scene
        if (loadNextScene)
            LoadNextScene();
    }

    private void CreateCreditsUI()
    {
        // Create Canvas
        GameObject canvasObject = new GameObject(
            "Final Credits Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        creditsCanvas = canvasObject.GetComponent<Canvas>();
        creditsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        creditsCanvas.sortingOrder = 9998;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Background
        GameObject backgroundObject = new GameObject(
            "Credits Background",
            typeof(RectTransform),
            typeof(Image)
        );

        backgroundObject.transform.SetParent(
            canvasObject.transform, false
        );

        RectTransform backgroundRect =
            backgroundObject.GetComponent<RectTransform>();

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage =
            backgroundObject.GetComponent<Image>();

        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = true;

        // Credits viewport
        GameObject viewportObject = new GameObject(
            "Credits Viewport",
            typeof(RectTransform),
            typeof(RectMask2D)
        );

        viewportObject.transform.SetParent(
            canvasObject.transform, false
        );

        creditsRect =
            viewportObject.GetComponent<RectTransform>();

        creditsRect.anchorMin = Vector2.zero;
        creditsRect.anchorMax = Vector2.one;
        creditsRect.offsetMin = Vector2.zero;
        creditsRect.offsetMax = Vector2.zero;

        // Credits text
        GameObject textObject = new GameObject(
            "Scrolling Credits Text",
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(
            viewportObject.transform, false
        );

        textRect = textObject.GetComponent<RectTransform>();

        // Anchor text to bottom center
        textRect.anchorMin = new Vector2(0.1f, 0f);
        textRect.anchorMax = new Vector2(0.9f, 0f);

        // Top pivot ensures the FIRST line enters first
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = Vector2.zero;

        creditsUIText = textObject.GetComponent<Text>();

        creditsUIText.font = creditsFont != null
            ? creditsFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        creditsUIText.fontSize = fontSize;
        creditsUIText.color = textColor;

        // Top alignment keeps the first line at the top
        creditsUIText.alignment = TextAnchor.UpperCenter;

        creditsUIText.horizontalOverflow =
            HorizontalWrapMode.Wrap;

        creditsUIText.verticalOverflow =
            VerticalWrapMode.Overflow;

        creditsUIText.supportRichText = true;
        creditsUIText.raycastTarget = false;
        creditsUIText.lineSpacing = 1.3f;

        // Apply editable credits text
        creditsUIText.text = creditsText;

        Canvas.ForceUpdateCanvases();

        // Calculate text height
        float width = creditsRect.rect.width * 0.8f;

        float preferredHeight =
            creditsUIText.cachedTextGeneratorForLayout.GetPreferredHeight(
                creditsText,
                creditsUIText.GetGenerationSettings(
                    new Vector2(width, 10000f)
                )
            );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            preferredHeight + 100f
        );

        // Start at adjustable bottom position
        textRect.anchoredPosition = new Vector2(
            0f,
            creditsStartOffset
        );
    }

    private IEnumerator ScrollCredits()
    {
        if (creditsRect == null || textRect == null)
            yield break;

        Canvas.ForceUpdateCanvases();

        float viewportHeight = creditsRect.rect.height;
        float textHeight = textRect.rect.height;

        // Adjustable starting position
        float startY = creditsStartOffset;

        // Scroll until entire text is above the screen
        float endY = viewportHeight + textHeight;

        float totalDistance = endY - startY;

        float duration;

        if (useDurationInsteadOfSpeed)
        {
            duration = Mathf.Max(0.01f, creditsDuration);
        }
        else
        {
            duration = totalDistance /
                       Mathf.Max(1f, scrollSpeed);
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float y = Mathf.Lerp(startY, endY, t);

            textRect.anchoredPosition =
                new Vector2(0f, y);

            yield return null;
        }

        textRect.anchoredPosition =
            new Vector2(0f, endY);
    }

    private void LoadNextScene()
    {
        if (useNextBuildIndex)
        {
            int currentIndex =
                SceneManager.GetActiveScene().buildIndex;

            int nextIndex = currentIndex + 1;

            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                if (GameSceneManager.Instance != null)
                    GameSceneManager.Instance.LoadScene(nextIndex);
                else
                    SceneManager.LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning(
                    "[FinalSceneSequence] No next scene in Build Settings."
                );
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning(
                    "[FinalSceneSequence] Next Scene Name is empty."
                );

                return;
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
