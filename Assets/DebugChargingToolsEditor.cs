#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DebugChargingTools))]
public class DebugChargingToolsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DebugChargingTools dbg = (DebugChargingTools)target;

        GUILayout.Space(10);
        GUILayout.Label("=== TASK DEBUG ===", EditorStyles.boldLabel);

        if (dbg.eventManager != null)
        {
            if (GUILayout.Button("Complete ALL tasks"))
                CompleteAll(dbg);

            if (GUILayout.Button("Complete ClearRats"))
                dbg.eventManager.CompleteEvent("ClearRats");

            if (GUILayout.Button("Complete PatchHoles"))
                dbg.eventManager.CompleteEvent("PatchHoles");

            GUILayout.Space(10);
        }

        GUILayout.Label("=== CHARGING DEBUG ===", EditorStyles.boldLabel);

        if (dbg.chargingStation != null)
        {
            if (GUILayout.Button("Simulate Plug Cable"))
            {
                SimulatePlug(dbg);
            }

            if (GUILayout.Button("Simulate Unplug Cable"))
            {
                SimulateUnplug(dbg);
            }

            if (GUILayout.Button("Force Start New Day"))
            {
                dbg.eventManager.StartNewDay();
            }
        }
    }

    void CompleteAll(DebugChargingTools dbg)
    {
        foreach (var ev in dbg.eventManager.todayEvents)
            dbg.eventManager.CompleteEvent(ev.eventName);
    }

    void SimulatePlug(DebugChargingTools dbg)
    {
        var cs = dbg.chargingStation;

        cs.SendMessage("PlugInCable", SendMessageOptions.DontRequireReceiver);
    }

    void SimulateUnplug(DebugChargingTools dbg)
    {
        var cs = dbg.chargingStation;

        cs.SendMessage("UnplugCable", SendMessageOptions.DontRequireReceiver);
    }
}
#endif
