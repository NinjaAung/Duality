using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

namespace AmbientSounds.Demo {

    public class DemoPanelController : MonoBehaviour
    {
        public LookAtController lac;
        public FirstPersonController firstPersonController;
        public GameObject panel;
        public Light directionalLight;
        public Slider dayNightSlider;
        public Text message;
        public Text header;

        public void Start()
        {
            ShowMessage(header.text, message.text, false);
        }


        public void ShowMessage(string title, string text, bool showSlider = false)
        {
            header.text = title;
            message.text = text.Replace("<br>","\n");
            panel.SetActive(true);
            lac.InteractionAllowed = false;
            firstPersonController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (showSlider)
            {
                dayNightSlider.transform.parent.gameObject.SetActive(true);
                dayNightSlider.value = 0;
            }
            else
            {
                dayNightSlider.transform.parent.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (dayNightSlider.IsActive())
            {
                UpdateDayNightSlider();
            }
        }

        public void HideMessage()
        {
            panel.SetActive(false);
            lac.InteractionAllowed = true;
            firstPersonController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            //Reset Day / Night
            dayNightSlider.value = 0;
            UpdateDayNightSlider();
            
        }

        void UpdateDayNightSlider()
        {
            AmbientSounds.AmbienceManager.SetValue("Time of Day", dayNightSlider.value);
            Vector3 euler = directionalLight.transform.rotation.eulerAngles;
            directionalLight.transform.rotation = Quaternion.Euler(new Vector3(Mathf.Lerp(65.5f, -20f, dayNightSlider.value), euler.y, euler.z));
            RenderSettings.ambientIntensity = Mathf.Lerp(1.25f, 0f, dayNightSlider.value);
        }

    }
}
