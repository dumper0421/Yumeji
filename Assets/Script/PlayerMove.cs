using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    public LayerMask layerMask;

    [SerializeField]
    private float speed_;
    [SerializeField]
    private float runSpeed_;

    private Vector3 vector;

  
    private float applyRunSpeed;
    private bool applyRunFlag= false;

    public int walkCount;
    private int currentWalkCount;

    private bool canMove = true;

    private Animator animator;

     void Start()
    {
     
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();

        if (StatusManager.Instance != null && StatusManager.Instance.playerStatus != null)
        {
            speed_ = StatusManager.Instance.playerStatus.BaseSpeed;
            runSpeed_ = StatusManager.Instance.playerStatus.RunSpeed;
        }
    }

    IEnumerator MoveCoroutine()
    {

        while (Input.GetAxisRaw("Vertical") != 0 || Input.GetAxisRaw("Horizontal") != 0)
        {
            //Run
            if (Input.GetKey(KeyCode.LeftShift))
            {
                applyRunSpeed = runSpeed_;
                applyRunFlag = true;
            }
            else
            {
                applyRunSpeed = 0;
                applyRunFlag = false;
            }

            vector.Set(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), transform.position.z);
            
            //방향키 2개누를 때 버그
            if (vector.x != 0)
            {
                vector.y = 0;
            }


            animator.SetFloat("DirX", vector.x);
            animator.SetFloat("DirY", vector.y);

            //이동 불가 지역
            RaycastHit2D hit;
            Vector2 start=transform.position; //현재위치
            Vector2 end=start+new Vector2(vector.x* speed_ * walkCount,vector.y* speed_ * walkCount);   //이동위치

            boxCollider.enabled = false; //플레이어 박스콜라이더에 인식되지 않게
            hit=Physics2D.Linecast(start,end,layerMask);
            boxCollider.enabled = true;
            if (hit.transform != null)
            {
                break;
            }

            animator.SetBool("Walking", true);

            //
            while (currentWalkCount < walkCount)
            {
                if (vector.x != 0)
                {
                    transform.Translate(vector.x * (speed_ + applyRunSpeed), 0, 0);
                }
                else if (vector.y != 0)
                {
                    transform.Translate(0, vector.y * (speed_ + applyRunSpeed), 0);
                }
                if (applyRunFlag)
                {
                    currentWalkCount++;
                }
                currentWalkCount++;
                yield return new WaitForSeconds(0.01f);
            }
            currentWalkCount = 0;
        }
        animator.SetBool("Walking", false);
        canMove = true;
    }

    void Update()
    {
        if (canMove)
        {
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
            {
                canMove = false;
                StartCoroutine(MoveCoroutine());
            }
        }
    }
        

 }

