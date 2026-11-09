using UnityEngine;

public class RoomCompletionGate : MonoBehaviour
{
    public enum RoomType
    {
        Room204,
        Room205,
        Room201
    }

    [Header("Room")]
    [SerializeField] private RoomType roomType;

    [Header("Scene Controller")]
    [SerializeField] private Sequence7Scene3Controller sceneController;

    [Header("Completion Objects")]
    [SerializeField] private Collider2D exitBlockCollider;
    [SerializeField] private GameObject roomHallLight;

    // 드레스 / 스파클 이벤트 완료
    private bool mainEventCompleted = false;

    // TV가 완전히 꺼짐
    private bool tvCompleted = false;

    // 최종 완료 중복 방지
    private bool roomCompleted = false;


    public bool MainEventCompleted => mainEventCompleted;
    public bool TVCompleted => tvCompleted;
    public bool RoomCompleted => roomCompleted;


    // 드레스 / 스파클 이벤트 완료 시 호출
    public void MarkMainEventCompleted()
    {
        if (mainEventCompleted)
            return;

        mainEventCompleted = true;

        Debug.Log(
            $"[{roomType}] 메인 이벤트 완료"
        );

        CheckRoomCompletion();
    }


    // TV가 실제로 꺼졌을 때 호출
    public void MarkTVCompleted()
    {
        if (tvCompleted)
            return;

        tvCompleted = true;

        Debug.Log(
            $"[{roomType}] TV 이벤트 완료"
        );

        CheckRoomCompletion();
    }


    private void CheckRoomCompletion()
    {
        if (roomCompleted)
            return;


        Debug.Log(
            $"[{roomType}] 완료 조건 검사 | " +
            $"메인 이벤트: {mainEventCompleted} / " +
            $"TV: {tvCompleted}"
        );


        // 둘 다 완료돼야 방 완료
        if (!mainEventCompleted || !tvCompleted)
            return;


        CompleteRoom();
    }


    private void CompleteRoom()
    {
        roomCompleted = true;


        // 출구 차단 해제
        if (exitBlockCollider != null)
        {
            exitBlockCollider.enabled = false;
        }


        // 복도의 해당 방 불빛 제거
        if (roomHallLight != null)
        {
            roomHallLight.SetActive(false);
        }


        // 최종 퍼즐 완료 상태 저장
        if (sceneController != null)
        {
            switch (roomType)
            {
                case RoomType.Room201:
                    sceneController.SetRoom201Completed();
                    break;

                case RoomType.Room204:
                    sceneController.SetRoom204Completed();
                    break;

                case RoomType.Room205:
                    sceneController.SetRoom205Completed();
                    break;
            }
        }


        Debug.Log(
            $"[{roomType}] ★ 방 퍼즐 최종 완료 / 출구 개방 ★"
        );
    }
}