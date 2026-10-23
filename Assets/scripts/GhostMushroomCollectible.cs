
using UnityEngine;

public class GhostMushroomCollectible : MonoBehaviour, IInteractable
{
    [Header("Collectible Settings")]
    public AudioClip collectSFX;

    [Range(0f, 1f)]
    public float volume = 1f;

    private bool collected = false;

    public void Interact()
    {
        if (collected)
            return;

        collected = true;

        // Play collection sound.
        if (collectSFX != null)
        {
            AudioSource.PlayClipAtPoint(
                collectSFX,
                transform.position,
                volume
            );
        }

        // Hide the mushroom.
        gameObject.SetActive(false);
    }
}
