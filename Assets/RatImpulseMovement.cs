using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RatImpulseMovement : MonoBehaviour
{
    public float moveForce = 5f;              // Сила импульса при движении
    public float directionChangeInterval = 2f;// Как часто менять направление
    public float pauseBetweenSteps = 0.5f;    // Задержка между "рывками"
    public float obstacleCheckDistance = 0.6f;// Расстояние для проверки препятствий
    public float turnSpeed = 360f;            // Скорость поворота тела к новому направлению

    private Rigidbody rb;
    private Vector3 moveDir;
    private float directionTimer;
    private float stepTimer;
    private bool canStep = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.drag = 2f; // чтобы крыса не скользила слишком долго
        PickNewDirection();
    }

    void Update()
    {
        directionTimer -= Time.deltaTime;

        // Проверка препятствий
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, transform.forward, obstacleCheckDistance))
        {
            PickNewDirection(true);
        }

        // Периодическая смена направления
        if (directionTimer <= 0f)
        {
            PickNewDirection();
        }

        // Поворот тела крысы в сторону движения
        if (moveDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // Импульсное движение
        stepTimer -= Time.deltaTime;
        if (canStep && stepTimer <= 0f)
        {
            Step();
            stepTimer = pauseBetweenSteps;
        }
    }

    void Step()
    {
        canStep = false;
        rb.AddForce(moveDir * moveForce, ForceMode.Impulse);
        Invoke(nameof(EnableStep), pauseBetweenSteps);
    }

    void EnableStep()
    {
        canStep = true;
    }

    void PickNewDirection(bool forceDifferent = false)
    {
        directionTimer = directionChangeInterval;
        Vector3 newDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

        if (forceDifferent && Vector3.Dot(newDir, moveDir) > 0.5f)
        {
            newDir = -moveDir;
        }

        moveDir = newDir;
    }

    // Отладка луча
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + transform.forward * obstacleCheckDistance);
    }
}