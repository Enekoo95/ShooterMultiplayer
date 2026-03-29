using Fusion;
using UnityEngine;
using System.Threading.Tasks;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    private NetworkRunner runner;

    private void Awake()
    {
        Instance = this;
    }

    private NetworkRunner GetRunner()
    {
        if (runner == null)
        {
            GameObject obj = new GameObject("NetworkRunner");

            runner = obj.AddComponent<NetworkRunner>();
            obj.AddComponent<NetworkSceneManagerDefault>();

            runner.ProvideInput = true;

            DontDestroyOnLoad(obj);
        }

        return runner;
    }

    public async Task StartHost(string roomName)
    {
        var runner = GetRunner();

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            Scene = SceneRef.FromIndex(2)
        });
    }

    public async Task JoinGame(string roomName)
    {
        var runner = GetRunner();

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            Scene = SceneRef.FromIndex(2)
        });
    }
}