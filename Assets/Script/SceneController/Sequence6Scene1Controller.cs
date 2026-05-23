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
        StartCoroutine(AddFilmOnSceneStart());
    }

    protected override void OnStopIntervalReached()
    {
    }

    private IEnumerator AddFilmOnSceneStart()
    {
        if (itemAdded) yield break;
        if (film == null) yield break;

        yield return null;

        var inv = InventoryManager.Instance;
        if (inv == null) yield break;

        inv.AddItem(film);
        itemAdded = true;
    }
}