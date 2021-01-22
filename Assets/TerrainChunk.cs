using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction{ UP, RIGHT, DOWN, LEFT};
public class TerrainChunk : MonoBehaviour
{
    public int maxChunkHeight;
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
            BlocksPool.instance.GiveBlockToPool(cube, BlockType.DirtGrass);
        }
    }

    /// <summary>
    /// Loads chunk from heightmap
    /// </summary>
    public void LoadChunk()
    {
        var time = Time.realtimeSinceStartup;
        if (!heightMap)
            Debug.LogError("No heightMap present, cannot generate cubes");

        var cube = Resources.Load<GameObject>("Prefabs/Quad");

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                var height = GetBlockHeight(x, y);

                var position = new Vector3(transform.position.x + x, height, transform.position.z + y);

                var block = BlocksPool.instance.GetBlockFromPool(BlockType.DirtGrass);
                block.transform.position = position;
                block.transform.parent = this.transform;

                var blockUnderCount = BlocksUnder(x, y, height);

                for (int i = 0; i < blockUnderCount; i++)
                {
                    position.y--;
                    block = BlocksPool.instance.GetBlockFromPool(BlockType.DirtGrass);
                    block.transform.position = position;
                    block.transform.parent = this.transform;
                }
            }
        }
        Debug.Log("Loading time" + (Time.realtimeSinceStartup - time));
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
}
