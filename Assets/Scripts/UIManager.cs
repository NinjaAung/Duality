using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DualityES;


public class UIManager : MonoBehaviour
{
    public Canvas deathCanvas;

    private void Awake()
    {
        EventSystem.instance.AddListener<PlayerState>(OnPlayerDeath);
    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<PlayerState>(OnPlayerDeath);

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

}
