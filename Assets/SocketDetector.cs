using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SocketDetector : MonoBehaviour {

    [Tooltip("Какие объекты считаем допустимыми (по имени префаба)")]
    public string allowedObjectName = "";

    [Header("События")]
    public UnityEvent OnOccupied;
    public UnityEvent OnFreed;

    public bool IsOccupied => currentObject != null;
    
    // Отслеживаем конкретный объект в сокете
    private GameObject currentObject;

    void OnTriggerEnter(Collider other) {
        if (!IsAllowed(other.gameObject)) return;
        
        // Если сокет уже занят - игнорируем
        if (currentObject != null) return;
        
        currentObject = other.gameObject;
        OnOccupied?.Invoke();
    }

    void OnTriggerExit(Collider other) {
        if (!IsAllowed(other.gameObject)) return;
        
        // Освобождаем только если выходит тот же объект, что и занял
        if (other.gameObject == currentObject) {
            currentObject = null;
            OnFreed?.Invoke();
        }
    }

    bool IsAllowed(GameObject obj) {
        if (string.IsNullOrEmpty(allowedObjectName))
            return true;

        return obj.name.StartsWith(allowedObjectName);
    }
    
    /// <summary>
    /// Возвращает объект, занимающий сокет (или null)
    /// </summary>
    public GameObject GetOccupyingObject() {
        return currentObject;
    }
    
    /// <summary>
    /// Принудительно обновляет состояние (полезно после спавна)
    /// </summary>
    public void ForceRefresh() {
        // Проверяем, действительно ли currentObject всё ещё в триггере
        if (currentObject != null) {
            Collider col = currentObject.GetComponent<Collider>();
            if (col == null || !GetComponent<Collider>().bounds.Intersects(col.bounds)) {
                currentObject = null;
                OnFreed?.Invoke();
            }
        }
    }
}