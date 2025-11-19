using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG
{
    public class PatchableHole : MonoBehaviour
    {
        [Header("Repair Progress")]
        public float RepairProgress = 0f; // 0–100%
        public bool IsPatched => RepairProgress >= 100f;

        [Header("Visual Effects")]
        public ParticleSystem RepairEffect; // Визуализация заварки
        public Renderer HoleRenderer;
        public Color DamagedColor = Color.red;
        public Color PatchedColor = Color.gray;

        [Header("IR Leak Particles")]
        [Tooltip("Система частиц для визуализации утечки воздуха (видна только в ИК)")]
        public ParticleSystem LeakParticles;
        
        [Tooltip("Цвет утечки в ИК-режиме")]
        public Color leakIRColor = new Color(1f, 0.3f, 0f, 1f); // Оранжевый для теплого воздуха
        
        [Tooltip("Интенсивность свечения утечки")]
        [Range(0f, 5f)]
        public float leakIntensity = 3f;
        
        [Tooltip("Скорость эмиссии частиц утечки")]
        public float leakEmissionRate = 100f;

        private IRVisibleParticles leakIRComponent;

        private void Start()
        {
            // Настройка визуала дыры
            if (HoleRenderer != null)
            {
                HoleRenderer.material.color = DamagedColor;
            }

            // Настройка ИК-частиц утечки
            SetupLeakParticles();
        }

        private void SetupLeakParticles()
        {
            if (LeakParticles == null)
            {
                // Создаем систему частиц автоматически
                GameObject leakGO = new GameObject("LeakParticles");
                leakGO.transform.SetParent(transform);
                leakGO.transform.localPosition = Vector3.zero;
                leakGO.transform.localRotation = Quaternion.identity;

                LeakParticles = leakGO.AddComponent<ParticleSystem>();
                ConfigureLeakParticles();
            }

            // Добавляем компонент для ИК-видимости
            leakIRComponent = LeakParticles.gameObject.GetComponent<IRVisibleParticles>();
            if (leakIRComponent == null)
            {
                leakIRComponent = LeakParticles.gameObject.AddComponent<IRVisibleParticles>();
            }

            // Настраиваем ИК-параметры
            leakIRComponent.irGlowColor = leakIRColor;
            leakIRComponent.irIntensity = leakIntensity;
            leakIRComponent.normalVisibility = 0f; // Полностью невидимо в обычном режиме
            leakIRComponent.autoPlayOnIRActive = false; // Управляем вручную
            leakIRComponent.autoStopOnIRInactive = false;

            // Запускаем частицы утечки
            if (!IsPatched)
            {
                LeakParticles.Play();
            }
        }

        private void ConfigureLeakParticles()
        {
            var main = LeakParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1.5f;
            main.startSize = 0.1f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1000;

            var emission = LeakParticles.emission;
            emission.rateOverTime = leakEmissionRate;

            var shape = LeakParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;

            // Добавляем турбулентность для имитации потока воздуха
            var noise = LeakParticles.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;

            var renderer = LeakParticles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        // Метод увеличения прогресса
        public void ApplyRepair(float amountPerSecond)
        {
            if (IsPatched) return;

            RepairProgress += amountPerSecond * Time.deltaTime;
            RepairProgress = Mathf.Clamp(RepairProgress, 0f, 100f);

            // Визуализация процесса заварки
            if (RepairEffect != null && !RepairEffect.isPlaying)
            {
                RepairEffect.Play();
            }

            // Обновляем цвет по прогрессу
            if (HoleRenderer != null)
            {
                HoleRenderer.material.color = Color.Lerp(DamagedColor, PatchedColor, RepairProgress / 100f);
            }

            // Уменьшаем интенсивность утечки по мере ремонта
            if (LeakParticles != null)
            {
                var emission = LeakParticles.emission;
                emission.rateOverTime = leakEmissionRate * (1f - RepairProgress / 100f);
            }

            // Если заварена
            if (IsPatched)
            {
                if (RepairEffect != null) RepairEffect.Stop();
                OnPatched();
            }
        }

        private void OnPatched()
        {
            Debug.Log($"{name} patched!");

            // Останавливаем утечку
            if (LeakParticles != null)
            {
                LeakParticles.Stop();
            }

            GetComponent<Collider>().enabled = false;
            
            // Уничтожаем через небольшую задержку, чтобы частицы успели исчезнуть
            Destroy(gameObject, 2f);
        }

        // Публичные методы для управления утечкой
        public void SetLeakIntensity(float intensity)
        {
            leakIntensity = intensity;
            if (leakIRComponent != null)
            {
                leakIRComponent.UpdateIRAppearance(leakIRColor, intensity);
            }
        }

        public void SetLeakColor(Color color)
        {
            leakIRColor = color;
            if (leakIRComponent != null)
            {
                leakIRComponent.UpdateIRAppearance(color, leakIntensity);
            }
        }

        // Для визуализации в редакторе
        private void OnDrawGizmos()
        {
            if (!IsPatched)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.2f);
                
                // Рисуем направление утечки
                Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
            }
        }
    }
}