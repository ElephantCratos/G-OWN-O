using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BNG;

public class SimpleTaskMenu : MonoBehaviour
{
    [Header("References")]
    public DayEventManager dayEventManager;
    public GameOverManager gameOverManager; // НОВОЕ: для статуса систем
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
        
        // НОВОЕ: Автопоиск GameOverManager
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<GameOverManager>();
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
        canvasRect.sizeDelta = new Vector2(900, 1100); // ИЗМЕНЕНО: увеличен размер
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
        contentText.fontSize = 28; // ИЗМЕНЕНО: уменьшен размер шрифта
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
        
        // Обновляем позицию и содержимое меню если оно видимо
        if (isVisible && canvas != null && playerCamera != null)
        {
            UpdateMenuPosition();
            UpdateMenuContent(); // НОВОЕ: постоянное обновление контента
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
        
        string text = $"<b><size=44>📋 ДЕНЬ {dayEventManager.currentDay}</size></b>\n\n";
        
        // НОВОЕ: Статус систем корабля
        text += GetSystemsStatus() + "\n";
        
        // НОВОЕ: Активные таймеры
        text += GetActiveTimers() + "\n";
        
        // Задания
        text += "<b><size=36>ЗАДАНИЯ НА ДЕНЬ:</size></b>\n";
        
        if (dayEventManager.todayEvents.Count == 0)
        {
            text += "<color=yellow>Нет заданий на сегодня</color>\n";
        }
        else
        {
            int completed = 0;
            foreach (var task in dayEventManager.todayEvents)
            {
                string status = task.isCompleted ? "<color=green>✓</color>" : "<color=red>○</color>";
                string taskName = GetTaskName(task.eventName);
                
                text += $"{status} {taskName}\n";
                
                string progress = GetTaskProgress(task.eventName);
                if (!string.IsNullOrEmpty(progress))
                {
                    text += $"  {progress}\n";
                }
                
                if (task.isCompleted) completed++;
            }
            
            text += $"\n<b>Прогресс: {completed}/{dayEventManager.todayEvents.Count}</b>";
        }
        
        contentText.text = text;
    }
    
    // НОВОЕ: Получение статуса систем
    string GetSystemsStatus()
    {
        if (gameOverManager == null) return "";
        
        string status = "<b><size=36>СИСТЕМЫ КОРАБЛЯ:</size></b>\n";
        
        // Герметичность
        float hull = gameOverManager.hullIntegrity;
        string hullColor = hull > 50f ? "green" : (hull > 25f ? "yellow" : "red");
        status += $"<color={hullColor}>Герметичность: {hull:F0}%</color>\n";
        
        // Здоровье
        float health = gameOverManager.playerHealth;
        string healthColor = health > 50f ? "green" : (health > 25f ? "yellow" : "red");
        status += $"<color={healthColor}>Здоровье: {health:F0}/{gameOverManager.maxPlayerHealth:F0}</color>\n";
        
        // Кислород
        float oxygen = gameOverManager.oxygenLevel;
        string oxygenColor = oxygen > 50f ? "green" : (oxygen > 25f ? "yellow" : "red");
        status += $"<color={oxygenColor}>Кислород: {oxygen:F0}%</color>\n";
        
        // Крысы рядом
        int rats = gameOverManager.GetNearbyRatsCount();
        if (rats > 0)
        {
            status += $"<color=red>Крыс рядом: {rats}</color>\n";
        }
        
        return status;
    }
    
    // НОВОЕ: Получение активных таймеров
    string GetActiveTimers()
    {
        if (gameOverManager == null) return "";
        
        string timers = "";
        bool hasTimers = false;
        
        // Таймер управления
        var controlTimer = gameOverManager.GetControlTimer();
        if (controlTimer.isActive)
        {
            if (!hasTimers)
            {
                timers += "<b><size=36>КРИТИЧЕСКИЕ ТАЙМЕРЫ:</size></b>\n";
                hasTimers = true;
            }
            
            float remaining = controlTimer.GetRemainingTime();
            string color = remaining > 30f ? "yellow" : "red";
            timers += $"<color={color}>Панель управления: {FormatTime(remaining)}</color>\n";
        }
        
        // Таймер батареи
        var batteryTimer = gameOverManager.GetBatteryTimer();
        if (batteryTimer.isActive)
        {
            if (!hasTimers)
            {
                timers += "<b><size=36>КРИТИЧЕСКИЕ ТАЙМЕРЫ:</size></b>\n";
                hasTimers = true;
            }
            
            float elapsed = batteryTimer.currentTime;
            string color = elapsed < 15f ? "yellow" : "red";
            timers += $"<color={color}>Разряженная батарея: {elapsed:F0}с</color>\n";
        }
        
        return hasTimers ? timers : "";
    }
    
    // НОВОЕ: Форматирование времени
    string FormatTime(float seconds)
    {
        if (seconds < 0) return "00:00";
        
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        
        return $"{mins:D2}:{secs:D2}";
    }
    
    string GetTaskName(string eventName)
    {
        switch (eventName)
        {
            case "ClearRats": return "<b><size=36>Уничтожить крыс</size></b>";
            case "PatchHoles": return "<b><size=36>Залатать пробоины</size></b>";
            case "FixControls": return "<b><size=36>Починить панель управления</size></b>";
            case "ReplacePins": return "<b><size=36>Заменить пины</size></b>";
            case "ReplaceBattery": return "<b><size=36>Заменить батарею</size></b>";
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
                return $"Убито: {spawner.killedRats}/{spawner.ratsToKill}";
        }
        else if (eventName == "PatchHoles")
        {
            HoleSpawner spawner = FindObjectOfType<HoleSpawner>();
            if (spawner != null)
            {
                return spawner.GetProgressText();
            }
        }
        else if (eventName == "FixControls")
        {
            ControlPanelMalfunction panel = FindObjectOfType<ControlPanelMalfunction>();
            if (panel != null)
            {
                if (panel.IsCurrentlyFixed())
                    return "<color=green>Параметры в норме</color>";
                else
                    return "<color=yellow>Требуется настройка</color>";
            }
        }
        else if (eventName == "ReplacePins")
        {
            PinReplacementTask pins = FindObjectOfType<PinReplacementTask>();
            if (pins != null)
                return pins.GetProgressText();
        }
        else if (eventName == "ReplaceBattery")
        {
            BatteryReplacementEvent battery = FindObjectOfType<BatteryReplacementEvent>();
            if (battery != null)
                return battery.GetStatus();
        }
        
        return "";
    }
    
    // Публичные методы
    public void Show() { ShowMenu(); }
    public void Hide() { HideMenu(); }
    public void Toggle() { ToggleMenu(); }
    public bool IsVisible => isVisible;
}