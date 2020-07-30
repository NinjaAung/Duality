using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class DeathTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            EventSystem.instance.RaiseEvent(new PlayerState { dead = true });
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            EventSystem.instance.RaiseEvent(new PlayerState { dead = true });
        }
    }

}
