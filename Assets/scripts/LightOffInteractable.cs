using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightOffInteractable : MonoBehaviour, IInteractable
{
    public enum LightKind { WallSwitch, Pumpkin }

    [Header("Task")]
    public TurnOffLightsTask lightsTask;
    public LightKind kind = LightKind.WallSwitch;

    [Header("Turn These Off")]
    public Light[] lights;
    [Tooltip("Optional glow objects to hide, such as a pumpkin flame or emissive mesh.")]
    public GameObject[] glowObjects;

    [Header("Sound")]
    [Tooltip("AudioSource on this object or another active GameObject.")]
    public AudioSource audioSource;
    public AudioClip switchSound;
    public AudioClip pumpkinSound;

    [Header("Optional Visual")]
    [Tooltip("Optional switch lever to rotate when clicked.")]
    public Transform lever;
    public Vector3 leverOffEulerAngles;

    private bool switchedOff;

    public void Interact()
    {
        if (switchedOff || lightsTask == null) return;
        if (!lightsTask.RegisterLight(kind == LightKind.Pumpkin)) return;

        switchedOff = true;
        if (lights != null)
            foreach (Light lightComponent in lights)
                if (lightComponent != null) lightComponent.enabled = false;

        if (glowObjects != null)
            foreach (GameObject glow in glowObjects)
                if (glow != null) glow.SetActive(false);

        if (lever != null) lever.localEulerAngles = leverOffEulerAngles;

        AudioClip clip = kind == LightKind.Pumpkin ? pumpkinSound : switchSound;
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
