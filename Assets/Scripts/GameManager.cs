using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

//Events associated with Game Manager
public class Judgment: DualityES.Event
{
    public float JudgmentScore;
}

public class WorldSwitching : DualityES.Event
{
    public Worlds targetWorld;
}

public enum Worlds
{
    Push,
    Pull,
    Peace
}


//A container class for all the GameObjects
[System.Serializable]
public class World {
    public GameObject m_World;
    public bool isActive;

    public void SetWorldStatus(bool status)
    {
        isActive = status;
        m_World.SetActive(status);
    }

}



public class GameManager: MonoBehaviour
{
    #region World Switching Variables 
    //The Worlds are assigned in the inspcetor
    public World world1Push;
    public World world2Pull;

    public Worlds currentWorld;
    
    [Range(1,100), Tooltip("In terms of seconds")]
    public float m_WorldSwitchCooldownTimer;

    private float currTimer;
    public  bool cooldownPassed = true;

    #endregion

    [Range(0,100)]
    public float m_Judgement;
    [Range(1f,10f),SerializeField]
    private float m_IncreaseRate = 0.01f;
    [Range(70, 100), SerializeField]
    private float m_JudgementOverloadValue = 70.0f;

    public bool pause;

    private void Awake()
    {
        currentWorld = Worlds.Push;
        //Setting the World 1 to be default on
        UpdatingWorldObjects(true);
        //Null Checker
        if(world1Push == null||world2Pull == null)
        {
            Debug.LogError("Unassgined World Variable");
        }

        //Listening to the Input Manager
        //On World Switch starts the other event that alerts the other listeners
        EventSystem.instance.AddListener<WorldSwitchButton>(OnWorldSwitch);
        //Listening to when the World Switches
        EventSystem.instance.AddListener<WorldSwitching>(WorldSwitch);

        EventSystem.instance.AddListener<ObjectContact>(NonNativeResponse);

        EventSystem.instance.AddListener<PauseGame>(Pause);

    }

    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<WorldSwitchButton>(OnWorldSwitch);
        EventSystem.instance.RemoveListener<WorldSwitching>(WorldSwitch);
        EventSystem.instance.RemoveListener<ObjectContact>(NonNativeResponse);

    }

    private void Update()
    {

        //Judgement Variable Update
        if (cooldownPassed)
        {
            m_Judgement += m_IncreaseRate * Time.deltaTime;
            EventSystem.instance.RaiseEvent(new Judgment { JudgmentScore = m_Judgement });
        }
        if(m_Judgement < 20)
        {
            AudioManager.instance.RemoveHeartBeat();
        }
        else
        {
            AudioManager.instance.PlayHeartBeat();
        }

        WSCooldownTimer(); // So they Don't spam the World Switching Mechanic
        JudgmentOverload();
    }

    void JudgmentOverload()
    {
        if(m_Judgement >= m_JudgementOverloadValue)
        {
            EventSystem.instance.RaiseEvent( new PlayerState { dead = true});
            //Debug.Log("Filler until program the Player death code");
        }
    }


    //When using the "non native" mechanic
    void NonNativeResponse(ObjectContact contact)
    {
        if(contact.contact == TypeOfContact.PullingObject)
        {
            if(currentWorld == Worlds.Push)
            {
                m_IncreaseRate += 2;
                Debug.Log("World Response");
            }
        }
        if (contact.contact == TypeOfContact.PushingObject)
        {
            if(currentWorld == Worlds.Pull)
            {
                m_IncreaseRate += 2;
                Debug.Log("World Response");

            }
        }

    }

    //When programming the inverse correlation between judgement and player speed
    //Add a <Judgement> listener in the Plyaer code to get the Judgement value and then
    //find a way to code an inverse relationship between the speed and the judgement

    // void OnPlayerDeath(PlayerDie player)
    // {
    //     //Either load the "save" feature or death UI play or directly take character to credit scene
    //     //Save
    //     //Or Death UI
    //     //Or Change it to the credit scene
    // }


    #region World Switching Functions
    void WSCooldownTimer()
    {
        if (cooldownPassed == false)
        {
            if (currTimer < m_WorldSwitchCooldownTimer)
            {
                currTimer += Time.deltaTime;
                Debug.Log(currTimer / m_WorldSwitchCooldownTimer);
                m_Judgement -= currTimer * .0047f;
                EventSystem.instance.RaiseEvent(new Judgment { JudgmentScore = m_Judgement });
            }
            else
            {
                cooldownPassed = true;
                currTimer = 0;
            }
        }
    }

    //Receiving that the Input that the world is going to change it "tells" the rest of the scripts
    //Was done this way so the Input doesn't have to tell determine which world the player currently is in
    //Depending on which world the current player is in the world changes accordingly
    private void OnWorldSwitch(WorldSwitchButton button)
    {
        if (cooldownPassed)
        {
            m_IncreaseRate = 0.1f;
            cooldownPassed = false;
            switch (currentWorld)
            {
                case Worlds.Push:
                    EventSystem.instance.RaiseEvent(new WorldSwitching { targetWorld = Worlds.Pull });
                    break;
                case Worlds.Pull:
                    EventSystem.instance.RaiseEvent(new WorldSwitching { targetWorld = Worlds.Push });
                    break;
                case Worlds.Peace:
                    break;
                default:
                    break;
                }
        }

    }

    //Updating which world the player is in.
    //Setting the world variables to be off or on, 
    private void WorldSwitch(WorldSwitching target)
    {
        //m_Judgement = 0;
       // m_IncreaseRate = 0.01f;
        switch (target.targetWorld)
        {
            case Worlds.Push:
                currentWorld = Worlds.Push;
                UpdatingWorldObjects(true);
                break;
            case Worlds.Pull:
                currentWorld = Worlds.Pull;
                UpdatingWorldObjects(false);
                break;
            case Worlds.Peace:
                break;
            default:
                break;
        }
    }


    //False -> Turning World 2 (Pull) on
    //True -> Turning World 1 (Push) on
    //Sets the turns the conatier objects on or off
    private void UpdatingWorldObjects(bool state)
    {
        world1Push.SetWorldStatus(state);
        bool opposite = !state;
        world2Pull.SetWorldStatus(opposite);
    }


    #endregion


    void Pause(PauseGame m_pause)
    {
        pause = !pause;
        EventSystem.instance.RaiseEvent(new PauseGameUI{ enabled = pause});

        if (pause)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

}