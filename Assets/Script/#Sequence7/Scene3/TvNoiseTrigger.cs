using UnityEngine;

public class TvNoiseTrigger : MonoBehaviour
{
    [Header("TV Noise")]
    [SerializeField] private AudioSource tvNoiseAudioSource;

    private bool isTVOff = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("[TV Noise] 플레이어 방 입장 감지");

        StartNoise();
    }

    public void StartNoise()
    {
        // TV가 이미 꺼졌다면 다시 잡음을 재생하지 않음
        if (isTVOff)
        {
            Debug.Log("[TV Noise] TV가 꺼진 상태라 잡음을 재생하지 않음");
            return;
        }

        if (tvNoiseAudioSource == null)
        {
            Debug.LogWarning("[TV Noise] AudioSource가 연결되지 않았습니다.");
            return;
        }

        if (tvNoiseAudioSource.isPlaying)
            return;

        tvNoiseAudioSource.Play();

        Debug.Log("[TV Noise] TV 잡음 재생 시작");
    }

    public void TurnOffNoise()
    {
        if (isTVOff)
            return;

        isTVOff = true;

        if (tvNoiseAudioSource != null)
        {
            tvNoiseAudioSource.Stop();
        }

        Debug.Log("[TV Noise] TV 잡음 종료");
    }
}