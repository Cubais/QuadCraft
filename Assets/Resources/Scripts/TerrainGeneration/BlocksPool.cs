using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType {Dirt, DirtGrass, Stone, StoneSnow, Sand, Ice, None}

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
        var block = Resources.Load<GameObject>("Prefabs/Blocks/Block");
        block.GetComponent<Block>().properties = GetBlockProperties(blockType);

        return Instantiate(block);
    }

    /// <summary>
    /// Returns given block back to pool for reuse
    /// </summary>
    /// <param name="block">GameObject representing block</param>
    /// <param name="type">Type of block</param>
    public void GiveBlockToPool(GameObject block)
    {
        var blockType = block.GetComponent<Block>().properties.blockType;
        block.SetActive(false);

        availableBlocksSet[(int)blockType].Add(block);
    }

    public void CoverHoles(GameObject block)
    {
        var blockTransform = block.transform;
        RaycastHit hit;
        // Cover holes under removed block
        if (!Physics.Raycast(block.transform.position, -block.transform.up, out hit, 30))
        {
            var newBlock = GetBlockFromPool(block.GetComponent<Block>().properties.blockType);
            newBlock.transform.position = block.transform.position - block.transform.up;
        }

        var positions = new List<Vector3>();
        positions.Add(block.transform.position + new Vector3(1, 1, 0));
        positions.Add(block.transform.position + new Vector3(-1, 1, 0));
        positions.Add(block.transform.position + new Vector3(0, 1, 1));
        positions.Add(block.transform.position + new Vector3(0, 1, -1));

        foreach (var position in positions)
        {
            var collisions = Physics.OverlapSphere(position, 0.2f);
            if (collisions.Length > 0 && collisions[0].CompareTag("Ground"))
            {
                if (!Physics.Raycast(position, Vector3.down, 1))
                {
                    var newBlock = GetBlockFromPool(collisions[0].GetComponent<Block>().properties.blockUnderType);
                    newBlock.transform.position = position + Vector3.down;
                }
            }
        }
    }

    BlockProperties GetBlockProperties(BlockType type)
    {
        return TerrainGeneration.instance.GetBlockProperties(type);
    }
}
