using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum S3S3PState
{
    None
}

public class Sequence3Scene3PuzzleController : DialogueController<S3S3PState>
{
    [Header("Refs")]
    [SerializeField] private CompanionSystem ReiCompanionSystem;
    [SerializeField] private PlayerMove_Test_Lerp HaruMove;

    [Header("Puzzle")]
    [SerializeField] private List<DialogueObject> RoesSwitches;
    [SerializeField] private List<GameObject> Lamps;
    [SerializeField] private AudioClip SwtichSound;
    [SerializeField] private AudioClip OpenSound;

    [SerializeField] private SpriteRenderer PostBoxSpriteRender;
    [SerializeField] private Sprite PostBoxSprite;
    [SerializeField] private GameObject PostBox;

    [SerializeField] private GameObject Door;

    [SerializeField] private AudioClip BGM;

    public ItemData OldKeyData;

    public string NextScene;


    public int PushCnt { get; private set; } = 0;

    // 이미 눌린 스위치 중복 카운트 방지
    private readonly HashSet<int> _pushed = new HashSet<int>();

    private const string StartDialogueId = "HaruRei1_01";
    private const string AlreadyPushedDialogueId = "Already_Pushed";

    private void Start()
    {
        dialogueManager.StartDialogue(StartDialogueId);
        SoundManager.Instance.StopBGM();
        SoundManager.Instance.PlayBGM(BGM);
    }

    protected override void HandleDialogueEnd(string dialogueId)
    {
        switch (dialogueId) { 
            case "HaruRei1_04": 
                HaruMove.SetCompanion(ReiCompanionSystem, "레이"); 
                break;
            case "GetRose":
                InventoryManager.Instance.AddItem(OldKeyData);
                PostBox.transform.GetChild(0).gameObject.SetActive(false);
                PostBox.GetComponent<DialogueObject>().IsDisposable = true;
                break;
            case "OpenDoor":
                SoundManager.Instance.PlaySFX(OpenSound);
                StartCoroutine(ChangeScene());
                break;
        }       

    }

    protected override void HandleOption(string text, string nextId)
    {
        if (TryGetRoseIndex(nextId, out int index))
        {
            TryPushRose(index);
        }
    }

    private void TryPushRose(int index)
    {
        if (RoesSwitches == null) return;
        if (index < 0 || index >= RoesSwitches.Count) return;

        if (!_pushed.Add(index)) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SwtichSound);

        var target = RoesSwitches[index];
        if (target != null)
        {
            target.StartDialogue = AlreadyPushedDialogueId;
            Lamps[index].GetComponent<Light2D>().enabled = true;
        }

        PushCnt++;

        if (PushCnt == 1)
        {
            PostBox.GetComponent<DialogueObject>().StartDialogue = "Less_LampPushed";
        }

        if (PushCnt == 4)
        {
            OnPuzzleComplete();
        }
    }

    private bool TryGetRoseIndex(string nextId, out int index)
    {
        index = -1;
        if (string.IsNullOrEmpty(nextId)) return false;

        const string prefix = "Rose";
        const string suffix = "_Pushed";

        if (!nextId.StartsWith(prefix)) return false;
        if (!nextId.EndsWith(suffix)) return false;

        int start = prefix.Length;
        int len = nextId.Length - start - suffix.Length;
        if (len <= 0) return false;

        string number = nextId.Substring(start, len);
        if (!int.TryParse(number, out int n)) return false;

        index = n - 1; 
        return true;
    }

    protected override void OnPuzzleComplete() 
    {
        PostBox.transform.GetChild(0).gameObject.SetActive(true);
        PostBox.GetComponent<DialogueObject>().StartDialogue = "CompletedPushed";
        PostBoxSpriteRender.sprite = PostBoxSprite;
    }
    protected override void TryProgress() { }

    IEnumerator ChangeScene()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(NextScene);
    }
}
