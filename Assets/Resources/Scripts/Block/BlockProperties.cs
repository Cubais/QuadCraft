using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Block Properties")]
public class BlockProperties : ScriptableObject
{
    public Material blockMaterial;
    public BlockType blockType;

    public float bottomGenerationLevel;
    public float topGenerationLevel;

    public float destroyTime;

    public BlockType blockUnderType;

    public bool WithinHeightRange(float height)
    {
        return (height >= bottomGenerationLevel && height <= topGenerationLevel);
    }
}
