
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class SplashScreenController : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Next Scene")]
    public string mainMenuSceneName = "MainMenu";

    private bool isLoading = false;

    private void Start()
    {
        // Unlock mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Reset time
        Time.timeScale = 1f;

        if (videoPlayer == null)
        {
            Debug.LogError(
                "[SplashScreen] Video Player is not assigned!"
            );
            return;
        }

        // Disable looping
        videoPlayer.isLooping = false;

        // Detect when video finishes
        videoPlayer.loopPointReached += OnVideoFinished;

        // Play splash video
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (isLoading)
            return;

        isLoading = true;

        // Load Main Menu
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
