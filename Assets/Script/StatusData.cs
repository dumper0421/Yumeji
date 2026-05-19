using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStatusData", menuName = "Status/StatusData")]
public class StatusData : ScriptableObject
{
    // 기준이 되는 Status만 저장
    public float MaxHealth = 100f;
    public float BaseSpeed = 5f;
}

