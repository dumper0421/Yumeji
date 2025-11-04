using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ChangeLightTrigger : MonoBehaviour
{
    public Light2D Light;
    public bool isTurnOn;
    private Collider2D _col;
    [SerializeField]
    private GameObject _BadCustomer;
    [SerializeField]
    private bool _isGusetEntry;
    [SerializeField]
    private Vector3 _targetPosition;

    public bool CanLeave;
    public DialogueManager DialogueManager;


    void Awake()
    {
        _col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Light.enabled = isTurnOn;
            // 중복 방지
            var col = GetComponent<BoxCollider2D>();
            StartCoroutine(ReenableAfter(0.2f));
        }

        if (_isGusetEntry)
            _BadCustomer.transform.position = _targetPosition;

        if (CanLeave)
        {
            DialogueManager.StartDialogue("Ken_Dialogue33");
        }
    }

    System.Collections.IEnumerator ReenableAfter(float sec)
    {
        _col.enabled = false;
        yield return new WaitForSeconds(sec);         
        if (_col) _col.enabled = true;
    }
}
