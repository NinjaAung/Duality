using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private ParticleSystem[] childrenParticleSytems;
    bool disabledRelevantPSEmissions = true;

    void Start()
    {
        childrenParticleSytems = gameObject.GetComponentsInChildren<ParticleSystem>();
    }

    void Update()
    {
         // Process each child's particle system and disable its emission module.
         // For each child, we disable all emission modules of its children.
        if( disabledRelevantPSEmissions )
        {
            foreach( ParticleSystem childPS in childrenParticleSytems )
            {
                // Get the emission module of the current child particle system [childPS].
                ParticleSystem.EmissionModule childPSEmissionModule = childPS.emission;
                // Disable the child's emission module.
                childPSEmissionModule.enabled = false;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player") && other.GetComponent<Player>()){
            Debug.Log("Enabiling Effect");
            foreach( ParticleSystem childPS in childrenParticleSytems )
            {
                // Get the emission module of the current child particle system [childPS].
                ParticleSystem.EmissionModule childPSEmissionModule = childPS.emission;
                // Disable the child's emission module.
                childPSEmissionModule.enabled = true;
            }
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
