using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveObject : InspectableObject
{
    [SerializeField] private SavePointFlashEffect flashEffect_;
    [SerializeField] private float yOffset_ = 0.05f;

    private Transform playerTransform_;

    protected override void OnInspect()
    {
        PopupUIManager.Instance.RegisterSaveEffect(flashEffect_);
        PopupUIManager.Instance.SetSaveLoadPopup(true);
    }

    public override void TryInspect()
    {
        if (IsDisposable && hasBeenInspected)
        {
            return;
        }

        if (!CanInteractFromBelow())
        {
            Debug.Log("세이브 포인트는 아래 방향에서만 상호작용할 수 있음");
            return;
        }

        hasBeenInspected = true;
        OnInspect();
    }

    private bool CanInteractFromBelow()
    {
        if (playerTransform_ == null)
        {
            playerTransform_ = FindPlayerTransform();
        }

        if (playerTransform_ == null)
        {
            Debug.LogWarning("플레이어를 찾을 수 없어서 세이브 상호작용을 할 수 없음");
            return false;
        }

        float playerY = playerTransform_.position.y;
        float saveY = transform.position.y;

        return playerY < saveY - yOffset_;
    }

    private Transform FindPlayerTransform()
    {
        // 1. GameManager에 플레이어가 등록되어 있으면 우선 사용
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            return GameManager.Instance.Player.transform;
        }

        // 2. Player 태그가 있으면 태그로 탐색
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer.transform;
        }

        // 3. 이름으로 탐색 (현재 프로젝트 기준 fallback)
        GameObject namedPlayer = GameObject.Find("Haru_Player");
        if (namedPlayer != null)
        {
            return namedPlayer.transform;
        }

        return null;
    }
}