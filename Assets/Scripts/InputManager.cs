using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class WorldSwitchButton : DualityES.Event
{
    public bool State = Input.GetButtonDown("World Switch");
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

    }

}