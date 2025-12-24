using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BNG
{
    /// <summary>
    /// Управляет критериями поражения и состоянием корабля
    /// ОБНОВЛЕНО: Полная система звуковых предупреждений для всех критических ситуаций
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
        public float criticalOxygenLevel = 10f;
        private bool isOxygenCritical = false;
        
        [Header("=== ЗВУКОВЫЕ ПРЕДУПРЕЖДЕНИЯ ===")]
        [Tooltip("Источник звука для предупреждений")]
        public AudioSource warningAudioSource;
        
        [Header("Звуки предупреждений таймеров")]
        [Tooltip("Звук предупреждения за 30 секунд")]
        public AudioClip timer30SecWarning;
        [Tooltip("Звук критического предупреждения (за 10 сек)")]
        public AudioClip timer10SecWarning;
        [Tooltip("Звуковой сигнал каждые 5 секунд в критическое время")]
        public AudioClip timerBeepSound;
        [Tooltip("Интервал сигналов в критическое время")]
        public float criticalBeepInterval = 5f;
        
        [Header("Звуки кислородной системы (пины)")]
        [Tooltip("Звук при вытаскивании всех пинов - начало кризиса")]
        public AudioClip oxygenCrisisStartSound;
        [Tooltip("Звук предупреждения при 50% кислорода")]
        public AudioClip oxygen50PercentWarning;
        [Tooltip("Звук критического уровня при 25% кислорода")]
        public AudioClip oxygen25PercentWarning;
        [Tooltip("Звук последнего шанса при 10% кислорода")]
        public AudioClip oxygen10PercentWarning;
        [Tooltip("Постоянный звук тревоги при критическом кислороде")]
        public AudioClip oxygenAlarmLoop;
        
        [Header("Звуки батареи")]
        [Tooltip("Звук при разрядке батареи - начало кризиса")]
        public AudioClip batteryCrisisStartSound;
        [Tooltip("Звук предупреждения при приближении к критическому времени")]
        public AudioClip batteryWarningSound;
        [Tooltip("Звук при каждой новой пробоине от разряженной батареи")]
        public AudioClip batteryHoleSpawnSound;
        
        [Header("Звуки герметичности (Hull)")]
        [Tooltip("Звук при падении герметичности до 50%")]
        public AudioClip hull50PercentWarning;
        [Tooltip("Звук при падении герметичности до 35%")]
        public AudioClip hull35PercentWarning;
        [Tooltip("Звук при падении герметичности до 25% - критический")]
        public AudioClip hull25PercentWarning;
        [Tooltip("Постоянная сирена при критической герметичности")]
        public AudioClip hullAlarmLoop;
        
        [Header("Звуки здоровья игрока")]
        [Tooltip("Звук при падении здоровья до 50%")]
        public AudioClip health50PercentWarning;
        [Tooltip("Звук при падении здоровья до 25%")]
        public AudioClip health25PercentWarning;
        [Tooltip("Звук сердцебиения при критическом здоровье")]
        public AudioClip heartbeatSound;
        
        [Header("Настройки звуковых предупреждений")]
        [Tooltip("Минимальный интервал между одинаковыми предупреждениями")]
        public float warningCooldown = 10f;
        [Tooltip("Громкость предупреждений")]
        [Range(0f, 1f)]
        public float warningVolume = 1f;
        
        // Флаги для отслеживания воспроизведенных предупреждений - Контроль
        private bool control30SecWarningPlayed = false;
        private bool control10SecWarningPlayed = false;
        private float controlLastBeepTime = 0f;
        
        // Флаги для кислорода
        private bool oxygenCrisisStartPlayed = false;
        private bool oxygen50WarningPlayed = false;
        private bool oxygen25WarningPlayed = false;
        private bool oxygen10WarningPlayed = false;
        private float lastOxygenAlarmTime = 0f;
        
        // Флаги для батареи
        private bool batteryCrisisStartPlayed = false;
        private bool batteryWarningPlayed = false;
        private float lastBatteryBeepTime = 0f;
        
        // Флаги для герметичности
        private bool hull50WarningPlayed = false;
        private bool hull35WarningPlayed = false;
        private bool hull25WarningPlayed = false;
        private float lastHullAlarmTime = 0f;
        
        // Флаги для здоровья
        private bool health50WarningPlayed = false;
        private bool health25WarningPlayed = false;
        private float lastHeartbeatTime = 0f;
        
        [Header("=== БЛОКИРОВКА ПОСЛЕ GAME OVER ===")]
        [Tooltip("Отключать ли взаимодействие с объектами после поражения")]
        public bool disableInteractionOnGameOver = true;
        [Tooltip("Список Grabber'ов для отключения")]
        public List<Grabber> playerGrabbers = new List<Grabber>();
        [Tooltip("Locomotion для отключения движения")]
        public SmoothLocomotion playerLocomotion;
        [Tooltip("Teleport для отключения телепортации")]
        public PlayerTeleport playerTeleport;
        
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
        public UnityEvent<string> OnGameOver;
        public UnityEvent OnCriticalWarning;
        
        public UnityEvent<float> OnControlTimerUpdate;
        public UnityEvent<float> OnBatteryTimerUpdate;
        
        [Header("Визуальные эффекты")]
        public GameObject criticalHullEffects;
        public GameObject criticalHealthEffects;
        public GameObject criticalOxygenEffects;
        
        [Header("Настройки Game Over")]
        public bool useGameOverUI = true;
        public string gameOverSceneName = "MainMenu";
        public float gameOverDelay = 3f;
        
        [Header("События укусов и индикаторов")]
        public UnityEvent<float> OnRatBite;
        public UnityEvent OnLowHealthWarning;
        public UnityEvent OnHealthRestored;
        public UnityEvent OnHullBreachWarning;
        public UnityEvent OnOxygenWarning;
        public UnityEvent OnControlWarning;
        public UnityEvent OnBatteryWarning;
        
        [Header("Эффекты укуса")]
        public GameObject biteDamageVignette;
        public float biteEffectDuration = 0.3f;
        public AudioSource biteAudioSource;
        public AudioClip[] biteAudioClips;
        
        [Header("Индикаторы проблем (UI)")]
        public GameObject hullWarningIndicator;
        public GameObject healthWarningIndicator;
        public GameObject oxygenWarningIndicator;
        public GameObject controlWarningIndicator;
        public GameObject batteryWarningIndicator;
        
        [Header("Вибрация контроллера (VR)")]
        public bool enableControllerVibration = true;
        [Range(0f, 1f)]
        public float biteVibrationIntensity = 0.7f;
        public float biteVibrationDuration = 0.2f;
        
        private bool isGameOver = false;
        private bool wasLowHealth = false;
        private Coroutine biteEffectCoroutine;
        
        // Для отслеживания предыдущих значений
        private float previousHullIntegrity = 100f;
        private float previousOxygenLevel = 100f;
        private float previousPlayerHealth = 100f;
        
        void Start()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
                else
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                        playerTransform = mainCam.transform;
                }
            }
            
            // Автопоиск компонентов для блокировки
            if (playerGrabbers.Count == 0)
            {
                Grabber[] foundGrabbers = FindObjectsOfType<Grabber>();
                playerGrabbers.AddRange(foundGrabbers);
                Debug.Log($"Найдено Grabber'ов: {playerGrabbers.Count}");
            }
            
            if (playerLocomotion == null)
            {
                playerLocomotion = FindObjectOfType<SmoothLocomotion>();
            }
            
            if (playerTeleport == null)
            {
                playerTeleport = FindObjectOfType<PlayerTeleport>();
            }
            
            // Автопоиск AudioSource если не назначен
            if (warningAudioSource == null)
            {
                warningAudioSource = gameObject.GetComponent<AudioSource>();
                if (warningAudioSource == null)
                {
                    warningAudioSource = gameObject.AddComponent<AudioSource>();
                    warningAudioSource.playOnAwake = false;
                    warningAudioSource.spatialBlend = 0f; // 2D звук
                }
            }
            
            SetIndicatorActive(hullWarningIndicator, false);
            SetIndicatorActive(healthWarningIndicator, false);
            SetIndicatorActive(oxygenWarningIndicator, false);
            SetIndicatorActive(controlWarningIndicator, false);
            SetIndicatorActive(batteryWarningIndicator, false);
            SetIndicatorActive(biteDamageVignette, false);
            
            // Инициализация предыдущих значений
            previousHullIntegrity = hullIntegrity;
            previousOxygenLevel = oxygenLevel;
            previousPlayerHealth = playerHealth;
        }
        
        void Update()
        {
            // Проверяем победу - если игра выиграна, не проверяем Game Over
            if (dayEventManager != null && dayEventManager.IsGameWon())
            {
                return;
            }
            
            if (isGameOver) return;
            
            UpdateHullIntegrity();
            UpdatePlayerHealth();
            UpdateControlMalfunction();
            UpdateBatteryEmergency();
            UpdateOxygenSystem();
            UpdateWarningIndicators();
            
            // Обновляем звуковые предупреждения
            UpdateHullWarnings();
            UpdateHealthWarnings();
            
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
            
            if (hullIntegrity < 40f && hullIntegrity >= criticalIntegrityLevel)
            {
                OnHullBreachWarning?.Invoke();
            }
        }
        
        void UpdateHullWarnings()
        {
            // Предупреждение при 50% герметичности
            if (hullIntegrity <= 50f && previousHullIntegrity > 50f && !hull50WarningPlayed)
            {
                PlayWarningSound(hull50PercentWarning);
                hull50WarningPlayed = true;
                OnCriticalWarning?.Invoke();
                OnHullBreachWarning?.Invoke();
                Debug.LogWarning("⚠️ ВНИМАНИЕ: Герметичность упала до 50%!");
            }
            
            // Предупреждение при 35% герметичности
            if (hullIntegrity <= 35f && previousHullIntegrity > 35f && !hull35WarningPlayed)
            {
                PlayWarningSound(hull35PercentWarning);
                hull35WarningPlayed = true;
                OnCriticalWarning?.Invoke();
                Debug.LogWarning("🚨 ОПАСНОСТЬ: Герметичность упала до 35%!");
            }
            
            // Критическое предупреждение при 25% герметичности
            if (hullIntegrity <= 25f && previousHullIntegrity > 25f && !hull25WarningPlayed)
            {
                PlayWarningSound(hull25PercentWarning);
                hull25WarningPlayed = true;
                OnCriticalWarning?.Invoke();
                Debug.LogWarning("💀 КРИТИЧЕСКОЕ: Герметичность 25%! Скоро Game Over!");
            }
            
            // Постоянная сирена при критической герметичности (< 30%)
            if (hullIntegrity < 30f && hullAlarmLoop != null)
            {
                if (Time.time - lastHullAlarmTime >= criticalBeepInterval)
                {
                    PlayWarningSound(hullAlarmLoop);
                    lastHullAlarmTime = Time.time;
                }
            }
            
            // Сброс флагов при восстановлении
            if (hullIntegrity > 55f)
            {
                hull50WarningPlayed = false;
            }
            if (hullIntegrity > 40f)
            {
                hull35WarningPlayed = false;
            }
            if (hullIntegrity > 30f)
            {
                hull25WarningPlayed = false;
            }
            
            previousHullIntegrity = hullIntegrity;
        }
        
        #endregion
        
        #region Player Health System
        
        void UpdatePlayerHealth()
        {
            if (ratSpawner == null || playerTransform == null)
            {
                RegenerateHealth();
                return;
            }
            
            List<GameObject> aliveRats = ratSpawner.GetAliveRats();
            
            if (aliveRats == null || aliveRats.Count == 0)
            {
                RegenerateHealth();
                CleanupRatCooldowns();
                return;
            }
            
            CleanupRatCooldowns();
            
            int bitesThisFrame = 0;
            
            foreach (GameObject rat in aliveRats)
            {
                if (rat == null) continue;
                if (bitesThisFrame >= maxSimultaneousBites) break;
                
                float distance = Vector3.Distance(rat.transform.position, playerTransform.position);
                
                if (distance <= ratAttackRange)
                {
                    if (CanRatBite(rat))
                    {
                        PerformRatBite(rat);
                        bitesThisFrame++;
                    }
                }
            }
            
            UpdateRatCooldowns();
            
            if (bitesThisFrame == 0 && !IsAnyRatNearby(aliveRats))
            {
                RegenerateHealth();
            }
            
            playerHealth = Mathf.Clamp(playerHealth, 0f, maxPlayerHealth);
            OnPlayerHealthChanged?.Invoke(playerHealth);
            
            CheckHealthState();
            
            if (criticalHealthEffects != null)
            {
                criticalHealthEffects.SetActive(playerHealth < 30f);
            }
        }
        
        void UpdateHealthWarnings()
        {
            // Предупреждение при 50% здоровья
            if (playerHealth <= 50f && previousPlayerHealth > 50f && !health50WarningPlayed)
            {
                PlayWarningSound(health50PercentWarning);
                health50WarningPlayed = true;
                OnLowHealthWarning?.Invoke();
                Debug.LogWarning("⚠️ ВНИМАНИЕ: Здоровье упало до 50%!");
            }
            
            // Критическое предупреждение при 25% здоровья
            if (playerHealth <= 25f && previousPlayerHealth > 25f && !health25WarningPlayed)
            {
                PlayWarningSound(health25PercentWarning);
                health25WarningPlayed = true;
                OnCriticalWarning?.Invoke();
                Debug.LogWarning("🚨 КРИТИЧЕСКОЕ: Здоровье 25%!");
            }
            
            // Звук сердцебиения при критическом здоровье (< 30%)
            if (playerHealth < 30f && heartbeatSound != null)
            {
                if (Time.time - lastHeartbeatTime >= 1.5f) // Каждые 1.5 секунды
                {
                    PlayWarningSound(heartbeatSound, 0.8f);
                    lastHeartbeatTime = Time.time;
                }
            }
            
            // Сброс флагов при восстановлении
            if (playerHealth > 55f)
            {
                health50WarningPlayed = false;
            }
            if (playerHealth > 30f)
            {
                health25WarningPlayed = false;
            }
            
            previousPlayerHealth = playerHealth;
        }
        
        bool IsAnyRatNearby(List<GameObject> rats)
        {
            foreach (GameObject rat in rats)
            {
                if (rat == null) continue;
                float distance = Vector3.Distance(rat.transform.position, playerTransform.position);
                if (distance <= ratAttackRange * 1.5f)
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
            playerHealth -= ratBiteDamage;
            ratBiteCooldowns[rat] = biteCooldown;
            OnRatBite?.Invoke(ratBiteDamage);
            PlayBiteEffects();
            
            if (enableControllerVibration)
            {
                TriggerControllerVibration();
            }
            
            Debug.Log($"🐀 Крыса укусила! Урон: {ratBiteDamage}, Здоровье: {playerHealth:F0}");
        }
        
        void PlayBiteEffects()
        {
            if (biteDamageVignette != null)
            {
                if (biteEffectCoroutine != null)
                    StopCoroutine(biteEffectCoroutine);
                biteEffectCoroutine = StartCoroutine(ShowBiteVignette());
            }
            
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
                
                OnControlTimerUpdate?.Invoke(controlMalfunctionTimer);
                
                float remaining = maxControlMalfunctionTime - controlMalfunctionTimer;
                
                // Звуковое предупреждение за 30 секунд
                if (remaining <= 30f && !control30SecWarningPlayed)
                {
                    PlayWarningSound(timer30SecWarning);
                    control30SecWarningPlayed = true;
                    OnCriticalWarning?.Invoke();
                    OnControlWarning?.Invoke();
                    Debug.LogWarning("⚠️ ПРЕДУПРЕЖДЕНИЕ: 30 секунд до потери управления!");
                }
                
                // Критическое предупреждение за 10 секунд
                if (remaining <= 10f && !control10SecWarningPlayed)
                {
                    PlayWarningSound(timer10SecWarning);
                    control10SecWarningPlayed = true;
                    Debug.LogWarning("🚨 КРИТИЧЕСКОЕ: 10 секунд до потери управления!");
                }
                
                // Звуковые сигналы каждые 5 секунд в критическое время
                if (remaining <= 30f && Time.time - controlLastBeepTime >= criticalBeepInterval)
                {
                    PlayWarningSound(timerBeepSound);
                    controlLastBeepTime = Time.time;
                }
                
                // Старое предупреждение за минуту
                if (controlMalfunctionTimer >= 240f && controlMalfunctionTimer < 240.5f)
                {
                    OnCriticalWarning?.Invoke();
                    OnControlWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Осталась 1 минута до потери управления!");
                }
            }
            else
            {
                // Сбрасываем таймер и флаги
                controlMalfunctionTimer = 0f;
                control30SecWarningPlayed = false;
                control10SecWarningPlayed = false;
                controlLastBeepTime = 0f;
                OnControlTimerUpdate?.Invoke(0f);
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
                
                OnBatteryTimerUpdate?.Invoke(batteryEmptyTimer);
                
                // НОВОЕ: Звук при начале кризиса батареи
                if (!batteryCrisisStartPlayed)
                {
                    PlayWarningSound(batteryCrisisStartSound);
                    batteryCrisisStartPlayed = true;
                    OnCriticalWarning?.Invoke();
                    OnBatteryWarning?.Invoke();
                    Debug.LogWarning("🔋 ВНИМАНИЕ: Батарея разряжена! Начинается кризис!");
                }
                
                // Предупреждение на середине времени
                if (batteryEmptyTimer >= maxBatteryEmptyTime * 0.5f && !batteryWarningPlayed)
                {
                    PlayWarningSound(batteryWarningSound);
                    batteryWarningPlayed = true;
                    Debug.LogWarning("🚨 ОПАСНОСТЬ: Половина времени батареи истекла!");
                }
                
                // Звуковые сигналы каждые 5 секунд
                if (Time.time - lastBatteryBeepTime >= criticalBeepInterval)
                {
                    PlayWarningSound(timerBeepSound);
                    lastBatteryBeepTime = Time.time;
                }
                
                if (batteryEmptyTimer >= 25f && batteryEmptyTimer < 25.5f)
                {
                    OnCriticalWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Батарея разряжена! Начинается обстрел корабля!");
                }
                
                if (batteryEmptyTimer >= nextHoleSpawnTime)
                {
                    SpawnEmergencyHole();
                    nextHoleSpawnTime = batteryEmptyTimer + holeSpawnInterval;
                    
                    // НОВОЕ: Звук при появлении пробоины
                    PlayWarningSound(batteryHoleSpawnSound);
                    
                    Debug.LogWarning($"💥 Без защиты батарей! Новая пробоина! (Таймер: {batteryEmptyTimer:F0}с)");
                }
            }
            else
            {
                // Сброс при восстановлении батареи
                if (batteryCrisisStartPlayed)
                {
                    Debug.Log("✓ Батарея восстановлена!");
                }
                
                batteryEmptyTimer = 0f;
                nextHoleSpawnTime = holeSpawnInterval;
                batteryCrisisStartPlayed = false;
                batteryWarningPlayed = false;
                OnBatteryTimerUpdate?.Invoke(0f);
            }
        }
        
        void SpawnEmergencyHole()
        {
            if (holeSpawner == null) return;
            holeSpawner.SpawnSingleHole();
        }
        
        #endregion
        
        #region Oxygen System
        
        void UpdateOxygenSystem()
        {
            if (attachModel == null) return;
            
            bool isCriticalPinSituation = CheckPinCriticalCondition();
            
            if (isCriticalPinSituation)
            {
                // НОВОЕ: Звук при начале кислородного кризиса (все пины вынуты)
                if (!isOxygenCritical)
                {
                    isOxygenCritical = true;
                    
                    // Воспроизводим звук начала кризиса
                    if (!oxygenCrisisStartPlayed)
                    {
                        PlayWarningSound(oxygenCrisisStartSound);
                        oxygenCrisisStartPlayed = true;
                        Debug.LogWarning("🔴 ТРЕВОГА: Все пины вынуты! Кислород начинает падать!");
                    }
                    
                    OnCriticalWarning?.Invoke();
                    OnOxygenWarning?.Invoke();
                    Debug.LogWarning("⚠️ КРИТИЧЕСКОЕ: Все пины вынуты или изношены! Кислород падает!");
                }
                
                oxygenLevel -= oxygenDepletionRate * Time.deltaTime;
                oxygenLevel = Mathf.Clamp(oxygenLevel, 0f, 100f);
                
                // НОВОЕ: Пороговые предупреждения кислорода
                
                // Предупреждение при 50% кислорода
                if (oxygenLevel <= 50f && previousOxygenLevel > 50f && !oxygen50WarningPlayed)
                {
                    PlayWarningSound(oxygen50PercentWarning);
                    oxygen50WarningPlayed = true;
                    Debug.LogWarning("⚠️ ВНИМАНИЕ: Кислород упал до 50%!");
                }
                
                // Предупреждение при 25% кислорода
                if (oxygenLevel <= 25f && previousOxygenLevel > 25f && !oxygen25WarningPlayed)
                {
                    PlayWarningSound(oxygen25PercentWarning);
                    oxygen25WarningPlayed = true;
                    OnCriticalWarning?.Invoke();
                    Debug.LogWarning("🚨 ОПАСНОСТЬ: Кислород упал до 25%!");
                }
                
                // Последний шанс при 10% кислорода
                if (oxygenLevel <= 10f && previousOxygenLevel > 10f && !oxygen10WarningPlayed)
                {
                    PlayWarningSound(oxygen10PercentWarning);
                    oxygen10WarningPlayed = true;
                    Debug.LogWarning("💀 КРИТИЧЕСКОЕ: Кислород 10%! Срочно вставьте пин!");
                }
                
                // Постоянная тревога при низком кислороде (< 30%)
                if (oxygenLevel < 30f && oxygenAlarmLoop != null)
                {
                    if (Time.time - lastOxygenAlarmTime >= criticalBeepInterval)
                    {
                        PlayWarningSound(oxygenAlarmLoop);
                        lastOxygenAlarmTime = Time.time;
                    }
                }
                
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
                    
                    // Сбрасываем флаги при восстановлении
                    oxygenCrisisStartPlayed = false;
                }
                
                oxygenLevel += (oxygenDepletionRate / 2f) * Time.deltaTime;
                oxygenLevel = Mathf.Clamp(oxygenLevel, 0f, 100f);
                
                // Сброс флагов предупреждений при восстановлении кислорода
                if (oxygenLevel > 55f)
                {
                    oxygen50WarningPlayed = false;
                }
                if (oxygenLevel > 30f)
                {
                    oxygen25WarningPlayed = false;
                }
                if (oxygenLevel > 15f)
                {
                    oxygen10WarningPlayed = false;
                }
                
                if (criticalOxygenEffects != null)
                {
                    criticalOxygenEffects.SetActive(false);
                }
            }
            
            previousOxygenLevel = oxygenLevel;
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
        
        #region Audio Warning System
        
        /// <summary>
        /// Воспроизведение звукового предупреждения
        /// </summary>
        void PlayWarningSound(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (warningAudioSource != null && clip != null)
            {
                warningAudioSource.PlayOneShot(clip, warningVolume * volumeMultiplier);
                Debug.Log($"🔊 Звук: {clip.name}");
            }
        }
        
        /// <summary>
        /// Воспроизведение предупреждения с проверкой cooldown
        /// </summary>
        void PlayWarningWithCooldown(AudioClip clip, ref float lastPlayTime)
        {
            if (Time.time - lastPlayTime >= warningCooldown)
            {
                PlayWarningSound(clip);
                lastPlayTime = Time.time;
            }
        }
        
        #endregion
        
        #region Warning Indicators
        
        void UpdateWarningIndicators()
        {
            SetIndicatorActive(hullWarningIndicator, hullIntegrity < 50f);
            SetIndicatorActive(healthWarningIndicator, playerHealth < 50f);
            SetIndicatorActive(oxygenWarningIndicator, oxygenLevel < 50f || isOxygenCritical);
            SetIndicatorActive(controlWarningIndicator, controlMalfunctionTimer > 0f);
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
                TriggerGameOver("Кислород закончился! Все системы кислорода отказали.");
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
            
            OnGameOver?.Invoke(reason);
            
            StopAllSystems();
            
            if (disableInteractionOnGameOver)
            {
                DisablePlayerInteraction();
            }
            
            if (!useGameOverUI)
            {
                StartCoroutine(GameOverSequence(reason));
            }
        }
        
        void DisablePlayerInteraction()
        {
            Debug.Log("🔒 Блокировка взаимодействия игрока...");
            
            foreach (Grabber grabber in playerGrabbers)
            {
                if (grabber != null)
                {
                    grabber.enabled = false;
                }
            }
            
            if (playerLocomotion != null)
            {
                playerLocomotion.enabled = false;
            }
            
            if (playerTeleport != null)
            {
                playerTeleport.enabled = false;
            }
            
            Debug.Log($"✓ Отключено: {playerGrabbers.Count} Grabber'ов, Locomotion, Teleport");
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
        
        public TimerInfo GetControlTimer()
        {
            if (controlPanelMalfunction != null && controlPanelMalfunction.IsMalfunctionActive() 
                && !controlPanelMalfunction.IsCurrentlyFixed())
            {
                return new TimerInfo(controlMalfunctionTimer, maxControlMalfunctionTime);
            }
            return new TimerInfo(-1, 0);
        }
        
        public TimerInfo GetBatteryTimer()
        {
            if (batteryReplacementEvent != null && batteryReplacementEvent.IsEventActive 
                && !batteryReplacementEvent.IsCompleted)
            {
                return new TimerInfo(batteryEmptyTimer, maxBatteryEmptyTime);
            }
            return new TimerInfo(-1, 0);
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
            
            // Сбрасываем все флаги предупреждений
            ResetAllWarningFlags();
            
            // Сбрасываем предыдущие значения
            previousHullIntegrity = 100f;
            previousOxygenLevel = 100f;
            previousPlayerHealth = 100f;
        }
        
        void ResetAllWarningFlags()
        {
            // Контроль
            control30SecWarningPlayed = false;
            control10SecWarningPlayed = false;
            controlLastBeepTime = 0f;
            
            // Кислород
            oxygenCrisisStartPlayed = false;
            oxygen50WarningPlayed = false;
            oxygen25WarningPlayed = false;
            oxygen10WarningPlayed = false;
            lastOxygenAlarmTime = 0f;
            
            // Батарея
            batteryCrisisStartPlayed = false;
            batteryWarningPlayed = false;
            lastBatteryBeepTime = 0f;
            
            // Герметичность
            hull50WarningPlayed = false;
            hull35WarningPlayed = false;
            hull25WarningPlayed = false;
            lastHullAlarmTime = 0f;
            
            // Здоровье
            health50WarningPlayed = false;
            health25WarningPlayed = false;
            lastHeartbeatTime = 0f;
        }
        
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
        
        public bool IsGameOver() => isGameOver;
        
        /// <summary>
        /// Принудительно воспроизвести предупреждение (для тестирования)
        /// </summary>
        public void TestPlayWarning(string warningType)
        {
            switch (warningType.ToLower())
            {
                case "oxygen":
                    PlayWarningSound(oxygenCrisisStartSound);
                    break;
                case "battery":
                    PlayWarningSound(batteryCrisisStartSound);
                    break;
                case "hull":
                    PlayWarningSound(hull50PercentWarning);
                    break;
                case "health":
                    PlayWarningSound(health50PercentWarning);
                    break;
                case "control":
                    PlayWarningSound(timer30SecWarning);
                    break;
            }
        }
        
        #endregion
    }
    
    [System.Serializable]
    public struct TimerInfo
    {
        public float currentTime;
        public float maxTime;
        public bool isActive;
        
        public TimerInfo(float current, float max)
        {
            currentTime = current;
            maxTime = max;
            isActive = current >= 0;
        }
        
        public float GetRemainingTime()
        {
            return isActive ? (maxTime - currentTime) : 0f;
        }
    }
}