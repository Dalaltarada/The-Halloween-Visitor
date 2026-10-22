using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class FinalSceneSequence : MonoBehaviour
{
    [Header("Player Lock")]
    public FirstPersonController playerController;

    [Header("Doorbell")]
    public AudioSource doorbellAudio;
    [Min(0f)] public float initialDelay = 1.5f;
    [Min(0f)] public float delayAfterDoorbell = 0.5f;

    [Header("Existing Door System")]
    public DoorInteractable door;

    private void Start()
    {
        if (playerController != null)
        {
            playerController.scriptedMovement = true;
            playerController.enabled = false; // Locks walking, jumping, and mouse look.
        }
        else
        {
            Debug.LogWarning("[FinalSceneSequence] Player Controller is not assigned.");
        }

        StartCoroutine(PlayFinalSequence());
    }

    private IEnumerator PlayFinalSequence()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        if (doorbellAudio != null && doorbellAudio.clip != null)
        {
            doorbellAudio.Stop();
            doorbellAudio.Play();
            yield return new WaitForSeconds(doorbellAudio.clip.length);
        }
        else
        {
            Debug.LogWarning("[FinalSceneSequence] Assign a doorbell AudioSource with an AudioClip.");
        }

        if (delayAfterDoorbell > 0f)
            yield return new WaitForSeconds(delayAfterDoorbell);

        if (door != null)
            door.OpenFromScript();
        else
            Debug.LogWarning("[FinalSceneSequence] DoorInteractable is not assigned.");
    }
}
