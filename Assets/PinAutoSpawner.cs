using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG
{
    /// <summary>
    /// Автоматически создаёт пины в пустых сокетах при старте сцены
    /// </summary>
    public class PinAutoSpawner : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("AttachModel с сокетами для пинов")]
        public AttachModel attachModel;

        [Header("Настройки спавна")]
        [Tooltip("Префаб пина для создания")]
        public GameObject pinPrefab;

        [Tooltip("Создавать пины при старте сцены")]
        public bool spawnOnStart = true;

        [Tooltip("Заполнять все пустые сокеты")]
        public bool fillAllSockets = true;

        [Tooltip("Если false, укажи сколько пинов создать")]
        public int pinsToSpawn = 4;

        [Header("Настройки износа при спавне")]
        [Tooltip("Устанавливать случайный износ при создании")]
        public bool setRandomWear = false;

        [Tooltip("Диапазон износа для новых пинов")]
        public float minWear = 0f;
        public float maxWear = 30f;

        [Header("Опции")]
        [Tooltip("Родительский объект для созданных пинов (для порядка в иерархии)")]
        public Transform pinsContainer;

        void Start()
        {
            if (spawnOnStart)
            {
                // Небольшая задержка чтобы все системы успели инициализироваться
                StartCoroutine(SpawnPinsDelayed());
            }
        }

        IEnumerator SpawnPinsDelayed()
        {
            // Ждём один кадр
            yield return null;

            SpawnPins();
        }

        /// <summary>
        /// Создаёт пины в пустых сокетах
        /// </summary>
        public void SpawnPins()
        {
            if (attachModel == null)
            {
                Debug.LogError("PinAutoSpawner: AttachModel не назначен!");
                return;
            }

            if (pinPrefab == null)
            {
                Debug.LogError("PinAutoSpawner: Префаб пина не назначен!");
                return;
            }

            if (attachModel.InsertPoints == null || attachModel.InsertPoints.Count == 0)
            {
                Debug.LogError("PinAutoSpawner: Нет точек крепления в AttachModel!");
                return;
            }

            // Получаем список пустых сокетов
            List<Transform> emptySockets = GetEmptySockets();

            if (emptySockets.Count == 0)
            {
                Debug.Log("PinAutoSpawner: Все сокеты уже заполнены");
                return;
            }

            // Определяем сколько пинов создать
            int count = fillAllSockets ? emptySockets.Count : Mathf.Min(pinsToSpawn, emptySockets.Count);

            Debug.Log($"PinAutoSpawner: Создаём {count} пинов в пустых сокетах");

            // Создаём пины
            for (int i = 0; i < count; i++)
            {
                Transform socket = emptySockets[i];
                CreatePinAtSocket(socket);
            }
        }

        /// <summary>
        /// Возвращает список пустых сокетов
        /// </summary>
        List<Transform> GetEmptySockets()
        {
            List<Transform> emptySockets = new List<Transform>();
            List<GameObject> insertedPins = attachModel.GetInsertedPins();

            foreach (Transform socket in attachModel.InsertPoints)
            {
                if (socket == null) continue;

                // Проверяем, есть ли пин в этом сокете
                bool isOccupied = false;

                foreach (GameObject pin in insertedPins)
                {
                    if (pin != null && Vector3.Distance(pin.transform.position, socket.position) < 0.05f)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied)
                {
                    emptySockets.Add(socket);
                }
            }

            return emptySockets;
        }

        /// <summary>
        /// Создаёт пин в указанном сокете
        /// </summary>
        void CreatePinAtSocket(Transform socket)
        {
            // Создаём пин
            GameObject newPin = Instantiate(pinPrefab, socket.position, socket.rotation);

            // Устанавливаем родителя для порядка
            if (pinsContainer != null)
            {
                newPin.transform.SetParent(pinsContainer);
            }

            // Устанавливаем износ если нужно
            if (setRandomWear)
            {
                PinWear pinWear = newPin.GetComponent<PinWear>();
                if (pinWear != null)
                {
                    float wear = Random.Range(minWear, maxWear);
                    pinWear.SetWearLevel(wear);
                    Debug.Log($"PinAutoSpawner: Пин создан с износом {wear:F1}%");
                }
            }

            // Настраиваем физику
            Rigidbody rb = newPin.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            Debug.Log($"PinAutoSpawner: Создан пин {newPin.name} в сокете {socket.name}");
        }

        /// <summary>
        /// Удаляет все пины из сокетов (для очистки)
        /// </summary>
        public void ClearAllPins()
        {
            List<GameObject> pins = attachModel.GetInsertedPins();

            foreach (GameObject pin in pins)
            {
                if (pin != null)
                {
                    Destroy(pin);
                }
            }

            Debug.Log($"PinAutoSpawner: Удалено {pins.Count} пинов");
        }

        /// <summary>
        /// Пересоздаёт все пины (удаляет старые и создаёт новые)
        /// </summary>
        public void RespawnAllPins()
        {
            ClearAllPins();
            
            // Ждём кадр перед созданием новых
            StartCoroutine(SpawnPinsDelayed());
        }

        /// <summary>
        /// Заполняет только пустые сокеты (не трогает существующие пины)
        /// </summary>
        public void FillEmptySockets()
        {
            SpawnPins();
        }

#if UNITY_EDITOR
        // Кнопки для тестирования в инспекторе
        [ContextMenu("Создать пины")]
        void EditorSpawnPins()
        {
            SpawnPins();
        }

        [ContextMenu("Удалить все пины")]
        void EditorClearPins()
        {
            ClearAllPins();
        }

        [ContextMenu("Пересоздать все пины")]
        void EditorRespawnPins()
        {
            ClearAllPins();
            SpawnPins();
        }
#endif
    }
}