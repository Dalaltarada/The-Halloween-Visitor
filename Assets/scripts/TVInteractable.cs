using UnityEngine;
using UnityEngine.Video;
public class TVInteractable : MonoBehaviour, IInteractable
{
    public TVRelaxTask relaxTask;
    [Header("Turn Off Lights Task")]
    public TurnOffLightsTask lightsTask;
    [Header("Video Player - keep on an always-active object")]
    public VideoPlayer videoPlayer;
    [Header("Visuals")]
    public GameObject tvScreenOn;
    public GameObject tvScreenOff;
    public Light tvLight;
    private bool isOn;
    private void Start()
    {
        isOn = false;
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
        }
        UpdateVisuals();
    }
    public void Interact()
    {
        // Once the lights task starts, this click turns the TV OFF and counts it.
        if (lightsTask != null && lightsTask.IsActive)
        {
            if (!isOn) return;
            if (!lightsTask.RegisterTV()) return;
            isOn = false;
            if (videoPlayer != null) videoPlayer.Stop();
            UpdateVisuals();
            return;
        }

        // Original relaxation task: turn the TV ON once pizza is placed.
        if (isOn || relaxTask == null || !relaxTask.TaskStarted ||
            !relaxTask.PizzaPlaced || relaxTask.TaskCompleted) return;
        isOn = true;
        UpdateVisuals();
        if (videoPlayer != null) videoPlayer.Play();
        else Debug.LogWarning("[TV] Assign VideoPlayer!");
        relaxTask.TurnOnTV();
    }
    private void UpdateVisuals()
    {
        if (tvScreenOn != null) tvScreenOn.SetActive(isOn);
        if (tvScreenOff != null) tvScreenOff.SetActive(!isOn);
        if (tvLight != null) tvLight.enabled = isOn;
    }
}
