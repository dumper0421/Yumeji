using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStopPoint : MonoBehaviour
{
    [SerializeField] private AstarEnemy _astarEnemy;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _astarEnemy.StopAllCoroutines();
            _astarEnemy.isStop = true;
            _astarEnemy.isMove = false;
            StartCoroutine(CO_MoveEnemy());
        }
    }

    IEnumerator CO_MoveEnemy()
    {
        yield return new WaitForSeconds(1f);

        _astarEnemy.isStop = false;
        _astarEnemy.isMove = true;
    }

 
}
