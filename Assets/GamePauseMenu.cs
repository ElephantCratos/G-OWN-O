using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using BNG;
using UnityButton = UnityEngine.UI.Button;
using UnityImage = UnityEngine.UI.Image;

public class GamePauseMenu : MonoBehaviour
{
    [Header("Scene")]
    public string mainMenuScene = "MainMenu";

    [Header("Position")]
    public Transform playerCamera;
    public Vector3 menuOffset = new Vector3(0, 0, 1.5f);
    public float menuScale = 0.002f;

    [Header("Input")]
    public KeyCode keyboardKey = KeyCode.Escape;
    public bool useRightController = true;
    public bool useLeftController = false;
    public bool useKeyboard = true;

    [Header("Colors")]
    public Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
    public Color buttonColor = new Color(0.15f, 0.18f, 0.25f, 1f);
    public Color primaryColor = new Color(0.3f, 0.6f, 0.9f, 1f);
    public Color dangerColor = new Color(0.9f, 0.3f, 0.3f, 1f);
    public Color hoverColor = new Color(0.25f, 0.3f, 0.4f, 1f);

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private GameObject backgroundPanel;
    private TextMeshProUGUI titleText;
    private UnityButton continueButton;
    private UnityButton restartButton;
    private UnityButton mainMenuButton;

    private bool isOpen;
    private InputBridge input;

    void Start()
    {
        input = InputBridge.Instance;

        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                playerCamera = cam.transform;
        }

        CreatePauseMenu();
        
        if (canvas != null)
            canvas.enabled = false;
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (IsMenuButtonPressed())
        {
            if (isOpen) Close();
            else Open();
        }

        // Меню следует за камерой когда открыто
        if (isOpen && canvas != null && playerCamera != null)
        {
            Vector3 targetPosition = playerCamera.position + 
                playerCamera.forward * menuOffset.z +
                playerCamera.up * menuOffset.y +
                playerCamera.right * menuOffset.x;

            canvas.transform.position = Vector3.Lerp(
                canvas.transform.position, targetPosition, Time.unscaledDeltaTime * 5f);
            
            canvas.transform.rotation = Quaternion.Slerp(
                canvas.transform.rotation,
                Quaternion.LookRotation(canvas.transform.position - playerCamera.position),
                Time.unscaledDeltaTime * 5f);
        }
    }

    bool IsMenuButtonPressed()
    {
        if (useKeyboard && Input.GetKeyDown(keyboardKey))
            return true;

        if (input == null)
            return false;

        if (useRightController && input.StartButtonDown)
            return true;

        if (useLeftController && input.BackButtonDown)
            return true;

        return false;
    }

    void CreatePauseMenu()
    {
        // 1. CANVAS
        GameObject canvasObj = new GameObject("PauseMenuCanvas");
        canvasObj.transform.SetParent(null); // Независимый объект
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

        // 2. BACKGROUND PANEL
        backgroundPanel = CreatePanel(canvasObj.transform, "Background", panelColor);
        RectTransform bgRect = backgroundPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(1200, 900);
        bgRect.anchoredPosition = Vector2.zero;

        // 3. TITLE
        CreateTitle(backgroundPanel.transform);

        // 4. BUTTONS
        continueButton = CreateButton(backgroundPanel.transform, "ContinueButton", 
            "ПРОДОЛЖИТЬ", primaryColor, new Vector2(0, 150));
        continueButton.onClick.AddListener(Close);

        restartButton = CreateButton(backgroundPanel.transform, "RestartButton", 
            "РЕСТАРТ", buttonColor, new Vector2(0, 0));
        restartButton.onClick.AddListener(Restart);

        mainMenuButton = CreateButton(backgroundPanel.transform, "MainMenuButton", 
            "В МЕНЮ", dangerColor, new Vector2(0, -150));
        mainMenuButton.onClick.AddListener(GoToMenu);

        Debug.Log("✅ Pause Menu создано!");
    }

    void CreateTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ПАУЗА";
        titleText.fontSize = 100;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        RectTransform rect = titleText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -100);
        rect.sizeDelta = new Vector2(1000, 150);
    }

    UnityButton CreateButton(Transform parent, string name, string text, Color color, Vector2 position)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(800, 120);

        UnityImage buttonImage = buttonObj.AddComponent<UnityImage>();
        buttonImage.color = color;

        UnityButton button = buttonObj.AddComponent<UnityButton>();

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = color * 0.7f;
        colors.selectedColor = color;
        colors.disabledColor = color * 0.5f;
        button.colors = colors;

        // Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 60;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;

        return button;
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

    void Open()
    {
        isOpen = true;
        PositionMenu();
        
        if (canvas != null)
            canvas.enabled = true;
        
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        
        Time.timeScale = 0f;
    }

    void Close()
    {
        isOpen = false;
        
        if (canvas != null)
            canvas.enabled = false;
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        
        Time.timeScale = 1f;
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    void OnDestroy()
    {
        // Уничтожаем canvas при удалении скрипта
        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    void PositionMenu()
    {
        if (canvas != null && playerCamera != null)
        {
            Vector3 targetPosition = playerCamera.position + 
                playerCamera.forward * menuOffset.z +
                playerCamera.up * menuOffset.y +
                playerCamera.right * menuOffset.x;

            canvas.transform.position = targetPosition;
            canvas.transform.rotation = Quaternion.LookRotation(
                canvas.transform.position - playerCamera.position);
        }
    }
}