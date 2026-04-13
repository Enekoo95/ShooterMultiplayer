using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el panel de opciones.
/// Permite ajustar volumen y calidad de gráficos.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Dropdown graphicsQualityDropdown;
    [SerializeField] private Button backButton;
    [SerializeField] private Text masterVolumeLabel;
    [SerializeField] private Text musicVolumeLabel;

    private void Start()
    {
        ValidateReferences();
        SetupSliderListeners();
        SetupDropdownListener();
        SetupButtonListeners();
        LoadSettings();
    }

    /// <summary>
    /// Valida que todas las referencias estén asignadas.
    /// </summary>
    private void ValidateReferences()
    {
        if (masterVolumeSlider == null)
            Debug.LogError("[SettingsPanel] masterVolumeSlider no está asignado");

        if (musicVolumeSlider == null)
            Debug.LogError("[SettingsPanel] musicVolumeSlider no está asignado");

        if (graphicsQualityDropdown == null)
            Debug.LogError("[SettingsPanel] graphicsQualityDropdown no está asignado");

        if (backButton == null)
            Debug.LogError("[SettingsPanel] backButton no está asignado");
    }

    /// <summary>
    /// Conecta los eventos de los sliders.
    /// </summary>
    private void SetupSliderListeners()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
    }

    /// <summary>
    /// Conecta el evento del dropdown de calidad gráfica.
    /// </summary>
    private void SetupDropdownListener()
    {
        if (graphicsQualityDropdown != null)
        {
            // Poblar el dropdown con nombres de niveles de calidad
            graphicsQualityDropdown.options.Clear();
            foreach (string qualityLevel in QualitySettings.names)
            {
                graphicsQualityDropdown.options.Add(new Dropdown.OptionData(qualityLevel));
            }

            graphicsQualityDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);
        }
    }

    /// <summary>
    /// Conecta el botón "Volver".
    /// </summary>
    private void SetupButtonListeners()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    /// <summary>
    /// Carga los valores de configuración guardados.
    /// </summary>
    private void LoadSettings()
    {
        if (PlayerSessionData.Instance == null)
            return;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = PlayerSessionData.Instance.MasterVolume;
            UpdateMasterVolumeLabel();
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerSessionData.Instance.MusicVolume;
            UpdateMusicVolumeLabel();
        }

        if (graphicsQualityDropdown != null)
        {
            graphicsQualityDropdown.value = PlayerSessionData.Instance.GraphicsQuality;
        }
    }

    /// <summary>
    /// Manejador de cambio de volumen maestro.
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        if (PlayerSessionData.Instance != null)
        {
            PlayerSessionData.Instance.SetMasterVolume(value);
            UpdateMasterVolumeLabel();
        }
    }

    /// <summary>
    /// Manejador de cambio de volumen de música.
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        if (PlayerSessionData.Instance != null)
        {
            PlayerSessionData.Instance.SetMusicVolume(value);
            UpdateMusicVolumeLabel();
        }
    }

    /// <summary>
    /// Manejador de cambio de calidad de gráficos.
    /// </summary>
    private void OnGraphicsQualityChanged(int value)
    {
        if (PlayerSessionData.Instance != null)
        {
            PlayerSessionData.Instance.SetGraphicsQuality(value);
        }
    }

    /// <summary>
    /// Actualiza el label del volumen maestro.
    /// </summary>
    private void UpdateMasterVolumeLabel()
    {
        if (masterVolumeLabel != null && masterVolumeSlider != null)
        {
            masterVolumeLabel.text = $"Volumen Maestro: {Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
        }
    }

    /// <summary>
    /// Actualiza el label del volumen de música.
    /// </summary>
    private void UpdateMusicVolumeLabel()
    {
        if (musicVolumeLabel != null && musicVolumeSlider != null)
        {
            musicVolumeLabel.text = $"Volumen Música: {Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
        }
    }

    /// <summary>
    /// Manejador del botón "Volver".
    /// </summary>
    private void OnBackButtonClicked()
    {
        Debug.Log("Volviendo al menú principal");

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowMainMenu();
        }
    }
}
