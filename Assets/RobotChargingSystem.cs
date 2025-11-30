using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using BNG;
public class RobotChargingSystem : MonoBehaviour
{
    [Header("References")]
    public ChargingCable chargingCable;
    public DayEventManager dayEventManager;
    public ScreenFader screenFader;
    public SmoothLocomotion playerLocomotion;
    
    [Header("Charging Settings")]
    public float chargingDuration = 3f;
    public float fadeToBlackDuration = 2f;
    public float fadeFromBlackDuration = 2f;
    
    [Header("Audio")]
    public AudioSource chargingSound;
    public AudioClip plugInSound;
    public AudioClip chargingLoopSound;
    public AudioClip wakeUpSound;
    
    [Header("Events")]
    public UnityEvent OnChargingStart;
    public UnityEvent OnNewDayStart;
    
    private bool isCharging = false;
    
    public void TryStartCharging()
    {
        // Проверяем, все ли ивенты дня завершены
        if (!dayEventManager.AreAllEventsCompleted())
        {
            ShowMessage("Системная ошибка: день не завершен. Задачи не выполнены.");
            return;
        }
        
        if (isCharging) return;
        
        StartCoroutine(ChargingSequence());
    }
    
    private IEnumerator ChargingSequence()
    {
        isCharging = true;
        
        // Блокируем движение игрока
        if (playerLocomotion != null)
        {
            playerLocomotion.enabled = false;
        }
        
        // Звук подключения
        if (chargingSound != null && plugInSound != null)
        {
            chargingSound.PlayOneShot(plugInSound);
        }
        
        OnChargingStart?.Invoke();
        
        yield return new WaitForSeconds(0.5f);
        
        // Запуск звука зарядки
        if (chargingSound != null && chargingLoopSound != null)
        {
            chargingSound.clip = chargingLoopSound;
            chargingSound.loop = true;
            chargingSound.Play();
        }
        
        // Затемнение экрана
        if (screenFader != null)
        {
            screenFader.DoFadeIn();
        }
        
        yield return new WaitForSeconds(fadeToBlackDuration);
        
        // Процесс "зарядки" - здесь происходят все изменения
        PerformNightChanges();
        
        yield return new WaitForSeconds(chargingDuration);
        
        // Останавливаем звук зарядки
        if (chargingSound != null)
        {
            chargingSound.Stop();
        }
        
        // Просыпание
        if (chargingSound != null && wakeUpSound != null)
        {
            chargingSound.PlayOneShot(wakeUpSound);
        }
        
        if (screenFader != null)
        {
            screenFader.DoFadeOut();
        }
        
        yield return new WaitForSeconds(fadeFromBlackDuration);
        
        // Разблокируем движение
        if (playerLocomotion != null)
        {
            playerLocomotion.enabled = true;
        }
        
        OnNewDayStart?.Invoke();
        isCharging = false;
        
        // Новый день начался
        dayEventManager.StartNewDay();
    }
    
    private void PerformNightChanges()
    {
        // Здесь происходят все изменения за ночь:
        // - Смена времени суток
        // - Спавн новых объектов/врагов
        // - Изменения в мире
        // - Обновление квестов
        Debug.Log("Выполняются ночные изменения...");
    }
    
    private void ShowMessage(string message)
    {
        Debug.Log(message);
        // Здесь можно показать UI сообщение игроку
    }
}

