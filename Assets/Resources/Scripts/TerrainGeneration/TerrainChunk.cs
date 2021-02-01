using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public enum Direction{ UP, RIGHT, DOWN, LEFT};
public class TerrainChunk : MonoBehaviour
{
    public int maxChunkHeight;
    public Transform invisibleBlocksParent;
    public Transform groundBlocksParent;

    // Representation of terrain chunk in height map
    public Texture2D HeightMap { get; private set; }
    public Vector2 NoiseOffset { get; private set; }

    private TerrainChunk[] neighbours = new TerrainChunk[4];
    
    private int chunkSize;
    private float scale;
    private bool changedTerrain = false;

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
        this.NoiseOffset = noiseOffset;
        this.scale = noiseScale;

        HeightMap = GenerateHeightMap();
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
                texture.SetPixel(x, y, GetNoise(x, y, NoiseOffset));
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
    private Color GetNoise(float x, float y, Vector2 noiseOffset)
    {
        var xCoord = (x / chunkSize) * scale + noiseOffset.x;
        var yCoord = (y / chunkSize) * scale + noiseOffset.y;

        var noise = Mathf.PerlinNoise(xCoord, yCoord);

        return new Color(noise, noise, noise);
    }

    /// <summary>
    /// Sets whether original terrain was changed
    /// </summary>
    /// <param name="changed">Terrain changed</param>
    public void SetChangedTerrain(bool changed)
    {
        changedTerrain = changed;
    }

    /// <summary>
    /// Get specific key for this terrain chunk
    /// </summary>    
    public string GetKey()
    {
        return this.transform.position.x + "_" + this.transform.position.z;
    }

