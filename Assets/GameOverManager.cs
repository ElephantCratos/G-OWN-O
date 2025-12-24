using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BNG
{
    /// <summary>
    /// Управляет критериями поражения и состоянием корабля
    /// ОБНОВЛЕНО: Блокировка игры после Game Over + Звуковые предупреждения таймеров
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
        public float criticalOxygenLevel = 0f;
        private bool isOxygenCritical = false;
        
        [Header("=== ЗВУКОВЫЕ ПРЕДУПРЕЖДЕНИЯ ТАЙМЕРОВ ===")]
        [Tooltip("Источник звука для предупреждений")]
        public AudioSource timerWarningAudioSource;
        
        [Tooltip("Звук предупреждения за 30 секунд")]
        public AudioClip timer30SecWarning;
        
        [Tooltip("Звук критического предупреждения (за 10 сек)")]
        public AudioClip timer10SecWarning;
        
        [Tooltip("Звуковой сигнал каждые 5 секунд в критическое время")]
        public AudioClip timerBeepSound;
        
        [Tooltip("Интервал сигналов в критическое время")]
        public float criticalBeepInterval = 5f;
        
        // Флаги для отслеживания воспроизведенных предупреждений
        private bool control30SecWarningPlayed = false;
        private bool control10SecWarningPlayed = false;
        private float controlLastBeepTime = 0f;
        
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
        
        // НОВОЕ: События для таймеров
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
            
            // НОВОЕ: Автопоиск AudioSource если не назначен
            if (timerWarningAudioSource == null)
            {
                timerWarningAudioSource = gameObject.GetComponent<AudioSource>();
                if (timerWarningAudioSource == null)
                {
                    timerWarningAudioSource = gameObject.AddComponent<AudioSource>();
                    timerWarningAudioSource.playOnAwake = false;
                    timerWarningAudioSource.spatialBlend = 0f; // 2D звук
                }
            }
            
            SetIndicatorActive(hullWarningIndicator, false);
            SetIndicatorActive(healthWarningIndicator, false);
            SetIndicatorActive(oxygenWarningIndicator, false);
            SetIndicatorActive(controlWarningIndicator, false);
            SetIndicatorActive(batteryWarningIndicator, false);
            SetIndicatorActive(biteDamageVignette, false);
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
                
                // ОБНОВЛЕНО: Уведомляем UI о таймере
                OnControlTimerUpdate?.Invoke(controlMalfunctionTimer);
                
                float remaining = maxControlMalfunctionTime - controlMalfunctionTimer;
                
                // НОВОЕ: Звуковое предупреждение за 30 секунд
                if (remaining <= 30f && !control30SecWarningPlayed)
                {
                    PlayTimerWarning(timer30SecWarning);
                    control30SecWarningPlayed = true;
                    OnCriticalWarning?.Invoke();
                    OnControlWarning?.Invoke();
                    Debug.LogWarning("⚠️ ПРЕДУПРЕЖДЕНИЕ: 30 секунд до потери управления!");
                }
                
                // НОВОЕ: Критическое предупреждение за 10 секунд
                if (remaining <= 10f && !control10SecWarningPlayed)
                {
                    PlayTimerWarning(timer10SecWarning);
                    control10SecWarningPlayed = true;
                    Debug.LogWarning("🚨 КРИТИЧЕСКОЕ: 10 секунд до потери управления!");
                }
                
                // НОВОЕ: Звуковые сигналы каждые 5 секунд в критическое время
                if (remaining <= 30f && Time.time - controlLastBeepTime >= criticalBeepInterval)
                {
                    PlayTimerWarning(timerBeepSound);
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
                
                // НОВОЕ: Уведомляем UI о таймере
                OnBatteryTimerUpdate?.Invoke(batteryEmptyTimer);
                
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
        
        #region Audio Warning System
        
        // НОВОЕ: Воспроизведение звукового предупреждения
        void PlayTimerWarning(AudioClip clip)
        {
            if (timerWarningAudioSource != null && clip != null)
            {
                timerWarningAudioSource.PlayOneShot(clip);
                Debug.Log($"🔊 Воспроизведено предупреждение: {clip.name}");
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
        
        // НОВОЕ: Методы для получения информации о таймерах
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
            
            // Сбрасываем флаги предупреждений
            control30SecWarningPlayed = false;
            control10SecWarningPlayed = false;
            controlLastBeepTime = 0f;
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
        
        #endregion
    }
    
    // НОВОЕ: Структура для передачи информации о таймере
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