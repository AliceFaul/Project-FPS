using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    public TextMeshProUGUI leaderboardText;

    Dictionary<string, int> kills = new Dictionary<string, int>();
    Dictionary<string, int> deaths = new Dictionary<string, int>();

    void Awake()
    {
        Instance = this;
    }

    public void AddKill(string killer, string victim)
    {
        if (!kills.ContainsKey(killer)) kills[killer] = 0;
        if (!deaths.ContainsKey(victim)) deaths[victim] = 0;

        kills[killer]++;
        deaths[victim]++;

        UpdateUI();
    }

    void UpdateUI()
    {
        leaderboardText.text = "";

        foreach (var player in kills)
        {
            string name = player.Key;
            int k = player.Value;
            int d = deaths.ContainsKey(name) ? deaths[name] : 0;

            leaderboardText.text += name + " | K: " + k + " | D: " + d + "\n";
        }
    }
}