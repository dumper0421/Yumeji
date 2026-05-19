using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject_Test: MonoBehaviour
{
    private bool isPlayerNearby = false; // 플레이어가 근처에 있는지 확인

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("NPC와 상호작용함!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // 플레이어가 범위에 들어왔을 때
        {
            isPlayerNearby = true;
            Debug.Log("NPC 근처에 플레이어 접근");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // 플레이어가 범위를 벗어났을 때
        {
            isPlayerNearby = false;
            Debug.Log("NPC 근처에서 플레이어 떠남");
        }
    }
}