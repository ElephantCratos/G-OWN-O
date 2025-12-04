using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BNG {
    public class SpaceshipControlPanel : MonoBehaviour {

        [Header("Ссылки на крутилки и рычаг")]
        public HingeHelper verticalKnob;
        public HingeHelper horizontalKnob;
        public Lever speedLever;

        [Header("UI элементы - Текущие значения")]
        public TextMeshProUGUI currentVerticalText;
        public TextMeshProUGUI currentHorizontalText;
        public TextMeshProUGUI currentSpeedText;

        [Header("UI элементы - Целевые значения")]
        public TextMeshProUGUI targetVerticalText;
        public TextMeshProUGUI targetHorizontalText;
        public TextMeshProUGUI targetSpeedText;

        [Header("UI элементы - Статус")]
        public Image verticalStatusImage;
        public Image horizontalStatusImage;
        public Image speedStatusImage;
        public Image overallStatusImage;

        [Header("Целевые параметры")]
        public float targetVerticalAngle = 90f;
        public float targetHorizontalAngle = 180f;
        public float targetSpeedPercent = 75f;

        [Header("Настройки допуска")]
        public float angleTolerance = 10f;
        public float speedTolerance = 5f;

        [Header("Цвета")]
        public Color correctColor = Color.green;
        public Color incorrectColor = Color.red;
        public Color warningColor = Color.yellow;

        [Header("Блокировка управления")]
        [Tooltip("Заблокированы ли контролы")]
        public bool controlsLocked = true;

        // Текущие значения
        private float currentVertical;
        private float currentHorizontal;
        private float currentSpeed;

        // Кэшированные ссылки на Grabbable
        private Grabbable verticalGrabbable;
        private Grabbable horizontalGrabbable;
        private Grabbable speedGrabbable;
        private Rigidbody verticalRb;
        private Rigidbody horizontalRb;
        private Rigidbody speedRb;
        private Collider verticalCollider;
        private Collider horizontalCollider;
        private Lever speedCollider;

        // Сохранённые исходные состояния
        private bool verticalWasKinematic;
        private bool horizontalWasKinematic;
        private bool speedWasKinematic;
        private void Start() {
    // Кэшируем Grabbable компоненты
    if (verticalKnob != null) {
        verticalGrabbable = verticalKnob.GetComponent<Grabbable>();
        verticalRb = verticalKnob.GetComponent<Rigidbody>();
        verticalKnob.onHingeChange.AddListener(OnVerticalChanged);
    }

    if (horizontalKnob != null) {
        horizontalGrabbable = horizontalKnob.GetComponent<Grabbable>();
        horizontalRb = horizontalKnob.GetComponent<Rigidbody>();
        horizontalKnob.onHingeChange.AddListener(OnHorizontalChanged);
    }

    if (speedLever != null) {
        speedGrabbable = speedLever.GetComponent<Grabbable>();
        speedRb = speedLever.GetComponent<Rigidbody>();
        speedLever.onLeverChange.AddListener(OnSpeedChanged);
    }
    if (speedLever != null) speedCollider = speedLever.GetComponent<Lever>();
    // По умолчанию блокируем и выставляем в целевые позиции
    SetControlsToTargetPositions();
    LockControls();
    
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
            if (currentVerticalText != null) {
                currentVerticalText.text = $"Вертикаль: {currentVertical:F0}°";
            }

            if (currentHorizontalText != null) {
                currentHorizontalText.text = $"Горизонталь: {currentHorizontal:F0}°";
            }

            if (currentSpeedText != null) {
                currentSpeedText.text = $"Скорость: {currentSpeed:F0}%";
            }

            bool verticalCorrect = IsAngleInRange(currentVertical, targetVerticalAngle, angleTolerance);
            bool horizontalCorrect = IsAngleInRange(currentHorizontal, targetHorizontalAngle, angleTolerance);
            bool speedCorrect = IsSpeedInRange(currentSpeed, targetSpeedPercent, speedTolerance);

            if (verticalStatusImage != null) {
                verticalStatusImage.color = verticalCorrect ? correctColor : incorrectColor;
            }

            if (horizontalStatusImage != null) {
                horizontalStatusImage.color = horizontalCorrect ? correctColor : incorrectColor;
            }

            if (speedStatusImage != null) {
                speedStatusImage.color = speedCorrect ? correctColor : incorrectColor;
            }

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

        // ==================== БЛОКИРОВКА ====================

        /// <summary>
        /// Блокирует крутилки и рычаг — игрок не может их трогать
        /// </summary>
        /// <summary>
/// Блокирует крутилки и рычаг — игрок не может их трогать
/// </summary>
public void LockControls()
{
    controlsLocked = true;
    
    // Отключаем Grabbable
    if (verticalGrabbable != null) verticalGrabbable.enabled = false;
    if (horizontalGrabbable != null) horizontalGrabbable.enabled = false;
    if (speedGrabbable != null) speedGrabbable.enabled = false;
    if (speedCollider != null) speedCollider.enabled = false;
    
    // Замораживаем Rigidbody — сохраняем исходное состояние
    if (verticalRb != null)
    {
        verticalWasKinematic = verticalRb.isKinematic;
        verticalRb.isKinematic = true;
    }
    
    if (horizontalRb != null)
    {
        horizontalWasKinematic = horizontalRb.isKinematic;
        horizontalRb.isKinematic = true;
    }
    
    if (speedRb != null)
    {
        speedWasKinematic = speedRb.isKinematic;
        speedRb.isKinematic = true;
    }

   
    
    Debug.Log("Контролы заблокированы");
}

/// <summary>
/// Разблокирует крутилки и рычаг
/// </summary>
public void UnlockControls()
{
    controlsLocked = false;
    
    // Включаем Grabbable
    if (verticalGrabbable != null) verticalGrabbable.enabled = true;
    if (horizontalGrabbable != null) horizontalGrabbable.enabled = true;
    if (speedGrabbable != null) speedGrabbable.enabled = true;
    if (speedCollider != null) speedCollider.enabled = true;
    
    // Восстанавливаем исходное состояние Rigidbody
    if (verticalRb != null)
    {
        verticalRb.isKinematic = verticalWasKinematic;
    }
    
    if (horizontalRb != null)
    {
        horizontalRb.isKinematic = horizontalWasKinematic;
    }
    
    if (speedRb != null)
    {
        speedRb.isKinematic = speedWasKinematic;
    }

    
    
    Debug.Log("Контролы разблокированы");
}


        /// <summary>
        /// Физически выставляет крутилки и рычаг в целевые позиции
        /// </summary>
        public void SetControlsToTargetPositions()
        {
            if (verticalKnob != null)
            {
                verticalKnob.SetHingeAngle(targetVerticalAngle);
            }
            
            if (horizontalKnob != null)
            {
                horizontalKnob.SetHingeAngle(targetHorizontalAngle);
            }
            
            if (speedLever != null)
            {
                speedLever.SetLeverPercentage(targetSpeedPercent);
            }
            
            // Синхронизируем внутренние значения
            currentVertical = targetVerticalAngle;
            currentHorizontal = targetHorizontalAngle;
            currentSpeed = targetSpeedPercent;
            
            UpdateDisplay();
            
            Debug.Log($"Контролы выставлены: V={targetVerticalAngle}°, H={targetHorizontalAngle}°, S={targetSpeedPercent}%");
        }

        // ==================== ПУБЛИЧНЫЕ МЕТОДЫ ====================

        public void SetTargetVertical(float angle) {
            targetVerticalAngle = NormalizeAngle(angle);
            UpdateTargetDisplay();
            UpdateDisplay();
        }

        public void SetTargetHorizontal(float angle) {
            targetHorizontalAngle = NormalizeAngle(angle);
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
        
        public bool IsLocked() => controlsLocked;
    }
}