    /// <summary>
    /// Get specific terrain chunk key in the given direction
    /// </summary>
    /// <param name="dir">Direction of requested key of terrain chunk</param>
    /// <returns></returns>
    public string GetKey(Direction dir)
    {
        string key= "";
        switch (dir)
        {
            case Direction.UP:
                key = (this.transform.position.x) + "_" + (this.transform.position.z + chunkSize);
                break;
            case Direction.RIGHT:
                key = (this.transform.position.x + chunkSize) + "_" + (this.transform.position.z);
                break;
            case Direction.DOWN:
                key = (this.transform.position.x) + "_" + (this.transform.position.z - chunkSize);
                break;
            case Direction.LEFT:
                key = (this.transform.position.x - chunkSize) + "_" + (this.transform.position.z);
                break;
            default:
                break;
        }

        return key;
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
            StartCoroutine(LoadChunkFromHeightMap());
            Debug.Log("HeightMap load");
        }
        else
        {
            LoadChunkFromData();
            Debug.Log("DataLoad");
        }
    }

    IEnumerator LoadChunkFromHeightMap()
    {
        if (!HeightMap)
            Debug.LogError("No heightMap present, cannot generate blocks");

        var time = Time.realtimeSinceStartup;
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

                // Fill gap
                for (int i = 0; i < blockUnderCount; i++)
                {
                    position.y--;
                    blockType = TerrainGeneration.instance.BlockTypeOnHeight((int)position.y, maxChunkHeight, false);

                    block = BlocksPool.instance.GetBlockFromPool(blockType);
                    block.transform.position = position;
                    block.transform.parent = groundBlocksParent;
                }

                // Create invisible block under each block, to be able to dynamically create terrain
                if (!Physics.Raycast(position, Vector3.down, 1f))
                {
                    block = BlocksPool.instance.GetBlockFromPool(BlockType.Invisible);
                    position.y--;
                    block.transform.position = position;
                    block.transform.parent = invisibleBlocksParent;
                }
            }

            if (Time.realtimeSinceStartup - time > 0.08)
            {
                Debug.Log("Switch");
                yield return null;
                time = Time.realtimeSinceStartup;
            }
        }

        Physics.SyncTransforms();

        // Create invisible block to be able to dynamically create terrain
        var blocksUnder = invisibleBlocksParent.transform.GetComponentsInChildren<Block>();
        foreach (var block in blocksUnder)
        {
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

            if (Time.realtimeSinceStartup - time > 0.08)
            {
                Debug.Log("Switch");
                yield return null;
                time = Time.realtimeSinceStartup;
            }
        }
    }

    /// <summary>
    /// Loads terain chunk from stored data
    /// </summary>
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

    /// <summary>
    /// Is invisible block in the forward, back, left, right directions ?
    /// </summary>
    /// <param name="position">Position from which condition is tested</param>
    /// <returns></returns>
    bool IsBlockInFourDirections(Vector3 position)
    {
        Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (var direction in directions)
        {
            if (Physics.Raycast(position, direction, out RaycastHit hit, 1f))
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
    /// <param name="height">Maximal required number of blocks to fill the gap</param>
    /// <returns>Number of block to be generated to fill the gap</returns>
    private int BlocksUnder(int x, int y, int height)
    {
        var blockHeights = new List<int>();

        // Look at the block on the LEFT within this chunk
        if (x != 0)
        {
            blockHeights.Add(GetBlockHeight(x - 1, y));
        }        
        else
        {
            blockHeights.Add(Mathf.FloorToInt(height - scale));
        }

        // Look at the block on the RIGHT within this chunk
        if (x != chunkSize)
        {
            blockHeights.Add(GetBlockHeight(x + 1, y));
        }        
       
        // Look at the BOTTOM block within this chunk
        if (y != 0)
        {
            blockHeights.Add(GetBlockHeight(x, y - 1));
        }        
        else
        {
            blockHeights.Add(Mathf.FloorToInt(height - scale));
        }

        // Look at the UPPER block within this chunk
        if (y != chunkSize)
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
        var height = HeightMap.GetPixel(x, y).grayscale * maxChunkHeight;
        height = Mathf.FloorToInt(height);

        return (int)height;
    }

    /// <summary>
    /// Get height of block from the given color
    /// </summary>
    /// <param name="color">Color representing height</param>
    /// <returns>Height based on the given color</returns>
    private int GetBlockHeight(Color color)
    {
        var height = color.grayscale * maxChunkHeight;
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
        chunkData.noiseOffset = new SVector2(NoiseOffset);

        chunkData.changedTerrain = changedTerrain;
        chunkData.terrainChunkTexture = (!changedTerrain) ? HeightMap.EncodeToPNG() : null;

        // If terrain was changed, we need to save each cube, otherwise we will generate terrain from heightmap
        if (changedTerrain)
        {
            // This chunk is not active, save last blocks data
            if (groundBlocksParent.childCount == 0)
            {
                chunkData.groundBlocks = groundBlocksData;
                chunkData.invisibleBlocks = invisibleBlocksData;
            }
            // If chunk is active, some changes can happened, so save all blocks from scratch
            else
            {
                groundBlocksData = new List<BlockSaveData>();
                invisibleBlocksData = new List<BlockSaveData>();

                // Load block data
                foreach (Transform block in groundBlocksParent)
                {
                    var blockData = new BlockSaveData(block.position, block.GetComponent<Block>().properties.blockType);
                    groundBlocksData.Add(blockData);
                }

                foreach (Transform block in invisibleBlocksParent)
                {
                    var blockData = new BlockSaveData(block.position, BlockType.Invisible);
                    invisibleBlocksData.Add(blockData);
                }

                chunkData.groundBlocks = groundBlocksData;
                chunkData.invisibleBlocks = invisibleBlocksData;
            }
        }

        return chunkData;
    }

    /// <summary>
    /// Loads terrain chunk data from save
    /// </summary>
    /// <param name="data">Data from save</param>
    /// <param name="chunkSize">Size of the terrain chunk</param>
    /// <param name="noiseScale">Scale of teh perlin noise</param>
    public void LoadChunkData(TerrainChunkData data,int chunkSize, float noiseScale)
    {
        this.chunkSize = chunkSize;
        this.scale = noiseScale;

        maxChunkHeight = data.chunkHeight;
        NoiseOffset = new Vector2(data.noiseOffset.x, data.noiseOffset.y);
        changedTerrain = data.changedTerrain;
        HeightMap = GenerateHeightMap();

        // If terrain was not changed, restore height map
        if (!changedTerrain)
        {
            HeightMap.LoadImage(data.terrainChunkTexture);
        }
        else
        {
            groundBlocksData = data.groundBlocks;
            invisibleBlocksData = data.invisibleBlocks;            
        }
    }    
}
