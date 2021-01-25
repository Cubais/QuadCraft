using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;
        
    public GameObject playerObject;
    public Joystick moveJoystick;
    public Joystick viewJoystick;

    public float minViewAngle = 60;
    public float maxViewAngle = -20;

    Vector2 currentMoveInput;
    Vector2 currentViewInput;

    private IPLayerInput player;
    
    bool jump;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }

        player = playerObject.GetComponent<IPLayerInput>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
            return;

        HandleInput();

        player.Move(currentMoveInput);
        player.Rotate(currentViewInput);

        if (jump)
        {
            player.Jump();
        }
        
    }

    private void HandleInput()
    {
        #if UNITY_EDITOR_WIN

        currentMoveInput.x = Input.GetAxisRaw("Horizontal");
        currentMoveInput.y = Input.GetAxisRaw("Vertical");

        currentViewInput.x = Input.GetAxis("Mouse X");
        currentViewInput.y = Input.GetAxis("Mouse Y");

        jump = Input.GetKeyDown(KeyCode.Space);

        #endif

        #if UNITY_ANDROID

        currentMoveInput.x = moveJoystick.Horizontal;
        currentMoveInput.y = moveJoystick.Vertical;

        currentViewInput.x = viewJoystick.Horizontal;
        currentViewInput.y = viewJoystick.Vertical;

        #endif
    }
}

public interface IPLayerInput
{
    void Move(Vector2 moveValue);
    void Rotate(Vector2 rotation);
    void Jump();
}
