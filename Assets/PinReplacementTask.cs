using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG
{
    /// <summary>
    /// Управляет заданием на замену изношенных пинов
    /// </summary>
    public class PinReplacementTask : MonoBehaviour
    {
        [Header("Ссылки")]
        public AttachModel attachModel;
        public DayEventManager dayEventManager;
        public PinAutoSpawner pinAutoSpawner; // Опционально: для автозаполнения пустых сокетов

        [Header("Настройки задания")]
        [Tooltip("Порог износа для замены (по умолчанию 50%)")]
        public float wearThreshold = 50f;

        [Header("Настройки генерации износа")]
        [Tooltip("Минимальное количество пинов с износом >50%")]
        public int minWornPins = 1;

        [Tooltip("Диапазон износа для изношенных пинов")]
        public float minWornValue = 51f;
        public float maxWornValue = 95f;

        [Tooltip("Диапазон износа для нормальных пинов")]
        public float minNormalValue = 0f;
        public float maxNormalValue = 49f;

        [Header("События")]
        public UnityEvent OnTaskCompleted;
        public UnityEvent OnTaskUncompleted;

        private bool isTaskActive = false;
        private bool wasCompleted = false;

        void Start()
        {
            if (attachModel == null)
            {
                Debug.LogError("AttachModel не назначен в PinReplacementTask!");
                return;
            }

            // Подписываемся на изменения в AttachModel
            attachModel.OnAverageWearChanged.AddListener(OnPinsChanged);
        }

        void OnDestroy()
        {
            if (attachModel != null)
            {
                attachModel.OnAverageWearChanged.RemoveListener(OnPinsChanged);
            }
        }

        /// <summary>
        /// Активирует задание на замену пинов
        /// </summary>
        public void StartReplacementTask()
        {
            isTaskActive = true;
            wasCompleted = false;

            Debug.Log("PinReplacementTask: Задание активировано");

            // Если есть автоспавнер - заполняем пустые сокеты
            if (pinAutoSpawner != null)
            {
                pinAutoSpawner.FillEmptySockets();
            }

            // Ждём кадр чтобы новые пины успели инициализироваться
            StartCoroutine(InitializeTaskDelayed());
        }

        /// <summary>
        /// Инициализация задания с задержкой
        /// </summary>
        IEnumerator InitializeTaskDelayed()
        {
            yield return null;

            // Устанавливаем случайный износ существующим пинам
            SetRandomWearToInsertedPins();

            // Разрешаем извлечение пинов
            attachModel.SetPinsLocked(false);

            // Проверяем начальное состояние
            CheckTaskCompletion();
        }

        /// <summary>
        /// Завершает задание на замену пинов
        /// </summary>
        public void EndReplacementTask()
        {
            isTaskActive = false;
            wasCompleted = false;

            Debug.Log("PinReplacementTask: Задание завершено");

            // Блокируем извлечение пинов
            attachModel.SetPinsLocked(true);

            // Ограничиваем износ до 50% для всех пинов
            LimitPinsWear();
        }

        /// <summary>
        /// Устанавливает случайный износ существующим пинам в сокетах
        /// </summary>
        void SetRandomWearToInsertedPins()
        {
            List<GameObject> insertedPins = attachModel.GetInsertedPins();

            if (insertedPins.Count == 0)
            {
                Debug.LogWarning("PinReplacementTask: Нет вставленных пинов для установки износа");
                return;
            }

            // Определяем сколько пинов будут изношены (минимум minWornPins)
            int wornCount = Random.Range(minWornPins, insertedPins.Count + 1);
            wornCount = Mathf.Max(wornCount, minWornPins);

            // Перемешиваем список пинов
            List<GameObject> shuffledPins = new List<GameObject>(insertedPins);
            ShuffleList(shuffledPins);

            // Назначаем износ
            for (int i = 0; i < shuffledPins.Count; i++)
            {
                GameObject pin = shuffledPins[i];
                PinWear pinWear = pin.GetComponent<PinWear>();

                if (pinWear != null)
                {
                    float wear;
                    
                    if (i < wornCount)
                    {
                        // Изношенный пин (>50%)
                        wear = Random.Range(minWornValue, maxWornValue);
                    }
                    else
                    {
                        // Нормальный пин (<50%)
                        wear = Random.Range(minNormalValue, maxNormalValue);
                    }

                    pinWear.SetWearLevel(wear);
                    Debug.Log($"PinReplacementTask: Пин {pin.name} получил износ {wear:F1}%");
                }
            }
        }

        /// <summary>
        /// Перемешивает список (Fisher-Yates shuffle)
        /// </summary>
        void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Вызывается при изменении пинов в сокетах
        /// </summary>
        void OnPinsChanged(float averageWear)
        {
            if (isTaskActive)
            {
                CheckTaskCompletion();
            }
        }

        /// <summary>
        /// Проверяет выполнение задания
        /// </summary>
        void CheckTaskCompletion()
        {
            if (!isTaskActive)
                return;

            bool isCompleted = IsTaskCompleted();

            // Если статус изменился
            if (isCompleted && !wasCompleted)
            {
                // Задание выполнено
                wasCompleted = true;
                OnTaskCompleted?.Invoke();
                
                if (dayEventManager != null)
                {
                    dayEventManager.CompleteEvent("ReplacePins");
                }

                Debug.Log("PinReplacementTask: Задание ВЫПОЛНЕНО!");
            }
            else if (!isCompleted && wasCompleted)
            {
                // Задание снова не выполнено
                wasCompleted = false;
                OnTaskUncompleted?.Invoke();
                
                if (dayEventManager != null)
                {
                    dayEventManager.UncompleteEvent("ReplacePins");
                }

                Debug.Log("PinReplacementTask: Задание ОТМЕНЕНО - условия не выполнены!");
            }
        }

        /// <summary>
        /// Проверяет, выполнено ли задание
        /// Условия: все сокеты заполнены И все пины имеют износ < 50%
        /// </summary>
        bool IsTaskCompleted()
        {
            // Проверяем, что все сокеты заполнены
            int socketsCount = attachModel.InsertPoints.Count;
            int insertedCount = attachModel.GetInsertedPinsCount();

            if (insertedCount < socketsCount)
            {
                return false; // Не все сокеты заполнены
            }

            // Проверяем износ всех пинов
            List<GameObject> pins = attachModel.GetInsertedPins();

            foreach (GameObject pin in pins)
            {
                PinWear pinWear = pin.GetComponent<PinWear>();
                
                if (pinWear != null)
                {
                    if (pinWear.WearLevel >= wearThreshold)
                    {
                        return false; // Найден изношенный пин
                    }
                }
            }

            return true; // Все условия выполнены
        }

        /// <summary>
        /// Ограничивает износ всех пинов до максимум 50%
        /// </summary>
        void LimitPinsWear()
        {
            List<GameObject> pins = attachModel.GetInsertedPins();

            foreach (GameObject pin in pins)
            {
                PinWear pinWear = pin.GetComponent<PinWear>();
                
                if (pinWear != null && pinWear.WearLevel > wearThreshold)
                {
                    pinWear.SetWearLevel(Random.Range(minNormalValue, maxNormalValue));
                    Debug.Log($"PinReplacementTask: Износ пина {pin.name} ограничен до {pinWear.WearLevel:F1}%");
                }
            }
        }

        /// <summary>
        /// Возвращает статус задания
        /// </summary>
        public bool IsActive()
        {
            return isTaskActive;
        }

        /// <summary>
        /// Возвращает прогресс задания в текстовом виде
        /// </summary>
        public string GetProgressText()
        {
            if (!isTaskActive)
                return "Задание неактивно";

            int socketsCount = attachModel.InsertPoints.Count;
            int insertedCount = attachModel.GetInsertedPinsCount();
            int wornCount = 0;

            List<GameObject> pins = attachModel.GetInsertedPins();
            foreach (GameObject pin in pins)
            {
                PinWear pinWear = pin.GetComponent<PinWear>();
                if (pinWear != null && pinWear.WearLevel >= wearThreshold)
                {
                    wornCount++;
                }
            }

            return $"Замена пинов: {insertedCount}/{socketsCount} сокетов, {wornCount} изношенных";
        }
    }
}