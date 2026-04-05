using System.Collections;
using UnityEngine;

public class Stair : MonoBehaviour
{
    [SerializeField] private PlayerMove_Test_Lerp _playerMove;
    [SerializeField] private Vector3 _targetPos;
    [SerializeField] private float _speed = 1f;
    [SerializeField] private Stair _oppositeStair;

    [SerializeField] private float _reactivateDelay = 0.2f;

    private bool _isLocked = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (_isLocked) return;

        StartCoroutine(HandleStairMove());
    }

    private IEnumerator HandleStairMove()
    {
        SetLock(true);
        if (_oppositeStair != null)
            _oppositeStair.SetLock(true);

        _playerMove.StopAllCoroutines();

        yield return StartCoroutine(_playerMove.MoveToPositionCoroutine(_targetPos, _speed));

        yield return new WaitForSeconds(_reactivateDelay);

        SetLock(false);
        if (_oppositeStair != null)
            _oppositeStair.SetLock(false);
    }

    public void SetLock(bool isLocked)
    {
        _isLocked = isLocked;
    }
}