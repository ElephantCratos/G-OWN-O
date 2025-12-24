using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;
using VRTooltips;
using BNG;

namespace VRMenu
{
    /// <summary>
    /// Игровое меню паузы для VR
    /// Интегрируется с HandTooltipSystem для управления тултипами
    /// Использует BNG InputBridge
    /// </summary>
    public class GamePauseMenu : MonoBehaviour
    {
        [Header("=== УПРАВЛЕНИЕ МЕНЮ ===")]
        [Tooltip("Какую кнопку использовать для открытия меню")]
        public MenuButtonType menuButtonType = MenuButtonType.Start;
        public KeyCode menuKey = KeyCode.Escape;
        public bool pauseGameOnOpen = true;
        
        public enum MenuButtonType
        {
            Start,      // Кнопка Start/Menu
            Back,       // Кнопка Back (Oculus)
            BButton,    // B на правом контроллере
            YButton     // Y на левом контроллере
        }
        
        private InputBridge input;
        
        [Header("=== ПОЗИЦИОНИРОВАНИЕ ===")]
        [SerializeField] private Transform menuSpawnPoint;
        [SerializeField] private float distanceFromCamera = 1.2f;
        [SerializeField] private float menuHeight = 1.3f;
        
        [Header("=== СЦЕНЫ ===")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        
        [Header("=== ЗВУКИ ===")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip openMenuSound;
        [SerializeField] private AudioClip closeMenuSound;
        
        [Header("=== СТИЛЬ ===")]
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        [SerializeField] private Color buttonColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        [SerializeField] private Color buttonHoverColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color accentColor = new Color(0.3f, 0.8f, 1f, 1f);
        [SerializeField] private Color dangerColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color toggleOnColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color toggleOffColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        [Header("=== СОБЫТИЯ ===")]
        public UnityEvent OnMenuOpened;
        public UnityEvent OnMenuClosed;
        public UnityEvent<float> OnVolumeChanged;
        
        // UI
        private GameObject menuRoot;
        private AudioSource audioSource;
        private GameObject pauseMenuPanel;
        private GameObject settingsPanel;
        private GameObject confirmExitPanel;
        
        private UnityEngine.UI.Slider volumeSlider;
        private UnityEngine.UI.Slider sfxVolumeSlider;
        private TextMeshProUGUI volumeValueText;
        private TextMeshProUGUI sfxVolumeValueText;
        
        private Toggle tooltipsToggle;
        private Image tooltipsToggleBg;
        private TextMeshProUGUI tooltipsStatusText;
        
        // Состояние
        private bool isMenuOpen = false;
        private float previousTimeScale = 1f;
        
        // Константы
        private const float MENU_WIDTH = 420f;
        private const float MENU_HEIGHT = 520f;
        private const float BTN_HEIGHT = 55f;
        private const float BTN_SPACING = 12f;
        private const float WORLD_SCALE = 0.003f;
        
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";

        void Start()
        {
            input = InputBridge.Instance;
            CreateMenu();
            LoadSettings();
            HideMenu();
        }
        
        void Update()
        {
            // Проверка кнопки через BNG InputBridge
            bool menuPressed = false;
            
            if (input != null)
            {
                switch (menuButtonType)
                {
                    case MenuButtonType.Start:
                        menuPressed = input.StartButtonDown;
                        break;
                    case MenuButtonType.Back:
                        menuPressed = input.BackButtonDown;
                        break;
                    case MenuButtonType.BButton:
                        menuPressed = input.BButtonDown;
                        break;
                    case MenuButtonType.YButton:
                        menuPressed = input.YButtonDown;
                        break;
                }
            }
            
            // Альтернатива - клавиатура
            if (Input.GetKeyDown(menuKey))
            {
                menuPressed = true;
            }
            
            if (menuPressed)
            {
                ToggleMenu();
            }
        }
        
        #region Menu Creation
        
        private void CreateMenu()
        {
            menuRoot = new GameObject("GamePauseMenu");
            menuRoot.transform.SetParent(transform);
            
            audioSource = menuRoot.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
            
            GameObject canvasObj = new GameObject("MenuCanvas");
            canvasObj.transform.SetParent(menuRoot.transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one * WORLD_SCALE;
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            // Устанавливаем слой UI
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer == -1) uiLayer = LayerMask.NameToLayer("Default");
            canvasObj.layer = uiLayer;
            
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(MENU_WIDTH, MENU_HEIGHT);
            
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            
            // BoxCollider для BNG Physics Raycast
            BoxCollider collider = canvasObj.AddComponent<BoxCollider>();
            collider.size = new Vector3(MENU_WIDTH, MENU_HEIGHT, 1f);
            collider.isTrigger = true;
            
            CreatePausePanel(canvasObj.transform);
            CreateSettingsPanel(canvasObj.transform);
            CreateConfirmPanel(canvasObj.transform);
            
            // Устанавливаем слой на все дочерние объекты
            SetLayerRecursively(menuRoot, uiLayer);
            
            ShowPauseMenu();
        }
        
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        
        private void CreatePausePanel(Transform parent)
        {
            pauseMenuPanel = CreatePanel(parent, "PausePanel", new Vector2(MENU_WIDTH, MENU_HEIGHT));
            pauseMenuPanel.GetComponent<Image>().color = backgroundColor;
            
            // Заголовок
            var title = CreateText(pauseMenuPanel.transform, "ПАУЗА", 36, FontStyles.Bold);
            SetAnchors(title, 0, 0.85f, 1, 1f, new Vector2(15, 0), new Vector2(-15, -15));
            title.GetComponent<TextMeshProUGUI>().color = accentColor;
            title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            // Линия
            var line = CreatePanel(pauseMenuPanel.transform, "Line", new Vector2(MENU_WIDTH - 60, 3));
            SetAnchors(line, 0.5f, 0.82f, 0.5f, 0.82f, Vector2.zero, Vector2.zero);
            line.GetComponent<Image>().color = accentColor;
            
            // Кнопки
            GameObject btns = new GameObject("Buttons");
            btns.transform.SetParent(pauseMenuPanel.transform);
            btns.transform.localPosition = Vector3.zero;
            btns.transform.localRotation = Quaternion.identity;
            btns.transform.localScale = Vector3.one;
            
            RectTransform btnsRect = btns.AddComponent<RectTransform>();
            btnsRect.anchorMin = btnsRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnsRect.anchoredPosition = new Vector2(0, -20);
            btnsRect.sizeDelta = new Vector2(MENU_WIDTH * 0.85f, MENU_HEIGHT * 0.65f);
            
            float total = 4 * BTN_HEIGHT + 3 * BTN_SPACING;
            float y = total / 2 - BTN_HEIGHT / 2;
            
            CreateButton(btnsRect, "▶ ПРОДОЛЖИТЬ", new Vector2(0, y), OnResume, accentColor);
            CreateButton(btnsRect, "⚙ НАСТРОЙКИ", new Vector2(0, y - (BTN_HEIGHT + BTN_SPACING)), OnSettings, buttonColor);
            CreateButton(btnsRect, "↺ РЕСТАРТ", new Vector2(0, y - 2 * (BTN_HEIGHT + BTN_SPACING)), OnRestart, buttonColor);
            CreateButton(btnsRect, "✕ В ГЛАВНОЕ МЕНЮ", new Vector2(0, y - 3 * (BTN_HEIGHT + BTN_SPACING)), OnMainMenuConfirm, dangerColor);
        }
        
        private void CreateSettingsPanel(Transform parent)
        {
            settingsPanel = CreatePanel(parent, "SettingsPanel", new Vector2(MENU_WIDTH, MENU_HEIGHT));
            settingsPanel.GetComponent<Image>().color = backgroundColor;
            
            // Заголовок
            var title = CreateText(settingsPanel.transform, "НАСТРОЙКИ", 32, FontStyles.Bold);
            SetAnchors(title, 0, 0.87f, 1, 1f, new Vector2(15, 0), new Vector2(-15, -10));
            title.GetComponent<TextMeshProUGUI>().color = accentColor;
            title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            // Линия
            var line = CreatePanel(settingsPanel.transform, "Line", new Vector2(MENU_WIDTH - 60, 3));
            SetAnchors(line, 0.5f, 0.84f, 0.5f, 0.84f, Vector2.zero, Vector2.zero);
            line.GetComponent<Image>().color = accentColor;
            
            // === ЗВУК ===
            var audioLabel = CreateText(settingsPanel.transform, "🔊 ЗВУК", 18, FontStyles.Bold);
            SetAnchors(audioLabel, 0.05f, 0.72f, 0.95f, 0.78f, Vector2.zero, Vector2.zero);
            audioLabel.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.7f);
            
            // Общая громкость
            volumeSlider = CreateSlider(settingsPanel.transform, "Общая громкость", 0.62f, out volumeValueText);
            volumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
            // SFX
            sfxVolumeSlider = CreateSlider(settingsPanel.transform, "Звуковые эффекты", 0.50f, out sfxVolumeValueText);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            
            // === ИНТЕРФЕЙС ===
            var uiLabel = CreateText(settingsPanel.transform, "📱 ИНТЕРФЕЙС", 18, FontStyles.Bold);
            SetAnchors(uiLabel, 0.05f, 0.36f, 0.95f, 0.42f, Vector2.zero, Vector2.zero);
            uiLabel.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.7f);
            
            // Тултипы
            CreateTooltipToggle(settingsPanel.transform);
            
            // Назад
            GameObject backContainer = new GameObject("BackBtn");
            backContainer.transform.SetParent(settingsPanel.transform);
            backContainer.transform.localPosition = Vector3.zero;
            backContainer.transform.localRotation = Quaternion.identity;
            backContainer.transform.localScale = Vector3.one;
            
            RectTransform backRect = backContainer.AddComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0.08f);
            backRect.sizeDelta = new Vector2(MENU_WIDTH * 0.8f, BTN_HEIGHT);
            
