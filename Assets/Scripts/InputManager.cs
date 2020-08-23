using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;


#region Input Events
public class PauseGame : DualityES.Event
{
}
public class WorldSwitchButton : DualityES.Event
{
}

public class JumpButton : DualityES.Event
{
}

public class GrabButton : DualityES.Event
{
    public bool grab;
}

public class MovementInput : DualityES.Event
{
    public float movInput;
}
#endregion

public class InputManager : MonoBehaviour
{
    public AudioSource audioSrc;

    protected static InputManager _instance;
    [SerializeField] private AudioClip m_WorldSwitch;
    //bool checkRestartButton = false;

    public static InputManager Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        //Singleton
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }



    private void OnDisable()
    {
    }


    private void Update()
    {

        if (Input.GetButtonDown("Pause"))
        {
            //triggers update pause variable in game manager script
            EventSystem.instance.RaiseEvent(new PauseGame { });
        }
    

        if(Input.GetButtonDown("World Switch"))
        {
            EventSystem.instance.RaiseEvent(new WorldSwitchButton { });
            audioSrc.PlayOneShot(m_WorldSwitch, 0.5f);
            
        }

        if (Input.GetButtonDown("Jump"))
        {
            EventSystem.instance.RaiseEvent(new JumpButton { });
        }

        if (Input.GetButton("Grab"))
        {
            EventSystem.instance.RaiseEvent(new GrabButton { grab = true });
        }
        else
        {
            EventSystem.instance.RaiseEvent(new GrabButton { grab = false });
        }

        //really insists on getting movement input etc. from the input manager for when we have to start
        //taking into account different axes from diferent controllers
        //for example, in the future if needed we can convert this input manager to an abstract class
        //if (Input.GetAxisRaw("Horizontal") != null)
        //{
        //}
        EventSystem.instance.RaiseEvent(new MovementInput { movInput = Input.GetAxisRaw("Horizontal") });

        //if (checkRestartButton)
        //{
        //    if (Input.GetKeyDown(KeyCode.R))
        //    {
        //        checkRestartButton = false;
        //        EventSystem.instance.RaiseEvent(new ReloadScene{ });
        //    }
        //}

    }


    private void CheckForState(PlayerState playerState)
    {
        if (playerState.dead == true)
        {
            //checkRestartButton = true;
        }
    }

}