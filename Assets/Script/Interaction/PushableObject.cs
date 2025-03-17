using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableObject : MonoBehaviour
{
    public float pushDistance = 1f;

    public void Push(Vector3 playerPosition)
    {
        Vector3 pushDirection = transform.position - playerPosition;
        pushDirection.Normalize();
        transform.position += pushDirection * pushDistance;
    }
}