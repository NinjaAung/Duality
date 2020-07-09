using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class CheckForRestart : DualityES.Event
{

}

public class KeyboardPressed : DualityES.Event
{
    public float horizontal = Input.GetAxis("Horizontal");
    public float vertical = Input.GetAxis("Vertical");
}


public class WorldSwitchButton : DualityES.Event
{

}

public class InputManager : MonoBehaviour
{

    protected static InputManager _instance;
    bool checkRestartButton = false;
    //public bool inverted = false;
    private int m_invert = 1;

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
    

        EventSystem.instance.AddListener<CheckForRestart>(CheckRestartButton);
    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<CheckForRestart>(CheckRestartButton);
    }


    private void Update()
    {
        EventSystem.instance.RaiseEvent(new KeyboardPressed
        {
            horizontal = Input.GetAxis("Horizontal"),
            vertical = Input.GetAxis("Vertical")
        });
        if (Input.GetKeyDown(KeyCode.K))
        {
            EventSystem.instance.RaiseEvent(new WorldSwitchButton { });

        };

        #region Pause
        if (Input.GetKey(KeyCode.P) || Input.GetKey(KeyCode.Joystick1Button3))
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

    void CheckRestartButton(CheckForRestart restart)
    {
        checkRestartButton = true;
    }
}
