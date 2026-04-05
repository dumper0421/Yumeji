using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveTrigger : MonoBehaviour
{
    [SerializeField] private List<GameObject> _objects = new List<GameObject>();
    [SerializeField] private bool _active;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            foreach (GameObject obj in _objects)
            {
                obj.SetActive(_active);
            }
        }
    }
}
