using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    public class BatteryReplacementEvent : MonoBehaviour
    {
        [Header("Ссылки")]
        public BatterySocket mainPowerSocket;
        public DayEventManager dayEventManager;
        
        [Header("Настройки")]
        public float criticalChargeLevel = 10f;
        public float minChargeToComplete = 50f;
        
        [Header("Эффекты")]
        public GameObject warningLights;
        public AudioSource warningSound;
        
        [Header("События")]
        public UnityEvent OnBatteryLow;
        public UnityEvent OnBatteryReplaced;
        public UnityEvent OnBatteryRelapsed;
        
        private bool isEventActive = false;
        private bool isCompleted = false;
        private bool hadBatteryBefore = false;
        
        private const string EVENT_NAME = "ReplaceBattery";
        
        private void Start()
        {
            if (mainPowerSocket != null)
            {
                mainPowerSocket.OnBatteryInserted.AddListener(OnBatteryInserted);
                mainPowerSocket.OnBatteryRemoved.AddListener(OnBatteryRemoved);
                
                if (mainPowerSocket.CurrentBattery != null)
                {
                    hadBatteryBefore = true;
                }
            }
        }
        
        private void OnDestroy()
        {
            if (mainPowerSocket != null)
            {
                mainPowerSocket.OnBatteryInserted.RemoveListener(OnBatteryInserted);
                mainPowerSocket.OnBatteryRemoved.RemoveListener(OnBatteryRemoved);
            }
        }
        
        private void Update()
        {
            // НОВОЕ: В первый день батарея не разряжается и события не активируются
            if (dayEventManager != null && dayEventManager.IsFirstDay())
            {
                // Просто пропускаем всю логику
                return;
            }
            
            if (mainPowerSocket == null) return;
            
            Battery currentBattery = mainPowerSocket.CurrentBattery;
            
            // === Проверка активации ивента ===
            
            if (!isEventActive && currentBattery != null && currentBattery.CurrentCharge <= criticalChargeLevel)
            {
                StartBatteryEvent("Батарея разряжена!");
            }
            
            // === Проверка завершения/отмены ===
            
            if (isEventActive)
            {
                bool conditionMet = currentBattery != null && currentBattery.CurrentCharge >= minChargeToComplete;
                
                if (!isCompleted && conditionMet)
                {
                    CompleteBatteryEvent();
                }
                else if (isCompleted && !conditionMet)
                {
                    UncompleteBatteryEvent();
                }
            }
        }
        
        private void OnBatteryInserted(Battery battery)
        {
            hadBatteryBefore = true;
            
            Debug.Log($"Батарея вставлена. Заряд: {battery.CurrentCharge:F0}%");
            
            // НОВОЕ: В первый день не активируем события
            if (dayEventManager != null && dayEventManager.IsFirstDay())
            {
                return;
            }
            
            if (isEventActive && battery.CurrentCharge >= minChargeToComplete)
            {
                CompleteBatteryEvent();
            }
        }
        
        private void OnBatteryRemoved(Battery battery)
        {
            Debug.Log("Батарея извлечена!");
            
            // НОВОЕ: В первый день не активируем события
            if (dayEventManager != null && dayEventManager.IsFirstDay())
            {
                return;
            }
            
            if (isEventActive && isCompleted)
            {
                UncompleteBatteryEvent();
            }
            else if (!isEventActive && hadBatteryBefore)
            {
                StartBatteryEvent("Батарея извлечена!");
            }
        }
        
        private void StartBatteryEvent(string reason)
        {
            // НОВОЕ: Дополнительная проверка на первый день
            if (dayEventManager != null && dayEventManager.IsFirstDay())
            {
                Debug.Log("⏸️ Батарея защищена в обучающий день");
                return;
            }
            
            isEventActive = true;
            isCompleted = false;
            
            if (dayEventManager != null)
            {
                dayEventManager.AddEvent(EVENT_NAME);
            }
            
            if (warningLights != null) warningLights.SetActive(true);
            if (warningSound != null) warningSound.Play();
            
            OnBatteryLow?.Invoke();
            
            Debug.Log($"⚠️ ВНИМАНИЕ! {reason} Требуется замена или зарядка!");
        }
        
        private void CompleteBatteryEvent()
        {
            isCompleted = true;
            
            if (warningLights != null) warningLights.SetActive(false);
            if (warningSound != null) warningSound.Stop();
            
            if (dayEventManager != null)
            {
                dayEventManager.CompleteEvent(EVENT_NAME);
            }
            
            OnBatteryReplaced?.Invoke();
            
            Debug.Log("✓ Питание восстановлено!");
        }
        
        private void UncompleteBatteryEvent()
        {
            isCompleted = false;
            
            if (warningLights != null) warningLights.SetActive(true);
            if (warningSound != null) warningSound.Play();
            
            if (dayEventManager != null)
            {
                dayEventManager.UncompleteEvent(EVENT_NAME);
            }
            
            OnBatteryRelapsed?.Invoke();
            
            Debug.Log("⚠️ Питание снова нестабильно!");
        }
        
        public void ResetEvent()
        {
            isEventActive = false;
            isCompleted = false;
            
            if (warningLights != null) warningLights.SetActive(false);
            if (warningSound != null) warningSound.Stop();
        }
        
        public void ForceComplete()
        {
            if (!isEventActive) return;
            
            isEventActive = false;
            isCompleted = false;
            
            if (warningLights != null) warningLights.SetActive(false);
            if (warningSound != null) warningSound.Stop();
            
            if (dayEventManager != null)
            {
                dayEventManager.CompleteEvent(EVENT_NAME);
            }
        }
        
        public bool IsEventActive => isEventActive;
        public bool IsCompleted => isCompleted;
        
        public string GetStatus()
        {
            // НОВОЕ: Специальное сообщение для первого дня
            if (dayEventManager != null && dayEventManager.IsFirstDay())
            {
                return "Питание: 🛡️ ЗАЩИЩЕНО (Обучение)";
            }
            
            if (!isEventActive) return "Питание: ОК";
            
            Battery battery = mainPowerSocket?.CurrentBattery;
            
            if (battery == null) return "⚠️ НЕТ БАТАРЕИ!";
            
            return $"⚠️ Заряд: {battery.CurrentCharge:F0}% (нужно {minChargeToComplete}%)";
        }
    }
}