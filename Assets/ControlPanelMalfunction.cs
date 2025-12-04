using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    public class ControlPanelMalfunction : MonoBehaviour
    {
        [Header("Ссылки")]
        public SpaceshipControlPanel controlPanel;
        public DayEventManager dayEventManager;
        
        [Header("Настройки поломки")]
        public float minDeviation = 30f;
        public float maxDeviation = 90f;
        public float minSpeedTarget = 20f;
        public float maxSpeedTarget = 90f;
        
        [Header("Аварийные эффекты")]
        public GameObject alarmLights;
        public AudioSource alarmSound;
        
        [Header("События")]
        public UnityEvent OnMalfunctionStart;
        public UnityEvent OnMalfunctionFixed;
        public UnityEvent OnMalfunctionRelapsed;
        
        private bool isMalfunctionActive = false;
        private bool isCurrentlyFixed = false;
        
        private void Update()
        {
            if (!isMalfunctionActive || controlPanel == null) return;
            
            bool allCorrect = controlPanel.AreAllParametersCorrect();
            
            if (allCorrect && !isCurrentlyFixed)
            {
                OnParametersFixed();
            }
            else if (!allCorrect && isCurrentlyFixed)
            {
                OnParametersRelapsed();
            }
        }
        
        private void OnParametersFixed()
        {
            isCurrentlyFixed = true;
            
            if (alarmLights != null) alarmLights.SetActive(false);
            if (alarmSound != null) alarmSound.Stop();
            
            if (dayEventManager != null)
            {
                dayEventManager.CompleteEvent("FixControls");
            }
            
            OnMalfunctionFixed?.Invoke();
            
            Debug.Log("Системы стабилизированы! Держите параметры до конца дня.");
        }
        
        private void OnParametersRelapsed()
        {
            isCurrentlyFixed = false;
            
            if (alarmLights != null) alarmLights.SetActive(true);
            if (alarmSound != null) alarmSound.Play();
            
            if (dayEventManager != null)
            {
                dayEventManager.UncompleteEvent("FixControls");
            }
            
            OnMalfunctionRelapsed?.Invoke();
            
            Debug.Log("ВНИМАНИЕ! Параметры снова вышли из нормы!");
        }
        
        public void StartMalfunction()
        {
            if (controlPanel == null || isMalfunctionActive) return;
            
            isMalfunctionActive = true;
            isCurrentlyFixed = false;
            
            // Разблокируем контролы
            controlPanel.UnlockControls();
            
            // Генерируем аварийные целевые значения
            float newVertical = GenerateDeviatedAngle(controlPanel.targetVerticalAngle);
            float newHorizontal = GenerateDeviatedAngle(controlPanel.targetHorizontalAngle);
            float newSpeed = GenerateDeviatedSpeed(controlPanel.targetSpeedPercent);
            
            controlPanel.SetTargetVertical(newVertical);
            controlPanel.SetTargetHorizontal(newHorizontal);
            controlPanel.SetTargetSpeed(newSpeed);
            
            if (alarmLights != null) alarmLights.SetActive(true);
            if (alarmSound != null) alarmSound.Play();
            
            OnMalfunctionStart?.Invoke();
            
            Debug.Log($"АВАРИЯ! Новые целевые параметры: V={newVertical:F0}°, H={newHorizontal:F0}°, S={newSpeed:F0}%");
        }
        
        public void EndMalfunctionEvent()
        {
            if (!isMalfunctionActive) return;
            
            isMalfunctionActive = false;
            isCurrentlyFixed = false;
            
            // Выставляем контролы в целевые позиции и блокируем
            controlPanel.SetControlsToTargetPositions();
            controlPanel.LockControls();
            
            if (alarmLights != null) alarmLights.SetActive(false);
            if (alarmSound != null) alarmSound.Stop();

            Debug.Log("Ивент поломки завершён");
        }
        
        public void ForceStopMalfunction()
        {
            if (!isMalfunctionActive) return;
            
            isMalfunctionActive = false;
            isCurrentlyFixed = false;
            
            controlPanel.SetControlsToTargetPositions();
            controlPanel.LockControls();
            
            if (alarmLights != null) alarmLights.SetActive(false);
            if (alarmSound != null) alarmSound.Stop();
        }
        
        private float GenerateDeviatedAngle(float originalAngle)
        {
            float deviation = Random.Range(minDeviation, maxDeviation);
            float direction = Random.value > 0.5f ? 1f : -1f;
            float newAngle = originalAngle + (deviation * direction);
            
            newAngle %= 360f;
            if (newAngle < 0) newAngle += 360f;
            
            return newAngle;
        }
        
        private float GenerateDeviatedSpeed(float originalSpeed)
        {
            float deviation = Random.Range(minDeviation, maxDeviation);
            float direction = Random.value > 0.5f ? 1f : -1f;
            float newSpeed = originalSpeed + (deviation * direction);
            
            return Mathf.Clamp(newSpeed, minSpeedTarget, maxSpeedTarget);
        }
        
        public bool IsMalfunctionActive() => isMalfunctionActive;
        public bool IsCurrentlyFixed() => isCurrentlyFixed;
    }
}
