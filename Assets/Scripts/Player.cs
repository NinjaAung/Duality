﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;


public enum TypeOfContact
{
    PushingObject,
    PullingObject
}
public class PlayerState : DualityES.Event
{
    public bool dead;
}

public class ObjectContact : DualityES.Event
{
    public TypeOfContact contact;
}


public class Player : MonoBehaviour {

	public PlayerController controller;
	public Animator animator;

	public float runSpeed = 40f;

	float horizontalMove = 0f;
	bool jump = false;
	bool crouch = false;
	public bool grab = false;
	bool dead = false;




	[Header("Grab & Pull"), Space(2)]
	[Range(0, 1), SerializeField] private float m_Distance = 1f;
	[SerializeField] private LayerMask m_ObstacleMask; 
	public GameObject m_Obstacle;
	
	public void Awake()
    {
        EventSystem.instance.AddListener<PlayerState>(ControllerToggle);
        EventSystem.instance.AddListener<JumpButton>(OnJump);
        EventSystem.instance.AddListener<GrabButton>(OnGrab);
        EventSystem.instance.AddListener<MovementInput>(GetHorizontal);

    }



    public void ControllerToggle(PlayerState playerDie)
    {
        //isInControl = false;
        dead = playerDie.dead;
        //have to make sure player movement is no existent
        //rb.velocity = Vector3.zero;

    }

    private void OnDisable()
    {
       EventSystem.instance.RemoveListener<PlayerState>(ControllerToggle);
       EventSystem.instance.RemoveListener<JumpButton>(OnJump);
       EventSystem.instance.RemoveListener<GrabButton>(OnGrab);
       EventSystem.instance.RemoveListener<MovementInput>(GetHorizontal);
    }

	void Update () {

        bool grabbedObject = false;

		Physics2D.queriesStartInColliders = false;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * transform.localScale.x, m_Distance, m_ObstacleMask);
        //Checks if it's on the right or the left
        // May have to change code if the skelton affects the localScale.x value
        //RaycastHit2D rightRayHit = Physics2D.Raycast(transform.position, Vector2.right , m_Distance, m_ObstacleMask);
		RaycastHit2D leftRayHit = Physics2D.Raycast(transform.position, Vector2.right * -1 , m_Distance, m_ObstacleMask);
		Debug.DrawRay(transform.position, (Vector2)transform.position + Vector2.right * transform.localScale.x * m_Distance, Color.red);

	

        //Crouch
		    //if (Input.GetButtonDown("Crouch"))
		    //{
		    //	crouch = true;
		    //} else if (Input.GetButtonUp("Crouch"))
		    //{
		    //	crouch = false;
		    //}

        //Grab 
        if(hit.collider != null)
        {
            if (hit.collider.gameObject.tag == "ObstacleMovable" && grab)
            {
                grabbedObject = true;
                m_Obstacle = hit.collider.gameObject;
                AttachObstacle(true);
                Debug.Log("Should Pick Up");

            }
            else if (grab == false)
            {
                grabbedObject = false;
                m_Obstacle = hit.collider.gameObject;
                AttachObstacle(false);

            }
        }

        if (grabbedObject)
        {
            PushOrPull(leftRayHit, horizontalMove);
        }

		animator.SetFloat("speed", Mathf.Abs(horizontalMove));

	}
    //Determiens if the Object is on the right or left and then determines if the character is moving to the left or the right
    public void PushOrPull(RaycastHit2D leftcast, float horizontalInput)
    {
        float horizontal= System.Math.Sign(horizontalInput);
        bool objectOnRight;

        if (leftcast.collider)
        {
            objectOnRight = false;
        }
        else
        {
            objectOnRight = true;
        }
        if(horizontal != 0)
        {
            if (objectOnRight && (horizontal == -1))
            {
                if (m_Obstacle.GetComponent<Obstacle>().GetPullable() == false)
                {
                    AttachObstacle(false);
                    return;
                }
                //Debug.Log(" Pulling to the left");
                EventSystem.instance.RaiseEvent(new ObjectContact { contact = TypeOfContact.PullingObject });
            }
            else if (objectOnRight && (horizontal == 1))
            {
                if (m_Obstacle.GetComponent<Obstacle>().GetPushable() == false)
                {
                    AttachObstacle(false);
                    return;
                }
                //Debug.Log(" Pushing to the right");
                EventSystem.instance.RaiseEvent(new ObjectContact { contact = TypeOfContact.PushingObject });
            }
            else if (objectOnRight == false && horizontal == -1)
            {
                if (m_Obstacle.GetComponent<Obstacle>().GetPushable() == false)
                {
                    AttachObstacle(false);
                    return;
                }
                //Debug.Log("Push to the left");
                EventSystem.instance.RaiseEvent(new ObjectContact { contact = TypeOfContact.PushingObject});

            }
            else if (objectOnRight == false && horizontal == 1)
            {
                if (m_Obstacle.GetComponent<Obstacle>().GetPullable() == false)
                {
                    AttachObstacle(false);
                    return;
                }
                //Debug.Log(" Pulling to the right");
                EventSystem.instance.RaiseEvent(new ObjectContact { contact = TypeOfContact.PullingObject });

            }
        }

    }

    public void AttachObstacle(bool attach)
    {
        if (attach)
        {
            m_Obstacle.GetComponent<FixedJoint2D>().enabled = true;
            m_Obstacle.GetComponent<Rigidbody2D>().mass = 1;
        }
        else
        {
            m_Obstacle.GetComponent<FixedJoint2D>().enabled = false;
            m_Obstacle.GetComponent<Rigidbody2D>().mass = 100;
        }
    }
    
    public void OnGrab(GrabButton _grab)
    {
        grab = _grab.grab;
        if (grab)
        {
            Debug.Log("grab it hoe");
        }
        
    }
    public void OnJump(JumpButton _jump)
    {
        //not necessary to use _jump in function, only here to use function in event system
        jump = true;
        animator.SetBool("isJumping", true);
    }

	public void OnLanding() 
	{
		animator.SetBool("isJumping", false);
	} 

	public void OnCrouch(bool isCrouching)
	{
		animator.SetBool("isCrouching",isCrouching);
	}

    public void GetHorizontal(MovementInput _horiz)
    {
        horizontalMove = _horiz.movInput * runSpeed;
    }

	void FixedUpdate ()
	{
        if (grab)
        {
            jump = false;
        }
        if (dead == false)
        {
            controller.Move(horizontalMove * Time.fixedDeltaTime, crouch, jump);
        }
        jump = false;

    }
}
