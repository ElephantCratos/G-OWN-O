using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BNG;


public class FlexibleCable : MonoBehaviour
{
    [Header("Cable Points")]
    public Transform startPoint; // Точка начала (розетка/стена)
    public Transform endPoint;   // Точка конца (штекер, который берут в руку)
    
    [Header("Cable Settings")]
    [Range(5, 50)]
    public int segmentCount = 20; // Количество сегментов кабеля
    public float cableWidth = 0.02f; // Толщина кабеля
    public float segmentLength = 0.1f; // Длина одного сегмента
    
    [Header("Physics")]
    public float gravity = -9.81f;
    public float damping = 0.9f; // Затухание колебаний
    public float stiffness = 100f; // Жёсткость кабеля
    
    [Header("Visual")]
    public Material cableMaterial;
    public bool useTextureScroll = true;
    public float textureScrollSpeed = 0.5f;
    
    [Header("Advanced")]
    public bool useCollision = false;
    public LayerMask collisionMask;
    public float collisionRadius = 0.01f;
    
    private LineRenderer lineRenderer;
    private Vector3[] points;
    private Vector3[] velocities;
    private float textureOffset = 0f;
    
    void Start()
    {
        InitializeCable();
    }
    
    void InitializeCable()
    {
        // Создаём LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // Настройка LineRenderer
        lineRenderer.positionCount = segmentCount;
        lineRenderer.startWidth = cableWidth;
        lineRenderer.endWidth = cableWidth;
        lineRenderer.material = cableMaterial;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.alignment = LineAlignment.View;
        
        // Отключаем shadows для производительности
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        
        // Инициализация массивов точек
        points = new Vector3[segmentCount];
        velocities = new Vector3[segmentCount];
        
        // Расставляем точки по прямой линии от start до end
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            points[i] = Vector3.Lerp(startPoint.position, endPoint.position, t);
            velocities[i] = Vector3.zero;
        }
    }
    
    void Update()
    {
        if (points == null) return;
        
        SimulateCable();
        UpdateLineRenderer();
        
        if (useTextureScroll)
        {
            ScrollTexture();
        }
    }
    
    void SimulateCable()
    {
        float deltaTime = Time.deltaTime;
        
        // Фиксируем начальную и конечную точки
        points[0] = startPoint.position;
        points[segmentCount - 1] = endPoint.position;
        
        // Применяем гравитацию и ограничения к промежуточным точкам
        for (int i = 1; i < segmentCount - 1; i++)
        {
            // Гравитация
            velocities[i] += Vector3.up * gravity * deltaTime;
            
            // Применяем скорость
            points[i] += velocities[i] * deltaTime;
            
            // Затухание
            velocities[i] *= damping;
        }
        
        // Применяем ограничения расстояния между точками (Verlet integration)
        for (int iteration = 0; iteration < 5; iteration++) // Несколько итераций для стабильности
        {
            for (int i = 0; i < segmentCount - 1; i++)
            {
                Vector3 delta = points[i + 1] - points[i];
                float currentDistance = delta.magnitude;
                float difference = currentDistance - segmentLength;
                
                // Корректируем позиции
                Vector3 correction = delta.normalized * (difference * 0.5f);
                
                if (i > 0) // Не двигаем начальную точку
                {
                    points[i] += correction;
                }
                if (i < segmentCount - 2) // Не двигаем конечную точку
                {
                    points[i + 1] -= correction;
                }
            }
        }
        
        // Опциональная коллизия с миром
        if (useCollision)
        {
            for (int i = 1; i < segmentCount - 1; i++)
            {
                if (Physics.SphereCast(points[i], collisionRadius, Vector3.down, out RaycastHit hit, 0.1f, collisionMask))
                {
                    points[i] = hit.point + hit.normal * collisionRadius;
                    velocities[i] = Vector3.zero;
                }
            }
        }
    }
    
    void UpdateLineRenderer()
    {
        if (lineRenderer != null && points != null)
        {
            lineRenderer.SetPositions(points);
        }
    }
    
    void ScrollTexture()
    {
        if (lineRenderer != null && lineRenderer.material != null)
        {
            textureOffset += textureScrollSpeed * Time.deltaTime;
            lineRenderer.material.SetTextureOffset("_MainTex", new Vector2(textureOffset, 0));
        }
    }
    
    // Получить позицию штекера (для ChargingCable)
    public Vector3 GetPlugPosition()
    {
        return endPoint.position;
    }
    
    // Визуализация в редакторе
    void OnDrawGizmos()
    {
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.02f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, 0.02f);
        }
    }
}
