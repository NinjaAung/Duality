using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class NextSceneAct : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        EventSystem.instance.RaiseEvent(new SceneLoadNext { });
    }
}
