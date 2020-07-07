using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmbientSounds.Demo
{
    /// <summary>
    /// Component to allow the player to look at interactable things which then are highlighted
    /// and can be used by the player.
    /// </summary>
    public class LookAtController : MonoBehaviour
    {
        [Tooltip("The maximum distance at which interactable objects are highlighted & usable")]
        public float maxDistance;
        [Tooltip("The layers we perform the check for interactable objects on")]
        public LayerMask layers;
        
        float lastInteractionTimestamp = 0f;
        const float interactionThreshold = 0.5f;

        private Interactable currentInteractable;
        public Interactable CurrentInteractable
        {
            get
            {
                return currentInteractable;
            }

            set
            {
                if (currentInteractable != null && currentInteractable.highlighted)
                {
                    currentInteractable.StopHighlight();
                }

                currentInteractable = value;

                if (currentInteractable != null && !currentInteractable.highlighted)
                {
                    currentInteractable.StartHighlight();
                }
            }
        }

        private bool interactionAllowed = true;
        internal bool InteractionAllowed
        {
            get
            {
                return interactionAllowed;
            }

            set
            {
                //if there is an actual change in value reset the timestamp
                //this allows for a small threshold until interaction becomes possible again
                if (value != interactionAllowed)
                {
                    lastInteractionTimestamp = Time.time;
                }


                interactionAllowed = value;
                
            }
        }

        public bool InInteractionThreshold()
        {
            return Time.time < lastInteractionTimestamp + interactionThreshold;
        }


        public void Update()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, layers))
            {
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable != null)
                {
                    CurrentInteractable = interactable;
                }
                else
                {
                    CurrentInteractable = null;
                }
            }
            else
            {
                CurrentInteractable = null;
            }

            if (Input.GetMouseButton(0))
            {
                //Debug.Log("-----------");
                //Debug.Log(interactionAllowed.ToString());
                //Debug.Log((Time.time > (lastInteractionTimestamp + interactionThreshold)).ToString());
                //Debug.Log(Time.time.ToString());
                //Debug.Log(lastInteractionTimestamp.ToString());

                if (currentInteractable != null && 
                    currentInteractable.highlighted && 
                    InteractionAllowed &&
                   !InInteractionThreshold())
                {
                    lastInteractionTimestamp = Time.time;
                    currentInteractable.Interact();
                }
            }
        }
    }
}
