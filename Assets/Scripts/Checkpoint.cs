using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Particle")]
    [SerializeField] private ParticleSystem particleSystem;
    [Header("Sound")]
    [SerializeField] private AudioSource audioSrc;

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
        }
    } 
}
