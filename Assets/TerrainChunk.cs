using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChunkNeighbour{ UP, RIGHT, DOWN, LEFT};
public class TerrainChunk : MonoBehaviour
{
    public int maxChunkHeight;
    // Representation of terrain chunk in height map
    public Texture2D heightMap { get; private set; }
    
    TerrainChunk[] neighbours = new TerrainChunk[4];

    Vector2 noiseOffset;    
    int chunkSize;
    float scale;
    
    public TerrainChunk GetNeighbour(ChunkNeighbour direction)
    {
        return neighbours[(int)direction];
    }

    public void SetNeighbour(ChunkNeighbour direction, TerrainChunk chunk)
    {
        neighbours[(int)direction] = chunk;
    }

    public void GenerateChunk(int size, float noiseScale, Vector2 noiseOffset)
    {
        this.chunkSize = size;
        this.noiseOffset = noiseOffset;
        this.scale = noiseScale;        

        heightMap = GenerateHeightMap();
        LoadChunk();
    }

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

    private Color GetNoise(float x, float y)
    {
        var xCoord = x / chunkSize * scale + noiseOffset.x;
        var yCoord = y / chunkSize * scale + noiseOffset.y;

        var noise = Mathf.PerlinNoise(xCoord, yCoord);

        return new Color(noise, noise, noise);
    }

    void GenerateNeighbour(ChunkNeighbour neighbour)
    {

    }

    void SaveChunk()
    {
        // From quads to height map
    }

    void LoadChunk()
    {
        if (!heightMap)
            Debug.LogError("No heightMap present, cannot generate cubes");

        var cube = Resources.Load<GameObject>("Prefabs/Quad");

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                var height = GetBlockHeight(x, y);

                var position = new Vector3(transform.position.x + x, height, transform.position.z + y);
                var block = Instantiate(cube, position, Quaternion.identity, this.gameObject.transform);

                var blockUnderCount = BlocksUnder(x, y, height);

                for (int i = 0; i < blockUnderCount; i++)
                {
                    position.y--;
                    Instantiate(cube, position, Quaternion.identity, this.gameObject.transform);
                }
            }
        }
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
