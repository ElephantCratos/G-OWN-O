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
    public int maxDays = 7; // Максимальное количество дней
    public List<DayEvent> todayEvents = new List<DayEvent>();
    
    [Header("Event Spawners")]
    public RatSpawner ratSpawner;
    public HoleSpawner holeSpawner;
    public ControlPanelMalfunction controlPanelMalfunction;
    public BatteryReplacementEvent batteryReplacementEvent;
    public PinReplacementTask pinReplacementTask;
    
    [Header("Events")]
    public UnityEvent OnDayComplete;
    public UnityEvent<int> OnNewDayStarted;
    public UnityEvent OnGameWon; // НОВОЕ: Событие победы
    
    private bool isGameWon = false;
    
    public bool AreAllEventsCompleted()
    {
        // НОВОЕ: В первый день нет событий, поэтому день всегда завершён
        if (currentDay == 1)
        {
            return true;
        }
        
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
        // Проверка на победу ПЕРЕД началом нового дня
        if (currentDay >= maxDays)
        {
            TriggerVictory();
            return;
        }
        
        if (controlPanelMalfunction != null)
            controlPanelMalfunction.EndMalfunctionEvent();
    
        if (batteryReplacementEvent != null)
            batteryReplacementEvent.ResetEvent();
    
        currentDay++;
        
        // Очищаем старые ивенты
        todayEvents.Clear();
        
        OnNewDayStarted?.Invoke(currentDay);
        
        Debug.Log($"═══════════════════════════");
        Debug.Log($"   НАЧАЛСЯ ДЕНЬ {currentDay}");
        Debug.Log($"═══════════════════════════");
        
        // НОВОЕ: В первый день не генерируем события
        if (currentDay == 1)
        {
            Debug.Log("🎓 ОБУЧАЮЩИЙ ДЕНЬ - Без заданий");
            Debug.Log("Осмотритесь, изучите управление и отправляйтесь на зарядку!");
        }
        else
        {
            // Генерируем новые ивенты для нового дня
            GenerateDayEvents();
            
            // Запускаем все ивенты
            StartAllEvents();
        }
    }
    
    private void TriggerVictory()
    {
        if (isGameWon) return;
        
        isGameWon = true;
        
        Debug.Log($"═══════════════════════════");
        Debug.Log($"       🏆 ПОБЕДА! 🏆");
        Debug.Log($"═══════════════════════════");
        Debug.Log($"Вы продержались {maxDays} дней!");
        Debug.Log($"Поздравляем с успешным завершением миссии!");
        Debug.Log($"═══════════════════════════");
        
        OnGameWon?.Invoke();
        
        // Останавливаем все системы
        StopAllSystems();
    }
    
    private void StopAllSystems()
    {
        if (ratSpawner != null)
            ratSpawner.StopSpawning();
        
        if (holeSpawner != null)
            holeSpawner.StopHoleEvent();
        
        if (controlPanelMalfunction != null)
            controlPanelMalfunction.ForceStopMalfunction();
    }
    
    private void GenerateDayEvents()
    {
        // НОВОЕ: В первый день не генерируем события
        if (currentDay == 1)
        {
            return;
        }
        
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
        
        // 40% шанс ивента с поломкой панели управления
        if (Random.value > 0.6f && controlPanelMalfunction != null)
        {
            DayEvent controlEvent = new DayEvent
            {
                eventName = "FixControls",
                isCompleted = false
            };
            todayEvents.Add(controlEvent);
        }

        // 40% шанс ивента с заменой пинов
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
            int randomEvent = Random.Range(0, 4);
            
            if (randomEvent == 0 && ratSpawner != null)
            {
                todayEvents.Add(new DayEvent { eventName = "ClearRats" });
            }
            else if (randomEvent == 1 && holeSpawner != null)
            {
                todayEvents.Add(new DayEvent { eventName = "PatchHoles" });
            }
            else if (randomEvent == 2 && controlPanelMalfunction != null)
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
    
    public string GetDayProgress()
    {
        // НОВОЕ: Специальное сообщение для первого дня
        if (currentDay == 1)
        {
            return $"День {currentDay}/{maxDays}: 🎓 Обучающий день";
        }
        
        int completed = 0;
        foreach (var dayEvent in todayEvents)
        {
            if (dayEvent.isCompleted) completed++;
        }
        
        return $"День {currentDay}/{maxDays}: {completed}/{todayEvents.Count} задач выполнено";
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
        Debug.Log($"[DayEventManager] AddEvent('{eventName}') вызван из:\n{System.Environment.StackTrace}");
        
        // НОВОЕ: Не добавляем события в первый день
        if (currentDay == 1)
        {
            Debug.Log($"⚠️ Попытка добавить событие '{eventName}' в обучающий день - игнорируется");
            return;
        }
        
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
    
    // НОВОЕ: Проверка на победу
    public bool IsGameWon()
    {
        return isGameWon;
    }
    
    // НОВОЕ: Проверка на первый день
    public bool IsFirstDay()
    {
        return currentDay == 1;
    }
}