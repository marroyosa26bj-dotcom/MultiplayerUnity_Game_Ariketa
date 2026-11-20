using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using System.Text;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI names;
    [SerializeField] private TextMeshProUGUI scores;

    private ScoreManager scoreManager;

    private void OnEnable()
    {
        BindToScoreManager();
    }

    private void Update()
    {
        if (scoreManager != ScoreManager.Instance)
            BindToScoreManager();
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.PlayerScores.OnListChanged -= OnPlayerScoresChanged;

        scoreManager = null;
    }

    private void BindToScoreManager()
    {
        if (scoreManager != null)
            scoreManager.PlayerScores.OnListChanged -= OnPlayerScoresChanged;

        scoreManager = ScoreManager.Instance;
        if (scoreManager == null)
            return;

        scoreManager.PlayerScores.OnListChanged += OnPlayerScoresChanged;
        GetLeaderboard();
    }

    private void OnPlayerScoresChanged(NetworkListEvent<PlayerScoreEntry> changeEvent)
    {
        GetLeaderboard();
    }

    private void GetLeaderboard()
    {
        if (scoreManager == null)
            return;

        List<PlayerScoreEntry> entries = new();
        foreach (PlayerScoreEntry entry in scoreManager.PlayerScores)
            entries.Add(entry);

        entries.Sort((left, right) => right.Score.CompareTo(left.Score));

        StringBuilder namesText = new("Name");
        StringBuilder scoresText = new("Scores");
        foreach (PlayerScoreEntry entry in entries)
        {
            namesText.AppendLine();
            namesText.Append(entry.Name.ToString());
            scoresText.AppendLine();
            scoresText.Append(entry.Score);
        }

        if (names != null)
            names.text = namesText.ToString();

        if (scores != null)
            scores.text = scoresText.ToString();
    }
}
