using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, IPLayerInput
{
    public float speed = 2f;
    public float rotationSpeed = 100f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public Transform cameraTransform;

    public float buildingModeRange = 4f;

    private float pitchRotation = 0;
    private CharacterController controller;
    private Vector3 velocity;

    private bool inBuildingMode = false;
    private bool inDestroyingMode = false;
    private GameObject currentDestroyingBlock;
    private IEnumerator currentDestroyingCoroutine;
    private bool buildingButtonPressed = false;
    private BlockType currentBlockToBuildType = BlockType.Dirt;
    private GameObject buildingModeBlock;
    private GameObject destroyingModeBlock;    

    void Start()
    {
        controller = GetComponent<CharacterController>();

        var buildingCube = Resources.Load<GameObject>("Prefabs/Blocks/BuildingCube");
        buildingModeBlock = Instantiate(buildingCube);
        buildingModeBlock.transform.SetParent(this.transform);

        var destroyingCube = Resources.Load<GameObject>("Prefabs/Blocks/DestroyingCube");
        destroyingModeBlock = Instantiate(destroyingCube);
        destroyingModeBlock.transform.SetParent(this.transform);

        buildingModeBlock.SetActive(false);
        destroyingModeBlock.SetActive(false);

        UIManager.instance.toggleBuildingMode += SetBuildingMode;
        UIManager.instance.toggleDestroyingMode += SetDestroyingMode;
        UIManager.instance.blockSwitch += BlockSelectionSwitch;
        UIManager.instance.buildingButtonPressed += BuildingButtonPressed;
        UIManager.instance.buildingButtonReleased += BuildingButtonReleased;

        //Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;            
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    #region Movement

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

    #endregion

    #region BuildingMode

    void BuildingButtonPressed()
    {
        buildingButtonPressed = true;
    }

    void BuildingButtonReleased()
    {
        buildingButtonPressed = false;
    }

    void BlockSelectionSwitch(BlockType type)
    {
        currentBlockToBuildType = type;
    }

    /// <summary>
    /// Changes building mode and calls moving of building block
    /// </summary>
    /// <param name="active">Should be building mode on/off</param>
    void SetBuildingMode(bool active)
    {
        inBuildingMode = active;

        if (inBuildingMode)
        {

            StartCoroutine(MoveBuildingBlock());
        }
        else
        {
            inDestroyingMode = false;
        }
    }

    /// <summary>
    /// Changes destroying mode and calls moving of destroying block
    /// </summary>
    /// <param name="active">Should be destroying mode on/off</param>
    void SetDestroyingMode(bool active)
    {
        inDestroyingMode = active;
        if(inDestroyingMode)
        {
            StartCoroutine(MoveDestroyingBlock());
        }
        else
        {
            StopCoroutine(MoveBuildingBlock());
            StartCoroutine(MoveBuildingBlock());
        }
    }

    /// <summary>
    /// Moves building block, indicating possible location of placing block
    /// </summary>    
    IEnumerator MoveBuildingBlock()
    {
        buildingModeBlock.SetActive(true);
        int playerMask =~ LayerMask.GetMask("Player");

        while (inBuildingMode && !inDestroyingMode)
        {
            var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, buildingModeRange, playerMask))
            {
                buildingModeBlock.SetActive(true);

                var position = hit.transform.position + hit.normal;
                buildingModeBlock.transform.position = position;
                buildingModeBlock.transform.rotation = Quaternion.Euler(0, 0, 0);

                if (buildingButtonPressed)
                {
                    buildingButtonPressed = false;
                    var block = BlocksPool.instance.GetBlockFromPool(currentBlockToBuildType);
                    var chunk = TerrainGeneration.instance.GetChunkOnPosition(transform.position.x, transform.position.z);
                    chunk.SetChangedTerrain(true);

                    block.transform.position = position;
                    block.transform.parent = chunk.groundBlocksParent.transform;                    
                }
            }
            else
            {
                buildingModeBlock.SetActive(false);
            }

            yield return null;
        }

        buildingModeBlock.SetActive(false);
    }

    /// <summary>
    /// Moves transparent red block, which indicates block to be removed
    /// Handles starting proccess of block removal
    /// </summary>    
    IEnumerator MoveDestroyingBlock()
    {
        destroyingModeBlock.SetActive(true);
        int playerMask = ~LayerMask.GetMask("Player");

        while (inBuildingMode && inDestroyingMode)
        {
            // Sending ray from the center of the screen
            var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, buildingModeRange, playerMask))
            {               
                destroyingModeBlock.SetActive(true);
                destroyingModeBlock.transform.position = hit.transform.position;
                destroyingModeBlock.transform.rotation = Quaternion.Euler(0, 0, 0);

                if (buildingButtonPressed)
                {
                    // Start destroying object if none is currently in destroying process
                    if (!currentDestroyingBlock)
                    {
                        currentDestroyingCoroutine = DestroyBlock(hit.transform.gameObject);
                        StartCoroutine(currentDestroyingCoroutine);
                    }
                    // If we are in destroying process and we changed focus on another block, stop destroying
                    else if (currentDestroyingBlock != hit.transform.gameObject)
                    {
                        ResetDestroyingProcess();
                    }
                }                
            }
            else
            {
                ResetDestroyingProcess();
                destroyingModeBlock.SetActive(false);
            }

            yield return null;
        }

        ResetDestroyingProcess();
        destroyingModeBlock.SetActive(false);
    }

    /// <summary>
    /// Stops current destroying coroutine and restores variables and slider
    /// </summary>
    void ResetDestroyingProcess()
    {
        if (currentDestroyingCoroutine != null)
        {
            StopCoroutine(currentDestroyingCoroutine);
            currentDestroyingCoroutine = null;
        }

        currentDestroyingBlock = null;        
        UIManager.instance.SetDestroyingSlider(0);
    }

    IEnumerator DestroyBlock(GameObject blockToDestroy)
    {
        currentDestroyingBlock = blockToDestroy;
        var time = Time.realtimeSinceStartup;
        
        var destroyTime = blockToDestroy.GetComponent<Block>().properties.destroyTime;

        while(buildingButtonPressed && inBuildingMode && inDestroyingMode)
        {
            if ((Time.realtimeSinceStartup - time) >= destroyTime)
            {
                BlocksPool.instance.CoverHoles(blockToDestroy);
                BlocksPool.instance.GiveBlockToPool(blockToDestroy);

                var chunk = TerrainGeneration.instance.GetChunkOnPosition(blockToDestroy.transform.position.x, blockToDestroy.transform.position.z);
                chunk.SetChangedTerrain(true);

                break;
            }
            
            UIManager.instance.SetDestroyingSlider((Time.realtimeSinceStartup - time) / destroyTime);
            yield return null;
        }

        UIManager.instance.SetDestroyingSlider(0);
        currentDestroyingBlock = null;
    }

    #endregion
}
