using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BNG;

public class RatSpawner : MonoBehaviour
{
    [Header("Spawn Zones")]
    [Tooltip("Список точек/зон где могут появляться крысы")]
    public List<Transform> spawnPoints = new List<Transform>();
    
    [Tooltip("Радиус разброса вокруг каждой точки")]
    public float SpawnRadius = 2f;

    [Header("Event Integration")]
    public DayEventManager dayEventManager;
    public int ratsToKill = 10;
    public int killedRats = 0;

    [Header("Rat Spawn Settings")]
    public GameObject RatPrefab;
    public int RatsPerSpawn = 5;
    public float SpawnInterval = 10f;
    public int MaxAliveRats = 20;

    private float timer;
    private List<GameObject> spawnedRats = new List<GameObject>();
    private bool isSpawning = false;

    void Update()
    {
        if (!isSpawning) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SpawnRats();
            timer = SpawnInterval;
        }

        spawnedRats.RemoveAll(r => r == null);
    }

    public void StartSpawning()
    {
        isSpawning = true;
        killedRats = 0;
        timer = 0f; // Спавним сразу
        Debug.Log("Начался спавн крыс!");
    }

    public void StopSpawning()
    {
        isSpawning = false;
        
        foreach (var rat in spawnedRats)
        {
            if (rat != null)
                Destroy(rat);
        }
        spawnedRats.Clear();
    }

    public void SpawnRats()
    {
        if (RatPrefab == null)
        {
            Debug.LogWarning("RatSpawner: не назначен RatPrefab!");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("RatSpawner: нет точек спавна!");
            return;
        }

        int aliveCount = spawnedRats.Count;
        int canSpawn = Mathf.Min(RatsPerSpawn, MaxAliveRats - aliveCount);

        for (int i = 0; i < canSpawn; i++)
        {
            // Выбираем случайную точку спавна
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            
            // Добавляем случайный разброс вокруг точки
            Vector3 offset = new Vector3(
                Random.Range(-SpawnRadius, SpawnRadius), 
                0, 
                Random.Range(-SpawnRadius, SpawnRadius)
            );
            Vector3 spawnPos = spawnPoint.position + offset;

            GameObject rat = Instantiate(RatPrefab, spawnPos, Quaternion.identity);
            
            // Подписываемся на событие смерти через Damageable
            Damageable damageable = rat.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.onDestroyed.AddListener(OnRatKilled);
            }
            else
            {
                Debug.LogWarning($"У крысы {rat.name} нет компонента Damageable!");
            }
            
            spawnedRats.Add(rat);
        }
    }

    private void OnRatKilled()
    {
        killedRats++;
        Debug.Log($"Убито крыс: {killedRats}/{ratsToKill}");
        
        if (killedRats >= ratsToKill)
        {
            StopSpawning();
            dayEventManager?.CompleteEvent("ClearRats");
        }
    }

    // Визуализация зон спавна в редакторе
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (var point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, SpawnRadius);
            }
        }
    }
}