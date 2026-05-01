using UnityEngine;
using Fusion;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance;

    public string playerName = "Jugador";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // sobrevive al cambio de escena
    }

    public void RegisterWithGameState(NetworkRunner runner, PlayerRef playerRef)
    {
        if (GameState.Instance == null)
        {
            Debug.LogError("[PlayerProfileManager] GameState.Instance es null al registrar.");
            return;
        }

        string nameToSend = string.IsNullOrEmpty(playerName) ? "Jugador" + playerRef.PlayerId : playerName;
        GameState.Instance.RegisterPlayer(playerRef, nameToSend);
        Debug.Log($"[PlayerProfileManager] Registrado como: {nameToSend}");
    }
}