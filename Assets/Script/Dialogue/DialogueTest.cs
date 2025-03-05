using UnityEngine;
using UnityEngine.UIElements;
using Yarn.Unity;

public class DialogueTest : MonoBehaviour
{

    private float rayDistance = 500f;
    public DialogueRunner dialogueRunner;
    public string startNodeName = "Start";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector2[] directions = new Vector2[]
            {
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right
            };

            foreach (Vector2 dir in directions)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, dir, rayDistance);
                Debug.DrawRay(transform.position, dir * rayDistance, Color.red, 0.5f);

                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject)
                    {
                        if (hit.collider.name == "PlayerHaru")
                        {
                            dialogueRunner.StartDialogue(startNodeName);
                            return; 
                        }
                    }
                }
            }
        }
    }
}