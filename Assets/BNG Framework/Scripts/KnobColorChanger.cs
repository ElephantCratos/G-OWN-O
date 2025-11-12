using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    public class KnobColorChanger : MonoBehaviour {

        [Header("References")]
        [Tooltip("Ссылка на HingeHelper крутилки")]
        public HingeHelper hinge;

        [Tooltip("Куб или другой объект, цвет которого будет меняться")]
        public Renderer targetRenderer;

        [Header("Настройки угла")]
        [Tooltip("Минимальный угол (при котором цвет красный)")]
        public float minAngle = 0f;

        [Tooltip("Максимальный угол (при котором цвет зелёный)")]
        public float maxAngle = 360f;

        private void Start() {
            if (hinge != null) {
                hinge.onHingeChange.AddListener(OnKnobChanged);
            }
        }

        private void OnDestroy() {
            if (hinge != null) {
                hinge.onHingeChange.RemoveListener(OnKnobChanged);
            }
        }

        private void OnKnobChanged(float angle) {
            if (targetRenderer == null) return;

            // Приводим угол в диапазон 0–360
            angle = NormalizeAngle(angle);

            // Получаем коэффициент 0–1
            float t = Mathf.InverseLerp(minAngle, maxAngle, angle);

            // Меняем цвет по градиенту: красный → жёлтый → зелёный
            // То есть сначала добавляем зелёный компонент, потом убавляем красный
            Color newColor;

            if (t < 0.5f) {
                // От красного к жёлтому (увеличиваем G)
                newColor = Color.Lerp(Color.red, Color.yellow, t * 2f);
            } else {
                // От жёлтого к зелёному (уменьшаем R)
                newColor = Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2f);
            }

            targetRenderer.material.color = newColor;
        }

        private float NormalizeAngle(float angle) {
            angle %= 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }
    }
}