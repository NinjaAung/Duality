using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;


public class UpdatePlayerUI : DualityES.Event
{

}

public class OnHealth : DualityES.Event
{
    public float deltaHealthAmt;
}
public class PlayerDie : DualityES.Event
{
    
}

public class Player : MonoBehaviour
{


    [SerializeField]
    private bool dead = false; // --> Also used for player & game state

    private bool isInControl = true;
    private bool isPauseOn = false;

    private float current_speed;

    private float distancetoGround = 0.0f;


    private int jump_count = 0;

    [Header("Player Settings"), Space(2)]
    private Rigidbody2D rb;
    public float jump_force = 0.0f;
    public float side_force = 0.0f;




    [Header("Gameplay Stats"), Space(2)]
    //Player Life Variables
    [SerializeField]
    public float health = 100;

    public void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        EventSystem.instance.AddListener<PlayerDie>(Die);

    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<PlayerDie>(Die);

    }

    public bool IsGrounded()
    {
        return Physics2D.Raycast(transform.position,Vector2.down);
    }


    public void Die(PlayerDie playerDie)
    {
        //isInControl = false;
        dead = true;
        //have to make sure player movement is no existent
        //rb.velocity = Vector3.zero;
        
    }


    void DeathCheck() {

    }
    
    // Update is called once per frame
    void Update()
    {
        DeathCheck();
        
    }

    //Is called multiple time per frame, use for physics
    void FixedUpdate()
    {
        bool isGrounded = IsGrounded();
        rb.AddForce(Vector2.right * current_speed * Time.deltaTime);

        if (Input.GetKeyUp(KeyCode.Space)) {
            rb.AddForce(Vector2.up * jump_force * Time.deltaTime, ForceMode2D.Impulse);
        }     
    }

}
