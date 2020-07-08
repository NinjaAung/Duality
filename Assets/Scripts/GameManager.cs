using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class Judgment: DualityES.Event
{
    public float JudgmentScore = 0.0f;
}

public enum Worlds
{
    Push,
    Pull,
    Peace
}
/*
    Place this ontop of the Input Manager
*/
public class WorldSwitchButton : DualityES.Event
{

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

public class WorldSwitching : DualityES.Event
{
    public Worlds targetWorld;
}

public class GameManager: MonoBehaviour
{
    //The World 1 is assigned in the world
    public World world1Push;
    public World world2Pull;

    public Worlds currentWorld;

    private void Awake()
    {
        currentWorld = Worlds.Push;
        UpdatingWorldObjects(true);

        //Listening to the Input Manager
        //On World Switch starts the other event that alerts the other listeners
        EventSystem.instance.AddListener<WorldSwitchButton>(OnWorldSwitch);
        //Listening to when the World Switches
        EventSystem.instance.AddListener<WorldSwitching>(WorldSwitch);

    }

    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<WorldSwitchButton>(OnWorldSwitch);
        EventSystem.instance.RemoveListener<WorldSwitching>(WorldSwitch);
    }

    //Receiving that the Input that the world is going to change it "tells" the rest of the scripts
    //Was done this way so the Input doesn't have to tell determine which world the player currently is in
    //Depending on which world the current player is in the world changes accordingly
    private void OnWorldSwitch(WorldSwitchButton button)
    {
        switch(currentWorld)
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

    //Updating which world the player is in.
    //Setting the world variables to be off or on, 
    private void WorldSwitch(WorldSwitching target)
    {
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

    /*
     * 
     * Place this code in the Input Manager to have it working. 

     Place this in the Unity Update in the Input Manager 
     
    if (Input.GetKeyDown(KeyCode.H))
    {
            EventSystem.instance.RaiseEvent(new WorldSwitchButton { });
            Debug.Log("Pressed the key :" + KeyCode.H);
        };

     * */

}