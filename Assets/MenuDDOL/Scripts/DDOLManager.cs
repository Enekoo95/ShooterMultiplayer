using UnityEngine;
using UnityEngine.SceneManagement;
public class DDOLManager : MonoBehaviour
{
    public static DDOLManager Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadRoom()
    {
        SceneManager.LoadScene("Room");
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
