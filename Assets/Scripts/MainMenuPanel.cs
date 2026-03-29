using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la pantalla principal del menú.
/// Permite introducir nombre, unirse a sala, acceder a opciones y salir.
/// </summary>
public class MainMenuPanel : MonoBehaviour
{
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private InputField roomNameInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        ValidateReferences();
        SetupButtonListeners();
        LoadSavedData();
    }

    /// <summary>
    /// Valida que todas las referencias estén asignadas en el Inspector.
    /// </summary>
    private void ValidateReferences()
    {
        if (playerNameInput == null)
            Debug.LogError("[MainMenuPanel] playerNameInput no está asignado en el Inspector");

        if (roomNameInput == null)
            Debug.LogError("[MainMenuPanel] roomNameInput no está asignado en el Inspector");

        if (joinButton == null)
            Debug.LogError("[MainMenuPanel] joinButton no está asignado en el Inspector");

        if (settingsButton == null)
            Debug.LogError("[MainMenuPanel] settingsButton no está asignado en el Inspector");

        if (quitButton == null)
            Debug.LogError("[MainMenuPanel] quitButton no está asignado en el Inspector");
    }

    /// <summary>
    /// Conecta los eventos de botones a métodos.
    /// </summary>
    private void SetupButtonListeners()
    {
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinButtonClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);
    }

    /// <summary>
    /// Carga los datos guardados previos en los campos.
    /// </summary>
    private void LoadSavedData()
    {
        if (PlayerSessionData.Instance != null)
        {
            if (playerNameInput != null)
                playerNameInput.text = PlayerSessionData.Instance.PlayerName;

            if (roomNameInput != null)
                roomNameInput.text = PlayerSessionData.Instance.RoomName;
        }
    }

    /// <summary>
    /// Manejador del botón "Unirse".
    /// </summary>
    private void OnJoinButtonClicked()
    {
        string playerName = playerNameInput?.text ?? "";
        string roomName = roomNameInput?.text ?? "";

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Por favor introduce un nombre de jugador");
            return;
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("Por favor introduce un nombre de sala");
            return;
        }

        Debug.Log($"Intentando unirse - Jugador: {playerName}, Sala: {roomName}");

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.JoinRoom(roomName, playerName);
        }
        else
        {
            Debug.LogError("MenuManager.Instance no encontrado");
        }
    }

    /// <summary>
    /// Manejador del botón "Opciones".
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        Debug.Log("Abriendo panel de opciones");

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowSettings();
        }
    }

    /// <summary>
    /// Manejador del botón "Salir".
    /// </summary>
    private void OnQuitButtonClicked()
    {
        Debug.Log("Botón salir presionado");

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.QuitGame();
        }
    }
}
