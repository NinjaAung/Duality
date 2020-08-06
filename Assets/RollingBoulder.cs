using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollingBoulder : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        
    }

    // Update is called once per frame
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            Debug.Log("Roll bitch");
            this.gameObject.SetActive(true);
        }
    }
}