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
    
    [Header("Hole Tracking - UNIFIED SYSTEM")]
    [Tooltip("Текущее количество активных (незаделанных) дыр")]
    public int currentActiveHoles = 0;
    
    [Tooltip("Сколько дыр уже заделано в этом ивенте")]
    public int totalPatchedHoles = 0;
    
    [Tooltip("Всего создано дыр в этом ивенте")]
    public int totalSpawnedHoles = 0;

    [Header("Hole Spawn Settings")]
    public GameObject HolePrefab;
    
    [Tooltip("Количество дыр для спавна в начале ивента")]
    public int eventHolesToSpawn = 5;

    private List<GameObject> spawnedHoles = new List<GameObject>();
    private bool isEventActive = false;
    private bool hasEventInDayManager = false;

    #region Event Methods
    public void StartHoleEvent()
    {
        isEventActive = true;
        hasEventInDayManager = true;
        
        // Сбрасываем счётчики при начале нового ивента
        totalPatchedHoles = 0;
        totalSpawnedHoles = 0;
        
        SpawnEventHoles();
        
        Debug.Log($"🚨 Начался ивент с пробоинами! Нужно заварить ВСЕ дыры!");
        Debug.Log($"📊 Создано дыр: {totalSpawnedHoles}, Активных: {currentActiveHoles}");
    }

    public void StopHoleEvent()
    {
        isEventActive = false;
        hasEventInDayManager = false;
        
        // Удаляем только дыры которые остались незаделанными
        foreach (var hole in spawnedHoles)
        {
            if (hole != null)
            {
                PatchableHole patchable = hole.GetComponent<PatchableHole>();
                if (patchable != null && !patchable.IsPatched)
                {
                    Destroy(hole);
                }
            }
        }
        
        CleanupHolesList();
        
        // Сбрасываем счётчики для нового дня
        totalPatchedHoles = 0;
        totalSpawnedHoles = 0;
        currentActiveHoles = 0;
        
        Debug.Log("✅ Ивент с пробоинами завершён! Счётчики сброшены.");
    }

    private void SpawnEventHoles()
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

        for (int i = 0; i < eventHolesToSpawn; i++)
        {
            CreateHole("Event");
        }

        Debug.Log($"💥 Создано пробоин от ивента: {eventHolesToSpawn}");
    }

    /// <summary>
    /// Публичный метод для создания одной дыры (для GameOverManager при разрядке батареи)
    /// </summary>
    public void SpawnSingleHole()
    {
        // НОВОЕ: Если ивента нет в списке дня, добавляем его динамически
        if (!hasEventInDayManager && dayEventManager != null)
        {
            Debug.Log("⚡ Батарея создала дыру! Автоматически активируем ивент 'PatchHoles'");
            
            dayEventManager.AddEvent("PatchHoles");
            isEventActive = true;
            hasEventInDayManager = true;
        }
        
        CreateHole("Battery");
    }

    /// <summary>
    /// Универсальный метод создания дыры
    /// </summary>
    private void CreateHole(string source)
    {
        if (HolePrefab == null || wallSpawnZones.Count == 0) return;

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
        currentActiveHoles++;
        totalSpawnedHoles++;
        
        Debug.Log($"💥 Создана дыра [{source}]! Всего: {totalSpawnedHoles}, Активных: {currentActiveHoles}, Заделано: {totalPatchedHoles}");
    }

    /// <summary>
    /// Возвращает количество активных дыр (для GameOverManager)
    /// </summary>
    public int GetActiveHolesCount()
    {
        CleanupHolesList();
        return currentActiveHoles;
    }

    /// <summary>
    /// Возвращает список активных дыр
    /// </summary>
    public List<GameObject> GetActiveHoles()
    {
        CleanupHolesList();
        return new List<GameObject>(spawnedHoles);
    }

    /// <summary>
    /// Очистка списка от null объектов и обновление счётчика
    /// </summary>
    private void CleanupHolesList()
    {
        spawnedHoles.RemoveAll(h => h == null);
        
        // Пересчитываем активные дыры (незаделанные)
        int activeCount = 0;
        foreach (var hole in spawnedHoles)
        {
            PatchableHole patchable = hole.GetComponent<PatchableHole>();
            if (patchable != null && !patchable.IsPatched)
            {
                activeCount++;
            }
        }
        currentActiveHoles = activeCount;
    }

    public void GetRandomPointOnWall(BoxCollider zone, out Vector3 position, out Quaternion rotation)
    {
        Transform zoneTransform = zone.transform;
        
        Vector3 localPoint = new Vector3(
            Random.Range(-zone.size.x / 2f, zone.size.x / 2f),
            Random.Range(-zone.size.y / 2f, zone.size.y / 2f),
            -zone.size.z / 2f
        );
        
        Vector3 worldPoint = zoneTransform.TransformPoint(zone.center + localPoint);
        Vector3 inwardDirection = -zoneTransform.forward;
        
        position = worldPoint + inwardDirection * WallOffset;
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
        totalPatchedHoles++;
        currentActiveHoles = Mathf.Max(0, currentActiveHoles - 1);
        
        Debug.Log($"🔧 Заварена дыра! Осталось: {currentActiveHoles}, Заварено: {totalPatchedHoles}/{totalSpawnedHoles}");

        CleanupHolesList();

        // Проверяем завершение ивента - нужно заделать ВСЕ дыры
        if (isEventActive && currentActiveHoles == 0)
        {
            Debug.Log($"✅ ВСЕ дыры заделаны! ({totalPatchedHoles}/{totalSpawnedHoles})");
            
            if (dayEventManager != null)
            {
                dayEventManager.CompleteEvent("PatchHoles");
            }
        }
        else if (isEventActive)
        {
            Debug.Log($"⏳ Ещё осталось дыр: {currentActiveHoles}");
        }
    }

    /// <summary>
    /// Для отображения прогресса в UI
    /// </summary>
    public string GetProgressText()
    {
        if (!isEventActive)
            return "Нет активных дыр";
        
        if (currentActiveHoles == 0)
            return $"✅ Все дыры заделаны ({totalPatchedHoles}/{totalSpawnedHoles})";
        
        return $"Заделано: {totalPatchedHoles}/{totalSpawnedHoles} | Осталось: {currentActiveHoles}";
    }

    /// <summary>
    /// Свойства для совместимости со старым кодом
    /// </summary>
    public int patchedHoles => totalPatchedHoles;
    public int holesToPatch => totalSpawnedHoles;

    #endregion

    #region Editor Tools
    #if UNITY_EDITOR
    [ContextMenu("Create Zones (Smart)")]
    public void CreateZonesSmart()
    {
        List<Transform> wallsToProcess = new List<Transform>();

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

            if (onlyBoxColliders && !(wallCollider is BoxCollider))
            {
                Debug.Log($"⏭️ Пропускаем {wall.name} ({wallCollider.GetType().Name} - не BoxCollider)");
                skippedWalls++;
                continue;
            }

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

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.matrix = t.localToWorldMatrix;
            Gizmos.DrawCube(zone.center, zone.size);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(zone.center, zone.size);
            
            Gizmos.matrix = Matrix4x4.identity;
            
            Vector3 inwardDirection = -t.forward;
            Gizmos.color = Color.red;
            DrawArrow(center, inwardDirection * 1f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(center, t.forward * 0.3f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(center, t.up * 0.3f);
            
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawRay(center, t.right * 0.3f);
            
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