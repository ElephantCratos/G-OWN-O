using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Если используешь TextMeshPro

namespace BNG
{
    /// <summary>
    /// UI индикатор среднего износа пинов в AttachModel
    /// </summary>
    public class WearIndicatorUI : MonoBehaviour
    {
        [Header("Ссылки")]
        public AttachModel attachModel;

        [Header("UI элементы")]
        [Tooltip("Текст для отображения процента износа")]
        public Text wearText; // Для обычного UI Text
        public TextMeshProUGUI wearTextTMP; // Для TextMeshPro

        [Tooltip("Image для визуального заполнения (Fill Amount)")]
        public Image wearFillImage;

        [Tooltip("Image для изменения цвета в зависимости от износа")]
        public Image wearColorImage;

        [Header("Настройки отображения")]
        public string textFormat = "Износ: {0:F1}%";
        
        [Header("Цвета по уровню износа")]
        public Color lowWearColor = Color.green;      // 0-30%
        public Color mediumWearColor = Color.yellow;   // 30-70%
        public Color highWearColor = Color.red;        // 70-100%

        [Header("Опции")]
        public bool hideWhenEmpty = false; // Скрывать UI когда нет пинов
        
        private CanvasGroup canvasGroup;

        void Start()
        {
            if (attachModel == null)
            {
                Debug.LogError("AttachModel не назначен в WearIndicatorUI!");
                return;
            }

            // Подписываемся на событие изменения износа
            attachModel.OnAverageWearChanged.AddListener(OnWearChanged);

            // Получаем CanvasGroup если есть
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && hideWhenEmpty)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Начальное обновление
            UpdateDisplay(attachModel.GetAverageWear());
        }

        void OnDestroy()
        {
            if (attachModel != null)
            {
                attachModel.OnAverageWearChanged.RemoveListener(OnWearChanged);
            }
        }

        /// <summary>
        /// Вызывается когда изменяется средний износ
        /// </summary>
        void OnWearChanged(float averageWear)
        {
            UpdateDisplay(averageWear);
        }

        /// <summary>
        /// Обновляет UI элементы
        /// </summary>
        void UpdateDisplay(float wearPercent)
        {
            // Проверяем есть ли вставленные пины
            int pinsCount = attachModel.GetInsertedPinsCount();
            bool hasPins = pinsCount > 0;

            // Скрываем/показываем UI
            if (hideWhenEmpty && canvasGroup != null)
            {
                canvasGroup.alpha = hasPins ? 1f : 0f;
            }

            // Если нет пинов, ничего не обновляем
            if (!hasPins && hideWhenEmpty)
                return;

            // Обновляем текст
            if (wearText != null)
            {
                wearText.text = string.Format(textFormat, wearPercent);
            }

            if (wearTextTMP != null)
            {
                wearTextTMP.text = string.Format(textFormat, wearPercent);
            }

            // Обновляем Fill Amount (0-1)
            if (wearFillImage != null)
            {
                wearFillImage.fillAmount = wearPercent / 100f;
            }

            // Обновляем цвет
            Color currentColor = GetColorForWear(wearPercent);
            
            if (wearColorImage != null)
            {
                wearColorImage.color = currentColor;
            }

            if (wearFillImage != null)
            {
                wearFillImage.color = currentColor;
            }
        }

        /// <summary>
        /// Возвращает цвет в зависимости от уровня износа
        /// </summary>
        Color GetColorForWear(float wearPercent)
        {
            if (wearPercent < 30f)
            {
                // Интерполяция между низким и средним
                float t = wearPercent / 30f;
                return Color.Lerp(lowWearColor, mediumWearColor, t);
            }
            else if (wearPercent < 70f)
            {
                // Интерполяция между средним и высоким
                float t = (wearPercent - 30f) / 40f;
                return Color.Lerp(mediumWearColor, highWearColor, t);
            }
            else
            {
                return highWearColor;
            }
        }

        /// <summary>
        /// Принудительное обновление (можно вызвать вручную)
        /// </summary>
        public void ForceUpdate()
        {
            if (attachModel != null)
            {
                UpdateDisplay(attachModel.GetAverageWear());
            }
        }
    }
}