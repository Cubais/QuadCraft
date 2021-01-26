using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public event Action<bool> toggleBuildingMode;
    public event Action<bool> toggleDestroyingMode;
    public event Action<BlockType> blockSwitch;
    public event Action buildingButtonPressed;
    public event Action buildingButtonReleased;

    public Button[] blockButtons;
    public Slider destroyingSlider;
    bool isDestroyingMode = false;
    bool inBuildingMode = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            OnToggleBuildingMode(!inBuildingMode);
        }

        if (inBuildingMode)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                OnBlockSwitch((int)BlockType.Dirt);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                OnBlockSwitch((int)BlockType.Sand);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                OnBlockSwitch((int)BlockType.Stone);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                OnBlockSwitch((int)BlockType.Ice);
            }            

            if (Input.GetKeyDown(KeyCode.E))
            {
                OnToggleDestroyingMode();
            }

            if (Input.GetMouseButtonDown(0))
            {
                OnBuildingButtonPressed();
            }

            if (Input.GetMouseButtonUp(0))
            {
                OnBuildingButtonReleased();
            }
        }
    }
    public void OnBuildingButtonPressed()
    {
        if (buildingButtonPressed != null)
        {
            buildingButtonPressed();
        }
    }

    public void OnBuildingButtonReleased()
    {
        if (buildingButtonReleased != null)
        {
            buildingButtonReleased();
        }
    }

    public void OnToggleBuildingMode(bool active)
    {
        inBuildingMode = active; // TODO: Remove after testing
        ResetDestroyingMode();

        if (toggleBuildingMode != null)
        {
            toggleBuildingMode(active);
        }
    }

    public void OnToggleDestroyingMode()
    {
        isDestroyingMode = !isDestroyingMode;
        SetInteractableBlocks(!isDestroyingMode);

        if (toggleDestroyingMode != null)
        {
            toggleDestroyingMode(isDestroyingMode);
        }
    }

    public void OnBlockSwitch(int type)
    {
        if (blockSwitch != null)
        {
            blockSwitch((BlockType)type);
        }
    }

    public void SetDestroyingSlider(float value)
    {        
        destroyingSlider.value = value;
    }

    void SetInteractableBlocks(bool active)
    {
        foreach (var block in blockButtons)
        {
            block.interactable = active;
        }

        destroyingSlider.gameObject.SetActive(!active);
    }

    private void ResetDestroyingMode()
    {
        isDestroyingMode = false;
    }
}
