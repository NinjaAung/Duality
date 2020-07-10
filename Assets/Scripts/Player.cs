using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class Player : MonoBehaviour {

	public PlayerController controller;

	public float runSpeed = 40f;

	float horizontalMove = 0f;
	bool jump = false;
	bool crouch = false;
	bool grabbed = false;
	bool dead = false;
	
	// Update is called once per frame
	public void Awake()
    {
        EventSystem.instance.AddListener<PlayerDie>(Die);

    }
	

	public class PlayerDie : DualityES.Event
	{
    
	}





	public void Die(PlayerDie playerDie)
    {
        //isInControl = false;
        dead = true;
        //have to make sure player movement is no existent
        //rb.velocity = Vector3.zero;
        
    }

    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<PlayerDie>(Die);
	}

	void Update () {

		horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

		if (Input.GetButtonDown("Jump"))
		{
			jump = true;
		}

		if (Input.GetButtonDown("Crouch"))
		{
			crouch = true;
		} else if (Input.GetButtonUp("Crouch"))
		{
			crouch = false;
		}

        if (Input.GetButtonDown("Grab"))
        {
			grabbed = true;

        } else if (Input.GetButtonUp("Grab"))
		{
			grabbed = false;
		}


	}

	void FixedUpdate ()
	{
		if (grabbed == true)
		{
			jump = false;
		}
		controller.Move(horizontalMove * Time.fixedDeltaTime, crouch, jump);
		jump = false;
		//controller.ObstacleGrab(grabbed);
	}
}
