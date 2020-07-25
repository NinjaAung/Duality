using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AmbientSounds;

public class AudioManager : MonoBehaviour
{
    [Header("Player Footprints")]
    [SerializeField] private List<AudioClip> m_FootSteps;
     //private AudioClip[] m_FootstepSounds = new AudioClip[5];    // an array of footstep sounds that will be randomly selected from.

    public Sequence Heartbeat;

    public AudioSource m_AudioSource;

    private static AudioManager _instance = null;

    public static AudioManager instance //Ensures that this is the only instance in the class
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AudioManager();
            }
            return _instance;
        }
    }

    public void Awake()
    {
        m_AudioSource = gameObject.GetComponent<AudioSource>();

        m_FootSteps = new List<AudioClip>();
    }

    public void PlayFootStepAudio()
    {
        /*

        //if (!m_CharacterController.isGrounded)
        //{
        //    return;
        //}
        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, m_FootstepSounds.Length);
        m_AudioSource = gameObject.GetComponent<AudioSource>();
        m_AudioSource.clip = m_FootstepSounds[n];
        m_AudioSource.PlayOneShot(m_AudioSource.clip);
        // move picked sound to index 0 so it's not picked next time
        m_FootstepSounds[n] = m_FootstepSounds[0];
        m_FootstepSounds[0] = m_AudioSource.clip;

        */
    }

    public void StopFootStepAudio()
    {
        m_AudioSource.Pause();
    }

    public void PlayHeartBeat()
    {
        AmbienceManager.AddSequence(Heartbeat);
        AmbienceManager.ActivateEvent("Heartbeat");
    }
    public void RemoveHeartBeat()
    {
        AmbienceManager.RemoveSequence(Heartbeat);
        AmbienceManager.DeactivateEvent("Heartbeat");
    }
}
