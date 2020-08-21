using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;
public class MainMenu : MonoBehaviour
{
    public void OnPlay() {
        Debug.Log("'PLAY' Pressed");
        EventSystem.instance.RaiseEvent(new SceneLoadNext { });
        //Scenemanager.Instance.NextScene(); Getting an error for no reason :(( Not regeistering the Next Scene
    }

    public void OnOptions() {
        Debug.Log("'OPTIONS' Pressed");
    }

    public void OnQuit() {
        Debug.Log("'QUIT' Pressed");
        Application.Quit();
        //UnityEditor.EditorApplication.isPlaying = false;
    }
}
