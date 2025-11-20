using UnityEngine;
using BNG;

public class NightVisionController : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Будет найдена автоматически в CenterEyeAnchor")]
    private Camera playerCamera;
    
    [Header("Night Vision Settings")]
    [Tooltip("Материал для эффекта ночного видения/ИК-спектра")]
    public Material nightVisionMaterial;
    
    [Tooltip("Цвет тинта для ИК-режима")]
    public Color irTint = new Color(0f, 1f, 0.3f, 1f);
    
    [Tooltip("Интенсивность свечения")]
    [Range(0f, 2f)]
    public float glowIntensity = 1.2f;
    
    [Header("Scanline Settings")]
    [Tooltip("Количество статических линий сканирования")]
    [Range(100f, 1000f)]
    public float scanlineCount = 400f;
    
    [Tooltip("Интенсивность статических линий")]
    [Range(0f, 1f)]
    public float scanlineIntensity = 0.15f;
    
    [Tooltip("Скорость движущейся сканирующей линии")]
    [Range(0f, 5f)]
    public float scanSpeed = 2.0f;
    
    [Tooltip("Толщина движущейся линии")]
    [Range(0.001f, 0.01f)]
    public float scanlineThickness = 0.003f;
    
    [Tooltip("Яркость сканирующей линии")]
    [Range(0f, 2f)]
    public float scanlineBrightness = 1.5f;
    
    [Tooltip("Кнопка для активации")]
    public ControllerBinding activationButton;
    
    [Tooltip("Какой контроллер использовать (Left/Right)")]
    public ControllerHand controllerHand = ControllerHand.Right;
    
    [Header("Audio")]
    public AudioClip toggleOnSound;
    public AudioClip toggleOffSound;
    private AudioSource audioSource;
    
    [Header("Visual Feedback")]
    [Tooltip("Показывать индикатор состояния")]
    public bool showIndicator = true;
    
    private bool isActive = false;
    private float originalAmbientIntensity;
    private InputBridge input;
    private bool buttonPressed = false;
    
    // Глобальное свойство шейдера для управления видимостью ИК-частиц
    private static readonly int IRModeActive = Shader.PropertyToID("_IRModeActive");

    void Start()
    {
        // Находим камеру в структуре XR Rig
        FindPlayerCamera();
        
        if (playerCamera == null)
        {
            Debug.LogError("NightVision: Player camera not found in CenterEyeAnchor!");
            enabled = false;
            return;
        }
        
        // Получаем InputBridge
        input = InputBridge.Instance;
        
        // Настраиваем аудио
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        
        // Сохраняем оригинальные настройки
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        
        // Устанавливаем начальное состояние для ИК-частиц
        Shader.SetGlobalFloat(IRModeActive, 0f);
        
        // Создаем материал если не задан
        if (nightVisionMaterial == null)
        {
            CreateDefaultNightVisionMaterial();
        }
        
        Debug.Log("NightVision: Initialized successfully on camera: " + playerCamera.name);
    }

    void FindPlayerCamera()
    {
        // Ищем камеру в стандартной структуре BNG
        GameObject xrRig = GameObject.Find("XR Rig Advanced");
        if (xrRig == null)
        {
            xrRig = GameObject.Find("XR Rig");
        }
        
        if (xrRig != null)
        {
            // Путь: XR Rig/CameraRig/TrackingSpace/CenterEyeAnchor
            Transform cameraRig = xrRig.transform.Find("CameraRig");
            if (cameraRig != null)
            {
                Transform trackingSpace = cameraRig.Find("TrackingSpace");
                if (trackingSpace != null)
                {
                    Transform centerEye = trackingSpace.Find("CenterEyeAnchor");
                    if (centerEye != null)
                    {
                        playerCamera = centerEye.GetComponent<Camera>();
                    }
                }
            }
        }
        
        // Если не нашли - пробуем через Camera.main
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (input == null || playerCamera == null) return;
        
        // Проверяем нажатие кнопки (детектируем момент нажатия, а не удержание)
        bool currentButtonState = input.GetControllerBindingValue(activationButton);
        
        if (currentButtonState && !buttonPressed)
        {
            ToggleNightVision();
        }
        
        buttonPressed = currentButtonState;
        
        // Для тестирования в редакторе
        if (Input.GetKeyDown(KeyCode.N))
        {
            ToggleNightVision();
        }
    }

    public void ToggleNightVision()
    {
        isActive = !isActive;
        
        if (isActive)
        {
            EnableNightVision();
            if (toggleOnSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(toggleOnSound);
            }
        }
        else
        {
            DisableNightVision();
            if (toggleOffSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(toggleOffSound);
            }
        }
        
        Debug.Log("NightVision: " + (isActive ? "Enabled" : "Disabled"));
    }

    private void EnableNightVision()
    {
        // Проверяем что камера все еще существует
        if (playerCamera == null)
        {
            FindPlayerCamera();
            if (playerCamera == null) return;
        }
        
        // Применяем эффект к камере
        var imageEffect = playerCamera.gameObject.GetComponent<NightVisionEffect>();
        if (imageEffect == null)
        {
            imageEffect = playerCamera.gameObject.AddComponent<NightVisionEffect>();
        }
        
        imageEffect.enabled = true;
        imageEffect.nightVisionMaterial = nightVisionMaterial;
        imageEffect.irTint = irTint;
        imageEffect.glowIntensity = glowIntensity;
        imageEffect.scanlineCount = scanlineCount;
        imageEffect.scanlineIntensity = scanlineIntensity;
        imageEffect.scanSpeed = scanSpeed;
        imageEffect.scanlineThickness = scanlineThickness;
        imageEffect.scanlineBrightness = scanlineBrightness;
        
        // Усиливаем освещение
        RenderSettings.ambientIntensity = Mathf.Max(originalAmbientIntensity, 1.2f);
        
        // АКТИВИРУЕМ ВИДИМОСТЬ ИК-ЧАСТИЦ
        Shader.SetGlobalFloat(IRModeActive, 1f);
        
        // Обновляем все ИК-частицы в сцене
        UpdateIRParticlesVisibility(true);
    }

    private void DisableNightVision()
    {
        if (playerCamera == null) return;
        
        var imageEffect = playerCamera.gameObject.GetComponent<NightVisionEffect>();
        if (imageEffect != null)
        {
            imageEffect.enabled = false;
        }
        
        // Восстанавливаем освещение
        RenderSettings.ambientIntensity = originalAmbientIntensity;
        
        // ДЕАКТИВИРУЕМ ВИДИМОСТЬ ИК-ЧАСТИЦ
        Shader.SetGlobalFloat(IRModeActive, 0f);
        
        // Обновляем все ИК-частицы в сцене
        UpdateIRParticlesVisibility(false);
    }
    
    // Обновляет видимость всех ИК-частиц в сцене
    private void UpdateIRParticlesVisibility(bool visible)
    {
        Debug.Log($"[NightVision] UpdateIRParticlesVisibility({visible}) called");
        
        IRVisibleParticles[] allIRParticles = FindObjectsOfType<IRVisibleParticles>();
        Debug.Log($"[NightVision] Found {allIRParticles.Length} IRVisibleParticles in scene");
        
        foreach (var irParticle in allIRParticles)
        {
            Debug.Log($"[NightVision] Updating {irParticle.gameObject.name}");
            irParticle.SetVisibility(visible);
        }
        
        if (allIRParticles.Length == 0)
        {
            Debug.LogWarning("[NightVision] No IRVisibleParticles found in scene! Make sure IRVisibleParticles component is attached to your particle systems.");
        }
    }

    private void CreateDefaultNightVisionMaterial()
    {
        Shader shader = Shader.Find("Hidden/NightVision");
        if (shader == null)
        {
            // Если шейдера нет, создаем простой вариант
            shader = Shader.Find("Unlit/Color");
            Debug.LogWarning("NightVision shader not found, using Unlit/Color");
        }
        
        nightVisionMaterial = new Material(shader);
        nightVisionMaterial.SetColor("_IRTint", irTint);
        nightVisionMaterial.SetFloat("_Intensity", glowIntensity);
    }

    void OnDestroy()
    {
        // Восстанавливаем настройки
        RenderSettings.ambientIntensity = originalAmbientIntensity;
        
        // Деактивируем ИК-режим
        Shader.SetGlobalFloat(IRModeActive, 0f);
        
        // Убираем эффект с камеры
        if (playerCamera != null)
        {
            var imageEffect = playerCamera.gameObject.GetComponent<NightVisionEffect>();
            if (imageEffect != null)
            {
                Destroy(imageEffect);
            }
        }
    }
    
    // Публичные методы для вызова из других скриптов
    public bool IsActive => isActive;
    
    public void Enable()
    {
        if (!isActive)
        {
            ToggleNightVision();
        }
    }
    
    public void Disable()
    {
        if (isActive)
        {
            ToggleNightVision();
        }
    }
}

