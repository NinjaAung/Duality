using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sound_test : MonoBehaviour
{
    [SerializeField] private AudioSource audioSrc;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player"))
        {
            if (audioSrc.isPlaying) {
                return;
            }
            audioSrc.Play();
        }
    } 
}
