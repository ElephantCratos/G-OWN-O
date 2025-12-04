using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using BNG;

public class DayEventManager : MonoBehaviour
{
    [System.Serializable]
    public class DayEvent
    {
        public string eventName;
        public bool isCompleted = false;
        public UnityEvent onEventStart;
        public UnityEvent onEventComplete;
    }
    
    [Header("Day Management")]
    public int currentDay = 1;
    public List<DayEvent> todayEvents = new List<DayEvent>();
    
    [Header("Event Spawners")]
    public RatSpawner ratSpawner;
    public HoleSpawner holeSpawner; // Добавили spawner пробоин
    public ControlPanelMalfunction controlPanelMalfunction;

    public BatteryReplacementEvent batteryReplacementEvent;

    public PinReplacementTask pinReplacementTask;
    
    [Header("Events")]
    public UnityEvent OnDayComplete;
    public UnityEvent<int> OnNewDayStarted;
    
    public bool AreAllEventsCompleted()
    {
        foreach (var dayEvent in todayEvents)
        {
            if (!dayEvent.isCompleted)
            {
                return false;
            }
        }
        return todayEvents.Count > 0;
    }
    
    public void CompleteEvent(string eventName)
    {
        foreach (var dayEvent in todayEvents)
        {
            if (dayEvent.eventName == eventName && !dayEvent.isCompleted)
            {
                dayEvent.isCompleted = true;
                dayEvent.onEventComplete?.Invoke();
                
                Debug.Log($"Ивент '{eventName}' завершен!");
                
                if (AreAllEventsCompleted())
                {
                    OnDayComplete?.Invoke();
                    Debug.Log("Все ивенты дня завершены! Можно идти на зарядку.");
                }
                
                break;
            }
        }
    }
    
    public void StartNewDay()
    {
        if (controlPanelMalfunction != null)
        controlPanelMalfunction.EndMalfunctionEvent();
    
        if (batteryReplacementEvent != null)
        batteryReplacementEvent.ResetEvent();
    
        currentDay++;
        
        // Очищаем старые ивенты
        todayEvents.Clear();
        
        OnNewDayStarted?.Invoke(currentDay);
        
        Debug.Log($"Начался день {currentDay}");
        
        // Генерируем новые ивенты для нового дня
        GenerateDayEvents();
        
        // Запускаем все ивенты
        StartAllEvents();
    }
    
     private void GenerateDayEvents()
    {
        // 50% шанс ивента с крысами
        if (Random.value > 0.5f && ratSpawner != null)
        {
            DayEvent ratEvent = new DayEvent
            {
                eventName = "ClearRats",
                isCompleted = false
            };
            todayEvents.Add(ratEvent);
        }
        
        // 50% шанс ивента с пробоинами
        if (Random.value > 0.5f && holeSpawner != null)
        {
            DayEvent holeEvent = new DayEvent
            {
                eventName = "PatchHoles",
                isCompleted = false
            };
            todayEvents.Add(holeEvent);
        }
        
        // === ДОБАВЛЕНО: 40% шанс ивента с поломкой панели управления ===
        if (Random.value > 0.6f && controlPanelMalfunction != null)
        {
            DayEvent controlEvent = new DayEvent
            {
                eventName = "FixControls",
                isCompleted = false
            };
            todayEvents.Add(controlEvent);
        }

        if (Random.value > 0.6f && pinReplacementTask != null)
        {
            DayEvent pinEvent = new DayEvent
            {
                eventName = "ReplacePins",
                isCompleted = false
            };
            todayEvents.Add(pinEvent);
        }
        
        // Если не выпало ни одного ивента, добавим хотя бы один
        if (todayEvents.Count == 0)
        {
            // Случайно выбираем какой ивент добавить
            int randomEvent = Random.Range(0, 3);
            
            if (randomEvent == 0 && ratSpawner != null)
            {
                todayEvents.Add(new DayEvent { eventName = "ClearRats" });
            }
            else if (randomEvent == 1 && holeSpawner != null)
            {
                todayEvents.Add(new DayEvent { eventName = "PatchHoles" });
            }
            else if (controlPanelMalfunction != null)
            {
                todayEvents.Add(new DayEvent { eventName = "FixControls" });
            }
            else if (pinReplacementTask != null)
            {
                todayEvents.Add(new DayEvent { eventName = "ReplacePins" });
            }
        }
        
        Debug.Log($"Сгенерировано ивентов: {todayEvents.Count}");
    }
    
    private void StartAllEvents()
    {
        foreach (var dayEvent in todayEvents)
        {
            switch (dayEvent.eventName)
            {
                case "ClearRats":
                    if (ratSpawner != null)
                    {
                        ratSpawner.StartSpawning();
                        Debug.Log("Запущен ивент: ClearRats");
                    }
                    break;
                    
                case "PatchHoles":
                    if (holeSpawner != null)
                    {
                        holeSpawner.StartHoleEvent();
                        Debug.Log("Запущен ивент: PatchHoles");
                    }
                    break;
                
                // === ДОБАВЛЕНО ===
                case "FixControls":
                    if (controlPanelMalfunction != null)
                    {
                        controlPanelMalfunction.StartMalfunction();
                        Debug.Log("Запущен ивент: FixControls - Авария систем!");
                    }
                    break;
                case "ReplacePins":
                    if (pinReplacementTask != null)
                    {
                        pinReplacementTask.StartReplacementTask();
                        Debug.Log("Запущен ивент: ReplacePins - Замена изношенных пинов!");
                    }
                    break;
            }
            
            dayEvent.onEventStart?.Invoke();
        }
    }
    
    // Метод для проверки состояния ивентов (для UI)
    public string GetDayProgress()
    {
        int completed = 0;
        foreach (var dayEvent in todayEvents)
        {
            if (dayEvent.isCompleted) completed++;
        }
        
        return $"День {currentDay}: {completed}/{todayEvents.Count} задач выполнено";
    }

    public void UncompleteEvent(string eventName)
{
    foreach (var dayEvent in todayEvents)
    {
        if (dayEvent.eventName == eventName && dayEvent.isCompleted)
        {
            dayEvent.isCompleted = false;
            
            Debug.Log($"Ивент '{eventName}' снова активен — параметры сбились!");
            break;
        }
    }
}
public void AddEvent(string eventName)
{
    // === ДЕБАГ: кто вызывает? ===
    Debug.Log($"[DayEventManager] AddEvent('{eventName}') вызван из:\n{System.Environment.StackTrace}");
    
    foreach (var existing in todayEvents)
    {
        if (existing.eventName == eventName)
        {
            Debug.Log($"Ивент '{eventName}' уже существует");
            return;
        }
    }
    
    DayEvent newEvent = new DayEvent
    {
        eventName = eventName,
        isCompleted = false
    };
    
    todayEvents.Add(newEvent);
    
    Debug.Log($"Добавлен динамический ивент: {eventName}");
}

/// <summary>
/// Удаляет ивент из списка (если нужно полностью убрать)
/// </summary>
public void RemoveEvent(string eventName)
{
    todayEvents.RemoveAll(e => e.eventName == eventName);
    Debug.Log($"Ивент '{eventName}' удалён");
}
public bool IsPinReplacementActive()
    {
        foreach (var dayEvent in todayEvents)
        {
            if (dayEvent.eventName == "ReplacePins")
            {
                return true;
            }
        }
        return false;
    }
}