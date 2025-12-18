using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityButton = UnityEngine.UI.Button;
using UnityImage = UnityEngine.UI.Image;
using UnityOutline = UnityEngine.UI.Outline;

namespace BNG
{
    /// <summary>
    /// Экран победы - создаётся процедурно при выигрыше
    /// </summary>
    public class VictoryUI_Procedural : MonoBehaviour
    {
        [Header("=== НАСТРОЙКИ СЦЕН ===")]
        public string gameSceneName = "GameScene";
        public string mainMenuSceneName = "MainMenu";
        
        [Header("=== ПОЗИЦИОНИРОВАНИЕ ===")]
        public Transform playerCamera;
        public Vector3 menuOffset = new Vector3(0, 0, 3f);
        public float menuScale = 0.002f;
        public bool followCamera = true;
        
        [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
        public bool useParticles = true;
        public bool usePulseEffect = true;
        public Color glowColor = new Color(0.2f, 1f, 0.2f); // Зелёное свечение
        
        [Header("=== АНИМАЦИЯ ===")]
        public float fadeInDuration = 1.5f;
        public float buttonActivationDelay = 2f;
        
        [Header("=== ЗВУКИ ===")]
        public AudioClip victorySound;
        public AudioClip buttonClickSound;
        public AudioClip ambienceSound;
        
        [Header("=== VR ЭФФЕКТЫ ===")]
        [Range(0f, 1f)]
        public float vibrationIntensity = 0.3f;
        
        [Header("=== ССЫЛКИ ===")]
        public GameOverManager gameOverManager;
        public DayEventManager dayEventManager;
        
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private GameObject backgroundPanel;
        private UnityImage backgroundImage;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI messageText;
        private TextMeshProUGUI statsText;
        private UnityButton restartButton;
        private UnityButton mainMenuButton;
        private AudioSource audioSource;
        private AudioSource ambienceSource;
        private GameObject particleEffect;
        private GameObject glowEffect;
        
        private bool isShowing = false;
        private bool buttonsActive = false;
        
        void Awake()
        {
            if (gameOverManager == null)
                gameOverManager = FindObjectOfType<GameOverManager>();
            
            if (dayEventManager == null)
                dayEventManager = FindObjectOfType<DayEventManager>();
            
            if (playerCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                    playerCamera = cam.transform;
            }
            
            CreateVictoryUI();
        }
        
        void Start()
        {
            if (canvas != null)
                canvas.enabled = false;
            
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            
            if (dayEventManager != null)
            {
                dayEventManager.OnGameWon.AddListener(ShowVictoryScreen);
            }
        }
        
        void CreateVictoryUI()
        {
            // 1. CANVAS
            GameObject canvasObj = new GameObject("VictoryCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.zero;
            
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            
            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920, 1080);
            canvasRect.localScale = new Vector3(menuScale, menuScale, menuScale);
            
            // 2. ЭФФЕКТ СВЕЧЕНИЯ
            CreateGlowEffect(canvasObj.transform);
            
            // 3. ФОНОВАЯ ПАНЕЛЬ
            backgroundPanel = CreatePanel(canvasObj.transform, "Background", new Color(0, 0.1f, 0, 0.9f));
            backgroundImage = backgroundPanel.GetComponent<UnityImage>();
            RectTransform bgRect = backgroundPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // 4. ГРАНИЦА
            CreateBorder(backgroundPanel.transform);
            
            // 5. ИКОНКА ПОБЕДЫ
            CreateVictoryIcon(backgroundPanel.transform);
            
            // 6. ЗАГОЛОВОК
            CreateTitleWithShadow(backgroundPanel.transform);
            
            // 7. СООБЩЕНИЕ ПОБЕДЫ
            messageText = CreateText(backgroundPanel.transform, "MessageText", 
                "🎉 ПОЗДРАВЛЯЕМ! 🎉\nВы успешно продержались 7 дней!", 
                55, new Color(1f, 1f, 0.8f));
            SetRectTransform(messageText.GetComponent<RectTransform>(), 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                Vector2.zero, new Vector2(0, -320), new Vector2(1400, 150));
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.lineSpacing = 10;
            
            // 8. РАЗДЕЛИТЕЛЬ
            CreateDivider(backgroundPanel.transform, -420);
            
            // 9. СТАТИСТИКА
            statsText = CreateText(backgroundPanel.transform, "StatsText", "", 50, new Color(0.9f, 0.9f, 0.9f));
            SetRectTransform(statsText.GetComponent<RectTransform>(), 
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                Vector2.zero, new Vector2(0, -50), new Vector2(1200, 300));
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.lineSpacing = 15;
            
            // 10. РАЗДЕЛИТЕЛЬ
            CreateDivider(backgroundPanel.transform, 130);
            
            // 11. КНОПКИ
            restartButton = CreateFancyButton(backgroundPanel.transform, "RestartButton", 
                "НОВАЯ ИГРА\n<size=50%>(Начать заново)</size>", 
                new Color(0.2f, 0.8f, 0.3f),
                new Vector2(700, 160), new Vector2(0, 280));
            restartButton.onClick.AddListener(OnRestartClicked);
            restartButton.interactable = false;
            
            mainMenuButton = CreateFancyButton(backgroundPanel.transform, "MainMenuButton", 
                "ГЛАВНОЕ МЕНЮ", 
                new Color(0.3f, 0.6f, 0.8f),
                new Vector2(700, 160), new Vector2(0, 100));
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            mainMenuButton.interactable = false;
            
            // 12. ПОДСКАЗКА
            CreateVRHint(backgroundPanel.transform);
            
            // 13. ЧАСТИЦЫ
            if (useParticles)
            {
                CreateParticleEffect(canvasObj.transform);
            }
            
            // 14. АУДИО
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.loop = true;
            ambienceSource.volume = 0.3f;
            
            Debug.Log("✅ Victory UI создан!");
        }
        
        void CreateGlowEffect(Transform parent)
        {
            glowEffect = new GameObject("GlowEffect");
            glowEffect.transform.SetParent(parent, false);
            
            RectTransform rect = glowEffect.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(2200, 1300);
            
            UnityImage glow = glowEffect.AddComponent<UnityImage>();
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.15f);
        }
        
