
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Scene to load when the game finishes.")]
    public int mainMenuSceneIndex = 0;

    [Header("Testing")]
    [Tooltip("Enable number keys 1-7 to switch scenes.")]
    public bool enableSceneTesting = true;

    private void Update()
    {
        if (!enableSceneTesting) return;

        // Number keys for testing scenes.
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

    // Load a scene using its Build Settings index.
    public void LoadScene(int sceneIndex)
    {
        if (sceneIndex < 0 ||
            sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                "[GameSceneManager] Invalid scene index: " + sceneIndex);
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }

    // Load the next scene in Build Settings.
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            LoadScene(nextIndex);
        }
        else
        {
            // Last scene finished. Return to main menu.
            ReturnToMainMenu();
        }
    }

    // Return to the main menu.
    public void ReturnToMainMenu()
    {
        LoadScene(mainMenuSceneIndex);
    }

    // Restart the current scene.
    public void RestartScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        LoadScene(currentIndex);
    }

    // Start game from the main menu.
    public void StartGame()
    {
        LoadScene(1);
    }

    // Optional: Quit game.
    public void QuitGame()
    {
        Debug.Log("[GameSceneManager] Quitting game...");
        Application.Quit();
    }
}
