using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using BNG;

namespace VRMenu
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Menu Settings")]
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private Transform menuSpawnPoint;
        [SerializeField] private float distanceFromCamera = 1.5f;
        [SerializeField] private float menuHeight = 1.2f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        
        [Header("Style")]
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        [SerializeField] private Color buttonColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        [SerializeField] private Color buttonHoverColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color accentColor = new Color(0.3f, 0.8f, 1f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        
        private GameObject menuRoot;
        private Canvas canvas;
        private AudioSource audioSource;
        private List<UnityEngine.UI.Button> menuButtons = new List<UnityEngine.UI.Button>();
        
        private GameObject mainMenuPanel;
        private GameObject settingsPanel;
        
        private UnityEngine.UI.Slider volumeSlider;
        private TextMeshProUGUI volumeValueText;
        
        private const float MENU_WIDTH = 400f;
        private const float MENU_HEIGHT = 500f;
        private const float BUTTON_HEIGHT = 60f;
        private const float BUTTON_SPACING = 15f;
        private const float WORLD_SCALE = 0.004f;
        
        private const string VOLUME_KEY = "MasterVolume";

        void Start()
        {
            CreateMenu();
            LoadSettings();
        }
        
        private void CreateMenu()
        {
            menuRoot = new GameObject("MainMenu");
            
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
                menuRoot.transform.position = new Vector3(
                    menuRoot.transform.position.x,
                    menuHeight,
                    menuRoot.transform.position.z
                );
                menuRoot.transform.LookAt(Camera.main.transform);
                menuRoot.transform.Rotate(0, 180, 0);
            }
            
            audioSource = menuRoot.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            
            GameObject canvasObj = new GameObject("MenuCanvas");
            canvasObj.transform.SetParent(menuRoot.transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one * WORLD_SCALE;
            
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            // Устанавливаем слой UI
            canvasObj.layer = LayerMask.NameToLayer("UI");
            if (canvasObj.layer == -1)
            {
                canvasObj.layer = LayerMask.NameToLayer("Default");
            }
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(MENU_WIDTH, MENU_HEIGHT);
            
            // GraphicRaycaster для взаимодействия
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            
            // CanvasGroup
            CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            // Добавляем BoxCollider для PhysicsRaycaster (BNG часто использует это)
            BoxCollider collider = canvasObj.AddComponent<BoxCollider>();
            collider.size = new Vector3(MENU_WIDTH, MENU_HEIGHT, 1f);
            collider.isTrigger = true;
            
            CreateMainMenuPanel(canvasObj.transform);
            CreateSettingsPanel(canvasObj.transform);
            
            // Устанавливаем слой UI на все дочерние объекты
            SetLayerRecursively(menuRoot, canvasObj.layer);
            
            ShowMainMenu();
        }
        
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        
        private void CreateMainMenuPanel(Transform parent)
        {
            mainMenuPanel = CreatePanel(parent, "MainMenuPanel", Vector2.zero, new Vector2(MENU_WIDTH, MENU_HEIGHT));
            Image bgImage = mainMenuPanel.GetComponent<Image>();
            bgImage.color = backgroundColor;
            bgImage.raycastTarget = true; // Важно для блокировки лучей
            
            RectTransform bgRect = mainMenuPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            
            GameObject titleObj = CreateText(mainMenuPanel.transform, "Title", "НАЗВАНИЕ ИГРЫ", 32, FontStyles.Bold);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = new Vector2(15, 0);
            titleRect.offsetMax = new Vector2(-15, -15);
            
            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.color = accentColor;
            titleText.alignment = TextAlignmentOptions.Center;
            
            GameObject accentLine = CreatePanel(mainMenuPanel.transform, "AccentLine", Vector2.zero, new Vector2(MENU_WIDTH - 60, 3));
            RectTransform lineRect = accentLine.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.82f);
            lineRect.anchorMax = new Vector2(0.5f, 0.82f);
            lineRect.sizeDelta = new Vector2(MENU_WIDTH - 60, 3);
            accentLine.GetComponent<Image>().color = accentColor;
            
            GameObject buttonsContainer = new GameObject("ButtonsContainer");
            buttonsContainer.transform.SetParent(mainMenuPanel.transform);
            buttonsContainer.transform.localPosition = Vector3.zero;
            buttonsContainer.transform.localRotation = Quaternion.identity;
            buttonsContainer.transform.localScale = Vector3.one;
            
            RectTransform containerRect = buttonsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0, -30);
            containerRect.sizeDelta = new Vector2(MENU_WIDTH * 0.85f, MENU_HEIGHT * 0.6f);
            
            float totalButtonsHeight = 4 * BUTTON_HEIGHT + 3 * BUTTON_SPACING;
            float startY = totalButtonsHeight / 2 - BUTTON_HEIGHT / 2;
            
            CreateMenuButton(containerRect, "NewGame", "НОВАЯ ИГРА", 
                new Vector2(0, startY), OnNewGame, true);
            
            CreateMenuButton(containerRect, "LoadGame", "ЗАГРУЗИТЬ", 
                new Vector2(0, startY - (BUTTON_HEIGHT + BUTTON_SPACING)), OnLoadGame, false);
            
            CreateMenuButton(containerRect, "Settings", "НАСТРОЙКИ", 
                new Vector2(0, startY - 2 * (BUTTON_HEIGHT + BUTTON_SPACING)), OnSettings, true);
            
            CreateMenuButton(containerRect, "Exit", "ВЫХОД", 
                new Vector2(0, startY - 3 * (BUTTON_HEIGHT + BUTTON_SPACING)), OnExit, true);
            
            GameObject versionObj = CreateText(mainMenuPanel.transform, "Version", "v0.1 Alpha", 14, FontStyles.Italic);
            RectTransform versionRect = versionObj.GetComponent<RectTransform>();
            versionRect.anchorMin = new Vector2(0, 0);
            versionRect.anchorMax = new Vector2(1, 0.08f);
            versionRect.offsetMin = new Vector2(15, 8);
            versionRect.offsetMax = new Vector2(-15, 0);
            
            TextMeshProUGUI versionText = versionObj.GetComponent<TextMeshProUGUI>();
            versionText.color = new Color(1, 1, 1, 0.3f);
            versionText.alignment = TextAlignmentOptions.Center;
        }
        
        private void CreateSettingsPanel(Transform parent)
        {
            settingsPanel = CreatePanel(parent, "SettingsPanel", Vector2.zero, new Vector2(MENU_WIDTH, MENU_HEIGHT));
            Image bgImage = settingsPanel.GetComponent<Image>();
            bgImage.color = backgroundColor;
            bgImage.raycastTarget = true;
            
            RectTransform bgRect = settingsPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            
            GameObject titleObj = CreateText(settingsPanel.transform, "Title", "НАСТРОЙКИ", 32, FontStyles.Bold);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = new Vector2(15, 0);
            titleRect.offsetMax = new Vector2(-15, -15);
            
            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.color = accentColor;
            titleText.alignment = TextAlignmentOptions.Center;
            
            GameObject accentLine = CreatePanel(settingsPanel.transform, "AccentLine", Vector2.zero, new Vector2(MENU_WIDTH - 60, 3));
            RectTransform lineRect = accentLine.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.82f);
            lineRect.anchorMax = new Vector2(0.5f, 0.82f);
            lineRect.sizeDelta = new Vector2(MENU_WIDTH - 60, 3);
            accentLine.GetComponent<Image>().color = accentColor;
            
            // Volume slider container
            GameObject sliderContainer = new GameObject("VolumeContainer");
            sliderContainer.transform.SetParent(settingsPanel.transform);
            sliderContainer.transform.localPosition = Vector3.zero;
            sliderContainer.transform.localRotation = Quaternion.identity;
            sliderContainer.transform.localScale = Vector3.one;
            
            RectTransform sliderContainerRect = sliderContainer.AddComponent<RectTransform>();
            sliderContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderContainerRect.pivot = new Vector2(0.5f, 0.5f);
            sliderContainerRect.anchoredPosition = new Vector2(0, 50);
            sliderContainerRect.sizeDelta = new Vector2(MENU_WIDTH * 0.85f, 80);
            
            volumeSlider = CreateVolumeSlider(sliderContainerRect, "Volume", "ГРОМКОСТЬ", out volumeValueText);
            
            // Back button
            GameObject backContainer = new GameObject("BackContainer");
            backContainer.transform.SetParent(settingsPanel.transform);
            backContainer.transform.localPosition = Vector3.zero;
            backContainer.transform.localRotation = Quaternion.identity;
            backContainer.transform.localScale = Vector3.one;
            
            RectTransform backRect = backContainer.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0.15f);
            backRect.anchorMax = new Vector2(0.5f, 0.15f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.anchoredPosition = Vector2.zero;
            backRect.sizeDelta = new Vector2(MENU_WIDTH * 0.8f, BUTTON_HEIGHT);
            
            CreateMenuButton(backRect, "Back", "← НАЗАД", Vector2.zero, OnBackToMainMenu, true);
        }
        
        private UnityEngine.UI.Slider CreateVolumeSlider(RectTransform parent, string name, string label, out TextMeshProUGUI valueText)
        {
            // Label
            GameObject labelObj = CreateText(parent, "Label", label, 20, FontStyles.Bold);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(0.7f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            
            // Value
            GameObject valueObj = CreateText(parent, "Value", "100%", 20, FontStyles.Bold);
            RectTransform valueRect = valueObj.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.7f, 0.6f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            valueText = valueObj.GetComponent<TextMeshProUGUI>();
            valueText.alignment = TextAlignmentOptions.Right;
            valueText.color = accentColor;
            
            // Slider
            GameObject sliderObj = new GameObject(name + "Slider");
            sliderObj.transform.SetParent(parent);
            sliderObj.transform.localPosition = Vector3.zero;
            sliderObj.transform.localRotation = Quaternion.identity;
            sliderObj.transform.localScale = Vector3.one;
            
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 0.5f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            
            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localRotation = Quaternion.identity;
            bgObj.transform.localScale = Vector3.one;
            
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = buttonColor;
            bgImage.raycastTarget = true;
            
            RectTransform bgRectT = bgObj.GetComponent<RectTransform>();
            bgRectT.anchorMin = Vector2.zero;
            bgRectT.anchorMax = Vector2.one;
            bgRectT.offsetMin = Vector2.zero;
            bgRectT.offsetMax = Vector2.zero;
            
            // Fill area
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform);
            fillArea.transform.localPosition = Vector3.zero;
            fillArea.transform.localRotation = Quaternion.identity;
            fillArea.transform.localScale = Vector3.one;
            
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);
            
            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localRotation = Quaternion.identity;
            fillObj.transform.localScale = Vector3.one;
            
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = accentColor;
            fillImage.raycastTarget = true;
            
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            slider.fillRect = fillRect;
            
            // Handle area
            GameObject handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(sliderObj.transform);
            handleArea.transform.localPosition = Vector3.zero;
            handleArea.transform.localRotation = Quaternion.identity;
            handleArea.transform.localScale = Vector3.one;
            
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);
            
            // Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform);
            handleObj.transform.localPosition = Vector3.zero;
            handleObj.transform.localRotation = Quaternion.identity;
            handleObj.transform.localScale = Vector3.one;
            
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;
            handleImage.raycastTarget = true;
            
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            handleRect.anchorMin = new Vector2(0, 0);
            handleRect.anchorMax = new Vector2(0, 1);
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            
            slider.onValueChanged.AddListener(OnVolumeChanged);
            
            return slider;
        }
        
        private GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = Vector3.one;
            
            Image image = panel.AddComponent<Image>();
            image.raycastTarget = false;
            
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            return panel;
        }
        
        private GameObject CreateText(Transform parent, string name, string content, int fontSize, FontStyles style)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = textColor;
            tmp.raycastTarget = false;
            
            return textObj;
        }
        
        private void CreateMenuButton(RectTransform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick, bool interactable)
        {
            GameObject buttonObj = new GameObject(name + "Button");
            buttonObj.transform.SetParent(parent);
            buttonObj.transform.localPosition = Vector3.zero;
            buttonObj.transform.localRotation = Quaternion.identity;
            buttonObj.transform.localScale = Vector3.one;
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(MENU_WIDTH * 0.8f, BUTTON_HEIGHT);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = interactable ? buttonColor : disabledColor;
            buttonImage.raycastTarget = true; // ВАЖНО для VR!
            
            UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = buttonImage;
            button.interactable = interactable;
            
            ColorBlock colors = button.colors;
            colors.normalColor = interactable ? buttonColor : disabledColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = accentColor;
            colors.disabledColor = disabledColor;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            
            if (interactable)
            {
                button.onClick.AddListener(onClick);
                button.onClick.AddListener(PlayClickSound);
            }
            
            // Hover events для звука
            EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();
            
            var pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { if (interactable) PlayHoverSound(); });
            trigger.triggers.Add(pointerEnter);
            
            // Акцентная линия
            GameObject accentLineObj = CreatePanel(buttonObj.transform, "Accent", Vector2.zero, new Vector2(4, BUTTON_HEIGHT));
            RectTransform accentRect = accentLineObj.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(4, 0);
            accentLineObj.GetComponent<Image>().color = interactable ? accentColor : disabledColor;
            
            // Текст
            GameObject textObj = CreateText(buttonObj.transform, "Label", label, 22, FontStyles.Bold);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.color = interactable ? textColor : disabledColor;
            
            menuButtons.Add(button);
        }
        
        private void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            settingsPanel.SetActive(false);
        }
        
        private void ShowSettings()
        {
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }
        
        private void PlayHoverSound()
        {
            if (hoverSound != null && audioSource != null)
                audioSource.PlayOneShot(hoverSound, 0.5f);
        }
        
        private void PlayClickSound()
        {
            if (clickSound != null && audioSource != null)
                audioSource.PlayOneShot(clickSound, 0.7f);
        }
        
        private void OnVolumeChanged(float value)
        {
            if (volumeValueText != null)
                volumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
            
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VOLUME_KEY, value);
        }
        
        private void LoadSettings()
        {
            float volume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
            
            if (volumeSlider != null)
            {
                volumeSlider.value = volume;
                OnVolumeChanged(volume);
            }
        }
        
        private void OnNewGame()
        {
            Debug.Log("Starting new game...");
            PlayerPrefs.Save();
            SceneManager.LoadScene(gameSceneName);
        }
        
        private void OnLoadGame()
        {
            Debug.Log("Load game - not implemented yet");
        }
        
        private void OnSettings()
        {
            Debug.Log("Opening settings...");
            ShowSettings();
        }
        
        private void OnBackToMainMenu()
        {
            Debug.Log("Back to main menu...");
            PlayerPrefs.Save();
            ShowMainMenu();
        }
        
        private void OnExit()
        {
            Debug.Log("Exiting game...");
            PlayerPrefs.Save();
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        void OnDestroy()
        {
            PlayerPrefs.Save();
            
            if (menuRoot != null)
                Destroy(menuRoot);
        }
    }
}