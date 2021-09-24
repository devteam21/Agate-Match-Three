using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private static TimeManager _instance = null;
    public static TimeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TimeManager>();
            }
            return _instance;
        }
    }

    public int duration;
    private float time;


    private void Start()
    {
        time = 0;
    }

    private void Update()
    {
        if (GameFlowManager.Instance.IsGameOver) return;

        if (time > duration)
        {
            GameFlowManager.Instance.GameOver();
            return;
        }

        time += Time.deltaTime;
    }

    public float GetRemainingTime()
    {
        return duration - time;
    }
}
