using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskCard : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI taskNameText;
    public TextMeshProUGUI iconText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI timerText; // НОВОЕ: Текст таймера
    public Image backgroundImage;
    public Image checkmarkImage;
    public GameObject timerPanel; // НОВОЕ: Панель с таймером
    public Image timerWarningIcon; // НОВОЕ: Иконка предупреждения
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color completedColor = Color.green;
    public Color criticalTimerColor = Color.red; // НОВОЕ: Цвет критического таймера
    public Color warningTimerColor = Color.yellow; // НОВОЕ: Цвет предупреждения
    
    private string eventName;
    private bool isCompleted;
    private float currentTimer = -1f; // -1 означает что таймера нет
    private float maxTimer = 0f;
    
    public void Setup(string name, string displayName, string icon, Color color, bool completed, string progress)
    {
        eventName = name;
        isCompleted = completed;
        
        if (taskNameText != null)
            taskNameText.text = displayName;
            
        if (iconText != null)
            iconText.text = icon;
            
        if (progressText != null)
            progressText.text = progress;
            
        if (backgroundImage != null)
            backgroundImage.color = color;
            
        UpdateCheckmark();
        
        // НОВОЕ: Скрываем таймер по умолчанию
        if (timerPanel != null)
            timerPanel.SetActive(false);
            
        if (timerWarningIcon != null)
            timerWarningIcon.gameObject.SetActive(false);
    }
    
    public void UpdateProgress(string progress, bool completed)
    {
        if (progressText != null)
            progressText.text = progress;
            
        isCompleted = completed;
        UpdateCheckmark();
    }
    
    // НОВОЕ: Обновление таймера
    public void UpdateTimer(float currentTime, float maxTime)
    {
        currentTimer = currentTime;
        maxTimer = maxTime;
        
        if (currentTimer < 0)
        {
            // Таймера нет
            if (timerPanel != null)
                timerPanel.SetActive(false);
            return;
        }
        
        // Показываем панель таймера
        if (timerPanel != null)
            timerPanel.SetActive(true);
        
        // Обновляем текст
        if (timerText != null)
        {
            float remaining = maxTimer - currentTimer;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            
            timerText.text = $"⏱️ {minutes:00}:{seconds:00}";
            
            // Меняем цвет в зависимости от оставшегося времени
            if (remaining <= 30f)
            {
                timerText.color = criticalTimerColor;
                if (timerWarningIcon != null)
                    timerWarningIcon.gameObject.SetActive(true);
            }
            else if (remaining <= 60f)
            {
                timerText.color = warningTimerColor;
                if (timerWarningIcon != null)
                    timerWarningIcon.gameObject.SetActive(false);
            }
            else
            {
                timerText.color = normalColor;
                if (timerWarningIcon != null)
                    timerWarningIcon.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateCheckmark()
    {
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(isCompleted);
        }
        
        if (backgroundImage != null && isCompleted)
        {
            backgroundImage.color = completedColor;
        }
    }
    
    public string GetEventName()
    {
        return eventName;
    }
    
    public bool IsCompleted()
    {
        return isCompleted;
    }
}