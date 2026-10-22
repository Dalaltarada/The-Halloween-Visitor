using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class GraveyardCandyFarewell : MonoBehaviour
{
    [Header("Player")]
    public FirstPersonController playerController;

    [Header("Candy - Existing Object In Hand")]
    public Transform candyInHand;
    public Transform candyLandingTarget;

    [Header("Throw Settings")]
    [Min(0.1f)] public float throwDuration = 1.1f;
    [Min(0f)] public float throwArcHeight = 0.7f;
    public bool matchLandingRotation = true;

    [Header("Cinematic Grave Zoom")]
    public Camera playerCamera;
    [Min(0f)] public float cameraMoveDistance = 1.2f;
    [Min(0.1f)] public float zoomDuration = 3f;

    [Header("Optional Sound")]
    public AudioSource audioSource;
    public AudioClip candyLandingSound;

    private Coroutine zoomRoutine;
    private bool farewellStarted;
    private bool candyThrown;
    private Coroutine throwRoutine;
    private bool controllerWasEnabled;
    private bool controllerWasScripted;
    private bool controllerLocked;

    public void BeginFarewell()
    {
        farewellStarted = true;
        candyThrown = false;

        if (playerController == null)
        {
            Debug.LogWarning("[Candy Farewell] Assign Player Controller.");
            return;
        }

        if (!controllerLocked)
        {
            controllerWasEnabled = playerController.enabled;
            controllerWasScripted = playerController.scriptedMovement;
            controllerLocked = true;
        }

        playerController.scriptedMovement = true;
        playerController.enabled = false;
    }

    public void ThrowCandy()
    {
        if (!farewellStarted || candyThrown)
            return;

        if (candyInHand == null || candyLandingTarget == null)
        {
            Debug.LogWarning("[Candy Farewell] Assign Candy In Hand and Candy Landing Target.");
            return;
        }

        candyThrown = true;

        // Move the existing candy out of the hand/camera hierarchy,
        // preserving its current world position and rotation.
        candyInHand.SetParent(null, true);
        candyInHand.gameObject.SetActive(true);

        // Disable physics during and after the cinematic throw.
        foreach (Rigidbody body in candyInHand.GetComponentsInChildren<Rigidbody>())
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
        }

        if (throwRoutine != null)
            StopCoroutine(throwRoutine);

        throwRoutine = StartCoroutine(AnimateCandyToTarget());
    }

    private IEnumerator AnimateCandyToTarget()
    {
        Vector3 start = candyInHand.position;
        Quaternion startRotation = candyInHand.rotation;
        float duration = Mathf.Max(0.1f, throwDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = t * t * (3f - 2f * t);

            // Read the target in world space. It can be moved in the editor.
            Vector3 position = Vector3.Lerp(start, candyLandingTarget.position, smoothT);
            position.y += Mathf.Sin(t * Mathf.PI) * throwArcHeight;

            candyInHand.position = position;
            if (matchLandingRotation)
                candyInHand.rotation = Quaternion.Slerp(startRotation, candyLandingTarget.rotation, smoothT);

            yield return null;
        }

        candyInHand.position = candyLandingTarget.position;
        if (matchLandingRotation)
            candyInHand.rotation = candyLandingTarget.rotation;

        if (audioSource != null && candyLandingSound != null)
            audioSource.PlayOneShot(candyLandingSound);

        throwRoutine = null;
    }

    // Invoke this from Fungus after the final dialogue.
    public void ZoomInOnGrave()
    {
        if (playerCamera == null && playerController != null)
            playerCamera = playerController.GetComponentInChildren<Camera>();

        if (playerCamera == null)
        {
            Debug.LogWarning("[Candy Farewell] Assign Player Camera for the grave zoom.");
            return;
        }

        if (zoomRoutine != null)
            StopCoroutine(zoomRoutine);

        zoomRoutine = StartCoroutine(AnimateGraveZoom());
    }

    private IEnumerator AnimateGraveZoom()
    {
        // Move straight forward along the camera's original viewing direction.
        // Do not rotate the camera or change its field of view.
        Transform cameraTransform = playerCamera.transform;
        Vector3 startPosition = cameraTransform.position;
        Vector3 endPosition = startPosition + cameraTransform.forward * cameraMoveDistance;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, zoomDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            cameraTransform.position = Vector3.Lerp(startPosition, endPosition, eased);
            yield return null;
        }

        cameraTransform.position = endPosition;
        zoomRoutine = null;
    }

    public void EndFarewell()
    {
        farewellStarted = false;

        if (playerController != null && controllerLocked)
        {
            playerController.scriptedMovement = controllerWasScripted;
            playerController.enabled = controllerWasEnabled;
            controllerLocked = false;
        }
    }
}
