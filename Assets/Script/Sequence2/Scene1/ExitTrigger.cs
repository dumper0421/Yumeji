using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitTrigger : MonoBehaviour
{
    public bool IsClear = false;
    public Image FadeImage;
    public DialogueManager DialogueManager;

    [Header("SFX Clips")]
    public AudioClip EngineSFX;
    public AudioClip StartSFX;

    public string NextSceneName;



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player") && IsClear)
            StartCoroutine(FadeOutChangeScene(0.5f));

        else if (collision.CompareTag("Player"))
            DialogueManager.StartDialogue("Worng_Exit");
    }

    public IEnumerator FadeOutChangeScene(float fadeDuration)
    {
        if (FadeImage == null) yield break;

        float elapsed = 0f;
        Color c = FadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : (elapsed / fadeDuration);
            float alpha = Mathf.Lerp(0f, 1f, t);
            FadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        FadeImage.color = new Color(c.r, c.g, c.b, 1f);

        SoundManager.Instance.PlaySFX(StartSFX);
        SoundManager.Instance.PlaySFX(EngineSFX);

        DialogueManager.StartDialogue("Haru_monologue1");

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(NextSceneName);

    }
}