// Компонент для применения эффекта к камере
[RequireComponent(typeof(Camera))]
public class NightVisionEffect : MonoBehaviour
{
    [HideInInspector]
    public Material nightVisionMaterial;
    [HideInInspector]
    public Color irTint = new Color(0f, 1f, 0.3f, 1f);
    [HideInInspector]
    public float glowIntensity = 1.2f;
    [HideInInspector]
    public float scanlineCount = 400f;
    [HideInInspector]
    public float scanlineIntensity = 0.15f;
    [HideInInspector]
    public float scanSpeed = 2.0f;
    [HideInInspector]
    public float scanlineThickness = 0.003f;
    [HideInInspector]
    public float scanlineBrightness = 1.5f;
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (nightVisionMaterial != null && enabled)
        {
            // Передаем все параметры в шейдер
            nightVisionMaterial.SetColor("_IRTint", irTint);
            nightVisionMaterial.SetFloat("_Intensity", glowIntensity);
            nightVisionMaterial.SetFloat("_Noise", 0.1f);
            nightVisionMaterial.SetFloat("_Vignette", 0.3f);
            
            // Параметры сканирующих линий
            nightVisionMaterial.SetFloat("_ScanlineCount", scanlineCount);
            nightVisionMaterial.SetFloat("_ScanlineIntensity", scanlineIntensity);
            nightVisionMaterial.SetFloat("_ScanSpeed", scanSpeed);
            nightVisionMaterial.SetFloat("_ScanlineThickness", scanlineThickness);
            nightVisionMaterial.SetFloat("_ScanlineBrightness", scanlineBrightness);
            
            Graphics.Blit(source, destination, nightVisionMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}