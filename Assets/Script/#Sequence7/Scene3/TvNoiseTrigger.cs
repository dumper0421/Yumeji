using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TvNoiseTrigger : MonoBehaviour
{
    [SerializeField]
    private Room204TVEvent tvEvent;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log(" 플레이어가 입장");

        if (tvEvent != null)
            tvEvent.StartTVNoise();
    }
}
