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
    #region Copped from the Low Poly Animal Pack: Wander Script - All the differnet States
    //Ideally we have a animation manager so we don't have to do it here?
    [Header("Animation States"), Space(5)]
    [SerializeField]
    private IdleState[] idleStates;
    [SerializeField]
    private MovementState[] movementStates;
    [SerializeField]
    private AnimalState[] attackingStates;
    [SerializeField]
    private AnimalState[] deathStates;
    //To see Gizmos
    [SerializeField, Tooltip("If true, gizmos will be drawn in the editor.")]
    private readonly bool showGizmos = true;

    #endregion

    //Player Components
    private Animator animator;
    private Rigidbody rb;
    public Vector3 centerOfMass;


    //Used to Calcuate the player's movement
    Vector3 input;
    public Vector3 steeringThrust;
    Vector3 forwardThrust;
    float forwardThrustAmount;

    //States
    bool isThrusting = false;
    bool isBoosting = false;
    [SerializeField]
    private bool isHiding = false;
    [SerializeField]
    private bool dead = false; // --> Also used for player & game state

    private bool isInControl = true;
    private bool isPauseOn = false;

    [Header("Player Settings"), Space(5)]
    //Speed Values
    public float speed = 4f;
    public float boostSpeed = 6f;
    public float maxSpeed = 2f;

    public float boostTime;
    float currBoostTime;
    public float boostLimit;
    float boostCount;
    public float boostCoolDown;
    public float currCoolTime;

    public float turnForceHorzintal = 3; // This is used in calculating player movement too
    public float turnForceVertical;
    public float rotMax;
    public float rotMin;

    [Header("Gameplay Stats"), Space(5)]
    //Player Life Variables
    [SerializeField]
    public float health = 100;
    [SerializeField]
    private float hunger = 100;

    public void Awake()
    {
        //Checks if there is an animation added
        if (idleStates.Length == 0 && movementStates.Length == 0)
        {
            Debug.LogError(string.Format("{0} has no idle or movement states state.", gameObject.name));
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody>();
        centerOfMass = rb.centerOfMass;
        animator = GetComponent<Animator>();

        animator.applyRootMotion = false;

        EventSystem.instance.AddListener<MouseClickedData>(UponMouseClick);
        EventSystem.instance.AddListener<KeyboardPressed>(KeyboardInput);
        EventSystem.instance.AddListener<PlayerDie>(Die);
        EventSystem.instance.AddListener<OnHealth>(UpdateHealth);
        EventSystem.instance.AddListener<CollectableEvent>(GotCollectable);
        EventSystem.instance.AddListener<OnDismissCollectable>(DismissCollectable);

        EventSystem.instance.AddListener<PauseGame>(PauseControl);

    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<MouseClickedData>(UponMouseClick);
        EventSystem.instance.RemoveListener<KeyboardPressed>(KeyboardInput);
        EventSystem.instance.RemoveListener<PlayerDie>(Die);
        EventSystem.instance.RemoveListener<OnHealth>(UpdateHealth);
        EventSystem.instance.RemoveListener<CollectableEvent>(GotCollectable);
        EventSystem.instance.RemoveListener<OnDismissCollectable>(DismissCollectable);

        EventSystem.instance.RemoveListener<PauseGame>(PauseControl);
    }

    void MoveAnimation(bool state)
    {
        if (!string.IsNullOrEmpty(movementStates[0].animationBool))
        {
            animator.SetBool(movementStates[0].animationBool, state);

            if(animator.GetBool(movementStates[0].animationBool))
            {
                //Debug.Log("Should be moving");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        EventSystem.instance.RaiseEvent(new PlayerPosition { position = this.transform.position });
        EventSystem.instance.RaiseEvent(new UpdatePlayerUI { healhpoints = health, hungerpoints = hunger });
        DeathCheck();
        
    }

    //Is called multiple time per frame, use for physics
    void FixedUpdate()
    {
//        Debug.Log(transform.rotation.eulerAngles.x);
        if (isInControl && isPauseOn == false)
        {
            if (!dead)
            {
                // Restricts player movement so only on input
                if (isThrusting)
                {
                    //Makes sure to update forwardThrustAmount before applying
                    Boost();
                    forwardThrust = Vector3.forward * forwardThrustAmount;

                    rb.AddRelativeForce(forwardThrust, ForceMode.Force);
                }

                Turn();
            }
        }

    }

    #region Input Functions
    void KeyboardInput(KeyboardPressed keyboardPressedData)
    {
        if (isInControl && isPauseOn == false)
        {
            if (!dead && !GameManager.Instance.pause)
            {
                input.Set(
                 keyboardPressedData.horizontal,
                 keyboardPressedData.vertical,
                 0.0f
                );

                steeringThrust = input.normalized;
                steeringThrust.x *= turnForceHorzintal;
                steeringThrust.y *= turnForceVertical;
            }
        }

    }

    void UponMouseClick(MouseClickedData mouseClickedData)
    {
        if (isInControl && isPauseOn == false)
        {
            if (mouseClickedData.clicked && !dead && !GameManager.Instance.pause)
            {

                if (!isBoosting)
                {
                    forwardThrustAmount = speed;
                }
                else
                {
                    forwardThrustAmount = boostSpeed;
                }
                //Animation
                isThrusting = true;
            }
            else
            {
                forwardThrustAmount = 0;
                isThrusting = false;
            }
        }

    }
    #endregion

    #region 
    public void Die(PlayerDie playerDie)
    {
        isInControl = false;
        dead = true;
        //have to make sure player movement is no existent
        rb.velocity = Vector3.zero;

        //have to disable all player animations other than death
        foreach (AnimalState state in idleStates)
        {
            if (!string.IsNullOrEmpty(state.animationBool))
            {
                animator.SetBool(state.animationBool, false);
            }
        }

        foreach (AnimalState state in movementStates)
        {
            if (!string.IsNullOrEmpty(state.animationBool))
            {
                animator.SetBool(state.animationBool, false);
            }
        }

        foreach (AnimalState state in attackingStates)
        {
            if (!string.IsNullOrEmpty(state.animationBool))
            {
                animator.SetBool(state.animationBool, false);
            }
        }

        if (!string.IsNullOrEmpty(deathStates[0].animationBool))
        {
            animator.SetBool(deathStates[0].animationBool, true);
        }

        //Have to collect death data to trigger specific death sequence
        switch (playerDie.causeOfDeath)
        {
            case CausesOfDeath.Acidification:
                EventSystem.instance.RaiseEvent(new DeathUI() {
                    cause = CausesOfDeath.Acidification
                });
                break;
            case CausesOfDeath.Deadzones:
                EventSystem.instance.RaiseEvent(new DeathUI() {  cause = CausesOfDeath.Deadzones });
                break;
            case CausesOfDeath.Eaten:
                EventSystem.instance.RaiseEvent(new DeathUI() { cause = CausesOfDeath.Eaten });
                break;
            case CausesOfDeath.Fishnets:
                EventSystem.instance.RaiseEvent(new DeathUI() {  cause = CausesOfDeath.Fishnets });
                break;
            case CausesOfDeath.Starvation:
                EventSystem.instance.RaiseEvent(new DeathUI() { cause = CausesOfDeath.Starvation });
                break;
            default:
                break;
        }
        AmbienceManager.ActivateEvent("LowerBackground");

    }
    #endregion

    public void UpdateHealth(OnHealth onHealth)
    {
        health += onHealth.deltaHealthAmt;
    }

    void DeathCheck() {
        if (health <= 0 || Input.GetKey(KeyCode.X))
        {
            dead = true;
        }

        if (!dead)
        {
            MoveAnimation(isThrusting);
        }
        else
        {
            EventSystem.instance.RaiseEvent(new PlayerDie() { causeOfDeath = CausesOfDeath.Acidification});
        }
    }
    void GotCollectable(CollectableEvent collectable)
    {
        isInControl = false;
    }
    void DismissCollectable(OnDismissCollectable onDismiss)
    {
        isInControl = true;
    }
    void PauseControl(PauseGame pauseGame)
    {
        isPauseOn = pauseGame.pause;
    }
}
