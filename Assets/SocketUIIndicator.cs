using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SocketUIIndicator : MonoBehaviour {

    public SocketDetector socket;
    public Image indicatorImage;

    public Color occupiedColor = Color.green;
    public Color freeColor = Color.red;

    void Start() {
        if (socket == null) return;

        socket.OnOccupied.AddListener(() => SetColor(true));
        socket.OnFreed.AddListener(() => SetColor(false));

        // Стартовое состояние
        SetColor(socket.IsOccupied);
    }

    void SetColor(bool occupied) {
        if (indicatorImage != null) {
            indicatorImage.color = occupied ? occupiedColor : freeColor;
        }
    }
}
