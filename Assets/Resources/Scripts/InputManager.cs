using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;
        
    public GameObject playerObject;
    public Joystick moveJoystick;
    public RectTransform[] touchArea;

    public float minViewAngle;
    public float maxViewAngle;

    private Vector2 currentMoveInput;
    private Vector2 currentViewInput;
    private int viewTouchID = -1;

    private IPLayerInput player;

    private bool jump;
    private bool recieveInput = true;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }        
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null || !recieveInput)
            return;

        HandleInput();

        player.Move(currentMoveInput);
        player.Rotate(currentViewInput);

        if (jump)
        {
            player.Jump();
        }
    }

    public void SetPlayer(IPLayerInput player)
    {
        this.player = player;
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
        ProcessMobileViewInput();        

    #endif
    }

    private void ProcessMobileViewInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];

            // If we don't have touches for view input, check if there is any
            if (viewTouchID == -1)
            {
                if (GetTouchInsideArea(out touch, TouchPhase.Began))
                {
                    viewTouchID = touch.fingerId;
                    
                }
            }
            else
            {
                // We need to find finger with saved id
                var found = false;
                foreach (var t in Input.touches)
                {
                    if (t.fingerId == viewTouchID)
                    {
                        touch = t;
                        found = true;
                        break;
                    }
                }

                // Our touch for view input is still on the screen
                if (found)
                {
                    if (touch.phase == TouchPhase.Moved )
                    {
                        var deltaMove = touch.deltaPosition;                        
                        //deltaMove.Normalize();

                        currentViewInput = deltaMove;
                    }
                    else if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                    {
                        currentViewInput = Vector2.zero;
                        viewTouchID = -1;
                    }
                    else if (touch.phase == TouchPhase.Stationary)
                    {
                        currentViewInput = Vector2.zero;
                    }
                }
                else
                {
                    viewTouchID = -1;
                }
            }
        }
        else
        {
            viewTouchID = -1;
        } 
    }

    private bool InsideTouchArea(Vector2 touchPosition)
    {
        foreach (var area in touchArea)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(area, touchPosition))
            {
                return true;
            }
        }

        return false;
    }

    private bool GetTouchInsideArea(out Touch touch, TouchPhase phase = TouchPhase.Moved)
    {
        foreach (var t in Input.touches)
        {
            if (InsideTouchArea(t.position) && t.phase == phase)
            {
                touch = t;
                return true;
            }
        }

        touch = new Touch();
        return false;
    }

    public void RecieveInput(bool recieve)
    {
        this.recieveInput = recieve;
    }
}

public interface IPLayerInput
{
    void Move(Vector2 moveValue);
    void Rotate(Vector2 rotation);
    void Jump();
}
