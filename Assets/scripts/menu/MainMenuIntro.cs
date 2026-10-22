using System.Collections;
using UnityEngine;

public class MainMenuIntro : MonoBehaviour
{
    [Header("Camera")]
    public Transform menuCamera;
    public Transform cameraFirstPosition;
    public Transform cameraSecondPosition;
    [Min(0.01f)] public float cameraMoveDuration = 4f;
    public AnimationCurve cameraEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Doorbell")]
    public AudioSource doorbellAudio;
    [Min(0f)] public float pauseBeforeDoorbell = 0.3f;

    [Header("Music")]
    public AudioSource musicAudio;
    [Range(0f, 1f)] public float musicTargetVolume = 0.65f;
    [Min(0f)] public float musicFadeDuration = 2f;

    [Header("Menu UI (separate CanvasGroups)")]
    public CanvasGroup titleGroup;
    public CanvasGroup buttonsGroup;
    [Min(0f)] public float titleFadeDuration = 1.5f;
    [Min(0f)] public float pauseBeforeButtons = 0.3f;
    [Min(0f)] public float buttonsFadeDuration = 1.5f;

    private IEnumerator Start()
    {
        SetGroup(titleGroup, 0f, false);
        SetGroup(buttonsGroup, 0f, false);

        if (musicAudio != null)
        {
            musicAudio.playOnAwake = false;
            musicAudio.volume = 0f;
            musicAudio.loop = true;
            musicAudio.Stop();
        }

        if (doorbellAudio != null)
        {
            doorbellAudio.playOnAwake = false;
            doorbellAudio.Stop();
        }

        if (menuCamera == null || cameraFirstPosition == null || cameraSecondPosition == null)
        {
            Debug.LogError("[MainMenuIntro] Assign camera and both camera positions.");
            yield break;
        }

        menuCamera.SetPositionAndRotation(cameraFirstPosition.position, cameraFirstPosition.rotation);
        yield return MoveCamera();
        if (pauseBeforeDoorbell > 0f) yield return new WaitForSeconds(pauseBeforeDoorbell);

        if (doorbellAudio != null && doorbellAudio.clip != null)
        {
            doorbellAudio.Play();
            // Wait until the sound finishes before the music and UI begin.
            while (doorbellAudio.isPlaying) yield return null;
        }

        if (musicAudio != null && musicAudio.clip != null)
        {
            musicAudio.Play();
            StartCoroutine(FadeMusic());
        }

        yield return FadeGroup(titleGroup, titleFadeDuration);
        if (pauseBeforeButtons > 0f) yield return new WaitForSeconds(pauseBeforeButtons);
        yield return FadeGroup(buttonsGroup, buttonsFadeDuration);
    }

    private IEnumerator MoveCamera()
    {
        float elapsed = 0f;
        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = cameraEase.Evaluate(Mathf.Clamp01(elapsed / cameraMoveDuration));
            menuCamera.position = Vector3.LerpUnclamped(cameraFirstPosition.position, cameraSecondPosition.position, t);
            menuCamera.rotation = Quaternion.SlerpUnclamped(cameraFirstPosition.rotation, cameraSecondPosition.rotation, t);
            yield return null;
        }
        menuCamera.SetPositionAndRotation(cameraSecondPosition.position, cameraSecondPosition.rotation);
    }

    private IEnumerator FadeMusic()
    {
        float elapsed = 0f;
        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.deltaTime;
            musicAudio.volume = Mathf.Lerp(0f, musicTargetVolume, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, musicFadeDuration)));
            yield return null;
        }
        musicAudio.volume = musicTargetVolume;
    }

    private static void SetGroup(CanvasGroup group, float alpha, bool interactive)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactive;
        group.blocksRaycasts = interactive;
    }

    private IEnumerator FadeGroup(CanvasGroup group, float duration)
    {
        if (group == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            yield return null;
        }
        SetGroup(group, 1f, true);
    }
}
