using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void OnPlay() {
        Debug.Log("'PLAY' Pressed");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OnOptions() {
        Debug.Log("'OPTIONS' Pressed");
    }

    public void OnQuit() {
        Debug.Log("'QUIT' Pressed");
        //Application.Quit();
        //UnityEditor.EditorApplication.isPlaying = false;
    }
}
