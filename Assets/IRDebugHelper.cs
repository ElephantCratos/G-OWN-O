using UnityEngine;

namespace BNG
{
    /// <summary>
    /// Вспомогательный скрипт для отладки ИК-системы
    /// Прикрепите к любому объекту в сцене
    /// </summary>
    public class IRDebugHelper : MonoBehaviour
    {
        [Header("Debug Controls")]
        [Tooltip("Клавиша для ручного переключения ИК-режима")]
        public KeyCode toggleKey = KeyCode.I;
        
        [Tooltip("Клавиша для проверки состояния всех ИК-частиц")]
        public KeyCode checkStatusKey = KeyCode.P;
        
        private NightVisionController nvController;
        
        void Start()
        {
            nvController = FindObjectOfType<NightVisionController>();
            
            if (nvController == null)
            {
                Debug.LogError("[IRDebug] NightVisionController not found in scene!");
            }
            else
            {
                Debug.Log("[IRDebug] NightVisionController found: " + nvController.gameObject.name);
            }
            
            CheckAllIRParticles();
        }
        
        void Update()
        {
            // Ручное переключение для теста
            if (Input.GetKeyDown(toggleKey))
            {
                if (nvController != null)
                {
                    Debug.Log("[IRDebug] Manual toggle pressed (I key)");
                    nvController.ToggleNightVision();
                }
                else
                {
                    Debug.LogError("[IRDebug] Cannot toggle - NightVisionController is null");
                }
            }
            
            // Проверка состояния
            if (Input.GetKeyDown(checkStatusKey))
            {
                CheckAllIRParticles();
            }
        }
        
        void CheckAllIRParticles()
        {
            Debug.Log("========== IR PARTICLES STATUS ==========");
            
            IRVisibleParticles[] allIR = FindObjectsOfType<IRVisibleParticles>();
            Debug.Log($"Total IRVisibleParticles in scene: {allIR.Length}");
            
            for (int i = 0; i < allIR.Length; i++)
            {
                var ir = allIR[i];
                var ps = ir.GetComponent<ParticleSystem>();
                var renderer = ir.GetComponent<ParticleSystemRenderer>();
                
                Debug.Log($"--- IR Particle #{i}: {ir.gameObject.name} ---");
                Debug.Log($"  Position: {ir.transform.position}");
                Debug.Log($"  UseSimpleVisibility: {ir.useSimpleVisibility}");
                Debug.Log($"  AutoPlayOnIRActive: {ir.autoPlayOnIRActive}");
                Debug.Log($"  Renderer.enabled: {(renderer != null ? renderer.enabled.ToString() : "NULL")}");
                
                if (renderer != null && renderer.material != null)
                {
                    Debug.Log($"  Material: {renderer.material.name}");
                    Debug.Log($"  Material Shader: {renderer.material.shader.name}");
                    Debug.Log($"  Material Color: {renderer.material.GetColor("_Color")}");
                }
                
                Debug.Log($"  ParticleSystem.isPlaying: {(ps != null ? ps.isPlaying.ToString() : "NULL")}");
                
                if (ps != null)
                {
                    Debug.Log($"  Particle Count: {ps.particleCount}");
                    Debug.Log($"  Start Size: {ps.main.startSize.constant}");
                    Debug.Log($"  Start Color: {ps.main.startColor.color}");
                    Debug.Log($"  Emission Rate: {ps.emission.rateOverTime.constant}");
                }
                
                Debug.Log($"  GameObject.activeSelf: {ir.gameObject.activeSelf}");
                Debug.Log($"  GameObject.activeInHierarchy: {ir.gameObject.activeInHierarchy}");
            }
            
            if (nvController != null)
            {
                Debug.Log($"NightVision IsActive: {nvController.IsActive}");
            }
            
            Debug.Log("=========================================");
        }
        
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== IR DEBUG HELPER ===");
            
            if (nvController != null)
            {
                GUILayout.Label($"NV Active: {nvController.IsActive}");
            }
            else
            {
                GUILayout.Label("NV Controller: NOT FOUND");
            }
            
            IRVisibleParticles[] allIR = FindObjectsOfType<IRVisibleParticles>();
            GUILayout.Label($"IR Particles: {allIR.Length}");
            
            GUILayout.Space(10);
            GUILayout.Label($"Press [{toggleKey}] to toggle NV");
            GUILayout.Label($"Press [{checkStatusKey}] to check status");
            
            if (GUILayout.Button("Toggle NV (Button)"))
            {
                if (nvController != null)
                {
                    nvController.ToggleNightVision();
                }
            }
            
            GUILayout.EndArea();
        }
    }
}