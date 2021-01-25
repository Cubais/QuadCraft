using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerNew : MonoBehaviour, IPLayerInput
{
    public float speed = 2f;
    public float rotationSpeed = 100f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public Transform cameraTransform;

    private float pitchRotation = 0;
    private CharacterController controller;
    private Vector3 velocity;
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            Debug.Log("True");
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
    }

    public void Move(Vector2 moveValue)
    {
        var move = transform.right * moveValue.x + transform.forward * moveValue.y;
        controller.Move(move * speed * Time.deltaTime);
    }

    public void Rotate(Vector2 rotation)
    {
        var rotX = rotation.x * rotationSpeed * Time.deltaTime;
        var rotY = rotation.y * rotationSpeed * Time.deltaTime;

        pitchRotation -= rotY;
        pitchRotation = Mathf.Clamp(pitchRotation, -60, 80);

        cameraTransform.localRotation = Quaternion.Euler(pitchRotation, 0f, 0f);
        transform.Rotate(Vector3.up * rotX);        
    }
}
