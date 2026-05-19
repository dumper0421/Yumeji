using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLayerSorting : MonoBehaviour
{
    public Transform player;       // 하루 Transform
    public float yOffset;          // Y좌표 보정값

    public int OrderA;
    public int OrderB;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null)
            return;

        // 플레이어의 Y좌표가 오브젝트 Y좌표(+ 보정값)보다 크면? (= 플레이어가 더 위에 있으면)
        if (player.position.y > transform.position.y + yOffset)
        {
            spriteRenderer.sortingOrder = OrderA; // 표지판을 플레이어 위로 그림
        }
        else
        {
            spriteRenderer.sortingOrder = OrderB; // 표지판을 플레이어 아래로 그림
        }
    }
}
