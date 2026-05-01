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
    }

    private void Start()
    {
        // Diagnóstico inicial
        if (killEntryPrefab == null)
            Debug.LogError("[KillFeed] killEntryPrefab NO está asignado en el Inspector.");
        if (killFeedContainer == null)
            Debug.LogError("[KillFeed] killFeedContainer NO está asignado en el Inspector.");

    }

    public void AddKill(string killerName, string victimName, string weaponName)
    {
        if (killEntryPrefab == null || killFeedContainer == null) return;

        // Eliminar entradas viejas si se supera el máximo antes de crear la nueva
        while (killEntries.Count >= maxEntries)
        {
            GameObject old = killEntries.Dequeue();
            if (old != null) Destroy(old);
        }

        // Crear nueva entrada
        GameObject entry = Instantiate(killEntryPrefab, killFeedContainer);

        entry.transform.localScale = Vector3.one;

        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
        {
            text.text = $"{killerName} elimino a {victimName} con {weaponName}  ";
        }
        else
        {
            Debug.LogError("[KillFeed] El prefab no tiene un componente TextMeshProUGUI.");
        }

        killEntries.Enqueue(entry);
        StartCoroutine(DestroyEntryRoutine(entry, entryLifetime));
    }

    private IEnumerator DestroyEntryRoutine(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (entry != null)
        {
            // Remover de la cola si aún existe allí
            if (killEntries.Contains(entry))
            {
                // Reconstruimos la cola sin este elemento
                Queue<GameObject> updatedQueue = new Queue<GameObject>();
                foreach (var item in killEntries)
                {
                    if (item != entry && item != null) updatedQueue.Enqueue(item);
                }
                killEntries = updatedQueue;
            }

            Destroy(entry);
        }
    }
}