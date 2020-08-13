using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class EndPoint: MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (CheckpointSystem.finishedPullEndpoint && CheckpointSystem.finishedPushEndpoint)
        {
            EventSystem.instance.RaiseEvent(new ResetGameScene { });

        }
    }
}
