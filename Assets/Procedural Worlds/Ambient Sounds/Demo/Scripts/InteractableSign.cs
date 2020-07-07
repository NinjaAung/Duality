using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmbientSounds.Demo
{

   

    public class InteractableSign : Interactable {

        public string title = "";
        [TextArea]
        public string message = "";
        public bool showSlider = false;
        DemoPanelController dpc = null;


        new public void Start()
        {
            base.Start();
            dpc = GameObject.Find("UI").GetComponent<DemoPanelController>();
        }


        override public void Interact()
        {
            if (dpc != null)
            {
                dpc.ShowMessage(title,message, showSlider);
            }
        }

 
    }
}
