using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    public PlayerController player;

    Vector2 currentMoveInput;
    bool jump;

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
        if (!player)
            return;

        HandleInput();

        player.Move(currentMoveInput);

        if (jump)
        {
            player.Jump();
        }
        
    }

    private void HandleInput()
    {        
        currentMoveInput.x = Input.GetAxisRaw("Horizontal");
        currentMoveInput.y = Input.GetAxisRaw("Vertical");

        jump = Input.GetKeyDown(KeyCode.Space);
    }
}

public interface IPLayerInput
{
    void Move(Vector2 moveValue);
    void Jump();
}
