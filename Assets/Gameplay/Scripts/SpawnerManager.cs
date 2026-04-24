using Fusion;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Windows;
using static Unity.Collections.Unicode;
using System;
using Unity.IO.LowLevel.Unsafe;

public class SpawnerManager : MonoBehaviour
{
    private object _spawnedPlayers;
    public GameObject playerPrefab;
    private int index;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnPlayer();
    }

    public async void SpawnPlayer()
    {
        Vector3 pos = Vector3.zero;
        NetworkObject playerObj = NetworkManager.Instance.runner.Spawn(playerPrefab, pos, Quaternion.identity, null);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
