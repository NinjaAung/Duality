using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class BoulderCrush : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Die Dammit");
            EventSystem.instance.RaiseEvent(new PlayerState { dead = true });
        }

    }
}
