using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG
{
    /// <summary>
    /// Управляет критериями поражения и состоянием корабля
    /// ОБНОВЛЕНО: Интеграция с GameOverUI
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        [Header("=== СИСТЕМЫ КОРАБЛЯ ===")]
        
        [Header("Герметичность корпуса")]
        [Range(0f, 100f)]
        public float hullIntegrity = 100f;
        public float integrityLossPerHole = 8f;
        public float criticalIntegrityLevel = 20f;
        
        [Header("Здоровье игрока")]
        public float playerHealth = 100f;
        public float maxPlayerHealth = 100f;
        public float ratDamagePerSecond = 5f;
        public float healthRegenerationRate = 2f;
        
        [Header("Потеря управления")]
        public float controlMalfunctionTimer = 0f;
        public float maxControlMalfunctionTime = 300f;
        
        [Header("Критическая ситуация с батареей")]
        public float batteryEmptyTimer = 0f;
        public float maxBatteryEmptyTime = 30f;
        public float holeSpawnInterval = 5f;
        private float nextHoleSpawnTime = 0f;
        
        [Header("Критическая ситуация с пинами")]
        public float oxygenLevel = 100f;
        public float oxygenDepletionRate = 5f;
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
        public UnityEvent<float> OnHullIntegrityChanged;
        public UnityEvent<float> OnPlayerHealthChanged;
        public UnityEvent<float> OnOxygenLevelChanged;
        public UnityEvent<string> OnGameOver; // ⬅️ ИСПОЛЬЗУЕТСЯ GAMEOVERUI
        public UnityEvent OnCriticalWarning;
        
        [Header("Визуальные эффекты")]
        public GameObject criticalHullEffects;
        public GameObject criticalHealthEffects;
        public GameObject criticalOxygenEffects;
        
        [Header("Настройки Game Over")]
        public bool useGameOverUI = true; // ⬅️ НОВОЕ: Использовать UI вместо прямой загрузки сцены
        public string gameOverSceneName = "MainMenu";
        public float gameOverDelay = 3f;
        
        private bool isGameOver = false;
        
        void Update()
        {
            if (isGameOver) return;
            
            UpdateHullIntegrity();
            UpdatePlayerHealth();
            UpdateControlMalfunction();
            UpdateBatteryEmergency();
            UpdateOxygenSystem();
            
            CheckGameOverConditions();
        }
        
        #region Hull Integrity System
        
        void UpdateHullIntegrity()
        {
            if (holeSpawner == null) return;
            
            int activeHoles = holeSpawner.GetActiveHolesCount();
            float targetIntegrity = 100f - (activeHoles * integrityLossPerHole);
            targetIntegrity = Mathf.Clamp(targetIntegrity, 0f, 100f);
            
            hullIntegrity = Mathf.Lerp(hullIntegrity, targetIntegrity, Time.deltaTime * 2f);
            
            OnHullIntegrityChanged?.Invoke(hullIntegrity);
            
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
            
            int aliveRats = ratSpawner.GetAliveRatsCount();
            
            if (aliveRats > 0)
            {
                float damage = ratDamagePerSecond * Time.deltaTime * Mathf.Min(aliveRats, 10);
                playerHealth -= damage;
            }
            else
            {
                playerHealth += healthRegenerationRate * Time.deltaTime;
            }
            
            playerHealth = Mathf.Clamp(playerHealth, 0f, maxPlayerHealth);
            
            OnPlayerHealthChanged?.Invoke(playerHealth);
            
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
                
                if (controlMalfunctionTimer >= 240f && controlMalfunctionTimer < 240.5f)
                {
                    OnCriticalWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Осталась 1 минута до потери управления!");
                }
            }
            else
            {
                controlMalfunctionTimer = 0f;
            }
        }
        
        #endregion
        
        #region Battery Emergency System
        
        void UpdateBatteryEmergency()
        {
            if (batteryReplacementEvent == null || holeSpawner == null) return;
            
            bool isBatteryEmpty = batteryReplacementEvent.IsEventActive && !batteryReplacementEvent.IsCompleted;
            
            if (isBatteryEmpty)
            {
                batteryEmptyTimer += Time.deltaTime;
                
                if (batteryEmptyTimer >= 25f && batteryEmptyTimer < 25.5f)
                {
                    OnCriticalWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Батарея разряжена! Начинается обстрел корабля!");
                }
                
                if (batteryEmptyTimer >= nextHoleSpawnTime)
                {
                    SpawnEmergencyHole();
                    nextHoleSpawnTime = batteryEmptyTimer + holeSpawnInterval;
                    
                    Debug.LogWarning($"💥 Без защиты батарей! Новая пробоина! (Таймер: {batteryEmptyTimer:F0}с)");
                }
            }
            else
            {
                batteryEmptyTimer = 0f;
                nextHoleSpawnTime = holeSpawnInterval;
            }
        }
        
        void SpawnEmergencyHole()
        {
            if (holeSpawner == null) return;
            holeSpawner.SpawnSingleHole();
        }
        
        #endregion
        
        #region Oxygen System (Pins)
        
        void UpdateOxygenSystem()
        {
            if (attachModel == null) return;
            
            bool isCriticalPinSituation = CheckPinCriticalCondition();
            
            if (isCriticalPinSituation)
            {
                if (!isOxygenCritical)
                {
                    isOxygenCritical = true;
                    OnCriticalWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Все пины вынуты или изношены! Кислород падает!");
                }
                
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
            
            if (insertedPins.Count == 0)
                return true;
            
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
            if (hullIntegrity <= criticalIntegrityLevel)
            {
                TriggerGameOver($"Критическая разгерметизация! Герметичность: {hullIntegrity:F0}%");
                return;
            }
            
            if (playerHealth <= 0f)
            {
                TriggerGameOver("Вы погибли от укусов крыс!");
                return;
            }
            
            if (controlMalfunctionTimer >= maxControlMalfunctionTime)
            {
                TriggerGameOver("Корабль потерян в космосе! Управление не восстановлено.");
                return;
            }
            
            if (oxygenLevel <= criticalOxygenLevel)
            {
                TriggerGameOver("Кислород закончился! Все системы креплений отказали.");
                return;
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
            
            // ⬅️ ИЗМЕНЕННОЕ: Вызываем OnGameOver для UI
            OnGameOver?.Invoke(reason);
            
            StopAllSystems();
            
            // ⬅️ НОВОЕ: Только если не используем UI, загружаем сцену напрямую
            if (!useGameOverUI)
            {
                StartCoroutine(GameOverSequence(reason));
            }
        }
        
        void StopAllSystems()
        {
            if (ratSpawner != null)
                ratSpawner.StopSpawning();
            
            if (holeSpawner != null)
                holeSpawner.StopHoleEvent();
            
            if (controlPanelMalfunction != null)
                controlPanelMalfunction.ForceStopMalfunction();
        }
        
        IEnumerator GameOverSequence(string reason)
        {
            yield return new WaitForSeconds(gameOverDelay);
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
        }
        
        #endregion
        
        #region Public Methods
        
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