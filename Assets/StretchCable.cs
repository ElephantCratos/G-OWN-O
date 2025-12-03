using UnityEngine;

public class StretchCable : MonoBehaviour {
    public Transform startPoint;   // точка на стене
    public Transform endPoint;     // точка на предмете / в руке игрока
    public LineRenderer line;
    public int curveSegments = 20; // насколько гладкий изгиб
    public float sagAmount = 0.2f; // провисание

    void Update() {
        if (line == null || startPoint == null || endPoint == null)
            return;

        Vector3 p0 = startPoint.position;
        Vector3 p2 = endPoint.position;

        // Средняя точка — пусть немного провисает вниз
        Vector3 mid = (p0 + p2) * 0.5f;
        mid.y -= sagAmount;

        // Интерполяция кривой Безье
        line.positionCount = curveSegments;

        for (int i = 0; i < curveSegments; i++) {
            float t = i / (float)(curveSegments - 1);

            Vector3 point =
                Mathf.Pow(1 - t, 2) * p0 +
                2 * (1 - t) * t * mid +
                Mathf.Pow(t, 2) * p2;

            line.SetPosition(i, point);
        }
    }
}
