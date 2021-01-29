using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction{ UP, RIGHT, DOWN, LEFT};
public class TerrainChunk : MonoBehaviour
{
    public int maxChunkHeight;
    public Transform invisibleBlocksParent;
    public Transform groundBlocksParent;

    // Representation of terrain chunk in height map
    public Texture2D heightMap { get; private set; }

    TerrainChunk[] neighbours = new TerrainChunk[4];

    public Vector2 noiseOffset { get; private set; }
    int chunkSize;
    float scale;

    bool changedTerrain = false;
    List<BlockSaveData> groundBlocksData = new List<BlockSaveData>();
    List<BlockSaveData> invisibleBlocksData = new List<BlockSaveData>();

    private void Awake()
    {
        this.transform.parent = TerrainGeneration.instance.terrainChunksParent.transform;
    }

    /// <summary>
    /// Get neighbouring chunk in given direction
    /// </summary>
    /// <param name="direction">Direction of neighbouring chunk</param>
    /// <returns>Neighbouring chunk in the requested direction or null</returns>
    public TerrainChunk GetNeighbour(Direction direction)
    {
        return neighbours[(int)direction];
    }

    /// <summary>
    /// Sets neighbouring chunk
    /// </summary>
    /// <param name="direction">Where is neighbouring chunk</param>
    /// <param name="chunk">Neighbouring chunk</param>
    public void SetNeighbour(Direction direction, TerrainChunk chunk)
    {
        neighbours[(int)direction] = chunk;
    }

    /// <summary>
    /// Generates heightmap and blocks for this chunk
    /// </summary>
    /// <param name="size">Size of chunk</param>
    /// <param name="noiseScale">Scale of perlin noise</param>
    /// <param name="noiseOffset">Offset of perlin noise</param>
    public void GenerateChunk(int size, float noiseScale, Vector2 noiseOffset)
    {
        this.chunkSize = size;
        this.noiseOffset = noiseOffset;
        this.scale = noiseScale;

        heightMap = GenerateHeightMap();
        LoadChunk();
    }

