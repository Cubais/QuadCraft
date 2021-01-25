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

    public Button[] blockButtons;
    public Slider destroyingSlider;
    bool isDestroyingMode = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }

    public void OnToggleBuildingMode(bool active)
    {
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
