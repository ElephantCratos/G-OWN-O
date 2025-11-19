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

        // Словарь для отслеживания занятости каждого сокета
        private Dictionary<Transform, GameObject> occupiedSockets = new Dictionary<Transform, GameObject>();

        private Vector3 originalScale;

        void OnTriggerStay(Collider other) {
            GameObject obj = other.gameObject;

            // Проверяем, можно ли крепить этот объект
            if (!IsAllowedObject(obj)) return;

            // Проверяем, не вставлен ли уже этот объект в какой-то сокет
            bool isAlreadyAttached = occupiedSockets.ContainsValue(obj);

            if (!isAlreadyAttached) {
                // Ищем ближайшую СВОБОДНУЮ точку
                Transform nearestPoint = GetNearestFreeInsertPoint(obj.transform.position);

                if (nearestPoint != null && Vector3.Distance(obj.transform.position, nearestPoint.position) < SnapDistance) {
                    // Занимаем сокет
                    occupiedSockets[nearestPoint] = obj;
                    originalScale = obj.transform.localScale;

                    // Отключаем физику, чтобы не дрожало
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                        rb.isKinematic = true;

                    Debug.Log($"{name}: {obj.name} вставлен в {nearestPoint.name}");
                }
            }

            // Если объект уже вставлен — плавно притягиваем к его сокету
            Transform attachedSocket = GetSocketForObject(obj);
            if (attachedSocket != null) {
                obj.transform.position = Vector3.Lerp(
                    obj.transform.position,
                    attachedSocket.position,
                    Time.deltaTime * SnapSpeed
                );

                obj.transform.rotation = Quaternion.Lerp(
                    obj.transform.rotation,
                    attachedSocket.rotation,
                    Time.deltaTime * SnapSpeed
                );

                // Сохраняем оригинальный масштаб
                obj.transform.localScale = originalScale;
            }
        }

        void OnTriggerExit(Collider other) {
            GameObject obj = other.gameObject;

            // Находим сокет, в котором был этот объект
            Transform socketToFree = GetSocketForObject(obj);

            if (socketToFree != null) {
                // Освобождаем сокет
                occupiedSockets.Remove(socketToFree);

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = false;

                Debug.Log($"{name}: {obj.name} удалён из сокета {socketToFree.name}");
            }
        }

        /// <summary>
        /// Возвращает сокет, в который вставлен данный объект (если есть).
        /// </summary>
        Transform GetSocketForObject(GameObject obj) {
            foreach (var kvp in occupiedSockets) {
                if (kvp.Value == obj)
                    return kvp.Key;
            }
            return null;
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
        /// Возвращает ближайшую СВОБОДНУЮ точку крепления к указанной позиции.
        /// </summary>
        Transform GetNearestFreeInsertPoint(Vector3 position) {
            if (InsertPoints == null || InsertPoints.Count == 0)
                return null;

            Transform nearest = null;
            float minDist = Mathf.Infinity;

            foreach (Transform point in InsertPoints) {
                if (point == null) continue;

                // ВАЖНО: Пропускаем занятые сокеты
                if (occupiedSockets.ContainsKey(point))
                    continue;

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