    /// <summary>
    /// Generates heightmap for chunk using PerlinNoise
    /// </summary>
    /// <returns></returns>
    private Texture2D GenerateHeightMap()
    {
        var texture = new Texture2D(chunkSize, chunkSize);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                texture.SetPixel(x, y, GetNoise(x, y));
            }
        }

        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Perlin noise represented by color
    /// </summary>
    /// <param name="x">X position on the chunk</param>
    /// <param name="y">Y position on the chunk</param>
    /// <returns></returns>
    private Color GetNoise(float x, float y)
    {
        var xCoord = (x / chunkSize) * scale + noiseOffset.x;
        var yCoord = (y / chunkSize) * scale + noiseOffset.y;

        var noise = Mathf.PerlinNoise(xCoord, yCoord);

        return new Color(noise, noise, noise);
    }

    public void SetChangedTerrain(bool changed)
    {
        changedTerrain = changed;
    }

    /// <summary>
    /// Disables blocks of the chunk
    /// </summary>
    public void DisableChunk()
    {
        groundBlocksData = new List<BlockSaveData>();
        invisibleBlocksData = new List<BlockSaveData>();

        // Need to copy blocks first, because blockPool changes block's parent
        var groundBlocksChildren = new List<GameObject>();
        var invisibleBlocksChildren = new List<GameObject>();
        foreach (Transform b in groundBlocksParent)
        {
            groundBlocksChildren.Add(b.gameObject);            
        }

        foreach (Transform b in invisibleBlocksParent)
        {
           invisibleBlocksChildren.Add(b.gameObject);
        }

        foreach (var block in groundBlocksChildren)
        {
            if (changedTerrain)
            {
                var blockData = new BlockSaveData(block.transform.position, block.GetComponent<Block>().properties.blockType);
                groundBlocksData.Add(blockData);
            }

            BlocksPool.instance.GiveBlockToPool(block);
        }

        foreach (var block in invisibleBlocksChildren)
        {
            if (changedTerrain)
            {
                var blockData = new BlockSaveData(block.transform.position, BlockType.Invisible);
                invisibleBlocksData.Add(blockData);
            }

            BlocksPool.instance.GiveBlockToPool(block);
        }                
    }

    /// <summary>
    /// Loads chunk blocks
    /// </summary>
    public void LoadChunk()
    {
        if (!changedTerrain)
        {
            LoadChunkFromHeightMap();
            Debug.Log("HeightMap load");
        }
        else
        {
            LoadChunkFromData();
            Debug.Log("DataLoad");
        }
    }

    void LoadChunkFromHeightMap()
    {
        if (!heightMap)
            Debug.LogError("No heightMap present, cannot generate cubes");

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                // Get floored height, position in world and block type based on height
                var height = GetBlockHeight(x, y);
                var position = new Vector3(transform.position.x + x, height, transform.position.z + y);
                var blockType = TerrainGeneration.instance.BlockTypeOnHeight(height, maxChunkHeight, true);

                var block = BlocksPool.instance.GetBlockFromPool(blockType);
                block.transform.position = position;
                block.transform.parent = groundBlocksParent;

                // Calculate number of block to generate under current one to fill gap
                var blockUnderCount = BlocksUnder(x, y, height);

                for (int i = 0; i < blockUnderCount; i++)
                {
                    position.y--;
                    blockType = TerrainGeneration.instance.BlockTypeOnHeight((int)position.y, maxChunkHeight, false);

                    block = BlocksPool.instance.GetBlockFromPool(blockType);
                    block.transform.position = position;
                    block.transform.parent = groundBlocksParent;
                }

                if (!Physics.Raycast(position, Vector3.down, 1f))
                {
                    block = BlocksPool.instance.GetBlockFromPool(BlockType.Invisible);
                    position.y--;
                    block.transform.position = position;
                    block.transform.parent = invisibleBlocksParent;
                }
            }
        }

        Physics.SyncTransforms();

        var blocksUnder = invisibleBlocksParent.transform.GetComponentsInChildren<Block>();
        foreach (var block in blocksUnder)
        {
            //Debug.DrawRay(block.transform.position, Vector3.down, Color.blue, 1000f);

            bool createNextBlock = true;
            var position = block.transform.position;

            while (createNextBlock)
            {
                createNextBlock = false;
                if (!Physics.Raycast(position, Vector3.down, 1f))
                {
                    if (IsBlockInFourDirections(position + Vector3.down))
                    {
                        position = position + Vector3.down;
                        var invBlock = BlocksPool.instance.GetBlockFromPool(BlockType.Invisible);
                        invBlock.transform.position = position;
                        invBlock.transform.parent = invisibleBlocksParent;

                        createNextBlock = true;
                    }
                }
            }
        }
    }

    void LoadChunkFromData()
    {
        foreach (var gBlock in groundBlocksData)
        {
            var block = BlocksPool.instance.GetBlockFromPool((BlockType)gBlock.type);
            block.transform.position = gBlock.position.GetVector3();
            block.transform.parent = groundBlocksParent.transform;
        }

        foreach (var iBlock in invisibleBlocksData)
        {
            var block = BlocksPool.instance.GetBlockFromPool(BlockType.Invisible);
            block.transform.position = iBlock.position.GetVector3();
            block.transform.parent = invisibleBlocksParent.transform;
        }
    }

    bool IsBlockInFourDirections(Vector3 position)
    {
        Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        RaycastHit hit;
        foreach (var direction in directions)
        {
            if (Physics.Raycast(position, direction, out hit, 1f))
            {
                if (hit.transform.gameObject.GetComponent<Block>().properties.blockType != BlockType.Invisible)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Looks to the neighbouring blocks and determines, how many block we need to cover gap
    /// </summary>
    /// <param name="x">X coord in the heightmap</param>
    /// <param name="y">Y coord in the heightmap</param>
    /// <param name="height">Minimal required number of blocks to fill the gap</param>
    /// <returns></returns>
    private int BlocksUnder(int x, int y, int height)
    {
        var blockHeights = new List<int>();

        // Look at the block on the LEFT
        if (x != 0)
        {
            blockHeights.Add(GetBlockHeight(x - 1, y));
        }

        // Look at the block on the RIGHT
        if (x != (chunkSize - 1))
        {
            blockHeights.Add(GetBlockHeight(x + 1, y));
        }

        // Look at the BOTTOM block
        if (y != 0)
        {
            blockHeights.Add(GetBlockHeight(x, y - 1));
        }

        // Look at the UPPER block
        if (y != (chunkSize - 1))
        {
            blockHeights.Add(GetBlockHeight(x, y + 1));
        }

        // Find block with the lowest height
        var minimalHeight = Mathf.Min(blockHeights.ToArray());

        return (height - minimalHeight - 1);
    }

    /// <summary>
    /// Get height of block from the heightmap of terrain chunk
    /// </summary>
    /// <param name="x">X coord on the height map</param>
    /// <param name="y">Y coord on the height map</param>
    /// <returns>Height of block at the given position</returns>
    private int GetBlockHeight(int x, int y)
    {
        var height = heightMap.GetPixel(x, y).grayscale * maxChunkHeight;
        height = Mathf.FloorToInt(height);

        return (int)height;
    }

    /// <summary>
    /// Fill TerrainChunkData with appropriate data to be saved
    /// </summary>
    /// <returns>TerrainChunkData instance with fill informations about chunk</returns>
    public TerrainChunkData SaveChunkData()
    {
        var chunkData = new TerrainChunkData();
        chunkData.worldPosition = new SVector2();
        chunkData.worldPosition.x = transform.position.x;
        chunkData.worldPosition.y = transform.position.z;

        chunkData.chunkHeight = maxChunkHeight;
        chunkData.noiseOffset = new SVector2(noiseOffset);

        chunkData.changedTerrain = changedTerrain;
        chunkData.terrainChunkTexture = (!changedTerrain) ? heightMap.EncodeToPNG() : null;

        // If terrain was changed, we need to save each cube, otherwise we will generate terrain from heightmap
        if (changedTerrain)
        {
            chunkData.groundBlocks = new List<BlockSaveData>();
            chunkData.invisibleBlocks = new List<BlockSaveData>();

            // This chunk has not been saved yet, need to store all cubes informations
            if (groundBlocksData.Count == 0)
            {
                Transform block;
                BlockType type;
                for (int i = 0; i < groundBlocksParent.childCount; i++)
                {
                    block = groundBlocksParent.GetChild(i);
                    type = block.GetComponent<Block>().properties.blockType;

                    var blockData = new BlockSaveData(block.transform.position, type);
                    chunkData.groundBlocks.Add(blockData);
                }

                for (int i = 0; i < invisibleBlocksParent.childCount; i++)
                {
                    block = invisibleBlocksParent.GetChild(i);
                    var blockData = new BlockSaveData(block.transform.position, BlockType.Invisible);
                    chunkData.invisibleBlocks.Add(blockData);
                }
            }
            else
            {
                chunkData.groundBlocks = groundBlocksData;
                chunkData.invisibleBlocks = invisibleBlocksData;
            }
        }

        return chunkData;
    }

    public void LoadChunkData(TerrainChunkData data,int chunkSize, float noiseScale)
    {
        this.chunkSize = chunkSize;
        this.scale = noiseScale;

        maxChunkHeight = data.chunkHeight;
        noiseOffset = new Vector2(data.noiseOffset.x, data.noiseOffset.y);
        changedTerrain = data.changedTerrain;
        heightMap = GenerateHeightMap();

        if (!changedTerrain)
        {
            heightMap.LoadImage(data.terrainChunkTexture);
        }
        else
        {
            groundBlocksData = data.groundBlocks;
            invisibleBlocksData = data.invisibleBlocks;            
        }
    }    
}
