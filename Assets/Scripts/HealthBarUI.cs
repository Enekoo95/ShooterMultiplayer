using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Actualiza la barra de vida en pantalla para el jugador local.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    private PlayerController localPlayer;

    private void Awake()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 100f;
            healthSlider.value = 100f;
        }
    }

    private void Update()
    {
        if (localPlayer == null)
        {
            localPlayer = FindLocalPlayer();
            if (localPlayer == null)
                return;
        }

        UpdateHealthBar(localPlayer.CurrentHealth);
    }

    private void UpdateHealthBar(float health)
    {
        float clampedHealth = Mathf.Clamp(health, 0f, 100f);

        if (healthSlider != null)
            healthSlider.value = clampedHealth;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(clampedHealth)} / 100";
    }

    private PlayerController FindLocalPlayer()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (player.HasInputAuthority)
                return player;
        }
        return null;
    }
}
