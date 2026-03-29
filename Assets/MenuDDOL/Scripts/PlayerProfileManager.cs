using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance;

    public string playerName;

    private void Awake()
    {
        Instance = this;
    }
}