using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG
{
    public class AttachModel : MonoBehaviour
    {
        [Header("Что можно вставлять в сокет")]
        [Tooltip("Укажи объекты, которые могут быть вставлены (по ссылке на префаб или объект в сцене)")]
        public List<GameObject> AllowedObjects = new List<GameObject>();

        [Header("Точки крепления (можно задать несколько)")]
        public List<Transform> InsertPoints = new List<Transform>();

        [Header("Настройки привязки")]
        [Tooltip("Скорость, с которой объект притягивается к точке крепления")]
        public float SnapSpeed = 10f;
        [Tooltip("Расстояние, при котором объект прикрепляется")]
        public float SnapDistance = 0.15f;

        private GameObject currentObject;
        private Transform activeInsertPoint;
        private Vector3 originalScale;

        void OnTriggerStay(Collider other) {
            GameObject obj = other.gameObject;

            // Проверяем, можно ли крепить этот объект
            if (!IsAllowedObject(obj)) return;

            // Если ничего не вставлено, ищем ближайшую точку
            if (currentObject == null) {
                Transform nearestPoint = GetNearestInsertPoint(obj.transform.position);

                if (nearestPoint != null && Vector3.Distance(obj.transform.position, nearestPoint.position) < SnapDistance) {
                    currentObject = obj;
                    activeInsertPoint = nearestPoint;
                    originalScale = obj.transform.localScale;

                    // Отключаем физику, чтобы не дрожало
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                        rb.isKinematic = true;

                    Debug.Log($"{name}: {obj.name} вставлен в {activeInsertPoint.name}");
                }
            }

            // Если объект уже вставлен — плавно притягиваем
            if (obj == currentObject) {
                obj.transform.position = Vector3.Lerp(
                    obj.transform.position,
                    activeInsertPoint.position,
                    Time.deltaTime * SnapSpeed
                );

                obj.transform.rotation = Quaternion.Lerp(
                    obj.transform.rotation,
                    activeInsertPoint.rotation,
                    Time.deltaTime * SnapSpeed
                );

                // Сохраняем оригинальный масштаб
                obj.transform.localScale = originalScale;
            }
        }

        void OnTriggerExit(Collider other) {
            GameObject obj = other.gameObject;

            if (obj == currentObject) {
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = false;

                currentObject = null;
                activeInsertPoint = null;

                Debug.Log($"{name}: {obj.name} удалён из сокета");
            }
        }

        /// <summary>
        /// Проверяет, входит ли объект в список разрешённых.
        /// </summary>
        bool IsAllowedObject(GameObject obj) {
            if (AllowedObjects == null || AllowedObjects.Count == 0)
                return true; // Если список пуст — разрешаем всё

            foreach (var allowed in AllowedObjects) {
                if (allowed == null) continue;
                // Проверяем по префабу или по имени
                if (obj.name.StartsWith(allowed.name))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Возвращает ближайшую точку крепления к указанной позиции.
        /// </summary>
        Transform GetNearestInsertPoint(Vector3 position) {
            if (InsertPoints == null || InsertPoints.Count == 0)
                return null;

            Transform nearest = null;
            float minDist = Mathf.Infinity;

            foreach (Transform point in InsertPoints) {
                if (point == null) continue;

                float dist = Vector3.Distance(position, point.position);
                if (dist < minDist) {
                    minDist = dist;
                    nearest = point;
                }
            }

            return nearest;
        }
    }
}