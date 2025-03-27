using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLayerSetting : MonoBehaviour
{
    private SpriteRenderer spriteRenderer_;

    private void Start()
    {
        spriteRenderer_ = GetComponent<SpriteRenderer>();
    }
    // Update is called once per frame
    void Update()
    {
        spriteRenderer_.sortingOrder = Mathf.RoundToInt(transform.position.y) * -1;
    }
}
