using UnityEngine;

public class MusicManager : MonoBehaviour
{
    static MusicManager instance;

    void Awake()
    {
        int numOfMusicPlayers = FindObjectsByType<MusicManager>(FindObjectsSortMode.None).Length;

        if (numOfMusicPlayers > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}