using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BNG
{
    /// <summary>
    /// Визуализация состояния робота в стиле технического HUD
    /// </summary>
    public class RobotHUDManager : MonoBehaviour
    {
        [Header("=== ССЫЛКИ ===")]
        public GameOverManager gameOverManager;
        public Canvas hudCanvas;
        
        [Header("=== ПОЛОСЫ СОСТОЯНИЯ ===")]
        [Tooltip("Полоса целостности корпуса")]
        public Image hullIntegrityBar;
        public Image hullIntegrityBarBackground;
        public TextMeshProUGUI hullIntegrityText;
        
        [Tooltip("Полоса заряда ядра (здоровье)")]
        public Image coreChargeBar;
        public Image coreChargeBarBackground;
        public TextMeshProUGUI coreChargeText;
        
        [Tooltip("Полоса кислорода/охлаждения")]
        public Image oxygenBar;
        public Image oxygenBarBackground;
        public TextMeshProUGUI oxygenText;
        
        [Header("=== ИКОНКИ ПРЕДУПРЕЖДЕНИЙ ===")]
        public GameObject warningPanel;
        public Image hullWarningIcon;
        public Image coreWarningIcon;
        public Image oxygenWarningIcon;
        public Image controlWarningIcon;
        public Image batteryWarningIcon;
        public Image ratWarningIcon;
        
        [Header("=== ЦВЕТА ===")]
        public Color normalColor = new Color(0.2f, 0.8f, 1f, 1f);      // Голубой
        public Color warningColor = new Color(1f, 0.8f, 0.2f, 1f);     // Жёлтый
        public Color criticalColor = new Color(1f, 0.2f, 0.2f, 1f);    // Красный
        public Color oxygenColor = new Color(0.2f, 1f, 0.6f, 1f);      // Зелёный
        
        [Header("=== ЭФФЕКТЫ ПОВРЕЖДЕНИЙ ===")]
        [Tooltip("Оверлей для глитч-эффекта")]
        public Image glitchOverlay;
        [Tooltip("Оверлей статических помех")]
        public Image staticNoiseOverlay;
        [Tooltip("Оверлей сканлайнов")]
        public Image scanlineOverlay;
        [Tooltip("Рамка повреждений по краям экрана")]
        public Image damageVignette;
        
        [Header("=== ЭФФЕКТ ИСКР ===")]
        public ParticleSystem sparkParticles;
        public Transform sparkSpawnPoint;
        
        [Header("=== ЗВУКИ ===")]
        public AudioSource hudAudioSource;
        public AudioClip[] glitchSounds;
        public AudioClip[] warningSounds;
        public AudioClip criticalAlarmSound;
        public AudioClip systemErrorSound;
        
        [Header("=== НАСТРОЙКИ АНИМАЦИИ ===")]
        public float barLerpSpeed = 5f;
        public float glitchDuration = 0.3f;
        public float warningBlinkSpeed = 2f;
        
        [Header("=== ТЕКСТОВЫЕ СООБЩЕНИЯ ===")]
        public TextMeshProUGUI systemStatusText;
        public TextMeshProUGUI warningMessageText;
        
        // Приватные переменные
        private float targetHullIntegrity = 100f;
        private float targetCoreCharge = 100f;
        private float targetOxygen = 100f;
        
        private float currentHullDisplay = 100f;
        private float currentCoreDisplay = 100f;
        private float currentOxygenDisplay = 100f;
        
        private bool isGlitching = false;
        private Coroutine glitchCoroutine;
        private Coroutine warningCoroutine;
        
        private Dictionary<Image, Coroutine> blinkingIcons = new Dictionary<Image, Coroutine>();
        
        void Start()
        {
            // Подписываемся на события GameOverManager
            if (gameOverManager != null)
            {
                gameOverManager.OnHullIntegrityChanged.AddListener(UpdateHullIntegrity);
                gameOverManager.OnPlayerHealthChanged.AddListener(UpdateCoreCharge);
                gameOverManager.OnOxygenLevelChanged.AddListener(UpdateOxygen);
                gameOverManager.OnRatBite.AddListener(OnDamageTaken);
                gameOverManager.OnCriticalWarning.AddListener(OnCriticalWarning);
                gameOverManager.OnGameOver.AddListener(OnGameOver);
            }
            
            // Скрываем эффекты на старте
            SetOverlayAlpha(glitchOverlay, 0f);
            SetOverlayAlpha(staticNoiseOverlay, 0f);
            SetOverlayAlpha(damageVignette, 0f);
            
            // Скрываем все предупреждения
            HideAllWarnings();
            
            // Инициализируем полосы
            UpdateAllBars();
        }
        
        void Update()
        {
            // Плавно обновляем полосы
            currentHullDisplay = Mathf.Lerp(currentHullDisplay, targetHullIntegrity, Time.deltaTime * barLerpSpeed);
            currentCoreDisplay = Mathf.Lerp(currentCoreDisplay, targetCoreCharge, Time.deltaTime * barLerpSpeed);
            currentOxygenDisplay = Mathf.Lerp(currentOxygenDisplay, targetOxygen, Time.deltaTime * barLerpSpeed);
            
            UpdateAllBars();
            UpdateWarningIcons();
            UpdateSystemStatus();
        }
        
        #region Bar Updates
        
        void UpdateAllBars()
        {
            // Hull Integrity (Целостность корпуса)
            if (hullIntegrityBar != null)
            {
                hullIntegrityBar.fillAmount = currentHullDisplay / 100f;
                hullIntegrityBar.color = GetStatusColor(currentHullDisplay);
            }
            if (hullIntegrityText != null)
            {
                hullIntegrityText.text = $"HULL: {currentHullDisplay:F0}%";
                hullIntegrityText.color = GetStatusColor(currentHullDisplay);
            }
            
            // Core Charge (Заряд ядра / Здоровье)
            if (coreChargeBar != null)
            {
                coreChargeBar.fillAmount = currentCoreDisplay / 100f;
                coreChargeBar.color = GetStatusColor(currentCoreDisplay);
            }
            if (coreChargeText != null)
            {
                coreChargeText.text = $"CORE: {currentCoreDisplay:F0}%";
                coreChargeText.color = GetStatusColor(currentCoreDisplay);
            }
            
            // Oxygen / Cooling (Кислород / Охлаждение)
            if (oxygenBar != null)
            {
                oxygenBar.fillAmount = currentOxygenDisplay / 100f;
                oxygenBar.color = currentOxygenDisplay > 30f ? oxygenColor : criticalColor;
            }
            if (oxygenText != null)
            {
                oxygenText.text = $"O2: {currentOxygenDisplay:F0}%";
                oxygenText.color = currentOxygenDisplay > 30f ? oxygenColor : criticalColor;
            }
        }
        
        Color GetStatusColor(float value)
        {
            if (value > 60f) return normalColor;
            if (value > 30f) return warningColor;
            return criticalColor;
        }
        
        #endregion
        
        #region Event Handlers
        
        public void UpdateHullIntegrity(float value)
        {
            targetHullIntegrity = value;
            
            // Глитч при резком падении
            if (targetHullIntegrity < currentHullDisplay - 5f)
            {
                TriggerGlitchEffect(0.2f);
            }
        }
        
        public void UpdateCoreCharge(float value)
        {
            // Конвертируем HP в проценты
            float maxHealth = gameOverManager != null ? gameOverManager.maxPlayerHealth : 100f;
            targetCoreCharge = (value / maxHealth) * 100f;
        }
        
        public void UpdateOxygen(float value)
        {
            targetOxygen = value;
        }
        
        public void OnDamageTaken(float damage)
        {
            // Визуальный глитч
            TriggerGlitchEffect(glitchDuration);
            
            // Красная виньетка
            StartCoroutine(FlashDamageVignette());
            
            // Искры
            SpawnSparks();
            
            // Звук
            PlayRandomSound(glitchSounds);
            
            // Сообщение
            ShowWarningMessage($"ВНИМАНИЕ: ПОВРЕЖДЕНИЕ КОРПУСА [-{damage:F0}]", 2f);
        }
        
        public void OnCriticalWarning()
        {
            // Интенсивный глитч
            TriggerGlitchEffect(1f);
            
            // Звук тревоги
            if (hudAudioSource != null && criticalAlarmSound != null)
            {
                hudAudioSource.PlayOneShot(criticalAlarmSound);
            }
            
            // Мигающее предупреждение
            ShowWarningMessage("⚠ КРИТИЧЕСКАЯ ОШИБКА СИСТЕМЫ ⚠", 5f);
            
            // Статические помехи
            StartCoroutine(StaticNoiseEffect(2f));
        }
        
        public void OnGameOver(string reason)
        {
            // Полный экран глитча
            StartCoroutine(GameOverGlitchSequence(reason));
        }
        
        #endregion
        
        #region Visual Effects
        
        public void TriggerGlitchEffect(float duration)
        {
            if (glitchCoroutine != null)
                StopCoroutine(glitchCoroutine);
            glitchCoroutine = StartCoroutine(GlitchEffectCoroutine(duration));
        }
        
        IEnumerator GlitchEffectCoroutine(float duration)
        {
            isGlitching = true;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // Случайная интенсивность глитча
                float intensity = Random.Range(0.3f, 1f);
                SetOverlayAlpha(glitchOverlay, intensity * 0.5f);
                
                // Случайное смещение UI элементов (опционально)
                if (hudCanvas != null)
                {
                    Vector3 offset = new Vector3(
                        Random.Range(-5f, 5f),
                        Random.Range(-5f, 5f),
                        0
                    );
                    // hudCanvas.transform.localPosition = offset;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            SetOverlayAlpha(glitchOverlay, 0f);
            if (hudCanvas != null)
            {
                hudCanvas.transform.localPosition = Vector3.zero;
            }
            isGlitching = false;
        }
        
        IEnumerator FlashDamageVignette()
        {
            // Быстрое появление
            float alpha = 0f;
            while (alpha < 0.6f)
            {
                alpha += Time.deltaTime * 8f;
                SetOverlayAlpha(damageVignette, alpha);
                
                // Меняем цвет на "электрический" голубой с красным
                if (damageVignette != null)
                {
                    damageVignette.color = Color.Lerp(
                        new Color(0f, 0.5f, 1f, alpha),  // Электрический голубой
                        new Color(1f, 0.3f, 0f, alpha), // Оранжевый (перегрев)
                        Mathf.PingPong(Time.time * 10f, 1f)
                    );
                }
                yield return null;
            }
            
            // Медленное затухание
            while (alpha > 0f)
            {
                alpha -= Time.deltaTime * 3f;
                SetOverlayAlpha(damageVignette, Mathf.Max(0, alpha));
                yield return null;
            }
        }
        
        IEnumerator StaticNoiseEffect(float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float intensity = Mathf.Sin(elapsed * 20f) * 0.3f + 0.2f;
                SetOverlayAlpha(staticNoiseOverlay, intensity);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            SetOverlayAlpha(staticNoiseOverlay, 0f);
        }
        
        IEnumerator GameOverGlitchSequence(string reason)
        {
            // Интенсивные помехи
            for (int i = 0; i < 10; i++)
            {
                SetOverlayAlpha(glitchOverlay, Random.Range(0.5f, 1f));
                SetOverlayAlpha(staticNoiseOverlay, Random.Range(0.3f, 0.8f));
                
                if (hudAudioSource != null && systemErrorSound != null)
                {
                    hudAudioSource.PlayOneShot(systemErrorSound, 0.5f);
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            // Финальное затемнение
            SetOverlayAlpha(glitchOverlay, 1f);
            
            // Показываем причину
            if (systemStatusText != null)
            {
                systemStatusText.text = $"SYSTEM FAILURE\n{reason}";
                systemStatusText.color = criticalColor;
                systemStatusText.fontSize = 48;
            }
        }
        
        void SpawnSparks()
        {
            if (sparkParticles != null)
            {
                // Спавним искры в случайной позиции около игрока
                if (sparkSpawnPoint != null)
                {
                    sparkParticles.transform.position = sparkSpawnPoint.position + 
                        Random.insideUnitSphere * 0.3f;
                }
                sparkParticles.Play();
            }
        }
        
        #endregion
        
        #region Warning Icons
        
        void UpdateWarningIcons()
        {
            if (gameOverManager == null) return;
            
            // Hull Warning
            UpdateWarningIcon(hullWarningIcon, gameOverManager.hullIntegrity < 50f, 
                gameOverManager.hullIntegrity < 30f);
            
            // Core Warning (Health)
            UpdateWarningIcon(coreWarningIcon, gameOverManager.playerHealth < 50f,
                gameOverManager.playerHealth < 30f);
            
            // Oxygen Warning
            UpdateWarningIcon(oxygenWarningIcon, gameOverManager.oxygenLevel < 50f,
                gameOverManager.oxygenLevel < 30f);
            
            // Control Warning
            UpdateWarningIcon(controlWarningIcon, gameOverManager.controlMalfunctionTimer > 0f,
                gameOverManager.controlMalfunctionTimer > 200f);
            
            // Battery Warning
            UpdateWarningIcon(batteryWarningIcon, gameOverManager.batteryEmptyTimer > 0f,
                gameOverManager.batteryEmptyTimer > 20f);
            
            // Rat Warning (nearby rats)
            int nearbyRats = gameOverManager.GetNearbyRatsCount();
            UpdateWarningIcon(ratWarningIcon, nearbyRats > 0, nearbyRats >= 3);
        }
        
        void UpdateWarningIcon(Image icon, bool showWarning, bool isCritical)
        {
            if (icon == null) return;
            
            if (!showWarning)
            {
                icon.gameObject.SetActive(false);
                StopIconBlink(icon);
                return;
            }
            
            icon.gameObject.SetActive(true);
            icon.color = isCritical ? criticalColor : warningColor;
            
            if (isCritical)
            {
                StartIconBlink(icon);
            }
            else
            {
                StopIconBlink(icon);
            }
        }
        
        void StartIconBlink(Image icon)
        {
            if (blinkingIcons.ContainsKey(icon)) return;
            
            blinkingIcons[icon] = StartCoroutine(BlinkIcon(icon));
        }
        
        void StopIconBlink(Image icon)
        {
            if (!blinkingIcons.ContainsKey(icon)) return;
            
            StopCoroutine(blinkingIcons[icon]);
            blinkingIcons.Remove(icon);
            
            if (icon != null)
            {
                Color c = icon.color;
                c.a = 1f;
                icon.color = c;
            }
        }
        
        IEnumerator BlinkIcon(Image icon)
        {
            while (true)
            {
                float alpha = (Mathf.Sin(Time.time * warningBlinkSpeed * Mathf.PI * 2f) + 1f) / 2f;
                alpha = Mathf.Lerp(0.3f, 1f, alpha);
                
                if (icon != null)
                {
                    Color c = icon.color;
                    c.a = alpha;
                    icon.color = c;
                }
                
                yield return null;
            }
        }
        
        void HideAllWarnings()
        {
            SetIconActive(hullWarningIcon, false);
            SetIconActive(coreWarningIcon, false);
            SetIconActive(oxygenWarningIcon, false);
            SetIconActive(controlWarningIcon, false);
            SetIconActive(batteryWarningIcon, false);
            SetIconActive(ratWarningIcon, false);
        }
        
        void SetIconActive(Image icon, bool active)
        {
            if (icon != null)
                icon.gameObject.SetActive(active);
        }
        
        #endregion
        
        #region Status Text
        
        void UpdateSystemStatus()
        {
            if (systemStatusText == null || gameOverManager == null) return;
            
            // Формируем статус в стиле робота
            string status = "SYSTEM STATUS: ";
            
            if (currentHullDisplay < 30f || currentCoreDisplay < 30f || currentOxygenDisplay < 30f)
            {
                status += "CRITICAL";
                systemStatusText.color = criticalColor;
            }
            else if (currentHullDisplay < 60f || currentCoreDisplay < 60f || currentOxygenDisplay < 60f)
            {
                status += "WARNING";
                systemStatusText.color = warningColor;
            }
            else
            {
                status += "NOMINAL";
                systemStatusText.color = normalColor;
            }
            
            systemStatusText.text = status;
        }
        
        public void ShowWarningMessage(string message, float duration)
        {
            if (warningCoroutine != null)
                StopCoroutine(warningCoroutine);
            warningCoroutine = StartCoroutine(ShowWarningMessageCoroutine(message, duration));
        }
        
        IEnumerator ShowWarningMessageCoroutine(string message, float duration)
        {
            if (warningMessageText == null) yield break;
            
            warningMessageText.gameObject.SetActive(true);
            warningMessageText.text = message;
            warningMessageText.color = criticalColor;
            
            // Эффект печатающегося текста
            string fullMessage = message;
            warningMessageText.text = "";
            
            foreach (char c in fullMessage)
            {
                warningMessageText.text += c;
                yield return new WaitForSeconds(0.02f);
            }
            
            yield return new WaitForSeconds(duration);
            
            // Затухание
            float alpha = 1f;
            while (alpha > 0f)
            {
                alpha -= Time.deltaTime;
                warningMessageText.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, alpha);
                yield return null;
            }
            
            warningMessageText.gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Utility
        
        void SetOverlayAlpha(Image overlay, float alpha)
        {
            if (overlay == null) return;
            
            Color c = overlay.color;
            c.a = alpha;
            overlay.color = c;
            overlay.gameObject.SetActive(alpha > 0.01f);
        }
        
        void PlayRandomSound(AudioClip[] clips)
        {
            if (hudAudioSource == null || clips == null || clips.Length == 0) return;
            
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            hudAudioSource.PlayOneShot(clip);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Показать кастомное предупреждение
        /// </summary>
        public void ShowCustomWarning(string message)
        {
            ShowWarningMessage(message, 3f);
            PlayRandomSound(warningSounds);
        }
        
        /// <summary>
        /// Принудительно запустить глитч-эффект
        /// </summary>
        public void ForceGlitch(float intensity, float duration)
        {
            TriggerGlitchEffect(duration);
        }
        
        #endregion
    }
}