using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType {Dirt, DirtGrass, Stone, StoneSnow, Sand, Ice, Invisible, None}

/// <summary>
/// Class representing Object Pool, which handles lazy creation of blocks
/// </summary>
public class BlocksPool : MonoBehaviour
{
    public static BlocksPool instance;
    public Transform availableBlocksParent;
    
    /// <summary>
    /// We have one set per block type, representing available blocks of that type
    /// </summary>
    HashSet<GameObject>[] availableBlocksSet = new HashSet<GameObject>[7];
    
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
        if (block == null || block.GetComponent<Block>() == null || block.GetComponent<Block>().properties == null)
        {
            Debug.Log("BAD");
        }

        var blockType = block.GetComponent<Block>().properties.blockType;
        block.transform.parent = availableBlocksParent;
        block.SetActive(false);

        availableBlocksSet[(int)blockType].Add(block);
    }

    /// <summary>
    /// Dynamically creates terrain after destroyed block
    /// </summary>
    /// <param name="block">Block to be destroyed</param>
    public void CoverHoles(GameObject block)
    {
        var blockTransform = block.transform;
        var blockTypeUnder = block.GetComponent<Block>().properties.blockUnderType;
        RaycastHit hit;

        var allDirections = new Vector3[] { Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back, Vector3.up };
        var directionsOfBlockReplace = new List<Vector3>();
        var blocksToReplace = new List<GameObject>();

        // Find on which side are invisible blocks and we will need to cover holes
        foreach (var direction in allDirections)
        {
            if (Physics.Raycast(block.transform.position, direction, out hit, 1))
            {
                var hittedGO = hit.collider.gameObject;
                if (hittedGO.CompareTag("Ground") && hittedGO.GetComponent<Block>().properties.blockType == BlockType.Invisible)
                {
                    directionsOfBlockReplace.Add(direction);
                    blocksToReplace.Add(hittedGO);
                }
            }
        }

        // Generate invisible blocks inside of terrain to be able to cover holes in future
        foreach (var blockDirection in directionsOfBlockReplace)
        {
            foreach (var direction in allDirections)
            {
                if (!Physics.Raycast(block.transform.position + blockDirection, direction, out hit, 1))
                {
                    var newBlock = GetBlockFromPool(BlockType.Invisible);
                    var chunk = TerrainGeneration.instance.GetChunkOnPosition(blocksToReplace[0].transform.position.x, blocksToReplace[0].transform.position.z);

                    newBlock.transform.position = block.transform.position + blockDirection + direction;
                    newBlock.transform.parent = chunk.invisibleBlocksParent.transform;
                }
            }

            // Refresh physics to make Raycast work
            Physics.SyncTransforms();
        }

        // Disable all invisible blocks and replace them with real ones
        foreach (var invisibleBlock in blocksToReplace)
        {
            var newBlock = GetBlockFromPool(blockTypeUnder);
            var chunk = TerrainGeneration.instance.GetChunkOnPosition(block.transform.position.x, block.transform.position.z);

            newBlock.transform.position = invisibleBlock.transform.position;
            newBlock.transform.parent = chunk.groundBlocksParent.transform;

            GiveBlockToPool(invisibleBlock);
        }        
    }

    BlockProperties GetBlockProperties(BlockType type)
    {
        return TerrainGeneration.instance.GetBlockProperties(type);
    }
}
