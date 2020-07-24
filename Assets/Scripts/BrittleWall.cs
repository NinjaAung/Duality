using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrittleWall : MonoBehaviour
{

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision Contact");
        if (collision.gameObject.CompareTag("ObstacleMovable"))
        {
            Debug.Log("Obstacle Contact");
            if (collision.gameObject.GetComponent<Rigidbody2D>().mass > 7)
            {
                Destroy(this.gameObject);
            }
        }
    }

}
