
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class PauseMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Player Controller")]
    public FirstPersonController playerController;

    [Header("Main Menu")]
    public string mainMenuSceneName = "main menu";

    [Header("Sound - All Audio")]
    public Slider volumeSlider;

    [Header("Screen")]
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;

    [Header("Mouse Sensitivity")]
    public Slider sensitivitySlider;

    [Range(0.1f, 10f)]
    public float defaultSensitivity = 2f;

    private readonly List<Resolution> availableResolutions =
        new List<Resolution>();

    private bool isPaused;
    private bool settingsOpen;
    private bool previousMovementLock;

    private void Awake()
    {
        // Ensure the game starts unpaused.
        Time.timeScale = 1f;

        isPaused = false;
        settingsOpen = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Find player automatically if not assigned.
        if (playerController == null)
            playerController = FindObjectOfType<FirstPersonController>();

        // Load saved volume.
        AudioListener.volume = PlayerPrefs.GetFloat(
            MainMenuButtons.VolumeKey, 1f);

        // Load saved fullscreen setting.
        bool fullscreen = PlayerPrefs.GetInt(
            MainMenuButtons.FullscreenKey,
            Screen.fullScreen ? 1 : 0) == 1;

        Screen.fullScreen = fullscreen;

        SetupResolutions();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
            {
                PauseGame();
            }
            else if (settingsOpen)
            {
                BackToPauseMenu();
            }
            else
            {
                ResumeGame();
            }
        }
    }

    // Pause gameplay and lock the FPC.
    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;
        settingsOpen = false;

        // Save and lock the player's movement state.
        if (playerController != null)
        {
            previousMovementLock = playerController.disableMovement;
            playerController.disableMovement = true;
        }

        // Freeze gameplay.
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Unlock and show the cursor.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[PauseMenu] Game paused.");
    }

    // Resume gameplay and restore the FPC.
    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        settingsOpen = false;

        // Resume gameplay.
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Restore previous player movement state.
        if (playerController != null)
            playerController.disableMovement = previousMovementLock;

        // Lock and hide the cursor.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[PauseMenu] Game resumed.");
    }

    // Open settings while keeping the game paused.
    public void OpenSettings()
    {
        if (!isPaused) return;

        settingsOpen = true;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        RefreshSettingsUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Return from settings to the pause menu.
    public void BackToPauseMenu()
    {
        if (!isPaused) return;

        settingsOpen = false;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Refresh saved settings.
    private void RefreshSettingsUI()
    {
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;

            volumeSlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    MainMenuButtons.VolumeKey, 1f));
        }

        if (fullscreenToggle != null)
        {
            bool fullscreen = PlayerPrefs.GetInt(
                MainMenuButtons.FullscreenKey,
                Screen.fullScreen ? 1 : 0) == 1;

            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 10f;

            sensitivitySlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    MainMenuButtons.SensitivityKey,
                    defaultSensitivity));
        }

        RefreshResolutionDropdown();
    }

    // Populate the resolution dropdown.
    private void SetupResolutions()
    {
        availableResolutions.Clear();

        foreach (Resolution r in Screen.resolutions)
        {
            if (!availableResolutions.Exists(
                x => x.width == r.width &&
                     x.height == r.height))
            {
                availableResolutions.Add(r);
            }
        }

        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        List<string> labels = new List<string>();

        foreach (Resolution r in availableResolutions)
        {
            labels.Add(r.width + " x " + r.height);
        }

        resolutionDropdown.AddOptions(labels);

        RefreshResolutionDropdown();
    }

    private void RefreshResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        int width = PlayerPrefs.GetInt(
            MainMenuButtons.WidthKey, Screen.width);

        int height = PlayerPrefs.GetInt(
            MainMenuButtons.HeightKey, Screen.height);

        int selected = availableResolutions.FindIndex(
            r => r.width == width && r.height == height);

        if (selected < 0)
        {
            selected = availableResolutions.FindIndex(
                r => r.width == Screen.width &&
                     r.height == Screen.height);
        }

        if (selected < 0)
            selected = Mathf.Max(
                0, availableResolutions.Count - 1);

        resolutionDropdown.SetValueWithoutNotify(selected);
        resolutionDropdown.RefreshShownValue();
    }

    // Change master volume.
    public void SetVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            MainMenuButtons.VolumeKey,
            AudioListener.volume);

        PlayerPrefs.Save();
    }

    // Toggle fullscreen.
    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;

        PlayerPrefs.SetInt(
            MainMenuButtons.FullscreenKey,
            value ? 1 : 0);

        PlayerPrefs.Save();
    }

    // Change screen resolution.
    public void SetResolution(int index)
    {
        if (index < 0 ||
            index >= availableResolutions.Count)
            return;

        Resolution r = availableResolutions[index];

        Screen.SetResolution(
            r.width,
            r.height,
            Screen.fullScreen);

        PlayerPrefs.SetInt(
            MainMenuButtons.WidthKey, r.width);

        PlayerPrefs.SetInt(
            MainMenuButtons.HeightKey, r.height);

        PlayerPrefs.Save();
    }

    // Change mouse sensitivity.
    public void SetMouseSensitivity(float value)
    {
        float sensitivity = Mathf.Clamp(
            value, 0.1f, 10f);

        PlayerPrefs.SetFloat(
            MainMenuButtons.SensitivityKey,
            sensitivity);

        PlayerPrefs.Save();

        // Apply sensitivity immediately to the FPC.
        if (playerController != null)
            playerController.SetSensitivity(sensitivity);
    }

    // Return to the main menu scene.
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Quit the game.
    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        Debug.Log("[PauseMenu] Quit works in a built game.");
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        // Prevent leaving the game paused if this object is destroyed.
        if (isPaused)
            Time.timeScale = 1f;
    }
}
