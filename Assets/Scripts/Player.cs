using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class Player : MonoBehaviour {

	public PlayerController controller;
	public Animator animator;

	public float runSpeed = 40f;

	float horizontalMove = 0f;
	bool jump = false;
	bool crouch = false;
	bool grabbed = false;
	bool dead = false;

	[Header("Grab & Pull"), Space(2)]
	[Range(0, 1), SerializeField] private float m_Distance = 1f;
	[SerializeField] private LayerMask m_ObstacleMask; 
	GameObject m_Obstacle;
	
	// Update is called once per frame
	public void Awake()
    {
        //EventSystem.instance.AddListener<PlayerDie>(Die);

    }
	


	// public void Die(PlayerDie playerDie)
    // {
    //     //isInControl = false;
    //     dead = true;
    //     //have to make sure player movement is no existent
    //     //rb.velocity = Vector3.zero;
        
    // }

    private void OnDisable()
    {
       // EventSystem.instance.RemoveListener<PlayerDie>(Die);
	}

	void Update () {


		Physics2D.queriesStartInColliders = false;
		RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * transform.localScale.x, m_Distance, m_ObstacleMask);
		Debug.DrawRay(transform.position, (Vector2)transform.position + Vector2.right * transform.localScale.x * m_Distance, Color.red);

		horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;


		if (Input.GetButtonDown("Jump"))
		{
			jump = true;
			animator.SetBool("isJumping", true);
		}

		if (Input.GetButtonDown("Crouch"))
		{
			crouch = true;
		} else if (Input.GetButtonUp("Crouch"))
		{
			crouch = false;
		}

        if (hit.collider != null && hit.collider.gameObject.tag == "ObstacleMovable" && Input.GetButtonDown("Grab"))
        {
			m_Obstacle = hit.collider.gameObject;
			m_Obstacle.GetComponent<FixedJoint2D>().enabled = true;
			m_Obstacle.GetComponent<Rigidbody2D>().mass = 1;
		}
		else if (Input.GetButtonUp("Grab"))
		{
			m_Obstacle = hit.collider.gameObject;
			m_Obstacle.GetComponent<FixedJoint2D>().enabled = false;
			m_Obstacle.GetComponent<Rigidbody2D>().mass = 100;
		}

		animator.SetFloat("speed", Mathf.Abs(horizontalMove));

	}

	public void OnLanding() 
	{
		animator.SetBool("isJumping", false);
	} 

	public void OnCrouch(bool isCrouching)
	{
		animator.SetBool("isCrouching",isCrouching);
	}

	void FixedUpdate ()
	{
		if ( Input.GetButtonDown("Grab") )
		{
			jump = false;
		}
		Debug.Log(crouch);
		controller.Move(horizontalMove * Time.fixedDeltaTime, crouch, jump);
		jump = false;
	}
}
