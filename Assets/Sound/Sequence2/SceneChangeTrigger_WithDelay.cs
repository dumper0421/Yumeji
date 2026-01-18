using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeTrigger_WithDelay : MonoBehaviour
{
    private bool hasTriggered = false;

    [SerializeField] private string sceneName_;
    [SerializeField] private float delay = 2f;
    [SerializeField] private AudioClip triggerSFX;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || hasTriggered)
            return;

        hasTriggered = true;
        StartCoroutine(SceneChangeSequence(collision.gameObject));
    }

    private IEnumerator SceneChangeSequence(GameObject player)
    {
        // 1. 사운드 정리
        SoundManager.Instance.StopAllSFX();
        SoundManager.Instance.StopBGM();

        // 2. SFX 재생
        if (triggerSFX != null)
            SoundManager.Instance.PlaySFX(triggerSFX);

        // 3. 플레이어 스프라이트 끄기
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = false;



        // 4. 대기
        yield return new WaitForSeconds(delay);

        // 5. 씬 전환
        if (sceneName_ != "Scenes/Final/Sequence1#7")
            SceneManager.LoadScene(sceneName_);
        else
            SceneManager.LoadScene(7);
    }
}
