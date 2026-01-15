using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CompanionSystem : MonoBehaviour
{
    // Start is called before the first frame update
    #region 
    public Vector3 TargetPos;
    public Vector3 StartPos;
    public Vector3 Direction;

    public string CompanionName;

    public float MoveSpeed;

    [SerializeField]
    private float _speed = 1f;
    [SerializeField]
    private float _runSpeed = 2f;
    [SerializeField]
    private Animator _animator;
    [SerializeField]
    private BoxCollider2D _collider;
    #endregion

    void Start()
    {
        _animator = GetComponent<Animator>();   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetVulnerable(bool isVulnerable)
    {
        _collider.enabled = isVulnerable;
    }

    public void SetPosition(Vector3 targetPos, Vector3 direction)
    {
        TargetPos = targetPos;
        StartPos = transform.position;
        Direction = direction;

        StartCoroutine(MoveCoroutine());
    }

    public IEnumerator MoveCoroutine()
    {
        _animator.SetFloat("DirX", Direction.x);
        _animator.SetFloat("DirY", Direction.y);
        _animator.SetFloat("AnimSpeed", MoveSpeed);

        float elapsedTime = 0f;
        float moveDuration = 0.2f / MoveSpeed;

        _animator.SetBool("Walking", true);

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(StartPos, TargetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _animator.SetBool("Walking", false);
        transform.position = TargetPos;
    }

}
