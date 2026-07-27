using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [7-4 호텔 3층] 씬 종료 연출.
///
/// 트리거(둘 중 무엇이든):
///   1) 창문 1타일 앞 진입  → 창문 앞 트리거 콜라이더 + PlayerZoneTrigger 의 On Player Enter() 에 Play() 연결
///   2) 관리인과 충돌        → 관리인(AstarEnemy/Enemy) 의 On Reached() 이벤트에 Play() 연결
///
/// 연출: 플레이어 조작 정지 + 관리인 이동 정지 → 화면 암전 → 유리 깨지며 떨어지는 SFX → 시퀀스8 정원으로 전환.
///
/// 두 트리거 모두 Play() 하나만 호출하므로 연출과 씬 전환이 완전히 동일하게 처리된다.
/// </summary>
public class S7S4EndingSequence : MonoBehaviour
{
    [Header("정지 대상")]
    [Tooltip("플레이어 오브젝트의 PlayerMove_Test_Lerp")]
    [SerializeField] private PlayerMove_Test_Lerp player;
    [Tooltip("관리인(적). 창문 앞 진입으로 끝날 때도 함께 멈춰 세운다.")]
    [SerializeField] private AstarEnemy caretaker;

    [Header("암전 (CutsceneManager 필요)")]
    [Tooltip("암전에 걸리는 시간(초)")]
    [SerializeField] private float fadeDuration = 1.0f;
    [Tooltip("완전 암전 후 씬 전환까지 유지 시간(초)")]
    [SerializeField] private float holdBlackTime = 1.5f;

    [Header("SFX")]
    [Tooltip("유리 깨지며 떨어지는 SFX")]
    [SerializeField] private AudioClip glassBreakSfx;
    [Tooltip("암전 시작 후 SFX 재생까지의 딜레이(초)")]
    [SerializeField] private float sfxDelay = 0.3f;

    [Header("씬 전환")]
    [Tooltip("전환할 시퀀스8 정원 씬 이름 (Build Settings 에 등록되어 있어야 함)")]
    [SerializeField] private string nextSceneName = "";

    [Header("시작 연출")]
    [Tooltip("씬 시작 시 검은 화면에서 페이드인. CutsceneManager 는 씬을 검게 시작시키므로 켜두는 것을 권장.")]
    [SerializeField] private bool fadeInOnStart = true;

    private bool _played = false;

    private IEnumerator Start()
    {
        if (!fadeInOnStart) yield break;

        // CutsceneManager.Start() 가 화면을 검게 만든 뒤 페이드인하도록 한 프레임 대기
        yield return null;

        if (CutsceneManager.Instance != null)
            CutsceneManager.Instance.FadeFromBlack(null, fadeDuration);
    }

    /// <summary>창문 트리거 / 관리인 On Reached 양쪽에서 호출한다. 중복 호출은 무시된다.</summary>
    public void Play()
    {
        if (_played) return;
        _played = true;
        StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        // 1) 플레이어 조작 정지
        if (player != null)
        {
            player.canMove = false;
            if (player.animator != null)
                player.animator.SetBool("Walking", false);
            player.enabled = false;
        }

        // 2) 관리인 이동 정지
        if (caretaker != null)
        {
            caretaker.CancelMovement();
            caretaker.enabled = false;
        }

        // 3) 화면 암전 시작 (fadeDuration 동안 서서히 검게)
        if (CutsceneManager.Instance != null)
            CutsceneManager.Instance.FadeToBlack(null, fadeDuration);

        // 4) 유리 깨지며 떨어지는 SFX (암전 도중에 재생)
        if (sfxDelay > 0f)
            yield return new WaitForSecondsRealtime(sfxDelay);

        if (glassBreakSfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(glassBreakSfx);

        // 5) 완전 암전 유지 (sfxDelay + holdBlackTime 이 fadeDuration 이상이 되도록 세팅)
        yield return new WaitForSecondsRealtime(holdBlackTime);

        // 6) 사운드 정리 후 시퀀스8 정원으로 전환
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
            SoundManager.Instance.StopAllSFX();
        }

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogError("[S7S4EndingSequence] nextSceneName 이 비어있다. 시퀀스8 정원 씬 이름을 지정하라.");
    }
}
