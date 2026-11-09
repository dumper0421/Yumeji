using UnityEngine;

public class Room205EventTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        BathroomEnter,
        ReturnToRoom
    }


    [SerializeField]
    private TriggerType triggerType;

    [SerializeField]
    private Room205SilhouetteEvent silhouetteEvent;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;


        switch (triggerType)
        {
            case TriggerType.BathroomEnter:

                Debug.Log("[Room205 Trigger] 화장실 진입 감지");

                silhouetteEvent.EnterBathroom();

                break;


            case TriggerType.ReturnToRoom:

                Debug.Log("[Room205 Trigger] 방 복귀 감지");

                silhouetteEvent.ReturnToRoom();

                break;
        }
    }
}