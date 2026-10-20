
using System.Collections;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [Header("Light")]
    public Light targetLight;

    [Header("Flicker Settings")]
    public float minFlickerDelay = 0.05f;
    public float maxFlickerDelay = 0.2f;

    [Range(0f, 1f)]
    public float minIntensityMultiplier = 0.1f;

    [Range(0f, 1f)]
    public float maxIntensityMultiplier = 1f;

    [Header("Duration")]
    public float flickerDuration = 3f;

    [Header("Options")]
    public bool turnOffAfterFlicker = false;
    public bool flickerOnStart = false;

    private float originalIntensity;
    private bool originalEnabled;
    private Coroutine flickerCoroutine;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
        {
            originalIntensity = targetLight.intensity;
            originalEnabled = targetLight.enabled;
        }
    }

    private void Start()
    {
        if (flickerOnStart)
            StartFlicker();
    }

    // Call this from Fungus.
    public void StartFlicker()
    {
        if (targetLight == null)
        {
            Debug.LogWarning("[LightFlicker] No Light assigned!");
            return;
        }

        StopFlicker();

        flickerCoroutine = StartCoroutine(FlickerRoutine());
    }

    // Call this from Fungus to stop the flicker.
    public void StopFlicker()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        RestoreLight();
    }

    // Instantly switch the light off.
    public void TurnOffLight()
    {
        StopFlicker();

        if (targetLight != null)
            targetLight.enabled = false;
    }

    // Restore the light to its original settings.
    public void RestoreLight()
    {
        if (targetLight == null)
            return;

        targetLight.intensity = originalIntensity;
        targetLight.enabled = originalEnabled;
    }

    private IEnumerator FlickerRoutine()
    {
        float elapsed = 0f;

        while (elapsed < flickerDuration)
        {
            float multiplier = Random.Range(
                minIntensityMultiplier,
                maxIntensityMultiplier
            );

            targetLight.enabled = true;
            targetLight.intensity =
                originalIntensity * multiplier;

            float delay = Random.Range(
                minFlickerDelay,
                maxFlickerDelay
            );

            yield return new WaitForSeconds(delay);

            elapsed += delay;
        }

        if (turnOffAfterFlicker)
        {
            targetLight.enabled = false;
            targetLight.intensity = originalIntensity;
        }
        else
        {
            RestoreLight();
        }

        flickerCoroutine = null;
    }

    private void OnDisable()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        RestoreLight();
    }
}
