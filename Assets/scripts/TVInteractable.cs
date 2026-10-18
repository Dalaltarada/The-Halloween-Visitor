
using UnityEngine;

public class TVInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    public TVRelaxTask relaxTask;

    [Header("TV Visuals")]
    public GameObject tvScreenOn;
    public GameObject tvScreenOff;

    [Header("Audio")]
    public AudioSource tvAudio;

    private bool isOn;

    private void Start()
    {
        UpdateTVVisuals();
    }

    public void Interact()
    {
        if (relaxTask == null || !relaxTask.TaskStarted)
            return;

        if (isOn)
            return;

        isOn = true;

        UpdateTVVisuals();

        if (tvAudio != null)
            tvAudio.Play();

        relaxTask.TurnOnTV();
    }

    private void UpdateTVVisuals()
    {
        if (tvScreenOn != null)
            tvScreenOn.SetActive(isOn);

        if (tvScreenOff != null)
            tvScreenOff.SetActive(!isOn);
    }
}
