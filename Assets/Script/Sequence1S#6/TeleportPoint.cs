using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    [SerializeField]
    private Vector2 TeleportPosition;
    [SerializeField]
    private Sequence1Scene6Controller _controller;
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
        else
        {
            int index = (Mathf.Abs((int)TeleportPosition.x) - 24) / 40;
            _controller.SetCinemachinePriority(index);
        }

        Debug.Log(collision.name);
        collision.transform.position = TeleportPosition;
    }
}
