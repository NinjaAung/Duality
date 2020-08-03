using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private List<AudioClip> m_FootSteps = new List<AudioClip>();
    [SerializeField] private List<AudioClip> m_Jumping = new List<AudioClip>();
    [SerializeField] private List<AudioClip> m_Landing = new List<AudioClip>();
    [SerializeField] private List<AudioClip> m_Pushing = new List<AudioClip>();
    [SerializeField] private List<AudioClip> m_Pulling = new List<AudioClip>();
    
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
        audioSrc.PlayOneShot(m_FootSteps[Random.Range(0,2)], Random.Range(0.50f,0.80f));
    }
    void jumping()
    {
        audioSrc.PlayOneShot(m_FootSteps[Random.Range(0,2)], Random.Range(0.50f,0.80f));
    }
    void landing()
    {
        audioSrc.PlayOneShot(m_FootSteps[Random.Range(0,2)], Random.Range(0.50f,0.80f));
    }
    void pushing()
    {
        audioSrc.PlayOneShot(m_FootSteps[Random.Range(0,2)], Random.Range(0.50f,0.80f));
    }
    void pulling()
    {
        audioSrc.PlayOneShot(m_FootSteps[Random.Range(0,2)], Random.Range(0.50f,0.80f));
    }
}
