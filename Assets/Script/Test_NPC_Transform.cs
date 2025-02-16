using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_NPC_Transform : MonoBehaviour
{
        public Transform player; // 플레이어의 Transform
        public float interactionRange = 2.0f; // 상호작용 가능한 거리

        void Update()
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance < interactionRange && Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("NPC와 상호작용함! (Transform)");
            }
        }
}

