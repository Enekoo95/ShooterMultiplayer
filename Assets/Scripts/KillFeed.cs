using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class KillFeed : MonoBehaviour
{
    public static KillFeed Instance { get; private set; }

    [SerializeField] private Transform killFeedContainer;
    [SerializeField] private GameObject killEntryPrefab;
    [SerializeField] private int maxEntries = 5;
    [SerializeField] private float entryLifetime = 5f;

    private Queue<GameObject> killEntries = new Queue<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[KillFeed] Instance registrada correctamente.");
    }

    private void Start()
    {
        // Diagnóstico: avisar si faltan referencias en el Inspector
        if (killEntryPrefab == null)
            Debug.LogError("[KillFeed] killEntryPrefab NO está asignado en el Inspector.");
        if (killFeedContainer == null)
            Debug.LogError("[KillFeed] killFeedContainer NO está asignado en el Inspector.");
    }

    public void AddKill(string killerName, string victimName, string weaponName)
    {
        Debug.Log($"[KillFeed] AddKill llamado: {killerName} [{weaponName}] {victimName}");

        if (killEntryPrefab == null || killFeedContainer == null)
        {
            Debug.LogError("[KillFeed] Faltan referencias en el Inspector.");
            return;
        }

        // Asegurarse de que el panel está activo
        gameObject.SetActive(true);

        // Eliminar entradas viejas si se supera el máximo
        while (killEntries.Count >= maxEntries)
        {
            GameObject old = killEntries.Dequeue();
            if (old != null) Destroy(old);
        }

        // Crear nueva entrada
        GameObject entry = Instantiate(killEntryPrefab, killFeedContainer);
        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
            text.text = $"{killerName}  [{weaponName}]  {victimName}";
        else
            Debug.LogError("[KillFeed] El killEntryPrefab no tiene TextMeshProUGUI como hijo.");

        killEntries.Enqueue(entry);
        StartCoroutine(DestroyEntry(entry, entryLifetime));
    }

    private IEnumerator DestroyEntry(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (entry == null) yield break;

        var newQueue = new Queue<GameObject>();
        foreach (var e in killEntries)
        {
            if (e != entry && e != null)
                newQueue.Enqueue(e);
        }
        killEntries = newQueue;

        Destroy(entry);
    }
}