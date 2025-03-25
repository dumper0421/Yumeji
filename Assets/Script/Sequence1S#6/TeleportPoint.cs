using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    [SerializeField]
    private Vector2 TeleportPosition;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") && !collision.CompareTag("Enemy"))
            return;

        if (collision.CompareTag("Enemy"))
        {
            AstarEnemy enemy = collision.GetComponent<AstarEnemy>();
            if (enemy != null)
            {
                enemy.CancelMovement();
                collision.GetComponent<EnemyPathfinder>().FinalNodeList.Clear();
            }
        }

        collision.transform.position = TeleportPosition;
    }
}
