using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public enum CausesOfDeath
{
    Deadzones,
    Hunger,
    Starvation,
    Fishnets,
    Eaten,
    Acidification
}

public class UpdatePlayerUI : DualityES.Event
{
    public float healhpoints;
    public float hungerpoints;
}
public class OnHealth : DualityES.Event
{
    public float deltaHealthAmt;
}
public class PlayerDie : DualityES.Event
{
    public CausesOfDeath causeOfDeath;
}
public class BeginRitualToEnterHabitat : DualityES.Event
{
    public bool onEnter;
}
public class PlayerPosition : DualityES.Event
{
    public Vector3 position;
}

public class Player : MonoBehaviour
{


    [SerializeField]
    private bool dead = false; // --> Also used for player & game state

    private bool isInControl = true;
    private bool isPauseOn = false;

    [Header("Player Settings"), Space(2)]
    public Rigidbody2D rb;

    public float jump_force = 0.0f;
    public float side_force = 0.0f;

    private float current_speed;


    [Header("Gameplay Stats"), Space(2)]
    //Player Life Variables
    [SerializeField]
    public float health = 100;

    public void Awake()
    {
        EventSystem.instance.AddListener<KeyboardPressed>(KeyboardInput);
        EventSystem.instance.AddListener<PlayerDie>(Die);

    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<KeyboardPressed>(KeyboardInput);
        EventSystem.instance.RemoveListener<PlayerDie>(Die);

    }

    // Update is called once per frame
    void Update()
    {
        DeathCheck();
        
    }

    //Is called multiple time per frame, use for physics
    void FixedUpdate()
    {

    }
    void KeyboardInput(KeyboardPressed keyboardPressedData)
    {
        current_speed = 0.0f;

    }
    public void Die(PlayerDie playerDie)
    {
        //isInControl = false;
        dead = true;
        //have to make sure player movement is no existent
        //rb.velocity = Vector3.zero;
        
    }


    void DeathCheck() {

    }


}
