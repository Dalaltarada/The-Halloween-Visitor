using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class DoorInteractable : MonoBehaviour, IInteractable
{
    public enum Axis { X, Y, Z }
    public enum HingeEdge { XMinimum, XMaximum, ZMinimum, ZMaximum }

    [Header("Door Settings")]
    public HingeEdge hingeEdge = HingeEdge.XMinimum;
    public Axis hingeAxis = Axis.Y;
    public float openAngle = 90f;
    public bool reverseOpenDirection;
    public float openSpeed = 2f;
    public Vector3 hingeWorldOffset = Vector3.zero;

    [Header("Door Sound Effects")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("Sound Timing")]
    [Min(0f)] public float openSoundDelay = 0f;
    [Min(0f)] public float closeSoundDelay = 0.5f;

    [Header("Interaction")]
    [Tooltip("Disable on neighbour doors so only the doorbell and Fungus can open/close them.")]
    public bool allowPlayerInteraction = true;

    private AudioSource audioSource;
    private bool isOpen;
    private bool isAnimating;
    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
    public bool IsFullyClosed => !isOpen && !isAnimating;
    public bool IsFullyOpen => isOpen && !isAnimating;
    private float currentAngle;
    private Vector3 hingePointWorld;
    private Vector3 rotationAxisWorld;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        CalculateHinge();
    }

    private void CalculateHinge()
    {
        Renderer doorRenderer = GetComponentInChildren<Renderer>();
        if (doorRenderer == null)
        {
            hingePointWorld = transform.position;
            Debug.LogWarning("DoorInteractable: No Renderer found on " + name);
        }
        else
        {
            Bounds bounds = doorRenderer.bounds;
            hingePointWorld = bounds.center;
            switch (hingeEdge)
            {
                case HingeEdge.XMinimum: hingePointWorld.x = bounds.min.x; break;
                case HingeEdge.XMaximum: hingePointWorld.x = bounds.max.x; break;
                case HingeEdge.ZMinimum: hingePointWorld.z = bounds.min.z; break;
                case HingeEdge.ZMaximum: hingePointWorld.z = bounds.max.z; break;
            }
        }
        hingePointWorld += hingeWorldOffset;
        rotationAxisWorld = GetRotationAxis();
    }

    public void Interact()
    {
        if (!allowPlayerInteraction || isAnimating) return;
        if (isOpen) CloseFromScript();
        else OpenFromScript();
    }

    public void OpenFromScript()
    {
        if (isAnimating || isOpen) return;
        float direction = reverseOpenDirection ? -1f : 1f;
        StartCoroutine(RotateDoor(Mathf.Abs(openAngle) * direction));
        StartCoroutine(PlayDelayedSound(openSound, openSoundDelay));
    }

    public void CloseFromScript()
    {
        if (isAnimating || !isOpen) return;
        StartCoroutine(RotateDoor(0f));
        StartCoroutine(PlayDelayedSound(closeSound, closeSoundDelay));
    }

    private IEnumerator RotateDoor(float targetAngle)
    {
        isAnimating = true;
        float startAngle = currentAngle;
        float elapsed = 0f;
        float duration = 1f / Mathf.Max(openSpeed, 0.01f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float newAngle = Mathf.Lerp(startAngle, targetAngle, t);
            float deltaAngle = newAngle - currentAngle;
            transform.RotateAround(hingePointWorld, rotationAxisWorld, deltaAngle);
            currentAngle = newAngle;
            yield return null;
        }
        float remainingAngle = targetAngle - currentAngle;
        if (Mathf.Abs(remainingAngle) > 0.0001f)
            transform.RotateAround(hingePointWorld, rotationAxisWorld, remainingAngle);
        currentAngle = targetAngle;
        isOpen = Mathf.Abs(currentAngle) > 0.01f;
        isAnimating = false;
    }

    private IEnumerator PlayDelayedSound(AudioClip clip, float delay)
    {
        if (clip == null) yield break;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(clip, soundVolume);
    }

    private Vector3 GetRotationAxis()
    {
        switch (hingeAxis)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            case Axis.Z: return Vector3.forward;
            default: return Vector3.up;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 point;
        Vector3 axis;
        if (Application.isPlaying)
        {
            point = hingePointWorld;
            axis = rotationAxisWorld;
        }
        else
        {
            Renderer doorRenderer = GetComponentInChildren<Renderer>();
            point = transform.position;
            if (doorRenderer != null)
            {
                Bounds bounds = doorRenderer.bounds;
                point = bounds.center;
                switch (hingeEdge)
                {
                    case HingeEdge.XMinimum: point.x = bounds.min.x; break;
                    case HingeEdge.XMaximum: point.x = bounds.max.x; break;
                    case HingeEdge.ZMinimum: point.z = bounds.min.z; break;
                    case HingeEdge.ZMaximum: point.z = bounds.max.z; break;
                }
            }
            point += hingeWorldOffset;
            axis = GetRotationAxis();
        }
        Gizmos.DrawSphere(point, 0.04f);
        Gizmos.DrawLine(point, point + axis * 0.3f);
    }
}
