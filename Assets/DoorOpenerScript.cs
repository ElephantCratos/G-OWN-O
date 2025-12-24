using System.Collections;
using UnityEngine;

public class DoorOpenerScript : MonoBehaviour
{
    private Animator animator;
    private bool isPlayerInside = false;

    void Start()
    {
        animator = GetComponentInParent<Animator>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            StartCoroutine(OpenDoor());
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            animator.Play("door_2_close");
        }
    }

    IEnumerator OpenDoor()
    {
        animator.Play("glass_door_open");
        
        // Ждём окончания анимации открытия
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        
        // Устанавливаем состояние "открыто"
        if (isPlayerInside) // Проверяем, что игрок всё ещё внутри
        {
            animator.Play("door_opened");
        }
    }
}