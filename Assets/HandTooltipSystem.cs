using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BNG;

namespace VRTooltips
{
    public class HandTooltipSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform handAnchor;
        [SerializeField] private Transform headTransform;
        
        [Header("Positioning")]
        [SerializeField] private Vector3 tooltipOffset = new Vector3(0f, 0.15f, 0.1f);
        [SerializeField] private float smoothSpeed = 15f;
        
        [Header("Tooltip Style")]
        [SerializeField] private float tooltipWidth = 400f;
        [SerializeField] private float tooltipHeight = 200f;
        [SerializeField] private float worldScale = 0.0008f;
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color titleColor = new Color(0.3f, 0.8f, 1f, 1f);
        [SerializeField] private Color textColor = Color.white;
        
        [Header("Detection")]
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private LayerMask tooltipTargetLayer;
        
        [Header("Toggle Settings")]
        [SerializeField] private ControllerHand controllerHand = ControllerHand.Right;
        [Tooltip("Разрешить переключение кнопкой на контроллере")]
        [SerializeField] private bool allowControllerToggle = true;
        [Tooltip("Загружать настройку из PlayerPrefs при старте")]
        [SerializeField] private bool loadFromPlayerPrefs = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        private GameObject tooltipRoot;
        private GameObject canvasObj;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI descriptionText;
        
        private TooltipData currentTarget;
        private bool buttonWasPressed = false;
        private bool isShowing = false;
        
        private InputBridge input;
        
        private Vector3 canvasTargetScale;
        
        // === СТАТИЧЕСКИЙ ДОСТУП ===
        private static HandTooltipSystem instance;
        private static bool _tooltipsEnabled = true;
        
        private const string TOOLTIPS_PREF_KEY = "TooltipsEnabled";
        
        /// <summary>
        /// Глобальное состояние тултипов (для доступа из меню)
        /// </summary>
        public static bool TooltipsEnabled
        {
            get => _tooltipsEnabled;
            set
            {
                if (_tooltipsEnabled != value)
                {
                    _tooltipsEnabled = value;
                    PlayerPrefs.SetInt(TOOLTIPS_PREF_KEY, value ? 1 : 0);
                    PlayerPrefs.Save();
                    
                    // Уведомляем инстанс
                    if (instance != null)
                    {
                        instance.OnTooltipsStateChanged(value);
                    }
                    
                    Debug.Log($"[HandTooltip] Tooltips globally set to: {(value ? "ON" : "OFF")}");
                }
            }
        }
        
        /// <summary>
        /// Получить текущий инстанс системы
        /// </summary>
        public static HandTooltipSystem Instance => instance;

