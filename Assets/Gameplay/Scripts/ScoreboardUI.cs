using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;

public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private GameObject scoreboardPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerEntryPrefab;

    private bool isScoreboardVisible = false;

    // Singleton para que GameState pueda llamarlo directamente
    public static ScoreboardUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleScoreboard();
    }

    public void RefreshIfVisible()
    {
        if (isScoreboardVisible)
            UpdateScoreboard();
    }

    public void ForceRefresh()
    {
        UpdateScoreboard();
    }

    private void ToggleScoreboard()
    {
        isScoreboardVisible = !isScoreboardVisible;
        scoreboardPanel.SetActive(isScoreboardVisible);

        if (isScoreboardVisible)
            UpdateScoreboard();
    }

    private void UpdateScoreboard()
    {
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        if (GameState.Instance == null) return;

        var sorted = new List<(PlayerRef, GameState.PlayerStats)>();
        foreach (var entry in GameState.Instance.PlayerStatsDictionary)
            sorted.Add((entry.Key, entry.Value));
        sorted.Sort((a, b) => b.Item2.Score.CompareTo(a.Item2.Score));

        foreach (var (playerRef, stats) in sorted)
        {
            GameObject entryObj = Instantiate(playerEntryPrefab, playerListContainer);
            TextMeshProUGUI[] texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 4)
            {
                texts[0].text = GameState.Instance.GetPlayerName(playerRef);
                texts[1].text = stats.Kills.ToString();
                texts[2].text = stats.Deaths.ToString();
                texts[3].text = stats.Score.ToString();
            }
        }

        if (titleText != null)
        {
            int timeLeft = GameState.Instance.TimeRemaining;
            titleText.text = $"SCOREBOARD  |  {timeLeft / 60}:{(timeLeft % 60):D2}";
        }
    }
}