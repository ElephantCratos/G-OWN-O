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

    public bool IsOccupied { get; private set; }

    void OnTriggerEnter(Collider other) {
        if (IsAllowed(other.gameObject)) {
            IsOccupied = true;
            OnOccupied?.Invoke();
        }
    }

    void OnTriggerExit(Collider other) {
        if (IsAllowed(other.gameObject)) {
            IsOccupied = false;
            OnFreed?.Invoke();
        }
    }

    bool IsAllowed(GameObject obj) {
        if (allowedObjectName == "")
            return true;

        return obj.name.StartsWith(allowedObjectName);
    }
}
