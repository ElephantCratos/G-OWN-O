using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    /// <summary>
    /// Управляет ивентом "поломки" панели управления кораблём
    /// </summary>
    public class ControlPanelMalfunction : MonoBehaviour
    {
        [Header("Ссылки")]
        public SpaceshipControlPanel controlPanel;
        public DayEventManager dayEventManager;
        
        [Header("Настройки поломки")]
        [Tooltip("Минимальное отклонение от текущих значений при поломке")]
        public float minDeviation = 30f;
        
        [Tooltip("Максимальное отклонение от текущих значений при поломке")]
        public float maxDeviation = 90f;
        
        [Tooltip("Минимальная целевая скорость при поломке")]
        public float minSpeedTarget = 20f;
        
        [Tooltip("Максимальная целевая скорость при поломке")]
        public float maxSpeedTarget = 90f;
        
        [Header("Аварийные эффекты (опционально)")]
        public GameObject alarmLights;
        public AudioSource alarmSound;
        
        [Header("События")]
        public UnityEvent OnMalfunctionStart;
        public UnityEvent OnMalfunctionFixed;
        
        // Сохранённые "нормальные" значения
        private float normalVertical;
        private float normalHorizontal;
        private float normalSpeed;
        
        private bool isMalfunctionActive = false;
        
        private void Start()
        {
            // Сохраняем начальные "нормальные" значения
            if (controlPanel != null)
            {
                normalVertical = controlPanel.targetVerticalAngle;
                normalHorizontal = controlPanel.targetHorizontalAngle;
                normalSpeed = controlPanel.targetSpeedPercent;
            }
        }
        
        private void Update()
        {
            // Проверяем, исправил ли игрок поломку
            if (isMalfunctionActive && controlPanel != null)
            {
                if (controlPanel.AreAllParametersCorrect())
                {
                    FixMalfunction();
                }
            }
        }
        
        /// <summary>
        /// Запускает ивент поломки — вызывается из DayEventManager
        /// </summary>
        public void StartMalfunction()
        {
            if (controlPanel == null || isMalfunctionActive) return;
            
            isMalfunctionActive = true;
            
            // Генерируем новые "аварийные" целевые значения
            float newVertical = GenerateDeviatedAngle(normalVertical);
            float newHorizontal = GenerateDeviatedAngle(normalHorizontal);
            float newSpeed = Random.Range(minSpeedTarget, maxSpeedTarget);
            
            // Убеждаемся, что скорость достаточно отличается от текущей
            if (Mathf.Abs(newSpeed - normalSpeed) < minDeviation)
            {
                newSpeed = normalSpeed + (Random.value > 0.5f ? minDeviation : -minDeviation);
                newSpeed = Mathf.Clamp(newSpeed, 0f, 100f);
            }
            
            // Устанавливаем новые целевые значения
            controlPanel.SetTargetVertical(newVertical);
            controlPanel.SetTargetHorizontal(newHorizontal);
            controlPanel.SetTargetSpeed(newSpeed);
            
            // Включаем эффекты тревоги
            if (alarmLights != null) alarmLights.SetActive(true);
            if (alarmSound != null) alarmSound.Play();
            
            OnMalfunctionStart?.Invoke();
            
            Debug.Log($"АВАРИЯ! Новые целевые параметры: V={newVertical:F0}°, H={newHorizontal:F0}°, S={newSpeed:F0}%");
        }
        
        /// <summary>
        /// Вызывается когда игрок исправил все параметры
        /// </summary>
        private void FixMalfunction()
        {
            isMalfunctionActive = false;
            
            // Выключаем эффекты тревоги
            if (alarmLights != null) alarmLights.SetActive(false);
            if (alarmSound != null) alarmSound.Stop();
            
            // Возвращаем нормальные целевые значения
            controlPanel.SetTargetVertical(normalVertical);
            controlPanel.SetTargetHorizontal(normalHorizontal);
            controlPanel.SetTargetSpeed(normalSpeed);
            
            OnMalfunctionFixed?.Invoke();
            
            // Сообщаем DayEventManager что ивент завершён
            if (dayEventManager != null)
            {
                dayEventManager.CompleteEvent("FixControls");
            }
            
            Debug.Log("Системы корабля стабилизированы!");
        }
        
        /// <summary>
        /// Генерирует угол, отклонённый от исходного
        /// </summary>
        private float GenerateDeviatedAngle(float originalAngle)
        {
            float deviation = Random.Range(minDeviation, maxDeviation);
            float direction = Random.value > 0.5f ? 1f : -1f;
            float newAngle = originalAngle + (deviation * direction);
            
            // Нормализуем угол в диапазон 0-360
            newAngle %= 360f;
            if (newAngle < 0) newAngle += 360f;
            
            return newAngle;
        }
        
        /// <summary>
        /// Принудительная остановка ивента (если нужно)
        /// </summary>
        public void ForceStopMalfunction()
        {
            if (!isMalfunctionActive) return;
            
            isMalfunctionActive = false;
            
            if (alarmLights != null) alarmLights.SetActive(false);
            if (alarmSound != null) alarmSound.Stop();
            
            controlPanel.SetTargetVertical(normalVertical);
            controlPanel.SetTargetHorizontal(normalHorizontal);
            controlPanel.SetTargetSpeed(normalSpeed);
        }
        
        public bool IsMalfunctionActive() => isMalfunctionActive;
    }
}