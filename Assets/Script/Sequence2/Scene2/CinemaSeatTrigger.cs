using System.Collections;
using UnityEngine;

public class CinemaSeatTrigger : SitObject
{
    [Header("Target")]
    [Tooltip("플레이어 오브젝트를 넣거나 비워두면 tag=Player로 찾는다.")]
    [SerializeField] private GameObject player;

    [Header("Event Controller")]
    [SerializeField] private Sequence2Scene2Controller scene2Controller; // ✅ 여기에 씬컨트롤러 연결

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private PlayerActionController action;
    private bool fired = false;

    private void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p;
        }

        if (player != null)
            action = player.GetComponent<PlayerActionController>();

        if (scene2Controller == null)
            scene2Controller = FindObjectOfType<Sequence2Scene2Controller>();

        if (debugLog)
        {
            Debug.Log($"🟣 [Seat:{name}] Awake - player={(player ? player.name : "NULL")}, action={(action ? "OK" : "NULL")}, controller={(scene2Controller ? "OK" : "NULL")}");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(WatchSitState());
    }

    private IEnumerator WatchSitState()
    {
        while (action == null)
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p;
            }

            if (player != null)
                action = player.GetComponent<PlayerActionController>();

            if (debugLog)
                Debug.Log($"🟣 [Seat:{name}] action 재탐색 중... action={(action ? "OK" : "NULL")}");

            yield return null;
        }

        if (debugLog)
            Debug.Log($"🟣 [Seat:{name}] Watch 시작");

        while (!fired)
        {
            bool isSitting = action.IsSitting;
            bool isThisSeat = (action.CurrentSeat == this);

            if (debugLog && isSitting)
            {
                string cur = action.CurrentSeat ? action.CurrentSeat.name : "NULL";
                Debug.Log($"🟡 [Seat:{name}] IsSitting=true / CurrentSeat={cur} / isThisSeat={isThisSeat}");
            }

            if (isSitting && isThisSeat)
            {
                fired = true;
                Debug.Log($"✅ [Seat:{name}] 착석 완료 감지! (이벤트 실행)");
                if (action != null)
                    action.enabled = false;
                OnSeatEventTriggered();
                yield break;
            }

            yield return null;
        }
    }

    private void OnSeatEventTriggered()
    {
        if (scene2Controller == null)
        {
            Debug.LogWarning($"⚠️ [Seat:{name}] Sequence2Scene2Controller를 못 찾았다.");
            return;
        }

        scene2Controller.StartCinemaSeatEvent(); // ✅ 여기서 이벤트 시작
    }
}
