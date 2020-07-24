using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FixedJoint2D), typeof(Rigidbody2D), typeof(Collider2D))]
public class Obstacle : MonoBehaviour
{
    public enum ObstacleType
    {
        Regular,
        Boulder,
        FallingTree
    }

    [SerializeField] private bool m_Pushable;
    [SerializeField] private bool m_Pullable;

    public ObstacleType type;

    public bool GetPushable()
    {
        return m_Pushable;
    }
    public bool GetPullable()
    {
        return m_Pullable;
    }

}
