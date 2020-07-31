using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class UpdateObstacleList : DualityES.Event
{
    public GameObject obstacle;
}

public class ObjectManager : MonoBehaviour
{
    public List<GameObject> m_Obstacles;

    [SerializeField]private List<Transform> m_OrignalPositions;
    [SerializeField] private List<bool> m_ObjectsIsEnabled;


    private void Awake()
    {
        EventSystem.instance.AddListener<PlayerState>(OnReviveEvent);
        EventSystem.instance.AddListener<UpdateObstacleList>(AddObstacle);
        m_Obstacles = new List<GameObject>();
        m_OrignalPositions = new List<Transform>();
        m_ObjectsIsEnabled = new List<bool>();
    }
    private void OnDisable()
    {
       EventSystem.instance.RemoveListener<PlayerState>(OnReviveEvent);
       EventSystem.instance.RemoveListener<UpdateObstacleList>(AddObstacle);
    }

    public void AddObstacle(UpdateObstacleList obstacle)
    {
        m_Obstacles.Add(obstacle.obstacle);
    }

    void Start()
    {
        for(int i = 0; i < m_Obstacles.Count; i++)
        {
            m_OrignalPositions[i] = m_Obstacles[i].GetComponent<Transform>();
        }
        for(int i = 0; i< m_Obstacles.Count; i++)
        {
            m_ObjectsIsEnabled[i] = m_Obstacles[i].activeInHierarchy;
        }
    }

    void OnReviveEvent(PlayerState state)
    {
        if (state.dead == false)//player comes back to life/checkpoint
        {
            for(int i = 0; i < m_Obstacles.Count; i++)
            {
                Transform tempTransform = m_Obstacles[i].transform;
                tempTransform.position = m_OrignalPositions[i].position;
                tempTransform.rotation = m_OrignalPositions[i].rotation;
                tempTransform.localScale = m_OrignalPositions[i].localScale;

                m_Obstacles[i].SetActive(m_ObjectsIsEnabled[i]);

            }
        }

    }

}
