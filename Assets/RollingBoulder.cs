using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class RollingBoulder : MonoBehaviour
{
    public GameObject boulder;
    // Start is called before the first frame update
    void Start()
    {
        boulder.SetActive(false);
    }

    private void Update()
    {
        
    }

    // Update is called once per frame
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Roll bitch");
            boulder.SetActive(true);
        }
    }
}