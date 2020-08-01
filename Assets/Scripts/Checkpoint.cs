using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private GameManager gm;

    void Start(){
        gm = GameManager.Instance;
    }
    void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player")){
            gm.lastCheckpointPos = transform.position;
        }
    } 
}
