using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkTeleportPoint : TeleportPoint
{
    public Vector3 ReturnPos;
    public float WaitSeconds = 2f;
    public override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        if (!collision.CompareTag("Player")) return;

        StartCoroutine(Return(collision.gameObject));

    }

    IEnumerator Return(GameObject target)
    {
        yield return new WaitForSeconds(WaitSeconds);
        target.transform.position = ReturnPos;
    }
}
