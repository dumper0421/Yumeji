using UnityEngine;

public class Room201EntryTrigger : MonoBehaviour
{
    [SerializeField]
    private Room201PhoneEvent phoneEvent;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;


        Debug.Log("[Room201] 플레이어 입장");


        if (phoneEvent != null)
        {
            phoneEvent.OnRoomEntered();
        }
    }
}