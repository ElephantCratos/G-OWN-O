using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BNG;
using UnityButton = UnityEngine.UI.Button;
using UnityImage = UnityEngine.UI.Image;

namespace VRMenu
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Scene")]
        public string gameSceneName = "Game";

        [Header("Position")]
        public Transform menuSpawnPoint;
        public float distanceFromCamera = 1.5f;
        public float menuHeight = 1.2f;

        [Header("Colors")]
        public Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        public Color buttonColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        public Color buttonHoverColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        public Color primaryColor = new Color(0.3f, 0.8f, 1f, 1f);
        public Color dangerColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        public Color textColor = Color.white;
        public Color disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        [Header("Audio")]
        public AudioClip hoverSound;
        public AudioClip clickSound;

        private GameObject menuRoot;
        private Canvas canvas;
        private AudioSource audioSource;
        private Camera mainCamera;

        private const float MENU_WIDTH = 500f;
        private const float MENU_HEIGHT = 650f;
        private const float BUTTON_HEIGHT = 80f;
        private const float BUTTON_SPACING = 20f;
        private const float WORLD_SCALE = 0.002f;

        void Start()
        {
            mainCamera = Camera.main;
            
            EnsureEventSystem();
            CreateMainMenu();
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                Debug.Log("✅ EventSystem создан автоматически");
            }
        }

        void CreateMainMenu()
        {
            menuRoot = new GameObject("MainMenu");

            if (menuSpawnPoint != null)
            {
                menuRoot.transform.position = menuSpawnPoint.position;
                menuRoot.transform.rotation = menuSpawnPoint.rotation;
            }
            else if (mainCamera != null)
            {
                Vector3 forward = mainCamera.transform.forward;
                forward.y = 0;
                forward.Normalize();

                menuRoot.transform.position = mainCamera.transform.position + forward * distanceFromCamera;
                menuRoot.transform.position = new Vector3(
                    menuRoot.transform.position.x,
                    menuHeight,
                    menuRoot.transform.position.z
                );
                menuRoot.transform.LookAt(mainCamera.transform);
                menuRoot.transform.Rotate(0, 180, 0);
            }

            audioSource = menuRoot.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            GameObject canvasObj = new GameObject("MenuCanvas");
            canvasObj.transform.SetParent(menuRoot.transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one * WORLD_SCALE;

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer == -1)
            {
                uiLayer = LayerMask.NameToLayer("Default");
                Debug.LogWarning("⚠️ UI слой не найден, используется Default");
            }
            canvasObj.layer = uiLayer;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(MENU_WIDTH, MENU_HEIGHT);

            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            CreateMenuPanel(canvasObj.transform);

            SetLayerRecursively(menuRoot, uiLayer);

            Debug.Log("✅ Main Menu создано");
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void CreateMenuPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "MenuPanel", Vector2.zero, new Vector2(MENU_WIDTH, MENU_HEIGHT));
            UnityImage bgImage = panel.GetComponent<UnityImage>();
            bgImage.color = panelColor;
            bgImage.raycastTarget = true;

            RectTransform bgRect = panel.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);

            // Title
            GameObject titleObj = CreateText(panel.transform, "Title", "НАЗВАНИЕ ИГРЫ", 48, FontStyles.Bold);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = new Vector2(15, 0);
            titleRect.offsetMax = new Vector2(-15, -15);

            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.color = primaryColor;
            titleText.alignment = TextAlignmentOptions.Center;

            // Accent line
            GameObject accentLine = CreatePanel(panel.transform, "AccentLine", Vector2.zero, new Vector2(MENU_WIDTH - 60, 4));
            RectTransform lineRect = accentLine.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.82f);
            lineRect.anchorMax = new Vector2(0.5f, 0.82f);
            lineRect.sizeDelta = new Vector2(MENU_WIDTH - 60, 4);
            accentLine.GetComponent<UnityImage>().color = primaryColor;

            // Buttons container
            GameObject buttonsContainer = new GameObject("ButtonsContainer");
            buttonsContainer.transform.SetParent(panel.transform);
            buttonsContainer.transform.localPosition = Vector3.zero;
            buttonsContainer.transform.localRotation = Quaternion.identity;
            buttonsContainer.transform.localScale = Vector3.one;

            RectTransform containerRect = buttonsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0, -20);
            containerRect.sizeDelta = new Vector2(MENU_WIDTH * 0.85f, MENU_HEIGHT * 0.6f);

            float totalButtonsHeight = 4 * BUTTON_HEIGHT + 3 * BUTTON_SPACING;
            float startY = totalButtonsHeight / 2 - BUTTON_HEIGHT / 2;

            CreateMenuButton(containerRect, "NewGame", "НОВАЯ ИГРА",
                new Vector2(0, startY), OnNewGame, primaryColor, true);

            CreateMenuButton(containerRect, "LoadGame", "ЗАГРУЗИТЬ",
                new Vector2(0, startY - (BUTTON_HEIGHT + BUTTON_SPACING)), OnLoadGame, buttonColor, false);

            CreateMenuButton(containerRect, "Settings", "НАСТРОЙКИ",
                new Vector2(0, startY - 2 * (BUTTON_HEIGHT + BUTTON_SPACING)), OnSettings, buttonColor, true);

            CreateMenuButton(containerRect, "Exit", "ВЫХОД",
                new Vector2(0, startY - 3 * (BUTTON_HEIGHT + BUTTON_SPACING)), OnExit, dangerColor, true);

            // Version hint
            GameObject hintObj = CreateText(panel.transform, "Version", "v0.1 Alpha", 18, FontStyles.Italic);
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0, 0);
            hintRect.anchorMax = new Vector2(1, 0.08f);
            hintRect.offsetMin = new Vector2(15, 10);
            hintRect.offsetMax = new Vector2(-15, 0);

            TextMeshProUGUI hintText = hintObj.GetComponent<TextMeshProUGUI>();
            hintText.color = new Color(1, 1, 1, 0.3f);
            hintText.alignment = TextAlignmentOptions.Center;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = Vector3.one;

            UnityImage image = panel.AddComponent<UnityImage>();
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

        private void CreateMenuButton(RectTransform parent, string name, string label, Vector2 position, 
            UnityEngine.Events.UnityAction onClick, Color color, bool interactable)
        {
            GameObject buttonObj = new GameObject(name + "Button");
            buttonObj.transform.SetParent(parent);
            buttonObj.transform.localPosition = Vector3.zero;
            buttonObj.transform.localRotation = Quaternion.identity;
            buttonObj.transform.localScale = Vector3.one;

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(MENU_WIDTH * 0.85f, BUTTON_HEIGHT);

            UnityImage buttonImage = buttonObj.AddComponent<UnityImage>();
            buttonImage.color = interactable ? color : disabledColor;
            buttonImage.raycastTarget = true;

            UnityButton button = buttonObj.AddComponent<UnityButton>();
            button.targetGraphic = buttonImage;
            button.interactable = interactable;

            ColorBlock colors = button.colors;
            colors.normalColor = interactable ? color : disabledColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = primaryColor;
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
            accentLineObj.GetComponent<UnityImage>().color = interactable ? primaryColor : disabledColor;

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

        private void OnNewGame()
        {
            Debug.Log("Starting new game...");
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnLoadGame()
        {
            Debug.Log("Load game - not implemented yet");
        }

        private void OnSettings()
        {
            Debug.Log("Opening settings...");
            // TODO: Добавить панель настроек
        }

        private void OnExit()
        {
            Debug.Log("Exiting game...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnDestroy()
        {
            if (menuRoot != null)
                Destroy(menuRoot);
        }
    }
}