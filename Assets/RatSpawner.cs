using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RatSpawner : MonoBehaviour
{
    [Header("Rat Spawn Settings")]
    [Tooltip("Префаб живой крысы, которую нужно спавнить")]
    public GameObject RatPrefab;

    [Tooltip("Сколько крыс спавнится за один цикл")]
    public int RatsPerSpawn = 5;

    [Tooltip("Интервал между спавнами (в секундах)")]
    public float SpawnInterval = 10f;

    [Tooltip("Максимальное количество живых крыс одновременно")]
    public int MaxAliveRats = 20;

    [Tooltip("Начинать спавн автоматически при запуске сцены")]
    public bool AutoStart = true;

    [Header("Spawn Area")]
    [Tooltip("Радиус разброса, чтобы крысы появлялись не в одной точке")]
    public float SpawnRadius = 2f;

    private float timer;
    private List<GameObject> spawnedRats = new List<GameObject>();

    void Start()
    {
        if (AutoStart)
        {
            timer = SpawnInterval;
        }
    }

    void Update()
    {
        if (!AutoStart) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SpawnRats();
            timer = SpawnInterval;
        }

        // Удаляем из списка мёртвых (уничтоженных) крыс
        spawnedRats.RemoveAll(r => r == null);
    }

    public void SpawnRats()
    {
        if (RatPrefab == null)
        {
            Debug.LogWarning("RatSpawner: не назначен RatPrefab!");
            return;
        }

        int aliveCount = spawnedRats.Count;
        int canSpawn = Mathf.Min(RatsPerSpawn, MaxAliveRats - aliveCount);

        for (int i = 0; i < canSpawn; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-SpawnRadius, SpawnRadius), 0, Random.Range(-SpawnRadius, SpawnRadius));
            Vector3 spawnPos = transform.position + offset;

            GameObject rat = Instantiate(RatPrefab, spawnPos, Quaternion.identity);
            spawnedRats.Add(rat);
        }
    }
}