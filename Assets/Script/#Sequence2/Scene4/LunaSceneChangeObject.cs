using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LunaSceneChangeObject : InspectableObject
{
    [Header("씬 전환")]
    public string nextSceneName;
    public float delayBeforeSceneChange = 2.0f;

    [Header("플레이어")]
    public Transform player;

    [Header("NPC 이미지")]
    public SpriteRenderer npcSpriteRenderer;
    public Sprite npcLookLeftSprite;
    public Sprite npcLookRightSprite;

    [Header("플레이어 이동 차단")]
    public PlayerMove_Test_Lerp playerMove;

    private bool isChangingScene = false;

    protected override void OnInspect()
    {
        if (isChangingScene)
            return;

        StartCoroutine(SceneChangeRoutine());
    }

    private IEnumerator SceneChangeRoutine()
    {
        isChangingScene = true;

        // 플레이어 이동 막기
        if (playerMove != null)
            playerMove.canMove = false;

        ChangeNpcDirection();

        yield return new WaitForSeconds(delayBeforeSceneChange);

        SceneManager.LoadScene(nextSceneName);
    }

    private void ChangeNpcDirection()
    {
        if (player == null || npcSpriteRenderer == null)
            return;

        // 플레이어가 NPC의 왼쪽에 있을 때
        if (player.position.x < transform.position.x)
        {
            // NPC는 왼쪽을 봐야 함
            if (npcLookLeftSprite != null)
                npcSpriteRenderer.sprite = npcLookLeftSprite;
        }
        else
        {
            // 플레이어가 NPC의 오른쪽에 있을 때
            // NPC는 오른쪽을 봐야 함
            if (npcLookRightSprite != null)
                npcSpriteRenderer.sprite = npcLookRightSprite;
        }
    }
}