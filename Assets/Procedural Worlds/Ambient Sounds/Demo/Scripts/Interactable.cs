using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmbientSounds.Demo
{
    public class Interactable : MonoBehaviour
    {
        Renderer myRenderer;
        [Tooltip("Color used when highlighting")]
        public Color highlightColor = Color.white;
        internal bool highlighted;

        public void Start()
        {
            myRenderer = GetComponent<Renderer>();
        }

        public void StartHighlight()
        {
            if (myRenderer != null)
            {
                myRenderer.material.EnableKeyword("_EMISSION");
                float intensity =  Mathf.InverseLerp(0f,255f,highlightColor.a);
                myRenderer.material.SetColor("_EmissionColor", highlightColor * intensity * 100); 
                highlighted = true;
            }
        }

        public void StopHighlight()
        {
            if (myRenderer != null)
            {
                myRenderer.material.SetColor("_EmissionColor", Color.black);
                myRenderer.material.DisableKeyword("_EMISSION");
                highlighted = false;
            }
        }

        public virtual void Interact()
        {
            throw new NotImplementedException();
        }
    }
}
