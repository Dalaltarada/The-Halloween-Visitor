
using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    public void LoadNextScene()
    {
        GameSceneManager manager =
            FindObjectOfType<GameSceneManager>();

        if (manager != null && manager.isActiveAndEnabled)
        {
            manager.LoadNextScene();
        }
        else
        {
            Debug.LogWarning(
                "[SceneTransitionTrigger] Active GameSceneManager not found!"
            );
        }
    }

    public void ReturnToMainMenu()
    {
        GameSceneManager manager =
            FindObjectOfType<GameSceneManager>();

        if (manager != null && manager.isActiveAndEnabled)
        {
            manager.ReturnToMainMenu();
        }
        else
        {
            Debug.LogWarning(
                "[SceneTransitionTrigger] Active GameSceneManager not found!"
            );
        }
    }
}
