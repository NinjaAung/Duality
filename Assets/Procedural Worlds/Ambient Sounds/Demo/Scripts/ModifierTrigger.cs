using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifierTrigger : MonoBehaviour {

    public string eventName = "";

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            AmbientSounds.AmbienceManager.ActivateEvent(eventName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            AmbientSounds.AmbienceManager.DeactivateEvent(eventName);
        }
    }

}
