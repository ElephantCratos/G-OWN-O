using UnityEngine;
using TMPro;

namespace BNG
{
    /// <summary>
    /// UI для отображения состояния систем корабля
    /// </summary>
    public class ShipSystemsUI : MonoBehaviour
    {
        [Header("Ссылки")]
        public GameOverManager gameOverManager;
        
        [Header("UI Elements - Hull Integrity")]
        public UnityEngine.UI.Slider hullIntegritySlider;
        public TextMeshProUGUI hullIntegrityText;
        public UnityEngine.UI.Image hullIntegrityFill;
        public GameObject hullWarningIcon;
        
        [Header("UI Elements - Player Health")]
        public UnityEngine.UI.Slider healthSlider;
        public TextMeshProUGUI healthText;
        public UnityEngine.UI.Image healthFill;
        public GameObject healthWarningIcon;
        
        [Header("UI Elements - Oxygen")]
        public UnityEngine.UI.Slider oxygenSlider;
        public TextMeshProUGUI oxygenText;
        public UnityEngine.UI.Image oxygenFill;
        public GameObject oxygenWarningIcon;
        
        [Header("UI Elements - Warnings")]
        public TextMeshProUGUI controlMalfunctionWarning;
        public TextMeshProUGUI batteryEmergencyWarning;
        public GameObject criticalWarningPanel;
        
        [Header("UI Elements - Game Over")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverReasonText;
        public TextMeshProUGUI gameOverStatsText;
        
        [Header("Color Settings")]
        public Color normalColor = Color.green;
        public Color warningColor = Color.yellow;
        public Color criticalColor = Color.red;
        
        [Header("Thresholds")]
        public float warningThreshold = 40f;
        public float criticalThreshold = 20f;
        
        void Start()
        {
            if (gameOverManager != null)
            {
                // Подписываемся на события
                gameOverManager.OnHullIntegrityChanged.AddListener(UpdateHullIntegrity);
                gameOverManager.OnPlayerHealthChanged.AddListener(UpdatePlayerHealth);
                gameOverManager.OnOxygenLevelChanged.AddListener(UpdateOxygen);
                gameOverManager.OnGameOver.AddListener(ShowGameOver);
                gameOverManager.OnCriticalWarning.AddListener(ShowCriticalWarning);
            }
            
            // Скрываем Game Over панель
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
                
            if (criticalWarningPanel != null)
                criticalWarningPanel.SetActive(false);
        }
        
        void Update()
        {
            UpdateWarnings();
        }
        
        #region Update Methods
        
        void UpdateHullIntegrity(float integrity)
        {
            if (hullIntegritySlider != null)
            {
                hullIntegritySlider.value = integrity;
            }
            
            if (hullIntegrityText != null)
            {
                hullIntegrityText.text = $"{integrity:F0}%";
            }
            
            if (hullIntegrityFill != null)
            {
                hullIntegrityFill.color = GetColorForValue(integrity);
            }
            
            if (hullWarningIcon != null)
            {
                hullWarningIcon.SetActive(integrity < warningThreshold);
            }
        }
        
        void UpdatePlayerHealth(float health)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = gameOverManager.maxPlayerHealth;
                healthSlider.value = health;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{health:F0}/{gameOverManager.maxPlayerHealth:F0}";
            }
            
            float healthPercent = (health / gameOverManager.maxPlayerHealth) * 100f;
            
            if (healthFill != null)
            {
                healthFill.color = GetColorForValue(healthPercent);
            }
            
            if (healthWarningIcon != null)
            {
                healthWarningIcon.SetActive(healthPercent < warningThreshold);
            }
        }
        
        void UpdateOxygen(float oxygen)
        {
            if (oxygenSlider != null)
            {
                oxygenSlider.value = oxygen;
            }
            
            if (oxygenText != null)
            {
                oxygenText.text = $"{oxygen:F0}%";
            }
            
            if (oxygenFill != null)
            {
                oxygenFill.color = GetColorForValue(oxygen);
            }
            
            if (oxygenWarningIcon != null)
            {
                oxygenWarningIcon.SetActive(oxygen < warningThreshold);
            }
        }
        
        void UpdateWarnings()
        {
            if (gameOverManager == null) return;
            
            // Control Malfunction Warning
            if (controlMalfunctionWarning != null)
            {
                if (gameOverManager.controlMalfunctionTimer > 0)
                {
                    float timeLeft = gameOverManager.maxControlMalfunctionTime - gameOverManager.controlMalfunctionTimer;
                    controlMalfunctionWarning.text = $"⚠️ ПОТЕРЯ УПРАВЛЕНИЯ: {timeLeft:F0}с";
                    controlMalfunctionWarning.gameObject.SetActive(true);
                    
                    // Мигание на последней минуте
                    if (timeLeft < 60f)
                    {
                        controlMalfunctionWarning.color = Mathf.PingPong(Time.time * 2f, 1f) > 0.5f ? criticalColor : warningColor;
                    }
                }
                else
                {
                    controlMalfunctionWarning.gameObject.SetActive(false);
                }
            }
            
            // Battery Emergency Warning
            if (batteryEmergencyWarning != null)
            {
                if (gameOverManager.batteryEmptyTimer > 0)
                {
                    float timeLeft = gameOverManager.maxBatteryEmptyTime - gameOverManager.batteryEmptyTimer;
                    batteryEmergencyWarning.text = $"⚠️ БАТАРЕЯ РАЗРЯЖЕНА: {gameOverManager.batteryEmptyTimer:F0}с\n💥 Новая пробоина каждые {gameOverManager.holeSpawnInterval:F0}с!";
                    batteryEmergencyWarning.gameObject.SetActive(true);
                    batteryEmergencyWarning.color = criticalColor;
                }
                else
                {
                    batteryEmergencyWarning.gameObject.SetActive(false);
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        Color GetColorForValue(float value)
        {
            if (value >= warningThreshold)
                return normalColor;
            else if (value >= criticalThreshold)
                return warningColor;
            else
                return criticalColor;
        }
        
        #endregion
        
        #region Event Handlers
        
        void ShowCriticalWarning()
        {
            if (criticalWarningPanel != null)
            {
                criticalWarningPanel.SetActive(true);
                StartCoroutine(HideCriticalWarningDelayed());
            }
        }
        
        System.Collections.IEnumerator HideCriticalWarningDelayed()
        {
            yield return new WaitForSeconds(3f);
            if (criticalWarningPanel != null)
                criticalWarningPanel.SetActive(false);
        }
        
        void ShowGameOver(string reason)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                
                if (gameOverReasonText != null)
                {
                    gameOverReasonText.text = reason;
                }
                
                if (gameOverStatsText != null && gameOverManager != null)
                {
                    gameOverStatsText.text = 
                        $"День: {gameOverManager.dayEventManager?.currentDay ?? 0}\n\n" +
                        $"Финальные показатели:\n" +
                        $"Герметичность: {gameOverManager.hullIntegrity:F0}%\n" +
                        $"Здоровье: {gameOverManager.playerHealth:F0}\n" +
                        $"Кислород: {gameOverManager.oxygenLevel:F0}%";
                }
            }
        }
        
        #endregion
    }
}