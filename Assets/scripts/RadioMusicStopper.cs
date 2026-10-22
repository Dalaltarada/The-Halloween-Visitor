using UnityEngine;

public class RadioMusicStopper : MonoBehaviour
{
    [Header("Radio Audio")]
    public AudioSource radioAudio;

    public void StopRadio()
    {
        if (radioAudio != null)
        {
            radioAudio.Stop();
            Debug.Log("[RadioMusicStopper] Radio music stopped.");
        }
    }
}