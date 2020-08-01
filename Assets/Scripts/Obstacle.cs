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

    [SerializeField] private bool m_Pushable;
    [SerializeField] private bool m_Pullable;


    private Animator animator;
    private AnimationClip m_FallingClip;
    private float m_animationDuration;


    private GameObject DetachableObject;

    public ObstacleType type = ObstacleType.Regular;

    public bool GetPushable()
    {
        return m_Pushable;
    }
    public bool GetPullable()
    {
        return m_Pullable;
    }

    public void Awake()
    {
        if(GetComponent<Animator>() != null)
        {
            animator = GetComponent<Animator>();
        }
        if(type == ObstacleType.Animated)
        {
            foreach(Transform child in transform)
            {
                if (child.CompareTag("Detachable"))
                {
                    DetachableObject = child.gameObject;
                }
            }
        }
    }

    void Start()
    {
        //This doesn't work and it bothers me but yes
        //Debug.Log(ObstacleManager.Instance.Obstacles);
        //ObstacleManager.Instance.AddObstacle(gameObject);


        if (type.Equals(ObstacleType.Animated))
        {
            GetAnimationDuration();

        }
    }



    public void Grab(Player player)
    {
        if (type.Equals(ObstacleType.Animated) == false)
        {
            //GetComponent<FixedJoint2D>().enabled = true;
            GetComponent<Joint2D>().enabled = true;
            GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
        }
        else
        {
            //Count until the player pulled for _ seconds
            animator.SetBool("isFalling", true);
            //Animate the Tree Fallin
            StartCoroutine(DetachObject(m_animationDuration));
            //Detach Object
        }
    }

    public void Release(Player player)
    {
        if (type.Equals(ObstacleType.Animated) == false)
        {
            //GetComponent<FixedJoint2D>().enabled = false;
            GetComponent<Joint2D>().enabled = false;
            GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePositionX;
        }

    }

    public void OnFinishedAnimation()//Attach to the last keyframe Event (Animation Window)
    {
        Debug.Log("TestWhenAnimationFinish");
        Vector3 temp = DetachableObject.transform.position;
        Vector3 temp2 = DetachableObject.transform.localPosition;


        //DetachableObject.transform.parent = transform.parent;
        //DetachableObject.transform.position = temp2;
        //DetachableObject.transform.position.z = 14f;

        DetachableObject.tag = "ObstacleMovable";
        var ObsCompnent = DetachableObject.GetComponent<Obstacle>();
        ObsCompnent.enabled = true;
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
        yield return new WaitForSeconds(animationDuration + 1);
        OnFinishedAnimation();
    }

    void Update()
    {

    }

}
