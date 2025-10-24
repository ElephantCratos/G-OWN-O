using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class PatchableHole : MonoBehaviour {
        public float RepairProgress = 0f; // 0–100%
        public bool IsPatched => RepairProgress >= 100f;
        
        public ParticleSystem RepairEffect; // Визуализация заварки
        public Renderer HoleRenderer;
        public Color DamagedColor = Color.red;
        public Color PatchedColor = Color.gray;

        private void Start() {
            if (HoleRenderer != null) {
                HoleRenderer.material.color = DamagedColor;
            }
        }

        // Метод увеличения прогресса
        public void ApplyRepair(float amountPerSecond) {
            if (IsPatched) return;

            RepairProgress += amountPerSecond * Time.deltaTime;
            RepairProgress = Mathf.Clamp(RepairProgress, 0f, 100f);

            if (RepairEffect != null && !RepairEffect.isPlaying) {
                RepairEffect.Play();
            }

            // Обновляем цвет по прогрессу
            if (HoleRenderer != null) {
                HoleRenderer.material.color = Color.Lerp(DamagedColor, PatchedColor, RepairProgress / 100f);
            }

            // Если заварена
            if (IsPatched) {
                if (RepairEffect != null) RepairEffect.Stop();
                
                OnPatched();
            }
        }

        private void OnPatched() {
            Debug.Log($"{name} patched!");
            GetComponent<Collider>().enabled = false;
            Destroy(gameObject);
           
        }
    }
}