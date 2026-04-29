using UnityEngine;
using UnityEngine.UI;

public class StreakHUD : MonoBehaviour
{
    [SerializeField] private Image grenadeIcon;
    [SerializeField] private Image airStrikeIcon;
    [SerializeField] private Image turretIcon;

    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.3f);

    private StreakSystem streakSystem;

    private void Update()
    {
        if (streakSystem == null)
        {
            // Buscar el StreakSystem del jugador local
            foreach (var s in FindObjectsOfType<StreakSystem>())
            {
                if (s.HasInputAuthority)
                {
                    streakSystem = s;
                    break;
                }
            }
            return;
        }

        UpdateIcon(grenadeIcon, streakSystem.HasGrenade);
        UpdateIcon(airStrikeIcon, streakSystem.HasAirStrike);
        UpdateIcon(turretIcon, streakSystem.HasTurret);
    }

    private void UpdateIcon(Image icon, bool isActive)
    {
        if (icon == null) return;
        icon.color = isActive ? activeColor : inactiveColor;
    }
}