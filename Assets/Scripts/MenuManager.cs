using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor principal del menú. Orquesta el flujo entre pantallas y la conexión a Fusion.
/// </summary>
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject loadingPanel;

    private NetworkRunner networkRunner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ShowMainMenu();
    }

    /// <summary>
    /// Muestra el panel principal del menú.
    /// </summary>
    public void ShowMainMenu()
    {
        mainMenuPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
        loadingPanel?.SetActive(false);
    }

    /// <summary>
    /// Muestra el panel de opciones.
    /// </summary>
    public void ShowSettings()
    {
        mainMenuPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
        loadingPanel?.SetActive(false);
    }

    /// <summary>
    /// Muestra la pantalla de cargando.
    /// </summary>
    public void ShowLoading()
    {
        mainMenuPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        loadingPanel?.SetActive(true);
    }

    /// <summary>
    /// Inicia la conexión a una sala de Fusion.
    /// </summary>
    public async void JoinRoom(string roomName, string playerName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogError("El nombre de la sala no puede estar vacío.");
            return;
        }

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogError("El nombre del jugador no puede estar vacío.");
            return;
        }

        ShowLoading();

        // Guardar datos de sesión
        PlayerSessionData.Instance.SetPlayerName(playerName);
        PlayerSessionData.Instance.SetRoomName(roomName);

        try
        {
            // Buscar o crear el NetworkRunner
            networkRunner = FindObjectOfType<NetworkRunner>();
            if (networkRunner == null)
            {
                Debug.LogError("No se encontró NetworkRunner en la escena. Asegúrate de tener un Runner de Fusion.");
                ShowMainMenu();
                return;
            }

            // Crear argumentos de entrada personalizados
            var args = new StartGameArgs();
            args.PlayerCount = 2; // Max 2 jugadores
            args.SessionName = roomName;

            // Iniciar el juego como cliente uniendo a la sala
            var result = await networkRunner.StartGame(args);

            if (result.Ok)
            {
                Debug.Log($"Conectado exitosamente a la sala: {roomName}");
                // La escena se cargará automáticamente via NetworkSceneManager de Fusion
            }
            else
            {
                Debug.LogError($"Error al conectar: {result.ShutdownReason}");
                ShowMainMenu();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción al unirse a la sala: {ex.Message}");
            ShowMainMenu();
        }
    }

    /// <summary>
    /// Sale del juego.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