            CreateButton(backRect, "← НАЗАД", Vector2.zero, OnBackToPause, buttonColor);
        }
        
        private void CreateConfirmPanel(Transform parent)
        {
            confirmExitPanel = CreatePanel(parent, "ConfirmPanel", new Vector2(MENU_WIDTH, 280));
            confirmExitPanel.GetComponent<Image>().color = backgroundColor;
            
            var title = CreateText(confirmExitPanel.transform, "⚠ ПОДТВЕРЖДЕНИЕ", 28, FontStyles.Bold);
            SetAnchors(title, 0, 0.75f, 1, 1f, new Vector2(15, 0), new Vector2(-15, -15));
            title.GetComponent<TextMeshProUGUI>().color = dangerColor;
            title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            var warning = CreateText(confirmExitPanel.transform, "Вы уверены, что хотите выйти?\nПрогресс текущего дня будет потерян!", 18, FontStyles.Normal);
            SetAnchors(warning, 0.1f, 0.45f, 0.9f, 0.75f, Vector2.zero, Vector2.zero);
            warning.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            GameObject btns = new GameObject("Buttons");
            btns.transform.SetParent(confirmExitPanel.transform);
            btns.transform.localPosition = Vector3.zero;
            btns.transform.localRotation = Quaternion.identity;
            btns.transform.localScale = Vector3.one;
            
            RectTransform btnsRect = btns.AddComponent<RectTransform>();
            btnsRect.anchorMin = btnsRect.anchorMax = new Vector2(0.5f, 0.15f);
            btnsRect.sizeDelta = new Vector2(MENU_WIDTH * 0.9f, BTN_HEIGHT);
            
            float w = (MENU_WIDTH * 0.9f - 20) / 2;
            CreateSmallButton(btnsRect, "ОТМЕНА", new Vector2(-w/2 - 5, 0), new Vector2(w, BTN_HEIGHT), OnCancelExit, buttonColor);
            CreateSmallButton(btnsRect, "ВЫЙТИ", new Vector2(w/2 + 5, 0), new Vector2(w, BTN_HEIGHT), OnConfirmExit, dangerColor);
        }
        
