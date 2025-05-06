using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MoveObstacle : Obstacle
{
    [SerializeField]
    private Vector2 _moveDirection;

    public bool IsRotate = false;
    public bool IsStart = false;

    public float RotationSpeed = 5f;
    public float MoveSpeed = 5f;


    protected virtual void Update()
    {
        if (!IsStart) return;

        transform.position = transform.position + (Vector3)_moveDirection * MoveSpeed * Time.deltaTime;
        if (IsRotate)
        {
            transform.Rotate(new Vector3(0, 0, RotationSpeed) * Time.deltaTime, Space.Self);
        }
    }

}
