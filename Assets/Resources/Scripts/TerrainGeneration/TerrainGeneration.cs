using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGeneration : MonoBehaviour
{
    public static TerrainGeneration instance;

    [Tooltip("Terrain chunk is a square of blocks, this number represents side of that square")]
    public int chunkSize;

    [Tooltip("Bigger number makes terrain more hilly, smaller more flat")]
    public float noiseScale;

    [Tooltip("Noise offset of starting terrain chunk")]
    public Vector2 noiseStartOffset;

    [Tooltip("Terrain is build of smaller terrain chunks arranged in square, this number represents number of chunks along the side of that square")]
    public int numOfChunks = 3;
        
    [SerializeField]
    GameObject terrainChunkPrefab;

    TerrainChunk[,] terrainChunks;

    BlockProperties[] blockProperties;
    BlockProperties lastBlockRequested;

    float time;
    private void Awake()
    {
        terrainChunks = new TerrainChunk[numOfChunks, numOfChunks];

        // TODO: Check for proper singleton implementation
        if (!instance)
        {
            instance = this;
        }

        var blocks = Resources.LoadAll<BlockProperties>("BlockTypes/");
        blockProperties = new BlockProperties[blocks.Length];

        foreach (var item in blocks)
        {
            blockProperties[(int)item.blockType] = item;
        }
    }

    private void Start()
    {
        GenerateTerrain();
    }

    // Update is called once per frame
    void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.N))
        {            
            GenerateTerrain();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            CreateChunkSpace(ref terrainChunks, Direction.UP);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            CreateChunkSpace(ref terrainChunks, Direction.DOWN);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            CreateChunkSpace(ref terrainChunks, Direction.LEFT);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            CreateChunkSpace(ref terrainChunks, Direction.RIGHT);
        }*/
    }

    /// <summary>
    /// Shifts map of chunks and load new ones in the given direction, unload old ones
    /// </summary>
    /// <param name="array">Array of chunks</param>
    /// <param name="direction">Directions where to load new chunks</param>
    void CreateChunkSpace(ref TerrainChunk[,] array, Direction direction)
    {
        switch (direction)
        {
            // Shifts array down and create new chunks at the top
            case Direction.UP:

                // GO throught chunks from bottom to the top
                for (int y = 0; y <= array.GetUpperBound(1); y++)
                {
                    for (int x = 0; x <= array.GetUpperBound(0); x++)
                    {
                        // Unload bottom chunks
                        if (y == 0)
                        {
                            Debug.Log("Disable");
                            // Unload these
                            array[x, y].DisableChunk();
                            array[x, y] = array[x, y + 1];
                        }
                        // Generate new chunks at the top or load if they were generated
                        else if (y == array.GetUpperBound(1))
                        {
                            Debug.Log("New chunk");
                            if (array[x, y - 1].GetNeighbour(Direction.UP) == null)
                            {
                                var chunkUnderPosition = array[x, y - 1].transform.position;
                                var noiseOffset = array[x, y - 1].noiseOffset;

                                array[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkUnderPosition.x, 0, chunkUnderPosition.z + chunkSize), Quaternion.identity).GetComponent<TerrainChunk>();
                                array[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x, noiseOffset.y + noiseScale));
                            }
                            else
                            {
                                array[x, y] = array[x, y - 1].GetNeighbour(Direction.UP);
                                array[x, y].LoadChunk();
                            }
                        }
                        else
                        {
                            array[x, y] = array[x, y + 1];
                        }
                    }
                }

                break;


            case Direction.RIGHT:

                // Go throught chunks from top to bottom
                for (int x = 0; x <= array.GetUpperBound(0); x++)
                {
                    for (int y = 0; y <= array.GetUpperBound(1); y++)
                    {
                        // Unloads chunks at the left side
                        if (x == 0)
                        {
                            array[x, y].DisableChunk();
                            array[x, y] = array[x + 1, y];
                        }
                        // Generate new ones at the right or load old ones
                        else if (x == array.GetUpperBound(0))
                        {
                            if (array[x - 1, y].GetNeighbour(Direction.RIGHT) == null)
                            {
                                var chunkOnLeftPosition = array[x - 1, y].transform.position;
                                var noiseOffset = array[x - 1, y].noiseOffset;

                                array[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkOnLeftPosition.x + chunkSize, 0, chunkOnLeftPosition.z), Quaternion.identity).GetComponent<TerrainChunk>();
                                array[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x + noiseScale, noiseOffset.y));
                            }
                            else
                            {
                                array[x, y] = array[x - 1, y].GetNeighbour(Direction.RIGHT);
                                array[x, y].LoadChunk();
                            }
                        }
                        else
                        {
                            array[x, y] = array[x + 1, y];
                        }
                    }
                }

                break;
            case Direction.DOWN:

                // Go throught chunks from top to bottom
                for (int y = array.GetUpperBound(1); y >= 0; y--)
                {
                    for (int x = 0; x <= array.GetUpperBound(0); x++)
                    {
                        // Generate new chunks at the bottom
                        if (y == 0)
                        {
                            if (array[x, y + 1].GetNeighbour(Direction.DOWN) == null)
                            {
                                var chunkAbovePosition = array[x, y + 1].transform.position;
                                var noiseOffset = array[x, y + 1].noiseOffset;

                                array[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkAbovePosition.x, 0, chunkAbovePosition.z - chunkSize), Quaternion.identity).GetComponent<TerrainChunk>();
                                array[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x, noiseOffset.y - noiseScale));
                            }
                            else
                            {
                                array[x, y] = array[x, y + 1].GetNeighbour(Direction.DOWN);
                                array[x, y].LoadChunk();
                            }
                        }
                        // Unload chunks at the top
                        else if (y == array.GetUpperBound(1))
                        {
                            // Unload these
                            array[x, y].DisableChunk();
                            array[x, y] = array[x, y - 1];
                        }
                        else
                        {
                            array[x, y] = array[x, y - 1];
                        }
                    }
                }
                break;

            case Direction.LEFT:

                for (int x = array.GetUpperBound(0); x >= 0; x--)
                {
                    for (int y = 0; y <= array.GetUpperBound(1); y++)
                    {
                        // Generate new chunks at left or load old ones
                        if (x == 0)
                        {
                            if (array[x + 1, y].GetNeighbour(Direction.LEFT) == null)
                            {
                                var chunkOnRightPosition = array[x + 1, y].transform.position;
                                var noiseOffset = array[x + 1, y].noiseOffset;

                                array[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkOnRightPosition.x - chunkSize, 0, chunkOnRightPosition.z), Quaternion.identity).GetComponent<TerrainChunk>();
                                array[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x - noiseScale, noiseOffset.y));
                            }
                            else
                            {
                                array[x, y] = array[x + 1, y].GetNeighbour(Direction.LEFT);
                                array[x, y].LoadChunk();
                            }
                        }
                        // Disable chunks on right
                        else if (x == array.GetUpperBound(0))
                        {
                            // Unload these
                            array[x, y].DisableChunk();
                            array[x, y] = array[x - 1, y];
                        }
                        else
                        {
                            array[x, y] = array[x - 1, y];
                        }
                    }
                }
                break;
            default:
                break;
        }

        SetChunkNeighbours(terrainChunks);
    }

    void GenerateTerrain()
    {
        time = Time.realtimeSinceStartup;

        terrainChunks[0, 0] = Instantiate(terrainChunkPrefab, Vector3.zero, Quaternion.identity).GetComponent<TerrainChunk>();
        terrainChunks[0, 0].GenerateChunk(chunkSize, noiseScale, noiseStartOffset);

        for (int x = 0; x <= terrainChunks.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= terrainChunks.GetUpperBound(1); y++)
            {
                if (terrainChunks[x, y])
                    continue;

                terrainChunks[x, y] = Instantiate(terrainChunkPrefab, new Vector3(x * chunkSize, 0, y * chunkSize), Quaternion.identity).GetComponent<TerrainChunk>();
                terrainChunks[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(x * noiseScale + noiseStartOffset.x, y * noiseScale + noiseStartOffset.y));
            }
        }

        SetChunkNeighbours(terrainChunks);

        Debug.Log(Time.realtimeSinceStartup - time);
    }

    void SetChunkNeighbours(TerrainChunk[,] chunks)
    {
        for (int x = 0; x <= chunks.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= chunks.GetUpperBound(1); y++)
            {
                // Left-most side
                if (x == 0)
                {
                    chunks[x, y].SetNeighbour(Direction.RIGHT, chunks[x + 1, y]);
                }
                // Righ-most side
                else if (x == chunks.GetUpperBound(0))
                {
                    chunks[x, y].SetNeighbour(Direction.LEFT, chunks[x - 1, y]);
                }
                else
                {
                    chunks[x, y].SetNeighbour(Direction.RIGHT, chunks[x + 1, y]);
                    chunks[x, y].SetNeighbour(Direction.LEFT , chunks[x - 1, y]);
                }
            }
        }

        for (int y = 0; y <= chunks.GetUpperBound(1); y++)
        {
            for (int x = 0; x <= chunks.GetUpperBound(0); x++)
            {
                // Bottom side
                if (y == 0)
                {
                    chunks[x, y].SetNeighbour(Direction.UP, chunks[x, y + 1]);
                }
                // Top side
                else if (y == chunks.GetUpperBound(1))
                {
                    chunks[x, y].SetNeighbour(Direction.DOWN, chunks[x, y - 1]);
                }
                else
                {
                    chunks[x, y].SetNeighbour(Direction.UP, chunks[x, y + 1]);
                    chunks[x, y].SetNeighbour(Direction.DOWN, chunks[x, y - 1]);
                }
            }
        }
    }

    public BlockProperties GetBlockProperties(BlockType type)
    {
        return blockProperties[(int)type];
    }

    /// <summary>
    /// Get BlockType based on height in the world
    /// </summary>
    /// <param name="height">Height of the block</param>
    /// <param name="maxChunkHeight">MaxHeight in the chunk</param>
    /// <param name="topBlock">Is this top block?</param>
    /// <returns>BlockType based on given height</returns>
    public BlockType BlockTypeOnHeight(int height, float maxChunkHeight, bool topBlock)
    {
        var blockPositionHeightRatio = height / maxChunkHeight;

        // It's very likely that we request the same type of block as in previous request
        if (lastBlockRequested && lastBlockRequested.WithinHeightRange(blockPositionHeightRatio))
        {
            return (topBlock) ? lastBlockRequested.blockType : lastBlockRequested.blockUnderType;
        }
        else
        {
            // Find appropriate block
            foreach (var block in blockProperties)
            {
                if (block.WithinHeightRange(blockPositionHeightRatio))
                {
                    lastBlockRequested = block;
                    return (topBlock) ? block.blockType : block.blockUnderType;
                }
            }

            Debug.LogError("Didn't find any suitable block type");
            return BlockType.None;
        }
    }
}
