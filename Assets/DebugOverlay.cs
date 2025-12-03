using UnityEngine;

public class DebugOverlay : MonoBehaviour
{
    [Header("References")]
    public DayEventManager dayEventManager;
    public ChargingStation chargingStation;

    [Header("Debug Settings")]
    public bool showOverlay = true;
    public KeyCode toggleKey = KeyCode.F1;

    private GUIStyle headerStyle;
    private GUIStyle textStyle;
    private GUIStyle okStyle;
    private GUIStyle warnStyle;

    void Start()
    {
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.cyan;

        textStyle = new GUIStyle();
        textStyle.fontSize = 14;
        textStyle.normal.textColor = Color.white;

        okStyle = new GUIStyle(textStyle);
        okStyle.normal.textColor = Color.green;

        warnStyle = new GUIStyle(textStyle);
        warnStyle.normal.textColor = Color.red;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showOverlay = !showOverlay;
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 700), GUI.skin.box);

        GUILayout.Label("=== DEBUG INFO ===", headerStyle);
        GUILayout.Space(10);

        // -------- DAY EVENTS --------
        if (dayEventManager != null)
        {
            GUILayout.Label($"Day: {dayEventManager.currentDay}", textStyle);
            GUILayout.Label(dayEventManager.GetDayProgress(), textStyle);

            GUILayout.Space(4);
            GUILayout.Label("Events:", textStyle);

            foreach (var e in dayEventManager.todayEvents)
            {
                var style = e.isCompleted ? okStyle : textStyle;
                GUILayout.Label($"  {(e.isCompleted ? "✓" : "○")} {e.eventName}", style);
            }

            if (dayEventManager.AreAllEventsCompleted())
            {
                GUILayout.Space(5);
                GUILayout.Label("ALL TASKS COMPLETE", okStyle);
            }
            else
            {
                GUILayout.Label("Tasks remaining...", warnStyle);
            }
        }

        GUILayout.Space(15);

        // -------- CHARGING --------
        if (chargingStation != null)
        {
            GUILayout.Label("Charging:", textStyle);

            GUILayout.Label(
                $"Plugged in: {(chargingStation.IsPluggedIn() ? "YES" : "NO")}",
                chargingStation.IsPluggedIn() ? okStyle : warnStyle
            );

            GUILayout.Label(
                $"Charging: {(chargingStation.IsCharging() ? "YES" : "NO")}",
                chargingStation.IsCharging() ? okStyle : warnStyle
            );

            GUILayout.Label(
                $"Can Unplug: {(chargingStation.CanUnplug() ? "YES" : "NO")}",
                chargingStation.CanUnplug() ? okStyle : textStyle
            );
        }

        GUILayout.Space(15);

        GUILayout.Label("Hotkeys:", headerStyle);
        GUILayout.Label("[F1] - Toggle overlay", textStyle);

        GUILayout.EndArea();
    }
}
