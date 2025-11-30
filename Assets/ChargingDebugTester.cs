using UnityEngine;
using BNG;

/// <summary>
/// Тестовый скрипт для работы без VR
/// Нажимайте клавиши для симуляции действий
/// </summary>
public class ChargingDebugTester : MonoBehaviour
{
    [Header("References")]
    public RobotChargingSystem chargingSystem;
    public DayEventManager dayEventManager;
    public ChargingCable chargingCable;
    
    [Header("Debug Settings")]
    public bool showDebugInfo = true;
    public KeyCode completeEvent1Key = KeyCode.Alpha1;
    public KeyCode completeEvent2Key = KeyCode.Alpha2;
    public KeyCode completeEvent3Key = KeyCode.Alpha3;
    public KeyCode forceChargeKey = KeyCode.Space;
    public KeyCode autoCompleteAllKey = KeyCode.C;
    public KeyCode resetDayKey = KeyCode.R;
    
    [Header("Visual Debug")]
    public bool showCableDistance = true;
    public Color gizmoNormalColor = Color.yellow;
    public Color gizmoReadyColor = Color.green;
    
    private float cableToSocketDistance;
    
    void Update()
    {
        HandleInput();
        
        if (showCableDistance && chargingCable != null)
        {
            CalculateCableDistance();
        }
    }
    
    void HandleInput()
    {
        // Завершение ивентов
        if (Input.GetKeyDown(completeEvent1Key))
        {
            CompleteEventByIndex(0);
        }
        
        if (Input.GetKeyDown(completeEvent2Key))
        {
            CompleteEventByIndex(1);
        }
        
        if (Input.GetKeyDown(completeEvent3Key))
        {
            CompleteEventByIndex(2);
        }
        
        // Автоматическое завершение всех ивентов
        if (Input.GetKeyDown(autoCompleteAllKey))
        {
            CompleteAllEvents();
        }
        
        // Принудительная зарядка (игнорирует проверку ивентов)
        if (Input.GetKeyDown(forceChargeKey))
        {
            ForceCharge();
        }
        
        // Сброс дня
        if (Input.GetKeyDown(resetDayKey))
        {
            ResetDay();
        }
        
        // Симуляция подключения кабеля (альтернатива VR)
        if (Input.GetKeyDown(KeyCode.P)) // P = Plug in
        {
            SimulatePlugIn();
        }
    }
    
    void CompleteEventByIndex(int index)
    {
        if (dayEventManager == null || dayEventManager.todayEvents == null) return;
        
        if (index >= 0 && index < dayEventManager.todayEvents.Count)
        {
            var eventToComplete = dayEventManager.todayEvents[index];
            dayEventManager.CompleteEvent(eventToComplete.eventName);
            Debug.Log($"✓ Ивент '{eventToComplete.eventName}' завершен!");
        }
        else
        {
            Debug.LogWarning($"Ивент с индексом {index} не найден!");
        }
    }
    
    void CompleteAllEvents()
    {
        if (dayEventManager == null) return;
        
        foreach (var dayEvent in dayEventManager.todayEvents)
        {
            if (!dayEvent.isCompleted)
            {
                dayEventManager.CompleteEvent(dayEvent.eventName);
            }
        }
        
        Debug.Log("✓✓✓ Все ивенты дня завершены! Можно идти на зарядку.");
    }
    
    void ForceCharge()
    {
        if (chargingSystem == null) return;
        
        Debug.Log("⚡ ПРИНУДИТЕЛЬНАЯ ЗАРЯДКА (игнорируются проверки ивентов)");
        
        // Временно завершаем все ивенты
        bool[] originalStates = null;
        if (dayEventManager != null)
        {
            originalStates = new bool[dayEventManager.todayEvents.Count];
            for (int i = 0; i < dayEventManager.todayEvents.Count; i++)
            {
                originalStates[i] = dayEventManager.todayEvents[i].isCompleted;
                dayEventManager.todayEvents[i].isCompleted = true;
            }
        }
        
        chargingSystem.TryStartCharging();
        
        // Восстанавливаем состояния (после корутины это не повлияет)
        // Это нужно только если зарядка не началась
    }
    
