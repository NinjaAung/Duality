using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;

    public float jump_force = 0.0f;
    public float side_force = 0.0f;

    private float current_speed;

    // Start is called before the first frame update

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        current_speed = 0.0f; 
        if ( Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) )
        {
            current_speed = side_force;
        } 
        else if ( Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) )
        {
            current_speed = -side_force;
        }
    
    }

    // Fixed Update is prefered for calculating physics
    void FixedUpdate() 
    {   
        rb.AddForce(new Vector2(current_speed * Time.deltaTime, 0.0f));
        if ( Input.GetKey(KeyCode.Space) ) 
        {
            rb.AddForce(new Vector2(0.0f, jump_force));
        }
    }
}
