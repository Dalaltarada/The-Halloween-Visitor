
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RadioInteractable : MonoBehaviour, IInteractable
{
    [Header("Radio Audio")]
    public AudioSource radioAudio;

    [Header("Settings")]
    public bool playOnStart = false;
    public bool loopMusic = true;

    private void Start()
    {
        if (radioAudio == null)
            radioAudio = GetComponent<AudioSource>();

        if (radioAudio == null)
        {
            Debug.LogWarning("[Radio] No AudioSource assigned!");
            return;
        }

        radioAudio.loop = loopMusic;
        radioAudio.playOnAwake = false;

        if (playOnStart)
            radioAudio.Play();
        else
            radioAudio.Stop();
    }

    public void Interact()
    {
        if (radioAudio == null) return;

        if (radioAudio.isPlaying)
        {
            radioAudio.Stop();
        }
        else
        {
            radioAudio.Play();
        }
    }
}
