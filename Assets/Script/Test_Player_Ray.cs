using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_Player_Ray : MonoBehaviour
{
    public float rayDistance = 2.0f; 
    public LayerMask npcLayer; 

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) 
        {
            InteractWithNPC();
        }
    }

    void InteractWithNPC()
    {
        Vector2 rayDirection = transform.forward; 
        RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, rayDistance, npcLayer);

        if (hit.collider != null) 
        {
            Debug.Log("상호작용(RayCast) " + hit.collider.name);
        }
        else
        {
            Debug.Log("NPC가 근처에 없음(RayCast)");
        }

        Debug.DrawRay(transform.position, rayDirection * rayDistance, Color.red, 0.5f);
    }
}