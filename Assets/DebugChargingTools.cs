using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugChargingTools : MonoBehaviour
{
    public DayEventManager eventManager;
    public ChargingStation chargingStation;

    [Header("Gizmos Settings")]
    public bool showGizmos = true;
    public Color plugPointColor = Color.cyan;
    public Color plugRadiusColor = new Color(0f, 1f, 1f, 0.2f);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos || chargingStation == null) return;

        if (chargingStation.cablePlugPoint != null)
        {
            // Позиция подключения
            Gizmos.color = plugPointColor;
            Gizmos.DrawSphere(chargingStation.cablePlugPoint.position, 0.03f);

            // Радиус подключения
            Gizmos.color = plugRadiusColor;
            Gizmos.DrawWireSphere(chargingStation.cablePlugPoint.position, chargingStation.plugDistance);
        }

        // Исходная позиция кабеля
        if (chargingStation.chargingCable != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                chargingStation.chargingCable.transform.position,
                Vector3.one * 0.04f
            );
        }
    }
#endif
}
