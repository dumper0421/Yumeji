using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerSetting : MonoBehaviour
{
    private SpriteRenderer spriteRender_;

    void Start()
    {
        spriteRender_ = GetComponent<SpriteRenderer>();
        spriteRender_.sortingOrder = Mathf.RoundToInt(transform.position.y) * -1;
    }
}