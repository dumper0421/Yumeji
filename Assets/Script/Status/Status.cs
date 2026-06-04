using UnityEngine;
using UnityEngine.Rendering;


public class Status : MonoBehaviour
{
    public StatusData statusData;

    private float currentHealth_;
    public float CurrentHealth
    {
        get { return currentHealth_; }
        set { currentHealth_ = value > 0 ? value : 0; }
    }


    private void Awake()
    {
        if(statusData == null)
            statusData = Resources.Load<StatusData>("ScriptableObject/StatusData/HaruStatusData");
    }

    public float MaxHealth
    {
        get { return statusData != null ? statusData.MaxHealth : 0f; }
    }

    public float BaseSpeed
    {
        get { return statusData != null ? statusData.BaseSpeed : 0f; }
    }

    public float RunSpeed
    {
        get { return statusData != null ? statusData.BaseSpeed * 2f : 0f; }
    }

    public void Init()
    {
        currentHealth_ = MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth_ = Mathf.Clamp(currentHealth_ - amount, 0, MaxHealth);

        if (currentHealth_ == 0)
            UIManager.Instance?.OpenGameOverUI();
    }

    public void Heal(float amount)
    {
        currentHealth_ = Mathf.Clamp(currentHealth_ + amount, 0, MaxHealth);
    }
}
