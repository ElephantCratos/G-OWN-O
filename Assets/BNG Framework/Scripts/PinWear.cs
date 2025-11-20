using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG
{
    /// <summary>
    /// Компонент для отслеживания степени износа пина
    /// </summary>
    public class PinWear : MonoBehaviour
    {
        [Header("Степень износа")]
        [Tooltip("Значение от 0 (новый) до 100 (полностью изношен)")]
        [Range(0f, 100f)]
        public float WearLevel = 0f;

        [Header("Визуальная индикация (опционально)")]
        [Tooltip("Материал пина будет менять цвет в зависимости от износа")]
        public Renderer pinRenderer;
        public Color newPinColor = Color.green;
        public Color wornPinColor = Color.red;

        void Start()
        {
            UpdateVisuals();
        }

        /// <summary>
        /// Устанавливает степень износа
        /// </summary>
        public void SetWearLevel(float wear)
        {
            WearLevel = Mathf.Clamp(wear, 0f, 100f);
            UpdateVisuals();
        }

        /// <summary>
        /// Увеличивает износ на указанное значение
        /// </summary>
        public void AddWear(float amount)
        {
            SetWearLevel(WearLevel + amount);
        }

        /// <summary>
        /// Возвращает нормализованное значение износа (0-1)
        /// </summary>
        public float GetNormalizedWear()
        {
            return WearLevel / 100f;
        }

        /// <summary>
        /// Обновляет визуальное отображение износа
        /// </summary>
        void UpdateVisuals()
        {
            if (pinRenderer != null)
            {
                float t = GetNormalizedWear();
                Color currentColor = Color.Lerp(newPinColor, wornPinColor, t);
                pinRenderer.material.color = currentColor;
            }
        }

#if UNITY_EDITOR
        // Для удобной настройки в редакторе
        void OnValidate()
        {
            UpdateVisuals();
        }
#endif
    }
}