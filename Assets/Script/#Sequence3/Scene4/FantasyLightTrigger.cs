using System.Collections;
using UnityEngine;

public class FantasyLightTrigger : SitObject
{
    [Header("Target")]
    [SerializeField] private GameObject player;

    [Header("Event Controller")]
    [SerializeField] private Sequence3Scene4Controller scene4Controller;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private PlayerActionController action;
    private bool fired = false;

    private void Awake()
    {
        if (player != null)
            action = player.GetComponent<PlayerActionController>();

        if (debugLog)
        {
            Debug.Log($"[ReiSideSeatTrigger:{name}] Awake - player={(player ? player.name : "NULL")}, action={(action ? "OK" : "NULL")}, controller={(scene4Controller ? "OK" : "NULL")}");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(WatchSitState());
    }

    private IEnumerator WatchSitState()
    {
        if (action == null)
        {
            Debug.LogWarning($"[ReiSideSeatTrigger:{name}] PlayerActionController가 비어있었다.");
            yield break;
        }

        if (scene4Controller == null)
        {
            Debug.LogWarning($"[ReiSideSeatTrigger:{name}] Sequence3Scene4Controller가 비어있었다.");
            yield break;
        }

        if (debugLog)
            Debug.Log($"[ReiSideSeatTrigger:{name}] Watch 시작");

        while (!fired)
        {
            bool isSitting = action.IsSitting;
            bool isThisSeat = (action.CurrentSeat == this);

            if (debugLog && isSitting)
            {
                string cur = action.CurrentSeat ? action.CurrentSeat.name : "NULL";
                Debug.Log($"[ReiSideSeatTrigger:{name}] IsSitting=true / CurrentSeat={cur} / isThisSeat={isThisSeat}");
            }

            if (isSitting && isThisSeat)
            {
                fired = true;

                if (debugLog)
                    Debug.Log($"[ReiSideSeatTrigger:{name}] 착석 완료 감지 → 환상의 빛 이벤트 실행");

                action.IsLockedByEvent = true;
                scene4Controller.StartFantasyLightEvent();
                yield break;
            }

            yield return null;
        }
    }
}