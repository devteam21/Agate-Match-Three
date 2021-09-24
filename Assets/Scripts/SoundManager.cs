using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance = null;
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SoundManager>();
            }
            return _instance;
        }
    }

    //Kumpulan Audio
    public AudioClip scoreNormal;
    public AudioClip scoreCombo;
    public AudioClip wrongMove;
    public AudioClip tap;

    private AudioSource player;

    private void Start()
    {
        player = GetComponent<AudioSource>();
    }

    public void PlayScore(bool isCombo)
    {
        if (isCombo)
        {
            player.PlayOneShot(scoreCombo);
        } else
        {
            player.PlayOneShot(scoreNormal);
        }
    }

    public void PlayTap()
    {
        player.PlayOneShot(tap);
    }


    public void PlayWrong()
    {
        player.PlayOneShot(wrongMove);
    }

    
}
