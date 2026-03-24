using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    [SerializeField] AudioPlayer audioPlayer;

    public static SoundFXManager instance;

    private void Awake()
    {
        if(instance == null) instance = this;
    }

    public void PlaySoundFX(AudioClip clip, Transform transform)
    {
        AudioPlayer player = Instantiate(audioPlayer,transform);
        player.Play(clip);
    }

    public void PlaySoundFX(AudioClip clip, Vector3 position)
    {
        AudioPlayer player = Instantiate(audioPlayer, position, Quaternion.identity);
        player.Play(clip);
    }
}