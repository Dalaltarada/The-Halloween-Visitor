
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RadioInteractable : MonoBehaviour, IInteractable
{
    [Header("Radio Audio")]
    public AudioSource radioAudio;

    [Header("Settings")]
    public bool playOnStart = false;
    public bool loopMusic = true;

    [Header("Fade Settings")]
    [Min(0f)]
    public float fadeOutDuration = 2f;

    private bool radioDisabled = false;
    private Coroutine fadeCoroutine;

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
        // Prevent interaction after disabling the radio.
        if (radioDisabled)
            return;

        if (radioAudio == null)
            return;

        if (radioAudio.isPlaying)
        {
            radioAudio.Stop();
        }
        else
        {
            radioAudio.Play();
        }
    }

    // Stop music immediately, without disabling interaction.
    public void StopRadio()
    {
        if (radioAudio == null)
            radioAudio = GetComponent<AudioSource>();

        if (radioAudio != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            radioAudio.Stop();
        }
    }

    // Call from Fungus after the dialogue.
    // Fade music out and permanently disable interaction.
    public void DisableRadio()
    {
        if (radioDisabled)
            return;

        radioDisabled = true;

        // Disable interaction immediately.
        Collider radioCollider = GetComponent<Collider>();

        if (radioCollider != null)
            radioCollider.enabled = false;

        if (radioAudio == null)
            radioAudio = GetComponent<AudioSource>();

        if (radioAudio != null)
        {
            fadeCoroutine = StartCoroutine(FadeOutRadio());
        }

        Debug.Log("[Radio] Radio disabled. Fading out music.");
    }

    private IEnumerator FadeOutRadio()
    {
        float startVolume = radioAudio.volume;
        float elapsed = 0f;

        if (fadeOutDuration > 0f)
        {
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / fadeOutDuration
                );

                radioAudio.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    t
                );

                yield return null;
            }
        }

        radioAudio.volume = 0f;
        radioAudio.Stop();
        radioAudio.enabled = false;

        fadeCoroutine = null;

        Debug.Log("[Radio] Music fully faded out.");
    }
}
