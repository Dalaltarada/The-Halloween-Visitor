using System.Reflection;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

// Attach to the same GameObject as your Standard Assets FirstPersonController.
// Compatible with the common Unity Standard Assets m_MouseLook implementation.
public class ApplyMouseSensitivity : MonoBehaviour
{
    public FirstPersonController playerController;
    [Min(0.1f)] public float defaultSensitivity = 2f;

    private void Start()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MainMenuButtons.VolumeKey, 1f);

        if (playerController == null)
            playerController = GetComponent<FirstPersonController>();

        if (playerController == null)
        {
            Debug.LogWarning("[Mouse Sensitivity] FirstPersonController not assigned.");
            return;
        }

        float sensitivity = PlayerPrefs.GetFloat(MainMenuButtons.SensitivityKey, defaultSensitivity);
        FieldInfo mouseLookField = typeof(FirstPersonController).GetField("m_MouseLook", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (mouseLookField == null)
        {
            Debug.LogWarning("[Mouse Sensitivity] m_MouseLook not found. Your modified FPC may need direct integration.");
            return;
        }

        object mouseLook = mouseLookField.GetValue(playerController);
        if (mouseLook == null) return;

        FieldInfo x = mouseLook.GetType().GetField("XSensitivity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo y = mouseLook.GetType().GetField("YSensitivity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (x == null || y == null)
        {
            Debug.LogWarning("[Mouse Sensitivity] XSensitivity/YSensitivity fields not found.");
            return;
        }
        x.SetValue(mouseLook, sensitivity);
        y.SetValue(mouseLook, sensitivity);
        mouseLookField.SetValue(playerController, mouseLook);
    }
}