        void Awake()
        {
            // Singleton-подобное поведение (но без DontDestroyOnLoad)
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.LogWarning("[HandTooltip] Multiple instances detected! Using the first one.");
            }
        }

        void Start()
        {
            DebugLog("=== HandTooltipSystem START ===");
            
            input = InputBridge.Instance;
            
            if (headTransform == null)
                headTransform = Camera.main?.transform;
            
            if (handAnchor == null)
                DebugLog("ERROR: Hand Anchor not assigned!", true);
            
            canvasTargetScale = Vector3.one * worldScale;
            
            // Загружаем сохранённую настройку
            if (loadFromPlayerPrefs)
            {
                _tooltipsEnabled = PlayerPrefs.GetInt(TOOLTIPS_PREF_KEY, 1) == 1;
                DebugLog($"Loaded tooltips setting: {(_tooltipsEnabled ? "ON" : "OFF")}");
            }
            
            CreateTooltipFromScratch();
            
            DebugLog("=== START COMPLETE ===");
        }
        
        private void CreateTooltipFromScratch()
        {
            // === ROOT — делаем дочерним к руке! ===
            tooltipRoot = new GameObject("HandTooltip");
            tooltipRoot.transform.SetParent(handAnchor);
            tooltipRoot.transform.localPosition = tooltipOffset;
            tooltipRoot.transform.localRotation = Quaternion.identity;
            tooltipRoot.transform.localScale = Vector3.one;
            
            // === CANVAS ===
            canvasObj = new GameObject("TooltipCanvas");
            canvasObj.transform.SetParent(tooltipRoot.transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = canvasTargetScale;
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(tooltipWidth, tooltipHeight);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            
            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // === BACKGROUND ===
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localRotation = Quaternion.identity;
            bgObj.transform.localScale = Vector3.one;
            
            Image backgroundImage = bgObj.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;
            
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // === TITLE ===
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(canvasObj.transform);
            titleObj.transform.localPosition = Vector3.zero;
            titleObj.transform.localRotation = Quaternion.identity;
            titleObj.transform.localScale = Vector3.one;
            
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Title";
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = titleColor;
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.raycastTarget = false;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.55f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(20, 5);
            titleRect.offsetMax = new Vector2(-20, -15);
            
            // === DESCRIPTION ===
            GameObject descObj = new GameObject("DescriptionText");
            descObj.transform.SetParent(canvasObj.transform);
            descObj.transform.localPosition = Vector3.zero;
            descObj.transform.localRotation = Quaternion.identity;
            descObj.transform.localScale = Vector3.one;
            
            descriptionText = descObj.AddComponent<TextMeshProUGUI>();
            descriptionText.text = "Description";
            descriptionText.fontSize = 24;
            descriptionText.color = textColor;
            descriptionText.alignment = TextAlignmentOptions.TopLeft;
            descriptionText.raycastTarget = false;
            descriptionText.overflowMode = TextOverflowModes.Ellipsis;
            
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(1, 0.55f);
            descRect.offsetMin = new Vector2(20, 15);
            descRect.offsetMax = new Vector2(-20, -5);
            
            // === ACCENT LINE ===
            GameObject lineObj = new GameObject("AccentLine");
            lineObj.transform.SetParent(canvasObj.transform);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;
            lineObj.transform.localScale = Vector3.one;
            
            Image lineImage = lineObj.AddComponent<Image>();
            lineImage.color = titleColor;
            lineImage.raycastTarget = false;
            
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 0);
            lineRect.anchorMax = new Vector2(0.015f, 1);
            lineRect.offsetMin = Vector2.zero;
            lineRect.offsetMax = Vector2.zero;
            
            DebugLog("Tooltip created and attached to hand!");
        }

        void Update()
        {
            // Переключение кнопкой контроллера
            if (allowControllerToggle)
            {
                HandleToggleButton();
            }
            
            // Основная логика
            if (_tooltipsEnabled)
            {
                DetectTooltipTarget();
            }
            else
            {
                HideTooltip();
            }
            
            UpdateTooltipRotation();
            UpdateTooltipVisibility();
            ForceCanvasScale();
        }
        
        void LateUpdate()
        {
            ForceCanvasScale();
            UpdateTooltipRotation();
        }
        
        private void ForceCanvasScale()
        {
            if (canvasObj == null) return;
            
            if (canvasObj.transform.localScale != canvasTargetScale)
                canvasObj.transform.localScale = canvasTargetScale;
        }
        
        private void HandleToggleButton()
        {
            if (input == null) return;
            
            bool buttonPressed = controllerHand == ControllerHand.Right 
                ? input.AButton 
                : input.XButton;
            
            if (buttonPressed && !buttonWasPressed)
            {
                // Используем статическое свойство для синхронизации с меню
                TooltipsEnabled = !TooltipsEnabled;
            }
            
            buttonWasPressed = buttonPressed;
        }
        
        /// <summary>
        /// Вызывается при изменении состояния тултипов (из меню или кнопкой)
        /// </summary>
        private void OnTooltipsStateChanged(bool enabled)
        {
            DebugLog($"Tooltips state changed to: {(enabled ? "ON" : "OFF")}");
            
            if (!enabled)
            {
                HideTooltip();
            }
        }
        
        private void DetectTooltipTarget()
        {
            if (headTransform == null) return;
            
            Ray gazeRay = new Ray(headTransform.position, headTransform.forward);
            Debug.DrawRay(gazeRay.origin, gazeRay.direction * maxDistance, Color.red);
            
            if (Physics.Raycast(gazeRay, out RaycastHit hit, maxDistance, tooltipTargetLayer, QueryTriggerInteraction.Ignore))
            {
                TooltipData data = hit.collider.GetComponent<TooltipData>();
                
                if (data != null)
                {
                    if (data != currentTarget)
                    {
                        currentTarget = data;
                        SetTooltipContent(data);
                        DebugLog($"New target: {data.Title}");
                    }
                    isShowing = true;
                }
                else
                {
                    currentTarget = null;
                    isShowing = false;
                }
            }
            else
            {
                if (currentTarget != null)
                    DebugLog("Target lost");
                    
                currentTarget = null;
                isShowing = false;
            }
        }
        
        private void SetTooltipContent(TooltipData data)
        {
            if (titleText != null)
                titleText.text = data.Title;
            
            if (descriptionText != null)
                descriptionText.text = data.Description;
        }
        
        private void HideTooltip()
        {
            isShowing = false;
            currentTarget = null;
        }
        
        private void UpdateTooltipRotation()
        {
            if (tooltipRoot == null || headTransform == null) return;
            
            Vector3 dirToHead = headTransform.position - tooltipRoot.transform.position;
            dirToHead.y = 0;
            
            if (dirToHead.sqrMagnitude > 0.001f)
            {
                // Разворачиваем на 180° чтобы текст был лицом к игроку
                Quaternion lookRot = Quaternion.LookRotation(dirToHead);
                Quaternion flip = Quaternion.Euler(0, 180, 0);
                tooltipRoot.transform.rotation = lookRot * flip;
            }
        }
        
        private void UpdateTooltipVisibility()
        {
            if (canvasGroup == null) return;
            
            float targetAlpha = isShowing ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
        }
        
        private void DebugLog(string message, bool isError = false)
        {
            if (!showDebugLogs) return;
            
            if (isError)
                Debug.LogError($"[HandTooltip] {message}");
            else
                Debug.Log($"[HandTooltip] {message}");
        }
        
        #region Public API
        
        /// <summary>
        /// Включить тултипы
        /// </summary>
        public void EnableTooltips()
        {
            TooltipsEnabled = true;
        }
        
        /// <summary>
        /// Выключить тултипы
        /// </summary>
        public void DisableTooltips()
        {
            TooltipsEnabled = false;
        }
        
        /// <summary>
        /// Переключить состояние тултипов
        /// </summary>
        public void ToggleTooltips()
        {
            TooltipsEnabled = !TooltipsEnabled;
        }
        
        /// <summary>
        /// Проверить, включены ли тултипы
        /// </summary>
        public bool AreTooltipsEnabled()
        {
            return TooltipsEnabled;
        }
        
        #endregion
        
        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            
            if (tooltipRoot != null)
                Destroy(tooltipRoot);
        }
        
        void OnDrawGizmos()
        {
            Transform head = headTransform;
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
            
            if (head != null)
            {
                Gizmos.color = isShowing ? Color.green : Color.red;
                Gizmos.DrawRay(head.position, head.forward * maxDistance);
            }
            
            if (handAnchor != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 tooltipPos = handAnchor.TransformPoint(tooltipOffset);
                Gizmos.DrawWireSphere(tooltipPos, 0.03f);
                Gizmos.DrawLine(handAnchor.position, tooltipPos);
            }
        }
    }
}