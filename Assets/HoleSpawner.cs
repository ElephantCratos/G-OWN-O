using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BNG;

public class HoleSpawner : MonoBehaviour
{
    [Header("Spawn Zones")]
    [Tooltip("Зоны спавна на стенах (BoxCollider). Пробоины появятся на поверхности зоны, обращённой внутрь")]
    public List<BoxCollider> wallSpawnZones = new List<BoxCollider>();
    
    [Tooltip("Отступ от стены внутрь помещения (в метрах)")]
    public float WallOffset = 0.05f;

    [Header("Auto-Setup from Walls")]
    [Tooltip("Корневой объект со стенами (например, объект со всеми Wall0-Wall16)")]
    public Transform wallsContainer;
    
    [Tooltip("Префикс имени стен (например, 'Wall' найдёт Wall0, Wall1, и т.д.)")]
    public string wallNamePrefix = "Wall";
    
    [Tooltip("Родительский объект для созданных зон")]
    public Transform zonesParent;
    
    [Tooltip("Насколько уменьшить зону относительно коллайдера стены (чтобы не заходить за углы)")]
    [Range(0f, 1f)]
    public float zoneShrinkFactor = 0.9f;

    [Header("Wall Filtering")]
    [Tooltip("Исключить стены с этими словами в имени")]
    public List<string> excludeKeywords = new List<string> { "Door", "Passage", "Opening" };
    
    [Tooltip("Использовать только BoxCollider (игнорировать MeshCollider с проходами)")]
    public bool onlyBoxColliders = true;

    [Header("Manual Wall Selection")]
    [Tooltip("ИЛИ вручную выберите стены (имеет приоритет если не пусто)")]
    public List<Transform> selectedWalls = new List<Transform>();

    [Header("Event Integration")]
    public DayEventManager dayEventManager;
    public int holesToPatch = 5;
    public int patchedHoles = 0;

    [Header("Hole Spawn Settings")]
    public GameObject HolePrefab;
    public int HolesToSpawn = 5;

    private List<GameObject> spawnedHoles = new List<GameObject>();
    private bool eventActive = false;

    #region Event Methods
    public void StartHoleEvent()
    {
        eventActive = true;
        patchedHoles = 0;
        
        SpawnHoles();
        
        Debug.Log($"Начался ивент с пробоинами! Нужно заварить: {holesToPatch}");
    }

    public void StopHoleEvent()
    {
        eventActive = false;
        
        foreach (var hole in spawnedHoles)
        {
            if (hole != null)
                Destroy(hole);
        }
        spawnedHoles.Clear();
        
        Debug.Log("Ивент с пробоинами завершён!");
    }


    private void SpawnHoles()
    {
        if (HolePrefab == null)
        {
            Debug.LogWarning("HoleSpawner: не назначен HolePrefab!");
            return;
        }

        if (wallSpawnZones.Count == 0)
        {
            Debug.LogWarning("HoleSpawner: нет зон спавна на стенах!");
            return;
        }

        int toSpawn = Mathf.Min(HolesToSpawn, holesToPatch);

        for (int i = 0; i < toSpawn; i++)
        {
            BoxCollider zone = wallSpawnZones[Random.Range(0, wallSpawnZones.Count)];
            
            Vector3 spawnPos;
            Quaternion spawnRot;
            GetRandomPointOnWall(zone, out spawnPos, out spawnRot);

            GameObject hole = Instantiate(HolePrefab, spawnPos, spawnRot);
            
            PatchableHole patchable = hole.GetComponent<PatchableHole>();
            if (patchable != null)
            {
                StartCoroutine(WatchHolePatch(patchable));
            }
            else
            {
                Debug.LogWarning($"У пробоины {hole.name} нет компонента PatchableHole!");
            }
            
            spawnedHoles.Add(hole);
        }

        Debug.Log($"Создано пробоин: {spawnedHoles.Count}");
    }
    /// <summary>
/// Возвращает количество активных дыр (для GameOverManager)
/// </summary>
public int GetActiveHolesCount()
{
    // Удаляем null объекты перед подсчётом
    spawnedHoles.RemoveAll(h => h == null);
    return spawnedHoles.Count;
}

/// <summary>
/// Возвращает список активных дыр (для дополнительной логики)
/// </summary>
public List<GameObject> GetActiveHoles()
{
    spawnedHoles.RemoveAll(h => h == null);
    return new List<GameObject>(spawnedHoles);
}



