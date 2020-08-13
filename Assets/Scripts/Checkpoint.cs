using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;


public class EndpointChecked : DualityES.Event
{
}

public class Checkpoint : MonoBehaviour
{

    public enum CheckpointType
    {
        Regular,
        Endpoint
    }


    [SerializeField] private CheckpointType m_CheckpointType = CheckpointType.Regular;
    private float specialDuration;

    [Header("Particle")]
    [SerializeField] private ParticleSystem particleSystem;
    [Header("Sound")]
    [SerializeField] private AudioSource audioSrc;
    bool isChecked = false;


    void Start()
    {
        if (particleSystem.isPlaying){
            particleSystem.Stop();
        }
    }
    private void OnEnable()
    {
        if (isChecked == false)
        {
            if (particleSystem.isPlaying)
            {
                particleSystem.Stop();
            }
        }
    }

    void EndpointEffect()
    {
        //People can add thier own spefical vfx if they want
    }

    void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player") && other.GetComponent<Player>()){

            isChecked = true;
            particleSystem.Play();
            //gm.lastCheckpointPos = transform.position;
            if (audioSrc.isPlaying) {
                return;
            }
            audioSrc.Play();
            if (transform.root == GameManager.Instance.world2Pull.m_World.transform)
            {
                CheckpointSystem.pullLastCheckpointPos = other.gameObject.transform.position;
                
            }
            else
            {
                CheckpointSystem.pushLastCheckpointPos = other.gameObject.transform.position;
            }

            if (m_CheckpointType == CheckpointType.Endpoint)
            {
                if(CheckpointSystem.finishedPullEndpoint == false)
                {
                    if (transform.root == GameManager.Instance.world2Pull.m_World.transform)
                    {
                        CheckpointSystem.finishedPullEndpoint = true;
                        EventSystem.instance.RaiseEvent(new EndpointChecked { });

                    }

                }
                if (CheckpointSystem.finishedPushEndpoint == false)
                {
                    if (transform.root == GameManager.Instance.world1Push.m_World.transform)
                    {
                        CheckpointSystem.finishedPushEndpoint = true;
                        EventSystem.instance.RaiseEvent(new EndpointChecked { });

                    }
                }
                


                EndpointEffect();

            }

        }
    } 
}
