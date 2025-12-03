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
        // Пример генерации случайных ивентов
        
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
        
        // Если не выпало ни одного ивента, добавим хотя бы один
        if (todayEvents.Count == 0 && ratSpawner != null)
        {
            DayEvent ratEvent = new DayEvent
            {
                eventName = "ClearRats",
                isCompleted = false
            };
            todayEvents.Add(ratEvent);
        }
        
        Debug.Log($"Сгенерировано ивентов: {todayEvents.Count}");
    }
    
    private void StartAllEvents()
    {
        foreach (var dayEvent in todayEvents)
        {
            // Запускаем ивент в зависимости от его имени
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
            }
            
            // Вызываем UnityEvent если он настроен в инспекторе
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
}