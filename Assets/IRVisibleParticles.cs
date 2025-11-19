using UnityEngine;

namespace BNG
{
    /// <summary>
    /// Компонент для частиц, которые видны только в ИК-режиме
    /// Прикрепите к GameObject с ParticleSystem
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class IRVisibleParticles : MonoBehaviour
    {
        [Header("IR Visibility Settings")]
        [Tooltip("Цвет свечения в ИК-режиме")]
        public Color irGlowColor = new Color(0f, 1f, 0.3f, 1f);
        
        [Tooltip("Интенсивность свечения в ИК-режиме")]
        [Range(0f, 5f)]
        public float irIntensity = 2.5f;
        
        [Tooltip("Видимость в обычном режиме (0 = невидимо, 1 = полностью видимо)")]
        [Range(0f, 1f)]
        public float normalVisibility = 0.05f;
        
        [Header("Particle Settings")]
        [Tooltip("Автоматически играть при включении ИК-режима")]
        public bool autoPlayOnIRActive = true;
        
        [Tooltip("Автоматически останавливать при выключении ИК-режима")]
        public bool autoStopOnIRInactive = false;
        
        private ParticleSystem particles;
        private ParticleSystemRenderer particleRenderer;
        private Material particleMaterial;
        private bool isIRActive = false;
        
        void Start()
        {
            particles = GetComponent<ParticleSystem>();
            particleRenderer = GetComponent<ParticleSystemRenderer>();
            
            // Создаем материал для частиц
            SetupIRMaterial();
            
            // Устанавливаем начальную видимость
            SetVisibility(false);
            
            // Если ИК-режим уже активен, обновляем
            if (Shader.GetGlobalFloat("_IRModeActive") > 0.5f)
            {
                SetVisibility(true);
            }
        }
        
        void SetupIRMaterial()
        {
            if (particleRenderer == null) return;
            
            // Создаем материал с нашим шейдером
            Shader irShader = Shader.Find("Custom/IRVisibleParticle");
            if (irShader != null)
            {
                particleMaterial = new Material(irShader);
                particleRenderer.material = particleMaterial;
                
                // Устанавливаем параметры
                particleMaterial.SetColor("_IRColor", irGlowColor);
                particleMaterial.SetFloat("_IRIntensity", irIntensity);
                particleMaterial.SetFloat("_NormalVisibility", normalVisibility);
            }
            else
            {
                Debug.LogWarning("IRVisibleParticles: Shader 'Custom/IRVisibleParticle' not found!");
            }
        }
        
        /// <summary>
        /// Устанавливает видимость частиц в зависимости от режима
        /// </summary>
        public void SetVisibility(bool irActive)
        {
            isIRActive = irActive;
            
            if (particleMaterial != null)
            {
                // Обновляем видимость в материале
                particleMaterial.SetFloat("_NormalVisibility", irActive ? 1f : normalVisibility);
            }
            
            // Управляем воспроизведением частиц
            if (particles != null)
            {
                if (irActive && autoPlayOnIRActive && !particles.isPlaying)
                {
                    particles.Play();
                }
                else if (!irActive && autoStopOnIRInactive && particles.isPlaying)
                {
                    particles.Stop();
                }
            }
        }
        
        /// <summary>
        /// Обновляет цвет и интенсивность ИК-свечения
        /// </summary>
        public void UpdateIRAppearance(Color color, float intensity)
        {
            irGlowColor = color;
            irIntensity = intensity;
            
            if (particleMaterial != null)
            {
                particleMaterial.SetColor("_IRColor", color);
                particleMaterial.SetFloat("_IRIntensity", intensity);
            }
        }
        
        void OnDestroy()
        {
            if (particleMaterial != null)
            {
                Destroy(particleMaterial);
            }
        }
        
        // Для дебага в редакторе
        void OnValidate()
        {
            if (Application.isPlaying && particleMaterial != null)
            {
                particleMaterial.SetColor("_IRColor", irGlowColor);
                particleMaterial.SetFloat("_IRIntensity", irIntensity);
                particleMaterial.SetFloat("_NormalVisibility", normalVisibility);
            }
        }
    }
}