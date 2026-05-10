using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence6Scene1Controller : SceneController
{
    [Header("Start Item")]
    public ItemData film;

    private bool itemAdded = false;

    protected override void Start()
    {
        base.Start();

        AddFilmOnSceneStart();
    }

    protected override void OnStopIntervalReached()
    {
        // 기존 SceneController 흐름에서 멈춤 시간이 끝났을 때 처리할 내용이 있으면 여기에 작성
    }

    private void AddFilmOnSceneStart()
    {
        if (itemAdded) return;
        if (film == null) return;
        if (InventoryManager.Instance == null) return;

        InventoryManager.Instance.AddItem(film);
        itemAdded = true;
    }
}