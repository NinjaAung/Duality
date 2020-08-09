using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private List<AudioClip> m_FootSteps = new List<AudioClip>();
    [SerializeField] private AudioClip m_Landing;
    [SerializeField] private AudioClip m_Jumping;
    [SerializeField] private AudioClip m_Pushing;
    [SerializeField] private AudioClip m_Pulling;
    
    public AudioSource audioSrc;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void walking()
    {
        audioSrc.PlayOneShot(m_FootSteps[Random.Range(0,2)], Random.Range(0.30f,0.50f));
    }
    void jumping()
    {
        audioSrc.PlayOneShot(m_Jumping, 0.5f);
    }
    void landing()
    {
        audioSrc.PlayOneShot(m_Landing);
    }
    void pushing()
    {
        audioSrc.PlayOneShot(m_Pushing);
    }
    void pulling()
    {
        audioSrc.PlayOneShot(m_Pulling);
    }
}