        void CreateBorder(Transform parent)
        {
            GameObject border = new GameObject("Border");
            border.transform.SetParent(parent, false);
            
            RectTransform rect = border.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(-40, -40);
            
            UnityOutline outline = border.AddComponent<UnityOutline>();
            outline.effectColor = glowColor;
            outline.effectDistance = new Vector2(4, -4);
            
            UnityImage img = border.AddComponent<UnityImage>();
            img.color = Color.clear;
        }
        
        void CreateVictoryIcon(Transform parent)
        {
            GameObject icon = new GameObject("VictoryIcon");
            icon.transform.SetParent(parent, false);
            
            RectTransform rect = icon.AddComponent<RectTransform>();
            SetRectTransform(rect, 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0, -80), new Vector2(100, 100));
            
            TextMeshProUGUI iconText = icon.AddComponent<TextMeshProUGUI>();
            iconText.text = "🏆";
            iconText.fontSize = 80;
            iconText.alignment = TextAlignmentOptions.Center;
        }
        
        void CreateTitleWithShadow(Transform parent)
        {
            // Тень
            GameObject shadow = new GameObject("TitleShadow");
            shadow.transform.SetParent(parent, false);
            
            TextMeshProUGUI shadowText = shadow.AddComponent<TextMeshProUGUI>();
            shadowText.text = "ПОБЕДА!";
            shadowText.fontSize = 120;
            shadowText.color = new Color(0, 0, 0, 0.5f);
            shadowText.alignment = TextAlignmentOptions.Center;
            shadowText.fontStyle = FontStyles.Bold;
            
            SetRectTransform(shadowText.GetComponent<RectTransform>(), 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                Vector2.zero, new Vector2(5, -175), new Vector2(1600, 200));
            
            // Основной текст
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(parent, false);
            
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "ПОБЕДА!";
            titleText.fontSize = 120;
            titleText.color = new Color(0.2f, 1f, 0.2f); // Яркий зелёный
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            
            UnityOutline outline = titleObj.AddComponent<UnityOutline>();
            outline.effectColor = new Color(0, 0.5f, 0);
            outline.effectDistance = new Vector2(3, -3);
            
            SetRectTransform(titleText.GetComponent<RectTransform>(), 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                Vector2.zero, new Vector2(0, -180), new Vector2(1600, 200));
        }
        
        void CreateDivider(Transform parent, float yPos)
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            
            RectTransform rect = divider.AddComponent<RectTransform>();
            SetRectTransform(rect, 
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(0, yPos), new Vector2(1200, 3));
            
