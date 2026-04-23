using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTeleportPoint : MonoBehaviour
{
    [SerializeField] private TeleportPoint _teleportPoint;
    [SerializeField] private AstarEnemy _astarEnemy;
    [SerializeField] private EnemyPathfinder _enemyPathfinder;

    private void Awake()
    {
        if(_teleportPoint == null)
            _teleportPoint = GetComponent<TeleportPoint>();
    }
 

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            _astarEnemy.StopAllCoroutines();
            collision.transform.position = _teleportPoint.TargetPoint - new Vector3(0f, 1f, 0f);
            _astarEnemy.ForceRepathNow();

        }
    }
}