    public void GetRandomPointOnWall(BoxCollider zone, out Vector3 position, out Quaternion rotation)
    {
        Transform zoneTransform = zone.transform;
        
        // Генерируем случайную точку на внутренней поверхности зоны
        Vector3 localPoint = new Vector3(
            Random.Range(-zone.size.x / 2f, zone.size.x / 2f),
            Random.Range(-zone.size.y / 2f, zone.size.y / 2f),
            -zone.size.z / 2f  // На внутренней грани (локальная ось -Z)
        );
        
        // Переводим в мировые координаты
        Vector3 worldPoint = zoneTransform.TransformPoint(zone.center + localPoint);
        
        // Направление внутрь = локальная ось -Z зоны в мировом пространстве
        Vector3 inwardDirection = -zoneTransform.forward;
        
        // Применяем offset вдоль этого направления
        position = worldPoint + inwardDirection * WallOffset;
        
        // Пробоина смотрит в том же направлении (внутрь помещения)
        rotation = Quaternion.LookRotation(inwardDirection);
    }

    private IEnumerator WatchHolePatch(PatchableHole hole)
    {
        while (hole != null && !hole.IsPatched)
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (hole != null && hole.IsPatched)
        {
            OnHolePatched();
        }
    }

    private void OnHolePatched()
    {
        if (!eventActive) return;

        patchedHoles++;
        Debug.Log($"Заварено пробоин: {patchedHoles}/{holesToPatch}");

        spawnedHoles.RemoveAll(h => h == null);

        if (patchedHoles >= holesToPatch)
        {
            StopHoleEvent();
            dayEventManager?.CompleteEvent("PatchHoles");
        }
    }
    #endregion

    #region Editor Tools
    #if UNITY_EDITOR
    [ContextMenu("Create Zones (Smart)")]
    public void CreateZonesSmart()
    {
        List<Transform> wallsToProcess = new List<Transform>();

        // Приоритет: ручной выбор > автоматический поиск
        if (selectedWalls.Count > 0)
        {
            Debug.Log("Используем вручную выбранные стены");
            wallsToProcess.AddRange(selectedWalls);
        }
        else if (wallsContainer != null)
        {
            Debug.Log("Используем автоматический поиск стен");
            foreach (Transform child in wallsContainer)
            {
                if (child.name.StartsWith(wallNamePrefix))
                {
                    wallsToProcess.Add(child);
                }
            }
        }
        else
        {
            Debug.LogError("Не задан ни Walls Container, ни Selected Walls!");
            return;
        }

        if (wallsToProcess.Count == 0)
        {
            Debug.LogError("Не найдено стен для обработки!");
            return;
        }

        if (zonesParent == null)
        {
            GameObject parent = new GameObject("WallSpawnZones");
            zonesParent = parent.transform;
        }

        wallSpawnZones.Clear();
        int createdZones = 0;
        int skippedWalls = 0;

        foreach (Transform wall in wallsToProcess)
        {
            if (wall == null) continue;

            // Проверка исключений по имени
            bool shouldExclude = false;
            foreach (string keyword in excludeKeywords)
            {
                if (wall.name.ToLower().Contains(keyword.ToLower()))
                {
                    shouldExclude = true;
                    Debug.Log($"⏭️ Пропускаем {wall.name} (содержит '{keyword}')");
                    skippedWalls++;
                    break;
                }
            }
            
            if (shouldExclude) continue;

            Collider wallCollider = wall.GetComponent<Collider>();
            if (wallCollider == null)
            {
                Debug.LogWarning($"⏭️ {wall.name}: нет коллайдера");
                skippedWalls++;
                continue;
            }

            // Фильтр по типу коллайдера
            if (onlyBoxColliders && !(wallCollider is BoxCollider))
            {
                Debug.Log($"⏭️ Пропускаем {wall.name} ({wallCollider.GetType().Name} - не BoxCollider)");
                skippedWalls++;
                continue;
            }

            // Создаём зону
            GameObject zoneObj = new GameObject($"SpawnZone_{wall.name}");
            zoneObj.transform.SetParent(zonesParent);
            
            Bounds bounds = wallCollider.bounds;
            zoneObj.transform.rotation = wall.rotation;
            zoneObj.transform.position = bounds.center;
            zoneObj.transform.localScale = Vector3.one;
            
            BoxCollider zoneBox = zoneObj.AddComponent<BoxCollider>();
            zoneBox.isTrigger = true;
            zoneBox.center = Vector3.zero;

            Vector3 worldSize = bounds.size;
            float minAxis = Mathf.Min(worldSize.x, Mathf.Min(worldSize.y, worldSize.z));
            
            Vector3 localSize = zoneObj.transform.InverseTransformVector(worldSize);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            
            if (Mathf.Approximately(worldSize.x, minAxis))
            {
                localSize.x = 0.2f;
                localSize.y *= zoneShrinkFactor;
                localSize.z *= zoneShrinkFactor;
            }
            else if (Mathf.Approximately(worldSize.y, minAxis))
            {
                localSize.y = 0.2f;
                localSize.x *= zoneShrinkFactor;
                localSize.z *= zoneShrinkFactor;
            }
            else
            {
                localSize.z = 0.2f;
                localSize.x *= zoneShrinkFactor;
                localSize.y *= zoneShrinkFactor;
            }
            
            zoneBox.size = localSize;
            wallSpawnZones.Add(zoneBox);
            createdZones++;
            
            Debug.Log($"✅ {wall.name}");
        }

        Debug.Log($"🎉 Создано: {createdZones} зон | Пропущено: {skippedWalls} стен");
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Find Walls Container Automatically")]
    public void FindWallsContainer()
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>();
        
        foreach (Transform t in allTransforms)
        {
            int wallCount = 0;
            foreach (Transform child in t)
            {
                if (child.name.StartsWith(wallNamePrefix))
                    wallCount++;
            }
            
            if (wallCount >= 5)
            {
                wallsContainer = t;
                Debug.Log($"✅ Найден контейнер стен: {t.name} ({wallCount} стен)");
                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }
        }
        
        Debug.LogWarning($"Не найден объект содержащий стены с префиксом '{wallNamePrefix}'");
    }

