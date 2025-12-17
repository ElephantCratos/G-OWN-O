using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
        public float healthRegenerationRate = 2f;
        
        [Header("Система укусов крыс")]
        [Tooltip("Урон от одного укуса")]
        public float ratBiteDamage = 10f;
        [Tooltip("Интервал между укусами одной крысы (секунды)")]
        public float biteCooldown = 1.5f;
        [Tooltip("Радиус атаки крысы")]
        public float ratAttackRange = 1.5f;
        [Tooltip("Максимум крыс, которые могут кусать одновременно")]
        public int maxSimultaneousBites = 5;
        [Tooltip("Ссылка на трансформ игрока")]
        public Transform playerTransform;
        
        // Словарь для отслеживания кулдауна укусов каждой крысы
        private Dictionary<GameObject, float> ratBiteCooldowns = new Dictionary<GameObject, float>();
        
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
        
        [Header("События укусов и индикаторов")]
        [Tooltip("Вызывается при каждом укусе крысы (передаёт урон)")]
        public UnityEvent<float> OnRatBite;
        [Tooltip("Вызывается при критическом здоровье (<30%)")]
        public UnityEvent OnLowHealthWarning;
        [Tooltip("Вызывается когда здоровье восстановилось")]
        public UnityEvent OnHealthRestored;
        [Tooltip("Вызывается при критической герметичности")]
        public UnityEvent OnHullBreachWarning;
        [Tooltip("Вызывается при проблемах с кислородом")]
        public UnityEvent OnOxygenWarning;
        [Tooltip("Вызывается при проблемах с управлением")]
        public UnityEvent OnControlWarning;
        
        [Header("Эффекты укуса")]
        [Tooltip("Красная виньетка при укусе")]
        public GameObject biteDamageVignette;
        [Tooltip("Длительность эффекта укуса")]
        public float biteEffectDuration = 0.3f;
        [Tooltip("Аудио укуса")]
        public AudioSource biteAudioSource;
        public AudioClip[] biteAudioClips;
        
        [Header("Индикаторы проблем (UI)")]
        [Tooltip("Иконка проблемы с герметичностью")]
        public GameObject hullWarningIndicator;
        [Tooltip("Иконка проблемы со здоровьем")]
        public GameObject healthWarningIndicator;
        [Tooltip("Иконка проблемы с кислородом")]
        public GameObject oxygenWarningIndicator;
        [Tooltip("Иконка проблемы с управлением")]
        public GameObject controlWarningIndicator;
        [Tooltip("Иконка проблемы с батареей")]
        public GameObject batteryWarningIndicator;
        
        [Header("Вибрация контроллера (VR)")]
        public bool enableControllerVibration = true;
        [Range(0f, 1f)]
        public float biteVibrationIntensity = 0.7f;
        public float biteVibrationDuration = 0.2f;
        
        private bool isGameOver = false;
        private bool wasLowHealth = false;
        private Coroutine biteEffectCoroutine;
        
        void Start()
        {
            // Автопоиск игрока если не назначен
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
                else
                {
                    // Пробуем найти камеру VR
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                        playerTransform = mainCam.transform;
                }
            }
            
            // Скрываем все индикаторы на старте
            SetIndicatorActive(hullWarningIndicator, false);
            SetIndicatorActive(healthWarningIndicator, false);
            SetIndicatorActive(oxygenWarningIndicator, false);
            SetIndicatorActive(controlWarningIndicator, false);
            SetIndicatorActive(batteryWarningIndicator, false);
            SetIndicatorActive(biteDamageVignette, false);
        }
        
        void Update()
        {
            if (isGameOver) return;
            
            UpdateHullIntegrity();
            UpdatePlayerHealth();
            UpdateControlMalfunction();
            UpdateBatteryEmergency();
            UpdateOxygenSystem();
            UpdateWarningIndicators();
            
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
            
            // Предупреждение при критической герметичности
            if (hullIntegrity < 40f && hullIntegrity >= criticalIntegrityLevel)
            {
                OnHullBreachWarning?.Invoke();
            }
        }
        
        #endregion
        
        #region Player Health System (с системой укусов)
        
        void UpdatePlayerHealth()
        {
            if (ratSpawner == null || playerTransform == null)
            {
                // Если нет спавнера или игрока, просто регенерируем
                RegenerateHealth();
                return;
            }
            
            // Получаем список живых крыс
            List<GameObject> aliveRats = ratSpawner.GetAliveRats();
            
            if (aliveRats == null || aliveRats.Count == 0)
            {
                RegenerateHealth();
                CleanupRatCooldowns();
                return;
            }
            
            // Очищаем мёртвых крыс из словаря кулдаунов
            CleanupRatCooldowns();
            
            int bitesThisFrame = 0;
            
            foreach (GameObject rat in aliveRats)
            {
                if (rat == null) continue;
                if (bitesThisFrame >= maxSimultaneousBites) break;
                
                // Проверяем расстояние до игрока
                float distance = Vector3.Distance(rat.transform.position, playerTransform.position);
                
                if (distance <= ratAttackRange)
                {
                    // Крыса в радиусе атаки — проверяем кулдаун
                    if (CanRatBite(rat))
                    {
                        PerformRatBite(rat);
                        bitesThisFrame++;
                    }
                }
            }
            
            // Обновляем кулдауны
            UpdateRatCooldowns();
            
            // Если крыс рядом нет — регенерация
            if (bitesThisFrame == 0 && !IsAnyRatNearby(aliveRats))
            {
                RegenerateHealth();
            }
            
            playerHealth = Mathf.Clamp(playerHealth, 0f, maxPlayerHealth);
            OnPlayerHealthChanged?.Invoke(playerHealth);
            
            // Проверяем состояние здоровья для событий
            CheckHealthState();
            
            // Визуальные эффекты при низком здоровье
            if (criticalHealthEffects != null)
            {
                criticalHealthEffects.SetActive(playerHealth < 30f);
            }
        }
        
        bool IsAnyRatNearby(List<GameObject> rats)
        {
            foreach (GameObject rat in rats)
            {
                if (rat == null) continue;
                float distance = Vector3.Distance(rat.transform.position, playerTransform.position);
                if (distance <= ratAttackRange * 1.5f) // Небольшой буфер
                    return true;
            }
            return false;
        }
        
        bool CanRatBite(GameObject rat)
        {
            if (!ratBiteCooldowns.ContainsKey(rat))
            {
                ratBiteCooldowns[rat] = 0f;
                return true;
            }
            
            return ratBiteCooldowns[rat] <= 0f;
        }
        
        void PerformRatBite(GameObject rat)
        {
            // Наносим урон
            playerHealth -= ratBiteDamage;
            
            // Устанавливаем кулдаун для этой крысы
            ratBiteCooldowns[rat] = biteCooldown;
            
            // Вызываем событие укуса
            OnRatBite?.Invoke(ratBiteDamage);
            
            // Визуальные и звуковые эффекты
            PlayBiteEffects();
            
            // Вибрация контроллера
            if (enableControllerVibration)
            {
                TriggerControllerVibration();
            }
            
            Debug.Log($"🐀 Крыса укусила! Урон: {ratBiteDamage}, Здоровье: {playerHealth:F0}");
        }
        
        void PlayBiteEffects()
        {
            // Красная виньетка
            if (biteDamageVignette != null)
            {
                if (biteEffectCoroutine != null)
                    StopCoroutine(biteEffectCoroutine);
                biteEffectCoroutine = StartCoroutine(ShowBiteVignette());
            }
            
            // Звук укуса
            if (biteAudioSource != null && biteAudioClips != null && biteAudioClips.Length > 0)
            {
                AudioClip clip = biteAudioClips[Random.Range(0, biteAudioClips.Length)];
                biteAudioSource.PlayOneShot(clip);
            }
        }
        
        IEnumerator ShowBiteVignette()
        {
            SetIndicatorActive(biteDamageVignette, true);
            yield return new WaitForSeconds(biteEffectDuration);
            SetIndicatorActive(biteDamageVignette, false);
        }
        
        void TriggerControllerVibration()
        {
            // Для BNG Framework
            if (InputBridge.Instance != null)
            {
                InputBridge.Instance.VibrateController(biteVibrationIntensity, biteVibrationIntensity, 
                    biteVibrationDuration, ControllerHand.Left);
                InputBridge.Instance.VibrateController(biteVibrationIntensity, biteVibrationIntensity, 
                    biteVibrationDuration, ControllerHand.Right);
            }
        }
        
        void UpdateRatCooldowns()
        {
            List<GameObject> keys = new List<GameObject>(ratBiteCooldowns.Keys);
            foreach (GameObject rat in keys)
            {
                if (ratBiteCooldowns[rat] > 0f)
                {
                    ratBiteCooldowns[rat] -= Time.deltaTime;
                }
            }
        }
        
        void CleanupRatCooldowns()
        {
            List<GameObject> toRemove = new List<GameObject>();
            foreach (var kvp in ratBiteCooldowns)
            {
                if (kvp.Key == null)
                    toRemove.Add(kvp.Key);
            }
            foreach (var key in toRemove)
            {
                ratBiteCooldowns.Remove(key);
            }
        }
        
        void RegenerateHealth()
        {
            if (playerHealth < maxPlayerHealth)
            {
                playerHealth += healthRegenerationRate * Time.deltaTime;
            }
        }
        
        void CheckHealthState()
        {
            bool isLowHealth = playerHealth < 30f;
            
            if (isLowHealth && !wasLowHealth)
            {
                OnLowHealthWarning?.Invoke();
                Debug.LogWarning("⚠️ Критически низкое здоровье!");
            }
            else if (!isLowHealth && wasLowHealth)
            {
                OnHealthRestored?.Invoke();
                Debug.Log("✓ Здоровье восстановлено");
            }
            
            wasLowHealth = isLowHealth;
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
                    OnControlWarning?.Invoke();
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
                    OnOxygenWarning?.Invoke();
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
        
        #region Warning Indicators
        
        void UpdateWarningIndicators()
        {
            // Индикатор герметичности
            SetIndicatorActive(hullWarningIndicator, hullIntegrity < 50f);
            
            // Индикатор здоровья
            SetIndicatorActive(healthWarningIndicator, playerHealth < 50f);
            
            // Индикатор кислорода
            SetIndicatorActive(oxygenWarningIndicator, oxygenLevel < 50f || isOxygenCritical);
            
            // Индикатор управления
            SetIndicatorActive(controlWarningIndicator, controlMalfunctionTimer > 0f);
            
            // Индикатор батареи
            SetIndicatorActive(batteryWarningIndicator, batteryEmptyTimer > 0f);
        }
        
        void SetIndicatorActive(GameObject indicator, bool active)
        {
            if (indicator != null && indicator.activeSelf != active)
            {
                indicator.SetActive(active);
            }
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
            
            if (!string.IsNullOrEmpty(gameOverSceneName))
            {
                SceneManager.LoadScene(gameOverSceneName);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Получить количество крыс рядом с игроком
        /// </summary>
        public int GetNearbyRatsCount()
        {
            if (ratSpawner == null || playerTransform == null) return 0;
            
            List<GameObject> rats = ratSpawner.GetAliveRats();
            int count = 0;
            
            foreach (GameObject rat in rats)
            {
                if (rat == null) continue;
                if (Vector3.Distance(rat.transform.position, playerTransform.position) <= ratAttackRange)
                    count++;
            }
            
            return count;
        }
        
        public string GetSystemStatus()
        {
            int nearbyRats = GetNearbyRatsCount();
            return $"Герметичность: {hullIntegrity:F0}%\n" +
                   $"Здоровье: {playerHealth:F0}/{maxPlayerHealth}\n" +
                   $"Кислород: {oxygenLevel:F0}%\n" +
                   $"Крыс рядом: {nearbyRats}\n" +
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
            wasLowHealth = false;
            ratBiteCooldowns.Clear();
        }
        
        /// <summary>
        /// Нанести урон игроку извне (например, от других источников)
        /// </summary>
        public void DamagePlayer(float damage, bool showEffects = true)
        {
            playerHealth -= damage;
            playerHealth = Mathf.Clamp(playerHealth, 0f, maxPlayerHealth);
            
            if (showEffects)
            {
                PlayBiteEffects();
            }
            
            OnPlayerHealthChanged?.Invoke(playerHealth);
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
        
        [ContextMenu("Debug: Simulate Rat Bite")]
        public void DebugSimulateBite()
        {
            DamagePlayer(ratBiteDamage);
            OnRatBite?.Invoke(ratBiteDamage);
        }
        
        #endregion
    }
}