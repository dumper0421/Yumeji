using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_NPC_Transform : MonoBehaviour
{
        public Transform player; // 플레이어의 Transform
        public float interactionRange = 1.0f; // 상호작용 가능한 거리

    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (Input.GetKeyDown(KeyCode.G))
        {

            if (distance < interactionRange) { 
                Debug.Log("아이템을 얻음 (Transform)");
            Destroy(gameObject);
        }
        else
            Debug.Log("아이템을 찾을 수 없음(Transform)");
        }
    }

    
       
}
