using UnityEngine;
using Fusion;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    public static bool IsPaused = false;
    private bool isPaused = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            TogglePause();
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        IsPaused = isPaused;
        pausePanel.SetActive(isPaused);
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    public void Resume()
    {
        isPaused = false;
        IsPaused = false;
        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
            runner.Shutdown();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}