            UnityImage img = divider.AddComponent<UnityImage>();
            img.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        }
        
        UnityButton CreateFancyButton(Transform parent, string name, string text, Color buttonColor, Vector2 size, Vector2 position)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            SetRectTransform(buttonRect, 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 
                Vector2.zero, position, size);
            
            UnityImage buttonImage = buttonObj.AddComponent<UnityImage>();
            buttonImage.color = buttonColor;
            
            UnityOutline outline = buttonObj.AddComponent<UnityOutline>();
            outline.effectColor = buttonColor * 0.7f;
            outline.effectDistance = new Vector2(2, -2);
            
            UnityButton button = buttonObj.AddComponent<UnityButton>();
            
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonColor * 1.3f;
            colors.pressedColor = buttonColor * 0.7f;
            colors.selectedColor = buttonColor;
            colors.disabledColor = buttonColor * 0.4f;
            button.colors = colors;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 55;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.enableWordWrapping = true;
            buttonText.fontStyle = FontStyles.Bold;
            
            return button;
        }
        
        void CreateVRHint(Transform parent)
        {
            GameObject hint = new GameObject("VRHint");
            hint.transform.SetParent(parent, false);
            
            TextMeshProUGUI hintText = hint.AddComponent<TextMeshProUGUI>();
            hintText.text = "<size=40%><color=#888888>Наведите контроллер и нажмите триггер</color></size>";
            hintText.fontSize = 35;
            hintText.alignment = TextAlignmentOptions.Center;
            
            SetRectTransform(hintText.GetComponent<RectTransform>(), 
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0, 20), new Vector2(1400, 50));
        }
        
        void CreateParticleEffect(Transform parent)
        {
            particleEffect = new GameObject("ParticleSystem");
            particleEffect.transform.SetParent(parent, false);
            particleEffect.transform.localPosition = Vector3.zero;
            
            ParticleSystem ps = particleEffect.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 4f;
            main.startSpeed = 0.8f;
            main.startSize = 0.08f;
            main.startColor = new Color(0.2f, 1f, 0.2f, 0.7f);
            main.maxParticles = 100;
            
            var emission = ps.emission;
            emission.rateOverTime = 20f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.5f, 1.5f, 0.1f);
            
            particleEffect.SetActive(false);
        }
        
        GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            UnityImage image = panel.AddComponent<UnityImage>();
            image.color = color;
            
            return panel;
        }
        
        TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return tmp;
        }
        
        void SetRectTransform(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, 
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
        
        void Update()
        {
            if (isShowing && canvas != null && playerCamera != null && followCamera)
            {
                Vector3 targetPosition = playerCamera.position + playerCamera.forward * menuOffset.z 
                    + playerCamera.up * menuOffset.y 
                    + playerCamera.right * menuOffset.x;
                
                canvas.transform.position = Vector3.Lerp(canvas.transform.position, targetPosition, Time.unscaledDeltaTime * 2f);
                canvas.transform.rotation = Quaternion.Slerp(canvas.transform.rotation, 
                    Quaternion.LookRotation(canvas.transform.position - playerCamera.position), 
                    Time.unscaledDeltaTime * 3f);
            }
        }
        
        public void ShowVictoryScreen()
        {
            if (isShowing) return;
            isShowing = true;
            
            StartCoroutine(VictorySequence());
        }
        
        IEnumerator VictorySequence()
        {
            // Позиция
            if (canvas != null && playerCamera != null)
            {
                Vector3 targetPosition = playerCamera.position + playerCamera.forward * menuOffset.z 
                    + playerCamera.up * menuOffset.y;
                canvas.transform.position = targetPosition;
                canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - playerCamera.position);
            }
            
            if (canvas != null)
                canvas.enabled = true;
            
            // Эффекты
            VibrateControllers(vibrationIntensity, 1f);
            
            if (useParticles && particleEffect != null)
            {
                particleEffect.SetActive(true);
            }
            
            // Звуки
            if (audioSource != null && victorySound != null)
                audioSource.PlayOneShot(victorySound);
            
            if (ambienceSource != null && ambienceSound != null)
            {
                ambienceSource.clip = ambienceSound;
                ambienceSource.Play();
            }
            
            // Статистика
            if (statsText != null)
                statsText.text = GetVictoryStats();
            
            // Пульсация
            if (usePulseEffect && backgroundImage != null)
            {
                StartCoroutine(PulseBackground());
            }
            
            // Fade in
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }
            
            // Анимация заголовка
            if (titleText != null)
            {
                StartCoroutine(AnimateTitle());
            }
            
            yield return new WaitForSecondsRealtime(buttonActivationDelay);
            
            // Активация кнопок
            buttonsActive = true;
            if (restartButton != null)
            {
                restartButton.interactable = true;
                StartCoroutine(AnimateButton(restartButton.transform, 0f));
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.interactable = true;
                StartCoroutine(AnimateButton(mainMenuButton.transform, 0.2f));
            }
        }
        
        IEnumerator PulseBackground()
        {
            Color originalColor = backgroundImage.color;
            float elapsed = 0f;
            
            while (isShowing)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0.85f, 0.95f, (Mathf.Sin(elapsed * 2f) + 1f) / 2f);
                backgroundImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }
        
        IEnumerator AnimateTitle()
        {
            Vector3 originalScale = titleText.transform.localScale;
            float duration = 1.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0.3f, 1.2f, EaseOutElastic(t));
                titleText.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            // Финальная нормализация
            elapsed = 0f;
            duration = 0.3f;
            Vector3 currentScale = titleText.transform.localScale;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                titleText.transform.localScale = Vector3.Lerp(currentScale, originalScale, t);
                yield return null;
            }
            
            titleText.transform.localScale = originalScale;
        }
        
        IEnumerator AnimateButton(Transform buttonTransform, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            
            Vector3 originalScale = buttonTransform.localScale;
            buttonTransform.localScale = Vector3.zero;
            
            float elapsed = 0f;
            float duration = 0.4f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0f, 1f, EaseOutBack(t));
                buttonTransform.localScale = originalScale * scale;
                yield return null;
            }
            
            buttonTransform.localScale = originalScale;
        }
        
        float EaseOutElastic(float t)
        {
            float p = 0.3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
        }
        
        float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        
        string GetVictoryStats()
        {
            if (gameOverManager == null || dayEventManager == null) 
                return "Миссия завершена успешно!";
            
            return $"<color=#FFD700>🎖️ МИССИЯ ЗАВЕРШЕНА 🎖️</color>\n\n" +
                   $"<color=#66FF66>Продержались:</color> {dayEventManager.maxDays} дней\n" +
                   $"<color=#66CCFF>Герметичность:</color> {gameOverManager.hullIntegrity:F0}%\n" +
                   $"<color=#FF66FF>Здоровье:</color> {gameOverManager.playerHealth:F0}/{gameOverManager.maxPlayerHealth:F0}\n" +
                   $"<color=#FFAA66>Кислород:</color> {gameOverManager.oxygenLevel:F0}%\n\n" +
                   $"<size=80%><color=#AAAAAA>Поздравляем с успешным выживанием!</color></size>";
        }
        
        void VibrateControllers(float intensity, float duration)
        {
            #if UNITY_STANDALONE || UNITY_EDITOR
            try
            {
                if (InputBridge.Instance != null)
                {
                    InputBridge.Instance.VibrateController(intensity, duration, 0.1f, ControllerHand.Left);
                    InputBridge.Instance.VibrateController(intensity, duration, 0.1f, ControllerHand.Right);
                }
            }
            catch { }
            #endif
        }
        
        void OnRestartClicked()
        {
            Debug.Log("Перезапуск игры...");
            
            if (audioSource != null && buttonClickSound != null)
                audioSource.PlayOneShot(buttonClickSound);
            
            VibrateControllers(0.3f, 0.2f);
            
            if (restartButton != null)
                restartButton.interactable = false;
            if (mainMenuButton != null)
                mainMenuButton.interactable = false;
            
            buttonsActive = false;
            
            if (ambienceSource != null)
                ambienceSource.Stop();
            
            StartCoroutine(LoadSceneWithFade(gameSceneName));
        }
        
        void OnMainMenuClicked()
        {
            Debug.Log("Главное меню...");
            
            if (audioSource != null && buttonClickSound != null)
                audioSource.PlayOneShot(buttonClickSound);
            
            VibrateControllers(0.3f, 0.2f);
            
            if (restartButton != null)
                restartButton.interactable = false;
            if (mainMenuButton != null)
                mainMenuButton.interactable = false;
            
            buttonsActive = false;
            
            if (ambienceSource != null)
                ambienceSource.Stop();
            
            StartCoroutine(LoadSceneWithFade(mainMenuSceneName));
        }
        
        IEnumerator LoadSceneWithFade(string sceneName)
        {
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                    yield return null;
                }
            }
            
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
        
        void OnDestroy()
        {
            Time.timeScale = 1f;
            
            if (dayEventManager != null)
                dayEventManager.OnGameWon.RemoveListener(ShowVictoryScreen);
        }
    }
}