using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType{Dirt, DirtGrass, Stone, StoneSnow, Sand, Ice}

/// <summary>
/// Class representing Object Pool, which handles lazy creation of blocks
/// </summary>
public class BlocksPool : MonoBehaviour
{
    public static BlocksPool instance;
    
    /// <summary>
    /// We have one set per block type, representing available blocks of that type
    /// </summary>
    HashSet<GameObject>[] availableBlocksSet = new HashSet<GameObject>[6];

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        for (int i = 0; i < availableBlocksSet.Length; i++)
        {            
            availableBlocksSet[i] = new HashSet<GameObject>();            
        }
    }

    /// <summary>
    /// Get block of given type
    /// </summary>
    /// <param name="blockType">Type of the requested block</param>
    /// <returns></returns>
    public GameObject GetBlockFromPool(BlockType blockType)
    {
        var blockTypeInt = (int)blockType;        

        // Is some block of requested type available
        if (availableBlocksSet[blockTypeInt].Count != 0)
        {
            GameObject returnBlock = null;

            // Get first available block, little work-around for HashSet            
            foreach (var item in availableBlocksSet[blockTypeInt])
            {
                returnBlock = item;
                break;
            }
            
            availableBlocksSet[blockTypeInt].Remove(returnBlock);

            returnBlock.SetActive(true);
            return returnBlock;
        }

        // Don't have requested block available, create one
        var cube = Resources.Load<GameObject>("Prefabs/Quad");

        return Instantiate(cube);
    }

    /// <summary>
    /// Returns given block back to pool for reuse
    /// </summary>
    /// <param name="block">GameObject representing block</param>
    /// <param name="type">Type of block</param>
    public void GiveBlockToPool(GameObject block, BlockType type)
    {
        block.SetActive(false);
        availableBlocksSet[(int)type].Add(block);
    }
}
