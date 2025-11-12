using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class KnobTargetController : MonoBehaviour {

        [Header("References")]
        public HingeHelper hinge;
        public Renderer targetRenderer;

        [Tooltip("Объект, который фактически вращается (может быть тем же, где HingeHelper)")]
        public Transform knobTransform;

        [Header("Target Settings")]
        [Tooltip("Целевой угол, при котором куб становится зелёным")]
        public float targetAngle = 270f;

        [Tooltip("Допустимое отклонение (в градусах)")]
        public float tolerance = 10f;

        [Tooltip("Скорость автоподкручивания (градусов в секунду)")]
        public float autoTwistSpeed = 40f;

        [Tooltip("Как часто менять направление (секунды)")]
        public float twistInterval = 1.5f;

        private float currentAngle;
        private float twistDirection;
        private float nextTwistTime;

        private void Start() {
            if (hinge != null) {
                hinge.onHingeChange.AddListener(OnKnobChanged);
            }

            if (!knobTransform) {
                knobTransform = transform;
            }

            nextTwistTime = Time.time + twistInterval;
            twistDirection = Random.value > 0.5f ? 1f : -1f;
        }

        private void OnDestroy() {
            if (hinge != null) {
                hinge.onHingeChange.RemoveListener(OnKnobChanged);
            }
        }

        private void Update() {
            // Если не в целевом диапазоне — подкручиваем
            if (!IsInTargetRange()) {
                AutoTwist();
            }
        }

        private void AutoTwist() {
            if (Time.time > nextTwistTime) {
                twistDirection = Random.value > 0.5f ? 1f : -1f;
                nextTwistTime = Time.time + twistInterval + Random.Range(-0.5f, 0.5f);
            }

            // Поворачиваем крутилку вручную
            float delta = autoTwistSpeed * twistDirection * Time.deltaTime;
            float newY = NormalizeAngle(knobTransform.localEulerAngles.y + delta);
            knobTransform.localEulerAngles = new Vector3(
                knobTransform.localEulerAngles.x,
                newY,
                knobTransform.localEulerAngles.z
            );
        }

        private void OnKnobChanged(float angle) {
            currentAngle = NormalizeAngle(angle);

            if (targetRenderer != null) {
                targetRenderer.material.color = IsInTargetRange() ? Color.green : Color.red;
            }
        }

        private bool IsInTargetRange() {
            float diff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
            return diff <= tolerance;
        }

        private float NormalizeAngle(float angle) {
            angle %= 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }
    }
}