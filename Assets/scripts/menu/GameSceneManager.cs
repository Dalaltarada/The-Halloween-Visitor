
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    // The one persistent manager used by all scenes.
    public static GameSceneManager Instance { get; private set; }

    [Header("Scene Settings")]
    [Tooltip("Scene to load when the game finishes.")]
    public int mainMenuSceneIndex = 0;

    [Header("Fade Settings")]
    [Min(0f)]
    public float fadeOutDuration = 1.2f;

    [Min(0f)]
    public float fadeInDuration = 1.2f;

    [Tooltip("How long to remain black before loading.")]
    [Min(0f)]
    public float blackScreenDelay = 0.3f;

    [Header("Testing")]
    [Tooltip("Enable number keys 1-7 to switch scenes.")]
    public bool enableSceneTesting = true;

    private Canvas fadeCanvas;
    private Image fadeImage;
    private bool isTransitioning;

    private void Awake()
    {
        // Destroy duplicate managers.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Keep this manager alive between scenes.
        DontDestroyOnLoad(gameObject);

        // Create the black fade screen automatically.
        CreateFadeCanvas();

        // Fade into the first scene.
        StartCoroutine(FadeIn());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!enableSceneTesting || isTransitioning)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            LoadScene(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            LoadScene(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            LoadScene(2);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            LoadScene(3);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            LoadScene(4);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            LoadScene(5);

        if (Input.GetKeyDown(KeyCode.Alpha7))
            LoadScene(6);
    }

    // Automatically create a full-screen black overlay.
    private void CreateFadeCanvas()
    {
        GameObject canvasObject =
            new GameObject("Scene Fade Canvas");

        canvasObject.transform.SetParent(transform, false);

        fadeCanvas = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;

        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920, 1080);

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject =
            new GameObject("Black Fade");

        imageObject.transform.SetParent(
            canvasObject.transform, false);

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;

        RectTransform rect =
            imageObject.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Fade from black into the current scene.
    private IEnumerator FadeIn()
    {
        isTransitioning = true;

        SetFadeAlpha(1f);
        fadeImage.raycastTarget = true;

        yield return Fade(1f, 0f, fadeInDuration);

        fadeImage.raycastTarget = false;
        isTransitioning = false;
    }

    // Fade out, load the next scene, then fade in.
    private IEnumerator TransitionToScene(int sceneIndex)
    {
        isTransitioning = true;
        fadeImage.raycastTarget = true;

        // Fade current scene to black.
        yield return Fade(
            fadeImage.color.a,
            1f,
            fadeOutDuration
        );

        // Hold the black screen briefly.
        if (blackScreenDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                blackScreenDelay
            );
        }

        // Reset time in case the game was paused.
        Time.timeScale = 1f;

        // Load the requested scene.
        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(sceneIndex);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // Wait one frame before fading in.
        yield return null;

        // Fade from black into the new scene.
        yield return Fade(1f, 0f, fadeInDuration);

        fadeImage.raycastTarget = false;
        isTransitioning = false;
    }

    // Smooth fade animation.
    private IEnumerator Fade(
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (duration <= 0f)
        {
            SetFadeAlpha(endAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            SetFadeAlpha(
                Mathf.Lerp(startAlpha, endAlpha, t)
            );

            yield return null;
        }

        SetFadeAlpha(endAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    // Load a scene using its Build Settings index.
    public void LoadScene(int sceneIndex)
    {
        // Forward calls from duplicate manager references
        // to the persistent manager.
        if (Instance != null && Instance != this)
        {
            Instance.LoadScene(sceneIndex);
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "[GameSceneManager] Manager is inactive. " +
                "Cannot start scene transition."
            );
            return;
        }

        if (isTransitioning)
            return;

        if (sceneIndex < 0 ||
            sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                "[GameSceneManager] Invalid scene index: " +
                sceneIndex
            );
            return;
        }

        StartCoroutine(TransitionToScene(sceneIndex));
    }

    // Load the next scene in Build Settings.
    public void LoadNextScene()
    {
        int currentIndex =
            SceneManager.GetActiveScene().buildIndex;

        int nextIndex = currentIndex + 1;

        if (nextIndex <
            SceneManager.sceneCountInBuildSettings)
        {
            LoadScene(nextIndex);
        }
        else
        {
            ReturnToMainMenu();
        }
    }

    // Return to main menu.
    public void ReturnToMainMenu()
    {
        LoadScene(mainMenuSceneIndex);
    }

    // Restart current scene.
    public void RestartScene()
    {
        int currentIndex =
            SceneManager.GetActiveScene().buildIndex;

        LoadScene(currentIndex);
    }

    // Start game from main menu.
    public void StartGame()
    {
        LoadScene(1);
    }

    // Load the post-game cemetery menu.
    public void LoadPostGameMenu()
    {
        LoadScene(6);
    }

    // Quit game.
    public void QuitGame()
    {
        Debug.Log("[GameSceneManager] Quitting game...");
        Application.Quit();
    }
}
