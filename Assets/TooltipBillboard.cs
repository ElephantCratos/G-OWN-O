using UnityEngine;
using System.Collections.Generic;

namespace VRTooltips
{
    public class TooltipBillboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [SerializeField] private bool lockYAxis = true;
        
        [Header("Smart Positioning")]
        [SerializeField] private float orbitRadius = 0.5f;        // Радиус "орбиты" вокруг точки крепления
        [SerializeField] private int checkPoints = 8;              // Сколько точек проверять
        [SerializeField] private bool preferUpward = true;         // Предпочитать верхние позиции
        [SerializeField] private float upwardBias = 0.3f;          // Насколько "тянуть" вверх
        [SerializeField] private float repositionSpeed = 5f;       // Скорость перемещения
        [SerializeField] private LayerMask wallLayers;
        
        [Header("Visibility")]
        [SerializeField] private float minVisibleDot = 0.1f;       // Мин. угол видимости (0.1 = почти сбоку ок)
        
        private Transform cameraTransform;
        private Transform anchor;
        private Vector3 currentBestPosition;
        private CanvasGroup canvasGroup;
        
        void Start()
        {
            cameraTransform = Camera.main.transform;
            anchor = transform.parent;
            currentBestPosition = transform.position;
            
            canvasGroup = GetComponentInChildren<CanvasGroup>();
            if (canvasGroup == null)
            {
                var canvas = GetComponentInChildren<Canvas>();
                if (canvas != null)
                    canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        void LateUpdate()
        {
            if (cameraTransform == null || anchor == null) return;
            
            // Найти лучшую позицию
            Vector3 bestPos = FindBestPosition();
            
            // Плавно двигаться к ней
            currentBestPosition = Vector3.Lerp(currentBestPosition, bestPos, Time.deltaTime * repositionSpeed);
            transform.position = currentBestPosition;
            
            // Поворот к камере
            RotateToCamera();
            
            // Фейд если всё-таки не видно
            UpdateVisibility();
        }
        
        private Vector3 FindBestPosition()
        {
            Vector3 anchorPos = anchor.position;
            Vector3 toCamera = (cameraTransform.position - anchorPos).normalized;
            
            // Генерируем точки вокруг объекта
            List<(Vector3 pos, float score)> candidates = new List<(Vector3, float)>();
            
            for (int i = 0; i < checkPoints; i++)
            {
                float angle = (360f / checkPoints) * i;
                
                // Горизонтальное смещение
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * orbitRadius;
                
                // Добавляем вертикальное смещение
                if (preferUpward)
                    offset.y += upwardBias;
                
                Vector3 candidatePos = anchorPos + offset;
                float score = EvaluatePosition(candidatePos, anchorPos, toCamera);
                
                candidates.Add((candidatePos, score));
            }
            
            // Добавляем точки сверху и снизу
            Vector3 topPos = anchorPos + Vector3.up * orbitRadius;
            candidates.Add((topPos, EvaluatePosition(topPos, anchorPos, toCamera)));
            
            // Находим лучшую
            Vector3 best = transform.position;
            float bestScore = float.MinValue;
            
            foreach (var (pos, score) in candidates)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    best = pos;
                }
            }
            
            return best;
        }
        
        private float EvaluatePosition(Vector3 pos, Vector3 anchorPos, Vector3 toCamera)
        {
            float score = 0f;
            
            // 1. Проверка: не внутри ли стены?
            if (Physics.CheckSphere(pos, 0.05f, wallLayers, QueryTriggerInteraction.Ignore))
                return -1000f; // Сразу отбраковываем
            
            // 2. Проверка: видно ли из камеры?
            Vector3 camToPos = pos - cameraTransform.position;
            float distToCamera = camToPos.magnitude;
            
            if (Physics.Raycast(cameraTransform.position, camToPos.normalized, distToCamera - 0.1f, wallLayers, QueryTriggerInteraction.Ignore))
                return -500f; // За стеной — плохо
            
            // 3. Проверка: не перекрывает ли сам объект? (raycast от anchor к позиции)
            Vector3 anchorToPos = pos - anchorPos;
            if (Physics.Raycast(anchorPos, anchorToPos.normalized, anchorToPos.magnitude - 0.05f, wallLayers, QueryTriggerInteraction.Ignore))
                score -= 100f;
            
            // 4. Бонус: ближе к направлению на камеру — лучше
            float dotToCamera = Vector3.Dot((pos - anchorPos).normalized, toCamera);
            score += dotToCamera * 50f;
            
            // 5. Бонус: выше — лучше (если preferUpward)
            if (preferUpward)
                score += pos.y - anchorPos.y * 30f;
            
            // 6. Бонус: ближе к камере — чуть лучше (легче читать)
            score += (10f - distToCamera) * 2f;
            
            return score;
        }
        
        private void RotateToCamera()
        {
            Vector3 dir = cameraTransform.position - transform.position;
            if (lockYAxis) dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(-dir);
        }
        
        private void UpdateVisibility()
        {
            if (canvasGroup == null) return;
            
            // Проверяем финальную видимость
            Vector3 toCamera = cameraTransform.position - transform.position;
            bool visible = !Physics.Raycast(transform.position, toCamera.normalized, toCamera.magnitude, wallLayers, QueryTriggerInteraction.Ignore);
            
            float targetAlpha = visible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
        }
        
        void OnDrawGizmosSelected()
        {
            Transform a = Application.isPlaying ? anchor : transform.parent;
            if (a == null) return;
            
            // Орбита
            Gizmos.color = Color.cyan;
            for (int i = 0; i < 32; i++)
            {
                float angle1 = (360f / 32) * i;
                float angle2 = (360f / 32) * (i + 1);
                Vector3 p1 = a.position + Quaternion.Euler(0, angle1, 0) * Vector3.forward * orbitRadius;
                Vector3 p2 = a.position + Quaternion.Euler(0, angle2, 0) * Vector3.forward * orbitRadius;
                p1.y += upwardBias;
                p2.y += upwardBias;
                Gizmos.DrawLine(p1, p2);
            }
            
            // Текущая позиция
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentBestPosition, 0.1f);
                Gizmos.DrawLine(a.position, currentBestPosition);
            }
        }
    }
}
