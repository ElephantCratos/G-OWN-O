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
        public float normalVisibility = 0.0f;
        
        [Header("Particle Settings")]
        [Tooltip("Автоматически играть при включении ИК-режима")]
        public bool autoPlayOnIRActive = true;
        
        [Tooltip("Автоматически останавливать при выключении ИК-режима")]
        public bool autoStopOnIRInactive = false;
        
        [Header("Alternative Method")]
        [Tooltip("Использовать простое отключение рендера вместо шейдера (рекомендуется если шейдер не работает)")]
        public bool useSimpleVisibility = true;
        
        private ParticleSystem particles;
        private ParticleSystemRenderer particleRenderer;
        private Material particleMaterial;
        private bool isIRActive = false;
        
        void Start()
        {
            particles = GetComponent<ParticleSystem>();
            particleRenderer = GetComponent<ParticleSystemRenderer>();
            
            Debug.Log($"[IRVisibleParticles] START on {gameObject.name}");
            Debug.Log($"[IRVisibleParticles] Particles: {particles != null}, Renderer: {particleRenderer != null}");
            
            // Создаем материал для частиц
            SetupIRMaterial();
            
            // Устанавливаем начальную видимость
            SetVisibility(false);
            
            // Если ИК-режим уже активен, обновляем
            float irMode = Shader.GetGlobalFloat("_IRModeActive");
            Debug.Log($"[IRVisibleParticles] Global IR Mode on start: {irMode}");
            if (irMode > 0.5f)
            {
                SetVisibility(true);
            }
        }
        
        void SetupIRMaterial()
        {
            if (particleRenderer == null) return;
            
            if (useSimpleVisibility)
            {
                // Простой метод - используем стандартный яркий материал
                Shader unlitShader = Shader.Find("Particles/Standard Unlit");
                if (unlitShader != null)
                {
                    Material simpleMat = new Material(unlitShader);
                    simpleMat.SetColor("_Color", irGlowColor);
                    simpleMat.EnableKeyword("_EMISSION");
                    simpleMat.SetColor("_EmissionColor", irGlowColor * irIntensity);
                    particleRenderer.material = simpleMat;
                    particleMaterial = simpleMat;
                    Debug.Log($"[IRVisibleParticles] Using simple Unlit material on {gameObject.name}");
                }
                
                // Начинаем с выключенным рендером
                particleRenderer.enabled = false;
                return;
            }
            
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
                
                Debug.Log($"[IRVisibleParticles] Material setup complete on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[IRVisibleParticles] Shader 'Custom/IRVisibleParticle' not found on {gameObject.name}! Using simple visibility.");
                useSimpleVisibility = true;
                SetupIRMaterial(); // Рекурсивно вызываем с simple visibility
            }
        }
        
        /// <summary>
        /// Устанавливает видимость частиц в зависимости от режима
        /// </summary>
        public void SetVisibility(bool irActive)
        {
            Debug.Log($"[IRVisibleParticles] SetVisibility({irActive}) called on {gameObject.name}");
            
            isIRActive = irActive;
            
            if (useSimpleVisibility)
            {
                // Простой метод - включаем/выключаем рендер
                if (particleRenderer != null)
                {
                    particleRenderer.enabled = irActive;
                    Debug.Log($"[IRVisibleParticles] Renderer.enabled = {irActive} on {gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[IRVisibleParticles] ParticleRenderer is NULL on {gameObject.name}!");
                }
            }
            else
            {
                // Метод через шейдер
                if (particleMaterial != null)
                {
                    // Обновляем видимость в материале
                    // 0 = невидимо, 1 = видимо в ИК-режиме
                    particleMaterial.SetFloat("_NormalVisibility", irActive ? 1f : 0f);
                    Debug.Log($"[IRVisibleParticles] Material _NormalVisibility = {(irActive ? 1f : 0f)} on {gameObject.name}");
                }
            }
            
            // Управляем воспроизведением частиц
            if (particles != null)
            {
                if (irActive && autoPlayOnIRActive)
                {
                    if (!particles.isPlaying)
                    {
                        particles.Play();
                        Debug.Log($"[IRVisibleParticles] Particles.Play() on {gameObject.name}");
                    }
                }
                else if (!irActive && autoStopOnIRInactive)
                {
                    if (particles.isPlaying)
                    {
                        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        Debug.Log($"[IRVisibleParticles] Particles.Stop() on {gameObject.name}");
                    }
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
            
            if (particleMaterial != null && !useSimpleVisibility)
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
            if (Application.isPlaying && particleMaterial != null && !useSimpleVisibility)
            {
                particleMaterial.SetColor("_IRColor", irGlowColor);
                particleMaterial.SetFloat("_IRIntensity", irIntensity);
                particleMaterial.SetFloat("_NormalVisibility", normalVisibility);
            }
        }
    }
}