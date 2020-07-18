using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class WorldSwitchButton : DualityES.Event
{
}

public class InputManager : MonoBehaviour
{

    protected static InputManager _instance;
    bool checkRestartButton = false;

    public bool pause = false;

    public static InputManager Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
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

        #region Pause
        if (Input.GetButtonDown("Pause"))
        {
            if (!pause)
            {
                //EventSystem.instance.RaiseEvent(new PauseGame { pause = true });
                pause = true;
            }
            else
            {
                //EventSystem.instance.RaiseEvent(new PauseGame { pause = false });
                pause = false;
            }
        }
        #endregion
        if(Input.GetButtonDown("World Switch"))
        {
            EventSystem.instance.RaiseEvent(new WorldSwitchButton { });
        }

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