    void SimulatePlugIn()
    {
        if (chargingCable == null)
        {
            Debug.LogWarning("ChargingCable не назначен!");
            return;
        }
        
        Debug.Log("🔌 Симуляция подключения кабеля...");
        
        // Перемещаем кабель к розетке
        if (chargingCable.cablePlugTransform != null && chargingCable.socketTransform != null)
        {
            chargingCable.cablePlugTransform.position = chargingCable.socketTransform.position;
            chargingCable.cablePlugTransform.rotation = chargingCable.socketTransform.rotation;
            
            // Вызываем приватный метод через рефлексию (хак для теста)
            var plugInMethod = chargingCable.GetType().GetMethod("PlugIn", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (plugInMethod != null)
            {
                plugInMethod.Invoke(chargingCable, null);
                Debug.Log("✓ Кабель подключен!");
            }
            else
            {
                // Альтернатива - просто запускаем зарядку
                chargingSystem.TryStartCharging();
            }
        }
    }
    
    void ResetDay()
    {
        if (dayEventManager == null) return;
        
        foreach (var dayEvent in dayEventManager.todayEvents)
        {
            dayEvent.isCompleted = false;
        }
        
        Debug.Log("🔄 День сброшен. Все ивенты снова незавершены.");
    }
    
    void CalculateCableDistance()
    {
        if (chargingCable.cablePlugTransform != null && chargingCable.socketTransform != null)
        {
            cableToSocketDistance = Vector3.Distance(
                chargingCable.cablePlugTransform.position,
                chargingCable.socketTransform.position
            );
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 500), style);
        
        GUILayout.Label("=== ROBOT CHARGING DEBUG ===", new GUIStyle(GUI.skin.label) { 
            fontSize = 16, 
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        });
        
        GUILayout.Space(10);
        
        // Информация о дне
        if (dayEventManager != null)
        {
            GUILayout.Label($"День: {dayEventManager.currentDay}", new GUIStyle(GUI.skin.label) { 
                fontSize = 14,
                normal = { textColor = Color.yellow }
            });
            
            GUILayout.Label($"Прогресс: {dayEventManager.GetDayProgress()}");
            
            GUILayout.Space(5);
            GUILayout.Label("Ивенты дня:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            for (int i = 0; i < dayEventManager.todayEvents.Count; i++)
            {
                var evt = dayEventManager.todayEvents[i];
                string status = evt.isCompleted ? "✓" : "○";
                Color color = evt.isCompleted ? Color.green : Color.gray;
                
                GUILayout.Label($"  {status} {evt.eventName}", new GUIStyle(GUI.skin.label) { 
                    normal = { textColor = color }
                });
            }
            
            bool allComplete = dayEventManager.AreAllEventsCompleted();
            if (allComplete)
            {
                GUILayout.Label(">>> МОЖНО ИДТИ НА ЗАРЯДКУ! <<<", new GUIStyle(GUI.skin.label) { 
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.green }
                });
            }
        }
        
        GUILayout.Space(10);
        
        // Информация о кабеле
        if (showCableDistance && chargingCable != null)
        {
            GUILayout.Label("Статус кабеля:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            if (chargingCable.cablePlugTransform != null && chargingCable.socketTransform != null)
            {
                Color distColor = cableToSocketDistance < chargingCable.connectDistance ? Color.green : Color.red;
                GUILayout.Label($"  Расстояние до розетки: {cableToSocketDistance:F3}m", new GUIStyle(GUI.skin.label) { 
                    normal = { textColor = distColor }
                });
                GUILayout.Label($"  Требуется: < {chargingCable.connectDistance}m");
            }
        }
        
        GUILayout.Space(10);
        
        // Управление
        GUILayout.Label("=== УПРАВЛЕНИЕ ===", new GUIStyle(GUI.skin.label) { 
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        });
        
        GUILayout.Label($"[1-3] - Завершить ивент 1-3");
        GUILayout.Label($"[C] - Завершить ВСЕ ивенты");
        GUILayout.Label($"[P] - Симуляция подключения кабеля");
        GUILayout.Label($"[SPACE] - Принудительная зарядка");
        GUILayout.Label($"[R] - Сброс дня");
        
        GUILayout.EndArea();
    }
    
    void OnDrawGizmos()
    {
        if (!showCableDistance || chargingCable == null) return;
        
        if (chargingCable.cablePlugTransform != null && chargingCable.socketTransform != null)
        {
            // Линия от кабеля до розетки
            bool isClose = cableToSocketDistance < chargingCable.connectDistance;
            Gizmos.color = isClose ? gizmoReadyColor : gizmoNormalColor;
            Gizmos.DrawLine(chargingCable.cablePlugTransform.position, chargingCable.socketTransform.position);
            
            // Сфера радиуса подключения
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(chargingCable.socketTransform.position, chargingCable.connectDistance);
            
            // Точки
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(chargingCable.socketTransform.position, 0.02f);
            
            Gizmos.color = isClose ? Color.green : Color.red;
            Gizmos.DrawSphere(chargingCable.cablePlugTransform.position, 0.02f);
        }
    }
}