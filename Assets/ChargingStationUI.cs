using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChargingStationUI : MonoBehaviour
{
    [Header("References")]
    public DayEventManager dayEventManager;
    public ChargingStation chargingStation;
    
    [Header("UI Elements")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI tasksText;
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    
    [Header("Colors")]
    public Color completedColor = Color.green;
    public Color incompleteColor = Color.red;
    public Color chargingColor = Color.yellow;
    
    private float notificationTimer = 0;
    private float notificationDuration = 3f;
    
    void Start()
    {
        // Подписываемся на события станции зарядки
        if (chargingStation != null)
        {
            chargingStation.OnTasksNotCompleted.AddListener(OnTasksNotCompleted);
            chargingStation.OnChargingStarted.AddListener(OnChargingStarted);
            chargingStation.OnDayChanged.AddListener(OnNewDayStarted);
        }
        
        // Скрываем панель уведомлений
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
    
    void Update()
    {
        UpdateStatusText();
        UpdateTasksText();
        
        // Скрываем уведомление через время
        if (notificationPanel != null && notificationPanel.activeSelf)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0)
            {
                notificationPanel.SetActive(false);
            }
        }
    }
    
    private void UpdateStatusText()
    {
        if (statusText == null) return;
        
        if (chargingStation != null && chargingStation.IsCharging())
        {
            statusText.text = "ЗАРЯДКА...";
            statusText.color = chargingColor;
        }
        else if (chargingStation != null && chargingStation.IsPluggedIn())
        {
            statusText.text = "ПОДКЛЮЧЕНО\nОтключите кабель";
            statusText.color = completedColor;
        }
        else if (dayEventManager != null && dayEventManager.AreAllEventsCompleted())
        {
            statusText.text = "ЗАДАНИЯ ВЫПОЛНЕНЫ\nМожно подзарядиться";
            statusText.color = completedColor;
        }
        else
        {
            statusText.text = "ЗАДАНИЯ НЕ ВЫПОЛНЕНЫ";
            statusText.color = incompleteColor;
        }
    }
    
    private void UpdateTasksText()
    {
        if (tasksText == null || dayEventManager == null) return;
        
        tasksText.text = dayEventManager.GetDayProgress();
    }
    
    public void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            notificationTimer = notificationDuration;
        }
    }
    
    // Методы для вызова из UnityEvents или напрямую
    public void OnTasksNotCompleted()
    {
        ShowNotification("Сначала завершите все задания!");
    }
    
    public void OnChargingStarted()
    {
        ShowNotification("Зарядка началась...");
    }
    
    public void OnNewDayStarted()
    {
        if (dayEventManager != null)
        {
            ShowNotification($"Начался день {dayEventManager.currentDay}");
        }
    }
}