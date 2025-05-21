using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class layersetting_picture : MonoBehaviour
{
    private SpriteRenderer spriteRender_;

    void Start()
    {
        spriteRender_ = GetComponent<SpriteRenderer>();
        spriteRender_.sortingOrder = Mathf.RoundToInt(transform.position.y) * (-1)+1;
    }
}
