using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private bool isConnecting = false;

    private void Start()
    {
        playerNameInput.text = PlayerPrefs.GetString("PlayerName", "");
        roomNameInput.text = PlayerPrefs.GetString("RoomName", "sala1");

        if (PlayerProfileManager.Instance != null && !string.IsNullOrEmpty(playerNameInput.text))
            PlayerProfileManager.Instance.playerName = playerNameInput.text;

        createButton.onClick.AddListener(OnCreate);
        joinButton.onClick.AddListener(OnJoin);
        quitButton.onClick.AddListener(() => Application.Quit());
    }

    private async void OnCreate()
    {
        if (!Validate()) return;
        SavePrefs();
        SetUI(false, "Creando sala...");
        await NetworkManager.Instance.StartHost(roomNameInput.text.Trim());
    }

    private async void OnJoin()
    {
        if (!Validate()) return;
        SavePrefs();
        SetUI(false, "Uniéndose a sala...");
        await NetworkManager.Instance.JoinGame(roomNameInput.text.Trim());
    }

    private bool Validate()
    {
        if (isConnecting) return false;
        if (string.IsNullOrWhiteSpace(playerNameInput.text)) { SetStatus("Introduce un nombre."); return false; }
        if (string.IsNullOrWhiteSpace(roomNameInput.text)) { SetStatus("Introduce nombre de sala."); return false; }
        return true;
    }

    private void SavePrefs()
    {
        string name = playerNameInput.text.Trim();
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.SetString("RoomName", roomNameInput.text.Trim());
        PlayerPrefs.Save();

        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.playerName = name;
    }

    private void SetUI(bool interactable, string status = "")
    {
        isConnecting = !interactable;
        createButton.interactable = interactable;
        joinButton.interactable = interactable;
        SetStatus(status);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}