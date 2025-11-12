using UnityEngine;

namespace BNG {
    public class LeverColorChanger : MonoBehaviour {

        [Tooltip("Ссылка на компонент рычага (Lever)")]
        public Lever lever;

        [Tooltip("Кубик, цвет которого будет меняться")]
        public Renderer cubeRenderer;

        [Tooltip("Минимальное значение процента, при котором куб начнет зеленеть")]
        public float greenZoneStart = 80f;

        private Color redColor = Color.red;
        private Color greenColor = Color.green;

        private Material _mat;
        private string _colorProperty = "_Color"; // стандартное имя, если не URP/HDRP

        void Start() {
            if (lever == null) {
                Debug.LogError("[LeverColorChanger] Lever не назначен!");
                return;
            }

            if (cubeRenderer == null) {
                Debug.LogError("[LeverColorChanger] Cube Renderer не назначен!");
                return;
            }

            // Копируем материал, чтобы не менять общий (иначе Unity не отобразит изменения)
            _mat = cubeRenderer.material;

            // Определяем правильное свойство цвета
            if (_mat.HasProperty("_BaseColor")) {
                _colorProperty = "_BaseColor"; // URP/HDRP
            } else if (_mat.HasProperty("_Color")) {
                _colorProperty = "_Color"; // Built-in
            } else {
                Debug.LogWarning($"[LeverColorChanger] Материал {_mat.name} не имеет свойства _Color или _BaseColor!");
            }

            lever.onLeverChange.AddListener(OnLeverMoved);
            Debug.Log("[LeverColorChanger] Подписка выполнена, цвет будет меняться при движении рычага.");
        }

        void OnLeverMoved(float percentage) {
            // Нормализуем в диапазоне 0..1
            float t = Mathf.InverseLerp(greenZoneStart, 100f, percentage);
            Color newColor = Color.Lerp(redColor, greenColor, t);

            if (_mat != null) {
                _mat.SetColor(_colorProperty, newColor);
            } else {
                Debug.LogWarning("[LeverColorChanger] Материал отсутствует!");
            }
        }
    }
}