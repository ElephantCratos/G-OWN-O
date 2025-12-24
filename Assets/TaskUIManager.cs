using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BNG;

public class TaskUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject taskUIPanel;
    public Transform taskCardContainer;
    public GameObject taskCardPrefab;
    
    [Header("Header Info")]
    public TextMeshProUGUI dayNumberText;
    public TextMeshProUGUI progressText;
    
    [Header("References")]
    public DayEventManager dayEventManager;
    public RatSpawner ratSpawner;
    public HoleSpawner holeSpawner;
    public ControlPanelMalfunction controlPanelMalfunction;
    public PinReplacementTask pinReplacementTask;
    public BatteryReplacementEvent batteryReplacementEvent;
    public GameOverManager gameOverManager; // НОВОЕ: Ссылка на GameOverManager для таймеров
    
    [Header("VR Input Settings")]
    [Tooltip("Кнопка на левом контроллере для открытия меню")]
    public ControllerBinding leftControllerButton = ControllerBinding.YButton;
    
    [Tooltip("Кнопка на правом контроллере для открытия меню")]
    public ControllerBinding rightControllerButton = ControllerBinding.BButton;
    
    [Tooltip("Использовать левый контроллер")]
    public bool useLeftController = true;
    
    [Tooltip("Использовать правый контроллер")]
    public bool useRightController = true;
    
    [Header("Settings")]
    public float updateInterval = 0.5f;
    
    private Dictionary<string, TaskCard> activeCards = new Dictionary<string, TaskCard>();
    private float updateTimer;
    private InputBridge input;
    
    private Dictionary<string, TaskInfo> taskTranslations = new Dictionary<string, TaskInfo>()
    {
        { "ClearRats", new TaskInfo("Уничтожить крыс", "🐀", new Color(0.8f, 0.2f, 0.2f)) },
        { "PatchHoles", new TaskInfo("Залатать пробоины", "🔧", new Color(1f, 0.5f, 0f)) },
        { "FixControls", new TaskInfo("Починить панель управления", "⚡", new Color(1f, 0.9f, 0.2f)) },
        { "ReplacePins", new TaskInfo("Заменить изношенные пины", "🔩", new Color(0.3f, 0.8f, 1f)) },
        { "ReplaceBattery", new TaskInfo("Заменить батарею", "🔋", new Color(0.2f, 0.8f, 0.3f)) }
    };
    
    void Start()
    {
        input = InputBridge.Instance;
        
        if (input == null)
        {
            Debug.LogError("TaskUIManager: InputBridge.Instance == null! Проверьте что BNG правильно настроен.");
        }
        else
        {
            Debug.Log("TaskUIManager: InputBridge найден успешно");
        }
        
        if (taskUIPanel != null)
        {
            taskUIPanel.SetActive(false);
            Debug.Log("TaskUIManager: UI панель скрыта при старте");
        }
        else
        {
            Debug.LogError("TaskUIManager: Task UI Panel не назначена!");
        }
        
        if (taskCardPrefab == null)
        {
            Debug.LogError("TaskUIManager: Task Card Prefab не назначен!");
        }
        
        if (taskCardContainer == null)
        {
            Debug.LogError("TaskUIManager: Task Card Container не назначен!");
        }
        
        // НОВОЕ: Автопоиск GameOverManager
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<GameOverManager>();
            if (gameOverManager == null)
            {
                Debug.LogWarning("TaskUIManager: GameOverManager не найден! Таймеры не будут отображаться.");
            }
        }
        
        if (dayEventManager != null)
        {
            dayEventManager.OnNewDayStarted.AddListener(OnNewDay);
        }
        else
        {
            Debug.LogWarning("TaskUIManager: Day Event Manager не назначен!");
        }
        
        Debug.Log($"TaskUIManager инициализирован. Кнопки: Left={leftControllerButton}, Right={rightControllerButton}");
    }
    
    private bool leftButtonPressed = false;
    private bool rightButtonPressed = false;
    
    void Update()
    {
        if (input == null) return;
        
        bool currentLeftState = useLeftController && input.GetControllerBindingValue(leftControllerButton);
        bool currentRightState = useRightController && input.GetControllerBindingValue(rightControllerButton);
        
        if (currentLeftState && !leftButtonPressed)
        {
            ToggleUI();
        }
        leftButtonPressed = currentLeftState;
        
        if (currentRightState && !rightButtonPressed)
        {
            ToggleUI();
        }
        rightButtonPressed = currentRightState;
        
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleUI();
        }
        
        if (taskUIPanel != null && taskUIPanel.activeSelf)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateTaskProgress();
            }
        }
    }
    
    void OnGUI()
    {
        if (input == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"Left Button: {input.GetControllerBindingValue(leftControllerButton)}");
        GUILayout.Label($"Right Button: {input.GetControllerBindingValue(rightControllerButton)}");
        GUILayout.Label($"UI Active: {(taskUIPanel != null ? taskUIPanel.activeSelf : false)}");
        GUILayout.Label("Press Tab to toggle (editor)");
        GUILayout.EndArea();
    }
    
    public void ToggleUI()
    {
        if (taskUIPanel != null)
        {
            bool newState = !taskUIPanel.activeSelf;
            taskUIPanel.SetActive(newState);
            
            Debug.Log($"TaskUIManager: UI панель {(newState ? "открыта" : "закрыта")}");
            
            if (newState)
            {
                RefreshUI();
            }
        }
        else
        {
            Debug.LogError("TaskUIManager: Task UI Panel == null!");
        }
    }
    
    public void ShowUI()
    {
        if (taskUIPanel != null)
        {
            taskUIPanel.SetActive(true);
            RefreshUI();
            Debug.Log("TaskUIManager: UI показан");
        }
    }
    
    public void HideUI()
    {
        if (taskUIPanel != null)
        {
            taskUIPanel.SetActive(false);
            Debug.Log("TaskUIManager: UI скрыт");
        }
    }
    
    private void OnNewDay(int dayNumber)
    {
        if (taskUIPanel != null && taskUIPanel.activeSelf)
        {
            RefreshUI();
        }
    }
    
    public void RefreshUI()
    {
        if (dayEventManager == null)
        {
            Debug.LogWarning("TaskUIManager: Day Event Manager не назначен!");
            return;
        }
        
        if (dayNumberText != null)
        {
            dayNumberText.text = $"День {dayEventManager.currentDay}";
        }
        
        UpdateProgressText();
        ClearAllCards();
        
        Debug.Log($"TaskUIManager: Создаём карточки для {dayEventManager.todayEvents.Count} заданий");
        
        foreach (var dayEvent in dayEventManager.todayEvents)
        {
            CreateTaskCard(dayEvent);
        }
    }
    
    private void UpdateProgressText()
    {
        if (progressText != null && dayEventManager != null)
        {
            int completed = 0;
            int total = dayEventManager.todayEvents.Count;
            
            foreach (var dayEvent in dayEventManager.todayEvents)
            {
                if (dayEvent.isCompleted) completed++;
            }
            
            progressText.text = $"Выполнено: {completed}/{total}";
            
            if (completed == total && total > 0)
            {
                progressText.color = Color.green;
            }
            else
            {
                progressText.color = Color.white;
            }
        }
    }
    
    private void UpdateTaskProgress()
    {
        UpdateProgressText();
        
        foreach (var kvp in activeCards)
        {
            string eventName = kvp.Key;
            TaskCard card = kvp.Value;
            
            if (card == null) continue;
            
            bool isCompleted = IsEventCompleted(eventName);
            string progress = GetEventProgress(eventName);
            
            card.UpdateProgress(progress, isCompleted);
            
            // НОВОЕ: Обновляем таймер для карточки
            UpdateCardTimer(eventName, card);
        }
    }
    
    // НОВОЕ: Обновление таймера для конкретной карточки
    private void UpdateCardTimer(string eventName, TaskCard card)
    {
        if (gameOverManager == null) return;
        
        TimerInfo timerInfo = new TimerInfo(-1, 0);
        
        switch (eventName)
        {
            case "FixControls":
                timerInfo = gameOverManager.GetControlTimer();
                break;
                
            case "ReplaceBattery":
                timerInfo = gameOverManager.GetBatteryTimer();
                break;
        }
        
        if (timerInfo.isActive)
        {
            card.UpdateTimer(timerInfo.currentTime, timerInfo.maxTime);
        }
        else
        {
            card.UpdateTimer(-1, 0);
        }
    }
    
    private void CreateTaskCard(DayEventManager.DayEvent dayEvent)
    {
        if (taskCardPrefab == null)
        {
            Debug.LogError("TaskUIManager: Task Card Prefab не назначен!");
            return;
        }
        
        if (taskCardContainer == null)
        {
            Debug.LogError("TaskUIManager: Task Card Container не назначен!");
            return;
        }
        
        GameObject cardObj = Instantiate(taskCardPrefab, taskCardContainer);
        TaskCard card = cardObj.GetComponent<TaskCard>();
        
        if (card != null)
        {
            TaskInfo info = GetTaskInfo(dayEvent.eventName);
            string progress = GetEventProgress(dayEvent.eventName);
            
            card.Setup(dayEvent.eventName, info.displayName, info.icon, info.color, dayEvent.isCompleted, progress);
            activeCards[dayEvent.eventName] = card;
            
            Debug.Log($"TaskUIManager: Создана карточка для {dayEvent.eventName}");
        }
        else
        {
            Debug.LogError($"TaskUIManager: У префаба карточки нет компонента TaskCard!");
        }
    }
    
    private TaskInfo GetTaskInfo(string eventName)
    {
        if (taskTranslations.ContainsKey(eventName))
        {
            return taskTranslations[eventName];
        }
        return new TaskInfo(eventName, "📋", Color.white);
    }
    
    private string GetEventProgress(string eventName)
    {
        switch (eventName)
        {
            case "ClearRats":
                if (ratSpawner != null)
                {
                    return $"Убито: {ratSpawner.killedRats}/{ratSpawner.ratsToKill}";
                }
                break;
                
            case "PatchHoles":
                if (holeSpawner != null)
                {
                    return $"Заварено: {holeSpawner.patchedHoles}/{holeSpawner.holesToPatch}";
                }
                break;
                
            case "FixControls":
                if (controlPanelMalfunction != null)
                {
                    if (controlPanelMalfunction.IsCurrentlyFixed())
                    {
                        return "Параметры в норме";
                    }
                    return "Требуется настройка";
                }
                break;
                
            case "ReplacePins":
                if (pinReplacementTask != null)
                {
                    return pinReplacementTask.GetProgressText();
                }
                break;
                
            case "ReplaceBattery":
                if (batteryReplacementEvent != null)
                {
                    return batteryReplacementEvent.GetStatus();
                }
                break;
        }
        
        return "Ожидание...";
    }
    
    private bool IsEventCompleted(string eventName)
    {
        if (dayEventManager == null) return false;
        
        foreach (var dayEvent in dayEventManager.todayEvents)
        {
            if (dayEvent.eventName == eventName)
            {
                return dayEvent.isCompleted;
            }
        }
        return false;
    }
    
    private void ClearAllCards()
    {
        foreach (Transform child in taskCardContainer)
        {
            Destroy(child.gameObject);
        }
        activeCards.Clear();
    }
}

[System.Serializable]
public class TaskInfo
{
    public string displayName;
    public string icon;
    public Color color;
    
    public TaskInfo(string name, string ico, Color col)
    {
        displayName = name;
        icon = ico;
        color = col;
    }
}