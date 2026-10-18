
using UnityEngine;

public class TrickOrTreatStoryTrigger : MonoBehaviour
{
    [Header("References")]
    public TrickOrTreatManager manager;

    [Header("Encounter")]
    public string encounterID;

    public void TriggerEncounter()
    {
        if (manager == null)
        {
            Debug.LogWarning(
                "[StoryTrigger] Manager missing!"
            );
            return;
        }

        manager.StartEncounter(encounterID);
    }
}
