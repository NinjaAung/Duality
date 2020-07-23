using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DualityES;

public class PauseGameUI : DualityES.Event
{
    public bool enabled;
}
public class UIManager : MonoBehaviour
{
    public Canvas deathCanvas;
    public Canvas pauseCanvas;

    private void Awake()
    {
        EventSystem.instance.AddListener<PlayerState>(OnPlayerDeath);
        EventSystem.instance.AddListener<PauseGameUI>(PauseGameUI);
    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<PlayerState>(OnPlayerDeath);
        EventSystem.instance.RemoveListener<PauseGameUI>(PauseGameUI);
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

}
