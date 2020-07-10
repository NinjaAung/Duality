using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using DualityES;

public class PlayerController : MonoBehaviour
{	
	[Header("Player Controls"), Space(2)]
	[SerializeField] private float m_JumpForce = 400f;							// Amount of force added when the player jumps.
	[Range(0, 1), SerializeField] private float m_CrouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	[Range(0, .3f), SerializeField] private float m_MovementSmoothing = .05f;	// How much to smooth out the movement
	[SerializeField] private bool m_AirControl = false;							// Whether or not a player can steer while jumping;

	[Header("Collider Checks"), Space(2)]
	[SerializeField] private LayerMask m_WhatIsGround;							// A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;							// A position marking where to check if the player is grounded.
	[SerializeField] private Transform m_CeilingCheck;							// A position marking where to check for ceilings
	[SerializeField] private Collider2D m_CrouchDisableCollider;				// A collider that will be disabled when crouching



	[Header("Slope Checks "), Space(2)]
	[SerializeField] private float maxSlopeAngle;
	[SerializeField] private float SlopeCheckDistance;
	[SerializeField] private PhysicsMaterial2D noFriction;
    [SerializeField] private PhysicsMaterial2D fullFriction;
	private float xInput;

	private float slopeDownAngle;
    private float slopeSideAngle;
    private float lastSlopeAngle;
	private bool canWalkOnSlope;
	private bool isOnSlope;

	private Vector2 slopeNormalPerp;

	[Header("Grab & Pull"), Space(2)]
	[Range(0, 1), SerializeField] private float m_Distance = 1f;
	[SerializeField] private LayerMask m_ObstacleMask; 
	GameObject m_Obstacle;



	const float k_GroundedRadius = .2f; 					// Radius of the overlap circle to determine if grounded
	private bool m_Grounded;            					// Whether or not the player is grounded.
	const float k_CeilingRadius = .2f; 						// Radius of the overlap circle to determine if the player can stand up

	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  					// For determining which way the player is currently facing.
	private Vector3 velocity = Vector3.zero;

	private CapsuleCollider2D cc;
	private Vector2 ColliderSize;

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();
		cc = GetComponent<CapsuleCollider2D>();

		ColliderSize = cc.size;
	}

	#region Slope Calculations
	private void SlopeCheck()
	{
		Vector2 CheckPos = transform.position - new Vector3(0.0f, ColliderSize.y /  2);

		SlopeCheckVertical(CheckPos);
		SlopeCheckHorizontal(CheckPos);


	}

	 private void SlopeCheckHorizontal(Vector2 checkPos)
    {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, SlopeCheckDistance, m_WhatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, SlopeCheckDistance, m_WhatIsGround);

        if (slopeHitFront)
        {
            isOnSlope = true;

            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);

        }
        else if (slopeHitBack)
        {
            isOnSlope = true;

            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            slopeSideAngle = 0.0f;
            isOnSlope = false;
        }

    }

    private void SlopeCheckVertical(Vector2 checkPos)
    {      
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, SlopeCheckDistance, m_WhatIsGround);

        if (hit)
        {

            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;            

            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if(slopeDownAngle != lastSlopeAngle)
            {
                isOnSlope = true;
            }                       

            lastSlopeAngle = slopeDownAngle;
           
            Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
            Debug.DrawRay(hit.point, hit.normal, Color.green);

        }

        if (slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle)
        {
            canWalkOnSlope = false;
        }
        else
        {
            canWalkOnSlope = true;
        }

        if (isOnSlope && canWalkOnSlope && xInput == 0.0f)
        {
            m_Rigidbody2D.sharedMaterial = fullFriction;
        }
        else
        {
            m_Rigidbody2D.sharedMaterial = noFriction;
        }
    }
	#endregion


	private void FixedUpdate()
	{
		SlopeCheck();
		m_Grounded = false;

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
				m_Grounded = true;
		}
	}


	public void ObstacleGrab(bool grabed)
	{
		Physics2D.queriesStartInColliders = false;
		RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * transform.localScale.x, m_Distance, m_ObstacleMask);

		//Debug.DrawRay(transform.position, (Vector2)transform.position + Vector2.right * transform.localScale.x * m_Distance , Color.green);

		// if (hit.collider.gameObject.tag == "ObstacleMovable" && grabed) {
		// 	m_Obstacle = hit.collider.gameObject;
		// 	m_Obstacle.GetComponent<PositionConstraint>().enabled = true;

		// } else if (!grabed)
		// {
		// 	m_Obstacle.GetComponent<PositionConstraint>().enabled = false;
		// }
		

	}

	#region Movement
	public void Move(float move, bool crouch, bool jump)
	{
		// If crouching, check to see if the character can stand up
		if (!crouch)
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

		//only control the player if grounded or airControl is turned on
		if (m_Grounded || m_AirControl)
		{

			// If crouching
			if (crouch)
			{
				// Reduce the speed by the crouchSpeed multiplier
				move *= m_CrouchSpeed;

				// Disable one of the colliders when crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			} else
			{
				// Enable the collider when not crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;
			}

			// Move the character by finding the target velocity
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			// And then smoothing it out and applying it to the character
			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref velocity, m_MovementSmoothing);

			// If the input is moving the player right and the player is facing left...
			if (move > 0 && !m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (move < 0 && m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
		}
		// If the player should jump...
		if (m_Grounded && jump)
		{
			// Add a vertical force to the player.
			m_Grounded = false;
			m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
		}
	}


	private void Flip()
	{
		// Switch the way the player is labelled as facing.
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
	#endregion
}