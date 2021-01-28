using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction{ UP, RIGHT, DOWN, LEFT};
public class TerrainChunk : MonoBehaviour
{
    public int maxChunkHeight;
    public Transform invisibleBlocks;
    public Transform groundBlocks;

    // Representation of terrain chunk in height map
    public Texture2D heightMap { get; private set; }
    
    TerrainChunk[] neighbours = new TerrainChunk[4];

    public Vector2 noiseOffset { get; private set; }
    int chunkSize;
    float scale;
    
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

    /// <summary>
    /// Disables blocks of the chunk
    /// </summary>
    public void DisableChunk()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var cube = transform.GetChild(i).gameObject;
            BlocksPool.instance.GiveBlockToPool(cube);
        }
    }

    /// <summary>
    /// Loads chunk from heightmap
    /// </summary>
    public void LoadChunk()
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
                block.transform.parent = groundBlocks;

                // Calculate number of block to generate under current one to fill gap
                var blockUnderCount = BlocksUnder(x, y, height);                

                for (int i = 0; i < blockUnderCount; i++)
                {
                    position.y--;
                    blockType = TerrainGeneration.instance.BlockTypeOnHeight((int)position.y, maxChunkHeight, false);

                    block = BlocksPool.instance.GetBlockFromPool(blockType);
                    block.transform.position = position;
                    block.transform.parent = groundBlocks;                    
                }

                if (!Physics.Raycast(position, Vector3.down, 1f))
                {
                    block = BlocksPool.instance.GetBlockFromPool(BlockType.Invisible);
                    position.y--;
                    block.transform.position = position;
                    block.transform.parent = invisibleBlocks;
                }
            }
        }

        Physics.SyncTransforms();

        var blocksUnder = invisibleBlocks.transform.GetComponentsInChildren<Block>();
        foreach (var block in blocksUnder)
        {
            //Debug.DrawRay(block.transform.position, Vector3.down, Color.blue, 1000f);
            
            bool createNextBlock = true;
            var position = block.transform.position;

            while(createNextBlock)
            {
                createNextBlock = false;
                if (!Physics.Raycast(position, Vector3.down, 1f))
                {
                    if (IsBlockInFourDirections(position + Vector3.down))
                    {
                        position = position + Vector3.down;
                        var invBlock = BlocksPool.instance.GetBlockFromPool(BlockType.Invisible);
                        invBlock.transform.position = position;
                        invBlock.transform.parent = invisibleBlocks;

                        createNextBlock = true;
                    }
                }
            }
        }
    }

    bool IsBlockInFourDirections(Vector3 position)
    {
        Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right};
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

    private List<Vector3> OutTerrainDirection(int x, int y)
    {
        var height = maxChunkHeight;
        Vector3[] directions = new Vector3[] {Vector3.left, Vector3.right, Vector3.forward, Vector3.back };        
        List<int> indexes = new List<int>();

        for (int i = 0; i < 4; i++)        
        {
            var direction = directions[i];
            if (x + direction.x > chunkSize || 
                x + direction.x < 0 || 
                y + direction.z > chunkSize || 
                y + direction.z < 0)
                continue;

            var blockHeight = GetBlockHeight(x + (int)direction.x, y + (int)direction.z);
            if (blockHeight <= height)
            {
                if (blockHeight == height)
                {
                    indexes.Add(i); 
                }
                else
                {
                    height = blockHeight;
                    indexes.Clear();
                    indexes.Add(i);
                }
                
            }
        }        

        var directionsResult = new List<Vector3>();
        if (height == GetBlockHeight(x, y))
        {
            return directionsResult;
        }

        for (int i = 0; i < directions.Length; i++)
        {
            if (indexes.Contains(i))
                continue;

            directionsResult.Add(directions[i]);
        }
        /*
        foreach (var index in indexes)
        {
            directionsResult.Add(directions[index]);
        }*/

        return directionsResult;
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
}
