using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionLight : MonoBehaviour
{
    [SerializeField]
    private float _rotateSecond = 0.5f;
    [SerializeField]
    private float _rotateScale = 0.25f;
    [SerializeField]
    private float _rotateSpeed = 2f;


    [SerializeField]
    private float _maxScale = 0.5f;
    [SerializeField]
    private float _minScale = 0.25f;
    [SerializeField]
    private float _scaleSpeed = 0.5f;
    [SerializeField]
    private bool _isSmooth = false;

    private bool _isShrinking = true;


    private float _rotateTimer = 0.5f;
    private float _originalScale = 0.25f;

    private int _rotateCnt = 0;
    private int _step = 0;

    readonly float[] _angles = { 45f, 90f, 135f, 180f };



    void Start()
    {
        _originalScale = transform.localScale.x;
    }

    void Update()
    {

        if (_isSmooth)
        {
            transform.Rotate(0f, 0f, _rotateSpeed * Time.deltaTime, Space.Self);

            float targetScale = _isShrinking ? _minScale : _maxScale;
            float currentScale = transform.localScale.x;
            float newScale = Mathf.MoveTowards(currentScale, targetScale, _scaleSpeed * Time.deltaTime);

            transform.localScale = new Vector3(newScale, newScale, transform.localScale.z);

            if (Mathf.Approximately(newScale, targetScale))
                _isShrinking = !_isShrinking;

        }

        else 
        {
            _rotateTimer += Time.deltaTime;

            if (_rotateTimer > _rotateSecond)
            {
                _rotateTimer = 0f;

                if (!_isSmooth)
                {
                    _step = (_step + 1) % _angles.Length;
                    transform.rotation = Quaternion.Euler(0f, 0f, _angles[_step]);
                    transform.localScale = (_rotateCnt % 3 == 0) ? Vector2.one * _rotateScale : Vector3.one * _originalScale;

                }

                _rotateCnt++;

            }
        }
    }

}
