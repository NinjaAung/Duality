using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Particle")]
    public ParticleSystem particleSystem;
    void Start()
    {
        if (particleSystem.isPlaying){
            particleSystem.Stop();
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player") && other.GetComponent<Player>()){
            Debug.Log("Enabiling Effect");
            particleSystem.Play();
            //gm.lastCheckpointPos = transform.position;
            if (transform.root == GameManager.Instance.world2Pull.m_World.transform)
            {
                CheckpointSystem.pullLastCheckpointPos = other.gameObject.transform.position;
                
            }
            else
            {
                CheckpointSystem.pushLastCheckpointPos = other.gameObject.transform.position;
            }
        }
    } 
}
