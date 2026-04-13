using TMPro;
using UnityEngine;

public class KillFeedManager : MonoBehaviour
{
    public static KillFeedManager Instance;

    public TextMeshProUGUI killFeedText;

    void Awake()
    {
        // đảm bảo chỉ có 1 cái duy nhất
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // không bị mất khi đổi scene
        DontDestroyOnLoad(gameObject);
    }

    public void AddKill(string killer, string victim)
    {
        killFeedText.text += $"{killer} 🔫 {victim}\n";
    }
}