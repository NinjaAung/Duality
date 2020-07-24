using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DualityES;


public class SceneLoadNext : DualityES.Event
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

    public int sceneIndex = 0;

    private void Awake()
    {
        EventSystem.instance.AddListener<SceneLoadNext>(NextScene);
        DontDestroyOnLoad(this.gameObject);//So it presist throughout the game

    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<SceneLoadNext>(NextScene);
    }


    public void NextScene(SceneLoadNext loadNextScene)
    {
        int temp = SceneManager.GetActiveScene().buildIndex + 1;
        Debug.Log("LoadNextScene");
        SceneManager.LoadScene(temp);
        sceneIndex++;
    }




}
