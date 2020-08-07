using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DualityES;


public class SceneLoadNext : DualityES.Event
{

}

public class ResetGameScene : DualityES.Event
{

}

public class Scenemanager : MonoBehaviour
{
    #region Singleton
    private static SceneManager _instance;
    public static SceneManager Instance //Ensures that this is the only instance in the class
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SceneManager();
            }
            return _instance;
        }
    }
    #endregion

    private int sceneIndex = 0;

    private void Awake()
    {
        EventSystem.instance.AddListener<SceneLoadNext>(NextScene);
        EventSystem.instance.AddListener<ResetGameScene>(MainMenuScene);
        DontDestroyOnLoad(this.gameObject);//So it presist throughout the game


    }

    private void Start()
    {
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<ResetGameScene>(MainMenuScene);
        EventSystem.instance.RemoveListener<SceneLoadNext>(NextScene);
    }


    public void NextScene(SceneLoadNext loadNextScene)
    {
        int temp = SceneManager.GetActiveScene().buildIndex + 1;
        Debug.Log("LoadNextScene");
        SceneManager.LoadScene(temp);
        sceneIndex++;
    }
    public void MainMenuScene(ResetGameScene reset)
    {
        SceneManager.LoadScene(1);
    }



}
