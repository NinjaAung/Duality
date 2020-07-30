using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

public class GrabbingObject : DualityES.Event
{
    public bool grabbing;
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

    private GameManager gm;




	[Header("Grab & Pull"), Space(2)]
	[Range(0, 1), SerializeField] private float m_Distance = 1f;
	[SerializeField] private LayerMask m_ObstacleMask; 
	public GameObject m_Obstacle;


    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        transform.position = gm.lastCheckpointPos;
    }
	
	public void OnEnable()
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
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * transform.localScale.x *-1, m_Distance, m_ObstacleMask);
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
                //Debug.Log("Should Pick Up");

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

        if(Input.GetKeyDown(KeyCode.R)){
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

	}

    //Determiens if the Object is on the right or left and then determines if the character is moving to the left or the right
    public void PushOrPull(RaycastHit2D leftcast, float horizontalInput)
    {
        bool objectOnRight;

        float horizontal = System.Math.Sign(horizontalInput);

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
            if (objectOnRight && horizontal < 0)
            {
                //if (m_Obstacle.GetComponent<Obstacle>().GetPullable() == false)
                //{
                //    AttachObstacle(false);
                //    return;
                //}
                //Debug.Log(" Pulling to the left");
                OnPull();

                EventSystem.instance.RaiseEvent(new ObjectContact { contact = TypeOfContact.PullingObject });
            }
            else if (objectOnRight && (horizontal == 1))
            {
                //Debug.Log(" Pushing to the right");
                OnPush();
                EventSystem.instance.RaiseEvent(new ObjectContact { contact = TypeOfContact.PushingObject });
            }
            else if (objectOnRight == false && horizontal == -1)
            {
                // Debug.Log("Push to the left");
                OnPush();

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
                OnPull();
                EventSystem.instance.RaiseEvent(new ObjectContact { contact = TypeOfContact.PullingObject });

            }
            else
            {
                DisableBothAnim();

            }
        }
        else
        {
            DisableBothAnim();
        }

    }

    public void AttachObstacle(bool attach)
    {
        if (attach)
        {
            EventSystem.instance.RaiseEvent(new GrabbingObject { grabbing = true });
            m_Obstacle.GetComponent<Obstacle>().Grab(this);
            //m_Obstacle.GetComponent<FixedJoint2D>().enabled = true;
            //m_Obstacle.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
        }
        else
        {
            EventSystem.instance.RaiseEvent(new GrabbingObject { grabbing = false });
            DisableBothAnim();
            m_Obstacle.GetComponent<Obstacle>().Release(this);
            //m_Obstacle.GetComponent<FixedJoint2D>().enabled = false;
            //m_Obstacle.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePositionX;
        }
    }
    
    public void OnGrab(GrabButton _grab)
    {
        grab = _grab.grab;
        if (grab)
        {
            //Debug.Log("grab it hoe");
        }
        
    }
    public void OnJump(JumpButton _jump)
    {
        //not necessary to use _jump in function, only here to use function in event system
        jump = true;
        animator.SetBool("isJumping", jump);
    }

	public void OnLanding() 
	{
        jump = false;
		animator.SetBool("isJumping", jump);
	} 

    private void OnPush()
    {
        animator.SetBool("pull", false);
        animator.SetBool("push", true);
    }
    private void OnPull()
    {
        animator.SetBool("push", false);
        animator.SetBool("pull", true);
    }

    private void DisableBothAnim()
    {
        animator.SetBool("push", false);
        animator.SetBool("pull", false);
    }

    /*
	public void OnCrouch(bool isCrouching)
	{
		animator.SetBool("isCrouching",isCrouching);
	}
    */
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
