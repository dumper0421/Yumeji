using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingChair : MonoBehaviour
{
    public GameObject Player;
    public Vector3 Offset;
    public AudioClip SFX;

    public float StartRange;
    public float LerpSpeed = 0.01f;

    private bool _hasStart = false;
    private bool _hasEnd = false;
    private bool _playSFX = false;

    private Vector3 _targetPos;

   void Start()
    {
        _targetPos = transform.position + Offset;
    }
    void Update()
    {

        if (Vector3.Distance(Player.transform.position,transform.position) < StartRange)
        {
            _hasStart = true;
            if (!_playSFX)
            {
                SoundManager.Instance.PlaySFX(SFX);
                _playSFX = true;
            }
        }

        if (_hasStart)
        {
            transform.position = Vector3.Lerp(transform.position, _targetPos, LerpSpeed);
        }

        if (Vector3.Distance(_targetPos, transform.position) < 0.01f)
        {
            _hasEnd = true;
            _hasStart = false;
            transform.position = _targetPos;

        }


    }
}