    [ContextMenu("Clear All Spawn Zones")]
    public void ClearAllSpawnZones()
    {
        if (zonesParent != null)
        {
            UnityEditor.Undo.DestroyObjectImmediate(zonesParent.gameObject);
            zonesParent = null;
        }
        
        wallSpawnZones.Clear();
        Debug.Log("Все зоны спавна удалены");
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Debug: Test Spawn Points")]
    public void DebugTestSpawnPoints()
    {
        if (wallSpawnZones.Count == 0)
        {
            Debug.LogWarning("Нет зон спавна!");
            return;
        }

        Debug.Log("=== ТЕСТ ТОЧЕК СПАВНА ===");
        
        foreach (var zone in wallSpawnZones)
        {
            if (zone == null) continue;

            Vector3 spawnPos;
            Quaternion spawnRot;
            GetRandomPointOnWall(zone, out spawnPos, out spawnRot);

            Vector3 direction = -zone.transform.forward;
            Debug.DrawRay(zone.transform.position, direction * 2f, Color.red, 5f);
            Debug.DrawRay(spawnPos, spawnRot * Vector3.forward * 0.3f, Color.yellow, 5f);
            
            Debug.Log($"{zone.name}:");
            Debug.Log($"  Центр зоны: {zone.transform.position}");
            Debug.Log($"  Направление внутрь: {direction}");
            Debug.Log($"  Точка спавна: {spawnPos}");
            Debug.Log($"  Разница по Y: {spawnPos.y - zone.transform.position.y}");
        }
        
        Debug.Log("Красные лучи = направление спавна. Проверьте их в Scene View!");
    }

    [ContextMenu("Debug: Show Wall Bounds")]
    public void DebugShowWallBounds()
    {
        if (wallsContainer == null)
        {
            Debug.LogError("Не назначен Walls Container!");
            return;
        }

        Debug.Log("=== ИНФОРМАЦИЯ О СТЕНАХ ===");
        
        foreach (Transform child in wallsContainer)
        {
            if (!child.name.StartsWith(wallNamePrefix))
                continue;

            Collider col = child.GetComponent<Collider>();
            if (col == null) continue;

            Bounds bounds = col.bounds;
            
            Debug.Log($"{child.name}:");
            Debug.Log($"  Position: {child.position}");
            Debug.Log($"  Bounds Center: {bounds.center}");
            Debug.Log($"  Bounds Size: {bounds.size}");
            Debug.Log($"  Min Axis: {Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z)}");
        }
        
        Debug.Log("========================");
    }

    [ContextMenu("Fix Zone Orientations")]
    public void FixZoneOrientations()
    {
        foreach (var zone in wallSpawnZones)
        {
            if (zone == null) continue;
            
            // Предполагаем что внутрь = вниз по мировой Y
            // Поворачиваем зону так чтобы её -forward смотрел вниз
            zone.transform.rotation = Quaternion.Euler(90, 0, 0);
            
            Debug.Log($"Исправлен поворот {zone.name}");
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
    #endif
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (wallSpawnZones == null) return;

        foreach (var zone in wallSpawnZones)
        {
            if (zone == null) continue;

            Transform t = zone.transform;
            Vector3 center = t.TransformPoint(zone.center);

            // Рисуем зону
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.matrix = t.localToWorldMatrix;
            Gizmos.DrawCube(zone.center, zone.size);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(zone.center, zone.size);
            
            Gizmos.matrix = Matrix4x4.identity;
            
            // Направление спавна (внутрь помещения) - БОЛЬШАЯ КРАСНАЯ СТРЕЛКА
            Vector3 inwardDirection = -t.forward;
            Gizmos.color = Color.red;
            DrawArrow(center, inwardDirection * 1f);
            
            // Локальные оси для понимания ориентации (маленькие)
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(center, t.forward * 0.3f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(center, t.up * 0.3f);
            
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawRay(center, t.right * 0.3f);
            
            // Тестовые точки спавна
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 3; i++)
            {
                Vector3 localPoint = new Vector3(
                    Random.Range(-zone.size.x / 2f, zone.size.x / 2f),
                    Random.Range(-zone.size.y / 2f, zone.size.y / 2f),
                    -zone.size.z / 2f
                );
                Vector3 worldPoint = t.TransformPoint(zone.center + localPoint);
                Vector3 spawnPoint = worldPoint + inwardDirection * WallOffset;
                
                Gizmos.DrawWireSphere(spawnPoint, 0.1f);
                Gizmos.DrawLine(worldPoint, spawnPoint);
            }
        }
        
        #if UNITY_EDITOR
        // Визуализация стен (для отладки)
        if (wallsContainer != null && UnityEditor.Selection.activeGameObject == gameObject)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            foreach (Transform child in wallsContainer)
            {
                if (!child.name.StartsWith(wallNamePrefix))
                    continue;
                    
                Collider col = child.GetComponent<Collider>();
                if (col != null)
                {
                    Bounds bounds = col.bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }
        #endif
    }
/// <summary>
/// Публичный метод для создания одной дыры (для GameOverManager)
/// </summary>
public void SpawnSingleHole()
{
    if (HolePrefab == null)
    {
        Debug.LogWarning("HoleSpawner: не назначен HolePrefab!");
        return;
    }

    if (wallSpawnZones.Count == 0)
    {
        Debug.LogWarning("HoleSpawner: нет зон спавна на стенах!");
        return;
    }

    BoxCollider zone = wallSpawnZones[Random.Range(0, wallSpawnZones.Count)];
    
    Vector3 spawnPos;
    Quaternion spawnRot;
    GetRandomPointOnWall(zone, out spawnPos, out spawnRot);

    GameObject hole = Instantiate(HolePrefab, spawnPos, spawnRot);
    
    // Если ивент активен, отслеживаем заваривание
    if (eventActive)
    {
        PatchableHole patchable = hole.GetComponent<PatchableHole>();
        if (patchable != null)
        {
            StartCoroutine(WatchHolePatch(patchable));
        }
    }
    
    spawnedHoles.Add(hole);
    
    Debug.Log($"💥 Создана экстренная пробоина! Всего дыр: {spawnedHoles.Count}");
}
    private void DrawArrow(Vector3 pos, Vector3 direction)
    {
        Gizmos.DrawRay(pos, direction);
        
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        
        Gizmos.DrawRay(pos + direction, right * 0.25f);
        Gizmos.DrawRay(pos + direction, left * 0.25f);
    }
    #endregion
}