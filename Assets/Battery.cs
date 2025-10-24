using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class Battery : Grabbable {
        [Header("Battery Settings")]
        public float MaxCharge = 100f;
        public float CurrentCharge = 100f;

        [Header("Visuals")]
        public Renderer BatteryRenderer;
        public Color EmptyColor = Color.red;
        public Color FullColor = Color.green;

        public bool IsInSocket = false;

        void Update() {
            if (BatteryRenderer != null) {
                BatteryRenderer.material.color = Color.Lerp(EmptyColor, FullColor, CurrentCharge / MaxCharge);
            }
        }

        public void Charge(float rate) {
            CurrentCharge = Mathf.Clamp(CurrentCharge + rate * Time.deltaTime, 0, MaxCharge);
        }

        public void Discharge(float rate) {
            CurrentCharge = Mathf.Clamp(CurrentCharge - rate * Time.deltaTime, 0, MaxCharge);
        }

        public bool IsEmpty() => CurrentCharge <= 0f;
        public bool IsFull() => CurrentCharge >= MaxCharge;
    }
}

