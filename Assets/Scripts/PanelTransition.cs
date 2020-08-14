using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;
using TMPro;

public class PanelTransition : MonoBehaviour
{

    //TMP code found here
    //https://stackoverflow.com/questions/56031067/using-coroutines-to-fade-in-out-textmeshpro-text-element

    [SerializeField] private TextMeshProUGUI m_ContinueText;
    //[SerializeField] private bool fadeIn = false;
//    [SerializeField] private bool fadeOnStart = false;
    [SerializeField] private float timeMultiplier;
    private bool FadeIncomplete = false;


    private bool m_pressedOnce = false;

    private void Start()
    {
        m_ContinueText = GetComponent<TextMeshProUGUI>();
        if (m_ContinueText == null)
        {
            Debug.LogError("Missing m_ContinueText");
            return;
        }
        FadeInText(3f);

    }



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnSpacePressed();
        }

    }

    void OnSpacePressed()
    {
        if (m_pressedOnce == false )
        {
            FadeOutText(-1f);

            m_pressedOnce = true;
        }


    }


    private IEnumerator FadeInText(float timeSpeed, TextMeshProUGUI text)
    {
        text.color = new Color(text.color.r, text.color.g, text.color.b, 0);
        while (text.color.a < 1.0f)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, text.color.a + (Time.deltaTime * timeSpeed));
            yield return null;
        }
    }
    private IEnumerator FadeOutText(float timeSpeed, TextMeshProUGUI text)
    {
        text.color = new Color(text.color.r, text.color.g, text.color.b, 1);
        while (text.color.a > 0.0f)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, text.color.a - (Time.deltaTime * timeSpeed));
            yield return null;
        }

        EventSystem.instance.RaiseEvent(new SceneLoadNext { });

    }
    public void FadeInText(float timeSpeed = -1.0f)
    {
        if (timeSpeed <= 0.0f)
        {
            timeSpeed = timeMultiplier;
        }
        StartCoroutine(FadeInText(timeSpeed, m_ContinueText));
    }
    public void FadeOutText(float timeSpeed = -1.0f)
    {
        if (timeSpeed <= 0.0f)
        {
            timeSpeed = timeMultiplier;
        }
        StartCoroutine(FadeOutText(timeSpeed, m_ContinueText));
    }


}
