
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class ApplyMouseSensitivity : MonoBehaviour
{
    [Header("Player Controller")]
    public FirstPersonController playerController;

    [Header("Default Sensitivity")]
    [Range(0.1f, 10f)]
    public float defaultSensitivity = 2f;

    private void Start()
    {
        if (playerController == null)
            playerController = GetComponent<FirstPersonController>();

        if (playerController == null)
        {
            Debug.LogWarning(
                "[Mouse Sensitivity] FirstPersonController not found."
            );
            return;
        }

        ApplySavedSensitivity();
    }

    public void ApplySavedSensitivity()
    {
        if (playerController == null)
            return;

        float sensitivity = PlayerPrefs.GetFloat(
            MainMenuButtons.SensitivityKey,
            defaultSensitivity
        );

        sensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);

        playerController.SetSensitivity(sensitivity);

        Debug.Log(
            "[Mouse Sensitivity] Applied sensitivity: " +
            sensitivity
        );
    }
}
