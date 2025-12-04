using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

        [Header("Блокировка пинов")]
        [Tooltip("Если true, пины нельзя извлечь из сокетов")]
        public bool ArePinsLocked = true;

        [Header("События")]
        public UnityEvent<float> OnAverageWearChanged = new UnityEvent<float>();

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

                    // Блокируем Grabbable если пины заблокированы
                    UpdatePinLockState(obj);

                    Debug.Log($"{name}: {obj.name} вставлен в {nearestPoint.name}");
                    
                    // Обновляем средний износ
                    UpdateAverageWear();
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
                // Проверяем, разрешено ли извлечение
                if (ArePinsLocked)
                {
                    // Пины заблокированы - не даём извлечь
                    Debug.Log($"{name}: Попытка извлечь {obj.name}, но пины заблокированы!");
                    
                    // Возвращаем пин обратно в сокет
                    StartCoroutine(ReturnPinToSocket(obj, socketToFree));
                    return;
                }

                // Освобождаем сокет
                occupiedSockets.Remove(socketToFree);

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = false;

                // Разблокируем Grabbable при извлечении
                Grabbable grabbable = obj.GetComponent<Grabbable>();
                if (grabbable != null)
                {
                    grabbable.enabled = true;
                }

                Debug.Log($"{name}: {obj.name} удалён из сокета {socketToFree.name}");
                
                // Обновляем средний износ
                UpdateAverageWear();
            }
        }

        /// <summary>
        /// Корутина для возврата пина в сокет при попытке извлечения
        /// </summary>
        IEnumerator ReturnPinToSocket(GameObject pin, Transform socket)
        {
            // Ждём один кадр
            yield return null;

            // Сбрасываем захват
            Grabbable grabbable = pin.GetComponent<Grabbable>();
            if (grabbable != null && grabbable.BeingHeld)
            {
                grabbable.DropItem(false, false);
            }

            // Возвращаем в позицию сокета
            pin.transform.position = socket.position;
            pin.transform.rotation = socket.rotation;

            // Обеспечиваем кинематику
            Rigidbody rb = pin.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Устанавливает блокировку извлечения пинов
        /// </summary>
        public void SetPinsLocked(bool locked)
        {
            ArePinsLocked = locked;

            // Обновляем состояние всех вставленных пинов
            foreach (var kvp in occupiedSockets)
            {
                if (kvp.Value != null)
                {
                    UpdatePinLockState(kvp.Value);
                }
            }

            Debug.Log($"{name}: Пины {(locked ? "ЗАБЛОКИРОВАНЫ" : "РАЗБЛОКИРОВАНЫ")}");
        }

        /// <summary>
        /// Обновляет состояние блокировки конкретного пина
        /// </summary>
        void UpdatePinLockState(GameObject pin)
        {
            Grabbable grabbable = pin.GetComponent<Grabbable>();
            if (grabbable != null)
            {
                // Если пины заблокированы - отключаем возможность хватать
                if (ArePinsLocked)
                {
                    // Если пин захвачен - отпускаем его
                    if (grabbable.BeingHeld)
                    {
                        grabbable.DropItem(false, false);
                    }
                    
                    // Отключаем компонент Grabbable
                    grabbable.enabled = false;
                }
                else
                {
                    // Включаем возможность хватать
                    grabbable.enabled = true;
                }
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

        /// <summary>
        /// Вычисляет средний уровень износа всех вставленных пинов
        /// </summary>
        public float GetAverageWear()
        {
            if (occupiedSockets.Count == 0)
                return 0f;

            float totalWear = 0f;
            int pinsWithWear = 0;

            foreach (var kvp in occupiedSockets)
            {
                GameObject pin = kvp.Value;
                if (pin != null)
                {
                    PinWear pinWear = pin.GetComponent<PinWear>();
                    if (pinWear != null)
                    {
                        totalWear += pinWear.WearLevel;
                        pinsWithWear++;
                    }
                }
            }

            return pinsWithWear > 0 ? totalWear / pinsWithWear : 0f;
        }

        /// <summary>
        /// Обновляет средний износ и вызывает событие
        /// </summary>
        void UpdateAverageWear()
        {
            float avgWear = GetAverageWear();
            OnAverageWearChanged?.Invoke(avgWear);
        }

        /// <summary>
        /// Возвращает список всех вставленных пинов
        /// </summary>
        public List<GameObject> GetInsertedPins()
        {
            List<GameObject> pins = new List<GameObject>();
            foreach (var kvp in occupiedSockets)
            {
                if (kvp.Value != null)
                    pins.Add(kvp.Value);
            }
            return pins;
        }

        /// <summary>
        /// Возвращает количество вставленных пинов
        /// </summary>
        public int GetInsertedPinsCount()
        {
            return occupiedSockets.Count;
        }
    }
}