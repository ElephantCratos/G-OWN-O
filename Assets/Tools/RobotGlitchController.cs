using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BNG
{
    /// <summary>
    /// Контроллер глитч-эффектов для робота
    /// Управляет шейдером и пост-процессингом
    /// </summary>
    public class RobotGlitchController : MonoBehaviour
    {
        [Header("=== МАТЕРИАЛЫ ===")]
        [Tooltip("Материал с глитч-шейдером")]
        public Material glitchMaterial;
        
        [Header("=== UI ЭЛЕМЕНТЫ ===")]
        public Image glitchOverlayImage;
        public Image staticOverlayImage;
        public Image scanlineOverlayImage;
        public Image vignetteImage;
        
        [Header("=== НАСТРОЙКИ ГЛИТЧА ===")]
        [Range(0f, 1f)]
        public float baseGlitchIntensity = 0f;
        [Range(0f, 1f)]
        public float baseScanlineIntensity = 0.05f;
        [Range(0f, 1f)]
        public float baseNoiseIntensity = 0f;
        
        [Header("=== ЦВЕТА ===")]
        public Color glitchColor = new Color(0f, 0.8f, 1f, 1f);
        public Color warningColor = new Color(1f, 0.3f, 0f, 1f);
        public Color damageVignetteColor = new Color(1f, 0.5f, 0f, 0.6f);
        
        [Header("=== СВЯЗЬ С ИГРОЙ ===")]
        public GameOverManager gameOverManager;
        
        // Текущие значения
        private float currentGlitchIntensity = 0f;
        private float currentNoiseIntensity = 0f;
        private float targetGlitchIntensity = 0f;
        private float targetNoiseIntensity = 0f;
        
        // Корутины
        private Coroutine damageGlitchCoroutine;
        private Coroutine criticalGlitchCoroutine;
        
        void Start()
        {
            // Инициализируем материал
            if (glitchMaterial != null)
            {
                glitchMaterial.SetFloat("_GlitchIntensity", 0f);
                glitchMaterial.SetFloat("_ScanlineIntensity", baseScanlineIntensity);
                glitchMaterial.SetFloat("_NoiseIntensity", 0f);
                glitchMaterial.SetColor("_GlitchColor", glitchColor);
                glitchMaterial.SetColor("_WarningColor", warningColor);
            }
            
            // Применяем материал к оверлею
            if (glitchOverlayImage != null && glitchMaterial != null)
            {
                glitchOverlayImage.material = glitchMaterial;
            }
            
            // Подписываемся на события
            if (gameOverManager != null)
            {
                gameOverManager.OnRatBite.AddListener(OnDamageTaken);
                gameOverManager.OnCriticalWarning.AddListener(OnCriticalState);
                gameOverManager.OnGameOver.AddListener(OnGameOver);
            }
            
            // Скрываем виньетку
            SetVignetteAlpha(0f);
        }
        
        void Update()
        {
            // Плавно интерполируем значения
            currentGlitchIntensity = Mathf.Lerp(currentGlitchIntensity, targetGlitchIntensity, Time.deltaTime * 5f);
            currentNoiseIntensity = Mathf.Lerp(currentNoiseIntensity, targetNoiseIntensity, Time.deltaTime * 5f);
            
            // Добавляем базовый глитч при низком здоровье
            float healthBasedGlitch = 0f;
            if (gameOverManager != null)
            {
                float healthPercent = gameOverManager.playerHealth / gameOverManager.maxPlayerHealth;
                if (healthPercent < 0.3f)
                {
                    healthBasedGlitch = (0.3f - healthPercent) / 0.3f * 0.3f;
                    // Добавляем случайные спайки
                    if (Random.value < 0.02f)
                    {
                        healthBasedGlitch += Random.Range(0.2f, 0.5f);
                    }
                }
            }
            
            // Применяем к шейдеру
            float finalGlitch = Mathf.Max(currentGlitchIntensity, healthBasedGlitch, baseGlitchIntensity);
            float finalNoise = Mathf.Max(currentNoiseIntensity, baseNoiseIntensity);
            
            if (glitchMaterial != null)
            {
                glitchMaterial.SetFloat("_GlitchIntensity", finalGlitch);
                glitchMaterial.SetFloat("_NoiseIntensity", finalNoise);
            }
            
            // Управляем видимостью оверлея
            if (glitchOverlayImage != null)
            {
                glitchOverlayImage.gameObject.SetActive(finalGlitch > 0.01f);
            }
        }
        
        #region Damage Effects
        
        public void OnDamageTaken(float damage)
        {
            if (damageGlitchCoroutine != null)
                StopCoroutine(damageGlitchCoroutine);
            damageGlitchCoroutine = StartCoroutine(DamageGlitchEffect(damage));
        }
        
        IEnumerator DamageGlitchEffect(float damage)
        {
            // Интенсивность зависит от урона
            float intensity = Mathf.Clamp01(damage / 30f) * 0.8f + 0.2f;
            
            // Резкий всплеск
            targetGlitchIntensity = intensity;
            targetNoiseIntensity = intensity * 0.5f;
            
            // Виньетка повреждения
            yield return StartCoroutine(FlashVignette(damageVignetteColor, 0.3f));
            
            // Быстрое затухание
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                targetGlitchIntensity = Mathf.Lerp(intensity, 0f, t);
                targetNoiseIntensity = Mathf.Lerp(intensity * 0.5f, 0f, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            targetGlitchIntensity = 0f;
            targetNoiseIntensity = 0f;
        }
        
        IEnumerator FlashVignette(Color color, float duration)
        {
            float halfDuration = duration / 2f;
            
            // Появление
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float alpha = Mathf.Lerp(0f, color.a, elapsed / halfDuration);
                SetVignetteColor(new Color(color.r, color.g, color.b, alpha));
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Затухание
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float alpha = Mathf.Lerp(color.a, 0f, elapsed / halfDuration);
                SetVignetteColor(new Color(color.r, color.g, color.b, alpha));
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            SetVignetteAlpha(0f);
        }
        
        #endregion
        
        #region Critical Effects
        
        public void OnCriticalState()
        {
            if (criticalGlitchCoroutine != null)
                StopCoroutine(criticalGlitchCoroutine);
            criticalGlitchCoroutine = StartCoroutine(CriticalGlitchEffect());
        }
        
        IEnumerator CriticalGlitchEffect()
        {
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // Случайные интенсивные спайки
                targetGlitchIntensity = Random.Range(0.5f, 1f);
                targetNoiseIntensity = Random.Range(0.3f, 0.7f);
                
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                
                elapsed += 0.1f;
            }
            
            // Затухание
            targetGlitchIntensity = 0f;
            targetNoiseIntensity = 0f;
        }
        
        #endregion
        
        #region Game Over
        
        public void OnGameOver(string reason)
        {
            StartCoroutine(GameOverEffect());
        }
        
        IEnumerator GameOverEffect()
        {
            // Нарастающий глитч
            float elapsed = 0f;
            float duration = 3f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                targetGlitchIntensity = Mathf.Lerp(0.3f, 1f, t);
                targetNoiseIntensity = Mathf.Lerp(0.2f, 0.8f, t);
                
                // Случайные скачки
                if (Random.value < 0.1f)
                {
                    targetGlitchIntensity = 1f;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Финальный "выключение экрана"
            targetGlitchIntensity = 1f;
            targetNoiseIntensity = 1f;
        }
        
        #endregion
        
        #region Utility
        
        void SetVignetteAlpha(float alpha)
        {
            if (vignetteImage == null) return;
            
            Color c = vignetteImage.color;
            c.a = alpha;
            vignetteImage.color = c;
            vignetteImage.gameObject.SetActive(alpha > 0.01f);
        }
        
        void SetVignetteColor(Color color)
        {
            if (vignetteImage == null) return;
            
            vignetteImage.color = color;
            vignetteImage.gameObject.SetActive(color.a > 0.01f);
        }
        
        /// <summary>
        /// Принудительно запустить глитч
        /// </summary>
        public void TriggerGlitch(float intensity, float duration)
        {
            StartCoroutine(ManualGlitch(intensity, duration));
        }
        
        IEnumerator ManualGlitch(float intensity, float duration)
        {
            targetGlitchIntensity = intensity;
            yield return new WaitForSeconds(duration);
            targetGlitchIntensity = 0f;
        }
        
        #endregion
    }
}