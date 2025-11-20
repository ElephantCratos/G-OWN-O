using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BNG {
    /// <summary>
    /// Панель управления космическим кораблём, отображающая текущие и целевые параметры
    /// </summary>
    public class SpaceshipControlPanel : MonoBehaviour {

        [Header("Ссылки на крутилки и рычаг")]
        [Tooltip("Крутилка для вертикального направления (вверх-вниз)")]
        public HingeHelper verticalKnob;

        [Tooltip("Крутилка для горизонтального направления (влево-вправо)")]
        public HingeHelper horizontalKnob;

        [Tooltip("Рычаг для управления скоростью")]
        public Lever speedLever;

        [Header("UI элементы - Текущие значения")]
        [Tooltip("Текст для отображения текущего вертикального угла")]
        public TextMeshProUGUI currentVerticalText;

        [Tooltip("Текст для отображения текущего горизонтального угла")]
        public TextMeshProUGUI currentHorizontalText;

        [Tooltip("Текст для отображения текущей скорости")]
        public TextMeshProUGUI currentSpeedText;

        [Header("UI элементы - Целевые значения")]
        [Tooltip("Текст для отображения целевого вертикального угла")]
        public TextMeshProUGUI targetVerticalText;

        [Tooltip("Текст для отображения целевого горизонтального угла")]
        public TextMeshProUGUI targetHorizontalText;

        [Tooltip("Текст для отображения целевой скорости")]
        public TextMeshProUGUI targetSpeedText;

        [Header("UI элементы - Статус")]
        [Tooltip("Индикатор статуса вертикального направления")]
        public Image verticalStatusImage;

        [Tooltip("Индикатор статуса горизонтального направления")]
        public Image horizontalStatusImage;

        [Tooltip("Индикатор статуса скорости")]
        public Image speedStatusImage;

        [Tooltip("Общий индикатор готовности (все параметры в норме)")]
        public Image overallStatusImage;

        [Header("Целевые параметры")]
        [Tooltip("Целевой вертикальный угол (градусы)")]
        public float targetVerticalAngle = 90f;

        [Tooltip("Целевой горизонтальный угол (градусы)")]
        public float targetHorizontalAngle = 180f;

        [Tooltip("Целевой процент скорости (0-100)")]
        public float targetSpeedPercent = 75f;

        [Header("Настройки допуска")]
        [Tooltip("Допустимое отклонение для углов (градусы)")]
        public float angleTolerance = 10f;

        [Tooltip("Допустимое отклонение для скорости (проценты)")]
        public float speedTolerance = 5f;

        [Header("Цвета")]
        public Color correctColor = Color.green;
        public Color incorrectColor = Color.red;
        public Color warningColor = Color.yellow;

        // Текущие значения
        private float currentVertical;
        private float currentHorizontal;
        private float currentSpeed;

        private void Start() {
            // Подписываемся на изменения крутилок и рычага
            if (verticalKnob != null) {
                verticalKnob.onHingeChange.AddListener(OnVerticalChanged);
            }

            if (horizontalKnob != null) {
                horizontalKnob.onHingeChange.AddListener(OnHorizontalChanged);
            }

            if (speedLever != null) {
                speedLever.onLeverChange.AddListener(OnSpeedChanged);
            }

            // Инициализация целевых значений
            UpdateTargetDisplay();
            UpdateDisplay();
        }

        private void OnDestroy() {
            if (verticalKnob != null) {
                verticalKnob.onHingeChange.RemoveListener(OnVerticalChanged);
            }

            if (horizontalKnob != null) {
                horizontalKnob.onHingeChange.RemoveListener(OnHorizontalChanged);
            }

            if (speedLever != null) {
                speedLever.onLeverChange.RemoveListener(OnSpeedChanged);
            }
        }

        private void OnVerticalChanged(float angle) {
            currentVertical = NormalizeAngle(angle);
            UpdateDisplay();
        }

        private void OnHorizontalChanged(float angle) {
            currentHorizontal = NormalizeAngle(angle);
            UpdateDisplay();
        }

        private void OnSpeedChanged(float percentage) {
            currentSpeed = percentage;
            UpdateDisplay();
        }

        private void UpdateDisplay() {
            // Обновляем текущие значения
            if (currentVerticalText != null) {
                currentVerticalText.text = $"Вертикаль: {currentVertical:F0}°";
            }

            if (currentHorizontalText != null) {
                currentHorizontalText.text = $"Горизонталь: {currentHorizontal:F0}°";
            }

            if (currentSpeedText != null) {
                currentSpeedText.text = $"Скорость: {currentSpeed:F0}%";
            }

            // Проверяем каждый параметр
            bool verticalCorrect = IsAngleInRange(currentVertical, targetVerticalAngle, angleTolerance);
            bool horizontalCorrect = IsAngleInRange(currentHorizontal, targetHorizontalAngle, angleTolerance);
            bool speedCorrect = IsSpeedInRange(currentSpeed, targetSpeedPercent, speedTolerance);

            // Обновляем индикаторы статуса
            if (verticalStatusImage != null) {
                verticalStatusImage.color = verticalCorrect ? correctColor : incorrectColor;
            }

            if (horizontalStatusImage != null) {
                horizontalStatusImage.color = horizontalCorrect ? correctColor : incorrectColor;
            }

            if (speedStatusImage != null) {
                speedStatusImage.color = speedCorrect ? correctColor : incorrectColor;
            }

            // Общий статус
            if (overallStatusImage != null) {
                if (verticalCorrect && horizontalCorrect && speedCorrect) {
                    overallStatusImage.color = correctColor;
                } else if (verticalCorrect || horizontalCorrect || speedCorrect) {
                    overallStatusImage.color = warningColor;
                } else {
                    overallStatusImage.color = incorrectColor;
                }
            }
        }

        private void UpdateTargetDisplay() {
            if (targetVerticalText != null) {
                targetVerticalText.text = $"Цель: {targetVerticalAngle:F0}° (±{angleTolerance:F0}°)";
            }

            if (targetHorizontalText != null) {
                targetHorizontalText.text = $"Цель: {targetHorizontalAngle:F0}° (±{angleTolerance:F0}°)";
            }

            if (targetSpeedText != null) {
                targetSpeedText.text = $"Цель: {targetSpeedPercent:F0}% (±{speedTolerance:F0}%)";
            }
        }

        private bool IsAngleInRange(float current, float target, float tolerance) {
            float diff = Mathf.Abs(Mathf.DeltaAngle(current, target));
            return diff <= tolerance;
        }

        private bool IsSpeedInRange(float current, float target, float tolerance) {
            float diff = Mathf.Abs(current - target);
            return diff <= tolerance;
        }

        private float NormalizeAngle(float angle) {
            angle %= 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }

        // Публичные методы для изменения целевых параметров во время игры
        public void SetTargetVertical(float angle) {
            targetVerticalAngle = angle;
            UpdateTargetDisplay();
            UpdateDisplay();
        }

        public void SetTargetHorizontal(float angle) {
            targetHorizontalAngle = angle;
            UpdateTargetDisplay();
            UpdateDisplay();
        }

        public void SetTargetSpeed(float percent) {
            targetSpeedPercent = Mathf.Clamp(percent, 0f, 100f);
            UpdateTargetDisplay();
            UpdateDisplay();
        }

        public bool AreAllParametersCorrect() {
            return IsAngleInRange(currentVertical, targetVerticalAngle, angleTolerance) &&
                   IsAngleInRange(currentHorizontal, targetHorizontalAngle, angleTolerance) &&
                   IsSpeedInRange(currentSpeed, targetSpeedPercent, speedTolerance);
        }
    }
}