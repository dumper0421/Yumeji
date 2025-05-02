using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowBar : MoveObstacle
{
    [Header("시작 트리거 영역")]
    public Vector2 TriggerPosition; // 플레이어가 도달해야 할 위치
    public GameObject Player;

    private SpriteRenderer _renderer;
    private BoxCollider2D _collider;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.enabled = false;

        _collider = GetComponent<BoxCollider2D>();
        _collider.enabled = false;
    }

    protected override void Update()
    {
        float distance = Mathf.Abs(TriggerPosition.x - Player.transform.position.x);

        if (distance < 0.5f)
        {
            IsStart = true;
            _renderer.enabled = true;
            _collider.enabled = true;
        }

        base.Update();
    }
}
