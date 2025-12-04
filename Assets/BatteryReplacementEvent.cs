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
        
        // === НОВОЕ: отслеживаем, была ли батарейка вставлена хоть раз ===
        private bool hadBatteryBefore = false;
        
        private const string EVENT_NAME = "ReplaceBattery";
        
        private void Start()
        {
            if (mainPowerSocket != null)
            {
                mainPowerSocket.OnBatteryInserted.AddListener(OnBatteryInserted);
                mainPowerSocket.OnBatteryRemoved.AddListener(OnBatteryRemoved);
                
                // Если при старте батарейка уже в слоте — запоминаем
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
            if (mainPowerSocket == null) return;
            
            Battery currentBattery = mainPowerSocket.CurrentBattery;
            
            // === Проверка активации ивента ===
            
            // Батарейка есть и села — активируем ивент
            if (!isEventActive && currentBattery != null && currentBattery.CurrentCharge <= criticalChargeLevel)
            {
                StartBatteryEvent("Батарея разряжена!");
            }
            
            // Батарейки нет, НО она была раньше — значит извлекли/потеряли
            // (этот кейс теперь обрабатывается в OnBatteryRemoved)
            
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
            // Теперь мы знаем что батарейка была
            hadBatteryBefore = true;
            
            Debug.Log($"Батарея вставлена. Заряд: {battery.CurrentCharge:F0}%");
            
            if (isEventActive && battery.CurrentCharge >= minChargeToComplete)
            {
                CompleteBatteryEvent();
            }
        }
        
        private void OnBatteryRemoved(Battery battery)
        {
            Debug.Log("Батарея извлечена!");
            
            // Если ивент был завершён — отменяем
            if (isEventActive && isCompleted)
            {
                UncompleteBatteryEvent();
            }
            // Если ивента не было, но батарейка была — запускаем
            else if (!isEventActive && hadBatteryBefore)
            {
                StartBatteryEvent("Батарея извлечена!");
            }
        }
        
        private void StartBatteryEvent(string reason)
        {
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
            // НЕ сбрасываем hadBatteryBefore — память о батарейке сохраняется
            
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
            if (!isEventActive) return "Питание: ОК";
            
            Battery battery = mainPowerSocket?.CurrentBattery;
            
            if (battery == null) return "⚠️ НЕТ БАТАРЕИ!";
            
            return $"⚠️ Заряд: {battery.CurrentCharge:F0}% (нужно {minChargeToComplete}%)";
        }
    }
}