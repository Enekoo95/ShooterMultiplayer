using UnityEngine;

/// <summary>
/// Singleton persistente que almacena datos del jugador entre escenas.
/// Se destruye al cargar escenas de juego y se recrea en el menú.
/// </summary>
public class PlayerSessionData : MonoBehaviour
{
    public static PlayerSessionData Instance { get; private set; }

    [SerializeField] private string playerName = "Player";
    [SerializeField] private string roomName = "";
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private int graphicsQuality = 2;

    public string PlayerName => playerName;
    public string RoomName => roomName;
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public int GraphicsQuality => graphicsQuality;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Actualiza el nombre del jugador.
    /// </summary>
    public void SetPlayerName(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
        {
            playerName = newName.Trim();
            PlayerPrefs.SetString("PlayerName", playerName);
        }
    }

    /// <summary>
    /// Actualiza el nombre de la sala a unirse.
    /// </summary>
    public void SetRoomName(string newRoomName)
    {
        roomName = newRoomName ?? "";
        PlayerPrefs.SetString("RoomName", roomName);
    }

    /// <summary>
    /// Actualiza el volumen maestro.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        AudioListener.volume = masterVolume;
    }

    /// <summary>
    /// Actualiza el volumen de música.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    /// <summary>
    /// Actualiza la calidad de gráficos (0-5).
    /// </summary>
    public void SetGraphicsQuality(int quality)
    {
        graphicsQuality = Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(graphicsQuality);
        PlayerPrefs.SetInt("GraphicsQuality", graphicsQuality);
    }

    /// <summary>
    /// Carga datos guardados de PlayerPrefs.
    /// </summary>
    private void LoadSettings()
    {
        playerName = PlayerPrefs.GetString("PlayerName", "Player");
        roomName = PlayerPrefs.GetString("RoomName", "");
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 2);

        AudioListener.volume = masterVolume;
        QualitySettings.SetQualityLevel(graphicsQuality);
    }

    private void Start()
    {
        LoadSettings();
    }
}
