using UnityEngine;

namespace BNG {
    public class LeverTargetController : MonoBehaviour {

        [Header("References")]
        [Tooltip("Ссылка на компонент рычага")]
        public Lever lever;

        [Tooltip("Объект, который визуально вращается (если не задан, берётся текущий)")]
        public Transform leverTransform;

        [Tooltip("Куб или другой объект, чей цвет нужно менять")]
        public Renderer targetRenderer;

        [Header("Target Settings")]
        [Tooltip("Целевой угол, при котором куб становится зелёным (в градусах по X)")]
        public float targetAngle = 30f;

        [Tooltip("Допустимое отклонение (в градусах)")]
        public float tolerance = 5f;

        [Tooltip("Скорость автоподкручивания (градусов в секунду)")]
        public float autoTwistSpeed = 25f;

        [Tooltip("Как часто менять направление (секунды)")]
        public float twistInterval = 1.5f;

        private float currentAngle;
        private float twistDirection;
        private float nextTwistTime;
        private bool isInTarget;

        private void Start() {
            if (lever != null) {
                lever.onLeverChange.AddListener(OnLeverChanged);
            }

            if (!leverTransform) {
                leverTransform = transform;
            }

            twistDirection = Random.value > 0.5f ? 1f : -1f;
            nextTwistTime = Time.time + twistInterval;
        }

        private void OnDestroy() {
            if (lever != null) {
                lever.onLeverChange.RemoveListener(OnLeverChanged);
            }
        }

        private void Update() {
            // Если не в целевом положении — "поддёргиваем" рычаг
            if (!IsInTargetRange()) {
                AutoTwist();
            }
        }

        private void OnLeverChanged(float percentage) {
            // Получаем текущий угол X из transform
            currentAngle = leverTransform.localEulerAngles.x;
            if (currentAngle > 180) currentAngle -= 360; // нормализация

            bool newState = IsInTargetRange();

            // Меняем цвет кубика
            if (targetRenderer != null) {
                targetRenderer.material.color = newState ? Color.green : Color.red;
            }

            // Если только что попал в нужный диапазон — можно сделать звук/щелчок
            if (newState && !isInTarget) {
                // Здесь можно добавить звук или эффект
            }

            isInTarget = newState;
        }

        private bool IsInTargetRange() {
            float diff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
            return diff <= tolerance;
        }

        private void AutoTwist() {
            if (Time.time > nextTwistTime) {
                twistDirection = Random.value > 0.5f ? 1f : -1f;
                nextTwistTime = Time.time + twistInterval + Random.Range(-0.5f, 0.5f);
            }

            // Двигаем рычаг вручную по X
            float delta = autoTwistSpeed * twistDirection * Time.deltaTime;
            float newX = leverTransform.localEulerAngles.x + delta;

            // нормализуем диапазон в [-180, 180]
            if (newX > 180) newX -= 360;

            // ограничиваем в пределах рычага
            newX = Mathf.Clamp(newX, lever.MinimumXRotation, lever.MaximumXRotation);

            leverTransform.localEulerAngles = new Vector3(newX, leverTransform.localEulerAngles.y, leverTransform.localEulerAngles.z);
        }
    }
}