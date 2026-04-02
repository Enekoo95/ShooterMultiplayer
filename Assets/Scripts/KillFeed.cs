using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestiona el feed de eliminaciones.
/// Muestra las kills recientes en la esquina superior derecha.
/// </summary>
public class KillFeed : MonoBehaviour
{
    [SerializeField] private Transform killFeedContainer;
    [SerializeField] private GameObject killEntryPrefab;
    [SerializeField] private int maxEntries = 5;
    [SerializeField] private float entryLifetime = 5f;

    private Queue<GameObject> killEntries = new Queue<GameObject>();

    public void AddKill(string killerName, string victimName, string weaponName)
    {
        // Crear nueva entrada
        GameObject entry = Instantiate(killEntryPrefab, killFeedContainer);
        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
        {
            text.text = $"{killerName} [{weaponName}] {victimName}";
        }

        killEntries.Enqueue(entry);

        // Remover entradas viejas si excede el máximo
        if (killEntries.Count > maxEntries)
        {
            GameObject oldEntry = killEntries.Dequeue();
            Destroy(oldEntry);
        }

        // Auto-destruir después del tiempo de vida
        StartCoroutine(DestroyEntry(entry, entryLifetime));
    }

    private IEnumerator DestroyEntry(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (entry != null && killEntries.Contains(entry))
        {
            killEntries.Dequeue();
            Destroy(entry);
        }
    }
}
