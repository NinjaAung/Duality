using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;
public class SaveableObject : MonoBehaviour
{
    private void Start()
    {
        EventSystem.instance.RaiseEvent(new UpdateObstacleList { obstacle = gameObject });
    }

}
