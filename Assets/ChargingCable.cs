using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using BNG;
public class ChargingCable : MonoBehaviour
{
    [Header("References")]
    public RobotChargingSystem chargingSystem;
    public Transform socketTransform; // Розетка на роботе
    public Transform cablePlugTransform; // Штекер кабеля
    
    [Header("Settings")]
    public float connectDistance = 0.3f; // Расстояние для подключения
    public bool requireBothHandsNearBody = true; // Нужно ли держать руки у тела
    
    [Header("Visual Feedback")]
    public GameObject connectionIndicator; // Индикатор возможности подключения
    public Material cableMaterial;
    public Color normalColor = Color.white;
    public Color readyColor = Color.green;
    public Color connectedColor = Color.cyan;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip nearSocketSound;
    
    private Grabbable grabbable;
    private bool isPluggedIn = false;
    private bool nearSocket = false;
    private bool wasGrabbed = false;
    
  [Header("Flexible Cable")]
    public FlexibleCable flexibleCable; 

    void Start()
    {
    grabbable = GetComponent<Grabbable>();
    
    // Если используем гибкий кабель
    if (flexibleCable != null && cablePlugTransform == null)
    {
        cablePlugTransform = flexibleCable.endPoint;
    }
        
        UpdateCableColor(normalColor);
    }
    
    void Update()
    {
        if (isPluggedIn) return;
        
        // Отслеживаем захват/отпускание кабеля
        bool isGrabbed = grabbable != null && grabbable.BeingHeld;
        
        if (isGrabbed && !wasGrabbed)
        {
            OnCableGrabbed();
        }
        else if (!isGrabbed && wasGrabbed)
        {
            OnCableReleased();
        }
        
        wasGrabbed = isGrabbed;
        
        // Проверяем расстояние до розетки
        if (cablePlugTransform != null && socketTransform != null)
        {
            float distance = Vector3.Distance(cablePlugTransform.position, socketTransform.position);
            
            if (distance < connectDistance)
            {
                if (!nearSocket)
                {
                    nearSocket = true;
                    OnNearSocket();
                }
            }
            else
            {
                if (nearSocket)
                {
                    nearSocket = false;
                    OnAwayFromSocket();
                }
            }
        }
    }
    
    private void OnCableGrabbed()
    {
        UpdateCableColor(normalColor);
    }
    
    private void OnCableReleased()
    {
        // Проверяем, можем ли подключиться
        if (nearSocket && !isPluggedIn)
        {
            PlugIn();
        }
    }
    
    private void OnNearSocket()
    {
        if (connectionIndicator != null)
        {
            connectionIndicator.SetActive(true);
        }
        
        UpdateCableColor(readyColor);
        
        if (audioSource != null && nearSocketSound != null)
        {
            audioSource.PlayOneShot(nearSocketSound);
        }
        
        // Вибрация контроллера (если кабель в руке)
        if (grabbable != null && grabbable.BeingHeld)
        {
            // Определяем какой рукой держат
            ControllerHand hand = ControllerHand.None;
            
            if (grabbable.GetPrimaryGrabber() != null)
            {
                hand = grabbable.GetPrimaryGrabber().HandSide;
            }
            
            if (hand != ControllerHand.None)
            {
                InputBridge.Instance.VibrateController(0.3f, 0.1f, 0.1f, hand);
            }
        }
    }
    
    private void OnAwayFromSocket()
    {
        if (connectionIndicator != null)
        {
            connectionIndicator.SetActive(false);
        }
        
        UpdateCableColor(normalColor);
    }
    
    private void PlugIn()
    {
        isPluggedIn = true;
        
        // Фиксируем кабель в розетке
        if (grabbable != null)
        {
            grabbable.enabled = false;
        }
        
        if (cablePlugTransform != null && socketTransform != null)
        {
            cablePlugTransform.position = socketTransform.position;
            cablePlugTransform.rotation = socketTransform.rotation;
        }
        
        UpdateCableColor(connectedColor);
        
        if (connectionIndicator != null)
        {
            connectionIndicator.SetActive(false);
        }
        
        // Запускаем систему зарядки
        if (chargingSystem != null)
        {
            chargingSystem.TryStartCharging();
        }
    }
    
    private void UpdateCableColor(Color color)
    {
        if (cableMaterial != null)
        {
            cableMaterial.color = color;
        }
    }
}