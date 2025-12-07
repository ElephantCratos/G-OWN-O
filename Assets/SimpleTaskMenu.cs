using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BNG;

public class SimpleTaskMenu : MonoBehaviour
{
    [Header("References")]
    public DayEventManager dayEventManager;
    public Transform playerCamera; // VR камера
    
    [Header("Settings")]
    public float distanceFromPlayer = 1.5f;
    public KeyCode testKey = KeyCode.Tab; // Для теста в редакторе
    public ControllerBinding vrButton = ControllerBinding.YButton;
    
    [Header("UI (создастся автоматически)")]
    public GameObject menuPanel;
    
    private GameObject canvas;
    private TextMeshProUGUI contentText;
    private bool isVisible = false;
    private InputBridge input;
    private bool buttonWasPressed = false;
    
    void Start()
    {
        input = InputBridge.Instance;
        
        // Находим VR камеру автоматически
        if (playerCamera == null)
        {
            GameObject xrRig = GameObject.Find("XR Rig Advanced");
            if (xrRig == null) xrRig = GameObject.Find("XR Rig");
            
            if (xrRig != null)
            {
                Transform cameraRig = xrRig.transform.Find("CameraRig");
                if (cameraRig != null)
                {
                    Transform trackingSpace = cameraRig.Find("TrackingSpace");
                    if (trackingSpace != null)
                    {
                        playerCamera = trackingSpace.Find("CenterEyeAnchor");
                    }
                }
            }
            
            if (playerCamera == null)
            {
                playerCamera = Camera.main.transform;
            }
        }
        
        CreateSimpleUI();
        HideMenu();
        
        Debug.Log("SimpleTaskMenu: Инициализирован. Нажмите " + testKey + " или кнопку " + vrButton);
    }
    
    void CreateSimpleUI()
    {
        // Создаём Canvas
        canvas = new GameObject("TaskMenuCanvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800, 600);
        canvasRect.localScale = Vector3.one * 0.001f;
        
        // Создаём панель
        menuPanel = new GameObject("Panel");
        menuPanel.transform.SetParent(canvas.transform, false);
        
        Image panelImg = menuPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        // Создаём текст
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(menuPanel.transform, false);
        
        contentText = textObj.AddComponent<TextMeshProUGUI>();
        contentText.fontSize = 32;
        contentText.color = Color.white;
        contentText.alignment = TextAlignmentOptions.TopLeft;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(40, 40);
        textRect.offsetMax = new Vector2(-40, -40);
        
        Debug.Log("SimpleTaskMenu: UI создан");
    }
    
    void Update()
    {
        if (input == null) return;
        
        // Проверка кнопки VR
        bool buttonPressed = input.GetControllerBindingValue(vrButton);
        if (buttonPressed && !buttonWasPressed)
        {
            ToggleMenu();
        }
        buttonWasPressed = buttonPressed;
        
        // Тест в редакторе
        if (Input.GetKeyDown(testKey))
        {
            ToggleMenu();
        }
        
        // Обновляем позицию меню если оно видимо
        if (isVisible && canvas != null && playerCamera != null)
        {
            UpdateMenuPosition();
        }
    }
    
    void ToggleMenu()
    {
        if (isVisible)
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
        
        Debug.Log("SimpleTaskMenu: " + (isVisible ? "Открыто" : "Закрыто"));
    }
    
    void ShowMenu()
    {
        if (canvas == null || dayEventManager == null)
        {
            Debug.LogError("SimpleTaskMenu: Canvas или DayEventManager == null");
            return;
        }
        
        UpdateMenuContent();
        UpdateMenuPosition();
        canvas.SetActive(true);
        isVisible = true;
    }
    
    void HideMenu()
    {
        if (canvas != null)
        {
            canvas.SetActive(false);
        }
        isVisible = false;
    }
    
    void UpdateMenuPosition()
    {
        if (playerCamera == null || canvas == null) return;
        
        // Размещаем меню перед игроком
        Vector3 forward = playerCamera.forward;
        forward.y = 0; // Убираем наклон
        forward.Normalize();
        
        canvas.transform.position = playerCamera.position + forward * distanceFromPlayer;
        canvas.transform.LookAt(playerCamera);
        canvas.transform.Rotate(0, 180, 0); // Поворачиваем к игроку
    }
    
    void UpdateMenuContent()
    {
        if (contentText == null || dayEventManager == null) return;
        
        string text = $"<b><size=48>ДЕНЬ {dayEventManager.currentDay}</size></b>\n\n";
        
        if (dayEventManager.todayEvents.Count == 0)
        {
            text += "<color=yellow>Нет заданий на сегодня</color>";
        }
        else
        {
            int completed = 0;
            foreach (var task in dayEventManager.todayEvents)
            {
                string status = task.isCompleted ? "<color=green>✓ ВЫПОЛНЕНО</color>" : "<color=red>○ В процессе</color>";
                string taskName = GetTaskName(task.eventName);
                
                text += $"{status}  {taskName}\n";
                text += GetTaskProgress(task.eventName) + "\n\n";
                
                if (task.isCompleted) completed++;
            }
            
            text += $"\n<b>Прогресс: {completed}/{dayEventManager.todayEvents.Count}</b>";
        }
        
        contentText.text = text;
    }
    
    string GetTaskName(string eventName)
    {
        switch (eventName)
        {
            case "ClearRats": return "Уничтожить крыс";
            case "PatchHoles": return "Залатать пробоины";
            case "FixControls": return "Починить панель управления";
            case "ReplacePins": return "Заменить пины";
            case "ReplaceBattery": return "Заменить батарею";
            default: return eventName;
        }
    }
    
    string GetTaskProgress(string eventName)
    {
        // Пытаемся найти компоненты автоматически
        if (eventName == "ClearRats")
        {
            RatSpawner spawner = FindObjectOfType<RatSpawner>();
            if (spawner != null)
                return $"   Убито: {spawner.killedRats}/{spawner.ratsToKill}";
        }
        else if (eventName == "PatchHoles")
        {
            HoleSpawner spawner = FindObjectOfType<HoleSpawner>();
            if (spawner != null)
                return $"   Заварено: {spawner.patchedHoles}/{spawner.holesToPatch}";
        }
        else if (eventName == "FixControls")
        {
            ControlPanelMalfunction panel = FindObjectOfType<ControlPanelMalfunction>();
            if (panel != null)
            {
                if (panel.IsCurrentlyFixed())
                    return "   <color=green>Параметры в норме</color>";
                else
                    return "   <color=yellow>Требуется настройка</color>";
            }
        }
        else if (eventName == "ReplacePins")
        {
            PinReplacementTask pins = FindObjectOfType<PinReplacementTask>();
            if (pins != null)
                return "   " + pins.GetProgressText();
        }
        else if (eventName == "ReplaceBattery")
        {
            BatteryReplacementEvent battery = FindObjectOfType<BatteryReplacementEvent>();
            if (battery != null)
                return "   " + battery.GetStatus();
        }
        
        return "";
    }
    
    // Публичные методы
    public void Show() { ShowMenu(); }
    public void Hide() { HideMenu(); }
    public void Toggle() { ToggleMenu(); }
    public bool IsVisible => isVisible;
}