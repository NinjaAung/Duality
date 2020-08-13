using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public interface IGrabbable
{
    void Grab(Player playerGrabbing);

    void Release(Player playerReleasing);

}


public class Obstacle : MonoBehaviour, IGrabbable
{
    public enum ObstacleType
    {
        Regular,
        Boulder, // Breaks Brittle Walls
        Animated
    }

    //[SerializeField] private bool m_Pushable;
    //[SerializeField] private bool m_Pullable;
    [SerializeField] private bool isBox;


    private Animator animator;
    private AnimationClip m_FallingClip;
    private float m_animationDuration;
    private bool playingAnim = false;

    private GameObject DetachableObject;

    public ObstacleType type = ObstacleType.Regular;

    private Rigidbody2D rb;

    private Joint2D joint;

    /*
    public bool GetPushable()
    {
        return m_Pushable;
    }
    public bool GetPullable()
    {
        return m_Pullable;
    }
    */
    public void Awake()
    {

    }

    void Start()
    {
        //This doesn't work and it bothers me but yes
        //Debug.Log(ObstacleManager.Instance.Obstacles);
        //ObstacleManager.Instance.AddObstacle(gameObject);

        if (GetComponent<Animator>() != null)
        {
            animator = GetComponent<Animator>();
        }
        if (type == ObstacleType.Animated)
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag("Detachable"))
                {
                    DetachableObject = child.gameObject;
                }
            }
        }

        if (type.Equals(ObstacleType.Animated))
        {
            GetAnimationDuration();

        }
        else
        {
            
            rb = GetComponent<Rigidbody2D>();

            if (isBox) 
            {
                //Debug.Log("This is a box");
                rb.constraints = RigidbodyConstraints2D.FreezePositionX ;
            } else
            {
               //Debug.Log("This is not a box") ;
            }

            joint = GetComponent<Joint2D>();

            if (transform.root == GameManager.Instance.world1Push.m_World.transform)
            {
                joint.connectedBody = GameManager.Instance.playerPush.rb;

            }
            else
            {
                joint.connectedBody = GameManager.Instance.playerPull.rb;
            }
        }
        
    }


    public void Grab(Player player)
    {
        if (type.Equals(ObstacleType.Animated) == false)
        {
            //GetComponent<FixedJoint2D>().enabled = true;
            joint.enabled = true;
            rb.constraints = RigidbodyConstraints2D.None;
        }
        else if(playingAnim == false)
        {
            //Count until the player pulled for _ seconds
            animator.SetBool("isFalling", true);
            //Animate the Tree Fallin
            StartCoroutine(DetachObject(m_animationDuration));
            playingAnim = true;
            //Detach Object
        }
    }

    public void Release(Player player)
    {
        if (type.Equals(ObstacleType.Animated) == false)
        {
            //GetComponent<FixedJoint2D>().enabled = false;
            joint.enabled = false;
            if (isBox)
            {
                rb.constraints = RigidbodyConstraints2D.FreezePositionX;
            }
        }

    }

    public void OnFinishedAnimation()//Attach to the last keyframe Event (Animation Window)
    {
        Debug.Log("TestWhenAnimationFinish");
        Vector3 temp = DetachableObject.transform.position;
        Quaternion tempRot = DetachableObject.transform.rotation;
        Vector3 tempScale = DetachableObject.transform.lossyScale;

        //Vector3 temp2 = DetachableObject.transform.localPosition;


        DetachableObject.transform.parent = null;
        float playbackTime = animator.playbackTime;

        animator.Rebind();
        animator.playbackTime = playbackTime;

        DetachableObject.transform.parent = gameObject.transform.parent;

        DetachableObject.transform.position = temp;
        DetachableObject.transform.rotation= tempRot;
        DetachableObject.transform.localScale = tempScale;
        //DetachableObject.transform.position.z = 14f;

        DetachableObject.tag = "ObstacleMovable";
        var ObsCompnent = DetachableObject.GetComponent<Obstacle>();
        DetachableObject.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;

        ObsCompnent.enabled = true;
        //animator.enabled = false;
    }
    void GetAnimationDuration()
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            switch (clip.name)
            {
                case "TreeFallingLeft":
                    m_animationDuration = clip.length;
                    //Debug.Log(m_animationDuration);
                    break;
                default:
                    break;
            }
        }
    }

    private IEnumerator DetachObject(float animationDuration)
    {
        yield return new WaitForSeconds(animationDuration +3f);
        OnFinishedAnimation();
    }

    void Update()
    {

    }

}
