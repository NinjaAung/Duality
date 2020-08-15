using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;

public class spirit : MonoBehaviour
{
    private SpriteRenderer renderer;
    public float judgmentMin = 10f;


    // Start is called before the first frame update
    void Awake()
    {
        EventSystem.instance.AddListener<Judgment>(GhostToggle);
        renderer = GetComponent<SpriteRenderer>();
    }

    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<Judgment>(GhostToggle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void  GhostToggle(Judgment judgment)
    {
        renderer.enabled = judgment.JudgmentScore >= judgmentMin;
    }
}
