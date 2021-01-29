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

    public GameObject terrainChunksParent;
        
    [SerializeField]
    GameObject terrainChunkPrefab;

    TerrainChunk[,] terrainChunks;

    BlockProperties[] blockProperties;
    BlockProperties lastBlockRequested;

    GameObject player;

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

    public void GenerateTerrain(bool fromSave, TerrainSaveData terrainData = null)
    {
        if (fromSave)
        {
            LoadTerrain(terrainData);
        }
        else
        {
            GenerateNewTerrain();
        }

        player = GameManager.instance.GetPlayer();
        StartCoroutine(ShiftTerrainOnPlayerPosition());
    }

    void GenerateNewTerrain()
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

    /// <summary>
    /// Shifts map of chunks and load new ones in the given direction, unload old ones
    /// </summary>
    /// <param name="terrainChunks">Array of chunks</param>
    /// <param name="direction">Directions where to load new chunks</param>
    void CreateChunkSpace(Direction direction)
    {
        switch (direction)
        {
            // Shifts array down and create new chunks at the top
            case Direction.UP:

                // GO throught chunks from bottom to the top
                for (int y = 0; y <= terrainChunks.GetUpperBound(1); y++)
                {
                    for (int x = 0; x <= terrainChunks.GetUpperBound(0); x++)
                    {
                        // Unload bottom chunks
                        if (y == 0)
                        {
                            Debug.Log("Disable");
                            // Unload these
                            terrainChunks[x, y].DisableChunk();
                            terrainChunks[x, y] = terrainChunks[x, y + 1];
                        }
                        // Generate new chunks at the top or load if they were generated
                        else if (y == terrainChunks.GetUpperBound(1))
                        {
                            Debug.Log("New chunk");
                            if (terrainChunks[x, y - 1].GetNeighbour(Direction.UP) == null)
                            {
                                var chunkUnderPosition = terrainChunks[x, y - 1].transform.position;
                                var noiseOffset = terrainChunks[x, y - 1].noiseOffset;

                                terrainChunks[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkUnderPosition.x, 0, chunkUnderPosition.z + chunkSize), Quaternion.identity).GetComponent<TerrainChunk>();
                                terrainChunks[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x, noiseOffset.y + noiseScale));
                            }
                            else
                            {
                                terrainChunks[x, y] = terrainChunks[x, y - 1].GetNeighbour(Direction.UP);
                                terrainChunks[x, y].LoadChunk();
                            }
                        }
                        else
                        {
                            terrainChunks[x, y] = terrainChunks[x, y + 1];
                        }
                    }
                }

                break;


            case Direction.RIGHT:

                // Go throught chunks from top to bottom
                for (int x = 0; x <= terrainChunks.GetUpperBound(0); x++)
                {
                    for (int y = 0; y <= terrainChunks.GetUpperBound(1); y++)
                    {
                        // Unloads chunks at the left side
                        if (x == 0)
                        {
                            terrainChunks[x, y].DisableChunk();
                            terrainChunks[x, y] = terrainChunks[x + 1, y];
                        }
                        // Generate new ones at the right or load old ones
                        else if (x == terrainChunks.GetUpperBound(0))
                        {
                            if (terrainChunks[x - 1, y].GetNeighbour(Direction.RIGHT) == null)
                            {
                                var chunkOnLeftPosition = terrainChunks[x - 1, y].transform.position;
                                var noiseOffset = terrainChunks[x - 1, y].noiseOffset;

                                terrainChunks[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkOnLeftPosition.x + chunkSize, 0, chunkOnLeftPosition.z), Quaternion.identity).GetComponent<TerrainChunk>();
                                terrainChunks[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x + noiseScale, noiseOffset.y));
                            }
                            else
                            {
                                terrainChunks[x, y] = terrainChunks[x - 1, y].GetNeighbour(Direction.RIGHT);
                                terrainChunks[x, y].LoadChunk();
                            }
                        }
                        else
                        {
                            terrainChunks[x, y] = terrainChunks[x + 1, y];
                        }
                    }
                }

                break;
            case Direction.DOWN:

                // Go throught chunks from top to bottom
                for (int y = terrainChunks.GetUpperBound(1); y >= 0; y--)
                {
                    for (int x = 0; x <= terrainChunks.GetUpperBound(0); x++)
                    {
                        // Generate new chunks at the bottom
                        if (y == 0)
                        {
                            if (terrainChunks[x, y + 1].GetNeighbour(Direction.DOWN) == null)
                            {
                                var chunkAbovePosition = terrainChunks[x, y + 1].transform.position;
                                var noiseOffset = terrainChunks[x, y + 1].noiseOffset;

                                terrainChunks[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkAbovePosition.x, 0, chunkAbovePosition.z - chunkSize), Quaternion.identity).GetComponent<TerrainChunk>();
                                terrainChunks[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x, noiseOffset.y - noiseScale));
                            }
                            else
                            {
                                terrainChunks[x, y] = terrainChunks[x, y + 1].GetNeighbour(Direction.DOWN);
                                terrainChunks[x, y].LoadChunk();
                            }
                        }
                        // Unload chunks at the top
                        else if (y == terrainChunks.GetUpperBound(1))
                        {
                            // Unload these
                            terrainChunks[x, y].DisableChunk();
                            terrainChunks[x, y] = terrainChunks[x, y - 1];
                        }
                        else
                        {
                            terrainChunks[x, y] = terrainChunks[x, y - 1];
                        }
                    }
                }
                break;

            case Direction.LEFT:

                for (int x = terrainChunks.GetUpperBound(0); x >= 0; x--)
                {
                    for (int y = 0; y <= terrainChunks.GetUpperBound(1); y++)
                    {
                        // Generate new chunks at left or load old ones
                        if (x == 0)
                        {
                            if (terrainChunks[x + 1, y].GetNeighbour(Direction.LEFT) == null)
                            {
                                var chunkOnRightPosition = terrainChunks[x + 1, y].transform.position;
                                var noiseOffset = terrainChunks[x + 1, y].noiseOffset;

                                terrainChunks[x, y] = Instantiate(terrainChunkPrefab, new Vector3(chunkOnRightPosition.x - chunkSize, 0, chunkOnRightPosition.z), Quaternion.identity).GetComponent<TerrainChunk>();
                                terrainChunks[x, y].GenerateChunk(chunkSize, noiseScale, new Vector2(noiseOffset.x - noiseScale, noiseOffset.y));
                            }
                            else
                            {
                                terrainChunks[x, y] = terrainChunks[x + 1, y].GetNeighbour(Direction.LEFT);
                                terrainChunks[x, y].LoadChunk();
                            }
                        }
                        // Disable chunks on right
                        else if (x == terrainChunks.GetUpperBound(0))
                        {
                            // Unload these
                            terrainChunks[x, y].DisableChunk();
                            terrainChunks[x, y] = terrainChunks[x - 1, y];
                        }
                        else
                        {
                            terrainChunks[x, y] = terrainChunks[x - 1, y];
                        }
                    }
                }
                break;
            default:
                break;
        }

        SetChunkNeighbours(this.terrainChunks);
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

    /// <summary>
    /// Check player position and shifts array if on the edge
    /// </summary>    
    IEnumerator ShiftTerrainOnPlayerPosition()
    {
        while (!player)
        {
            Debug.Log("No player");
            player = GameManager.instance.GetPlayer();
            yield return new WaitForEndOfFrame();
        }

        while (player)
        {
            Debug.Log("Checking");

            // Get the player distance from down-left corner of chunks
            var xDif = player.transform.position.x - terrainChunks[0, 0].transform.position.x;
            var zDif = player.transform.position.z - terrainChunks[0, 0].transform.position.z;

            // Calculate position within chunks array
            var xPos = Mathf.Floor(xDif / chunkSize);
            var yPos = Mathf.Floor(zDif / chunkSize);

            var shiftDirection = new List<Direction>();

            // Look at which edge we are
            if (xPos == 0)
            {
                shiftDirection.Add(Direction.LEFT);
            }

            if (xPos == terrainChunks.GetUpperBound(0))
            {
                shiftDirection.Add(Direction.RIGHT);
            }

            if (yPos == 0)
            {
                shiftDirection.Add(Direction.DOWN);
            }

            if (yPos == terrainChunks.GetUpperBound(1))
            {
                shiftDirection.Add(Direction.UP);
            }

            // Shift terrain in appropriate direction
            foreach (var direction in shiftDirection)
            {
                CreateChunkSpace(direction);
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    /// Returns chunk based on given position in the world
    /// </summary>
    /// <param name="x">X position in the world</param>
    /// <param name="z">Z position in the world</param>
    /// <returns></returns>
    public TerrainChunk GetChunkOnPosition(float x, float z)
    {
        var posX = Mathf.Floor((x - terrainChunks[0, 0].transform.position.x) / chunkSize);
        var posY = Mathf.Floor((z - terrainChunks[0, 0].transform.position.z) / chunkSize);        

        return terrainChunks[(int)posX, (int)posY];
    }

    /// <summary>
    /// Get center position int the generated world
    /// </summary>
    /// <returns>Position in the center of the terrain</returns>
    public Vector3 GetCenterPosition()
    {
        var posX = terrainChunks[0, 0].transform.position.x + (numOfChunks * chunkSize) / 2f;
        var posY = 30;
        var posZ = terrainChunks[0, 0].transform.position.z + (numOfChunks * chunkSize) / 2f;

        return new Vector3(posX, posY, posZ);
    }

    /// <summary>
    /// Fills up information about this terrain
    /// </summary>
    /// <returns>Filled up TerrainSaveData</returns>
    public TerrainSaveData SaveTerrain()
    {
        var terrainData = new TerrainSaveData();
        terrainData.startNoiseOffset = new SVector2();

        terrainData.chunkSize = chunkSize;
        terrainData.noiseScale = noiseScale;
        terrainData.startNoiseOffset.x = noiseStartOffset.x;
        terrainData.startNoiseOffset.y = noiseStartOffset.y;
        terrainData.numOfChunks = numOfChunks;
        terrainData.terrainChunks = new List<SVector2>();

        foreach (Transform chunk in terrainChunksParent.transform)
        {
            terrainData.terrainChunks.Add(new SVector2(chunk.position.x, chunk.position.z));
        }

        return terrainData;
    }

    public List<TerrainChunkData> SaveChunksIndividually()
    {
        var terrainChunkData = new List<TerrainChunkData>();
        for (int i = 0; i < terrainChunksParent.transform.childCount; i++)
        {
            var chunk = terrainChunksParent.transform.GetChild(i).GetComponent<TerrainChunk>();
            terrainChunkData.Add(chunk.SaveChunkData());
        }

        return terrainChunkData;
    }

    public void LoadTerrain(TerrainSaveData terrainData)
    {
        chunkSize = terrainData.chunkSize;
        noiseScale = terrainData.noiseScale;
        noiseStartOffset = new Vector2(terrainData.startNoiseOffset.x, terrainData.startNoiseOffset.y);
        numOfChunks = terrainData.numOfChunks;
        terrainChunks = new TerrainChunk[numOfChunks, numOfChunks];

        foreach (var chunkPosition in terrainData.terrainChunks)
        {
            var data = SaveManager.instance.LoadChunkData(chunkPosition.GetVector2());
            var position = new Vector3(data.worldPosition.x, 0, data.worldPosition.y);

            var chunk = Instantiate(terrainChunkPrefab, position, Quaternion.identity, terrainChunksParent.transform).GetComponent<TerrainChunk>();
            chunk.LoadChunkData(data, chunkSize, noiseScale);
        }
        
        var playerPosition = SaveManager.instance.LoadPlayerPosition();

        // Get world position of center chunk
        var posX = Mathf.FloorToInt(playerPosition.x / chunkSize) * chunkSize;
        var posZ = Mathf.FloorToInt(playerPosition.z / chunkSize) * chunkSize;
              
        // Go throught loaded chunks and assign active ones
        foreach (Transform chunk in terrainChunksParent.transform)
        {
            if (Mathf.Abs(posX - chunk.position.x) <= chunkSize && Mathf.Abs(posZ - chunk.position.z) <= chunkSize)
            {
                var x = 1 - (posX - chunk.position.x) / chunkSize;
                var y = 1 - (posZ - chunk.position.z) / chunkSize;
                var terrainChunk = chunk.GetComponent<TerrainChunk>();
                terrainChunks[(int)x, (int)y] = terrainChunk;
                terrainChunk.LoadChunk();
            }
        }
    }
}
