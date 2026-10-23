
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
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
        // Keep this manager alive between scenes.
        DontDestroyOnLoad(gameObject);

        // Prevent duplicate managers when changing scenes.
        GameSceneManager[] managers =
            FindObjectsOfType<GameSceneManager>();

        foreach (GameSceneManager manager in managers)
        {
            if (manager != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        CreateFadeCanvas();
        StartCoroutine(FadeIn());
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

    // Create a full-screen black overlay automatically.
    private void CreateFadeCanvas()
    {
        GameObject canvasObject =
            new GameObject("Scene Fade Canvas");

        canvasObject.transform.SetParent(transform);

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

    // Fade from black into the scene.
    private IEnumerator FadeIn()
    {
        isTransitioning = true;

        yield return Fade(1f, 0f, fadeInDuration);

        fadeImage.raycastTarget = false;

        isTransitioning = false;
    }

    // Fade from one scene to another.
    private IEnumerator TransitionToScene(int sceneIndex)
    {
        isTransitioning = true;

        fadeImage.raycastTarget = true;

        // Fade screen to black.
        yield return Fade(
            fadeImage.color.a,
            1f,
            fadeOutDuration);

        // Remain black briefly.
        if (blackScreenDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                blackScreenDelay);
        }

        // Reset time in case the game was paused.
        Time.timeScale = 1f;

        // Load the new scene.
        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(sceneIndex);

        while (!loadOperation.isDone)
            yield return null;

        // Keep the screen black for one frame.
        yield return null;

        // Fade into the new scene.
        yield return Fade(1f, 0f, fadeInDuration);

        fadeImage.raycastTarget = false;

        isTransitioning = false;
    }

    // Smooth alpha animation.
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

            float t = Mathf.Clamp01(
                elapsed / duration);

            // Smooth fade.
            t = Mathf.SmoothStep(0f, 1f, t);

            SetFadeAlpha(
                Mathf.Lerp(startAlpha, endAlpha, t));

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
        if (isTransitioning)
            return;

        if (sceneIndex < 0 ||
            sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                "[GameSceneManager] Invalid scene index: " +
                sceneIndex);

            return;
        }

        StartCoroutine(
            TransitionToScene(sceneIndex));
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
