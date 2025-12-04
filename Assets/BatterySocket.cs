using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    public class BatterySocket : MonoBehaviour {

        [Header("Socket Settings")]
        public Transform InsertPoint;
        public bool IsCharger = false;
        public float PowerRate = 10f;

        [Header("Snap Settings")]
        public float SnapSpeed = 10f;
        public float SnapDistance = 0.15f;
        
        [Tooltip("Расстояние для выхода из слота (должно быть больше SnapDistance)")]
        public float ExitDistance = 0.25f;

        [Header("События")]
        public UnityEvent<Battery> OnBatteryInserted;
        public UnityEvent<Battery> OnBatteryRemoved;

        private Battery currentBattery;
        
        // === Защита от дребезга ===
        private bool batteryFullySnapped = false;
        
        public Battery CurrentBattery => currentBattery;
        public bool HasBattery => currentBattery != null;

        void OnTriggerStay(Collider other) {
            Battery battery = other.GetComponent<Battery>();
            if (battery == null) return;

            float distance = Vector3.Distance(other.transform.position, InsertPoint.position);

            // Батарея входит в слот
            if (currentBattery == null && distance < SnapDistance) {
                currentBattery = battery;
                battery.IsInSocket = true;
                batteryFullySnapped = false; // Ещё не зафиксирована
                
                Debug.Log($"{name}: Battery entering socket...");
            }

            // Батарея в слоте и не удерживается — фиксируем
            if (battery == currentBattery && !battery.BeingHeld) {
                Rigidbody rb = battery.GetComponent<Rigidbody>();
                if (rb != null) {
                    rb.isKinematic = true;
                }

                // Притягиваем к точке
                battery.transform.position = Vector3.Lerp(battery.transform.position, InsertPoint.position, Time.deltaTime * SnapSpeed);
                battery.transform.rotation = Quaternion.Lerp(battery.transform.rotation, InsertPoint.rotation, Time.deltaTime * SnapSpeed);

                // Проверяем — батарейка полностью зафиксировалась?
                float snapDist = Vector3.Distance(battery.transform.position, InsertPoint.position);
                if (!batteryFullySnapped && snapDist < 0.01f) {
                    batteryFullySnapped = true;
                    OnBatteryInserted?.Invoke(battery);
                    Debug.Log($"{name}: Battery fully snapped!");
                }

                // Заряд / разряд
                if (IsCharger)
                    battery.Charge(PowerRate);
                else
                    battery.Discharge(PowerRate);
            }

            // Батарею взяли рукой — извлекаем
            if (battery.BeingHeld && battery == currentBattery) {
                RemoveBattery(battery);
            }
        }

        void OnTriggerExit(Collider other) {
            Battery battery = other.GetComponent<Battery>();
            if (battery == null || battery != currentBattery) return;
            
            // Проверяем реальное расстояние — защита от дребезга
            float distance = Vector3.Distance(other.transform.position, InsertPoint.position);
            
            // Выходим только если действительно далеко
            if (distance > ExitDistance) {
                RemoveBattery(battery);
            }
        }
        
        private void RemoveBattery(Battery battery) {
            if (battery == null || battery != currentBattery) return;
            
            Rigidbody rb = battery.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            battery.IsInSocket = false;
            
            Battery removedBattery = currentBattery;
            currentBattery = null;
            
            // Вызываем событие только если батарейка была полностью вставлена
            if (batteryFullySnapped) {
                OnBatteryRemoved?.Invoke(removedBattery);
                Debug.Log($"{name}: Battery removed.");
            }
            
            batteryFullySnapped = false;
        }
    }
}