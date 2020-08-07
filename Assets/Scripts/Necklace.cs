using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class Necklace : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.GetComponent<Player>())
        {
            EventSystem.instance.RaiseEvent(new WorldSwitchButton { });

        }
    }

}
