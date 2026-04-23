using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static AnalogTVNoiseController;

public class S5S3StagePoint : MonoBehaviour
{
    [SerializeField] private TVNoisePreset preset;
    [SerializeField] private PlayerMove_Test_Lerp _playerMove;
    [SerializeField] AnalogTVNoiseController _noiseControlloer;
    [SerializeField] private bool _isWrong;
    [SerializeField] private bool _isExit;

    [SerializeField] private float _slowSpeed;
    [SerializeField] private float _slowRunSpeed;



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (_isExit)
            {
                _playerMove.RevertSpeed();
                _noiseControlloer.SetPreset(preset);
            }
            else
            {
                _noiseControlloer.SetPreset(preset);

                if (_isWrong)
                    _playerMove.ChangeSpeed(_slowSpeed, _slowRunSpeed);
            }

        }
    }

}
