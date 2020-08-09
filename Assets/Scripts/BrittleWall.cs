using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrittleWall : MonoBehaviour
{

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ObstacleMovable"))
        {
            if (collision.gameObject.GetComponent<Obstacle>())
            {
                if (collision.gameObject.GetComponent<Obstacle>().type.Equals(Obstacle.ObstacleType.Boulder))
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }

}
