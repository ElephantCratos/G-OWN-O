using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class BatterySocket : MonoBehaviour {

        [Header("Socket Settings")]
        public Transform InsertPoint;
        public bool IsCharger = false;
        public float PowerRate = 10f;

        [Header("Snap Settings")]
        public float SnapSpeed = 10f;
        public float SnapDistance = 0.15f;

        private Battery currentBattery;

        void OnTriggerStay(Collider other) {
            Battery battery = other.GetComponent<Battery>();
            if (battery == null) return;

            // Если батарея близко — считаем, что она "в слоте"
            if (currentBattery == null && Vector3.Distance(other.transform.position, InsertPoint.position) < SnapDistance) {
                currentBattery = battery;
                battery.IsInSocket = true;
                Debug.Log($"{name}: Battery snapped in!");
            }

            // Если батарея уже вставлена и не удерживается рукой — фиксируем позицию
            if (battery == currentBattery && !battery.BeingHeld) {
                // Отключаем физику, чтобы не дрожала
                Rigidbody rb = battery.GetComponent<Rigidbody>();
                if (rb != null) {
                    rb.isKinematic = true;
                }

                // Плавно притягиваем к InsertPoint
                battery.transform.position = Vector3.Lerp(battery.transform.position, InsertPoint.position, Time.deltaTime * SnapSpeed);
                battery.transform.rotation = Quaternion.Lerp(battery.transform.rotation, InsertPoint.rotation, Time.deltaTime * SnapSpeed);

                // Заряд / разряд
                if (IsCharger)
                    battery.Charge(PowerRate);
                else
                    battery.Discharge(PowerRate);
            }

            // Если батарею держат — снова разрешаем физику
            if (battery.BeingHeld && battery == currentBattery) {
                Rigidbody rb = battery.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;

                battery.IsInSocket = false;
                currentBattery = null;
                Debug.Log($"{name}: Battery manually removed.");
            }
        }

        void OnTriggerExit(Collider other) {
            Battery battery = other.GetComponent<Battery>();
            if (battery != null && battery == currentBattery) {
                Rigidbody rb = battery.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;

                battery.IsInSocket = false;
                currentBattery = null;
                Debug.Log($"{name}: Battery exited socket.");
            }
        }
    }
}
