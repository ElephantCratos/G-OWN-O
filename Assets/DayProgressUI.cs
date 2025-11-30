using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using BNG;

public class DayProgressUI : MonoBehaviour
{
    [Header("References")]
    public DayEventManager dayEventManager;
    
    [Header("UI Elements")]
    public UnityEngine.UI.Text progressText; // Для обычного UI Text
    public TMPro.TextMeshProUGUI progressTextTMP; // Для TextMeshPro
    
    [Header("Settings")]
    public bool updateEveryFrame = true;
    public float updateInterval = 0.5f; // Если не каждый кадр
    
    private float lastUpdateTime;
    
    void Update()
    {
        if (updateEveryFrame)
        {
            UpdateUI();
        }
        else
        {
            if (Time.time - lastUpdateTime > updateInterval)
            {
                UpdateUI();
                lastUpdateTime = Time.time;
            }
        }
    }
    
    void UpdateUI()
    {
        if (dayEventManager == null) return;
        
        string text = dayEventManager.GetDayProgress();
        
        // Обновляем обычный Text
        if (progressText != null)
        {
            progressText.text = text;
        }
        
        // Обновляем TextMeshPro
        if (progressTextTMP != null)
        {
            progressTextTMP.text = text;
        }
    }
    
    // Можно вызвать вручную из других скриптов
    public void ForceUpdate()
    {
        UpdateUI();
    }
}