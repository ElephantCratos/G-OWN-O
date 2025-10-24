using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace BNG {
    public class ShipSystemParameter : MonoBehaviour {

        [Header("Parameter Settings")]
        public string ParameterName = "Stabilizer";
        public float CurrentValue = 50f;      // текущее значение (0–100)
        public float TargetValue = 50f;       // куда должно стремиться
        public float NormalMin = 45f;         // нижняя граница нормы
        public float NormalMax = 55f;         // верхняя граница нормы

        [Header("Random Failure Settings")]
        public float MinFailureInterval = 5f;
        public float MaxFailureInterval = 15f;
        public float MaxDeviation = 40f; // насколько далеко может сбиться параметр

        [Header("Visuals")]
        public Renderer IndicatorRenderer;
        public Color NormalColor = Color.green;
        public Color WarningColor = Color.red;

        private Coroutine failureRoutine;

        public bool InNormalRange => CurrentValue >= NormalMin && CurrentValue <= NormalMax;

        void Start() {
            failureRoutine = StartCoroutine(RandomFailures());
        }

        void Update() {
            // Плавное возвращение к target (можно убрать, если не нужно)
           

            UpdateIndicator();
        }

        IEnumerator RandomFailures() {
            while (true) {
                float waitTime = Random.Range(MinFailureInterval, MaxFailureInterval);
                yield return new WaitForSeconds(waitTime);

                // Случайный сбой — меняем targetValue
                float offset = Random.Range(-MaxDeviation, MaxDeviation);
                TargetValue = Mathf.Clamp(50f + offset, 0f, 100f);
                Debug.Log($"{ParameterName} drifted to {TargetValue}");
            }
        }

        void UpdateIndicator() {
            if (IndicatorRenderer != null) {
                IndicatorRenderer.material.color = InNormalRange ? NormalColor : WarningColor;
            }
        }

        // Метод корректировки (вызывается из тумблера)
        public void AdjustValue(float delta) {
            TargetValue = Mathf.Clamp(TargetValue + delta, 0f, 100f);
        }

        public void ForceStable() {
            TargetValue = 50f;
        }
    }
}

