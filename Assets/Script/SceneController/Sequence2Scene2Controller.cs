using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Sequence2Scene2Controller : SceneController
{
    [Header("Y Position BGM Control")]
    [SerializeField] private float yThreshold;
    [SerializeField] private bool playAbove = true;

    private bool isBgmPlaying;

    [Header("Event - References")]
    [SerializeField] private DialogueManager dialogueManager;          // 씬에 있는 DialogueManager 연결
    [SerializeField] private PlayerActionController playerAction;      // Player에 붙어있는 PlayerActionController (비워두면 자동 탐색)

    [Header("Event - Projector SFX (Loop)")]
    [SerializeField] private AudioSource projectorSource;             // loop용 AudioSource (없으면 자동 생성)
    [SerializeField] private AudioClip projectorLoopClip;             // "틱...틱..." 루프 클립

    [Header("Event - Fade (CanvasGroup)")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;             // 전체 화면 검정 CanvasGroup (알파로 페이드)

    [Header("Event - Flicker Overlay (Choose one)")]
    [SerializeField] private SpriteRenderer flickerSprite;            // 2D 스프라이트 오버레이 방식
    [SerializeField] private Image flickerImage;                      // UI 이미지 오버레이 방식(둘 중 하나만 써도 됨)

    [SerializeField]
    private Color[] flickerColors = new Color[]
    {
        new Color(0.05f, 0.10f, 0.18f, 0.25f), // 짙은 파랑 톤
        new Color(0.06f, 0.16f, 0.10f, 0.25f), // 탁한 녹색 톤
        new Color(0.18f, 0.05f, 0.05f, 0.25f)  // 핏빛 붉은 톤
    };
    [SerializeField] private Vector2 flickerIntervalRange = new Vector2(1f, 2f);

    [Header("Event - Dialogue IDs (JSON)")]
    [SerializeField] private string emptyCinemaMonologueId = "Cinema01";

    // 티켓별 1~2번째 대사 id (JSON에 해당 id로 Dialogues가 존재해야 함)
    [SerializeField] private string lilyLine1Id = "TicketA_01";
    [SerializeField] private string lilyLine2Id = "TicketA_02";

    [SerializeField] private string mulLine1Id = "TicketB_01";
    [SerializeField] private string mulLine2Id = "TicketB_02";

    [SerializeField] private string yiyiLine1Id = "TicketC_01";
    [SerializeField] private string yiyiLine2Id = "TicketC_02";

    [Header("Event - Ticket Item Names (Inventory)")]
    [SerializeField] private string lilyTicketItemName = "MovieTicketA";
    [SerializeField] private string mulTicketItemName = "MovieTicketB";
    [SerializeField] private string yiyiTicketItemName = "MovieTicketC";

    private bool eventRunning = false;
    private Coroutine flickerCo;

    private void Awake()
    {
        

        if (Player != null && playerAction == null)
            playerAction = Player.GetComponent<PlayerActionController>();

        if (projectorSource == null)
        {
            projectorSource = gameObject.GetComponent<AudioSource>();
            if (projectorSource == null) projectorSource = gameObject.AddComponent<AudioSource>();
        }
        projectorSource.playOnAwake = false;
        projectorSource.loop = true;

        // 페이드 초기값
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.interactable = false;
        }

        SetFlickerActive(false);
    }

    private void Update()
    {
        // ===== bgm
        if (Player == null) return;

        float playerY = Player.transform.position.y;

        bool shouldPlay =
            playAbove ? playerY <= yThreshold
                      : playerY > yThreshold;

        if (shouldPlay && !isBgmPlaying)
        {
            if (bgm != null)
                SoundManager.Instance.PlayBGM(bgm);

            isBgmPlaying = true;
        }
        else if (!shouldPlay && isBgmPlaying)
        {
            SoundManager.Instance.StopBGM();
            isBgmPlaying = false;
        }
    }

    /// <summary>
    /// CinemaSeatTrigger에서 호출할 함수
    /// </summary>
    public void StartCinemaSeatEvent()
    {
        if (eventRunning) return;
        eventRunning = true;
        StartCoroutine(CoCinemaSeatEvent());
    }

    private IEnumerator CoCinemaSeatEvent()
    {
        // 1) 조작 제한 (앉기 풀리는 문제도 여기서 해결)
        LockPlayerControls(true);

        // 1-2) 영사기 사운드 루프 시작
        PlayProjectorLoop(true);

        // 1-3) 독백: “…영화관에 사람이 없네. 오싹한걸.”
        yield return StartDialogueAndWait(emptyCinemaMonologueId);

        // 1-4) 2초 페이드아웃
        yield return FadeTo(1f, 2f);

        // 2) 암전 1초 유지
        yield return new WaitForSeconds(1f);

        // 2-2) 2초 페이드인 + 점멸 시작
        yield return FadeTo(0f, 2f);
        StartFlicker();

        // 3) 점멸 시작 후 5초 → 첫 번째 대사
        yield return new WaitForSeconds(5f);
        var (line1, line2) = GetTicketDialoguePair();
        if (!string.IsNullOrEmpty(line1))
            yield return StartDialogueAndWait(line1);

        // 3-2) 플레이어가 넘기고 나면 5초 → 두 번째 대사
        yield return new WaitForSeconds(5f);
        if (!string.IsNullOrEmpty(line2))
            yield return StartDialogueAndWait(line2);

        // 4) 두 번째 대사 넘기면 점멸 5초 더
        yield return new WaitForSeconds(5f);

        // 4-2) 2초 페이드아웃 (완전 암전)
        yield return FadeTo(1f, 2f);

        // 4-3) 사운드도 종료(페이드아웃 끝날 때 멎게)
        PlayProjectorLoop(false);
        StopFlicker();

        // 이벤트 종료: 조작 풀기
        LockPlayerControls(false);

        eventRunning = false;
    }

    // ====== Helpers ======

    private void LockPlayerControls(bool locked)
    {
        if (playerMoveTestLerp != null)
            playerMoveTestLerp.canMove = !locked;

        // “다시 상호작용키 누르면 앉기 풀림” 방지:
        // 이벤트 중엔 PlayerActionController 자체를 꺼버리면 가장 확실했다.
        if (playerAction != null)
            playerAction.enabled = !locked;

        // 애니메이터 자체를 끄면 앉은 자세가 깨질 수 있어서 여기선 끄지 않았다.
    }

    private void PlayProjectorLoop(bool play)
    {
        if (projectorSource == null || projectorLoopClip == null) return;

        if (play)
        {
            projectorSource.clip = projectorLoopClip;
            if (!projectorSource.isPlaying) projectorSource.Play();
        }
        else
        {
            if (projectorSource.isPlaying) projectorSource.Stop();
        }
    }

    private void StartFlicker()
    {
        SetFlickerActive(true);

        if (flickerCo != null) StopCoroutine(flickerCo);
        flickerCo = StartCoroutine(CoFlicker());
    }

    private void StopFlicker()
    {
        if (flickerCo != null)
        {
            StopCoroutine(flickerCo);
            flickerCo = null;
        }
        SetFlickerActive(false);
    }

    private IEnumerator CoFlicker()
    {
        while (true)
        {
            var c = flickerColors[Random.Range(0, flickerColors.Length)];
            SetFlickerColor(c);

            float wait = Random.Range(flickerIntervalRange.x, flickerIntervalRange.y);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SetFlickerActive(bool on)
    {
        if (flickerSprite != null) flickerSprite.gameObject.SetActive(on);
        if (flickerImage != null) flickerImage.gameObject.SetActive(on);
    }

    private void SetFlickerColor(Color c)
    {
        if (flickerSprite != null) flickerSprite.color = c;
        if (flickerImage != null) flickerImage.color = c;
    }

    private (string line1, string line2) GetTicketDialoguePair()
    {
        if (InventoryManager.Instance.HasItem(lilyTicketItemName))
            return (lilyLine1Id, lilyLine2Id);

        if (InventoryManager.Instance.HasItem(mulTicketItemName))
            return (mulLine1Id, mulLine2Id);

        if (InventoryManager.Instance.HasItem(yiyiTicketItemName))
            return (yiyiLine1Id, yiyiLine2Id);

        Debug.Log("티켓없음");
        return (lilyLine1Id, lilyLine2Id);
    }

    private IEnumerator StartDialogueAndWait(string dialogueId)
    {
        if (dialogueManager == null || string.IsNullOrEmpty(dialogueId))
            yield break;

        dialogueManager.StartDialogue(dialogueId);
        yield return new WaitUntil(() => !dialogueManager.isRunning);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float start = fadeCanvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    protected override void OnStopIntervalReached() { }
}
