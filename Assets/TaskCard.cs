using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskCard : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;
    public Image iconBackground;
    public TextMeshProUGUI iconText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI progressText;
    public Image statusIcon;
    
    [Header("Status Sprites")]
    public Sprite completedSprite;
    public Sprite inProgressSprite;
    
    [Header("Colors")]
    public Color completedColor = new Color(0.2f, 0.8f, 0.3f);
    public Color inProgressColor = new Color(0.3f, 0.3f, 0.3f);
    public Color completedBgColor = new Color(0.1f, 0.4f, 0.15f, 0.8f);
    public Color inProgressBgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    private string eventName;
    private bool isCompleted;
    
    public void Setup(string evtName, string title, string icon, Color iconColor, bool completed, string progress = "")
    {
        eventName = evtName;
        isCompleted = completed;
        
        if (titleText != null)
        {
            titleText.text = title;
        }
        
        if (iconText != null)
        {
            iconText.text = icon;
        }
        
        if (iconBackground != null)
        {
            iconBackground.color = iconColor;
        }
        
        UpdateProgress(progress, completed);
    }
    
    public void UpdateProgress(string progress, bool completed)
    {
        isCompleted = completed;
        
        if (progressText != null)
        {
            progressText.text = progress;
        }
        
        // Обновляем визуальное состояние
        if (backgroundImage != null)
        {
            backgroundImage.color = completed ? completedBgColor : inProgressBgColor;
        }
        
        if (statusIcon != null)
        {
            if (completed && completedSprite != null)
            {
                statusIcon.sprite = completedSprite;
                statusIcon.color = completedColor;
            }
            else if (!completed && inProgressSprite != null)
            {
                statusIcon.sprite = inProgressSprite;
                statusIcon.color = inProgressColor;
            }
        }
        
        // Добавляем визуальный эффект для завершённых заданий
        if (titleText != null)
        {
            titleText.color = completed ? completedColor : Color.white;
        }
    }
}