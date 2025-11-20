using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace VRTooltips
{
    /// <summary>
    /// Красивая система tooltip'ов для VR с BNG Framework
    /// Автоматически поворачивается к камере и плавно появляется/исчезает
    /// </summary>
    public class VRTooltip : MonoBehaviour
    {
        [Header("Tooltip Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        
        [Header("Settings")]
        [SerializeField] private float showDistance = 3f;
        [SerializeField] private float hideDistance = 5f;
        [SerializeField] private float fadeSpeed = 5f;
        [SerializeField] private bool alwaysFaceCamera = true;
        [SerializeField] private Vector3 offset = new Vector3(0, 0.5f, 0);
        
        [Header("Animation")]
        [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private bool scaleOnShow = true;
        [SerializeField] private bool floatAnimation = true;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float floatAmount = 0.1f;
        
        [Header("Style")]
        [SerializeField] private Color accentColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Gradient backgroundGradient;
        
        private Transform playerCamera;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private bool isVisible = false;
        private Coroutine currentAnimation;
        private float floatOffset;

        void Start()
        {
            Initialize();
        }

        void Initialize()
{
    // Найти камеру игрока
    if (Camera.main != null)
        playerCamera = Camera.main.transform;
    
    // Получить или добавить компоненты
    canvasGroup = GetComponent<CanvasGroup>();
    if (canvasGroup == null)
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    
    rectTransform = GetComponent<RectTransform>();
    
    // Сохранить оригинальные значения
    originalScale = new Vector3(0.1f, 0.1f, 0.1f);
    originalPosition = transform.localPosition;
    
    // Начать скрытым
    canvasGroup.alpha = 0;
    if (scaleOnShow)
        transform.localScale = Vector3.zero;
    
    // Применить акцентный цвет
    if (iconImage != null)
        iconImage.color = accentColor;
    
    // ВАЖНО: Найти UI компоненты если не назначены
    if (titleText == null || descriptionText == null)
    {
        FindUIComponents();
    }
    
    // Установить placeholder текст если пустой
    if (titleText != null && string.IsNullOrEmpty(titleText.text))
    {
        titleText.text = "Title";
    }
    
    if (descriptionText != null && string.IsNullOrEmpty(descriptionText.text))
    {
        descriptionText.text = "Description";
    }
}
        void Update()
        {
            if (playerCamera == null) return;
            
            // Поворот к камере
            if (alwaysFaceCamera)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - playerCamera.position);
            }
            
            // Проверка дистанции
            float distance = Vector3.Distance(playerCamera.position, transform.position);
            
            if (distance < showDistance && !isVisible)
            {
                Show();
            }
            else if (distance > hideDistance && isVisible)
            {
                Hide();
            }
            
            // Плавающая анимация
            if (floatAnimation && isVisible)
            {
                floatOffset += Time.deltaTime * floatSpeed;
                Vector3 floatPos = originalPosition + new Vector3(0, Mathf.Sin(floatOffset) * floatAmount, 0);
                transform.localPosition = floatPos;
            }
        }

        public void Show()
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            
            currentAnimation = StartCoroutine(AnimateShow());
        }

        public void Hide()
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            
            currentAnimation = StartCoroutine(AnimateHide());
        }

        IEnumerator AnimateShow()
        {
            isVisible = true;
            float elapsed = 0;
            
            Vector3 startScale = scaleOnShow ? Vector3.zero : originalScale;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < animationDuration)
            {
                Debug.LogWarning(originalScale);
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float curveValue = showCurve.Evaluate(t);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, curveValue);
                
                if (scaleOnShow)
                {
                    transform.localScale = Vector3.Lerp(startScale, originalScale, curveValue);
                }
                
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
            transform.localScale = originalScale;
        }

        IEnumerator AnimateHide()
        {
            isVisible = false;
            float elapsed = 0;
            
            Vector3 startScale = transform.localScale;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float curveValue = showCurve.Evaluate(1 - t);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, 1 - curveValue);
                
                if (scaleOnShow)
                {
                    transform.localScale = Vector3.Lerp(startScale, Vector3.zero, 1 - curveValue);
                }
                
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            if (scaleOnShow)
                transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Установить содержимое tooltip'а
        /// </summary>
       public void SetContent(string title, string description, Sprite icon = null)
{
    Debug.Log($"SetContent called with title: {title}, description: {description}");
    
    if (titleText == null || descriptionText == null)
    {
        Debug.LogWarning("Text components are null, finding...");
        FindUIComponents();
    }
    
    if (titleText != null)
    {
        titleText.text = title;
        titleText.ForceMeshUpdate(); // ← Принудительно обновить!
        Debug.Log($"Title text set to: {titleText.text}, enabled: {titleText.enabled}, gameObject active: {titleText.gameObject.activeSelf}");
    }
    
    if (descriptionText != null)
    {
        descriptionText.text = description;
        descriptionText.ForceMeshUpdate(); // ← Принудительно обновить!
        Debug.Log($"Description text set to: {descriptionText.text}, enabled: {descriptionText.enabled}");
    }
    
    if (icon != null && iconImage != null)
    {
        iconImage.sprite = icon;
        iconImage.enabled = true;
    }
}
        
        /// <summary>
        /// Автоматически найти UI компоненты
        /// </summary>
        private void FindUIComponents()
        {
            // Найти все TextMeshPro компоненты
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            
            foreach (var tmp in allTexts)
            {
                if (titleText == null && (tmp.name.Contains("Title") || tmp.name == "TitleText"))
                {
                    titleText = tmp;
                    Debug.Log($"Found title: {tmp.name}");
                }
                else if (descriptionText == null && (tmp.name.Contains("Description") || tmp.name == "DescriptionText"))
                {
                    descriptionText = tmp;
                    Debug.Log($"Found description: {tmp.name}");
                }
            }
            
            // Найти иконку
            if (iconImage == null)
            {
                Image[] allImages = GetComponentsInChildren<Image>(true);
                foreach (var img in allImages)
                {
                    if (img.name == "Icon" || img.name == "IconImage")
                    {
                        iconImage = img;
                        Debug.Log($"Found icon: {img.name}");
                        break;
                    }
                }
            }
            
            // Найти фон
            if (backgroundImage == null)
            {
                Image[] allImages = GetComponentsInChildren<Image>(true);
                foreach (var img in allImages)
                {
                    if (img.name.Contains("Panel") || img.name.Contains("Background"))
                    {
                        backgroundImage = img;
                        Debug.Log($"Found background: {img.name}");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Изменить цветовую схему
        /// </summary>
        public void SetColor(Color color)
        {
            accentColor = color;
            if (iconImage != null)
                iconImage.color = color;
        }

        void OnDrawGizmosSelected()
        {
            // Визуализация зон показа/скрытия
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, showDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hideDistance);
        }
    }
}