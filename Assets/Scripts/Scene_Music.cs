using UnityEngine;

public class Scene_Music : MonoBehaviour
{
    public AudioClip backgroundMusic;

    void Start()
    {
        MusicManager.Instance.PlayMusic(backgroundMusic);
    }
}
