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
        public UnityEvent onEventComplete;
    }
    
    [Header("Day Management")]
    public int currentDay = 1;
    public List<DayEvent> todayEvents = new List<DayEvent>();
    
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
        
        // Сбрасываем все ивенты
        foreach (var dayEvent in todayEvents)
        {
            dayEvent.isCompleted = false;
        }
        
        OnNewDayStarted?.Invoke(currentDay);
        
        Debug.Log($"Начался день {currentDay}");
        
        // Здесь можно генерировать новые ивенты для нового дня
        GenerateDayEvents();
    }
    
    private void GenerateDayEvents()
    {
        // Логика генерации новых ивентов для дня
        // Можно добавлять случайные задачи, менять их количество и т.д.
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