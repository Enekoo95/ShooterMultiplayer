using UnityEngine;
using TMPro;
using Fusion;

public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private GameObject scoreboardPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerEntryPrefab;

    private bool isScoreboardVisible = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleScoreboard();
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

        if (GameState.Instance == null)
            return;

        // Iterar con PlayerRef correcto
        foreach (var entry in GameState.Instance.PlayerStatsDictionary)
        {
            PlayerRef playerRef = entry.Key;
            var stats = entry.Value;

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
            titleText.text = $"SCOREBOARD - {timeLeft / 60}:{(timeLeft % 60):D2}";
        }
    }
}