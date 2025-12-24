using UnityEngine;
using UnityEngine.UI;
using BNG;


namespace BNG {
    public class SkyboxRotator : MonoBehaviour {
        
         public SpaceshipControlPanel controlPanel;
        
        [Header("Debug")]
        public bool enableDebug = true;
        
        private Material skyboxInstance;
        
        private void Start() {
            if (controlPanel == null) {
                Debug.LogError("SimpleSkyboxRotator: controlPanel не назначен!");
                return;
            }
            
            if (RenderSettings.skybox == null) {
                Debug.LogError("SimpleSkyboxRotator: Нет скайбокса в RenderSettings!");
                return;
            }
            
            // Клонируем материал — это ключевой момент!
            skyboxInstance = new Material(RenderSettings.skybox);
            RenderSettings.skybox = skyboxInstance;
            
            Debug.Log($"SimpleSkyboxRotator: Создан клон материала. Шейдер: {skyboxInstance.shader.name}");
        }
        
        private void LateUpdate() {
            if (controlPanel == null || skyboxInstance == null) return;
            
            float rotation = -controlPanel.CurrentHorizontal;
            skyboxInstance.SetFloat("_Rotation", rotation);
            
            if (enableDebug) {
                Debug.Log($"[Skybox] Rotation: {rotation:F1}°");
            }
        }
        
        private void OnDestroy() {
            // Чистим клон
            if (skyboxInstance != null) {
                Destroy(skyboxInstance);
            }
        }
        
        [ContextMenu("Test Rotate 90")]
        private void TestRotate90() {
            if (skyboxInstance != null) {
                skyboxInstance.SetFloat("_Rotation", 90f);
                Debug.Log("Тест: _Rotation = 90");
            }
        }
        
        [ContextMenu("Test Rotate 0")]
        private void TestRotate0() {
            if (skyboxInstance != null) {
                skyboxInstance.SetFloat("_Rotation", 0f);
                Debug.Log("Тест: _Rotation = 0");
            }
        }
    }
}