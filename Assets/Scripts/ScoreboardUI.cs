using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona la interfaz del scoreboard.
/// Se muestra/oculta con la tecla TAB.
/// </summary>
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
        {
            ToggleScoreboard();
        }
    }

    private void ToggleScoreboard()
    {
        isScoreboardVisible = !isScoreboardVisible;
        scoreboardPanel.SetActive(isScoreboardVisible);

        if (isScoreboardVisible)
        {
            UpdateScoreboard();
        }
    }

    private void UpdateScoreboard()
    {
        // Limpiar lista anterior
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        if (GameState.Instance == null)
            return;

        var sortedStats = GameState.Instance.GetSortedStats();

        foreach (var stats in sortedStats)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
            TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 3)
            {
                texts[0].text = GameState.Instance.GetPlayerName(new Fusion.PlayerRef()); // TODO: Obtener PlayerRef correcto
                texts[1].text = stats.Kills.ToString();
                texts[2].text = stats.Score.ToString();
            }
        }

        // Actualizar título con tiempo restante
        if (titleText != null)
        {
            int timeLeft = GameState.Instance.TimeRemaining;
            titleText.text = $"SCOREBOARD - {timeLeft / 60}:{(timeLeft % 60):D2}";
        }
    }
}
