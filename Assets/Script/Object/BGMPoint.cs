using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMPoint : MonoBehaviour
{
    public AudioClip BGM;

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            SoundManager.Instance.StopBGM();
            SoundManager.Instance.PlayBGM(BGM);
        }
    }
}
