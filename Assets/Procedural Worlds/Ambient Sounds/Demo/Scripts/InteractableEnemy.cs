using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmbientSounds.Demo
{

    public class InteractableEnemy: Interactable {

        override public void Interact()
        {
            Destroy(gameObject);
        }
 
    }
}
