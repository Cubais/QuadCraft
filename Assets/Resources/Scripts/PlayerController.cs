using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, IPLayerInput
{
    public Transform cameraObject;
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 2.0f;

    Rigidbody rigid;

    Vector3 moveDirection = Vector3.zero;    

    // Start is called before the first frame update
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    public void Move(Vector2 moveValue)
    {
        if (moveValue == Vector2.zero)
            return;

        var moveVector = transform.forward * moveValue.y + transform.right * moveValue.x;
        moveVector *= moveSpeed * Time.deltaTime;

        rigid.MovePosition(transform.position + moveVector);
    }

    public void Jump()
    {
        rigid.AddForce(Vector3.up * 5.2f, ForceMode.Impulse);
    }

    public void Rotate(Vector2 rotation)
    {
        if (rotation == Vector2.zero)
            return;
        var minViewAngle = InputManager.instance.minViewAngle;
        var maxViewAngle = InputManager.instance.maxViewAngle;

        var playerRoatation = rotation.x * rotationSpeed * Time.deltaTime;

        this.transform.Rotate(0, playerRoatation, 0);

        var cameraRotation = rotation.y * rotationSpeed * Time.deltaTime;
        var rot = cameraObject.rotation.eulerAngles + new Vector3(cameraRotation, 0, 0);
        rot.x = (rot.x < minViewAngle) ? minViewAngle : rot.x;
        rot.x = (rot.x > maxViewAngle) ? maxViewAngle : rot.x;

        cameraObject.eulerAngles = rot;
    }
}
