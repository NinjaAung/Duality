using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DualityES;
using TMPro;

public class PauseGameUI : DualityES.Event
{
    public bool enabled;
}
public class UIManager : MonoBehaviour
{
    public Canvas deathCanvas;
    public Canvas pauseCanvas;
    public Canvas endCanvas;

    public TextMeshProUGUI endCanvaText;

    private void Awake()
    {
        EventSystem.instance.AddListener<PlayerState>(OnPlayerDeath);
        EventSystem.instance.AddListener<PauseGameUI>(PauseGameUI);
        EventSystem.instance.AddListener<EndSceneEvent>(OnEndSceneUI);

    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<PlayerState>(OnPlayerDeath);
        EventSystem.instance.RemoveListener<PauseGameUI>(PauseGameUI);
        EventSystem.instance.RemoveListener<EndSceneEvent>(OnEndSceneUI);
    }

    private void Start()
    {
        if(endCanvas== null)
        {
            return;
        }
        endCanvas.enabled = false;
    }

    private void OnPlayerDeath(PlayerState playerState)
    {
        if (playerState.dead)
        {
            deathCanvas.enabled = true;
        }
        else
        {
            deathCanvas.enabled = false;
        }
    }

    private void PauseGameUI(PauseGameUI pause)
    {
        if (pause.enabled)
        {
            pauseCanvas.enabled = true;
        }
        else
        {
            pauseCanvas.enabled = false;
        }
    }

    private void OnEndSceneUI(EndSceneEvent endSceneEvent)
    {
        endCanvas.enabled = true;
        if (endCanvaText ==null)
        {
            return;
        }
        StartCoroutine(TextUtl.FadeInText(endSceneEvent.m_animationDuration, endCanvaText));

    }

}
