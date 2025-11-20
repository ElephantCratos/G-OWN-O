using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRTooltips
{

  public class TooltipTrigger : MonoBehaviour
    {
        [Header("Tooltip Prefab")]
        [SerializeField] private GameObject tooltipPrefab;
        
        [Header("Content")]
        [SerializeField] private string tooltipTitle = "Grab";
        [SerializeField] private string tooltipDescription = "Press grip to grab object";
        [SerializeField] private Sprite tooltipIcon;
        
        [Header("Spawn Settings")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0, 1f, 0);
        [SerializeField] private bool spawnOnStart = true;
        
        private GameObject tooltipInstance;
        private VRTooltip tooltip;

        void Start()
        {
            if (spawnOnStart)
            {
                SpawnTooltip();
            }
        }

 public void SpawnTooltip()
{
    if (tooltipPrefab == null)
    {
        Debug.LogWarning("Tooltip prefab not assigned on " + gameObject.name);
        return;
    }

    tooltipInstance = Instantiate(tooltipPrefab, transform);
    
    // Принудительно сбросить ВСЕ Transform'ы внутри
    Transform[] allTransforms = tooltipInstance.GetComponentsInChildren<Transform>();

// Сначала сохраняем все скейлы
Dictionary<Transform, Vector3> savedScales = new Dictionary<Transform, Vector3>();

foreach (Transform t in allTransforms)
{
    if (t == tooltipInstance.transform) continue;
    savedScales[t] = t.localScale;
}

// Потом применяем изменения
foreach (Transform t in allTransforms)
{
    Debug.LogWarning(t.localScale);
    if (t == tooltipInstance.transform) continue;
    
    t.localPosition = Vector3.zero;
    t.localRotation = Quaternion.identity;
    t.localScale = savedScales[t];
}
    
    // Теперь установить правильную позицию root'а
    tooltipInstance.transform.localPosition = spawnOffset;
    tooltipInstance.transform.localRotation = Quaternion.identity;
    tooltipInstance.transform.localScale = Vector3.one;
    
    tooltip = tooltipInstance.GetComponent<VRTooltip>();
    if (tooltip != null)
    {
        tooltip.SetContent(tooltipTitle, tooltipDescription, tooltipIcon);
    }
}
        public void DestroyTooltip()
        {
            if (tooltipInstance != null)
            {
                Destroy(tooltipInstance);
            }
        }

        void OnDestroy()
        {
            DestroyTooltip();
        }
    }
}

