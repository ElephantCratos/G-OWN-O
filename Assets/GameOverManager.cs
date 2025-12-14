using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BNG
{
    /// <summary>
    /// Управляет критериями поражения и состоянием корабля
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        [Header("=== СИСТЕМЫ КОРАБЛЯ ===")]
        
        [Header("Герметичность корпуса")]
        [Range(0f, 100f)]
        public float hullIntegrity = 100f;
        public float integrityLossPerHole = 8f; // -8% за каждую дыру
        public float criticalIntegrityLevel = 20f; // Game Over если <20%
        
        [Header("Здоровье игрока")]
        public float playerHealth = 100f;
        public float maxPlayerHealth = 100f;
        public float ratDamagePerSecond = 5f;
        public float healthRegenerationRate = 2f; // +2 HP/сек без крыс
        
        [Header("Потеря управления")]
        public float controlMalfunctionTimer = 0f;
        public float maxControlMalfunctionTime = 300f; // 5 минут
        
        [Header("Критическая ситуация с батареей")]
        public float batteryEmptyTimer = 0f;
        public float maxBatteryEmptyTime = 30f; // 30 секунд до новых дыр
        public float holeSpawnInterval = 5f; // Новая дыра каждые 5 сек
        private float nextHoleSpawnTime = 0f;
        
        [Header("Критическая ситуация с пинами")]
        public float oxygenLevel = 100f;
        public float oxygenDepletionRate = 5f; // -5% кислорода в секунду
        public float criticalOxygenLevel = 0f;
        private bool isOxygenCritical = false;
        
        [Header("=== ССЫЛКИ НА КОМПОНЕНТЫ ===")]
        public HoleSpawner holeSpawner;
        public RatSpawner ratSpawner;
        public ControlPanelMalfunction controlPanelMalfunction;
        public BatteryReplacementEvent batteryReplacementEvent;
        public PinReplacementTask pinReplacementTask;
        public AttachModel attachModel;
        public DayEventManager dayEventManager;
        
        [Header("=== UI И ЭФФЕКТЫ ===")]
        public UnityEvent<float> OnHullIntegrityChanged; // Передаёт %
        public UnityEvent<float> OnPlayerHealthChanged; // Передаёт HP
        public UnityEvent<float> OnOxygenLevelChanged; // Передаёт %
        public UnityEvent<string> OnGameOver; // Передаёт причину
        public UnityEvent OnCriticalWarning; // Критическое состояние
        
        [Header("Визуальные эффекты")]
        public GameObject criticalHullEffects; // Красные огни, сирена
        public GameObject criticalHealthEffects; // Красный экран
        public GameObject criticalOxygenEffects; // Синий туман
        
        [Header("Настройки Game Over")]
        public string gameOverSceneName = "MainMenu"; // Сцена для перезагрузки
        public float gameOverDelay = 3f; // Задержка перед Game Over
        
        private bool isGameOver = false;
        
        void Update()
        {
            if (isGameOver) return;
            
            // Обновляем все системы
            UpdateHullIntegrity();
            UpdatePlayerHealth();
            UpdateControlMalfunction();
            UpdateBatteryEmergency();
            UpdateOxygenSystem();
            
            // Проверяем условия поражения
            CheckGameOverConditions();
        }
        
        #region Hull Integrity System
        
        void UpdateHullIntegrity()
        {
            if (holeSpawner == null) return;
            
            // Считаем активные дыры
            int activeHoles = holeSpawner.GetActiveHolesCount();
            
            // Целевая герметичность = 100% - (количество_дыр × потеря_на_дыру)
            float targetIntegrity = 100f - (activeHoles * integrityLossPerHole);
            targetIntegrity = Mathf.Clamp(targetIntegrity, 0f, 100f);
            
            // Плавно меняем герметичность
            hullIntegrity = Mathf.Lerp(hullIntegrity, targetIntegrity, Time.deltaTime * 2f);
            
            OnHullIntegrityChanged?.Invoke(hullIntegrity);
            
            // Визуальные эффекты при критической герметичности
            if (criticalHullEffects != null)
            {
                criticalHullEffects.SetActive(hullIntegrity < 40f);
            }
        }
        
        #endregion
        
        #region Player Health System
        
        void UpdatePlayerHealth()
        {
            if (ratSpawner == null) return;
            
            // Считаем живых крыс рядом с игроком (упрощённо - все живые крысы)
            int aliveRats = ratSpawner.GetAliveRatsCount();
            
            if (aliveRats > 0)
            {
                // Крысы атакуют - наносим урон
                float damage = ratDamagePerSecond * Time.deltaTime * Mathf.Min(aliveRats, 10); // Макс 10 крыс одновременно
                playerHealth -= damage;
            }
            else
            {
                // Нет крыс - восстанавливаем здоровье
                playerHealth += healthRegenerationRate * Time.deltaTime;
            }
            
            playerHealth = Mathf.Clamp(playerHealth, 0f, maxPlayerHealth);
            
            OnPlayerHealthChanged?.Invoke(playerHealth);
            
            // Визуальные эффекты при низком здоровье
            if (criticalHealthEffects != null)
            {
                criticalHealthEffects.SetActive(playerHealth < 30f);
            }
        }
        
        #endregion
        
        #region Control Malfunction System
        
        void UpdateControlMalfunction()
        {
            if (controlPanelMalfunction == null) return;
            
            if (controlPanelMalfunction.IsMalfunctionActive() && !controlPanelMalfunction.IsCurrentlyFixed())
            {
                controlMalfunctionTimer += Time.deltaTime;
                
                // Предупреждение на 4 минуте
                if (controlMalfunctionTimer >= 240f && controlMalfunctionTimer < 240.5f)
                {
                    OnCriticalWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Осталась 1 минута до потери управления!");
                }
            }
            else
            {
                // Параметры в норме - обнуляем таймер
                controlMalfunctionTimer = 0f;
            }
        }
        
        #endregion
        
        #region Battery Emergency System
        
        void UpdateBatteryEmergency()
        {
            if (batteryReplacementEvent == null || holeSpawner == null) return;
            
            // Проверяем: батарея разряжена и не заменена
            bool isBatteryEmpty = batteryReplacementEvent.IsEventActive && !batteryReplacementEvent.IsCompleted;
            
            if (isBatteryEmpty)
            {
                batteryEmptyTimer += Time.deltaTime;
                
                // Предупреждение на 25 секунде
                if (batteryEmptyTimer >= 25f && batteryEmptyTimer < 25.5f)
                {
                    OnCriticalWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Батарея разряжена! Начинается обстрел корабля!");
                }
                
                // Каждые 5 секунд создаём новую дыру
                if (batteryEmptyTimer >= nextHoleSpawnTime)
                {
                    SpawnEmergencyHole();
                    nextHoleSpawnTime = batteryEmptyTimer + holeSpawnInterval;
                    
                    Debug.LogWarning($"💥 Без защиты батарей! Новая пробоина! (Таймер: {batteryEmptyTimer:F0}с)");
                }
            }
            else
            {
                // Батарея в порядке - сбрасываем таймер
                batteryEmptyTimer = 0f;
                nextHoleSpawnTime = holeSpawnInterval;
            }
        }
        
        void SpawnEmergencyHole()
        {
            if (holeSpawner == null) return;
            
            // Используем публичный метод HoleSpawner
            holeSpawner.SpawnSingleHole();
        }
        
        #endregion
        
        #region Oxygen System (Pins)
        
        void UpdateOxygenSystem()
        {
            if (attachModel == null) return;
            
            // Проверяем: все ли пины вынуты ИЛИ все имеют износ >90%
            bool isCriticalPinSituation = CheckPinCriticalCondition();
            
            if (isCriticalPinSituation)
            {
                if (!isOxygenCritical)
                {
                    isOxygenCritical = true;
                    OnCriticalWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Все пины вынуты или изношены! Кислород падает!");
                }
                
                // Снижаем кислород
                oxygenLevel -= oxygenDepletionRate * Time.deltaTime;
                oxygenLevel = Mathf.Clamp(oxygenLevel, 0f, 100f);
                
                if (criticalOxygenEffects != null)
                {
                    criticalOxygenEffects.SetActive(true);
                }
            }
            else
            {
                if (isOxygenCritical)
                {
                    isOxygenCritical = false;
                    Debug.Log("✓ Кислород восстановлен - пин вставлен!");
                }
                
                // Восстанавливаем кислород
                oxygenLevel += (oxygenDepletionRate / 2f) * Time.deltaTime;
                oxygenLevel = Mathf.Clamp(oxygenLevel, 0f, 100f);
                
                if (criticalOxygenEffects != null)
                {
                    criticalOxygenEffects.SetActive(false);
                }
            }
            
            OnOxygenLevelChanged?.Invoke(oxygenLevel);
        }
        
        bool CheckPinCriticalCondition()
        {
            int socketsCount = attachModel.InsertPoints.Count;
            List<GameObject> insertedPins = attachModel.GetInsertedPins();
            
            // Условие 1: Все пины вынуты
            if (insertedPins.Count == 0)
                return true;
            
            // Условие 2: Все вставленные пины имеют износ >= 90%
            bool hasGoodPin = false;
            foreach (GameObject pin in insertedPins)
            {
                PinWear pinWear = pin.GetComponent<PinWear>();
                if (pinWear != null && pinWear.WearLevel < 90f)
                {
                    hasGoodPin = true;
                    break;
                }
            }
            
            return !hasGoodPin;
        }
        
        #endregion
        
        #region Game Over Conditions
        
        void CheckGameOverConditions()
        {
            // 1. Критическая разгерметизация
            if (hullIntegrity <= criticalIntegrityLevel)
            {
                TriggerGameOver($"Критическая разгерметизация! Герметичность: {hullIntegrity:F0}%");
                return;
            }
            
            // 2. Смерть от крыс
            if (playerHealth <= 0f)
            {
                TriggerGameOver("Вы погибли от укусов крыс!");
                return;
            }
            
            // 3. Потеря управления кораблём
            if (controlMalfunctionTimer >= maxControlMalfunctionTime)
            {
                TriggerGameOver("Корабль потерян в космосе! Управление не восстановлено.");
                return;
            }
            
            // 4. Критическая нехватка кислорода
            if (oxygenLevel <= criticalOxygenLevel)
            {
                TriggerGameOver("Кислород закончился! Все системы креплений отказали.");
                return;
            }
            
            // 5. Слишком долго без батареи (опционально - если хотите жёсткий лимит)
            if (batteryEmptyTimer >= maxBatteryEmptyTime)
            {
                // Можно добавить Game Over или оставить только спавн дыр
                // TriggerGameOver("Корабль уничтожен без защиты батарей!");
            }
        }
        
        void TriggerGameOver(string reason)
        {
            if (isGameOver) return;
            
            isGameOver = true;
            
            Debug.Log($"═══════════════════════════");
            Debug.Log($"       GAME OVER");
            Debug.Log($"═══════════════════════════");
            Debug.Log($"Причина: {reason}");
            Debug.Log($"День: {dayEventManager?.currentDay ?? 0}");
            Debug.Log($"═══════════════════════════");
            
            OnGameOver?.Invoke(reason);
            
            // Останавливаем все системы
            StopAllSystems();
            
            // Запускаем перезагрузку
            StartCoroutine(GameOverSequence(reason));
        }
        
        void StopAllSystems()
        {
            // Останавливаем спавн крыс
            if (ratSpawner != null)
                ratSpawner.StopSpawning();
            
            // Останавливаем все активные ивенты
            if (holeSpawner != null)
                holeSpawner.StopHoleEvent();
            
            if (controlPanelMalfunction != null)
                controlPanelMalfunction.ForceStopMalfunction();
        }
        
        IEnumerator GameOverSequence(string reason)
        {
            // Показываем экран Game Over с задержкой
            yield return new WaitForSeconds(gameOverDelay);
            
            // Перезагружаем сцену или переходим в меню
            if (!string.IsNullOrEmpty(gameOverSceneName))
            {
                SceneManager.LoadScene(gameOverSceneName);
            }
            else
            {
                // Альтернатива: перезагрузка текущей сцены
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
        
        #endregion
        
        #region Public Methods (для UI и отладки)
        
        public string GetSystemStatus()
        {
            return $"Герметичность: {hullIntegrity:F0}%\n" +
                   $"Здоровье: {playerHealth:F0}/{maxPlayerHealth}\n" +
                   $"Кислород: {oxygenLevel:F0}%\n" +
                   $"Контроль: {(controlMalfunctionTimer > 0 ? $"{(maxControlMalfunctionTime - controlMalfunctionTimer):F0}с" : "ОК")}\n" +
                   $"Батарея: {(batteryEmptyTimer > 0 ? $"⚠️ {batteryEmptyTimer:F0}с" : "ОК")}";
        }
        
        public void ResetAllSystems()
        {
            hullIntegrity = 100f;
            playerHealth = maxPlayerHealth;
            oxygenLevel = 100f;
            controlMalfunctionTimer = 0f;
            batteryEmptyTimer = 0f;
            isOxygenCritical = false;
            isGameOver = false;
        }
        
        // Для тестирования в редакторе
        [ContextMenu("Debug: Trigger Hull Breach")]
        public void DebugTriggerHullBreach()
        {
            hullIntegrity = 15f;
        }
        
        [ContextMenu("Debug: Trigger Low Health")]
        public void DebugTriggerLowHealth()
        {
            playerHealth = 5f;
        }
        
        [ContextMenu("Debug: Trigger Oxygen Critical")]
        public void DebugTriggerOxygenCritical()
        {
            oxygenLevel = 5f;
        }
        
        #endregion
    }
}