        private void CreateTooltipToggle(Transform parent)
        {
            GameObject container = new GameObject("TooltipToggle");
            container.transform.SetParent(parent);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.24f);
            rect.anchorMax = new Vector2(0.95f, 0.34f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            
            // Label
            var label = CreateText(container.transform, "Показывать подсказки", 18, FontStyles.Normal);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.65f, 1);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            label.GetComponent<TextMeshProUGUI>().verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // Toggle button
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(container.transform);
            toggleObj.transform.localPosition = Vector3.zero;
            toggleObj.transform.localRotation = Quaternion.identity;
            toggleObj.transform.localScale = Vector3.one;
            
            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.7f, 0.1f);
            toggleRect.anchorMax = new Vector2(1f, 0.9f);
            toggleRect.offsetMin = toggleRect.offsetMax = Vector2.zero;
            
            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(toggleObj.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = Vector3.one;
            
            tooltipsToggleBg = bg.AddComponent<Image>();
            
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            
            // Status text
            tooltipsStatusText = CreateText(bg.transform, "ВКЛ", 16, FontStyles.Bold).GetComponent<TextMeshProUGUI>();
            RectTransform statusRect = tooltipsStatusText.GetComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = statusRect.offsetMax = Vector2.zero;
            tooltipsStatusText.alignment = TextAlignmentOptions.Center;
            tooltipsStatusText.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // Toggle
            tooltipsToggle = toggleObj.AddComponent<Toggle>();
            tooltipsToggle.targetGraphic = tooltipsToggleBg;
            tooltipsToggle.isOn = HandTooltipSystem.TooltipsEnabled;
            tooltipsToggle.onValueChanged.AddListener(OnTooltipsChanged);
            
            // Hover
            var trigger = toggleObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entry.callback.AddListener((_) => PlayHoverSound());
            trigger.triggers.Add(entry);
            
            UpdateTooltipVisual(HandTooltipSystem.TooltipsEnabled);
        }
        
        #endregion
        
        #region UI Helpers
        
        private GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = Vector3.one;
            
            panel.AddComponent<Image>().raycastTarget = false;
            
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            
            return panel;
        }
        
        private GameObject CreateText(Transform parent, string content, int fontSize, FontStyles style)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = textColor;
            tmp.raycastTarget = false;
            
            return obj;
        }
        
        private void SetAnchors(GameObject obj, float minX, float minY, float maxX, float maxY, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
        
        private UnityEngine.UI.Slider CreateSlider(Transform parent, string label, float yPos, out TextMeshProUGUI valueText)
        {
            GameObject container = new GameObject(label + "Slider");
            container.transform.SetParent(parent);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, yPos - 0.05f);
            containerRect.anchorMax = new Vector2(0.95f, yPos + 0.05f);
            containerRect.offsetMin = containerRect.offsetMax = Vector2.zero;
            
            // Label
            var labelObj = CreateText(container.transform, label, 16, FontStyles.Normal);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0.7f, 1f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            
            // Value
            var valueObj = CreateText(container.transform, "100%", 16, FontStyles.Bold);
            RectTransform valueRect = valueObj.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.7f, 0.5f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = valueRect.offsetMax = Vector2.zero;
            valueText = valueObj.GetComponent<TextMeshProUGUI>();
            valueText.alignment = TextAlignmentOptions.Right;
            valueText.color = accentColor;
            
            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(container.transform);
            sliderObj.transform.localPosition = Vector3.zero;
            sliderObj.transform.localRotation = Quaternion.identity;
            sliderObj.transform.localScale = Vector3.one;
            
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 0.45f);
            sliderRect.offsetMin = sliderRect.offsetMax = Vector2.zero;
            
            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            
            // BG
            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(sliderObj.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = Vector3.one;
            bg.AddComponent<Image>().color = buttonColor;
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            
            // Fill
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform);
            fillArea.transform.localPosition = Vector3.zero;
            fillArea.transform.localRotation = Quaternion.identity;
            fillArea.transform.localScale = Vector3.one;
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.2f);
            fillAreaRect.anchorMax = new Vector2(1, 0.8f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            fill.transform.localPosition = Vector3.zero;
            fill.transform.localRotation = Quaternion.identity;
            fill.transform.localScale = Vector3.one;
            fill.AddComponent<Image>().color = accentColor;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            
            slider.fillRect = fillRect;
            
            // Handle
            GameObject handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(sliderObj.transform);
            handleArea.transform.localPosition = Vector3.zero;
            handleArea.transform.localRotation = Quaternion.identity;
            handleArea.transform.localScale = Vector3.one;
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8, 0);
            handleAreaRect.offsetMax = new Vector2(-8, 0);
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform);
            handle.transform.localPosition = Vector3.zero;
            handle.transform.localRotation = Quaternion.identity;
            handle.transform.localScale = Vector3.one;
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16, 0);
            handleRect.anchorMin = new Vector2(0, 0);
            handleRect.anchorMax = new Vector2(0, 1);
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            
            return slider;
        }
        
        private void CreateButton(RectTransform parent, string label, Vector2 pos, UnityAction onClick, Color accent)
        {
            GameObject btn = new GameObject(label + "Btn");
            btn.transform.SetParent(parent);
            btn.transform.localPosition = Vector3.zero;
            btn.transform.localRotation = Quaternion.identity;
            btn.transform.localScale = Vector3.one;
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(MENU_WIDTH * 0.8f, BTN_HEIGHT);
            
            Image img = btn.AddComponent<Image>();
            img.color = buttonColor;
            
            UnityEngine.UI.Button button = btn.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = img;
            
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = accent;
            button.colors = colors;
            
            button.onClick.AddListener(onClick);
            button.onClick.AddListener(PlayClickSound);
            
            // Hover
            var trigger = btn.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entry.callback.AddListener((_) => PlayHoverSound());
            trigger.triggers.Add(entry);
            
            // Accent line
            GameObject line = new GameObject("Accent");
            line.transform.SetParent(btn.transform);
            line.transform.localPosition = Vector3.zero;
            line.transform.localRotation = Quaternion.identity;
            line.transform.localScale = Vector3.one;
            line.AddComponent<Image>().color = accent;
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 0);
            lineRect.anchorMax = new Vector2(0, 1);
            lineRect.pivot = new Vector2(0, 0.5f);
            lineRect.sizeDelta = new Vector2(4, 0);
            lineRect.anchoredPosition = Vector2.zero;
            
            // Text
            var text = CreateText(btn.transform, label, 20, FontStyles.Bold);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            text.GetComponent<TextMeshProUGUI>().verticalAlignment = VerticalAlignmentOptions.Middle;
        }
        
        private void CreateSmallButton(RectTransform parent, string label, Vector2 pos, Vector2 size, UnityAction onClick, Color color)
        {
            GameObject btn = new GameObject(label + "Btn");
            btn.transform.SetParent(parent);
            btn.transform.localPosition = Vector3.zero;
            btn.transform.localRotation = Quaternion.identity;
            btn.transform.localScale = Vector3.one;
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            
            Image img = btn.AddComponent<Image>();
            img.color = color;
            
            UnityEngine.UI.Button button = btn.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = img;
            
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f, 1f);
            colors.pressedColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 1f);
            button.colors = colors;
            
            button.onClick.AddListener(onClick);
            button.onClick.AddListener(PlayClickSound);
            
            var trigger = btn.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entry.callback.AddListener((_) => PlayHoverSound());
            trigger.triggers.Add(entry);
            
            var text = CreateText(btn.transform, label, 18, FontStyles.Bold);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            text.GetComponent<TextMeshProUGUI>().verticalAlignment = VerticalAlignmentOptions.Middle;
        }
        
        #endregion
        
        #region Navigation
        
        private void ShowPauseMenu()
        {
            pauseMenuPanel.SetActive(true);
            settingsPanel.SetActive(false);
            confirmExitPanel.SetActive(false);
        }
        
        private void ShowSettings()
        {
            pauseMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
            confirmExitPanel.SetActive(false);
            
            // Синхронизация с HandTooltipSystem
            if (tooltipsToggle != null)
            {
                tooltipsToggle.isOn = HandTooltipSystem.TooltipsEnabled;
                UpdateTooltipVisual(HandTooltipSystem.TooltipsEnabled);
            }
        }
        
        private void ShowConfirmExit()
        {
            pauseMenuPanel.SetActive(false);
            settingsPanel.SetActive(false);
            confirmExitPanel.SetActive(true);
        }
        
        public void ToggleMenu()
        {
            if (isMenuOpen) CloseMenu();
            else OpenMenu();
        }
        
        public void OpenMenu()
        {
            if (isMenuOpen) return;
            isMenuOpen = true;
            
            UpdateMenuPosition();
            menuRoot.SetActive(true);
            ShowPauseMenu();
            
            if (pauseGameOnOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            
            PlaySound(openMenuSound);
            OnMenuOpened?.Invoke();
        }
        
        public void CloseMenu()
        {
            if (!isMenuOpen) return;
            isMenuOpen = false;
            
            menuRoot.SetActive(false);
            
            if (pauseGameOnOpen)
            {
                Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1f;
            }
            
            SaveSettings();
            PlaySound(closeMenuSound);
            OnMenuClosed?.Invoke();
        }
        
        private void HideMenu()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
        }
        
        private void UpdateMenuPosition()
        {
            if (menuRoot == null) return;
            
            if (menuSpawnPoint != null)
            {
                menuRoot.transform.position = menuSpawnPoint.position;
                menuRoot.transform.rotation = menuSpawnPoint.rotation;
            }
            else if (Camera.main != null)
            {
                Vector3 forward = Camera.main.transform.forward;
                forward.y = 0;
                forward.Normalize();
                
                menuRoot.transform.position = Camera.main.transform.position + forward * distanceFromCamera;
                menuRoot.transform.position = new Vector3(menuRoot.transform.position.x, menuHeight, menuRoot.transform.position.z);
                menuRoot.transform.LookAt(Camera.main.transform);
                menuRoot.transform.Rotate(0, 180, 0);
            }
        }
        
        #endregion
        
        #region Callbacks
        
        private void OnResume() => CloseMenu();
        private void OnSettings() => ShowSettings();
        private void OnBackToPause() { SaveSettings(); ShowPauseMenu(); }
        private void OnRestart() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        private void OnMainMenuConfirm() => ShowConfirmExit();
        private void OnCancelExit() => ShowPauseMenu();
        private void OnConfirmExit() { Time.timeScale = 1f; SaveSettings(); SceneManager.LoadScene(mainMenuSceneName); }
        
        private void OnMasterVolumeChanged(float value)
        {
            if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
            AudioListener.volume = value;
            OnVolumeChanged?.Invoke(value);
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            if (sfxVolumeValueText != null) sfxVolumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
        }
        
        private void OnTooltipsChanged(bool enabled)
        {
            // Напрямую управляем HandTooltipSystem
            HandTooltipSystem.TooltipsEnabled = enabled;
            UpdateTooltipVisual(enabled);
            PlayClickSound();
            Debug.Log($"📝 Подсказки: {(enabled ? "ВКЛ" : "ВЫКЛ")}");
        }
        
        private void UpdateTooltipVisual(bool enabled)
        {
            if (tooltipsToggleBg != null)
                tooltipsToggleBg.color = enabled ? toggleOnColor : toggleOffColor;
            if (tooltipsStatusText != null)
                tooltipsStatusText.text = enabled ? "ВКЛ" : "ВЫКЛ";
        }
        
        #endregion
        
        #region Settings
        
        private void LoadSettings()
        {
            float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            
            AudioListener.volume = masterVol;
            
            if (volumeSlider != null) { volumeSlider.value = masterVol; OnMasterVolumeChanged(masterVol); }
            if (sfxVolumeSlider != null) { sfxVolumeSlider.value = sfxVol; OnSFXVolumeChanged(sfxVol); }
        }
        
        private void SaveSettings()
        {
            if (volumeSlider != null) PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volumeSlider.value);
            if (sfxVolumeSlider != null) PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolumeSlider.value);
            PlayerPrefs.Save();
        }
        
        #endregion
        
        #region Audio
        
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, 0.7f);
        }
        
        private void PlayHoverSound()
        {
            if (hoverSound != null && audioSource != null) audioSource.PlayOneShot(hoverSound, 0.4f);
        }
        
        private void PlayClickSound()
        {
            if (clickSound != null && audioSource != null) audioSource.PlayOneShot(clickSound, 0.6f);
        }
        
        #endregion
        
        public bool IsMenuOpen => isMenuOpen;
        
        void OnDestroy()
        {
            SaveSettings();
            if (isMenuOpen && pauseGameOnOpen) Time.timeScale = 1f;
        }
    }
}