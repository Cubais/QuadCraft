using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, IPLayerInput
{
    public bool selfControlled = false;

    Rigidbody rigid;
    Vector3 moveDirection = Vector3.zero;
    bool canJump = false;
    public float speed = 2.0f;
    float height = 0;

    // Start is called before the first frame update
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (selfControlled)
        {
            rigid.MovePosition(transform.position + Vector3.forward * speed * Time.deltaTime);
        }

        if (canJump)
        {
            Jump();
            canJump = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {        
        // First trigger detects
        if (other.CompareTag("Ground") && !canJump)
        {
            canJump = true;                        
        }
        // Second trigger detects, switch off canJump, because 2 blocks are in front of player
        else if (other.CompareTag("Ground") && canJump)
        {
            canJump = false;            
        }
    }

    public void Move(Vector2 moveValue)
    {
        if (moveValue == Vector2.zero)
            return;

        var moveVector = new Vector3(moveValue.x, 0, moveValue.y);
        moveVector *= speed * Time.deltaTime;

        rigid.MovePosition(transform.position + moveVector);
    }

    public void Jump()
    {
        rigid.AddForce(Vector3.up * 5.2f, ForceMode.Impulse);
    }
}
