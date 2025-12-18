using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BNG
{
    /// <summary>
    /// Объединённая панель отладки для всех систем игры
    /// </summary>
    public class DebugChargingTools : MonoBehaviour
    {
        [Header("Ссылки - Старые")]
        public DayEventManager eventManager;
        public ChargingStation chargingStation;
        
        [Header("Ссылки - Game Over System")]
        public GameOverManager gameOverManager;
        public TextMeshProUGUI statusText;
        
        [Header("Настройки отладки")]
        public bool showDebugPanel = true;
        public KeyCode toggleKey = KeyCode.F1;
        
        [Header("Gizmos Settings")]
        public bool showGizmos = true;
        public Color plugPointColor = Color.cyan;
        public Color plugRadiusColor = new Color(0f, 1f, 1f, 0.2f);
        
        private bool isPanelVisible = true;
        
        void Update()
        {
            // Переключение панели отладки
            if (Input.GetKeyDown(toggleKey))
            {
                isPanelVisible = !isPanelVisible;
                if (statusText != null)
                    statusText.gameObject.SetActive(isPanelVisible);
            }
            
            // Обновление текста статуса
            if (isPanelVisible && statusText != null && gameOverManager != null)
            {
                UpdateStatusText();
            }
        }
        
        void UpdateStatusText()
        {
            string status = "═══ СИСТЕМЫ КОРАБЛЯ ═══\n\n";
            
            // Hull Integrity
            status += $"🛡️ ГЕРМЕТИЧНОСТЬ: {gameOverManager.hullIntegrity:F1}%\n";
            if (gameOverManager.holeSpawner != null)
            {
                int holes = gameOverManager.holeSpawner.GetActiveHolesCount();
                status += $"   Активных дыр: {holes}\n";
            }
            status += "\n";
            
            // Player Health
            status += $"❤️ ЗДОРОВЬЕ: {gameOverManager.playerHealth:F0}/{gameOverManager.maxPlayerHealth:F0}\n";
            if (gameOverManager.ratSpawner != null)
            {
                int rats = gameOverManager.ratSpawner.GetAliveRatsCount();
                status += $"   Живых крыс: {rats}\n";
            }
            status += "\n";
            
            // Oxygen
            status += $"💨 КИСЛОРОД: {gameOverManager.oxygenLevel:F1}%\n";
            if (gameOverManager.attachModel != null)
            {
                int pins = gameOverManager.attachModel.GetInsertedPinsCount();
                int total = gameOverManager.attachModel.InsertPoints.Count;
                status += $"   Пинов вставлено: {pins}/{total}\n";
            }
            status += "\n";
            
            // Control Malfunction
            if (gameOverManager.controlMalfunctionTimer > 0)
            {
                float timeLeft = gameOverManager.maxControlMalfunctionTime - gameOverManager.controlMalfunctionTimer;
                status += $"⚠️ ПОТЕРЯ УПРАВЛЕНИЯ: {timeLeft:F0}с\n\n";
            }
            else
            {
                status += $"✓ Управление: OK\n\n";
            }
            
            // Battery Emergency
            if (gameOverManager.batteryEmptyTimer > 0)
            {
                status += $"⚠️ БЕЗ БАТАРЕИ: {gameOverManager.batteryEmptyTimer:F0}с\n";
                status += $"   Новая дыра через: {(gameOverManager.holeSpawnInterval - (gameOverManager.batteryEmptyTimer % gameOverManager.holeSpawnInterval)):F0}с\n\n";
            }
            else
            {
                status += $"✓ Батарея: OK\n\n";
            }
            
            // Day Info
            if (gameOverManager.dayEventManager != null)
            {
                status += $"📅 День: {gameOverManager.dayEventManager.currentDay}\n";
                status += gameOverManager.dayEventManager.GetDayProgress();
            }
            
            status += "\n\n[F1] - Скрыть/Показать";
            
            statusText.text = status;
        }
        
        
        public void ResetAllSystems()
        {
            if (gameOverManager != null)
                gameOverManager.ResetAllSystems();
        }
        
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
}