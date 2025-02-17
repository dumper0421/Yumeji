using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "NewStatusData", menuName = "Status/StatusData")]
public class StatusData : ScriptableObject
{
    // 기준이 되는 Status만 저장
    public float MaxHealth = 100f;
    public float BaseSpeed = 5f;
}

public class Status : MonoBehaviour
{
    public StatusData statusData;

    private float currentHealth_;
    public float CurrentHealth
    {
        get => currentHealth_;
        set
        {
            currentHealth_ = value > 0 ? value : 0; 
        }
    }
    public float MaxHealth => statusData.MaxHealth;
    public float BaseSpeed => statusData.BaseSpeed;
    public float RunSpeed => statusData.BaseSpeed * 2f;

    public void Init()
    {
        currentHealth_ = MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth_ = Mathf.Clamp(currentHealth_ - amount, 0, MaxHealth);
    }

    public void Heal(float amount)
    {
        currentHealth_ = Mathf.Clamp(currentHealth_ + amount, 0, MaxHealth);
    }
}
